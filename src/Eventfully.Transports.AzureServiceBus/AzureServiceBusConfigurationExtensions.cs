using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully.Transports.AzureServiceBus
{

   

    public static class AzureServiceBusConfigurationExtensions
    {
        public static AzureServiceBusTransportSettings UseAzureServiceBusTransport(this IEndpointFluent endpointSettings)
        {
            var settings = new AzureServiceBusTransportSettings();
            endpointSettings.TransportSettings = settings;
            return settings;
        }

        //public static AzureServiceBusTransportConfiguration UseAzureServiceBusTransport(this Profile profile)
        //{
        //    var transport = new AzureServiceBusTransport();
        //    var aconfig = new AzureServiceBusTransportConfiguration();
        //    //profile.RegisterTransport(transport, aconfig);
        //    return aconfig;
        //}
    }
}
