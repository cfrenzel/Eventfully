using Contracts;
using Contracts.Commands;
using Contracts.Events;
using MassTransit;
using MassTransit.Logging;

namespace OrderingApi.Consumers;

public class PaymentRequestTimeoutConsumer : IConsumer<PaymentRequestTimeout>
{
    private readonly ILogger<PaymentRequestTimeoutConsumer> _logger;

    private readonly ApplicationDbContext _db;
   
    public PaymentRequestTimeoutConsumer(ApplicationDbContext db, ILogger<PaymentRequestTimeoutConsumer> logger)
    {
        _logger = logger;
        _db = db;
    }

    public async Task Consume(ConsumeContext<PaymentRequestTimeout> context)
    {
        _logger.LogInformation("Payment Processing Timeout: {OrderId}", context.Message.OrderId);
        var paymentEndpoint = await context.GetSendEndpoint(new Uri("queue:cancel-payment"));
        await paymentEndpoint.Send(new CancelPayment()
        {
            OrderId = context.Message.OrderId,
        });
        await _db.SaveChangesAsync();
    }

    public class Definition : ConsumerDefinition<PaymentRequestTimeoutConsumer>
    {
        private readonly IServiceProvider _provider;
        public Definition(IServiceProvider provider) => _provider = provider;
        
        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<PaymentRequestTimeoutConsumer> consumerConfigurator)
        {
            endpointConfigurator.UseEntityFrameworkOutbox<ApplicationDbContext>(_provider);
        }
    }
}