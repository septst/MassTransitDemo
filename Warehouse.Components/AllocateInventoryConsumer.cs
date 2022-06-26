using MassTransit;
using Warehouse.Contracts;

namespace Warehouse.Components;

public class AllocateInventoryConsumer :
    IConsumer<AllocateInventory>
{
    public async Task Consume(ConsumeContext<AllocateInventory> context)
    {
        await Task.Delay(100);

        await context.RespondAsync<InventoryAllocated>(new
        {
            context.Message.AllocationId,
            context.Message.ItemNumber,
            context.Message.Quantity
        });
    }
}