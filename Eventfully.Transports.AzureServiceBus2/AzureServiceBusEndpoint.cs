using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Eventfully.Transports.AzureServiceBus2
{
    public abstract class AzureServiceBusEndpoint : Endpoint2<AzureServiceBusEndpointSettings>
    {
     
        public override bool SupportsDelayedDispatch => false; 
       
        
        public AzureServiceBusEndpoint(AzureServiceBusEndpointSettings settings): base(settings)
        {
           
        }
        public static AzureServiceBusEndpoint Create(AzureServiceBusEndpointSettings settings, IServiceFactory services)
        {
            switch (settings.EndpointType)
            {
                case EndpointType.Queue:
                    return new QueueEndpoint(settings);
                case EndpointType.Topic:
                    return new TopicEndpoint(settings);
                default:
                    return null;
            }
        }
        //public abstract Task Dispatch(string messageTypeIdenfifier, byte[] message, MessageMetaData metaData = null);
        public abstract ServiceBusProcessor CreateProcessor(ServiceBusClient client, ServiceBusProcessorOptions options = null);
    }

    public class QueueEndpoint : AzureServiceBusEndpoint
    {
        public QueueEndpoint(AzureServiceBusEndpointSettings settings) : base(settings)
        {
            
        }
        
        public override Task Dispatch(string messageTypeIdenfifier, byte[] message, MessageMetaData metaData = null)
        {
            throw new NotImplementedException();
        }

        public override ServiceBusProcessor CreateProcessor(ServiceBusClient client, ServiceBusProcessorOptions options = null)
        {
            return client.CreateProcessor(this.Name, options);
        }
    }

    public class TopicEndpoint : AzureServiceBusEndpoint
    {
        public string SubscriptionName { get; set; }


        public TopicEndpoint(AzureServiceBusEndpointSettings settings) : base(settings)
        {
            this.SubscriptionName = settings.SubscriptionName;
        }
        public override ServiceBusProcessor CreateProcessor(ServiceBusClient client,
            ServiceBusProcessorOptions options = null)
        {
            return client.CreateProcessor(this.Name, this.SubscriptionName, options);
        }
        
        public override Task Dispatch(string messageTypeIdenfifier, byte[] message, MessageMetaData metaData = null)
        {
            throw new NotImplementedException();
        }
    }
    
}