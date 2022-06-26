using MassTransit;
using Sample.Contracts;

namespace Sample.Components.Consumers;

public class FullfillOrderConsumer :
    IConsumer<FullfillOrder>
{
    public async Task Consume(ConsumeContext<FullfillOrder> context)
    {
        var builder = new RoutingSlipBuilder(InVar.Id);

        builder.AddActivity(
            "AllocateInventory",
            new Uri("queue:allocate-inventory-execute"),
            new
            {
                ItemNumber = "Item123",
                Quantity = 10.0m
            });

        builder.AddVariable("OrderId", context.Message.OrderId);
    }
}