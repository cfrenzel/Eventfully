using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Eventfully.Filters
{
    public class MessageSerializationStep : IPipelineStep<IntegrationMessageFilterContext, TransportMessageFilterContext>
    {
        private static readonly Dictionary<string, IMessageExtractor> _messageExtractorCache = new Dictionary<string, IMessageExtractor>();

        public TransportMessageFilterContext Process(IntegrationMessageFilterContext context)
        {
            byte[] messageBytes = null;
            if (context.Message is IsSerialized)
                messageBytes = (context.Message as IsSerialized).Message;
            else
            {
                string messageBody = JsonConvert.SerializeObject(context.Message);
                messageBytes = Encoding.UTF8.GetBytes(messageBody);
            }

            return new TransportMessageFilterContext(
                new TransportMessage(context.Message.MessageType, messageBytes, context.MessageMetaData),
                context.Endpoint,
                FilterDirection.Outbound
             );
        }
    }
}
