using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Shouldly;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;
using FakeItEasy;
using System.Data.SqlClient;
using Xunit.Abstractions;
using Eventfully.Transports.AzureServiceBus;
using Microsoft.Azure.ServiceBus;
using System.Threading;

namespace Eventfully.Transports.AzureServieBus.IntegrationTests
{
    using static IntegrationTestFixture;
    
    [Collection("Sequential")]
    public class DispatchTests : IntegrationTestBase, IClassFixture<DispatchTests.Fixture>
    {
        private readonly Fixture _fixture;
        private readonly ITestOutputHelper _log;

        public DispatchTests(Fixture fixture, ITestOutputHelper log)
        {
            this._fixture = fixture;
            this._log = log;

            IntegrationTestFixture.ClearQueue(IntegrationTestFixture.QueueEndpoint).GetAwaiter().GetResult();
            //using (var scope = NewScope())
            //{
            //}
        }

        public class Fixture
        {
        }

        [Fact]
        public void Should_set_support_for_delayed_dispatch()
        {
            IntegrationTestFixture.Transport.SupportsDelayedDispatch.ShouldBeTrue();
        }

       

        [Fact]
        public async Task Should_dispatch_to_service_bus_queue()
        {
            var message = new TestMessage()
            {
                Id = Guid.NewGuid(),
                Description = "Test queue",
                MessageDate = DateTime.Now,
                Name = "Queue",
            };
            var messageBytes = IntegrationTestFixture.Serialize(message);

            var fakeHandler = A.Fake<ITestMessageHandler>();
            A.CallTo(() => fakeHandler.HandleMessage(A<Message>.Ignored, A<CancellationToken>.Ignored)).Returns(Task.CompletedTask);
            A.CallTo(() => fakeHandler.HandleException(A<ExceptionReceivedEventArgs>.Ignored)).Returns(Task.CompletedTask);

            var client = IntegrationTestFixture.ReadFromQueue(IntegrationTestFixture.QueueEndpoint, fakeHandler);
            
            await IntegrationTestFixture.Transport.Dispatch(message.MessageType, messageBytes, IntegrationTestFixture.QueueEndpoint, new MessageMetaData(messageId: message.Id.ToString()));
            await Task.Delay(1000);

             A.CallTo(() => fakeHandler.HandleMessage(
                A<Message>.That.Matches(x => 
                    x.MessageId.Equals(message.Id.ToString())
                    && x.Body.SequenceEqual(messageBytes)
                ),
                A<CancellationToken>.Ignored
            )).MustHaveHappenedOnceOrMore();

            //A.CallTo(() => fakeHandler.HandleException(
            //   A<ExceptionReceivedEventArgs>.Ignored
            //)).MustNotHaveHappened();

            await client.CloseAsync();
        }


        [Fact]
        public async Task Should_dispatch_to_service_bus_topic()
        {
            var message = new TestMessage()
            {
                Description = "Test topic",
                Id = Guid.NewGuid(),
                MessageDate = DateTime.Now,
                Name = "Topic",
            };
            var messageBytes = IntegrationTestFixture.Serialize(message);
            var fakeHandler = A.Fake<ITestMessageHandler>();
            A.CallTo(() => fakeHandler.HandleMessage(A<Message>.Ignored, A<CancellationToken>.Ignored)).Returns(Task.CompletedTask);
            A.CallTo(() => fakeHandler.HandleException(A<ExceptionReceivedEventArgs>.Ignored)).Returns(Task.CompletedTask);

            var client = IntegrationTestFixture.ReadFromTopicSubscription(IntegrationTestFixture.SubscriptionEndpoint, fakeHandler);
            await IntegrationTestFixture.Transport.Dispatch(message.MessageType, messageBytes, IntegrationTestFixture.TopicEndpoint, new MessageMetaData(messageId: message.Id.ToString()));
            await Task.Delay(1000);

            A.CallTo(() => fakeHandler.HandleMessage(
                A<Message>.That.Matches(x => 
                    x.MessageId.Equals(message.Id.ToString()) &&
                    x.Body.SequenceEqual(messageBytes)
                ),
                A<CancellationToken>.Ignored
            )).MustHaveHappenedOnceOrMore();

            await client.CloseAsync();

        }



        [Fact]
        public async Task Should_delay_on_service_bus_queue()
        {
            var message = new TestMessage()
            {
                Id = Guid.NewGuid(),
                Description = "Test Delay Queue",
                MessageDate = DateTime.Now,
                Name = "Queue Delay",
            };
            var messageBytes = IntegrationTestFixture.Serialize(message);

            var fakeHandler = A.Fake<ITestMessageHandler>();
            A.CallTo(() => fakeHandler.HandleMessage(A<Message>.Ignored, A<CancellationToken>.Ignored)).Returns(Task.CompletedTask);
            A.CallTo(() => fakeHandler.HandleException(A<ExceptionReceivedEventArgs>.Ignored)).Returns(Task.CompletedTask);

            var client = IntegrationTestFixture.ReadFromQueue(IntegrationTestFixture.QueueEndpoint, fakeHandler);

            var meta = new MessageMetaData(delay: TimeSpan.FromSeconds(10), messageId: message.Id.ToString());
            await IntegrationTestFixture.Transport.Dispatch(message.MessageType, messageBytes, IntegrationTestFixture.QueueEndpoint, meta);
            await Task.Delay(7000);

            A.CallTo(() => fakeHandler.HandleMessage(
               A<Message>.That.Matches(x =>
                   x.MessageId.Equals(message.Id.ToString())
                   && x.Body.SequenceEqual(messageBytes)
               ),
               A<CancellationToken>.Ignored
            )).MustNotHaveHappened();

            await Task.Delay(6000);

            A.CallTo(() => fakeHandler.HandleMessage(
               A<Message>.That.Matches(x =>
                   x.MessageId.Equals(message.Id.ToString())
                   && x.Body.SequenceEqual(messageBytes)
               ),
               A<CancellationToken>.Ignored
           )).MustHaveHappenedOnceOrMore();

            await client.CloseAsync();
        }

        [Fact]
        public async Task Should_expire_on_service_bus_queue()
        {
            var message = new TestMessage()
            {
                Id = Guid.NewGuid(),
                Description = "Test Expire Queue",
                MessageDate = DateTime.Now,
                Name = "Queue Delay",
            };
            var messageBytes = IntegrationTestFixture.Serialize(message);

            var fakeHandler = A.Fake<ITestMessageHandler>();
            A.CallTo(() => fakeHandler.HandleMessage(A<Message>.Ignored, A<CancellationToken>.Ignored)).Returns(Task.CompletedTask);
            A.CallTo(() => fakeHandler.HandleException(A<ExceptionReceivedEventArgs>.Ignored)).Returns(Task.CompletedTask);

          
            var meta = new MessageMetaData(expiresAtUtc: DateTime.UtcNow.AddSeconds(10), messageId: message.Id.ToString());
            await IntegrationTestFixture.Transport.Dispatch(message.MessageType, messageBytes, IntegrationTestFixture.QueueEndpoint, meta);

            await Task.Delay(12000);

            var client = IntegrationTestFixture.ReadFromQueue(IntegrationTestFixture.QueueEndpoint, fakeHandler);

            await Task.Delay(3000);
           
            A.CallTo(() => fakeHandler.HandleMessage(
               A<Message>.That.Matches(x =>
                   x.MessageId.Equals(message.Id.ToString())
                   && x.Body.SequenceEqual(messageBytes)
               ),
               A<CancellationToken>.Ignored
            )).MustNotHaveHappened();          
            await client.CloseAsync();
        }

    }
}
