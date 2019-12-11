using Eventfully.Transports;
using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{
    public static class TransportConfigurationExtensions
    {
        public static LocalTransportSettings UseLocalTransport(this IEndpointFluent endpointSettings)
        {
            var settings = new LocalTransportSettings();
            endpointSettings.TransportSettings = settings;
            return settings;
        }
    }
}
