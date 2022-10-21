using Contracts.Events;
using MassTransit;
using OrderingApi.Entities;

namespace Sample.Api.Processes;

public class OrderSubmissionProcess :MassTransitStateMachine<OrderState>
{
    private readonly ILogger<OrderSubmissionProcess> _logger;
    public State Pending { get; private set; }
    public State Paid { get; private set; }
    public State Created { get; private set; }
    public State Cancelled { get; private set; }
    
    public OrderSubmissionProcess(ILogger<OrderSubmissionProcess> logger)
    {
        _logger = logger;
        this.InstanceState(x => x.CurrentState);
        this.ConfigureCorrelationIds();
        During(Initial,
            When(OrderSumitted)
                .TransitionTo(Pending)
        );
    }
    public Event<PaymentProcessed> PaymentProcessed { get; private set; }
    public Event<PaymentProcessed> PaymentRequestTimeout { get; private set; }
    private void ConfigureCorrelationIds()
    {
        Event(() => PaymentProcessed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentRequestTimeout, x => x.CorrelateById(context => context.Message.OrderId));
    }
}