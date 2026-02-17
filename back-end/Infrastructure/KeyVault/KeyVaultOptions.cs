// Infrastructure/KeyVault/KeyVaultOptions.cs
namespace AdsApi.Infrastructure.KeyVault;

public sealed class KeyVaultOptions
	{
	public string? VaultUri { get; init; } = default; // e.g. https://my-vault.vault.azure.net/
	public TimeSpan CacheTtl { get; init; } = TimeSpan.FromMinutes(10);
	public bool Enabled { get; init; } = true;
	}

