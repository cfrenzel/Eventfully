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
    [Collection("Sequential")]
    public class ProcessManagerStateMachineTests : IClassFixture<ProcessManagerStateMachineTests.Fixture>
    {
        private readonly Fixture _fixture;

        public ProcessManagerStateMachineTests(ProcessManagerStateMachineTests.Fixture fixture)
        {
            this._fixture = fixture;
        }


        public class Fixture
        {
            //public PizzaFullfilmentProcess PizzaProcess { get; set; }
            public MessageContext FakeContext { get; set; }
            public Fixture()
            {
                //PizzaProcess = new PizzaFullfilmentProcess();
                var messagingService = new MessagingService(A.Fake<IOutbox>(), A.Fake<IServiceFactory>(), null, null);
                FakeContext = new MessageContext(null, A.Fake<IEndpoint>(), messagingService);
            }
        }
        public class PizzaFullfilmentState : IProcessManagerMachineState
        {
            public Guid Id { get; set; }
            public DateTime? PreparedAtUtc { get; set; }
            public DateTime? PaidAtUtc { get; set; }
            public DateTime? ShippedAtUtc { get; set; }
            public DateTime? DeliveredAtUtc { get; set; }
            public DateTime? CompletedAtUtc { get; set; }
            public bool CanBeShipped { get { return PreparedAtUtc.HasValue && PaidAtUtc.HasValue; } }
            public string CurrentState { get; set; }
            public byte[] RowVersion { get; set; }
            public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        }

        public class PizzaFullfilmentProcess : ProcessManagerMachine<PizzaFullfilmentState, Guid>,
            //IMachineMessageHandler<PizzaOrderedEvent>,
            IMachineMessageHandler<PizzaPaidForEvent>,
            IMachineMessageHandler<PizzaPreparedEvent>,
            IMachineMessageHandler<PizzaDeliveredEvent>
        {
            //public override Action InitialState => Ordered;

            public PizzaFullfilmentProcess(){
                Become(Ordered);
            }

            public void Ordered()
            {
                //Ignore<PizzaOrderedEvent>();
                Handle<PizzaPaidForEvent>((message, context) => {
                    if (State.PaidAtUtc.HasValue)
                        return;
                    State.PaidAtUtc = message.PaidAt;
                    if (State.CanBeShipped)
                        Become(Ready);
                });

                Handle<PizzaPreparedEvent>((message, context) => {
                    if (State.PreparedAtUtc.HasValue)
                        return;
                    State.PreparedAtUtc = message.PreparedAt;
                    if (State.CanBeShipped)
                        Become(Ready);
                });
            }

            public void Ready()
            {
                Handle<PizzaShippedEvent>((message, context) => {
                    if (State.ShippedAtUtc.HasValue)
                        return;
                    State.ShippedAtUtc = message.ShippedAt;
                    Become(OutForDelivery);
                });
            }

            public void OutForDelivery()
            {
                Handle<PizzaDeliveredEvent>((message, context) => {
                    if (State.DeliveredAtUtc.HasValue)
                        return;
                    State.DeliveredAtUtc = message.DeliveredAt;
                    Become(Complete);
                });
            }

            public void Complete()
            {
                State.CompletedAtUtc = DateTime.UtcNow;
            }
        }

        public class DuplicateHandlerProcess : ProcessManagerMachine<PizzaFullfilmentState, Guid>,
           IMachineMessageHandler<PizzaPaidForEvent>,
           IMachineMessageHandler<PizzaPreparedEvent>
        {
            public DuplicateHandlerProcess(){Become(Initial);}
            public void Initial() {
                Handle<PizzaPreparedEvent>((message, context) => Become(Duplicate));
                
            }
            public void Duplicate()
            {
                Handle<PizzaPreparedEvent>((message, context) => {});
                Handle<PizzaPreparedEvent>((message, context) =>{});
                Handle<PizzaPaidForEvent>((message, context) => {});
            }
        }

        //public class PizzaOrderedEvent : IntegrationEvent
        //{
        //    public override string MessageType => "Pizza.Ordered";
        //}

        public class PizzaPaidForEvent : IntegrationEvent
        {
            public override string MessageType => "Pizza.PaidFor";
            public DateTime PaidAt { get; set; }
            public string Method { get; set; }
            public Decimal Amount { get; set; }
        }

        public class PizzaPreparedEvent : IntegrationEvent
        {
            public override string MessageType => "Pizza.Prepared";
            public DateTime PreparedAt { get; set; }
        }
        public class PizzaShippedEvent : IntegrationEvent
        {
            public override string MessageType => "Pizza.Shipped";
            public DateTime ShippedAt { get; set; }
        }
        public class PizzaDeliveredEvent : IntegrationEvent
        {
            public override string MessageType => "Pizza.Delivered";
            public DateTime DeliveredAt { get; set; }
        }


        [Fact]
        public void Should_instantiate_state_on_new()
        {
            var process = new PizzaFullfilmentProcess();
            process.State.ShouldNotBeNull();
            process.State.CurrentState.ShouldBe("Ordered");
        }

        [Fact]
        public void Should_throw_on_register_duplicate_handlers_per_state()
        {
            var process = new DuplicateHandlerProcess();
            Should.Throw<InvalidOperationException>(
               () => process.HandleCore(
                    new PizzaPreparedEvent() { PreparedAt = DateTime.UtcNow },
                    _fixture.FakeContext)
            );
        }

        [Fact]
        public void Should_throw_on_message_with_no_handler_in_state()
        {
            var process = new PizzaFullfilmentProcess();
            Should.Throw<InvalidProcessManagerStateException>(
               () => process.HandleCore(
                    new PizzaDeliveredEvent() { DeliveredAt = DateTime.UtcNow },
                    _fixture.FakeContext)
            );
        }

        [Fact]
        public void Should_throw_on_set_state_with_invalid_state_string()
        {
            var process = new PizzaFullfilmentProcess();
            Should.Throw<Exception>(
               () => process.SetState(new PizzaFullfilmentState() {  CurrentState = "InvalidStateName"})
            );
        }

        [Fact]
        public void Should_leave_current_state_null_when_setState_with_null_current_state()
        {
            var process = new PizzaFullfilmentProcess();
            process.SetState(new PizzaFullfilmentState() { CurrentState = null });
            process.State.CurrentState.ShouldBeNull();
        }

        [Fact]
        public void Should_throw_on_set_state_with_null_state()
        {
            var process = new PizzaFullfilmentProcess();
            Should.Throw<ArgumentNullException>(
               () => process.SetState(null)
            );
        }


        [Fact]
        public void Should_transition_on_message_to_completion()
        {
            var process = new PizzaFullfilmentProcess();
            process.State.CurrentState.ShouldBe("Ordered");

            var prepareDate = DateTime.UtcNow;
            process.HandleCore(new PizzaPreparedEvent() { PreparedAt = prepareDate }, _fixture.FakeContext);
            process.State.CurrentState.ShouldBe("Ordered");
            process.State.PreparedAtUtc.ShouldBe(prepareDate);

            var payDate = DateTime.UtcNow;
            process.HandleCore(new PizzaPaidForEvent() { PaidAt = payDate }, _fixture.FakeContext);
            process.State.CurrentState.ShouldBe("Ready");
            process.State.PaidAtUtc.ShouldBe(payDate);

            var shipDate = DateTime.UtcNow;
            process.HandleCore(new PizzaShippedEvent() { ShippedAt = shipDate }, _fixture.FakeContext);
            process.State.CurrentState.ShouldBe("OutForDelivery");
            process.State.ShippedAtUtc.ShouldBe(shipDate);

            var deliverDate = DateTime.UtcNow;
            process.HandleCore(new PizzaDeliveredEvent() { DeliveredAt = deliverDate }, _fixture.FakeContext);
            process.State.CurrentState.ShouldBe("Complete");
            process.State.DeliveredAtUtc.ShouldBe(deliverDate);
            process.State.CompletedAtUtc.Value.ShouldBeGreaterThan(deliverDate);
        }

    }
}