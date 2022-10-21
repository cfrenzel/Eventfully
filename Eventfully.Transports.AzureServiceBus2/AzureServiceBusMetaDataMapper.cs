using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully.Transports.AzureServiceBus2
{
    public class AzureServiceBusMetaDataMapper
    {
        public void ApplyMetaData(Azure.Messaging.ServiceBus.ServiceBusMessage message, MessageMetaData meta, string messageTypeIdentifier)
        {
            if (meta != null)
            {
                foreach (var item in meta)
                {
                    if (HeaderType.MessageId.Equals(item.Key) && !String.IsNullOrEmpty(item.Value))
                        message.MessageId = item.Value;
                    else if (HeaderType.ContentType.Equals(item.Key))
                        message.ContentType = item.Value;
                    else if (HeaderType.CorrelationId.Equals(item.Key) && !String.IsNullOrEmpty(item.Value))
                        message.CorrelationId = item.Value;
                    else if (HeaderType.ReplyTo.Equals(item.Key))
                        message.ReplyTo = item.Value;
                    else if (HeaderType.SessionId.Equals(item.Key) && !String.IsNullOrEmpty(item.Value))
                        message.SessionId = item.Value;
                    else if (HeaderType.TimeToLive.Equals(item.Key) && meta.TimeToLive.HasValue)
                        message.TimeToLive = meta.TimeToLive.Value;                   
                    else
                    {
                        message.ApplicationProperties.Add(item.Key, item.Value);
                    }
                }
            }
            message.Subject = messageTypeIdentifier;
        }

        public MessageMetaData ExtractMetaData(ServiceBusReceivedMessage message)
        {
            MessageMetaData meta = new MessageMetaData();
            
            if (!String.IsNullOrEmpty(message.Subject))
                meta.MessageType = message.Subject;

            if (!String.IsNullOrEmpty(message.MessageId))
                meta.MessageId = message.MessageId;

            try
            {
                //message throws exception if not in a state where expiration can be calculated
                if (message.TimeToLive != default(TimeSpan))
                    meta.TimeToLive = message.TimeToLive;
            }
            catch { }

            if (message.ApplicationProperties.ContainsKey("OriginalMessageId"))
            {
                var messageId = (string)message.ApplicationProperties["OriginalMessageId"];
                meta.MessageId = messageId;
                meta.Add("TransientMessageId", message.MessageId);
            }

            if (!String.IsNullOrEmpty(message.ContentType))
                meta.ContentType = message.ContentType;

            if (!String.IsNullOrEmpty(message.CorrelationId))
                meta.CorrelationId = message.CorrelationId;

            if (!String.IsNullOrEmpty(message.ReplyTo))
                meta.ReplyTo = message.ReplyTo;

            if (!String.IsNullOrEmpty(message.SessionId))
                meta.SessionId = message.SessionId;

            foreach(var item in message.ApplicationProperties)
            { 
                meta[item.Key] =  item.Value != null ? item.Value.ToString() : null;
            }

            return meta;
        }
       
    }
}
