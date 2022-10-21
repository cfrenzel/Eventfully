using System;
using System.Collections.Generic;
using Eventfully.Filters;

namespace Eventfully.Transports.AzureServiceBus2
{
    public enum EndpointType
    {
        Queue,
        Topic
    }
    
    public class AzureServiceBusEndpointSettings : EndpointSettings<AzureServiceBusEndpointSettings>
    {
        public EndpointType EndpointType { get;  }
        public string SubscriptionName { get; set; }
        
       
    
        public AzureServiceBusEndpointSettings(string name, EndpointType endpointType = EndpointType.Topic)
        {
            this.Name = name;
            this.EndpointType = endpointType;
        }

        public virtual AzureServiceBusEndpointSettings BindMessage<T>(Action<MessageSettings> configBuilder = null)
        {
            var settings = new MessageSettings(typeof(T));
            this.MessageSettings.Add(settings);
            this.MessageTypes.Add(typeof(T));
            if (configBuilder != null)
                configBuilder.Invoke(settings);
            return this;
        }

       

     
    }
}