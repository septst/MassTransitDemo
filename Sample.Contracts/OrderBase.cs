namespace Sample.Contracts;

public interface OrderBase
{
    Guid OrderId { get; }
    DateTime Timestamp { get; }

    string CustomerNumber { get; }
}