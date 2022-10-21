/*namespace Company.Consumers
{
    using MassTransit;

    public class SampleConsumerDefinition : ConsumerDefinition<SampleConsumer>
    {
        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<SampleConsumer> consumerConfigurator)
        {
            endpointConfigurator.UseMessageRetry(r => r.Intervals(500, 1000));
        }
    }
}*/