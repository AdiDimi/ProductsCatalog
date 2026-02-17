using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace AdsApi.Infrastructure.KeyVault;

public static class KeyVaultExtentions
	{
	public static void AddKeyVaultOptions (this WebApplicationBuilder builder)
		{
		builder.Services.Configure<KeyVaultOptions>(
		builder.Configuration.GetSection("KeyVault"));

		builder.Services.AddMemoryCache();

		builder.Services.AddSingleton(sp =>
		{
			var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<KeyVaultOptions>>().Value;

			if (string.IsNullOrWhiteSpace(options.VaultUri))
				throw new InvalidOperationException("KeyVault:VaultUri is missing.");

			var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
				{
				// optional: speed up local dev by excluding some sources if you want
				// ExcludeInteractiveBrowserCredential = true
				});

			return new SecretClient(new Uri(options.VaultUri), credential);
		});

		builder.Services.AddSingleton<IKeyVaultService, KeyVaultService>();
		}
	}

