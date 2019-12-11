
Azure Key Vault Secrets -  Managed Service Identity based key provider
https://docs.microsoft.com/en-us/samples/azure-samples/app-service-msi-keyvault-dotnet/keyvault-msi-appservice-sample/

Configuring Azure Key Vault for use as EncryptionKeyProvider

1) Generate/Use existing AES key of length: 128, 192 or 256
- ex: openssl rand 128 > sym_keyfile.key

2) Import Key into Key Vault
 - Create a New Secret in the KeyVault
	- Name:  <yourkeyname>
	- Value: <yourkeytext>
	- ContentType: application/octet-stream

3) Authenticating with the KeyVault
    - By default the provider will Managed Service Identity to transparently authenticate ( make sure you app has permissions to acces the vault).
	- This works great while the app is running in azure, but when developing in VS you need to be logged into Azure with an account that has access


4) Using the provider
 
 ***** Using the provider for a single message type *********

 ConfigureEndpoint("TestEndpoint")
                .AsInboundOutbound()
                    .BindEvent<TestEvent>()
                    .UseEncryption(
                           "yourkeyname", 
                           new AzureKeyVaultKeyProvider("yourkeyvaulturl")
					)
                .UseAzureServiceBusTransport()
                ;

