/*using Microsoft.Extensions.Logging;

namespace Company.Consumers
{
    using System.Threading.Tasks;
    using MassTransit;
    using Contracts;

    public class PizzaWorkflow : IConsumer<Contracts.OrderCreated>
    {
        private ILogger<PizzaWorkflow> _logger;

        public OrderConsumer(ILogger<PizzaWorkflow> logger)
        {
            _logger = logger;
        }
        
        public Task Consume(ConsumeContext<Contracts.OrderCreated> context)
        {
            _logger.LogInformation("Received Order: {OrderId} for SKU: {Sku}", context.Message.OrderId, context.Message.Sku);
            return Task.CompletedTask;
        }
    }
}*/