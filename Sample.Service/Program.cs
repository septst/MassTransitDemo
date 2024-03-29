﻿using System.Diagnostics;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sample.Components.Consumers;
using Sample.Components.CourierActivities;
using Sample.Components.StateMachines;
using Sample.Components.StateMachines.OrderStateMachineActivities;
using Serilog;
using Serilog.Events;

namespace Sample.Service;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var isService = !(Debugger.IsAttached || args.Contains("--console"));

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        var builder = new HostBuilder()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", true);
                config.AddEnvironmentVariables();

                if (args != null)
                    config.AddCommandLine(args);
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddScoped<AcceptOrderActivity>();
                services.TryAddSingleton(KebabCaseEndpointNameFormatter.Instance);
                services.AddMassTransit(cfg =>
                {
                    cfg.AddConsumersFromNamespaceContaining<SubmitOrderConsumer>();
                    cfg.AddActivitiesFromNamespaceContaining<AllocateInventoryActivity>();
                    cfg.AddSagaStateMachine<OrderStateMachine, OrderState>(typeof(OrderStateMachineDefinition))
                        .MongoDbRepository(m =>
                        {
                            m.Connection = "mongodb://127.0.0.1";
                            m.DatabaseName = "orders";
                        });
                    cfg.UsingRabbitMq((context, mqCfg) => { mqCfg.ConfigureEndpoints(context); });
                });
                services.AddHostedService<MassTransitConsoleHostedService>();
            })
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddSerilog(dispose: true);
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
            });
        
        Log.CloseAndFlush();

        if (isService)
            await builder.UseWindowsService().Build().RunAsync();
        else
            await builder.RunConsoleAsync();
    }
}