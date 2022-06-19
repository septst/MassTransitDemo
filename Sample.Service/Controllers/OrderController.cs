using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Sample.Contracts;

namespace Sample.Service.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly ILogger<OrderController> _logger;
        private readonly IRequestClient<SubmitOrder> _requestClient;
        private readonly ISendEndpointProvider _sendEndpointProvider;

        public OrderController(
            ILogger<OrderController> logger,
            IRequestClient<SubmitOrder> requestClient,
            ISendEndpointProvider  sendEndpointProvider
        )
        {
            _logger = logger;
            _requestClient = requestClient;
            _sendEndpointProvider = sendEndpointProvider;
        }

        [HttpPost]
        public async Task<IActionResult> Post(Guid orderId,
            string customerNumber)
        {
            var (accepted, rejected) =
                await _requestClient.GetResponse<OrderSubmissionAccepted, OrderSubmissionRejected>(new
                {
                    OrderId = orderId,
                    Timestamp = InVar.Timestamp,
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
                new Uri($"exchange:submit-order"));
            await endpoint.Send<SubmitOrder>(new
                {
                    OrderId = orderId,
                    Timestamp = InVar.Timestamp,
                    CustomerNumber = customerNumber
                });

            return Accepted();
        }
    }
}