using MassTransit;

namespace Sample.Components.Consumers;

public class SubmitOrderConsumerDefinition:
    ConsumerDefinition<SubmitOrderConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<SubmitOrderConsumer> consumerConfigurator)
    {
        endpointConfigurator.UseMessageRetry(r => r.Interval(5, 10000));
    }
}