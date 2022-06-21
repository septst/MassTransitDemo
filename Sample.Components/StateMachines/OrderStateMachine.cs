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
                .Then(context => 
                    {
                        context.Instance.SubmitDate = context.Data.Timestamp;
                        context.Instance.CustomerNumber = context.Data.CustomerNumber;
                        context.Instance.Updated = InVar.Timestamp;
                    })
                .TransitionTo(SubmittedState));
        
        During(SubmittedState,
            Ignore(OrderSubmittedEvent));
        
        DuringAny(
            When(OrderSubmittedEvent)
                .Then(context => 
                    {
                        context.Instance.SubmitDate = context.Data.Timestamp;
                        context.Instance.CustomerNumber = context.Data.CustomerNumber;
                        context.Instance.Updated = InVar.Timestamp;
                    })      
            );
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

    public DateTime SubmitDate { get; set; }
    public DateTime Updated { get; set; }
    public string CustomerNumber { get; set; }
        

    public int Version { get; set; }
}