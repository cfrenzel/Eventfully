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
using Eventfully.Transports.Testing;

namespace Eventfully.EFCoreOutbox.IntegrationTests
{
    using static IntegrationTestFixture;
    
    [Collection("Sequential")]
    public class OutboxRetryTests : IntegrationTestBase, IClassFixture<OutboxRetryTests.Fixture>
    {
        private Fixture _fixture;
        public OutboxRetryTests(Fixture fixture)
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
                    MaxTries = 3,
                    SqlConnectionString = ConnectionString,
                    DisableTransientDispatch = true,
                    },
                    new ConstantRetryStrategy(0.5)
                );

                MessagingService.Instance.Outbox = this.Outbox;
            }
        }



        [Fact]
        public async Task Should_update_priority_date_and_trycount_on_dispatch_failure_up_to_max()
        {
            string endpointName = "OutboxRetryTests_1.1";
            
            var message = await IntegrationTestFixture.CreateOutboxMessage(endpointName, "retry_1", OutboxMessageStatus.Ready);
            var lastPriorityDate = message.PriorityDateUtc;

            var callback = A.Fake<Func<string, byte[], MessageMetaData, string, Task>>();
            A.CallTo(() => callback(A<string>.Ignored, A<byte[]>.Ignored, A<MessageMetaData>.Ignored, A<string>.Ignored))
                .Throws(new ApplicationException("Test: foreced vailure in outbox relay"));
            await _fixture.Outbox.Relay(callback);

            A.CallTo(() => callback(A<string>.Ignored, A<byte[]>.Ignored,A<MessageMetaData>.That.Matches(x => x.MessageId.StartsWith("retry_")),A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxMessages = db.Set<OutboxMessage>().OrderBy(x=>x.CreatedAtUtc)
                    .AsNoTracking().ToList();
                var outboxMessage = outboxMessages[0];
                outboxMessage.Status.ShouldBe((int)OutboxMessageStatus.Ready);
                outboxMessage.TryCount.ShouldBe(1);
                outboxMessage.PriorityDateUtc.ShouldBeGreaterThan(lastPriorityDate);
                lastPriorityDate = outboxMessage.PriorityDateUtc;
            }

            await Task.Delay(500);//wait the retry interval
            callback = A.Fake<Func<string, byte[], MessageMetaData, string, Task>>();
            A.CallTo(() => callback(A<string>.Ignored, A<byte[]>.Ignored, A<MessageMetaData>.Ignored, A<string>.Ignored))
            .Throws(new ApplicationException("Test: foreced vailure in outbox relay"));

            await _fixture.Outbox.Relay(callback);
            A.CallTo(() => callback(A<string>.Ignored, A<byte[]>.Ignored, A<MessageMetaData>.That.Matches(x => x.MessageId.StartsWith("retry_")), A<string>.Ignored))
               .MustHaveHappenedOnceExactly();
            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxMessages = db.Set<OutboxMessage>().OrderBy(x => x.CreatedAtUtc).AsNoTracking().ToList();
                var outboxMessage = outboxMessages[0];
                outboxMessage.Status.ShouldBe((int)OutboxMessageStatus.Ready);
                outboxMessage.TryCount.ShouldBe(2);
                outboxMessage.PriorityDateUtc.ShouldBeGreaterThan(lastPriorityDate);
                lastPriorityDate = outboxMessage.PriorityDateUtc;
            }

            await Task.Delay(500);//wait the retry interval
            callback = A.Fake<Func<string, byte[], MessageMetaData, string, Task>>();
            A.CallTo(() => callback(A<string>.Ignored, A<byte[]>.Ignored, A<MessageMetaData>.Ignored, A<string>.Ignored))
                .Throws(new ApplicationException("Test: foreced vailure in outbox relay"));
            await _fixture.Outbox.Relay(callback);

            A.CallTo(() => callback(A<string>.Ignored, A<byte[]>.Ignored, A<MessageMetaData>.That.Matches(x => x.MessageId.StartsWith("retry_")), A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxMessages = db.Set<OutboxMessage>().OrderBy(x => x.CreatedAtUtc).AsNoTracking().ToList();
                var outboxMessage = outboxMessages[0];
                outboxMessage.TryCount.ShouldBe(3);
                outboxMessage.Status.ShouldBe((int)OutboxMessageStatus.Failed);
                outboxMessage.PriorityDateUtc.ShouldBe(lastPriorityDate);//don't incerment after failure
            }
        }



    }
}
