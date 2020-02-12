using System;
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
    public class MessageDispatcherTests : IClassFixture<MessageDispatcherTests.Fixture>
    {
        private readonly Fixture _fixture;
        public IServiceFactory FakeServiceFactory;
        public IOutboxSession FakeOutboxSession;
        public IEndpoint FakeEndpoint;
        public MessagingService MessagingService;
    
        public MessageDispatcherTests(MessageDispatcherTests.Fixture fixture)
        {
            this._fixture = fixture;
            this.FakeServiceFactory = A.Fake<IServiceFactory>();
            this.FakeOutboxSession = A.Fake<IOutboxSession>();
            this.FakeEndpoint = A.Fake<IEndpoint>();
            var fakeOutbox = A.Fake<IOutbox>();
            this.MessagingService = new MessagingService(fakeOutbox, this.FakeServiceFactory, null, null);
        }


        public class Fixture
        {
        }

        public class TestMessage : IIntegrationEvent
        {
            public string MessageType => "MessageDispatcher.Test";
            public Guid TestId { get; set;}
        }

      
        public class TestState : IProcessManagerMachineState
        {
            public Guid Id { get; set; }
            public string CurrentState { get; set; }
        }

        [Fact]
        public async Task Should_dispatch_to_handler_class()
        {
            var testMessage = new TestMessage() { TestId = Guid.NewGuid() };
            var fakeHandlerClass = A.Fake<IMessageHandler<TestMessage>>();
            
            var messageContext = new MessageContext(null, this.FakeEndpoint, this.MessagingService, new MessageTypeProperties()
            {
                MessageTypeIdentifier = testMessage.MessageType,
                Type = testMessage.GetType()
            });

            A.CallTo(() => this.FakeServiceFactory.CreateScope()).Returns(this.FakeServiceFactory);
            A.CallTo(() => this.FakeServiceFactory.GetInstance(typeof(IOutboxSession))).Returns(this.FakeOutboxSession);
            A.CallTo(() => this.FakeServiceFactory.GetInstance(typeof(IMessageHandler<TestMessage>))).Returns(fakeHandlerClass as IMessageHandler<TestMessage>);

            MessageDispatcher dispatcher = new MessageDispatcher(this.FakeServiceFactory);
            await dispatcher.Dispatch(testMessage, messageContext);

            A.CallTo(() => fakeHandlerClass.Handle(testMessage, messageContext))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task Should_dispatch_to_state_machine()
        {
            var testMessage = new TestMessage() { TestId = Guid.NewGuid() };
            var fakeStateMachine = A.Fake<ISaga<TestState, Guid>>(
                x => x.Implements<IMachineMessageHandler<TestMessage>>()
            );
            A.CallTo(() => ((ISaga)fakeStateMachine).FindKey(A<IIntegrationMessage>.Ignored, A<MessageMetaData>.Ignored))
             .Returns(Guid.NewGuid());

            var fakeSagaPersistence = A.Fake<ISagaPersistence<TestState, Guid>>();
       
            MessagingMap.AddSaga(new SagaProperties(
                fakeStateMachine.GetType(), typeof(Guid),
                typeof(TestState),
                fakeSagaPersistence.GetType(),
                new List<Type> { typeof(TestMessage) },
                true /* custom handler for state machines*/)
            );

            var messageContext = new MessageContext(null, this.FakeEndpoint, this.MessagingService, new MessageTypeProperties()
            {
                SagaType = fakeStateMachine.GetType(), 
                MessageTypeIdentifier = testMessage.MessageType,
                Type = testMessage.GetType()
            });
           
            A.CallTo(() => this.FakeServiceFactory.CreateScope()).Returns(this.FakeServiceFactory);
            A.CallTo(() => this.FakeServiceFactory.GetInstance(typeof(IOutboxSession))).Returns(this.FakeOutboxSession);
            A.CallTo(() => this.FakeServiceFactory.GetInstance(fakeSagaPersistence.GetType())).Returns(fakeSagaPersistence);
            A.CallTo(() => this.FakeServiceFactory.GetInstance(typeof(ICustomMessageHandler<TestMessage>))).Returns(fakeStateMachine as ICustomMessageHandler<TestMessage>);
            A.CallTo(() => this.FakeServiceFactory.GetInstance(typeof(IMessageHandler<TestMessage>))).Returns(fakeStateMachine as IMessageHandler<TestMessage>);

            MessageDispatcher dispatcher = new MessageDispatcher(this.FakeServiceFactory);
            await dispatcher.Dispatch(testMessage, messageContext);

            A.CallTo(() => ((ISagaPersistence)fakeSagaPersistence).LoadOrCreateState(fakeStateMachine, A<object>.Ignored))
             .MustHaveHappenedOnceExactly();

            A.CallTo(() => ((IMachineMessageHandler<TestMessage>)fakeStateMachine).Handle(testMessage, messageContext))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => ((ISagaPersistence)fakeSagaPersistence).AddOrUpdateState(fakeStateMachine))
             .MustHaveHappenedOnceExactly();

        }

        [Fact]
        public async Task Should_dispatch_to_saga()
        {
            var testMessage = new TestMessage() { TestId = Guid.NewGuid() };
            var fakeSaga = A.Fake<ISaga<TestState, Guid>>(
                x => x.Implements<IMessageHandler<TestMessage>>()
            );
            A.CallTo(() => ((ISaga)fakeSaga).FindKey(A<IIntegrationMessage>.Ignored, A<MessageMetaData>.Ignored))
             .Returns(Guid.NewGuid());

            var fakeSagaPersistence = A.Fake<ISagaPersistence<TestState, Guid>>();

            MessagingMap.AddSaga(new SagaProperties(
                fakeSaga.GetType(), typeof(Guid),
                typeof(TestState),
                fakeSagaPersistence.GetType(),
                new List<Type> { typeof(TestMessage) },
                false /* no custom handler for saga*/)
            );

            var messageContext = new MessageContext(null, this.FakeEndpoint, this.MessagingService, new MessageTypeProperties()
            {
                SagaType = fakeSaga.GetType(),
                MessageTypeIdentifier = testMessage.MessageType,
                Type = testMessage.GetType()
            });

            A.CallTo(() => this.FakeServiceFactory.CreateScope()).Returns(this.FakeServiceFactory);
            A.CallTo(() => this.FakeServiceFactory.GetInstance(typeof(IOutboxSession))).Returns(this.FakeOutboxSession);
            A.CallTo(() => this.FakeServiceFactory.GetInstance(fakeSagaPersistence.GetType())).Returns(fakeSagaPersistence);
            A.CallTo(() => this.FakeServiceFactory.GetInstance(typeof(IMessageHandler<TestMessage>))).Returns(fakeSaga as IMessageHandler<TestMessage>);

            MessageDispatcher dispatcher = new MessageDispatcher(this.FakeServiceFactory);
            await dispatcher.Dispatch(testMessage, messageContext);

            A.CallTo(() => ((ISagaPersistence)fakeSagaPersistence).LoadOrCreateState(fakeSaga, A<object>.Ignored))
             .MustHaveHappenedOnceExactly();

            A.CallTo(() => ((IMessageHandler<TestMessage>)fakeSaga).Handle(testMessage, messageContext))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => ((ISagaPersistence)fakeSagaPersistence).AddOrUpdateState(fakeSaga))
             .MustHaveHappenedOnceExactly();

        }


    }
}