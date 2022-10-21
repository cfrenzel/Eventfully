using System;
using System.Collections.Generic;
using System.Text;
using Eventfully.Transports;

namespace Eventfully
{
    public class MessagingConfiguration
    {
        /// <summary>
        /// allow extention methods of MessagingConfiguration to register services with whatever
        /// Dependency Injection container being used
        /// </summary>
        public IServiceRegistrar Services { get; set; }          
        //public List<TransportSettings2> TransportSettings { get; set; } = new List<TransportSettings>();
        public ICountingSemaphore OutboxConsumerSemaphore { get; set; }

        public MessagingConfiguration(IServiceRegistrar services = null)
        {
            Services = services;
        }
    }
}
