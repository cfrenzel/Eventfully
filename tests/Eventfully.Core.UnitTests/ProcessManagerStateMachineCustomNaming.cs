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
    public class ProcessManagerStateMachineCustomNamingTests : IClassFixture<ProcessManagerStateMachineCustomNamingTests.Fixture>
    {
        private readonly Fixture _fixture;

        public ProcessManagerStateMachineCustomNamingTests(ProcessManagerStateMachineCustomNamingTests.Fixture fixture)
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