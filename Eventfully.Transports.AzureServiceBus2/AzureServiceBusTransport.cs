using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Eventfully.Transports.AzureServiceBus2
{
    public class AzureServiceBusTransport : ITransport2
    {
        private readonly IEnumerable<AzureServiceBusTransportSettings> _settings;
        private List<AzureServiceBusMessagePump> _pumps = new List<AzureServiceBusMessagePump>();
        
        private List<IEndpoint2> Endpoints = new List<IEndpoint2>();
        private readonly IServiceFactory _services;
        
        public AzureServiceBusTransport(IEnumerable<AzureServiceBusTransportSettings> settings, IServiceFactory services)
        {
            _settings = settings;
            _services = services;
        }
        public bool SupportsDelayedDispatch => true;
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            //Create a ASB client for each group of Transport Settings registered
            //Potentially more than one ASB namespace could be used
            foreach (var settings in _settings)
                InitializeTransport(settings);

            foreach (var pump in _pumps)
                await pump.StartAsync(cancellationToken);
        }

        
        protected void InitializeTransport(AzureServiceBusTransportSettings settings)
        {
            var client = new ServiceBusClient(settings.ConnectionString, new ServiceBusClientOptions(){ });
            foreach (var endpointSettings in settings.Endpoints)
            {
                ///TODO: find out if we have a consumer for the endpoint
                var endpoint = AzureServiceBusEndpoint.Create(endpointSettings, _services);
                var pump = new AzureServiceBusMessagePump(client, endpoint, new AzureServiceBusMetaDataMapper(), new AzureServiceBusMessagePumpSettings()
                {
                    MaxConcurrentHandlers = 1,
                    MaxLevel1Retry = 1,
                });
                _pumps.Add(pump);
            }
        }

        public Task StopAsync()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEndpoint2> GetEndpoints()
        {
            throw new NotImplementedException();
        }
        
        
    }
}