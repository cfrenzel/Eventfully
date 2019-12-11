﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully.Transports.AzureServiceBus
{
    public class AzureServiceBusTransportFactory : ITransportFactory
    {
        public Transport Create(TransportSettings settings)
        {
            AzureServiceBusTransport transport = new AzureServiceBusTransport(settings);
            return transport;
        }
    }
}
