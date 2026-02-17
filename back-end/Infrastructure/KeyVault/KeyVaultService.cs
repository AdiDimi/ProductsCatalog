
using Azure;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace AdsApi.Infrastructure.KeyVault;

public sealed class KeyVaultService : IKeyVaultService
	{
	private readonly SecretClient _client;
	private readonly IMemoryCache _cache;
	private readonly ILogger<KeyVaultService> _log;
	private readonly KeyVaultOptions _options;

	// Prevent stampede when many requests ask for the same secret concurrently.
	private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);

	public KeyVaultService (
		SecretClient client,
		IMemoryCache cache,
		IOptions<KeyVaultOptions> options,
		ILogger<KeyVaultService> log)
		{
		_client = client;
		_cache = cache;
		_log = log;
		_options = options.Value;
		}

	public async Task<string> GetSecretAsync (string name, CancellationToken ct = default)
		{
		var value = await TryGetSecretAsync(name, ct).ConfigureAwait(false);
		return value ?? throw new KeyNotFoundException($"KeyVault secret '{name}' was not found.");
		}

	public async Task<string?> TryGetSecretAsync (string name, CancellationToken ct = default)
		{
		if (!_options.Enabled)
			{
			_log.LogDebug("KeyVault is disabled. Returning null for secret '{SecretName}'.", name);
			return null;
			}

		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("Secret name is required.", nameof(name));

		var cacheKey = $"kv:{name}";
		if (_cache.TryGetValue(cacheKey, out string? cached) && !string.IsNullOrEmpty(cached))
			return cached;

		var gate = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
		await gate.WaitAsync(ct).ConfigureAwait(false);

		try
			{
			// Re-check cache after acquiring lock
			if (_cache.TryGetValue(cacheKey, out cached) && !string.IsNullOrEmpty(cached))
				return cached;

			try
				{
				// Current version (no version specified)
				Response<KeyVaultSecret> resp = await _client.GetSecretAsync(name, cancellationToken: ct)
					.ConfigureAwait(false);

				var secretValue = resp.Value.Value;

				_cache.Set(cacheKey, secretValue, new MemoryCacheEntryOptions
					{
					AbsoluteExpirationRelativeToNow = _options.CacheTtl
					});
				_log.LogInformation("KeyVault secret '{secretValue}' found.", secretValue);
				return secretValue;
				}
			catch (RequestFailedException ex) when (ex.Status == 404)
				{
				_log.LogWarning("KeyVault secret '{SecretName}' not found (404).", name);
				_cache.Set<string?>(cacheKey, null, new MemoryCacheEntryOptions
					{
					AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1) // short negative cache
					});
				//_cache.Set(cacheKey, null, new MemoryCacheEntryOptions
				//	{
				//	AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1) // short negative cache
				//	});
				return null;
				}
			}
		finally
			{
			gate.Release();
			}
		}
	}
