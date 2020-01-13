﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Xunit;
using Shouldly;
using Eventfully.Outboxing;
using Eventfully.Transports.Testing;
using FakeItEasy;
using Eventfully.Handlers;
using Eventfully.Transports;

namespace Eventfully.Core.UnitTests
{
    [Collection("Sequential")]
    public class MessagingServiceTests : IClassFixture<MessagingServiceTests.Fixture>
    {
        private readonly Fixture _fixture;

        public MessagingServiceTests(MessagingServiceTests.Fixture fixture)
        {
            this._fixture = fixture;
        }


        public class Fixture
        {
            public TestMessage Message { get; set; }
            public TestOutboundEndpoint Endpoint { get; set; }
            public Fixture()
            {
                this.Message = new TestMessage();
                this.Endpoint = new TestOutboundEndpoint("MessagingServiceTests_1.1", new List<Type> { typeof(Fixture.TestMessage) }, null);
                UnitTestFixture.MessagingService.AddEndpoint(this.Endpoint);
            }

            public class TestMessage : IIntegrationEvent
            {
                public string MessageType => "Test.MessagingServiceTests";
                public string Description { get; set; } = "TestMessage";
            }
            public class UnkownMessage : IIntegrationEvent
            {
                public string MessageType => "Unkown.MessagingServiceTests";
                public string Description { get; set; } = "TestMessage";
            }

        }

        [Fact]
        public void Should_be_singleton()
        {
            UnitTestFixture.MessagingService.ShouldBe(MessagingService.Instance);
        }

        [Fact]
        public void Should_throw_if_instantiated_more_than_once()
        {
            Should.Throw<InvalidOperationException>(() =>
                new MessagingService(A.Fake<IOutbox>(), A.Fake<IMessageHandlerFactory>())
            ); ;
        }

        [Fact]
        public void Should_throw_if_add_endpoint_without_transport()
        {

            var endpoint = new TestNullTransportEndpoint();
           Should.Throw<InvalidOperationException>(() =>
                UnitTestFixture.MessagingService.AddEndpoint(endpoint)
            );
        }

        [Fact]
        public void Should_throw_when_dispatch_without_message()
        {
            Should.Throw<ArgumentNullException>(() =>
               UnitTestFixture.MessagingService.Dispatch(null, null, A.Fake<IEndpoint>(), A.Fake<IOutboxSession>())
            );
        }
      
        [Fact]
        public void Should_throw_when_dispatch_without_endpoint()
        {
            var outboxSession = A.Fake<IOutboxSession>();
            Should.Throw<ArgumentNullException>(() =>
               UnitTestFixture.MessagingService.Dispatch(_fixture.Message, null, null, outboxSession)
            );
        }

        [Fact]
        public void Should_throw_on_dispatch_when_cannot_determine_endpoint_from_message_type()
        {
            var outboxSession = A.Fake<IOutboxSession>();
            Should.Throw<EndpointNotFoundException>(() =>
               UnitTestFixture.MessagingService.Dispatch(new Fixture.UnkownMessage(), null, outboxSession)
            );
        }

        [Fact]
        public void Should_dispatch_through_outbound_message_pipeline_with_default_serializer()
        {
            var outboxSession = A.Fake<IOutboxSession>();
            UnitTestFixture.MessagingService.Publish(_fixture.Message, outboxSession, null);
            A.CallTo(() => outboxSession.Dispatch(
                            _fixture.Message.MessageType,
                            A<byte[]>.That.Matches(x => x.Length > 0),
                            A<MessageMetaData>.That.IsNull(),
                            _fixture.Endpoint,
                            A<OutboxDispatchOptions>.That.Matches(x => x.Delay == null && !x.ExpiresAtUtc.HasValue)
                            )
                        ).MustHaveHappenedOnceExactly();
            A.CallTo(() => UnitTestFixture.MessagingService.DispatchTransientCore(A<string>.Ignored, A<byte[]>.Ignored, A<MessageMetaData>.Ignored, A<string>.Ignored))
                .MustNotHaveHappened();
        
        }

        [Fact]
        public void Should_convert_meta_data_to_dispatch_options_for_expires_at()
        {
            DateTime expiresAt = DateTime.UtcNow.AddMinutes(45);
            var outboxSession = A.Fake<IOutboxSession>();
            var metaData = new MessageMetaData(expiresAtUtc: expiresAt);
            UnitTestFixture.MessagingService.Publish(_fixture.Message, outboxSession, metaData);

            A.CallTo(() => outboxSession.Dispatch(
                            _fixture.Message.MessageType,
                            A<byte[]>.That.Matches(x => x.Length > 0),
                            metaData,
                            _fixture.Endpoint,
                            A<OutboxDispatchOptions>.That
                                .Matches(x => x.Delay == null
                                           && x.ExpiresAtUtc.Equals(expiresAt))
                            )
                        ).MustHaveHappenedOnceExactly();
            A.CallTo(() => UnitTestFixture.MessagingService.DispatchTransientCore(A<string>.Ignored, A<byte[]>.Ignored, A<MessageMetaData>.Ignored, A<string>.Ignored))
                .MustNotHaveHappened();
        }

        [Fact]
        public void Should_convert_meta_data_to_outbox_delay_when_not_endoint_supports_delay()
        {
            var delay = TimeSpan.FromMinutes(30);
            var outboxSession = A.Fake<IOutboxSession>();
            var metaData = new MessageMetaData(delay: delay);
            UnitTestFixture.MessagingService.Publish(_fixture.Message, outboxSession, metaData);
                
            A.CallTo(() => outboxSession.Dispatch(
                            _fixture.Message.MessageType,
                            A<byte[]>.That.Matches(x => x.Length > 0),
                            metaData,
                            _fixture.Endpoint,
                            A<OutboxDispatchOptions>.That
                                .Matches(x => x.Delay.Equals(delay)
                                          && x.SkipTransientDispatch == true //set when delay set in outbox                                          
                                          && !x.ExpiresAtUtc.HasValue)
                            )
                        ).MustHaveHappenedOnceExactly();

            A.CallTo(() => UnitTestFixture.MessagingService.DispatchTransientCore(A<string>.Ignored, A<byte[]>.Ignored, A<MessageMetaData>.Ignored, A<string>.Ignored))
                .MustNotHaveHappened();
        }
        [Fact]
        public void Should_convert_meta_data_to_dispatch_options_for_skip_transient()
        {
            var delay = TimeSpan.FromMinutes(30);
            var outboxSession = A.Fake<IOutboxSession>();
            var metaData = new MessageMetaData(skipTransient: true);
            UnitTestFixture.MessagingService.Publish(_fixture.Message, outboxSession, metaData);

            A.CallTo(() => outboxSession.Dispatch(
                            _fixture.Message.MessageType,
                            A<byte[]>.That.Matches(x => x.Length > 0),
                           metaData,
                            _fixture.Endpoint,
                            A<OutboxDispatchOptions>.That
                                .Matches(x => x.SkipTransientDispatch == true
                                          && x.Delay == null
                                          && !x.ExpiresAtUtc.HasValue)
                            )
                        ).MustHaveHappenedOnceExactly();

            A.CallTo(() => UnitTestFixture.MessagingService.DispatchTransientCore(A<string>.Ignored, A<byte[]>.Ignored, A<MessageMetaData>.Ignored, A<string>.Ignored))
                .MustNotHaveHappened();
        }
    }
}