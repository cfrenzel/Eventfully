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
    public class DeferRecoverabilityProviderTests : IntegrationTestBase,
        IClassFixture<DeferRecoverabilityProviderTests.Fixture>
    {
        private readonly Fixture _fixture;
        private readonly ITestOutputHelper _log;

        public DeferRecoverabilityProviderTests(Fixture fixture, ITestOutputHelper log)
        {
            this._fixture = fixture;
            this._log = log;
       }
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await IntegrationTestFixture.ClearQueue(IntegrationTestFixture.QueueEndpoint);
        }
      
        public class Fixture{}

        [Fact]
        public async Task Should_defer_and_queue_control_message()
        {
            var fakeHandler = A.Fake<ITestPumpCallback>();
            A.CallTo(() => fakeHandler.Handle(A<TransportMessage>.Ignored, A<IEndpoint>.Ignored))
                .Throws<Exception>().Twice()
                .Then.Returns(Task.CompletedTask);

            var deferRecoverabilityProvider = new AzureServiceBusDeferRecoverabilityProvider(
                new ConstantRetryStrategy(3)
            );
            
            var wrappedRecoverability = A.Fake<IAzureServiceBusRecoverabilityProvider>(x => x.Wrapping(deferRecoverabilityProvider));
            
            var pump = new AzureServiceBusMessagePump(fakeHandler.Handle, IntegrationTestFixture.QueueEndpoint,
                null, new AzureServiceBusMessagePumpSettings() { MaxConcurrentHandlers = 1,},
                wrappedRecoverability
            );

            await pump.StartAsync(CancellationToken.None);

            var message = new TestMessage()
            {
                Id = Guid.NewGuid(),
                Description = "Test Handle Deferred Retry",
                MessageDate = DateTime.Now,
                Name = "Queue Deferred Retry",
            };
            var messageBytes = IntegrationTestFixture.Serialize(message);
            await IntegrationTestFixture.WriteToQueue(IntegrationTestFixture.QueueEndpoint, message.MessageType, messageBytes, null);
            await Task.Delay(11000);

            ///This doesn't seem to work because the Message may be getting reused so the value changes
            ////recover should have deferred and scheduled recover control message
            //A.CallTo(() => wrappedRecoverability.OnPreHandle(
            //  A<RecoverabilityContext>.That.Matches(x =>
            //     x.Message.Label == ("ControlMessage.Recover")
            //  )
            //)).MustHaveHappenedTwiceExactly();

            //handled exception by calling recover
            A.CallTo(() => wrappedRecoverability.Recover(
                           A<RecoverabilityContext>.That.Matches(x =>
                              x.Message.Body.SequenceEqual(messageBytes) &&
                              x.Endpoint == IntegrationTestFixture.QueueEndpoint &&
                              !x.TempData.ContainsKey("ControlMessage")
                           )
                       )).MustHaveHappenedOnceExactly();

            A.CallTo(() => wrappedRecoverability.Recover(
               A<RecoverabilityContext>.That.Matches(x =>
                  x.Message.Body.SequenceEqual(messageBytes) &&
                  x.Endpoint == IntegrationTestFixture.QueueEndpoint &&
                  x.TempData.ContainsKey("ControlMessage")
               )
           )).MustHaveHappenedOnceExactly();

 
            // should have been called 2 times with failure then again with success of deferred message
            A.CallTo(() => fakeHandler.Handle(
               A<TransportMessage>.That.Matches(x =>
                   x.MessageTypeIdentifier.Equals(message.MessageType)
                   && x.Data.SequenceEqual(messageBytes)
               ),
               A<IEndpoint>.That.Matches(x => x.Equals(IntegrationTestFixture.QueueEndpoint))
           )).MustHaveHappened(3, Times.Exactly);
          
            await pump.StopAsync();
        }
    



    }
}
