using Microsoft.Azure.ServiceBus;
using Shouldly;
using System;
using Xunit;

namespace Eventfully.Transports.AzureServiceBus.UnitTests
{
    public class MetaDataMapperApplyTests
    {

        AzureServiceBusMetaDataMapper Mapper = new AzureServiceBusMetaDataMapper();
        string MessageType = "MessageType.ASBUnit";
        public MetaDataMapperApplyTests()
        {

        }


        [Fact]
        public void Should_map_messagetype_to_label()
        {
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message();
            MessageMetaData md = new MessageMetaData();
            Mapper.ApplyMetaData(m, md, MessageType);
            m.Label.ShouldBe(MessageType);
        }

        [Fact]
        public void Should_map_messageid_to_messagid()
        {
            string messageId = "123";
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message();
            MessageMetaData md = new MessageMetaData(messageId:messageId);
            Mapper.ApplyMetaData(m, md, MessageType);
            m.MessageId.ShouldBe(messageId);
        }

        [Fact]
        public void Should_map_contenttype_to_contenttype()
        {
            string contenttype = "text/json";
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message();
            MessageMetaData md = new MessageMetaData() { ContentType = contenttype };
            Mapper.ApplyMetaData(m, md, MessageType);
            m.ContentType.ShouldBe(contenttype);
        }


        [Fact]
        public void Should_map_correlationid_to_correlationid()
        {
            string correlationid = "567";
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message();
            MessageMetaData md = new MessageMetaData(correlationId: correlationid);
            Mapper.ApplyMetaData(m, md, MessageType);
            m.CorrelationId.ShouldBe(correlationid);
        }

        [Fact]
        public void Should_map_replyto_to_replyto()
        {
            string replyto = "endpoint://address";
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message();
            MessageMetaData md = new MessageMetaData() { ReplyTo = replyto };
            Mapper.ApplyMetaData(m, md, MessageType);
            m.ReplyTo.ShouldBe(replyto);
        }

        [Fact]
        public void Should_map_sessionId_to_sessionId()
        {
            string sessionId = "endpoint://address";
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message();
            MessageMetaData md = new MessageMetaData() { SessionId = sessionId };
            Mapper.ApplyMetaData(m, md, MessageType);
            m.SessionId.ShouldBe(sessionId);
        }

        [Fact]
        public void Should_map_timetolive_to_timetolive()
        {
            var timetolive = TimeSpan.FromSeconds(30);
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message();
            MessageMetaData md = new MessageMetaData() { TimeToLive = timetolive };
            Mapper.ApplyMetaData(m, md, MessageType);
            m.TimeToLive.ShouldBe(timetolive);
        }

        [Fact]
        public void Should_map_createdat_to_properties()
        {
            var createdAt = DateTime.UtcNow;
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message();
            MessageMetaData md = new MessageMetaData() { CreatedAtUtc = createdAt  };
            Mapper.ApplyMetaData(m, md, MessageType);
            m.UserProperties["CreatedAtUtc"].ShouldBe(createdAt.ToString("o"));
        }
       
        [Fact]
        public void Should_map_replyToEndpointName_to_properties()
        {
            var replyToEndpointName = "Replies";
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message();
            MessageMetaData md = new MessageMetaData() { ReplyToEndpointName = replyToEndpointName };
            Mapper.ApplyMetaData(m, md, MessageType);
            m.UserProperties["ReplyToEndpointName"].ShouldBe(replyToEndpointName);
        }

        [Fact]
        public void Should_map_skipTransientDispatch_to_properties()
        {
            var skipTransientDispatch = true;
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message();
            MessageMetaData md = new MessageMetaData() { SkipTransientDispatch = skipTransientDispatch };
            Mapper.ApplyMetaData(m, md, MessageType);
            m.UserProperties["SkipTransientDispatch"].ShouldBe("True");
        }

        [Fact]
        public void Should_map_encrypted_to_properties()
        {
            var encrypted = true;
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message();
            MessageMetaData md = new MessageMetaData() { Encrypted = encrypted };
            Mapper.ApplyMetaData(m, md, MessageType);
            m.UserProperties["Encrypted"].ShouldBe("True");
        }

        [Fact]
        public void Should_map_encryptionMethod_to_properties()
        {
            var encryptionMethod = "AES";
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message();
            MessageMetaData md = new MessageMetaData() { EncryptionMethod = encryptionMethod };
            Mapper.ApplyMetaData(m, md, MessageType);
            m.UserProperties[HeaderType.EncryptionMethod.Value].ShouldBe(encryptionMethod);
        }

        [Fact]
        public void Should_map_encryptionKeyName_to_properties()
        {
            var encryptionKeyName = "eventEncryptionKey";
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message();
            MessageMetaData md = new MessageMetaData() { EncryptionKeyName = encryptionKeyName };
            Mapper.ApplyMetaData(m, md, MessageType);
            m.UserProperties[HeaderType.EncryptionKeyName.Value].ShouldBe(encryptionKeyName);
        }


        [Fact]
        public void Should_map_custom_metadata_to_properties()
        {
            var customKey = "Custom1";
            var customVal = "Custom1Value";
            Microsoft.Azure.ServiceBus.Message m = new Microsoft.Azure.ServiceBus.Message();
            MessageMetaData md = new MessageMetaData();
            md.Add(customKey, customVal);
            Mapper.ApplyMetaData(m, md, MessageType);
            m.UserProperties[customKey].ShouldBe(customVal);
        }




    }

}
