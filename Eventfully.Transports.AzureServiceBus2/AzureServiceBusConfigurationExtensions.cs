using System;

namespace Eventfully.Transports.AzureServiceBus2
{

    public static class AzureServiceBusConfigurationExtensions
    {
        public static AzureServiceBusTransportSettings UseAzureServiceBus(this MessagingConfiguration config, string connectionString,
            Action<AzureServiceBusTransportSettings> configBuilder)
        {
            var settings = new AzureServiceBusTransportSettings(connectionString);
            configBuilder.Invoke(settings);
            config.Services.AddSingleton<AzureServiceBusTransportSettings>(settings);
            config.Services.AddSingleton<ITransport2,AzureServiceBusTransport>(true);
            return settings;
        }

    }
}