using MassTransit;
using Sample.Contracts;

namespace Sample.Components.StateMachines;

public class OrderStateMachine :
    MassTransitStateMachine<OrderState>
{
    public OrderStateMachine()
    {
        // Specify CorrelationId for the event
        Event(() => OrderSubmittedEvent,
            x =>
                x.CorrelateById(y => y.Message.OrderId));
        Event(() => OrderStatusRequestedEvent,
            x =>
            {
                x.CorrelateById(y => y.Message.OrderId);
                x.OnMissingInstance(x =>
                    x.ExecuteAsync(async context =>
                        {
                            if (context.RequestId.HasValue)
                            {
                                await context.RespondAsync<OrderNotFound>(new { context.Message.OrderId });
                            }
                        }
                    ));
            });

        //Specify instance state
        InstanceState(x => x.CurrentState);

        Initially(
            When(OrderSubmittedEvent)
                .Then(context =>
                {
                    context.Saga.SubmitDate = context.Message.Timestamp;
                    context.Saga.CustomerNumber = context.Message.CustomerNumber;
                    context.Saga.Updated = InVar.Timestamp;
                })
                .TransitionTo(SubmittedState));

        During(SubmittedState,
            Ignore(OrderSubmittedEvent));

        DuringAny(
            When(OrderStatusRequestedEvent)
                .RespondAsync(x =>
                    x.Init<OrderStatus>(new
                    {
                        OrderId = x.Saga.CorrelationId,
                        State = x.Saga.CurrentState
                    }))
        );

        DuringAny(
            When(OrderSubmittedEvent)
                .Then(context =>
                {
                    context.Saga.SubmitDate = context.Message.Timestamp;
                    context.Saga.CustomerNumber ??= context.Message.CustomerNumber;
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
    public string? CustomerNumber { get; set; }


    public int Version { get; set; }
}