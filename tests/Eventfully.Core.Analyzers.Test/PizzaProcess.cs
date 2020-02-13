using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Eventfully.Core.Analyzers.Test
{  
     /// <summary>
     /// This is included as a reminder to make changes in the Analyzer/code fixer when changes are made to the 
     /// process manager related interfaces
     /// </summary>      
    public class PizzaFulfillmentProcess : ProcessManagerMachine<PizzaFulfillmentStatus, Guid>,
       ITriggeredBy<PizzaOrderedEvent>,
       IMachineMessageHandler<PizzaPaidForEvent>,
       IMachineMessageHandler<PizzaPreparedEvent>,
       IMachineMessageHandler<PizzaDeliveredEvent>
    {  
        private readonly ILogger<PizzaFulfillmentProcess> _log;
        private readonly IMessagingClient _messageClient;
         
        public PizzaFulfillmentProcess(IMessagingClient messageClient, ILogger<PizzaFulfillmentProcess> log)
        { 
            _log = log;
            _messageClient = messageClient;
                     
            this.MapIdFor< PizzaOrderedEvent>((m, md) => m.OrderId);
            this.MapIdFor<PizzaPaidForEvent>((m, md) => m.OrderId);
            this.MapIdFor<PizzaPreparedEvent>((m, md) => m.OrderId);
            this.MapIdFor<PizzaDeliveredEvent>((m, md) => m.OrderId);
        }


        public Task Handle(PizzaOrderedEvent ev, MessageContext context)
        {
            this.State.OrderedAtUtc = ev.OrderedAt;
            Become(Ordered);
            return Task.CompletedTask;
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


        public class Persistence : SagaPersistence<PizzaFulfillmentStatus, Guid>
        {
            public Persistence() { }
            public override  Task LoadOrCreateState(ISaga<PizzaFulfillmentStatus, Guid> saga, Guid sagaId)
            {
                PizzaFulfillmentStatus state = null;
                state = state ?? new PizzaFulfillmentStatus(sagaId);
                saga.SetState(state);
                return Task.CompletedTask;
            }
            public override Task AddOrUpdateState(ISaga<PizzaFulfillmentStatus, Guid> saga)
            {
                return Task.CompletedTask;
            }
        }

    }

    public class PizzaOrderedEvent : IntegrationEvent
    {
        public override string MessageType => "Pizza.Ordered";
        public Guid OrderId { get; set; }
        public int Quantity { get; set; }
        public string SpecialInstructions { get; set; }
        public DateTime OrderedAt { get; set; }
    }

    public class PizzaPaidForEvent : IntegrationEvent
    {
        public Guid OrderId { get; set; }
        public override string MessageType => "Pizza.PaidFor";
        public DateTime PaidAt { get; set; }
        public string Method { get; set; }
        public Decimal Amount { get; set; }
    }

    public class PizzaPreparedEvent : IntegrationEvent
    {
        public Guid OrderId { get; set; }
        public override string MessageType => "Pizza.Prepared";
        public DateTime PreparedAt { get; set; }
    }
    public class PizzaShippedEvent : IntegrationEvent
    {
        public Guid OrderId { get; set; }
        public override string MessageType => "Pizza.Shipped";
        public DateTime ShippedAt { get; set; }
    }
    public class PizzaDeliveredEvent : IntegrationEvent
    {
        public Guid OrderId { get; set; }
        public override string MessageType => "Pizza.Delivered";
        public DateTime DeliveredAt { get; set; }
    }


    public class PizzaFulfillmentStatus : IProcessManagerMachineState
    {
        public Guid Id { get; private set; }
        public DateTime? OrderedAtUtc { get; set; }
        public DateTime? PreparedAtUtc { get; set; }
        public DateTime? PaidAtUtc { get; set; }
        public DateTime? ShippedAtUtc { get; set; }
        public DateTime? DeliveredAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public bool CanBeShipped { get { return PreparedAtUtc.HasValue && PaidAtUtc.HasValue; } }

        /// <summary>
        /// required for the statemachine to save is current state
        /// </summary>
        public string CurrentState { get; set; }

        /// <summary>
        /// optimistic conccurency
        /// required to make sure that multiple instances of the state machine don't interfer with eachother
        /// </summary>
        [Timestamp, ConcurrencyCheck]
        public byte[] RowVersion { get; set; }

        private PizzaFulfillmentStatus() { }
        public PizzaFulfillmentStatus(Guid id)
        {
            this.Id = id;
        }
    }
}
