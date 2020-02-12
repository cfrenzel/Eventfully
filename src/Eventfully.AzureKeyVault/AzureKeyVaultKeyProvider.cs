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
                 return client;
            }
        }
    }

}
