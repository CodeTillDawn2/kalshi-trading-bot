using KalshiBotAPI.Configuration;
using KalshiBotAPI.KalshiAPI;
using KalshiBotAPI.Websockets;
using KalshiBotAPI.WebSockets.Interfaces;
using KalshiBotData.Data;
using KalshiBotData.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using BacklashDTOs.Configuration;
using BacklashBot.Hubs;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Management;
using BacklashBot.Management.Interfaces;
using BacklashBot.Middleware;
using BacklashBot.Services;
using BacklashBot.Services.Interfaces;
using BacklashBot.State;
using BacklashBot.State.Interfaces;
using BacklashInterfaces.KalshiBotOverseer.State;
using BacklashInterfaces.SmokehouseBot.Services;
using BacklashDTOs.Data;
using TradingStrategies.Classification;
using TradingStrategies.Classification.Interfaces;
using TradingStrategies.Configuration;
using TradingStrategies.Helpers.Interfaces;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using KalshiBotLogging;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine($"Application starting at {DateTime.UtcNow}");

// ## Logging Configuration
builder.Logging.ClearProviders();
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.StaticFiles", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Routing", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);

// Register ILoggerFactory
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();

// Register DatabaseLoggingQueue as a singleton and hosted service
builder.Services.AddSingleton<DatabaseLoggingQueue>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<DatabaseLoggingQueue>());

// ## Configuration Setup
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddCommandLine(args);

// Bind configurations to respective models
builder.Services.Configure<LoggingConfig>(builder.Configuration.GetSection("Logging"));
builder.Services.Configure<KalshiConfig>(builder.Configuration.GetSection("Kalshi"));
builder.Services.Configure<SnapshotConfig>(builder.Configuration.GetSection("Snapshots"));
builder.Services.Configure<TradingConfig>(builder.Configuration.GetSection("TradingConfig"));
builder.Services.Configure<CalculationConfig>(builder.Configuration.GetSection("CalculationConfig"));
builder.Services.Configure<ExecutionConfig>(builder.Configuration.GetSection("Execution"));

// Increase shutdown timeout
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});

// ## Service Registrations
builder.Services.AddSingleton<IServiceFactory, ServiceFactory>();
builder.Services.AddSingleton<ICentralBrain, CentralBrain>();
builder.Services.AddSingleton<ICentralErrorHandler, CentralErrorHandler>();
builder.Services.AddSingleton<IScopeManagerService, KaslhiBotScopeManagerService>();
builder.Services.AddSingleton<ICentralPerformanceMonitor, CentralPerformanceMonitor>();
builder.Services.AddSingleton<IMarketManagerService, MarketManagerService>();
builder.Services.AddSingleton<IStatusTrackerService, KalshiBotStatusTracker>();
builder.Services.AddSingleton<IBotReadyStatus, KalshiBotReadyStatus>();
builder.Services.AddSingleton<IBrainStatusService, BrainStatusService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<ICentralBrain>());

// Register the custom logger provider with DI
builder.Services.AddSingleton<ILoggerProvider>(provider =>
{
    var loggingConfig = provider.GetRequiredService<IOptions<LoggingConfig>>().Value;
    var minLevel = LogLevel.Warning; // Default
    if (!string.IsNullOrEmpty(loggingConfig.SqlDatabaseLogLevel))
    {
        try
        {
            minLevel = Enum.Parse<LogLevel>(loggingConfig.SqlDatabaseLogLevel, true);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Invalid SqlDatabaseLogLevel '{loggingConfig.SqlDatabaseLogLevel}' in LoggingConfig. Using default Warning level. Error: {ex.Message}");
        }
    }

    return new DatabaseLoggerProvider(
        provider.GetRequiredService<DatabaseLoggingQueue>(),
        minLevel,
        loggingConfig,
        provider.GetRequiredService<IOptions<ExecutionConfig>>().Value,
        provider.GetRequiredService<IBrainStatusService>(),
        "BacklashBot", // defaultEnvironment
        "BacklashInstance"); // defaultInstance
});

// Register OrderbookChangeTracker with transient lifetime
builder.Services.AddTransient(provider =>
{
    var dataCache = provider.GetRequiredService<IServiceFactory>().GetDataCache();
    if (dataCache == null) throw new InvalidOperationException("DataCache is null");
    return new OrderbookChangeTracker(
        "unknown",
        provider.GetRequiredService<ILogger<OrderbookChangeTracker>>(),
        dataCache,
        provider.GetRequiredService<IOptions<TradingConfig>>(),
        provider.GetRequiredService<IScopeManagerService>(),
        provider.GetRequiredService<IStatusTrackerService>()
    );
});

// Register MarketData factory
builder.Services.AddTransient<Func<MarketDTO, MarketData>>(provider =>
{
    var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
    return (market) =>
    {
        using var scope = scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        var tradingCalculator = sp.GetRequiredService<IServiceFactory>().GetTradingCalculator();
        var dataCache = sp.GetRequiredService<IServiceFactory>().GetDataCache();
        if (tradingCalculator == null || dataCache == null) throw new InvalidOperationException("TradingCalculator or DataCache is null");
        return new MarketData(
            market,
            sp.GetRequiredService<ILogger<MarketData>>(),
            tradingCalculator,
            new OrderbookChangeTracker(
                market.market_ticker,
                sp.GetRequiredService<ILogger<OrderbookChangeTracker>>(),
                dataCache,
                sp.GetRequiredService<IOptions<TradingConfig>>(),
                sp.GetRequiredService<IScopeManagerService>(),
                sp.GetRequiredService<IStatusTrackerService>()
            ),
            sp.GetRequiredService<IOptions<CalculationConfig>>()
        );
    };
});

// Database context
builder.Services.AddDbContext<KalshiBotContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IKalshiBotContext>(provider => provider.GetRequiredService<KalshiBotContext>());
builder.Services.AddScoped<IKalshiAPIService, KalshiAPIService>();

// Register services as scoped
builder.Services.AddScoped<ISqlDataService, SqlDataService>();
builder.Services.AddScoped<ITradingSnapshotService, TradingSnapshotService>();
builder.Services.AddScoped<IMarketDataService, MarketDataService>();
builder.Services.AddScoped<ITradingCalculator, TradingCalculator>();
builder.Services.AddScoped<IMarketAnalysisHelper, MarketAnalysisHelper>();
builder.Services.AddScoped<IOrderBookService, OrderBookService>();
builder.Services.AddScoped<IMarketDataInitializer, MarketDataInitializer>();
builder.Services.AddScoped<ICandlestickService, CandlestickService>();
builder.Services.AddScoped<IBroadcastService, BroadcastService>();
builder.Services.AddScoped<IMarketRefreshService, MarketRefreshService>();
builder.Services.AddScoped<IWebSocketMonitorService, WebSocketMonitorService>();
builder.Services.AddSingleton<IOverseerClientService, OverseerClientService>();
builder.Services.AddScoped<IWebSocketConnectionManager, WebSocketConnectionManager>();
builder.Services.AddScoped<IMessageProcessor, MessageProcessor>();
builder.Services.AddScoped<ISubscriptionManager>(sp => new SubscriptionManager(
    sp.GetRequiredService<ILogger<SubscriptionManager>>(),
    sp.GetRequiredService<IWebSocketConnectionManager>(),
    sp.GetRequiredService<IDataCache>(),
    sp.GetRequiredService<IStatusTrackerService>()
));
builder.Services.AddScoped<IKalshiWebSocketClient>(sp => new KalshiWebSocketClient(
    sp.GetRequiredService<IOptions<KalshiConfig>>(),
    sp.GetRequiredService<ILogger<IKalshiWebSocketClient>>(),
    sp.GetRequiredService<IStatusTrackerService>(),
    sp.GetRequiredService<IBotReadyStatus>(),
    sp.GetRequiredService<ISqlDataService>(),
    sp.GetRequiredService<IWebSocketConnectionManager>(),
    sp.GetRequiredService<ISubscriptionManager>(),
    sp.GetRequiredService<IMessageProcessor>(),
    sp.GetRequiredService<IDataCache>(),
    sp.GetRequiredService<IOptions<LoggingConfig>>().Value.StoreWebSocketEvents
));
builder.Services.AddScoped<IInterestScoreService, InterestScoreService>();
builder.Services.AddScoped<IOvernightActivitiesHelper, OvernightActivitiesHelper>();
builder.Services.AddScoped<ISnapshotPeriodHelper, SnapshotPeriodHelper>();
builder.Services.AddScoped<IDataCache, BacklashBot.State.DataCache>();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// SignalR configuration
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
});

// Configure Kestrel for IIS (no specific URL binding)
builder.WebHost.ConfigureKestrel(options => { });

var loggingConfig = builder.Configuration.GetSection("Logging").Get<LoggingConfig>();
if (!string.IsNullOrEmpty(loggingConfig?.ConsoleLogLevel))
{
    try
    {
        var consoleLogLevel = Enum.Parse<LogLevel>(loggingConfig.ConsoleLogLevel, true);
        builder.Logging.SetMinimumLevel(consoleLogLevel);
    }
    catch (ArgumentException ex)
    {
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        Console.WriteLine($"Invalid ConsoleLogLevel '{loggingConfig.ConsoleLogLevel}' in LoggingConfig. Falling back to Information. Error: {ex.Message}");
    }
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

Console.WriteLine($"Building application at {DateTime.UtcNow}");
var app = builder.Build();
Console.WriteLine($"Application built at {DateTime.UtcNow}");

// Validate DI container
try
{
    using (var scope = app.Services.CreateScope())
    {
        scope.ServiceProvider.GetRequiredService<IServiceProvider>();
        Console.WriteLine($"DI container validated successfully at {DateTime.UtcNow}");
    }
}
catch (Exception ex)
{
    Console.WriteLine("DI container validation failed: {0}", ex.Message);
    throw;
}

// Configure LoggerExtensions
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    Console.WriteLine($"LoggerExtensions configured at {DateTime.UtcNow}");
}

// Register shutdown hook
app.Lifetime.ApplicationStopping.Register(() =>
{
    var brain = app.Services.GetRequiredService<ICentralBrain>();
    brain.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
});

// ## HTTP Pipeline Configuration
app.UseRouting();
app.UseAuthorization();

app.MapHub<ChartHub>("/chartHub");
app.MapGet("/shutdown-services", async (ICentralBrain brain, ILogger<Program> logger) =>
{
    try
    {
        if (brain.IsStartingUp)
        {
            return Results.BadRequest("Cannot shut down: services are currently starting up.");
        }
        if (brain.IsShuttingDown)
        {
            return Results.BadRequest("Cannot shut down: services are already shutting down.");
        }

        logger.LogInformation("Shutdown services requested at {0}", DateTime.UtcNow);

        await brain.ShutdownDashboardAsync();
        return Results.Ok("Services shut down successfully. Application is stopping.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error shutting down services");
        return Results.Problem("Error shutting down services.");
    }
});
app.MapGet("/restart-services", async (ICentralBrain brain) =>
{
    try
    {
        if (brain.IsStartingUp)
        {
            return Results.BadRequest("Cannot restart: services are already starting up.");
        }
        if (brain.IsShuttingDown)
        {
            return Results.BadRequest("Cannot restart: services are currently shutting down.");
        }
        if (!brain.IsServicesStopped)
        {
            return Results.BadRequest("Cannot restart: services must be stopped first.");
        }

        await brain.StartDashboard();
        return Results.Ok("Services restarted successfully.");
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error restarting services");
        return Results.Problem("Error restarting services.");
    }
});

// Start the application
Console.WriteLine("Starting application at {0}", DateTime.UtcNow);
using (var scope = app.Services.CreateScope())
{
    bool brainInitialized = false;

    try
    {
        if (!brainInitialized)
        {
            await app.StartAsync();
            brainInitialized = true;
            Console.WriteLine("Application started successfully at {0}", DateTime.UtcNow);
        }
    }
    catch (Exception ex)
    {
        brainInitialized = false;
        Console.WriteLine("Application startup failed: {0}", ex.Message);
        throw;
    }
}

await app.WaitForShutdownAsync();
