﻿using MassTransit;
using Microsoft.Extensions.Logging;
using Sample.Contracts;

namespace Sample.Components.Consumers;

public class SubmitOrderConsumer
    : IConsumer<SubmitOrder>
{
    private readonly ILogger<SubmitOrderConsumer> _logger;

    public SubmitOrderConsumer(ILogger<SubmitOrderConsumer> logger)
    {
        _logger = logger;
    }
    
    public async Task Consume(ConsumeContext<SubmitOrder> context)
    {
        _logger.Log(LogLevel.Debug, "SubmitOrderConsumer: {CustomerNumber}", context.Message.CustomerNumber);
        if (context.RequestId != null)
        {
            if (context.Message.CustomerNumber.Contains("Test"))
            {
                await context.RespondAsync<OrderSubmissionRejected>(new
                {
                    context.Message.OrderId,
                    InVar.Timestamp,
                    context.Message.CustomerNumber,
                    Reason = $"The customer cannot place this order."
                });
            }
            else
            {
                await context.Publish<OrderSubmitted>(new
                {
                    context.Message.OrderId,
                    InVar.Timestamp,
                    context.Message.CustomerNumber
                });
                
                await context.RespondAsync<OrderSubmissionAccepted>(new
                {
                    context.Message.OrderId,
                    InVar.Timestamp,
                    context.Message.CustomerNumber
                });
            }
        }
    }
}