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
using System.Data.Common;

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
            public MessagingService MessagingService;

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

                  var handlerFactory = A.Fake<IServiceFactory>();
                 this.MessagingService = new MessagingService(this.Outbox, handlerFactory);

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

            var callback = A.Fake<Dispatcher>();
            await _fixture.Outbox.Relay(callback);

            A.CallTo(() => callback(
                A<string>.Ignored,
                A<byte[]>.Ignored,
                A<MessageMetaData>.That.Matches(x => x.MessageId.StartsWith("ready_")),
                A<string>.Ignored,
                false)
            ).MustHaveHappenedTwiceExactly();

            A.CallTo(() => callback(
                A<string>.Ignored,
                A<byte[]>.Ignored,
                A<MessageMetaData>.That.Matches(x => !x.MessageId.StartsWith("ready_")),
                A<string>.Ignored,
                false)
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

                //these were never in the ready status
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

            //var callback = A.Fake<Func<string, byte[], MessageMetaData, string, Task>>();
            var callback = A.Fake<Dispatcher>();
            await _fixture.Outbox.Relay(callback);

            A.CallTo(() => callback(
                A<string>.Ignored,
                A<byte[]>.Ignored,
                A<MessageMetaData>.Ignored,
                A<string>.Ignored,
                false)
            ).MustHaveHappened(_fixture.Outbox.BatchSize, Times.Exactly);

            A.CallTo(() => callback(
                A<string>.Ignored,
                A<byte[]>.Ignored,
                A<MessageMetaData>.That.Matches(x => !x.MessageId.StartsWith("ready_")),
                A<string>.Ignored,
                false)
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

            //var callback = A.Fake<Func<string, byte[], MessageMetaData, string, Task>>();
            var callback = A.Fake<Dispatcher>();
            await _fixture.Outbox.Relay(callback);

            A.CallTo(() => callback(
                A<string>.Ignored,
                A<byte[]>.Ignored,
                A<MessageMetaData>.Ignored,
                A<string>.Ignored, 
                false)
            ).MustHaveHappened(2, Times.Exactly);

        }

        [Fact]
        public async Task Should_hydrate_all_properties()
        {
            string endpointName = "OutboxDequeueTests1.1";
            var messageMetaData = new MessageMetaData();
            var serializedMessageMetaData = messageMetaData != null ? JsonConvert.SerializeObject(messageMetaData) : null;

            var message1 = new OutboxMessage("Test.Message1", _fixture.MessageBytes, "{ meta: 1}", DateTime.UtcNow, endpointName,
                skipTransientDispatch: true,
                expiresAtUtc: DateTime.UtcNow.AddMinutes(30),
                id: Guid.NewGuid())
            { Status = (int)OutboxMessageStatus.Ready, TryCount = 0, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-3) };

            var message2 = new OutboxMessage("Test.Message2", _fixture.MessageBytes, null, DateTime.UtcNow.AddSeconds(2), null,
                skipTransientDispatch: true,
                expiresAtUtc: null)
            { Status = (int)OutboxMessageStatus.InProgress, TryCount = 1, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-2) };
            ;

            var message3 = new OutboxMessage("Test.Message3", _fixture.MessageBytes, "{ meta: 3}", DateTime.UtcNow.AddSeconds(3), endpointName,
                skipTransientDispatch: false,
                expiresAtUtc: null)
            { Status = (int)OutboxMessageStatus.Processed, TryCount = 2, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-1) };
            ;

            await IntegrationTestFixture.CreateOutboxMessage(message1);
            await IntegrationTestFixture.CreateOutboxMessage(message2);
            await IntegrationTestFixture.CreateOutboxMessage(message3);

            //if the number of parameters changes in the outbox implementation this should throw an error
            using (var conn = new SqlConnectionFactory(ConnectionString).Get())
            {
                var sql = @"
                   select inserted.Id, inserted.PriorityDateUtc, inserted.[Type], inserted.Endpoint,
                           inserted.TryCount, inserted.[Status], inserted.ExpiresAtUtc, inserted.CreatedAtUtc,
                           od.Id, od.[Data], od.MetaData
	                FROM OutboxMessages inserted
	                INNER JOIN OutboxMessageData od
		                ON inserted.Id = od.Id  
                    ORDER BY inserted.CreatedAtUtc asc
                    ";

                conn.Open();
                DbCommand command = conn.CreateCommand();
                command.CommandText = sql;

                using (var reader = await command.ExecuteReaderAsync())
                {
                    var outboxMessages = _fixture.Outbox.HydrateOutboxMessages(reader);
                    outboxMessages.Count().ShouldBe(3);
                    var o1 = outboxMessages.ElementAt(0);
                    var o2 = outboxMessages.ElementAt(1);
                    var o3 = outboxMessages.ElementAt(2);

                    o1.Id.ShouldBe(message1.Id);
                    o1.PriorityDateUtc.ShouldBe(message1.PriorityDateUtc);
                    o1.Type.ShouldBe(message1.Type);
                    o1.Endpoint.ShouldBe(message1.Endpoint);
                    o1.TryCount.ShouldBe(message1.TryCount);
                    o1.Status.ShouldBe(message1.Status);
                    o1.ExpiresAtUtc.ShouldBe(message1.ExpiresAtUtc);
                    o1.CreatedAtUtc.ShouldBe(message1.CreatedAtUtc);
                    o1.MessageData.Id.ShouldBe(message1.MessageData.Id);
                    o1.MessageData.Data.ShouldBe(message1.MessageData.Data);
                    o1.MessageData.MetaData.ShouldBe(message1.MessageData.MetaData);

                    o2.Id.ShouldBe(message2.Id);
                    o2.PriorityDateUtc.ShouldBe(message2.PriorityDateUtc);
                    o2.Type.ShouldBe(message2.Type);
                    o2.Endpoint.ShouldBe(message2.Endpoint);
                    o2.TryCount.ShouldBe(message2.TryCount);
                    o2.Status.ShouldBe(message2.Status);
                    o2.ExpiresAtUtc.ShouldBe(message2.ExpiresAtUtc);
                    o2.CreatedAtUtc.ShouldBe(message2.CreatedAtUtc);
                    o2.MessageData.Id.ShouldBe(message2.MessageData.Id);
                    o2.MessageData.Data.ShouldBe(message2.MessageData.Data);
                    o2.MessageData.MetaData.ShouldBe(message2.MessageData.MetaData);

                    o3.Id.ShouldBe(message3.Id);
                    o3.PriorityDateUtc.ShouldBe(message3.PriorityDateUtc);
                    o3.Type.ShouldBe(message3.Type);
                    o3.Endpoint.ShouldBe(message3.Endpoint);
                    o3.TryCount.ShouldBe(message3.TryCount);
                    o3.Status.ShouldBe(message3.Status);
                    o3.ExpiresAtUtc.ShouldBe(message3.ExpiresAtUtc);
                    o3.CreatedAtUtc.ShouldBe(message3.CreatedAtUtc);
                    o3.MessageData.Id.ShouldBe(message3.MessageData.Id);
                    o3.MessageData.Data.ShouldBe(message3.MessageData.Data);
                    o3.MessageData.MetaData.ShouldBe(message3.MessageData.MetaData);

                }
            }//using
        }
    }
}
