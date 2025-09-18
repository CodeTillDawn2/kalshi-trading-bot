using KalshiBotAPI.Configuration;
using KalshiBotAPI.KalshiAPI;
using KalshiBotAPI.Websockets;
using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.Extensions.Logging;
using BacklashBotData.Data;
using BacklashBotData.Data.Interfaces;
using BacklashBotData.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using BacklashDTOs.Configuration;
using BacklashBot.Configuration;
using BacklashBot.Services;
using BacklashBot.Hubs;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Management;
using BacklashBot.Management.Interfaces;
using BacklashBot.Middleware;
using BacklashBot.Services.Interfaces;
using BacklashBot.State;
using BacklashBot.State.Interfaces;
using BacklashInterfaces.SmokehouseBot.Services;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashDTOs.Data;
using TradingStrategies.Classification;
using TradingStrategies.Classification.Interfaces;
using TradingStrategies.Configuration;
using TradingStrategies.Helpers.Interfaces;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using BacklashCommon.Services;
using BacklashInterfaces.PerformanceMetrics;
using KalshiBotLogging;
using BacklashCommon.Helpers;

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
builder.Services.AddSingleton<DatabaseLoggingQueue>(provider => new DatabaseLoggingQueue(provider.GetService<IServiceProvider>(), false)); // isOverseer = false
builder.Services.AddHostedService(provider => provider.GetRequiredService<DatabaseLoggingQueue>());

// ## Configuration Setup
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddCommandLine(args);

// Bind configurations to respective models
builder.Services.Configure<LoggingConfig>(builder.Configuration.GetSection("Communications:Logging"));
builder.Services.Configure<KalshiConfig>(builder.Configuration.GetSection("Kalshi"));
builder.Services.Configure<KalshiAPIServiceConfig>(builder.Configuration.GetSection("API"));
builder.Services.Configure<WebSocketConnectionManagerConfig>(builder.Configuration.GetSection("Websockets:WebSocketConnectionManager"));
builder.Services.Configure<MessageProcessorConfig>(builder.Configuration.GetSection("Websockets:MessageProcessor"));
builder.Services.Configure<SubscriptionManagerConfig>(builder.Configuration.GetSection("Websockets:SubscriptionManager"));
builder.Services.Configure<WebSocketMonitorConfig>(builder.Configuration.GetSection("Websockets:WebSocketMonitor"));
builder.Services.Configure<KalshiWebSocketClientConfig>(builder.Configuration.GetSection("Websockets:KalshiWebSocketClient"));
builder.Services.Configure<TradingSnapshotServiceConfig>(builder.Configuration.GetSection("WatchedMarkets:TradingSnapshotService"));
builder.Services.Configure<SnapshotPeriodHelperConfig>(builder.Configuration.GetSection("WatchedMarkets:SnapshotPeriodHelper"));
builder.Services.Configure<OrderbookChangeTrackerConfig>(builder.Configuration.GetSection("WatchedMarkets:OrderbookChangeTracker"));
builder.Services.Configure<MarketRefreshServiceConfig>(builder.Configuration.GetSection("WatchedMarkets:MarketRefreshService"));
builder.Services.Configure<PseudoCandlestickExtensionsConfig>(builder.Configuration.GetSection("WatchedMarkets:PseudoCandlestickExtensions"));
builder.Services.Configure<GeneralExecutionConfig>(builder.Configuration.GetSection("Central:GeneralExecution"));
builder.Services.Configure<OverseerClientServiceConfig>(builder.Configuration.GetSection("Communications:OverseerClientService"));
builder.Services.Configure<CandlestickServiceConfig>(builder.Configuration.GetSection("WatchedMarkets:CandlestickService"));
builder.Services.Configure<BroadcastServiceConfig>(builder.Configuration.GetSection("Communications:BroadcastService"));
builder.Services.Configure<MarketDataInitializerConfig>(builder.Configuration.GetSection("WatchedMarkets:MarketDataInitializer"));
builder.Services.Configure<CentralPerformanceMonitorConfig>(builder.Configuration.GetSection("Central:CentralPerformanceMonitor"));
builder.Services.Configure<KalshiBotScopeManagerServiceConfig>(builder.Configuration.GetSection("Central:KalshiBotScopeManagerService"));
builder.Services.Configure<MarketDataConfig>(builder.Configuration.GetSection("WatchedMarkets:MarketData"));
builder.Services.Configure<CentralBrainConfig>(builder.Configuration.GetSection("Central:CentralBrain"));
builder.Services.Configure<TargetCalculationServiceConfig>(builder.Configuration.GetSection("WatchedMarkets:TargetCalculationService"));
builder.Services.Configure<BrainStatusServiceConfig>(builder.Configuration.GetSection("Central:BrainStatusService"));
builder.Services.Configure<SnapshotGroupHelperConfig>(builder.Configuration.GetSection("SnapshotGroupHelper"));
builder.Services.Configure<QueueMonitoringConfig>(builder.Configuration.GetSection("Central:CentralPerformanceMonitor"));
builder.Services.Configure<InterestScoreConfig>(builder.Configuration.GetSection("WatchedMarkets:InterestScore"));
builder.Services.Configure<ErrorHandlerConfig>(builder.Configuration.GetSection("Central:ErrorHandler"));
builder.Services.Configure<OrderBookServiceConfig>(builder.Configuration.GetSection("WatchedMarkets:OrderBookService"));
builder.Services.Configure<BacklashBot.State.CalculationConfig>(builder.Configuration.GetSection("WatchedMarkets:CalculationConfig"));

// Increase shutdown timeout
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});

// ## Service Registrations
builder.Services.AddSingleton<IServiceFactory, ServiceFactory>();
builder.Services.AddSingleton<ICentralBrain>(sp => new CentralBrain(
    sp.GetRequiredService<ILogger<ICentralBrain>>(),
    sp.GetRequiredService<IServiceFactory>(),
    sp.GetRequiredService<IServiceScopeFactory>(),
    sp.GetRequiredService<IOptions<TradingSnapshotServiceConfig>>(),
    sp.GetRequiredService<IOptions<GeneralExecutionConfig>>(),
    sp.GetRequiredService<IOptions<GeneralExecutionConfig>>(),
    sp.GetRequiredService<ICentralErrorHandler>(),
    sp.GetRequiredService<ICentralPerformanceMonitor>(),
    sp.GetRequiredService<IMarketManagerService>(),
    sp.GetRequiredService<IHostApplicationLifetime>(),
    sp.GetRequiredService<IScopeManagerService>(),
    sp.GetRequiredService<IStatusTrackerService>(),
    sp.GetRequiredService<IBotReadyStatus>(),
    sp.GetRequiredService<IBrainStatusService>(),
    sp.GetRequiredService<IOptions<CentralBrainConfig>>(),
    sp.GetRequiredService<IHealthCheckService>(),
    sp.GetRequiredService<Func<BacklashInterfaces.SmokehouseBot.Timers.ITimer>>()));
builder.Services.AddSingleton<ICentralErrorHandler, CentralErrorHandler>();
builder.Services.AddSingleton<IScopeManagerService, KalshiBotScopeManagerService>();
builder.Services.AddSingleton<ICentralPerformanceMonitor>(sp => new CentralPerformanceMonitor(
    sp.GetRequiredService<ILogger<ICentralPerformanceMonitor>>(),
    sp.GetRequiredService<IServiceFactory>(),
    sp.GetRequiredService<IOptions<GeneralExecutionConfig>>(),
    sp.GetRequiredService<IOptions<QueueMonitoringConfig>>(),
    sp.GetRequiredService<IOptions<CentralPerformanceMonitorConfig>>(),
    sp.GetRequiredService<IOptions<GeneralExecutionConfig>>(),
    sp.GetRequiredService<IServiceScopeFactory>(),
    sp.GetRequiredService<IScopeManagerService>(),
    sp.GetRequiredService<IStatusTrackerService>()));
builder.Services.AddSingleton<INightActivitiesPerformanceMetrics>(provider =>
    (INightActivitiesPerformanceMetrics)provider.GetRequiredService<ICentralPerformanceMonitor>());
builder.Services.AddSingleton<IWebSocketPerformanceMetrics>(provider =>
    (IWebSocketPerformanceMetrics)provider.GetRequiredService<ICentralPerformanceMonitor>());
builder.Services.AddSingleton<IMarketManagerService>(sp => new MarketManagerService(
    sp.GetRequiredService<IServiceFactory>(),
    sp.GetRequiredService<ILogger<IMarketManagerService>>(),
    sp.GetRequiredService<IServiceScopeFactory>(),
    sp.GetRequiredService<ICentralPerformanceMonitor>(),
    sp.GetRequiredService<IOptions<GeneralExecutionConfig>>(),
    sp.GetRequiredService<IOptions<CentralBrainConfig>>(),
    sp.GetRequiredService<IScopeManagerService>(),
    sp.GetRequiredService<IStatusTrackerService>(),
    sp.GetRequiredService<IBrainStatusService>(),
    sp.GetRequiredService<ITargetCalculationService>()));
builder.Services.AddSingleton<IStatusTrackerService, KalshiBotStatusTracker>();
builder.Services.AddSingleton<IBotReadyStatus, KalshiBotReadyStatus>();
builder.Services.AddSingleton<IBrainStatusService, BrainStatusService>();
builder.Services.AddSingleton<ITargetCalculationService, TargetCalculationService>();
builder.Services.AddTransient<BacklashInterfaces.SmokehouseBot.Timers.ITimer, BacklashBot.Timers.SystemTimer>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<ICentralBrain>());

// Register the custom logger provider with DI
builder.Services.AddSingleton<ILoggerProvider>(provider =>
{
    var loggingConfig = provider.GetRequiredService<IOptions<LoggingConfig>>().Value;
    var minLevel = LogLevel.Warning; // Default
    if (!string.IsNullOrEmpty(loggingConfig.LogLevel?.SqlDatabaseLogLevel))
    {
        try
        {
            minLevel = Enum.Parse<LogLevel>(loggingConfig.LogLevel.SqlDatabaseLogLevel, true);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Invalid SqlDatabaseLogLevel '{loggingConfig.LogLevel.SqlDatabaseLogLevel}' in LoggingConfig. Using default Warning level. Error: {ex.Message}");
        }
    }

    return new DatabaseLoggerProvider(
        provider.GetRequiredService<DatabaseLoggingQueue>(),
        minLevel,
        loggingConfig,
        null, // IBrainStatusService - avoid circular dependency
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
        provider.GetRequiredService<IOptions<OrderbookChangeTrackerConfig>>(),
        provider.GetRequiredService<IScopeManagerService>(),
        provider.GetRequiredService<IStatusTrackerService>(),
        provider.GetRequiredService<ICentralPerformanceMonitor>()
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
                sp.GetRequiredService<IOptions<OrderbookChangeTrackerConfig>>(),
                sp.GetRequiredService<IScopeManagerService>(),
                sp.GetRequiredService<IStatusTrackerService>(),
                sp.GetRequiredService<ICentralPerformanceMonitor>()
            ),
            sp.GetRequiredService<IOptions<BacklashBot.State.CalculationConfig>>()
        );
    };
});

// Database context
builder.Services.AddDbContext<BacklashBotContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IBacklashBotContext>(provider => provider.GetRequiredService<BacklashBotContext>());
builder.Services.AddScoped<IKalshiAPIService>(sp => new KalshiAPIService(
    sp.GetRequiredService<ILogger<IKalshiAPIService>>(),
    sp.GetRequiredService<IConfiguration>(),
    sp.GetRequiredService<IServiceScopeFactory>(),
    sp.GetRequiredService<IStatusTrackerService>(),
    sp.GetRequiredService<IOptions<KalshiConfig>>(),
    sp.GetRequiredService<IOptions<KalshiAPIServiceConfig>>(),
    sp.GetRequiredService<IPerformanceMonitor>()
));

// Register services as scoped
builder.Services.AddScoped<ISqlDataService, KalshiBotData.Data.SqlDataService>();
builder.Services.AddScoped<ITradingSnapshotService, TradingSnapshotService>();
builder.Services.AddScoped<IMarketDataService, MarketDataService>();
builder.Services.AddScoped<ITradingCalculator, TradingCalculator>();
builder.Services.AddScoped<ISnapshotGroupHelper, SnapshotGroupHelper>();
builder.Services.AddScoped<IOrderBookService>(provider =>
    new OrderBookService(
        provider.GetRequiredService<ILogger<IOrderBookService>>(),
        provider.GetRequiredService<IServiceScopeFactory>(),
        provider.GetRequiredService<IServiceFactory>(),
        provider.GetRequiredService<IScopeManagerService>(),
        provider.GetRequiredService<IStatusTrackerService>(),
        provider.GetRequiredService<IOptions<OrderBookServiceConfig>>().Value
    ));
builder.Services.AddScoped<IMarketDataInitializer, MarketDataInitializer>();
builder.Services.AddScoped<ICandlestickService>(sp => new CandlestickService(
    sp.GetRequiredService<ILogger<ICandlestickService>>(),
    sp.GetRequiredService<IServiceScopeFactory>(),
    sp.GetRequiredService<IStatusTrackerService>(),
    sp.GetRequiredService<IOptions<CandlestickServiceConfig>>(),
    sp.GetRequiredService<IOptions<CentralBrainConfig>>(),
    sp.GetRequiredService<IOptions<LoggingConfig>>(),
    sp.GetRequiredService<IServiceFactory>(),
    sp.GetRequiredService<IScopeManagerService>()));
builder.Services.AddScoped<IBroadcastService>(sp => new BroadcastService(
    sp.GetRequiredService<IHubContext<BacklashBotHub>>(),
    sp.GetRequiredService<IServiceFactory>(),
    sp.GetRequiredService<IStatusTrackerService>(),
    sp.GetRequiredService<IServiceScopeFactory>(),
    sp.GetRequiredService<ILogger<IBroadcastService>>(),
    sp.GetRequiredService<IScopeManagerService>(),
    sp.GetRequiredService<IConfiguration>(),
    sp.GetRequiredService<ICentralPerformanceMonitor>(),
    sp.GetRequiredService<IOptions<BacklashBot.Configuration.BroadcastServiceConfig>>()));
builder.Services.AddScoped<IMarketRefreshService, MarketRefreshService>();
builder.Services.AddScoped<IWebSocketMonitorService>(sp => new WebSocketMonitorService(
    sp.GetRequiredService<IServiceScopeFactory>(),
    sp.GetRequiredService<IServiceFactory>(),
    sp.GetRequiredService<ILogger<IWebSocketMonitorService>>(),
    sp.GetRequiredService<IConfiguration>(),
    sp.GetRequiredService<IScopeManagerService>(),
    sp.GetRequiredService<IBotReadyStatus>(),
    sp.GetRequiredService<IStatusTrackerService>(),
    sp.GetRequiredService<ICentralPerformanceMonitor>()));
builder.Services.AddSingleton<IOverseerClientService>(sp => new OverseerClientService(
    sp.GetRequiredService<ILogger<OverseerClientService>>(),
    sp.GetRequiredService<IServiceFactory>(),
    sp.GetRequiredService<IOptions<OverseerClientServiceConfig>>(),
    sp.GetRequiredService<IOptions<GeneralExecutionConfig>>(),
    sp.GetRequiredService<ICentralPerformanceMonitor>()));
builder.Services.AddScoped<IWebSocketConnectionManager>(sp => new WebSocketConnectionManager(
    sp.GetRequiredService<IOptions<KalshiConfig>>(),
    sp.GetRequiredService<IOptions<WebSocketConnectionManagerConfig>>(),
    sp.GetRequiredService<ILogger<WebSocketConnectionManager>>(),
    sp.GetRequiredService<ICentralPerformanceMonitor>()
));
builder.Services.AddScoped<IMessageProcessor>(sp => new MessageProcessor(
    sp.GetRequiredService<ILogger<MessageProcessor>>(),
    sp.GetRequiredService<IWebSocketConnectionManager>(),
    sp.GetRequiredService<ISubscriptionManager>(),
    sp.GetRequiredService<IStatusTrackerService>(),
    sp.GetRequiredService<ISqlDataService>(),
    sp.GetRequiredService<IKalshiAPIService>(),
    sp.GetRequiredService<IOptions<MessageProcessorConfig>>().Value,
    sp.GetRequiredService<IMessageProcessorPerformanceMetrics>()
));
builder.Services.AddScoped<ISubscriptionManager>(sp => new SubscriptionManager(
    sp.GetRequiredService<ILogger<SubscriptionManager>>(),
    sp.GetRequiredService<IWebSocketConnectionManager>(),
    sp.GetRequiredService<IDataCache>(),
    sp.GetRequiredService<IStatusTrackerService>(),
    sp.GetRequiredService<IOptions<SubscriptionManagerConfig>>()
));
builder.Services.AddScoped<IKalshiWebSocketClient>(sp => new KalshiWebSocketClient(
    sp.GetRequiredService<IOptions<KalshiConfig>>(),
    sp.GetRequiredService<IOptions<KalshiWebSocketClientConfig>>(),
    sp.GetRequiredService<ILogger<IKalshiWebSocketClient>>(),
    sp.GetRequiredService<IStatusTrackerService>(),
    sp.GetRequiredService<IBotReadyStatus>(),
    sp.GetRequiredService<ISqlDataService>(),
    sp.GetRequiredService<IWebSocketConnectionManager>(),
    sp.GetRequiredService<ISubscriptionManager>(),
    sp.GetRequiredService<IMessageProcessor>(),
    sp.GetRequiredService<IDataCache>(),
    sp.GetRequiredService<IWebSocketPerformanceMetrics>(),
    sp.GetRequiredService<IOptions<LoggingConfig>>().Value.StoreWebSocketEvents,
    sp.GetRequiredService<IOptions<WebSocketConnectionManagerConfig>>().Value.BufferSize,
    sp.GetRequiredService<IOptions<WebSocketConnectionManagerConfig>>().Value.EnablePerformanceMetrics ?? true
));
builder.Services.AddScoped<IInterestScoreService, InterestScoreService>();
builder.Services.AddScoped<IOvernightActivitiesHelper>(provider =>
    new OvernightActivitiesHelper(
        provider.GetRequiredService<ILogger<BacklashCommon.Services.OvernightActivitiesHelper>>(),
        provider.GetRequiredService<IInterestScoreService>(),
        provider.GetRequiredService<ISnapshotGroupHelper>(),
        provider.GetRequiredService<IOptions<GeneralExecutionConfig>>(),
        provider.GetRequiredService<ISqlDataService>(),
        provider.GetRequiredService<INightActivitiesPerformanceMetrics>()));
builder.Services.AddScoped<ISnapshotPeriodHelper>(provider =>
    new SnapshotPeriodHelper(provider.GetRequiredService<IOptions<SnapshotPeriodHelperConfig>>().Value));
builder.Services.AddScoped<BacklashInterfaces.SmokehouseBot.Services.IHealthCheckService, HealthCheckService>();
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

var loggingConfig = builder.Configuration.GetSection("Communications:Logging").Get<LoggingConfig>();
if (!string.IsNullOrEmpty(loggingConfig?.LogLevel?.ConsoleLogLevel))
{
    try
    {
        var consoleLogLevel = Enum.Parse<LogLevel>(loggingConfig.LogLevel.ConsoleLogLevel, true);
        builder.Logging.SetMinimumLevel(consoleLogLevel);
    }
    catch (ArgumentException ex)
    {
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        Console.WriteLine($"Invalid ConsoleLogLevel '{loggingConfig.LogLevel.ConsoleLogLevel}' in LoggingConfig. Falling back to Information. Error: {ex.Message}");
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

app.MapHub<BacklashBotHub>("/chartHub");
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
