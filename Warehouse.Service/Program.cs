﻿using System.Diagnostics;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Warehouse.Components;

namespace Warehouse.Service;

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
                services.TryAddSingleton(KebabCaseEndpointNameFormatter.Instance);
                services.AddMassTransit(cfg =>
                {
                    cfg.AddConsumersFromNamespaceContaining<AllocateInventoryConsumer>();
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