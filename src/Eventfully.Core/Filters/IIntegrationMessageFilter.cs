using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Eventfully.Filters
{
    /// <summary>
    /// A filter that acts on messages and meta data in the IIntegrationMessage form
    /// Ex. modifying headers based on message content 
    /// <see cref="ITransportMessageFilter"/> to act on messages while in the byte[] form
    /// </summary>
    public interface IIntegrationMessageFilter : IPipelineStep<IntegrationMessageFilterContext>
    {
        FilterDirection SupportsDirection { get; }

        new IntegrationMessageFilterContext Process(IntegrationMessageFilterContext context);
    }

    public class IntegrationMessageFilterContext
    {
        public FilterDirection Direction { get; set; }

        public IIntegrationMessage Message { get; set; }
        public MessageMetaData MessageMetaData { get; set; }
        public IEndpoint Endpoint { get; set; }

        public MessageTypeProperties Props { get; set; }

        public IntegrationMessageFilterContext(IIntegrationMessage message, MessageMetaData meta, IEndpoint endpoint, FilterDirection direction, MessageTypeProperties props)
        {
            Message = message;
            MessageMetaData = meta;
            Endpoint = endpoint;
            Direction = direction;
            Props = props;
        }
    }

  

}
