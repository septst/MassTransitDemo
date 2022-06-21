using MassTransit;
using Sample.Contracts;

namespace Sample.Components.StateMachines;

public class OrderStateMachineDefinition :
    SagaDefinition<OrderState>
{
    public OrderStateMachineDefinition()
    {
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureSaga(
        IReceiveEndpointConfigurator endpointConfigurator,
        ISagaConfigurator<OrderState> sagaConfigurator)
    {
        var partition = endpointConfigurator.CreatePartitioner(8);
        sagaConfigurator.Message<OrderSubmitted>(x =>
            x.UsePartitioner(partition,
                o => o.Message.CustomerNumber));

        endpointConfigurator.UseMessageRetry(r => 
                r.Exponential(50,
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromSeconds(30) ));
        endpointConfigurator.UseInMemoryOutbox();
    }
}