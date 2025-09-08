// Updated Program.cs with IConfiguration registration
using KalshiBotAPI.Configuration;
using KalshiBotAPI.KalshiAPI;
using KalshiBotAPI.Websockets;
using KalshiBotAPI.WebSockets.Interfaces;
using KalshiBotData.Data;
using KalshiBotData.Data.Interfaces;
using KalshiBotOverseer;
using KalshiBotOverseer.State;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmokehouseBot.KalshiAPI.Interfaces;
using SmokehouseBot.Services;
using SmokehouseBot.Services.Interfaces;
using SmokehouseBot.State.Interfaces;

class Program
{
    static async Task Main(string[] args)
    {
        // Set up configuration
        var currentDir = Directory.GetCurrentDirectory();
        Console.WriteLine($"Current working directory: {currentDir}");

        // Check if appsettings files exist
        var appsettingsPath = Path.Combine(currentDir, "appsettings.json");
        var localAppsettingsPath = Path.Combine(currentDir, "appsettings.local.json");
        Console.WriteLine($"appsettings.json exists: {File.Exists(appsettingsPath)}");
        Console.WriteLine($"appsettings.local.json exists: {File.Exists(localAppsettingsPath)}");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(currentDir)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
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
        services.AddScoped<IKalshiWebSocketClient>(sp => new KalshiWebSocketClient(
            sp.GetRequiredService<IOptions<KalshiConfig>>(),
            sp.GetRequiredService<ILogger<IKalshiWebSocketClient>>(),
            sp.GetRequiredService<IStatusTrackerService>(),
            sp.GetRequiredService<IBotReadyStatus>(),
            sp.GetRequiredService<ISqlDataService>(),
            false
            ));
        services.AddScoped<ISqlDataService, SqlDataService>();
        services.AddDbContext<KalshiBotContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IKalshiBotContext>(provider => provider.GetService<KalshiBotContext>());
        services.AddSingleton<IStatusTrackerService, OverseerStatusTracker>();
        services.AddSingleton<IBotReadyStatus, OverseerReadyStatus>();

        // Register the new WebSocketMonitorServiceLite as singleton
        services.AddSingleton<IWebSocketMonitorService, WebSocketMonitorServiceLite>();

        // Configure Kalshi settings with fallback
        services.Configure<KalshiConfig>(configuration.GetSection("Kalshi"));

        // Optional: Manual override if configuration binding fails
        services.PostConfigure<KalshiConfig>(config =>
        {
            if (string.IsNullOrEmpty(config.KeyId) || string.IsNullOrEmpty(config.KeyFile))
            {
                Console.WriteLine("Warning: KalshiConfig not properly bound, attempting manual override...");

                // Try to read from local file directly
                var localConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.local.json");
                if (File.Exists(localConfigPath))
                {
                    try
                    {
                        var localJson = File.ReadAllText(localConfigPath);
                        var localConfig = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(localJson);
                        if (localConfig.TryGetProperty("Kalshi", out var kalshiElement))
                        {
                            if (kalshiElement.TryGetProperty("Environment", out var env)) config.Environment = env.GetString() ?? "prd";
                            if (kalshiElement.TryGetProperty("KeyId", out var keyId)) config.KeyId = keyId.GetString() ?? "";
                            if (kalshiElement.TryGetProperty("KeyFile", out var keyFile)) config.KeyFile = keyFile.GetString() ?? "";
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to read local config: {ex.Message}");
                    }
                }
            }
        });

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