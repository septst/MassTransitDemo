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

        public OrderController(
            ILogger<OrderController> logger,
            IRequestClient<SubmitOrder> requestClient
        )
        {
            _logger = logger;
            _requestClient = requestClient;
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
                ? Ok(await accepted)
                : BadRequest(await rejected);
        }
    }
}