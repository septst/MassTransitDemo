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
        Event(() => OrderStatusRequestedEvent, x => 
        {
            x.CorrelateById(y => y.Message.OrderId);
            //x.OnMissingInstance
        });

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
        
        During(
            When(OrderStatusRequestedEvent)
            .RespondAsync(x => x.Init<OrderStatus>(new
            {
                OrderId = x.Instance.CorrelationId,
                State = x.Instance.CurrentState
            }))
        );
        
        DuringAny(
            When(OrderSubmittedEvent)
                .Then(context => 
                    {
                        context.Instance.SubmitDate = context.Data.Timestamp;
                        context.Instance.CustomerNumber = context.Data.CustomerNumber;
                    })      
            );
    }

    public State SubmittedState { get; private set; }
    public Event<OrderSubmitted> OrderSubmittedEvent { get; private set; }
    public Event<CheckOrder> OrderStatusRequestedEvent { get; private set; }
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