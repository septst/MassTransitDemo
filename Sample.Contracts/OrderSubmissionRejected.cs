namespace Sample.Contracts;

public interface OrderSubmissionRejected : OrderBase
{
    public string Reason { get; set; }
}