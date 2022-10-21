using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Eventfully.Filters
{
    public class MessagePipeline : BidirectionalPipeline<TransportMessageFilterContext, MessageFilterContext>
    {
        public MessagePipeline(IEnumerable<ITransportMessageFilter> transportFilters, IEnumerable<IMessageFilter> messageFilters)
        {
            this.InSteps = input =>
            {
                return input
                        .In(new BidirectionalPipeline<TransportMessageFilterContext>(transportFilters))
                        .In(new MessageSerializationStep())
                        .In(new BidirectionalPipeline<MessageFilterContext>(messageFilters))
                    ;
            };
            this.OutSteps = input =>
            {
                return input
                        .Out(new BidirectionalPipeline<MessageFilterContext>(messageFilters))
                        .Out(new MessageSerializationStep())
                        .Out(new BidirectionalPipeline<TransportMessageFilterContext>(transportFilters))
                    ;
            };
        }
    }
}
