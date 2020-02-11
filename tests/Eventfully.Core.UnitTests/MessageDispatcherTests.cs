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

        public MessageDispatcherTests(MessageDispatcherTests.Fixture fixture)
        {
            this._fixture = fixture;
        }


        public class Fixture
        {
           
        }

        public class TestMessage : IIntegrationEvent
        {
            public string MessageType => "MessageDispatcher.Test";
            public Guid TestId { get; set;}
        }

        public class BasicTestStateMachine : ProcessManagerMachine<TestStateMachineState, Guid>,
            IMachineMessageHandler<TestMessage>
        {
            public BasicTestStateMachine()
            {
                this.MapIdFor<TestMessage>((m, md) => m.TestId);
            }
            public void Initial()
            {
                Handle<TestMessage>((message, context) =>
                {
                    return Task.CompletedTask;
                });
            }
        }
        public class TriggeredTestStateMachine : ProcessManagerMachine<TestStateMachineState, Guid>,
           ITriggeredBy<TestMessage>
        {
            public TriggeredTestStateMachine()
            {
                this.MapIdFor<TestMessage>((m, md) => m.TestId);
            }
            public Task Handle(TestMessage ev, MessageContext context)
            {
                return Task.CompletedTask;
            }
        }

        public class TestStateMachineState : IProcessManagerMachineState
        {
            public Guid Id { get; set; }
            public string CurrentState { get; set; }
        }

        public class StubSagaPersistence : ISagaPersistence<TestStateMachineState, Guid>
        {
            TestStateMachineState State;
            public StubSagaPersistence(TestStateMachineState state)
            {
                State = state;
            }
            public Task AddOrUpdateState(ISaga<TestStateMachineState, Guid> saga)
            {
                State = saga.State;
                return Task.CompletedTask;
            }

            public Task AddOrUpdateState(ISaga saga)
            {
                State =(TestStateMachineState)saga.State;
                return Task.CompletedTask;
            }

            public Task LoadOrCreateState(ISaga<TestStateMachineState, Guid> saga, Guid sagaId)
            {
                saga.SetState(State);
                return Task.CompletedTask;
            }

            public Task LoadOrCreateState(ISaga saga, object sagaId)
            {
                saga.SetState(State);
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Should_call_ProcessManagerMachine_Handle()
        {
            var sagaType = typeof(TestStateMachineState);
            var fakeServiceFactory = A.Fake<IServiceFactory>();
            var fakeOutboxSession = A.Fake<IOutboxSession>();
            var fakeEndpoint = A.Fake<IEndpoint>();
            var fakeOutbox = A.Fake<IOutbox>();
            var messagingService = new MessagingService(fakeOutbox, fakeServiceFactory, null, null);

            var stateMachine = new BasicTestStateMachine();
            var wrappedStateMachine = A.Fake<BasicTestStateMachine>(x => x.Wrapping(stateMachine));
           
            var testMessage = new TestMessage();

            var sagaState = new TestStateMachineState() { CurrentState = "Initial" };
            var stubSagaPersistence = new StubSagaPersistence(sagaState);
            var sagaPersistenceType = stubSagaPersistence.GetType();
            
            MessagingMap.AddSaga(new SagaProperties(
                sagaType, typeof(Guid),
                typeof(TestStateMachineState),
                sagaPersistenceType,
                new List<Type> { typeof(TestMessage) },
                true /* custom handler for state machines*/)
            );

            var messageContext = new MessageContext(null, fakeEndpoint, messagingService, new MessageTypeProperties()
            {
                SagaType = sagaType, 
                MessageTypeIdentifier = testMessage.MessageType,
                Type = typeof(TestMessage)
            });
           
            A.CallTo(() => fakeServiceFactory.CreateScope()).Returns(fakeServiceFactory);
            A.CallTo(() => fakeServiceFactory.GetInstance(typeof(IOutboxSession))).Returns(fakeOutboxSession);
            A.CallTo(() => fakeServiceFactory.GetInstance(stubSagaPersistence.GetType())).Returns(stubSagaPersistence);
            A.CallTo(() => fakeServiceFactory.GetInstance(typeof(ICustomMessageHandler<TestMessage>))).Returns(wrappedStateMachine as ICustomMessageHandler<TestMessage>);
            A.CallTo(() => fakeServiceFactory.GetInstance(typeof(IMessageHandler<TestMessage>))).Returns(wrappedStateMachine as IMessageHandler<TestMessage>);

            MessageDispatcher dispatcher = new MessageDispatcher(fakeServiceFactory);
            await dispatcher.Dispatch(testMessage, messageContext);

            A.CallTo(() => stateMachine.Handle(testMessage, messageContext))
                .MustHaveHappenedOnceExactly();

        }

       
    }
}