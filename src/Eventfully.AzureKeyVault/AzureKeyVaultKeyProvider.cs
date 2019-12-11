using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using System;
using System.Collections.Generic;

namespace Eventfully
{

    public class AzureKeyVaultKeyProvider : IEncryptionKeyProvider
    {
        private readonly string _keyVaultUrl;
        private static readonly  Dictionary<string, KeyVaultClient> _clients = new Dictionary<string, KeyVaultClient>();

        public AzureKeyVaultKeyProvider(string keyVaultuUrl)
        {
            _keyVaultUrl = keyVaultuUrl;
        }

        public string GetKey(string secretName = null)
        {
            var client = _getClient();
            //var secret = client.GetSecret(secretName);
            //return secret.Value.Value;
            var secret = client.GetSecretAsync(_keyVaultUrl, secretName).GetAwaiter().GetResult();
            return secret.Value;
        }

        private KeyVaultClient _getClient()
        {
            if (_clients.ContainsKey(this._keyVaultUrl))
                return _clients[_keyVaultUrl];
            else
            {

                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                var client = new KeyVaultClient(
                    new KeyVaultClient.AuthenticationCallback(
                        azureServiceTokenProvider.KeyVaultTokenCallback));

               
                //var azureServiceTokenProvider1 = new AzureServiceTokenProvider();
                //var client = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider1.KeyVaultTokenCallback));
                //_clients.Add(_keyVaultUrl, client);
                //var secret = kv.GetSecretAsync(_keyVaultUrl, "test2").GetAwaiter().GetResult();
                //_clients.Add(_keyVaultUrl, client);

                // Create a new secret client using the default credential from Azure.Identity using environment variables previously set,
                // including AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, and AZURE_TENANT_ID.
                //var client = new SecretClient(vaultUri: new Uri(_keyVaultUrl), credential: new DefaultAzureCredential());
                //_clients.Add(_keyVaultUrl, client);
                return client;
            }
        }
    }

}
