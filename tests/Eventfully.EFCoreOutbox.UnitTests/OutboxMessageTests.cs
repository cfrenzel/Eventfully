using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Xunit;
using Shouldly;

namespace Eventfully.EFCoreOutbox.UnitTests
{
    public class OutboxMessageTests
    {
        private TestMessage Message;
        private byte[] MessageBytes;
        private MessageMetaData MessageMetaData;
        private string SerializedMessageMetaData;

        //private OutboxMessage OutboxMessage;
        public OutboxMessageTests()
        {

            Guid messageId = Guid.NewGuid();
            DateTime messageDate = DateTime.UtcNow;

            this.Message = new TestMessage()
            {
                Id = messageId,
                Description = "Test Message Text",
                Name = "Test Message Name",
                MessageDate = messageDate,
            };

            this.MessageMetaData = new MessageMetaData(delay: TimeSpan.FromSeconds(10), correlationId: this.Message.Id.ToString(), messageId: this.Message.Id.ToString(), skipTransient: true);
            this.SerializedMessageMetaData = this.MessageMetaData != null ? JsonConvert.SerializeObject(this.MessageMetaData) : null;
            string messageBody = JsonConvert.SerializeObject(this.Message);
            this.MessageBytes = Encoding.UTF8.GetBytes(messageBody);
        }


        [Fact]
        public void Should_set_required_properties()
        {
            var priorityDate = DateTime.UtcNow;

            var outboxMessage = new OutboxMessage(
                this.Message.MessageType,
                this.MessageBytes,
                this.SerializedMessageMetaData,
                priorityDate
            );

            outboxMessage.Id.ShouldNotBe(default(Guid));
            outboxMessage.Type.ShouldBe(this.Message.MessageType);
            outboxMessage.MessageData.ShouldNotBeNull();
            outboxMessage.MessageData.Data.ShouldBe(this.MessageBytes);
            outboxMessage.MessageData.MetaData.ShouldBe(this.SerializedMessageMetaData);
            outboxMessage.PriorityDateUtc.ShouldBe(priorityDate);
        }

        [Fact]
        public void Should_set_defaults()
        {
            var outboxMessage = new OutboxMessage(
                this.Message.MessageType,
                this.MessageBytes,
                this.SerializedMessageMetaData,
                DateTime.UtcNow
            );

            outboxMessage.Id.ShouldNotBe(default(Guid));
            outboxMessage.TryCount.ShouldBe(0);
            outboxMessage.CreatedAtUtc.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            outboxMessage.ExpiresAtUtc.ShouldBeNull();
            outboxMessage.Endpoint.ShouldBeNull();
            outboxMessage.SkipTransientDispatch.ShouldBeFalse();
            outboxMessage.Status.ShouldBe(1);//when transient dispatch enabled
        }

        [Fact]
        public void Should_allow_null_meta_data()
        {
            var outboxMessage = new OutboxMessage(
               this.Message.MessageType,
               this.MessageBytes,
               null,
               DateTime.UtcNow
           );

            outboxMessage.MessageData.ShouldNotBeNull();
            outboxMessage.MessageData.Data.ShouldBe(this.MessageBytes);
            outboxMessage.MessageData.MetaData.ShouldBeNull();
        }


        [Fact]
        public void Should_generate_id()
        {
            var outboxMessage = new OutboxMessage(
               this.Message.MessageType,
               this.MessageBytes,
               this.SerializedMessageMetaData,
               DateTime.UtcNow
           );
            outboxMessage.Id.ShouldNotBe(default(Guid));
        }


        [Fact]
        public void Should_allow_to_schedule_in_past()
        {
            var priorityDate = new DateTime(2019, 6, 3, 8, 0, 0, DateTimeKind.Utc);

            var outboxMessage = new OutboxMessage(
               this.Message.MessageType,
               this.MessageBytes,
               this.SerializedMessageMetaData,
               priorityDate

           );
            outboxMessage.PriorityDateUtc.ShouldBe(priorityDate);
        }

        [Fact]
        public void Should_allow_to_schedule_in_future()
        {
            var priorityDate = new DateTime(DateTime.UtcNow.Year + 1, 6, 3, 8, 0, 0, DateTimeKind.Utc);

            var outboxMessage = new OutboxMessage(
               this.Message.MessageType,
               this.MessageBytes,
               this.SerializedMessageMetaData,
               priorityDate

           );
            outboxMessage.PriorityDateUtc.ShouldBe(priorityDate);
        }


        [Fact]
        public void Should_not_be_expired_when_expiration_not_set()
        {
            var outboxMessage = new OutboxMessage(
               this.Message.MessageType,
               this.MessageBytes,
               this.SerializedMessageMetaData,
               DateTime.UtcNow
           );

            outboxMessage.IsExpired(DateTime.MinValue).ShouldBeFalse();
            outboxMessage.IsExpired(DateTime.MaxValue).ShouldBeFalse();
        }


        [Fact]
        public void Should_calculate_expired_when_expiration_set()
        {
            var expiresAt = new DateTime(2019, 6, 3, 8, 0, 0, DateTimeKind.Utc);

            var outboxMessage = new OutboxMessage(
               this.Message.MessageType,
               this.MessageBytes,
               this.SerializedMessageMetaData,
               DateTime.UtcNow,
               false,
               expiresAt
           );
            outboxMessage.ExpiresAtUtc.ShouldBe(expiresAt);
            outboxMessage.IsExpired(expiresAt.AddMilliseconds(-1)).ShouldBeFalse();
            outboxMessage.IsExpired(expiresAt).ShouldBeTrue();
            outboxMessage.IsExpired(expiresAt.AddMilliseconds(1)).ShouldBeTrue();
        }

        [Fact]
        public void Should_set_status_to_ready_if_transient_diasabled()
        {
            var expiresAt = new DateTime(2019, 6, 3, 8, 0, 0, DateTimeKind.Utc);

            var outboxMessage = new OutboxMessage(
               this.Message.MessageType,
               this.MessageBytes,
               this.SerializedMessageMetaData,
               DateTime.UtcNow,
               false,
               expiresAt
           );
            outboxMessage.Status.ShouldBe(1);

            outboxMessage = new OutboxMessage(
                         this.Message.MessageType,
                         this.MessageBytes,
                         this.SerializedMessageMetaData,
                         DateTime.UtcNow,
                         true,
                         expiresAt
                     );
            outboxMessage.Status.ShouldBe(0);

        }



    }

}
