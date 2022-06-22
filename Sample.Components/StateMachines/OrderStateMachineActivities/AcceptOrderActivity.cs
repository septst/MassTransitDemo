using MassTransit;
using Sample.Contracts;

namespace Sample.Components.StateMachines.OrderStateMachineActivities;

public class AcceptOrderActivity :
    IStateMachineActivity<OrderState, OrderAccepted>
{
    public void Probe(ProbeContext context)
    {
        context.Add("name", "accept-order");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public async Task Execute(
        BehaviorContext<OrderState, OrderAccepted> context,
        IBehavior<OrderState, OrderAccepted> next)
    {
        Console.WriteLine($"The Order Id is {context.Message.OrderId}");
        //Todo do something here later
        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderState, OrderAccepted, TException> context,
        IBehavior<OrderState, OrderAccepted> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }
}