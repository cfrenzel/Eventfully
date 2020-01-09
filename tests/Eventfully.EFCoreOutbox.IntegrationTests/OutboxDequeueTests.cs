using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Shouldly;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Eventfully.Handlers;
using FakeItEasy;

namespace Eventfully.EFCoreOutbox.IntegrationTests
{
    using static IntegrationTestFixture;
    
    [Collection("Sequential")]
    public class OutboxDequeueTests : IntegrationTestBase, IClassFixture<OutboxDequeueTests.Fixture>
    {
        private Fixture _fixture;
        public OutboxDequeueTests(Fixture fixture)
        {
            this._fixture = fixture;
            //clear outbox
            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                db.Set<OutboxMessage>().RemoveRange(db.Set<OutboxMessage>());
                db.SaveChanges();
            }
        }

        public class Fixture
        {
            public Outbox<ApplicationDbContext> Outbox;
            public TestMessage Message;
            public byte[] MessageBytes;
   
            public Fixture()
            {
                this.Message = new TestMessage()
                {
                    Id = Guid.NewGuid(),
                    Description = "Test Message Text",
                    Name = "Test Message Name",
                    MessageDate = DateTime.UtcNow,
                };

                string messageBody = JsonConvert.SerializeObject(this.Message);
                this.MessageBytes = Encoding.UTF8.GetBytes(messageBody);

                this.Outbox = new Outbox<ApplicationDbContext>(new OutboxSettings()
                {
                    BatchSize = 5,
                    MaxConcurrency = 1,
                    MaxTries = 10,
                    SqlConnectionString = ConnectionString,
                    DisableTransientDispatch = true,
                });

                MessagingService.Instance.Outbox = this.Outbox;
            }
        }



        [Fact]
        public async Task Should_only_dequeue_ready_messages()
        {
            string endpointName = "OutboxDequeueTests1.2";

            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "ready_1", OutboxMessageStatus.Ready);
            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "ready_2", OutboxMessageStatus.Ready);
            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "inprogress_3", OutboxMessageStatus.InProgress);
            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "processed_4", OutboxMessageStatus.Processed);
            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "failed_5", OutboxMessageStatus.Failed);

            var callback = A.Fake<Func<string, byte[], MessageMetaData, string, Task>>();
            await _fixture.Outbox.Relay(callback);

            A.CallTo(() => callback(
                A<string>.Ignored,
                A<byte[]>.Ignored,
                A<MessageMetaData>.That.Matches(x => x.MessageId.StartsWith("ready_")),
                A<string>.Ignored)
            ).MustHaveHappenedTwiceExactly();

            A.CallTo(() => callback(
                A<string>.Ignored,
                A<byte[]>.Ignored,
                A<MessageMetaData>.That.Matches(x => !x.MessageId.StartsWith("ready_")),
                A<string>.Ignored)
            ).MustNotHaveHappened();

            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxMessages = db.Set<OutboxMessage>()
                    .OrderBy(x=>x.CreatedAtUtc)
                    .AsNoTracking().ToList();
                outboxMessages.Count.ShouldBe(5);
                
                outboxMessages[0].Status.ShouldBe((int)OutboxMessageStatus.Processed);
                outboxMessages[0].TryCount.ShouldBe(1);

                outboxMessages[1].Status.ShouldBe((int)OutboxMessageStatus.Processed);
                outboxMessages[1].TryCount.ShouldBe(1);

                //these were never int he ready status
                outboxMessages[2].TryCount.ShouldBe(0);
                outboxMessages[3].TryCount.ShouldBe(0);
                outboxMessages[4].TryCount.ShouldBe(0);
        
            }
        }

        [Fact]
        public async Task Should_limit_dequeue_to_batch_size()
        {
            string endpointName = "OutboxDequeueTests1.3";

            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "ready_1", OutboxMessageStatus.Ready);
            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "ready_2", OutboxMessageStatus.Ready);
            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "ready_3", OutboxMessageStatus.Ready);
            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "ready_4", OutboxMessageStatus.Ready);
            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "ready_5", OutboxMessageStatus.Ready);
            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "ready_6", OutboxMessageStatus.Ready);

            var callback = A.Fake<Func<string, byte[], MessageMetaData, string, Task>>();
            await _fixture.Outbox.Relay(callback);

            A.CallTo(() => callback(
                A<string>.Ignored,
                A<byte[]>.Ignored,
                A<MessageMetaData>.Ignored,
                A<string>.Ignored)
            ).MustHaveHappened(_fixture.Outbox.BatchSize, Times.Exactly);

            A.CallTo(() => callback(
                A<string>.Ignored,
                A<byte[]>.Ignored,
                A<MessageMetaData>.That.Matches(x => !x.MessageId.StartsWith("ready_")),
                A<string>.Ignored)
            ).MustNotHaveHappened();
        }
       
        
        [Fact]
        public async Task Should_not_dequeue_priority_dates_in_future()
        {
            string endpointName = "OutboxDequeueTests1.4";

            var futureDate = DateTime.UtcNow.AddMinutes(2);

            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "ready_1", OutboxMessageStatus.Ready);
            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "ready_2", OutboxMessageStatus.Ready);
            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "ready_4", OutboxMessageStatus.Ready, futureDate);
            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "ready_5", OutboxMessageStatus.Ready, futureDate);
            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "ready_6", OutboxMessageStatus.Ready, futureDate);

            var callback = A.Fake<Func<string, byte[], MessageMetaData, string, Task>>();
            await _fixture.Outbox.Relay(callback);

            A.CallTo(() => callback(
                A<string>.Ignored,
                A<byte[]>.Ignored,
                A<MessageMetaData>.Ignored,
                A<string>.Ignored)
            ).MustHaveHappened(2, Times.Exactly);

        }


    }
}
