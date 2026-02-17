namespace AdsApi.Infrastructure.KeyVault;

public interface IKeyVaultService
	{
	Task<string> GetSecretAsync (string name, CancellationToken ct = default);
	Task<string?> TryGetSecretAsync (string name, CancellationToken ct = default);
	}
