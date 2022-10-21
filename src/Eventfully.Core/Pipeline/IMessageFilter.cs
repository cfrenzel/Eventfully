using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Eventfully.Filters
{
    /// <summary>
    /// A filter that acts on messages and meta data in the IMessage form
    /// Ex. modifying headers based on message content 
    /// <see cref="ITransportMessageFilter"/> to act on messages while in the byte[] form
    /// </summary>
    public interface IMessageFilter : IBidirectionalPipelineStep<MessageFilterContext>
    {
  
    }

    public class MessageFilterContext
    {
        public IMessage Message { get; set; }
        public MessageMetaData MessageMetaData { get; set; }
        public IEndpoint2 Endpoint { get; set; }
        public MessageTypeProperties2 Props { get; set; }

        public MessageFilterContext(IMessage message, MessageMetaData meta, IEndpoint2 endpoint,  MessageTypeProperties2 props)
        {
            Message = message;
            MessageMetaData = meta;
            Endpoint = endpoint;
            Props = props;
        }
    }

  

}
