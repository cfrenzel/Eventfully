using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Shouldly;
using Eventfully.Outboxing;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Eventfully.EFCoreOutbox.IntegrationTests
{
    using static IntegrationTestFixture;
    public class OutboxEnqueueTests : IntegrationTestBase, IClassFixture<OutboxEnqueueTests.Fixture>
    {
        private Fixture _fixture;
    
        public OutboxEnqueueTests(Fixture fixture)
        {
            this._fixture = fixture;
           
        }

        public class Fixture
        {
            public Outbox<ApplicationDbContext> Outbox;

            public TestMessage Message;
            public byte[] MessageBytes;
            public MessageMetaData MessageMetaData;
            public string SerializedMessageMetaData;
            public Endpoint TestEndpoint;


            public Fixture()
            {
                Outbox = new Outbox<ApplicationDbContext>(new OutboxSettings()
                {
                    BatchSize = 10,
                    MaxConcurrency = 1,
                    MaxTries = 10,
                    SqlConnectionString = ConnectionString,
                    DisableTransientDispatch = false,
                });

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
                string messageBody = JsonConvert.SerializeObject(this.Message);
                this.SerializedMessageMetaData = this.MessageMetaData != null ? JsonConvert.SerializeObject(this.MessageMetaData) : null;
                this.MessageBytes = Encoding.UTF8.GetBytes(messageBody);


                this.TestEndpoint = new Endpoint(new EndpointSettings("Test")
                {
                    TransportSettings = new Transports.LocalTransportSettings()
                });


            }
        }

        [Fact]
        public async Task Should_enqueue_message()
        {
            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxSession = new OutboxSession<ApplicationDbContext>(_fixture.Outbox, db);
                await outboxSession.Dispatch(_fixture.Message.MessageType, _fixture.MessageBytes, null, _fixture.TestEndpoint );
                await db.SaveChangesAsync();
            }

            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                
                var outboxMessages = db.Set<OutboxMessage>()
                    .Include(x=>x.MessageData)
                    .AsNoTracking()
                    .ToList();

                outboxMessages.ShouldHaveSingleItem("should be one message");
                
                var m = outboxMessages.SingleOrDefault();
                m.Id.ShouldNotBe(default(Guid), "id should not be default guid");
                m.Type.ShouldBe(_fixture.Message.MessageType);
                m.Status.ShouldBe(1, "status should be 1/Processing");//skip transient
                m.TryCount.ShouldBe(0);
                m.MessageData.ShouldNotBeNull();
                m.MessageData.Id.ShouldBe(m.Id, "message and messagedata should share id");
                m.MessageData.Data.ShouldBe(_fixture.MessageBytes);
                m.MessageData.MetaData.ShouldBeNull();

                m.CreatedAtUtc.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
                m.PriorityDateUtc.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
                
                m.ExpiresAtUtc.ShouldBeNull();
                m.IsExpired(DateTime.UtcNow).ShouldBeFalse();
                m.Endpoint.ShouldBe("Test");
            }
        }


        [Fact]
        public async Task Should_enable_transient()
        {
            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxSession = new OutboxSession<ApplicationDbContext>(_fixture.Outbox, db);
                await outboxSession.Dispatch(_fixture.Message.MessageType, _fixture.MessageBytes, null, null, new OutboxDispatchOptions()
                {
                      SkipTransientDispatch = false,
                });
                await db.SaveChangesAsync();
            }

            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxMessages = db.Set<OutboxMessage>()
                    .Include(x => x.MessageData)
                    .OrderBy(x=>x.CreatedAtUtc)                    
                    .AsNoTracking()
                    .ToList();

                var m = outboxMessages.Last();
                m.Status.ShouldBe(1, "status should be 1/Processing");
                m.TryCount.ShouldBe(0);// we don't count the transient attempt towards outbox tries
            }
        }

        [Fact]
        public async Task Should_disable_transient()
        {
            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxSession = new OutboxSession<ApplicationDbContext>(_fixture.Outbox, db);
                await outboxSession.Dispatch(_fixture.Message.MessageType, _fixture.MessageBytes, null, null, new OutboxDispatchOptions()
                {
                    SkipTransientDispatch = true,
                });
                await db.SaveChangesAsync();
            }

            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxMessages = db.Set<OutboxMessage>()
                    .Include(x => x.MessageData)
                    .OrderBy(x => x.CreatedAtUtc)
                    .AsNoTracking()
                    .ToList();

                var m = outboxMessages.Last();
                m.Status.ShouldBe(0, "status should be 0/Ready");
                m.TryCount.ShouldBe(0);// we don't count the transient attempt towards outbox tries
            }
        }

        [Fact]
        public async Task Should_set_dalayed_dispatch()
        {
            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxSession = new OutboxSession<ApplicationDbContext>(_fixture.Outbox, db);
                await outboxSession.Dispatch(_fixture.Message.MessageType, _fixture.MessageBytes, null, null, new OutboxDispatchOptions()
                {
                     Delay = TimeSpan.FromHours(1)
                });
                await db.SaveChangesAsync();
            }

            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxMessages = db.Set<OutboxMessage>()
                    .Include(x => x.MessageData)
                    .OrderBy(x => x.CreatedAtUtc)
                    .AsNoTracking()
                    .ToList();

                var m = outboxMessages.Last();
                m.PriorityDateUtc.ShouldBeInRange(DateTime.UtcNow.AddMinutes(59), DateTime.UtcNow.AddSeconds(61));
            }
        }

        [Fact]
        public async Task Should_set_expiration()
        {
            var expiration = DateTime.UtcNow.AddMinutes(10);
            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxSession = new OutboxSession<ApplicationDbContext>(_fixture.Outbox, db);
                await outboxSession.Dispatch(_fixture.Message.MessageType, _fixture.MessageBytes, null, null, new OutboxDispatchOptions()
                {
                    ExpiresAtUtc =  expiration
                });
                await db.SaveChangesAsync();
            }

            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxMessages = db.Set<OutboxMessage>()
                    .Include(x => x.MessageData)
                    .OrderBy(x => x.CreatedAtUtc)
                    .AsNoTracking()
                    .ToList();

                var m = outboxMessages.Last();
                m.ExpiresAtUtc.ShouldBe(expiration);
            }
        }

        [Fact]
        public async Task Should_set_meta_data()
        {
            var expiration = DateTime.UtcNow.AddMinutes(10);
            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxSession = new OutboxSession<ApplicationDbContext>(_fixture.Outbox, db);
                await outboxSession.Dispatch(_fixture.Message.MessageType, _fixture.MessageBytes, _fixture.MessageMetaData);
                await db.SaveChangesAsync();
            }

            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxMessages = db.Set<OutboxMessage>()
                    .Include(x => x.MessageData)
                    .OrderBy(x => x.CreatedAtUtc)
                    .AsNoTracking()
                    .ToList();

                var m = outboxMessages.Last();
                m.MessageData.MetaData.ShouldNotBeNull();
                m.MessageData.MetaData = _fixture.SerializedMessageMetaData;
            }
        }


    }
}
