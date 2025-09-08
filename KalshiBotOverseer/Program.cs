// Updated Program.cs with IConfiguration registration
using KalshiBotAPI.KalshiAPI;
using KalshiBotAPI.Websockets;
using KalshiBotAPI.WebSockets.Interfaces;
using KalshiBotOverseer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmokehouseBot.KalshiAPI.Interfaces;
using SmokehouseBot.Services;
using SmokehouseBot.Services.Interfaces;
using SmokehouseBot.State.Interfaces;

class Program
{
    static async Task Main(string[] args)
    {
        // Set up configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
            .AddCommandLine(args)
            .Build();

        // Set up dependency injection
        var services = new ServiceCollection();

        // Add configuration as singleton
        services.AddSingleton<IConfiguration>(configuration);

        // Add logging services
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Register required services
        services.AddScoped<IKalshiAPIService, KalshiAPIService>();
        services.AddScoped<IKalshiWebSocketClient, KalshiWebSocketClient>();
        services.AddSingleton<IBotReadyStatus, IBotReadyStatus>();

        // Register the new WebSocketMonitorServiceLite as singleton
        services.AddSingleton<IWebSocketMonitorService, WebSocketMonitorServiceLite>();

        // Register the new EventSubscriber as singleton
        services.AddSingleton<Overseer>();

        var serviceProvider = services.BuildServiceProvider();

        // Get the monitor service and start it
        var monitorService = serviceProvider.GetRequiredService<IWebSocketMonitorService>();
        var cancellationTokenSource = new CancellationTokenSource();

        try
        {
            monitorService.StartServices(cancellationTokenSource.Token);

            // Get the overseer and start it (subscribes to events)
            var eventSubscriber = serviceProvider.GetRequiredService<Overseer>();
            eventSubscriber.Start();

            Console.WriteLine("Services started. Press any key to stop...");
            Console.ReadKey();

            // Stop overseer
            eventSubscriber.Stop();
        }
        finally
        {
            // Stop services on shutdown
            await monitorService.StopServicesAsync(cancellationTokenSource.Token);
        }
    }
}