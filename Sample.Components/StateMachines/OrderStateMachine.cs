using MassTransit;
using MongoDB.Bson.Serialization.Attributes;
using Sample.Components.StateMachines.OrderStateMachineActivities;
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
                                await context.RespondAsync<OrderNotFound>(new { context.Message.OrderId });
                        }
                    ));
            });
        Event(() => AccountClosedEvent,
            x =>
                x.CorrelateBy((saga, context) =>
                    saga.CustomerNumber == context.Message.CustomerNumber));
        Event(() => OrderAcceptedEvent, x =>
            x.CorrelateById(y => y.Message.OrderId));

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
                .TransitionTo(Submitted));

        During(Submitted,
            Ignore(OrderSubmittedEvent),
            When(AccountClosedEvent)
                .TransitionTo(Cancelled),
            When(OrderAcceptedEvent)
                .Activity(x => x.OfType<AcceptOrderActivity>())
                .TransitionTo(Accepted));

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

    public State Submitted { get; private set; }
    public State Cancelled { get; private set; }
    public State Accepted { get; private set; }
    public Event<OrderSubmitted> OrderSubmittedEvent { get; private set; }
    public Event<OrderAccepted> OrderAcceptedEvent { get; private set; }
    public Event<CheckOrder> OrderStatusRequestedEvent { get; private set; }
    public Event<CustomerAccountClosed> AccountClosedEvent { get; private set; }
}

public class OrderState :
    SagaStateMachineInstance,
    ISagaVersion
{
    public string CurrentState { get; set; }

    public DateTime SubmitDate { get; set; }
    public DateTime Updated { get; set; }
    public string? CustomerNumber { get; set; }

    public int Version { get; set; }

    [BsonId] public Guid CorrelationId { get; set; }
}