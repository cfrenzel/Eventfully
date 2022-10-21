using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Eventfully.Filters
{
    /// <summary>
    /// A filter that acts on messages and meta data while the message content is in
    /// its byte[] form. Ex. Encrypting/Decrypting the message context 
    /// <see cref="IMessageFilter"/> to act on messages while in the IMessage object format
    /// </summary>
    public interface ITransportMessageFilter : IBidirectionalPipelineStep<TransportMessageFilterContext>
    {
        TransportMessageFilterContext OnIncoming(TransportMessageFilterContext context);
        TransportMessageFilterContext OnOutgoing(TransportMessageFilterContext context);
        
    }

    public class TransportMessageFilterContext
    {
        public TransportMessage TransportMessage { get; set; }
        public IEndpoint2 Endpoint { get; set; }

        public TransportMessageFilterContext(TransportMessage message, IEndpoint2 endpoint)
        {
            TransportMessage = message;
            Endpoint = endpoint;
        }
    }
    
    


   
}
