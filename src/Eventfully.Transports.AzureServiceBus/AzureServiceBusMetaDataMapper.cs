using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully.Transports.AzureServiceBus
{
    public class AzureServiceBusMetaDataMapper
    {
        public void ApplyMetaData(Microsoft.Azure.ServiceBus.Message message, MessageMetaData meta, string messageTypeIdentifier)
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
                        message.UserProperties.Add(item.Key, item.Value);
                    }
                }
            }
            message.Label = messageTypeIdentifier;
        }

        public MessageMetaData ExtractMetaData(Microsoft.Azure.ServiceBus.Message message)
        {
            MessageMetaData meta = new MessageMetaData();

            if (!String.IsNullOrEmpty(message.Label))
                meta.MessageType = message.Label;

            if (!String.IsNullOrEmpty(message.MessageId))
                meta.MessageId = message.MessageId;

            try
            {
                //message throws exception if not in a state where expiration can be calculated
                if (message.ExpiresAtUtc != default(DateTime))
                    meta.ExpiresAtUtc = message.ExpiresAtUtc;
            }
            catch { }

            if (message.UserProperties.ContainsKey("OriginalMessageId"))
            {
                var messageId = (string)message.UserProperties["OriginalMessageId"];
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

            foreach(var item in message.UserProperties)
            {
                    meta[item.Key] =  item.Value != null ? item.Value.ToString() : null;
            }

            return meta;
        }
       
    }
}
