﻿using System;
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
using Eventfully.Handlers;
using FakeItEasy;
using Eventfully.Transports;
using Eventfully.Transports.Testing;

namespace Eventfully.EFCoreOutbox.IntegrationTests
{
    using static IntegrationTestFixture;
    
    [Collection("Sequential")]
    public class TransientDispatchTests : IntegrationTestBase, IClassFixture<TransientDispatchTests.Fixture>
    {
        private Fixture _fixture;
        private int _delayForTransientProcessor = 400;
        public TransientDispatchTests(Fixture fixture)
        {
            this._fixture = fixture;
        }

        public class Fixture
        {
            public MessagingService MessagingService;

            public Outbox<ApplicationDbContext> Outbox;
            public TestMessage Message;
            public byte[] MessageBytes;
            public MessageMetaData MessageMetaData;
            public string SerializedMessageMetaData;

            public Fixture()
            {
                this.Message = new TestMessage()
                {
                    Id = Guid.NewGuid(),
                    Description = "Test Message Text",
                    Name = "Test Message Name",
                    MessageDate = DateTime.UtcNow,
                };

                this.MessageMetaData = new MessageMetaData(delay: TimeSpan.FromSeconds(10), correlationId: this.Message.Id.ToString(), messageId: this.Message.Id.ToString(), skipTransient: true);
                string messageBody = JsonConvert.SerializeObject(this.Message);
                this.SerializedMessageMetaData = this.MessageMetaData != null ? JsonConvert.SerializeObject(this.MessageMetaData) : null;
                this.MessageBytes = Encoding.UTF8.GetBytes(messageBody);

                this.Outbox = new Outbox<ApplicationDbContext>(new OutboxSettings()
                {
                    BatchSize = 10,
                    MaxConcurrency = 1,
                    MaxTries = 10,
                    SqlConnectionString = ConnectionString,
                    DisableTransientDispatch = false,
                });

                var handlerFactory = A.Fake<IServiceFactory>();
                this.MessagingService = new MessagingService(this.Outbox, handlerFactory);
            }
        }


        [Fact]
        public async Task Should_use_global_enable_transient()
        {
            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxSession = new OutboxSession<ApplicationDbContext>(_fixture.Outbox, db);
                var endpoint = A.Fake<TestOutboundEndpoint>(x => x.WithArgumentsForConstructor(() =>
                    new TestOutboundEndpoint("TransientDispatchTestEndpoint1.1", null, null)                   )
                );
                _fixture.MessagingService.AddEndpoint(endpoint);
                await _fixture.MessagingService.StartAsync();

                await outboxSession.Dispatch(_fixture.Message.MessageType, _fixture.MessageBytes, null, endpoint, new OutboxDispatchOptions());
                A.CallTo(() => endpoint.Dispatch(_fixture.Message.MessageType, _fixture.MessageBytes, null)).MustNotHaveHappened();
                await db.SaveChangesAsync();
                await Task.Delay(_delayForTransientProcessor);
                A.CallTo(() => endpoint.Dispatch(_fixture.Message.MessageType, _fixture.MessageBytes, null)).MustHaveHappenedOnceExactly();
            }

            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxMessages = db.Set<OutboxMessage>().Include(x => x.MessageData).OrderBy(x=>x.CreatedAtUtc)                    
                    .AsNoTracking().ToList();
                var m = outboxMessages.Last();
                m.Status.ShouldBe((int)OutboxMessageStatus.Processed, "status should be 2/Processed");
                m.TryCount.ShouldBe(0);// we don't count the transient attempt towards outbox tries
            }
        }

        [Fact]
        public async Task Should_overide_global_with_message_level_disable_transient()
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
                var outboxMessages = db.Set<OutboxMessage>().Include(x => x.MessageData).OrderBy(x => x.CreatedAtUtc)
                    .AsNoTracking().ToList();

                var m = outboxMessages.Last();
                m.Status.ShouldBe((int)OutboxMessageStatus.Ready, "status should be 0/Ready");
            }
        }

        [Fact]
        public async Task Should_become_ready_on_transient_failure()
        {
            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxSession = new OutboxSession<ApplicationDbContext>(_fixture.Outbox, db);
                var endpoint = A.Fake<TestOutboundEndpoint>(x => x.WithArgumentsForConstructor(() =>
                    new TestOutboundEndpoint("TransientDispatchTestEndpoint1.2", null, null))
                );
                _fixture.MessagingService.AddEndpoint(endpoint);
                await _fixture.MessagingService.StartAsync();

                A.CallTo(() => endpoint.Dispatch(_fixture.Message.MessageType, _fixture.MessageBytes, null))
                    .Throws(new ApplicationException("Test:Forced failure of transient dispatch"));

                await outboxSession.Dispatch(_fixture.Message.MessageType, _fixture.MessageBytes, null, endpoint, new OutboxDispatchOptions());
                
                A.CallTo(() => endpoint.Dispatch(_fixture.Message.MessageType, _fixture.MessageBytes, null)).MustNotHaveHappened();
                await db.SaveChangesAsync();
                await Task.Delay(_delayForTransientProcessor);
                A.CallTo(() => endpoint.Dispatch(_fixture.Message.MessageType, _fixture.MessageBytes, null)).MustHaveHappenedOnceExactly();
            }

            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxMessages = db.Set<OutboxMessage>().Include(x => x.MessageData).OrderBy(x => x.CreatedAtUtc)
                    .AsNoTracking().ToList();
                var m = outboxMessages.Last();
                m.Status.ShouldBe((int)OutboxMessageStatus.Ready, "status should be 0/Ready");
            }
        }

        [Fact]
        public async Task Should_fail_permnently_on_expired_message()
        {
            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxSession = new OutboxSession<ApplicationDbContext>(_fixture.Outbox, db);
                var endpoint = A.Fake<TestOutboundEndpoint>(x => x.WithArgumentsForConstructor(() =>
                    new TestOutboundEndpoint("TransientDispatchTestEndpoint1.3", null, null))
                );
                _fixture.MessagingService.AddEndpoint(endpoint);
                await _fixture.MessagingService.StartAsync();

                await outboxSession.Dispatch(_fixture.Message.MessageType, _fixture.MessageBytes, null, endpoint, new OutboxDispatchOptions()
                {
                    ExpiresAtUtc = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
                await Task.Delay(_delayForTransientProcessor);
            }

            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxMessages = db.Set<OutboxMessage>().Include(x => x.MessageData).OrderBy(x => x.CreatedAtUtc)
                    .AsNoTracking().ToList();
                var m = outboxMessages.Last();
                m.Status.ShouldBe((int)OutboxMessageStatus.Failed, "status should be 100/Failed");
            }
        }


        /// <summary>
        /// We want the outbox to handle the delay of the transport can't
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Should_abandon_transient_with_delay_when_endpoint_does_not_support_delay()
        {
            /*
              public class TestTransport : Transport
              {
                public override bool SupportsDelayedDispatch => false;
              */
            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxSession = new OutboxSession<ApplicationDbContext>(_fixture.Outbox, db);
                var endpoint = A.Fake<TestOutboundEndpoint>(x => x.WithArgumentsForConstructor(() =>
                    new TestOutboundEndpoint("TransientDispatchTestEndpoint1.4", null, null))
                );
                _fixture.MessagingService.AddEndpoint(endpoint);
                await _fixture.MessagingService.StartAsync();

                ///the configuration gets a little funky here because in normal use the 
                ///end user uses metaData to configure delay while behind the scenes meta is
                ///used to set the the OutboxDispatchOptions, this test should probably be done
                ///on the MessagingService
                MessageMetaData meta = new MessageMetaData(TimeSpan.FromMinutes(1));
                await outboxSession.Dispatch(_fixture.Message.MessageType, _fixture.MessageBytes, meta, endpoint, new OutboxDispatchOptions()
                {
                    Delay = meta.DispatchDelay
                });
                await db.SaveChangesAsync();
                await Task.Delay(_delayForTransientProcessor);
                A.CallTo(() => endpoint.Dispatch(_fixture.Message.MessageType, _fixture.MessageBytes, null)).MustNotHaveHappened();
            }

            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var outboxMessages = db.Set<OutboxMessage>().Include(x => x.MessageData).OrderBy(x => x.CreatedAtUtc)
                    .AsNoTracking().ToList();
                var m = outboxMessages.Last();
                m.Status.ShouldBe((int)OutboxMessageStatus.Ready, "status should be 0/Ready");
            }
        }
    }
}
