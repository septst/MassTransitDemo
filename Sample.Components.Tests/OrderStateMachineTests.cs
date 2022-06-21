using System;
using System.Threading.Tasks;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Sample.Components.Consumers;
using Sample.Components.StateMachines;
using Sample.Contracts;
using Xunit;
using Xunit.Abstractions;

namespace Sample.Components.Tests;

public class OrderStateMachineTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public OrderStateMachineTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Publish_Order_Submitted_Creates_A_New_Instance()
    {
        var orderStateMachine = new OrderStateMachine();

        var harness = new InMemoryTestHarness();
        var saga = harness.StateMachineSaga<OrderState, OrderStateMachine>(orderStateMachine);

        await harness.Start();

        try
        {
            var orderId = InVar.Id;
            await harness.Bus.Publish<OrderSubmitted>(new
            {
                OrderId = orderId,
                Timestamp = InVar.Timestamp,
                CustomerNumber = "12345"
            });

            var isCreated = await saga.Created.Any(o => o.CorrelationId == orderId);
            isCreated.Should().BeTrue();

            var instanceId = await saga.Exists(orderId, o => o.SubmittedState);
            instanceId.Should().NotBeNull();
            
            var instance = saga.Created.Contains(instanceId.Value);
            instance.CustomerNumber.Should().Be("12345");
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
    public async Task Check_Order_Status_Returns_Response()
    {
        var orderStateMachine = new OrderStateMachine();

        var harness = new InMemoryTestHarness();
        var saga = harness.StateMachineSaga<OrderState, OrderStateMachine>(orderStateMachine);

        await harness.Start();

        try
        {
            var orderId = InVar.Id;
            await harness.Bus.Publish<OrderSubmitted>(new
            {
                OrderId = orderId,
                Timestamp = InVar.Timestamp,
                CustomerNumber = "12345"
            });

            var isCreated = await saga.Created.Any(o => o.CorrelationId == orderId);
            isCreated.Should().BeTrue();

            var instanceId = await saga.Exists(orderId, o => o.SubmittedState);
            instanceId.Should().NotBeNull();

            var requestClient = await harness.ConnectRequestClient<CheckOrder>();
            var response = await requestClient.GetResponse<OrderStatus>(new { OrderId = orderId });
            response.Message.State.Should().Be(orderStateMachine.SubmittedState.Name);
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