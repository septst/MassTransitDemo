using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sample.Contracts;

namespace Sample.Service.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public CustomerController(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }
        
        [HttpDelete]
        public async Task<IActionResult> Delete(
            Guid id,
            string customerNumber)
        {
            await _publishEndpoint.Publish<CustomerAccountClosed>(new
            {
                Id = id,
                CustomerNumber = customerNumber
            });

            return Ok();
        }
    }
}
