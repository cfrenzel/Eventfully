using Microsoft.Azure.ServiceBus;
using Shouldly;
using System;
using Xunit;

namespace Eventfully.Transports.AzureServiceBus.UnitTests
{
    public class MetaDataMapperExtractTests
    {

        AzureServiceBusMetaDataMapper Mapper = new AzureServiceBusMetaDataMapper();
        string MessageType = "MessageType.ASBUnit";
        public MetaDataMapperExtractTests()
        {

        }


        [Fact]
        public void Should_map_label_to_messagetype()
        {
            string label = MessageType;
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message()
            {
                Label = label,
            };
            var meta = Mapper.ExtractMetaData(m);
            meta.MessageType.ShouldBe(label);
        }

        [Fact]
        public void Should_map_messageid_to_messagid()
        {
            string messageId = "123";
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message()
            {
                MessageId = messageId,
            };
            var meta = Mapper.ExtractMetaData(m);
            meta.MessageId.ShouldBe(messageId);
        }

        [Fact]
        public void Should_map_contenttype_to_contenttype()
        {
            string contenttype = "text/json";
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message()
            {
                ContentType = contenttype,
            };
            var meta = Mapper.ExtractMetaData(m);
            meta.ContentType.ShouldBe(contenttype);
        }


        [Fact]
        public void Should_map_correlationid_to_correlationid()
        {
            string correlationid = "567";
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message()
            {
                CorrelationId = correlationid,
            };
            var meta = Mapper.ExtractMetaData(m);
            meta.CorrelationId.ShouldBe(correlationid);
        }



        [Fact]
        public void Should_map_replyto_to_replyto()
        {
            string replyto = "endpoint://address";
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message()
            {
                ReplyTo = replyto,
            };
            var meta = Mapper.ExtractMetaData(m);
            meta.ReplyTo.ShouldBe(replyto);
        }

        [Fact]
        public void Should_map_sessionId_to_sessionId()
        {
            string sessionId = "endpoint://address";
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message()
            {
                SessionId = sessionId,
            };
            var meta = Mapper.ExtractMetaData(m);
            meta.SessionId.ShouldBe(sessionId);
        }

        [Fact]
        public void Should_not_map_timetolive()
        {
            var now = DateTime.UtcNow;
            var timetolive = TimeSpan.FromSeconds(30);

            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message()
            {
                TimeToLive = timetolive,
            };
            //time to live on message results in setting ExpiresAtUtc
            var meta = Mapper.ExtractMetaData(m);
            meta.TimeToLive.ShouldBeNull();
        }

        [Fact]
        public void Should_map_property_to_createdat()
        {
            var createdAt = DateTime.UtcNow;
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message();
            m.UserProperties.Add(HeaderType.CreatedAtUtc.Value, createdAt.ToString("o"));
            var meta = Mapper.ExtractMetaData(m);
            meta.CreatedAtUtc.ShouldBe(createdAt);
        }
       
        [Fact]
        public void Should_map_property_to_replyToEndpointNam()
        {
            var replyToEndpointName = "Replies";
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message();
            m.UserProperties.Add(HeaderType.ReplyToEndpointName.Value, replyToEndpointName);
            var meta = Mapper.ExtractMetaData(m);
            meta.ReplyToEndpointName.ShouldBe(replyToEndpointName);
          }

        [Fact]
        public void Should_map_property_to_skipTransientDispatch()
        {
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message();
            m.UserProperties.Add(HeaderType.SkipTransientDispatch.Value, "True");
            var meta = Mapper.ExtractMetaData(m);
            meta.SkipTransientDispatch.ShouldBeTrue();
        }

        [Fact]
        public void Should_map_property_to_encrypted()
        {
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message();
            m.UserProperties.Add(HeaderType.Encrypted.Value, "True");
            var meta = Mapper.ExtractMetaData(m);
            meta.Encrypted.ShouldBeTrue();
        }

        [Fact]
        public void Should_map_property_to_encryptionmethod()
        {
            var encryptionMethod = "AES";
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message();
            m.UserProperties.Add(HeaderType.EncryptionMethod.Value, encryptionMethod);
            var meta = Mapper.ExtractMetaData(m);
            meta.EncryptionMethod.ShouldBe(encryptionMethod);
        }


        [Fact]
        public void Should_map_property_to_encryptionKeyname()
        {
            var encryptionKeyName = "eventEncryptionKey";
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message();
            m.UserProperties.Add(HeaderType.EncryptionKeyName.Value, encryptionKeyName);
            var meta = Mapper.ExtractMetaData(m);
            meta.EncryptionKeyName.ShouldBe(encryptionKeyName);
        }


        [Fact]
        public void Should_map_custom_properties_to_metadata()
        {
            var customKey = "Custom1";
            var customVal = "Custom1Value";
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message();
            m.UserProperties.Add(customKey, customVal);
            var meta = Mapper.ExtractMetaData(m);
            meta[customKey].ShouldBe(customVal);
        }




    }

}
