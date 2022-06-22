using System;
using System.Threading.Tasks;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Sample.Components.Consumers;
using Sample.Contracts;
using Xunit;
using Xunit.Abstractions;

namespace Sample.Components.Tests;

public class SubmitOrderConsumerTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public SubmitOrderConsumerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Consume_Order_Request_Returns_Acceptance()
    {
        var harness = new InMemoryTestHarness();
        var consumer = harness.Consumer<SubmitOrderConsumer>();

        await harness.Start();

        try
        {
            var orderId = InVar.Id;
            await harness.InputQueueSendEndpoint.Send<SubmitOrder>(new
            {
                OrderId = orderId,
                InVar.Timestamp,
                CustomerNumber = "Customer1"
            });

            var isConsumed = await consumer.Consumed.Any<SubmitOrder>();
            isConsumed.Should().BeTrue();
        }
        catch (Exception e)
        {
            _testOutputHelper.WriteLine(e.ToString());
            throw;
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Consume_Order_Request_Returns_OrderId()
    {
        var harness = new InMemoryTestHarness();
        var consumer = harness.Consumer<SubmitOrderConsumer>();

        await harness.Start();

        try
        {
            var orderId = InVar.Id;
            var requestClient = await harness.ConnectRequestClient<SubmitOrder>();

            var response = await requestClient.GetResponse<OrderSubmissionAccepted>(new
            {
                OrderId = orderId,
                InVar.Timestamp,
                CustomerNumber = "Customer1"
            });

            var isConsumed = await consumer.Consumed.Any<SubmitOrder>();
            var isSent = await harness.Sent.Any<OrderSubmissionAccepted>();
            isConsumed.Should().BeTrue();
            isSent.Should().BeTrue();
            response.Message.OrderId.Should().Be(orderId);
        }
        catch (Exception e)
        {
            _testOutputHelper.WriteLine(e.ToString());
            throw;
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Consume_Test_Consumer_Order_Request_Rejects_Order()
    {
        var harness = new InMemoryTestHarness();
        var consumer = harness.Consumer<SubmitOrderConsumer>();

        await harness.Start();

        try
        {
            var orderId = InVar.Id;
            var requestClient = await harness.ConnectRequestClient<SubmitOrder>();

            var (accepted, rejected) =
                await requestClient.GetResponse<OrderSubmissionAccepted, OrderSubmissionRejected>(new
                {
                    OrderId = orderId,
                    InVar.Timestamp,
                    CustomerNumber = "Test"
                });

            var isConsumed = await consumer.Consumed.Any<SubmitOrder>();
            var isSent = await harness.Sent.Any<OrderSubmissionRejected>();
            var response = await rejected;
            isConsumed.Should().BeTrue();
            isSent.Should().BeTrue();
            accepted.IsCompletedSuccessfully.Should().BeFalse();
            rejected.IsCompletedSuccessfully.Should().BeTrue();
            response.Message.Reason.Should().NotBeEmpty();
        }
        catch (Exception e)
        {
            _testOutputHelper.WriteLine(e.ToString());
            throw;
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Consume_Test_Consumer_Order_Request_Not_Publish_Order_Submitted()
    {
        var harness = new InMemoryTestHarness();
        var consumer = harness.Consumer<SubmitOrderConsumer>();

        await harness.Start();

        try
        {
            var orderId = InVar.Id;
            await harness.InputQueueSendEndpoint.Send<SubmitOrder>(new
            {
                OrderId = orderId,
                InVar.Timestamp,
                CustomerNumber = "TestCustomer"
            });

            var isConsumed = await consumer.Consumed.Any<SubmitOrder>();
            var isPublished = await harness.Published.Any<OrderSubmitted>();
            isConsumed.Should().BeTrue();
            isPublished.Should().BeFalse();
        }
        catch (Exception e)
        {
            _testOutputHelper.WriteLine(e.ToString());
            throw;
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Consume_Order_Request_Publish_Order_Submitted()
    {
        var harness = new InMemoryTestHarness();
        var consumer = harness.Consumer<SubmitOrderConsumer>();

        await harness.Start();

        try
        {
            var orderId = InVar.Id;
            await harness.InputQueueSendEndpoint.Send<SubmitOrder>(new
            {
                OrderId = orderId,
                InVar.Timestamp,
                CustomerNumber = "12345"
            });

            var isConsumed = await consumer.Consumed.Any<SubmitOrder>();
            var isPublished = await harness.Published.Any<OrderSubmitted>();
            isConsumed.Should().BeTrue();
            isPublished.Should().BeTrue();
        }
        catch (Exception e)
        {
            _testOutputHelper.WriteLine(e.ToString());
            throw;
        }
        finally
        {
            await harness.Stop();
        }
    }
}