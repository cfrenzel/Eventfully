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
    public class ProcessManagerStateMachineTests : IClassFixture<ProcessManagerStateMachineTests.Fixture>
    {
        private readonly Fixture _fixture;

        public ProcessManagerStateMachineTests(ProcessManagerStateMachineTests.Fixture fixture)
        {
            this._fixture = fixture;
        }


        public class Fixture
        {
            public MessageContext FakeContext { get; set; }
            public Fixture()
            {
                var messagingService = new MessagingService(A.Fake<IOutbox>(), A.Fake<IServiceFactory>(), null, null);
                FakeContext = new MessageContext(null, A.Fake<IEndpoint>(), messagingService);
            }
        }
        public class PizzaFulfillmentState : IProcessManagerMachineState
        {
            public Guid Id { get; set; }
            public DateTime? OrderedAtUtc { get; set; }
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

        public class PizzaFulfillmentProcess : ProcessManagerMachine<PizzaFulfillmentState, Guid>,
            IMachineMessageHandler<PizzaPaidForEvent>,
            IMachineMessageHandler<PizzaPreparedEvent>,
            IMachineMessageHandler<PizzaDeliveredEvent>
        {
            public PizzaFulfillmentProcess()
            {

            }

            public void Ordered()
            {
                Handle<PizzaPaidForEvent>((message, context) =>
                {
                    if (State.PaidAtUtc.HasValue)
                        return Task.CompletedTask;
                    State.PaidAtUtc = message.PaidAt;
                    if (State.CanBeShipped)
                        Become(Ready);
                    return Task.CompletedTask;
                });

                Handle<PizzaPreparedEvent>((message, context) =>
                {
                    if (State.PreparedAtUtc.HasValue)
                        return Task.CompletedTask;
                    State.PreparedAtUtc = message.PreparedAt;
                    if (State.CanBeShipped)
                        Become(Ready);
                    return Task.CompletedTask;
                });
            }

            public void Ready()
            {
                Handle<PizzaShippedEvent>((message, context) =>
                {
                    if (State.ShippedAtUtc.HasValue)
                        return Task.CompletedTask;
                    State.ShippedAtUtc = message.ShippedAt;
                    Become(OutForDelivery);
                    return Task.CompletedTask;
                });
            }

            public void OutForDelivery()
            {
                Handle<PizzaDeliveredEvent>((message, context) =>
                {
                    if (State.DeliveredAtUtc.HasValue)
                        return Task.CompletedTask;
                    State.DeliveredAtUtc = message.DeliveredAt;
                    Become(Complete);
                    return Task.CompletedTask;
                });
            }

            public void Complete()
            {
                State.CompletedAtUtc = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Same methods differe concrete type -Int64 key
        /// Using to test that the MethodInfo cache doesn't collide between generic types
        /// </summary>
        public class PizzaFulfillmentProcessInt64 : ProcessManagerMachine<PizzaFulfillmentState, Int64>,
          IMachineMessageHandler<PizzaPaidForEvent>,
          IMachineMessageHandler<PizzaPreparedEvent>,
          IMachineMessageHandler<PizzaDeliveredEvent>
        {
            public PizzaFulfillmentProcessInt64() { }

            public void Ordered()
            {
                Handle<PizzaPaidForEvent>((message, context) =>
                {
                    if (State.PaidAtUtc.HasValue)
                        return Task.CompletedTask;
                    State.PaidAtUtc = message.PaidAt;
                    if (State.CanBeShipped)
                        Become(Ready);
                    return Task.CompletedTask;
                });

                Handle<PizzaPreparedEvent>((message, context) =>
                {
                    if (State.PreparedAtUtc.HasValue)
                        return Task.CompletedTask;
                    State.PreparedAtUtc = message.PreparedAt;
                    if (State.CanBeShipped)
                        Become(Ready);
                    return Task.CompletedTask;
                });
            }

            public void Ready()
            {
                Handle<PizzaShippedEvent>((message, context) =>
                {
                    if (State.ShippedAtUtc.HasValue)
                        return Task.CompletedTask;
                    State.ShippedAtUtc = message.ShippedAt;
                    Become(OutForDelivery);
                    return Task.CompletedTask;
                });
            }

            public void OutForDelivery()
            {
                Handle<PizzaDeliveredEvent>((message, context) =>
                {
                    if (State.DeliveredAtUtc.HasValue)
                        return Task.CompletedTask;
                    State.DeliveredAtUtc = message.DeliveredAt;
                    Become(Complete);
                    return Task.CompletedTask;
                });
            }

            public void Complete()
            {
                State.CompletedAtUtc = DateTime.UtcNow;
            }
        }

        public class PizzaFulfillmentProcessCustomNaming : ProcessManagerMachine<PizzaFulfillmentState, Guid>,
         IMachineMessageHandler<PizzaPaidForEvent>,
         IMachineMessageHandler<PizzaPreparedEvent>,
         IMachineMessageHandler<PizzaDeliveredEvent>
        {
            public PizzaFulfillmentProcessCustomNaming() { }

            public void CustomOrdered()
            {
                Handle<PizzaPaidForEvent>((message, context) =>
                {
                    if (State.PaidAtUtc.HasValue)
                        return Task.CompletedTask;
                    State.PaidAtUtc = message.PaidAt;
                    if (State.CanBeShipped)
                        Become(CustomReady);
                    return Task.CompletedTask;
                });

                Handle<PizzaPreparedEvent>((message, context) =>
                {
                    if (State.PreparedAtUtc.HasValue)
                        return Task.CompletedTask;
                    State.PreparedAtUtc = message.PreparedAt;
                    if (State.CanBeShipped)
                        Become(CustomReady);
                    return Task.CompletedTask;
                });
            }
            public void CustomReady()
            {
                Handle<PizzaShippedEvent>((message, context) =>
                {
                    if (State.ShippedAtUtc.HasValue)
                        return Task.CompletedTask;
                    State.ShippedAtUtc = message.ShippedAt;
                    Become(CustomComplete);
                    return Task.CompletedTask;
                });
            }

            public void CustomComplete()
            {
                State.CompletedAtUtc = DateTime.UtcNow;
            }
            protected override string MapMethodNameToState(string methodName)
            {
                return methodName.Remove(0, 6);//remove custom for state name;
            }

            protected override string MapStateToMethodName(string state)
            {
                return $"Custom{state}";
            }
        }
            
        public class PizzaTriggeredFulfillmentProcess : ProcessManagerMachine<PizzaFulfillmentState, Guid>,
            ITriggeredBy<PizzaOrderedEvent>,
            IMachineMessageHandler<PizzaDeliveredEvent>
        {
            public PizzaTriggeredFulfillmentProcess() { }

            public Task Handle(PizzaOrderedEvent message, MessageContext context)
            {
                this.State.OrderedAtUtc = message.OrderedAt;
                Become(Ordered);
                return Task.CompletedTask;
            }
            public void Ordered()
            {
                Handle<PizzaDeliveredEvent>((message, context) =>
                {
                    if (State.DeliveredAtUtc.HasValue)
                        return Task.CompletedTask;
                    State.DeliveredAtUtc = message.DeliveredAt;
                    Become(Complete);
                    return Task.CompletedTask;
                });
            }

            public void Complete()
            {
                State.CompletedAtUtc = DateTime.UtcNow;
            }

        }


        public class DuplicateHandlerProcess : ProcessManagerMachine<PizzaFulfillmentState, Guid>,
           IMachineMessageHandler<PizzaPaidForEvent>,
           IMachineMessageHandler<PizzaPreparedEvent>
        {
            public DuplicateHandlerProcess() { }
            public void Initial()
            {
                Handle<PizzaPreparedEvent>((message, context) =>
                {
                    Become(Duplicate);
                    return Task.CompletedTask;
                });
            }
            public void Duplicate()
            {
                Handle<PizzaPreparedEvent>((message, context) => { return Task.CompletedTask; });
                Handle<PizzaPreparedEvent>((message, context) => { return Task.CompletedTask; });
                Handle<PizzaPaidForEvent>((message, context) => { return Task.CompletedTask; });
            }
        }

        public class PizzaOrderedEvent : IntegrationEvent
        {
            public override string MessageType => "Pizza.Ordered";
            public DateTime? OrderedAt { get; set; }
        }

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
        public void Should_not_instantiate_state_on_new()
        {
            var process = new PizzaFulfillmentProcess();
            process.State.ShouldBeNull();
        }

        [Fact]
        public void Should_throw_on_set_state_to_null()
        {
            var process = new PizzaFulfillmentProcess();
            Should.Throw<InvalidOperationException>(
               () => process.SetState(null)
            );
        }


        [Fact]
        public void Should_throw_on_register_duplicate_handlers_per_state()
        {
            var process = new DuplicateHandlerProcess();
            process.SetState(new PizzaFulfillmentState() { CurrentState = "Initial" });
            Should.Throw<InvalidOperationException>(
               () => process.Handle(
                    new PizzaPreparedEvent() { PreparedAt = DateTime.UtcNow },
                    _fixture.FakeContext)
            );
        }

        [Fact]
        public void Should_throw_on_message_with_no_handler_in_state()
        {
            var process = new PizzaFulfillmentProcess();
            process.SetState(new PizzaFulfillmentState() { CurrentState = "Ordered" });
            Should.Throw<InvalidMessageForStateException>(
               () => process.Handle(
                    new PizzaDeliveredEvent() { DeliveredAt = DateTime.UtcNow },
                    _fixture.FakeContext)
            );
        }

        [Fact]
        public void Should_throw_on_set_state_with_invalid_state_string()
        {
            var process = new PizzaFulfillmentProcess();
            Should.Throw<Exception>(
               () => process.SetState(new PizzaFulfillmentState() { CurrentState = "InvalidStateName" })
            );
        }

        [Fact]
        public void Should_leave_current_state_null_when_set_state_with_null()
        {
            var process = new PizzaFulfillmentProcess();
            process.SetState(new PizzaFulfillmentState() { CurrentState = null });
            process.State.CurrentState.ShouldBeNull();
        }


        [Fact]
        public async Task Should_use_trigger_handler_on_ITriggeredBy_event()
        {
            var orderDate = DateTime.UtcNow;
            var process = new PizzaTriggeredFulfillmentProcess();
            process.SetState(new PizzaFulfillmentState());
            process.State.CurrentState.ShouldBeNull();
            var trigger = process as ITriggeredBy<PizzaOrderedEvent>;
            await trigger.Handle(new PizzaOrderedEvent() { OrderedAt = orderDate }, _fixture.FakeContext);
            process.State.OrderedAtUtc.ShouldBe(orderDate);
            process.State.CurrentState.ShouldBe("Ordered");

            var deliverDate = DateTime.UtcNow;
            await process.Handle(new PizzaDeliveredEvent() { DeliveredAt = deliverDate }, _fixture.FakeContext);
            process.State.CurrentState.ShouldBe("Complete");
            process.State.DeliveredAtUtc.ShouldBe(deliverDate);
            process.State.CompletedAtUtc.Value.ShouldBeGreaterThan(deliverDate);
        }

        [Fact]
        public async Task Should_transition_on_message_to_completion()
        {
            var process = new PizzaFulfillmentProcess();
            process.SetState(new PizzaFulfillmentState() { CurrentState = "Ordered" });
            process.State.CurrentState.ShouldBe("Ordered");

            var prepareDate = DateTime.UtcNow;
            await process.Handle(new PizzaPreparedEvent() { PreparedAt = prepareDate }, _fixture.FakeContext);
            process.State.CurrentState.ShouldBe("Ordered");
            process.State.PreparedAtUtc.ShouldBe(prepareDate);

            var payDate = DateTime.UtcNow;
            await process.Handle(new PizzaPaidForEvent() { PaidAt = payDate }, _fixture.FakeContext);
            process.State.CurrentState.ShouldBe("Ready");
            process.State.PaidAtUtc.ShouldBe(payDate);

            var shipDate = DateTime.UtcNow;
            await process.Handle(new PizzaShippedEvent() { ShippedAt = shipDate }, _fixture.FakeContext);
            process.State.CurrentState.ShouldBe("OutForDelivery");
            process.State.ShippedAtUtc.ShouldBe(shipDate);

            var deliverDate = DateTime.UtcNow;
            await process.Handle(new PizzaDeliveredEvent() { DeliveredAt = deliverDate }, _fixture.FakeContext);
            process.State.CurrentState.ShouldBe("Complete");
            process.State.DeliveredAtUtc.ShouldBe(deliverDate);
            process.State.CompletedAtUtc.Value.ShouldBeGreaterThan(deliverDate);
        }


        [Fact]
        public async Task Should_keep_seperate_method_cache_per_concrete_machine()
        {
            var process = new PizzaFulfillmentProcess();
            process.SetState(new PizzaFulfillmentState() { CurrentState = "Ordered" });
            process.State.CurrentState.ShouldBe("Ordered");
          
            var process2 = new PizzaFulfillmentProcessInt64();
            process2.SetState(new PizzaFulfillmentState() { CurrentState = "Ordered" });
            process2.State.CurrentState.ShouldBe("Ordered");


            var prepareDate = DateTime.UtcNow;
            await process.Handle(new PizzaPreparedEvent() { PreparedAt = prepareDate }, _fixture.FakeContext);
            process.State.CurrentState.ShouldBe("Ordered");
            process.State.PreparedAtUtc.ShouldBe(prepareDate);

            await process2.Handle(new PizzaPreparedEvent() { PreparedAt = prepareDate }, _fixture.FakeContext);
            process2.State.CurrentState.ShouldBe("Ordered");
            process2.State.PreparedAtUtc.ShouldBe(prepareDate);

            var payDate = DateTime.UtcNow;
            await process.Handle(new PizzaPaidForEvent() { PaidAt = payDate }, _fixture.FakeContext);
            process.State.CurrentState.ShouldBe("Ready");
            process.State.PaidAtUtc.ShouldBe(payDate);

            await process2.Handle(new PizzaPaidForEvent() { PaidAt = payDate }, _fixture.FakeContext);
            process2.State.CurrentState.ShouldBe("Ready");
            process2.State.PaidAtUtc.ShouldBe(payDate);

            var shipDate = DateTime.UtcNow;
            await process.Handle(new PizzaShippedEvent() { ShippedAt = shipDate }, _fixture.FakeContext);
            process.State.CurrentState.ShouldBe("OutForDelivery");
            process.State.ShippedAtUtc.ShouldBe(shipDate);

            await process2.Handle(new PizzaShippedEvent() { ShippedAt = shipDate }, _fixture.FakeContext);
            process2.State.CurrentState.ShouldBe("OutForDelivery");
            process2.State.ShippedAtUtc.ShouldBe(shipDate);

            var deliverDate = DateTime.UtcNow;
            await process.Handle(new PizzaDeliveredEvent() { DeliveredAt = deliverDate }, _fixture.FakeContext);
            process.State.CurrentState.ShouldBe("Complete");
            process.State.DeliveredAtUtc.ShouldBe(deliverDate);
            process.State.CompletedAtUtc.Value.ShouldBeGreaterThan(deliverDate);

            await process2.Handle(new PizzaDeliveredEvent() { DeliveredAt = deliverDate }, _fixture.FakeContext);
            process2.State.CurrentState.ShouldBe("Complete");
            process2.State.DeliveredAtUtc.ShouldBe(deliverDate);
            process2.State.CompletedAtUtc.Value.ShouldBeGreaterThan(deliverDate);
        }

        [Fact]
        public async Task Should_transition_with_custom_naming()
        {
            var process = new PizzaFulfillmentProcessCustomNaming();
            process.SetState(new PizzaFulfillmentState() { CurrentState = "Ordered" });
            process.State.CurrentState.ShouldBe("Ordered");

            var prepareDate = DateTime.UtcNow;
            await process.Handle(new PizzaPreparedEvent() { PreparedAt = prepareDate }, _fixture.FakeContext);
            process.State.CurrentState.ShouldBe("Ordered");
            process.State.PreparedAtUtc.ShouldBe(prepareDate);

            var payDate = DateTime.UtcNow;
            await process.Handle(new PizzaPaidForEvent() { PaidAt = payDate }, _fixture.FakeContext);
            process.State.CurrentState.ShouldBe("Ready");
            process.State.PaidAtUtc.ShouldBe(payDate);

            var shipDate = DateTime.UtcNow;
            await process.Handle(new PizzaShippedEvent() { ShippedAt = shipDate }, _fixture.FakeContext);
            process.State.CurrentState.ShouldBe("Complete");
            process.State.CompletedAtUtc.ShouldNotBeNull();
        }
    }
}