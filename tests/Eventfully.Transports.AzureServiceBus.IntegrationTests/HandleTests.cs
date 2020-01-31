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
    public class HandleTests : IntegrationTestBase, IClassFixture<HandleTests.Fixture>
    {
        private readonly Fixture _fixture;
        private readonly ITestOutputHelper _log;

        public HandleTests(Fixture fixture, ITestOutputHelper log)
        {
            this._fixture = fixture;
            this._log = log;
            //using (var scope = NewScope())
            //{
            //}
        }

        public class Fixture
        { 

        }

       
     
        [Fact]
        public async Task Should_handle_from_service_bus_queue()
        {
            var fakeHandler = A.Fake<ITestPumpCallback>();
            A.CallTo(() => fakeHandler.Handle(A<TransportMessage>.Ignored, A<IEndpoint>.Ignored)).Returns(Task.CompletedTask);

            var pump = new AzureServiceBusMessagePump(fakeHandler.Handle, IntegrationTestFixture.QueueEndpoint,
                null, new AzureServiceBusMessagePumpSettings()
                {
                    MaxConcurrentHandlers = 1,
                    MaxCompletionImmediateRetry = 1
                });
            await pump.StartAsync(CancellationToken.None);

            var message = new TestMessage()
            {
                Id = Guid.NewGuid(),
                Description = "Test Handle Queue",
                MessageDate = DateTime.Now,
                Name = "Queue",
            };
            var messageBytes = IntegrationTestFixture.Serialize(message);
            await IntegrationTestFixture.WriteToQueue(IntegrationTestFixture.QueueEndpoint, message.MessageType, messageBytes, null);
            await Task.Delay(1000);

             A.CallTo(() => fakeHandler.Handle(
                A<TransportMessage>.That.Matches(x => 
                    x.MessageTypeIdentifier.Equals(message.MessageType)
                    && x.Data.SequenceEqual(messageBytes)
                ),
                A<IEndpoint>.That.Matches(x=>x.Equals(IntegrationTestFixture.QueueEndpoint))
            )).MustHaveHappenedOnceOrMore();
            await pump.StopAsync();
        }

        [Fact]
        public async Task Should_handle_from_service_bus_subscription()
        {
            var fakeHandler = A.Fake<ITestPumpCallback>();
            A.CallTo(() => fakeHandler.Handle(A<TransportMessage>.Ignored, A<IEndpoint>.Ignored)).Returns(Task.CompletedTask);

            var pump = new AzureServiceBusMessagePump(fakeHandler.Handle, IntegrationTestFixture.SubscriptionEndpoint,
                null, new AzureServiceBusMessagePumpSettings()
                {
                    MaxConcurrentHandlers = 1,
                    MaxCompletionImmediateRetry = 1
                });
            await pump.StartAsync(CancellationToken.None);

            var message = new TestMessage()
            {
                Id = Guid.NewGuid(),
                Description = "Test Handle Subscription",
                MessageDate = DateTime.Now,
                Name = "Topic Subscription",
            };
            var messageBytes = IntegrationTestFixture.Serialize(message);
            await IntegrationTestFixture.WriteToTopic(IntegrationTestFixture.TopicEndpoint, message.MessageType, messageBytes, null);
            await Task.Delay(1000);

            A.CallTo(() => fakeHandler.Handle(
               A<TransportMessage>.That.Matches(x =>
                   x.MessageTypeIdentifier.Equals(message.MessageType)
                   && x.Data.SequenceEqual(messageBytes)
               ),
               A<IEndpoint>.That.Matches(x => x.Equals(IntegrationTestFixture.SubscriptionEndpoint))
           )).MustHaveHappenedOnceOrMore();

            await pump.StopAsync();
        }


      



    }
}
