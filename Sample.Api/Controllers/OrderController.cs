using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Sample.Contracts;

namespace Sample.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrderController : ControllerBase
{
    private readonly IRequestClient<CheckOrder> _checkOrderRequestClient;
    private readonly ILogger<OrderController> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly IRequestClient<SubmitOrder> _submitOrderRequestClient;

    public OrderController(
        ILogger<OrderController> logger,
        IRequestClient<SubmitOrder> submitOrderRequestClient,
        IRequestClient<CheckOrder> checkOrderRequestClient,
        ISendEndpointProvider sendEndpointProvider,
        IPublishEndpoint publishEndpoint
    )
    {
        _logger = logger;
        _submitOrderRequestClient = submitOrderRequestClient;
        _checkOrderRequestClient = checkOrderRequestClient;
        _sendEndpointProvider = sendEndpointProvider;
        _publishEndpoint = publishEndpoint;
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid id)
    {
        var (status, notFound) = await _checkOrderRequestClient.GetResponse<OrderStatus, OrderNotFound>(new
        {
            OrderId = id
        });

        if (status.IsCompletedSuccessfully)
        {
            var response = await status;
            return Ok(response.Message);
        }
        else
        {
            var response = await notFound;
            return NotFound(response.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Post(Guid orderId,
        string customerNumber)
    {
        var (accepted, rejected) =
            await _submitOrderRequestClient.GetResponse<OrderSubmissionAccepted, OrderSubmissionRejected>(new
            {
                OrderId = orderId,
                InVar.Timestamp,
                CustomerNumber = customerNumber
            });

        return accepted.IsCompletedSuccessfully
            ? Accepted(await accepted)
            : BadRequest(await rejected);
    }

    [HttpPut]
    public async Task<IActionResult> Put(Guid orderId,
        string customerNumber)
    {
        var endpoint = await _sendEndpointProvider.GetSendEndpoint(
            new Uri("exchange:submit-order"));
        await endpoint.Send<SubmitOrder>(new
        {
            OrderId = orderId,
            InVar.Timestamp,
            CustomerNumber = customerNumber
        });

        return Accepted();
    }

    [HttpPatch]
    public async Task<IActionResult> Patch(Guid id)
    {
        await _publishEndpoint.Publish<OrderAccepted>(new
        {
            OrderId = id,
            InVar.Timestamp
        });

        return Accepted();
    }
}