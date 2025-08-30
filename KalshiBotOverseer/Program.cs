// Modified Program.cs
using KalshiBotAPI.KalshiAPI;
using KalshiBotAPI.Websockets;
using KalshiBotAPI.WebSockets.Interfaces;
using KalshiBotOverseer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmokehouseBot.KalshiAPI.Interfaces;
using SmokehouseBot.Management.Interfaces;
using SmokehouseBot.Services;
using SmokehouseBot.Services.Interfaces;
using SmokehouseDTOs;
using System;
using System.Threading;

class Program
{
    static async Task Main(string[] args)
    {
        // Set up dependency injection
        var services = new ServiceCollection();

        // Register required services 
        services.AddScoped<IKalshiAPIService, KalshiAPIService>(); 
        services.AddScoped<IKalshiWebSocketClient, KalshiWebSocketClient>(); 

        // Register the new WebSocketMonitorServiceLite as singleton
        services.AddSingleton<IWebSocketMonitorService, WebSocketMonitorServiceLite>();

        // Register the new EventSubscriber as singleton
        services.AddSingleton<Overseer>();

        var serviceProvider = services.BuildServiceProvider();

        // Get the monitor service and start it
        var monitorService = serviceProvider.GetRequiredService<IWebSocketMonitorService>();
        var cancellationTokenSource = new CancellationTokenSource();
        monitorService.StartServices(cancellationTokenSource.Token);

        // Get the event subscriber and start it (subscribes to events)
        var eventSubscriber = serviceProvider.GetRequiredService<Overseer>();
        eventSubscriber.Start();

        Console.WriteLine("Services started. Press any key to stop...");
        Console.ReadKey();

        // Stop services on shutdown
        await monitorService.StopServicesAsync(cancellationTokenSource.Token);
    }
}
