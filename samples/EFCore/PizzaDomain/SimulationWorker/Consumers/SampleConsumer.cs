/*using Microsoft.Extensions.Logging;

namespace Company.Consumers
{
    using System.Threading.Tasks;
    using MassTransit;
    using Contracts;

    public class SampleConsumer : IConsumer<Contracts.SampleMessage>
    {
        private ILogger<SampleConsumer> _logger;

        public SampleConsumer(ILogger<SampleConsumer> logger)
        {
            _logger = logger;
        }
        
        public Task Consume(ConsumeContext<Contracts.SampleMessage> context)
        {
            _logger.LogInformation("Received Text: {Text}", context.Message.Value);
            return Task.CompletedTask;
        }
    }
}*/