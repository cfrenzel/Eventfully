using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Eventfully.Filters
{
    public class MessageSerializationStep : IBidirectionalPipelineStep<TransportMessageFilterContext, MessageFilterContext>
    {
        private static readonly Dictionary<string, IMessageExtractor> _messageExtractorCache = new Dictionary<string, IMessageExtractor>();
      
        public TransportMessageFilterContext OnOutgoing(MessageFilterContext context)
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
                context.Endpoint
             );
        }
        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings()
        { 
           ContractResolver =  new PrivateSetterResolver()
        };
        
        public MessageFilterContext OnIncoming(TransportMessageFilterContext context)
        {
            string messageType = context.TransportMessage.MetaData.MessageType;
            if (String.IsNullOrEmpty(messageType))
                throw new InvalidMessageTypeException(messageType);

            var metaData = context.TransportMessage.MetaData;
            var messageProps = MessagingMap2.GetProps(messageType)
                ?? throw new UnknownMessageTypeException(messageType);

            IMessage message = _extractMessage(context.TransportMessage.Data, messageProps);

            return new MessageFilterContext(message, context.TransportMessage.MetaData, context.Endpoint, messageProps);
        }


        /// <summary>
        /// Convert message off the wire from byte[] into a concrete implementation of IIntegrationMessage
        /// delegates to custom extractors and defaults to Json deserialization
        /// </summary>
        private IMessage _extractMessage(byte[] messageData, MessageTypeProperties2 props)
        {
            IMessage message;
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
                message = (IMessage)JsonConvert.DeserializeObject(json, props.Type, _serializerSettings);
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
