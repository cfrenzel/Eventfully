using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully.Transports.AzureServiceBus
{

   
    /*public class WrappedTransportSettings : EndpointSubSettings
    {
        private AzureServiceBusTransportSettings _transportSettings;
        public WrappedTransportSettings(IEndpointFluent endpointSettings, AzureServiceBusTransportSettings transportSettings) : base(endpointSettings)
        {
            _transportSettings = transportSettings;
        }

        public bool SpecialFeature {
            get { return _transportSettings.SpecialFeature; }
            set { _transportSettings.SpecialFeature = value; }
        }
    }*/
    
    public static class AzureServiceBusConfigurationExtensions
    {
        public static AzureServiceBusTransportSettings UseAzureServiceBus(this MessagingConfiguration config, string connectionString, Action<AzureServiceBusTransportSettings> configBuilder = null)
        {
            //return AddMessaging(services, profile, null, null, messageAndHandlerAssemblies);
            var settings = new AzureServiceBusTransportSettings(connectionString);
            if (configBuilder != null)
                configBuilder.Invoke(settings);
            //config.TransportSettings.Add(settings);
            return settings;
            //endpointSettings.TransportSettings = settings;
            //return new WrappedTransportSettings(endpointSettings, settings);
        }
        // public static WrappedTransportSettings UseAzureServiceBus(this IEndpointFluent endpointSettings)
        // {
        //     var settings = new AzureServiceBusTransportSettings();
        //     endpointSettings.TransportSettings = settings;
        //     return new WrappedTransportSettings(endpointSettings, settings);
        // }
       
    }
}
