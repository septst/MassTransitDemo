using MassTransit;
using Warehouse.Contracts;

namespace Sample.Components.CourierActivities;

public class AllocateInventoryActivity :
    IActivity<AllocateInventoryArguments, AllocateInventoryLog>
{
    private readonly IRequestClient<AllocateInventory> _requestClient;

    public AllocateInventoryActivity(IRequestClient<AllocateInventory> requestClient)
    {
        _requestClient = requestClient;
    }

    public async Task<ExecutionResult> Execute(ExecuteContext<AllocateInventoryArguments> context)
    {
        var orderId = context.Arguments.OrderId;
        var itemNumber = context.Arguments.ItemNumber;
        if (string.IsNullOrEmpty(itemNumber))
            throw new ArgumentNullException(nameof(itemNumber));

        var quantity = context.Arguments.Quantity;
        if (quantity < 0.0m)
            throw new ArgumentException(nameof(quantity));

        var allocationId = InVar.Id;
        var response = await _requestClient.GetResponse<InventoryAllocated>(new
        {
            AllocationId = allocationId,
            itemNumber,
            Quantity = quantity
        });

        return context.Completed(new { AllocationId = allocationId });
    }

    public async Task<CompensationResult> Compensate(CompensateContext<AllocateInventoryLog> context)
    {
        await context.Publish<AllocationReleaseRequested>(new
        {
            context.Log.AllocationId,
            Reason = "Test Reason"
        });

        return context.Compensated();
    }
}