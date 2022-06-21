using MassTransit;
using Sample.Contracts;

namespace Sample.Components.StateMachines;

public class OrderStateMachine:
    MassTransitStateMachine<OrderState>
{
    public OrderStateMachine()
    {
        // Specify CorrelationId for the event
        Event(() => OrderSubmittedEvent,
            x => 
                x.CorrelateById(y => y.Message.OrderId));
        
        //Specify instance state
        InstanceState(x => x.CurrentState);
        
        Initially(
            When(OrderSubmittedEvent)
                .TransitionTo(SubmittedState));
    }

    public State SubmittedState { get; private set; }
    public Event<OrderSubmitted> OrderSubmittedEvent { get; private set; }
}

public class OrderState : 
    SagaStateMachineInstance,
    ISagaVersion
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; }
    public int Version { get; set; }
}