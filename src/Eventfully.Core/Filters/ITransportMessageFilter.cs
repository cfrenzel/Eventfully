using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Eventfully.Filters
{
    [Flags]
    public enum FilterDirection
    {
        Inbound = 1,
        Outbound = 2,
    }
    /// <summary>
    /// A filter that acts on messages and meta data while the message content is in
    /// its byte[] form. Ex. Encrypting/Decrypting the message context 
    /// <see cref="IIntegrationMessageFilter"/> to act on messages while in the IIntegrationMessage format
    /// </summary>
    public interface ITransportMessageFilter : IPipelineStep<TransportMessageFilterContext>
    {
        FilterDirection SupportsDirection { get;}
        new TransportMessageFilterContext Process(TransportMessageFilterContext context);
    }

    public class TransportMessageFilterContext
    {
        public FilterDirection Direction { get; set; }
        public TransportMessage TransportMessage { get; set; }
        public IEndpoint Endpoint { get; set; }

        public TransportMessageFilterContext(TransportMessage message, IEndpoint endpoint, FilterDirection direction)
        {
            TransportMessage = message;
            Endpoint = endpoint;
            Direction = direction;
        }
    }
    
    


   
}
