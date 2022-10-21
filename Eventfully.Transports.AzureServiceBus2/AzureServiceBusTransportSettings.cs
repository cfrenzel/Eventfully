using System;
using System.Collections.Generic;

namespace Eventfully.Transports.AzureServiceBus2
{
   public class AzureServiceBusTransportSettings : 
        ISupportQueues<AzureServiceBusEndpointSettings>,
        ISupportTopics<AzureServiceBusEndpointSettings>
    {
        public List<AzureServiceBusEndpointSettings> Endpoints { get; } = new List<AzureServiceBusEndpointSettings>();

        public string ConnectionString { get; set; }
        public AzureServiceBusTransportSettings(string connectionString)
        {
            this.ConnectionString = connectionString;
        }
        public AzureServiceBusEndpointSettings ConfigureQueue(string name)
        {
            var settings = new AzureServiceBusEndpointSettings(name, EndpointType.Queue);
            this.Endpoints.Add(settings);
            return settings;
        }
        
        public AzureServiceBusEndpointSettings ConfigureTopic(string name, string subscriptionName = null)
        {
            var settings = new AzureServiceBusEndpointSettings(name, EndpointType.Topic)
            {
                SubscriptionName = subscriptionName,
            };
            this.Endpoints.Add(settings);
            return settings;
        }
       
        public AzureServiceBusEndpointSettings ConfigureTopic<M>(string name, string subscriptionName = null, Action<MessageSettings> configBuilder = null)
        {
            var settings = new AzureServiceBusEndpointSettings(name, EndpointType.Topic);
            settings.SubscriptionName = subscriptionName;
            settings.BindMessage<M>(configBuilder);
            this.Endpoints.Add(settings);
            return settings;
        }
    }
}