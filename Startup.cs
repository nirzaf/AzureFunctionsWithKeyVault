using System;
using System.Collections.Generic;

using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using AzureFunctionsWithKeyVault;

using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using static System.Environment;

[assembly: FunctionsStartup(typeof(Startup))] // <--- This is the important line 

namespace AzureFunctionsWithKeyVault;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        #region Access Key Vault

        string KeyVaultUrl = GetEnvironmentVariable("KeyVaultUrl")!;
        string ClientId = GetEnvironmentVariable("ClientId")!;
        string ClientSecret = GetEnvironmentVariable("ClientSecret")!;
        string TenantId = GetEnvironmentVariable("TenantId")!;

        var executionContextOptions = builder.Services.BuildServiceProvider()
            .GetService<IOptions<ExecutionContextOptions>>()!.Value;
        
        var keyVaultClient = new SecretClient(new Uri(KeyVaultUrl),
            new ClientSecretCredential(TenantId, ClientId, ClientSecret));
        
        var config = new ConfigurationBuilder()
            .AddAzureKeyVault(keyVaultClient, new AzureKeyVaultConfigurationOptions())
            .SetBasePath(executionContextOptions.AppDirectory)
            .AddJsonFile("local.settings.json", true, true)
            .AddEnvironmentVariables()
            .Build();

        builder.Services.AddSingleton<IConfiguration>(config);

        //fetching secrets from key vault 
        IEnumerable<SecretProperties> secrets = keyVaultClient.GetPropertiesOfSecrets();

        // setting fetched secrets to environment variables
        foreach (SecretProperties secret in secrets)
        {
            string secretValue = keyVaultClient.GetSecret(secret.Name).Value.Value;
            // setting secret value to environment variable
            SetEnvironmentVariable(secret.Name, secretValue);
        }
        
        #endregion
    }
}