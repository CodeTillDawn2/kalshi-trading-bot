using BacklashBot.Configuration;
using BacklashBot.Hubs;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Management;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services;
using BacklashBot.Services.Interfaces;
using BacklashBot.State;
using BacklashBot.State.Interfaces;
using BacklashBotData.Configuration;
using BacklashBotData.Data;
using BacklashBotData.Data.Interfaces;
using BacklashCommon.Configuration;
using BacklashCommon.Helpers;
using BacklashCommon.Services;
using BacklashDTOs.Data;
using BacklashInterfaces.PerformanceMetrics;
using BacklashInterfaces.SmokehouseBot.Services;
using KalshiBotAPI.Configuration;
using KalshiBotAPI.KalshiAPI;
using KalshiBotAPI.Websockets;
using KalshiBotAPI.WebSockets.Interfaces;
using KalshiBotLogging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using TradingStrategies.Classification.Interfaces;
using TradingStrategies.Helpers.Interfaces;

// Local function to generate session identifier
string GenerateSessionIdentifier(int length = 5)
{
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    // Use more bytes for better entropy
    var data = new byte[length + 8]; // Extra 8 bytes for timestamp
    using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
    {
        rng.GetBytes(data);
    }
    // Incorporate timestamp for additional entropy
    var timestamp = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
    for (int i = 0; i < Math.Min(8, data.Length); i++)
    {
        data[i] ^= timestamp[i];
    }
    var result = new char[length];
    for (int i = 0; i < length; i++)
    {
        result[i] = chars[data[i] % chars.Length];
    }
    return new string(result);
}

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine($"Application starting at {DateTime.UtcNow}");

// ## Logging Configuration
builder.Logging.ClearProviders();
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.StaticFiles", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Routing", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);

// Add console logging provider
// builder.Logging.AddConsole(); // Disabled to use only custom formatted logging

// Register ILoggerFactory
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();

// Register DatabaseLoggingQueue as a singleton and hosted service
builder.Services.AddSingleton<DatabaseLoggingQueue>(provider => new DatabaseLoggingQueue(provider.GetService<IServiceProvider>(), false)); // isOverseer = false
builder.Services.AddHostedService(provider => provider.GetRequiredService<DatabaseLoggingQueue>());

// ## Configuration Setup
var baseConfig = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

builder.Configuration
    .AddConfiguration(baseConfig)
    .AddSecretsConfiguration(AppDomain.CurrentDomain.BaseDirectory, baseConfig)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

// Bind configurations to respective models
builder.Services.AddOptions<SecretsConfig>()
    .Bind(builder.Configuration.GetSection(SecretsConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<LoggingConfig>()
    .Bind(builder.Configuration.GetSection(LoggingConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<KalshiConfig>()
    .Bind(builder.Configuration.GetSection(KalshiConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Interpolate placeholders in KalshiConfig
builder.Services.PostConfigure<KalshiConfig>(config =>
{
    // Interpolate KeyId and KeyFile
    var interpolatedKeyId = BacklashCommon.Configuration.ConfigurationHelper.InterpolateConfigurationValue(config.KeyId, builder.Configuration);
    var interpolatedKeyFile = BacklashCommon.Configuration.ConfigurationHelper.InterpolateConfigurationValue(config.KeyFile, builder.Configuration);

    // Resolve the key file path to the secrets directory
    var secretsConfig = builder.Configuration.GetSection(BacklashCommon.Configuration.SecretsConfig.SectionName).Get<BacklashCommon.Configuration.SecretsConfig>();
    var resolvedKeyFile = BacklashCommon.Configuration.ConfigurationHelper.ResolveSecretsFilePath(interpolatedKeyFile, secretsConfig, AppDomain.CurrentDomain.BaseDirectory);

    // Update the config with interpolated values
    config.KeyId = interpolatedKeyId;
    config.KeyFile = resolvedKeyFile;
});
builder.Services.AddOptions<KalshiAPIServiceConfig>()
    .Bind(builder.Configuration.GetSection(KalshiAPIServiceConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<WebSocketConnectionManagerConfig>()
    .Bind(builder.Configuration.GetSection(WebSocketConnectionManagerConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<MessageProcessorConfig>()
    .Bind(builder.Configuration.GetSection(MessageProcessorConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<SubscriptionManagerConfig>()
    .Bind(builder.Configuration.GetSection(SubscriptionManagerConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<WebSocketMonitorConfig>()
    .Bind(builder.Configuration.GetSection(WebSocketMonitorConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<KalshiWebSocketClientConfig>()
    .Bind(builder.Configuration.GetSection(KalshiWebSocketClientConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<WebSocketMonitorServiceConfig>()
    .Bind(builder.Configuration.GetSection(WebSocketMonitorServiceConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<TradingSnapshotServiceConfig>()
    .Bind(builder.Configuration.GetSection(TradingSnapshotServiceConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<OrderbookChangeTrackerConfig>()
    .Bind(builder.Configuration.GetSection(OrderbookChangeTrackerConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<MarketRefreshServiceConfig>()
    .Bind(builder.Configuration.GetSection(MarketRefreshServiceConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<GeneralExecutionConfig>()
    .Bind(builder.Configuration.GetSection(GeneralExecutionConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<InstanceNameConfig>()
    .Bind(builder.Configuration.GetSection(InstanceNameConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<OverseerClientServiceConfig>()
    .Bind(builder.Configuration.GetSection(OverseerClientServiceConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<CandlestickServiceConfig>()
    .Bind(builder.Configuration.GetSection(CandlestickServiceConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<BroadcastServiceConfig>()
    .Bind(builder.Configuration.GetSection(BroadcastServiceConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<MarketDataInitializerConfig>()
    .Bind(builder.Configuration.GetSection(MarketDataInitializerConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<CentralPerformanceMonitorConfig>()
    .Bind(builder.Configuration.GetSection(CentralPerformanceMonitorConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<KalshiBotScopeManagerServiceConfig>()
    .Bind(builder.Configuration.GetSection(KalshiBotScopeManagerServiceConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<MarketServiceDataConfig>()
    .Bind(builder.Configuration.GetSection(MarketServiceDataConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<CentralBrainConfig>()
    .Bind(builder.Configuration.GetSection(CentralBrainConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<TargetCalculationServiceConfig>()
    .Bind(builder.Configuration.GetSection(TargetCalculationServiceConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<BrainStatusServiceConfig>()
    .Bind(builder.Configuration.GetSection(BrainStatusServiceConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<SnapshotGroupHelperConfig>()
    .Bind(builder.Configuration.GetSection(SnapshotGroupHelperConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<QueueMonitoringConfig>()
    .Bind(builder.Configuration.GetSection(QueueMonitoringConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<InterestScoreConfig>()
    .Bind(builder.Configuration.GetSection(InterestScoreConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<CentralErrorHandlerConfig>()
    .Bind(builder.Configuration.GetSection(CentralErrorHandlerConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<OrderBookServiceConfig>()
    .Bind(builder.Configuration.GetSection(OrderBookServiceConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<BacklashBot.State.CalculationsConfig>()
    .Bind(builder.Configuration.GetSection(BacklashBot.State.CalculationsConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<BacklashBotDataConfig>()
    .Bind(builder.Configuration.GetSection(BacklashBotDataConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<DataStorageConfig>()
    .Bind(builder.Configuration.GetSection(DataStorageConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<InstanceNameConfig>()
    .Bind(builder.Configuration.GetSection(InstanceNameConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var connectionString = BacklashCommon.Configuration.ConfigurationHelper.BuildConnectionString(builder.Configuration);
builder.Services.AddSingleton(new BacklashCommon.Configuration.ConnectionStringProvider(connectionString));

// Generate session identifier early to avoid circular dependency
var sessionIdentifier = GenerateSessionIdentifier();
builder.Services.AddSingleton(sessionIdentifier);

// Increase shutdown timeout
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});

// ## Service Registrations
builder.Services.AddSingleton<ICentralErrorHandler, CentralErrorHandler>();
builder.Services.AddSingleton<ICentralPerformanceMonitor>(sp => new CentralPerformanceMonitor(
    sp.GetRequiredService<ILogger<ICentralPerformanceMonitor>>(),
    sp.GetRequiredService<IOptions<GeneralExecutionConfig>>(),
    sp.GetRequiredService<IOptions<InstanceNameConfig>>().Value.Name,
    sp.GetRequiredService<IOptions<QueueMonitoringConfig>>(),
    sp.GetRequiredService<IOptions<CentralPerformanceMonitorConfig>>(),
    sp.GetRequiredService<IServiceScopeFactory>(),
    sp.GetRequiredService<IStatusTrackerService>()));
builder.Services.AddSingleton<IPerformanceMonitor>(provider =>
    provider.GetRequiredService<ICentralPerformanceMonitor>());
builder.Services.AddSingleton<IMessageProcessorPerformanceMetrics>(provider =>
    (IMessageProcessorPerformanceMetrics)provider.GetRequiredService<ICentralPerformanceMonitor>());
builder.Services.AddSingleton<ISqlDataServicePerformanceMetrics>(provider =>
    (ISqlDataServicePerformanceMetrics)provider.GetRequiredService<ICentralPerformanceMonitor>());
builder.Services.AddSingleton<IWebSocketPerformanceMetrics>(provider =>
    (IWebSocketPerformanceMetrics)provider.GetRequiredService<ICentralPerformanceMonitor>());
builder.Services.AddSingleton<ISubscriptionManagerPerformanceMetrics>(provider =>
    (ISubscriptionManagerPerformanceMetrics)provider.GetRequiredService<ICentralPerformanceMonitor>());
builder.Services.AddSingleton<INightActivitiesPerformanceMetrics>(provider =>
    (INightActivitiesPerformanceMetrics)provider.GetRequiredService<ICentralPerformanceMonitor>());
builder.Services.AddSingleton<IScopeManagerService, KalshiBotScopeManagerService>();
builder.Services.AddSingleton<IServiceFactory, ServiceFactory>();
builder.Services.AddSingleton<ICentralBrain>(sp =>
{
    Console.WriteLine("Resolving ILogger<ICentralBrain>");
    var logger = sp.GetRequiredService<ILogger<ICentralBrain>>();
    Console.WriteLine("Resolving IServiceFactory");
    var serviceFactory = sp.GetRequiredService<IServiceFactory>();
    Console.WriteLine("Resolving IServiceScopeFactory");
    var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
    Console.WriteLine("Resolving IOptions<TradingSnapshotServiceConfig>");
    var tradingSnapshotServiceConfig = sp.GetRequiredService<IOptions<TradingSnapshotServiceConfig>>();
    Console.WriteLine("Resolving IOptions<GeneralExecutionConfig> 1");
    var generalExecutionConfig1 = sp.GetRequiredService<IOptions<GeneralExecutionConfig>>();
    Console.WriteLine("Resolving IOptions<GeneralExecutionConfig> 2");
    var generalExecutionConfig2 = sp.GetRequiredService<IOptions<InstanceNameConfig>>();
    Console.WriteLine("Resolving ICentralErrorHandler");
    var centralErrorHandler = sp.GetRequiredService<ICentralErrorHandler>();
    Console.WriteLine("Resolving ICentralPerformanceMonitor");
    var centralPerformanceMonitor = sp.GetRequiredService<ICentralPerformanceMonitor>();
    Console.WriteLine("Resolving IMarketManagerService");
    var marketManager = sp.GetRequiredService<IMarketManagerService>();
    Console.WriteLine("Resolving IHostApplicationLifetime");
    var appLifetime = sp.GetRequiredService<IHostApplicationLifetime>();
    Console.WriteLine("Resolving IScopeManagerService");
    var scopeManagerService = sp.GetRequiredService<IScopeManagerService>();
    Console.WriteLine("Resolving IStatusTrackerService");
    var statusTrackerService = sp.GetRequiredService<IStatusTrackerService>();
    Console.WriteLine("Resolving IBotReadyStatus");
    var botReadyStatus = sp.GetRequiredService<IBotReadyStatus>();
    Console.WriteLine("Resolving IBrainStatusService");
    var brainStatusService = sp.GetRequiredService<IBrainStatusService>();
    Console.WriteLine("Resolving IOptions<CentralBrainConfig>");
    var centralBrainConfig = sp.GetRequiredService<IOptions<CentralBrainConfig>>();
    Console.WriteLine("Resolving Func<BacklashInterfaces.SmokehouseBot.Timers.ITimer>");
    var timerFactory = sp.GetRequiredService<Func<BacklashInterfaces.SmokehouseBot.Timers.ITimer>>();
    Console.WriteLine("Creating CentralBrain");
    return new CentralBrain(
        logger,
        serviceFactory,
        scopeFactory,
        tradingSnapshotServiceConfig,
        generalExecutionConfig1,
        generalExecutionConfig2,
        centralErrorHandler,
        centralPerformanceMonitor,
        marketManager,
        appLifetime,
        scopeManagerService,
        statusTrackerService,
        botReadyStatus,
        brainStatusService,
        centralBrainConfig,
        timerFactory);
});
builder.Services.AddSingleton<INightActivitiesPerformanceMetrics>(provider =>
    (INightActivitiesPerformanceMetrics)provider.GetRequiredService<ICentralPerformanceMonitor>());
builder.Services.AddSingleton<IWebSocketPerformanceMetrics>(provider =>
    (IWebSocketPerformanceMetrics)provider.GetRequiredService<ICentralPerformanceMonitor>());
builder.Services.AddSingleton<IMarketManagerService>(sp => new MarketManagerService(
    sp.GetRequiredService<IServiceFactory>(),
    sp.GetRequiredService<ILogger<IMarketManagerService>>(),
    sp.GetRequiredService<IServiceScopeFactory>(),
    sp.GetRequiredService<ICentralPerformanceMonitor>(),
    sp.GetRequiredService<IOptions<InstanceNameConfig>>(),
    sp.GetRequiredService<IOptions<CentralBrainConfig>>(),
    sp.GetRequiredService<IScopeManagerService>(),
    sp.GetRequiredService<IStatusTrackerService>(),
    sp.GetRequiredService<IBrainStatusService>(),
    sp.GetRequiredService<ITargetCalculationService>()));
builder.Services.AddSingleton<IStatusTrackerService, KalshiBotStatusTracker>();
builder.Services.AddSingleton<IBotReadyStatus, KalshiBotReadyStatus>();
builder.Services.AddSingleton<IBrainStatusService>(sp => new BrainStatusService(
    sp.GetRequiredService<IServiceScopeFactory>(),
    sp.GetRequiredService<IOptions<InstanceNameConfig>>(),
    sp.GetRequiredService<IOptions<BrainStatusServiceConfig>>(),
    sp.GetRequiredService<string>(), // sessionIdentifier
    sp.GetRequiredService<ILogger<BrainStatusService>>()));
builder.Services.AddSingleton<ITargetCalculationService, TargetCalculationService>();
builder.Services.AddTransient<BacklashInterfaces.SmokehouseBot.Timers.ITimer, BacklashBot.Timers.SystemTimer>();
builder.Services.AddSingleton<Func<BacklashInterfaces.SmokehouseBot.Timers.ITimer>>(sp => () => sp.GetRequiredService<BacklashInterfaces.SmokehouseBot.Timers.ITimer>());
builder.Services.AddHostedService(provider => provider.GetRequiredService<ICentralBrain>());

// Re-enable custom logger provider
builder.Services.AddSingleton<ILoggerProvider>(provider =>
{
    var loggingConfig = provider.GetRequiredService<IOptions<LoggingConfig>>().Value;
    var instanceNameConfig = provider.GetRequiredService<IOptions<InstanceNameConfig>>().Value;
    var minLevel = Enum.Parse<LogLevel>(loggingConfig.SqlDatabaseLogLevel, true);

    return new DatabaseLoggerProvider(
        provider.GetRequiredService<DatabaseLoggingQueue>(),
        loggingConfig,
        instanceNameConfig.Name,
        minLevel,
        provider.GetRequiredService<string>(), // sessionIdentifier
        loggingConfig.Environment);
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
            sp.GetRequiredService<IOptions<MarketServiceDataConfig>>()
        );
    };
});

// Database context - register manually to provide required constructor parameters
builder.Services.AddScoped<IBacklashBotContext>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<BacklashBotContext>>();
    var dataConfig = provider.GetRequiredService<IOptions<BacklashBotDataConfig>>().Value;
    var connStr = provider.GetRequiredService<BacklashCommon.Configuration.ConnectionStringProvider>().Value;
    return new BacklashBotContext(connStr, logger, dataConfig);
});
builder.Services.AddScoped<IKalshiAPIService>(sp => new KalshiAPIService(
    sp.GetRequiredService<ILogger<IKalshiAPIService>>(),
    sp.GetRequiredService<BacklashCommon.Configuration.ConnectionStringProvider>().Value, // connectionString
    sp.GetRequiredService<IServiceScopeFactory>(),
    sp.GetRequiredService<IStatusTrackerService>(),
    sp.GetRequiredService<IOptions<KalshiConfig>>(),
    sp.GetRequiredService<IOptions<KalshiAPIServiceConfig>>(),
    sp.GetRequiredService<IPerformanceMonitor>()
));

// Register services as scoped
builder.Services.AddScoped<ISqlDataService>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<ISqlDataService>>();
    var dataConfig = serviceProvider.GetRequiredService<IOptions<BacklashBotDataConfig>>().Value;
    var performanceMetrics = serviceProvider.GetServices<ISqlDataServicePerformanceMetrics>();
    var connectionString = serviceProvider.GetRequiredService<BacklashCommon.Configuration.ConnectionStringProvider>().Value;
    return new KalshiBotData.Data.SqlDataService(connectionString, logger, dataConfig, performanceMetrics);
});
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
    sp.GetRequiredService<IOptions<DataStorageConfig>>(),
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
    sp.GetRequiredService<IOptions<WebSocketMonitorServiceConfig>>(),
    sp.GetRequiredService<IScopeManagerService>(),
    sp.GetRequiredService<IBotReadyStatus>(),
    sp.GetRequiredService<IStatusTrackerService>(),
    sp.GetRequiredService<ICentralPerformanceMonitor>()));
builder.Services.AddSingleton<IOverseerClientService>(sp => new OverseerClientService(
    sp.GetRequiredService<ILogger<OverseerClientService>>(),
    sp.GetRequiredService<IServiceFactory>(),
    sp.GetRequiredService<IOptions<OverseerClientServiceConfig>>(),
    sp.GetRequiredService<IOptions<InstanceNameConfig>>(),
    sp.GetRequiredService<ICentralPerformanceMonitor>()));
builder.Services.AddScoped<IWebSocketConnectionManager>(sp => new WebSocketConnectionManager(
    sp.GetRequiredService<IOptions<KalshiConfig>>(),
    sp.GetRequiredService<IOptions<WebSocketConnectionManagerConfig>>(),
    sp.GetRequiredService<ILogger<WebSocketConnectionManager>>(),
    sp.GetRequiredService<ICentralPerformanceMonitor>()
));
builder.Services.AddScoped<IDataCache, DataCache>();
builder.Services.AddScoped<IMessageProcessor>(sp => new MessageProcessor(
    sp.GetRequiredService<ILogger<MessageProcessor>>(),
    sp.GetRequiredService<IWebSocketConnectionManager>(),
    sp.GetRequiredService<ISubscriptionManager>(),
    sp.GetRequiredService<IStatusTrackerService>(),
    sp.GetRequiredService<ISqlDataService>(),
    sp.GetRequiredService<IKalshiAPIService>(),
    sp.GetRequiredService<IOptions<MessageProcessorConfig>>().Value,
    sp.GetRequiredService<IOptions<KalshiAPIServiceConfig>>(),
    sp.GetRequiredService<IMessageProcessorPerformanceMetrics>(),
    sp.GetRequiredService<IPerformanceMonitor>()
));
builder.Services.AddScoped<ISubscriptionManager>(sp => new KalshiBotAPI.Websockets.SubscriptionManager(
    sp.GetRequiredService<ILogger<KalshiBotAPI.Websockets.SubscriptionManager>>(),
    sp.GetRequiredService<IWebSocketConnectionManager>(),
    sp.GetRequiredService<IDataCache>(),
    sp.GetRequiredService<IStatusTrackerService>(),
    sp.GetRequiredService<IOptions<SubscriptionManagerConfig>>()
));
builder.Services.AddScoped<IKalshiWebSocketClient>(sp => {
    var client = new KalshiWebSocketClient(
        sp.GetRequiredService<IOptions<KalshiConfig>>(),
        sp.GetRequiredService<IOptions<KalshiWebSocketClientConfig>>(),
        sp.GetRequiredService<ILogger<IKalshiWebSocketClient>>(),
        sp.GetRequiredService<IStatusTrackerService>(),
        sp.GetRequiredService<IBotReadyStatus>(),
        sp.GetRequiredService<ISqlDataService>(),
        sp.GetRequiredService<IWebSocketConnectionManager>(),
        sp.GetRequiredService<ISubscriptionManager>(),
        sp.GetRequiredService<IMessageProcessor>(),
        sp.GetRequiredService<IWebSocketPerformanceMetrics>(),
        sp.GetRequiredService<IOptions<LoggingConfig>>().Value.StoreWebSocketEvents
    );
    // Enable all channels for BacklashBot
    client.EnableAllChannels();
    return client;
});
builder.Services.AddScoped<IInterestScoreService, InterestScoreService>();
builder.Services.AddScoped<IOvernightActivitiesHelper>(provider =>
    new OvernightActivitiesHelper(
        provider.GetRequiredService<ILogger<OvernightActivitiesHelper>>(),
        provider.GetRequiredService<ISnapshotGroupHelper>(),
        provider.GetRequiredService<IOptions<DataStorageConfig>>(),
        provider.GetRequiredService<ISqlDataService>(),
        provider.GetRequiredService<INightActivitiesPerformanceMetrics>()));
builder.Services.AddScoped<ISnapshotPeriodHelper>(provider =>
    new SnapshotPeriodHelper(provider.GetRequiredService<IOptions<SnapshotPeriodHelperConfig>>().Value));
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// SignalR configuration
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
});

// Disable MVC discovery by clearing application parts
builder.Services.AddControllers().ConfigureApplicationPartManager(manager => manager.ApplicationParts.Clear());


// Configure Kestrel for IIS (no specific URL binding)
builder.WebHost.ConfigureKestrel(options => { });

var loggingConfig = builder.Configuration.GetSection(LoggingConfig.SectionName).Get<LoggingConfig>();
var consoleLogLevel = Enum.Parse<LogLevel>(loggingConfig.ConsoleLogLevel, true);
builder.Logging.SetMinimumLevel(consoleLogLevel);

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
// Configure endpoints for SignalR-only application
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

// Pre-startup database connectivity check
Console.WriteLine("Performing pre-startup database connectivity check...");
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
        await context.TestDbAsync();
        Console.WriteLine("Database connectivity check passed");
    }
}
catch (Exception ex)
{
    Console.WriteLine("Database connectivity check failed: {0}", ex.Message);
    Console.WriteLine("Exception type: {0}", ex.GetType().Name);
    throw new Exception("Database connectivity check failed during startup", ex);
}

using (var scope = app.Services.CreateScope())
{
    try
    {
        Console.WriteLine("About to call app.StartAsync() at {0}", DateTime.UtcNow);
        await app.StartAsync();
        Console.WriteLine("app.StartAsync() completed at {0}", DateTime.UtcNow);
        Console.WriteLine("Application started successfully at {0}", DateTime.UtcNow);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Application startup failed: {0}", ex.Message);
        Console.WriteLine("Exception type: {0}", ex.GetType().Name);
        Console.WriteLine("Stack trace: {0}", ex.StackTrace);
        throw;
    }
}

await app.WaitForShutdownAsync();
