using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Eventfully.Filters
{
    public class MessageExtractionStep : IPipelineStep<TransportMessageFilterContext, IntegrationMessageFilterContext>
    {
        private static readonly Dictionary<string, IMessageExtractor> _messageExtractorCache = new Dictionary<string, IMessageExtractor>();
        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings()
        { 
           ContractResolver =  new PrivateSetterResolver()
        };
        public IntegrationMessageFilterContext Process(TransportMessageFilterContext context)
        {
            string messageType = context.TransportMessage.MetaData.MessageType;
            if (String.IsNullOrEmpty(messageType))
                throw new InvalidMessageTypeException(messageType);

            var metaData = context.TransportMessage.MetaData;
            var messageProps = MessagingMap.GetProps(messageType)
                ?? throw new UnknownMessageTypeException(messageType);

            IIntegrationMessage message = _extractMessage(context.TransportMessage.Data, messageProps);

            return new IntegrationMessageFilterContext(message, context.TransportMessage.MetaData, context.Endpoint, FilterDirection.Inbound, messageProps);
        }


        /// <summary>
        /// Convert message off the wire from byte[] into a concrete implementation of IIntegrationMessage
        /// delegates to custom extractors and defaults to Json deserialization
        /// </summary>
        private IIntegrationMessage _extractMessage(byte[] messageData, MessageTypeProperties props)
        {
            IIntegrationMessage message;
            if (props.HasCustomExtractor)
            {
                IMessageExtractor extractor = null;
                if (_messageExtractorCache.ContainsKey(props.MessageTypeIdentifier))
                    extractor = _messageExtractorCache[props.MessageTypeIdentifier];
                else
                {
                    extractor = (IMessageExtractor)Activator.CreateInstance(props.Type, true);
                    _messageExtractorCache.Add(props.MessageTypeIdentifier, extractor);
                }
                message = extractor.Extract(messageData);
            }
            else
            {
                //by default the message is json and will be deserialized to the type with a matching MessageTypeIdentifier property:"MessageType"
                var json = Encoding.UTF8.GetString(messageData);
                message = (IIntegrationMessage)JsonConvert.DeserializeObject(json, props.Type, _serializerSettings);
            }
            return message;
        }
    }
    public class PrivateSetterResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var prop = base.CreateProperty(member, memberSerialization);
            if (!prop.Writable)
            {
                var property = member as PropertyInfo;
                prop.Writable = property?.GetSetMethod(true) != null;
            }
            return prop;
        }
    }
}
