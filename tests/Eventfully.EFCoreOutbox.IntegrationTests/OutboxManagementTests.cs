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
    public class OutboxManagementTests : IntegrationTestBase, IClassFixture<OutboxManagementTests.Fixture>
    {
        private Fixture _fixture;
        public OutboxManagementTests(Fixture fixture)
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
           
            public Fixture()
            {
                
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
        public async Task Should_reset_aged_with_status_in_progress()
        {
            string endpointName = "OutboxManagementTests_1.1";
            
            var readyOrig = await IntegrationTestFixture.CreateOutboxMessage(endpointName, "ready_1", OutboxMessageStatus.Ready);
            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "ready_2", OutboxMessageStatus.InProgress, DateTime.UtcNow.AddMinutes(-40));
            var reset1 = await IntegrationTestFixture.CreateOutboxMessage(endpointName, "ready_3", OutboxMessageStatus.InProgress, DateTime.UtcNow.AddHours(-1));
            var reset2 = await IntegrationTestFixture.CreateOutboxMessage(endpointName, "ready_4", OutboxMessageStatus.InProgress, DateTime.UtcNow.AddHours(-1.1));
            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "ready_5", OutboxMessageStatus.Processed);
            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "ready_6", OutboxMessageStatus.Failed);

            await _fixture.Outbox.Reset(TimeSpan.FromMinutes(58));

            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxMessages = db.Set<OutboxMessage>().Include(x=>x.MessageData)
                    .Where(x=>x.Status == (int)OutboxMessageStatus.Ready)
                    .OrderBy(x=>x.CreatedAtUtc)
                    .AsNoTracking().ToList();
                outboxMessages.Count.ShouldBe(3);

                outboxMessages[0].Status.ShouldBe((int)OutboxMessageStatus.Ready);
                outboxMessages[0].TryCount.ShouldBe(0);
                outboxMessages[0].PriorityDateUtc.ShouldBe(readyOrig.PriorityDateUtc);

                outboxMessages[1].Status.ShouldBe((int)OutboxMessageStatus.Ready);
                outboxMessages[1].TryCount.ShouldBe(0);
                outboxMessages[1].PriorityDateUtc.ShouldBe(reset1.PriorityDateUtc);

                outboxMessages[2].Status.ShouldBe((int)OutboxMessageStatus.Ready);
                outboxMessages[2].TryCount.ShouldBe(0);
                outboxMessages[2].PriorityDateUtc.ShouldBe(reset2.PriorityDateUtc);

            }
        }


        [Fact]
        public async Task Should_cleanup_aged_with_status_processed()
        {
            string endpointName = "OutboxManagementTests_1.2";
            
            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "ready_1", OutboxMessageStatus.Ready, DateTime.UtcNow.AddMinutes(-61));
            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "inprogress_2", OutboxMessageStatus.InProgress, DateTime.UtcNow.AddMinutes(-61));
            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "failed_3", OutboxMessageStatus.Failed, DateTime.UtcNow.AddMinutes(-61));
            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "processed_4", OutboxMessageStatus.Processed);
            await IntegrationTestFixture.CreateOutboxMessage(endpointName, "processed_5", OutboxMessageStatus.Processed, DateTime.UtcNow.AddMinutes(-55));
            var cleanup1 = await IntegrationTestFixture.CreateOutboxMessage(endpointName, "processed_6", OutboxMessageStatus.Processed, DateTime.UtcNow.AddMinutes(-60));
            var cleanup2 = await IntegrationTestFixture.CreateOutboxMessage(endpointName, "processed_7", OutboxMessageStatus.Processed, DateTime.UtcNow.AddMinutes(-61));
 
            await _fixture.Outbox.CleanUp(TimeSpan.FromMinutes(58));

            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxMessages = db.Set<OutboxMessage>()
                    .OrderBy(x => x.CreatedAtUtc)
                    .AsNoTracking().ToList();
                outboxMessages.Count.ShouldBe(5);

                //there shoule only be 2 processed message left, the other 2 should be deleted
                outboxMessages.Where(x => x.Status == (int)OutboxMessageStatus.Processed).Count().ShouldBe(2);
            }
        }



    }
}
