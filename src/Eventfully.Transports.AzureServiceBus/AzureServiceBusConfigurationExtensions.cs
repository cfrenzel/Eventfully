using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully.Transports.AzureServiceBus
{

   
    public class WrappedTransportSettings : EndpointSubSettings
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

    }

    public static class AzureServiceBusConfigurationExtensions
    {
        public static WrappedTransportSettings UseAzureServiceBusTransport(this IEndpointFluent endpointSettings)
        {
            var settings = new AzureServiceBusTransportSettings();
            endpointSettings.TransportSettings = settings;
            return new WrappedTransportSettings(endpointSettings, settings);
        }
       
    }
}
