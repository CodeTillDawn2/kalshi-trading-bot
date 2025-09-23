using BacklashBot.KalshiAPI.Interfaces;
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
using BacklashInterfaces.PerformanceMetrics;
using BacklashInterfaces.Constants;
using BacklashInterfaces.PerformanceMetrics;
using BacklashOverseer.Config;
using BacklashOverseer.Services;
using BacklashOverseer.State;
using KalshiBotAPI.Configuration;
using KalshiBotAPI.KalshiAPI;
using KalshiBotAPI.Websockets;
using KalshiBotAPI.WebSockets.Interfaces;
using KalshiBotData.Data;
using KalshiBotLogging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BacklashOverseer
{
    /// <summary>
    /// Configures services and the request pipeline for the BacklashOverseer application.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Initializes a new instance of the Startup class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Gets the application configuration.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Configures the services for dependency injection.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Add configuration as singleton
            services.AddSingleton<IConfiguration>(Configuration);

            // Bind configurations from appsettings
            services.AddOptions<LoggingConfig>()
                .Bind(Configuration.GetSection(LoggingConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<InstanceNameConfig>()
                .Bind(Configuration.GetSection(InstanceNameConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Add logging services
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddFilter("Microsoft.AspNetCore.Hosting", LogLevel.Warning);
                builder.AddFilter("Microsoft.AspNetCore.StaticFiles", LogLevel.Warning);
                builder.AddFilter("Microsoft.AspNetCore.Routing", LogLevel.Warning);
                builder.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
                builder.AddConsole();
                var loggingConfig = Configuration.GetSection(LoggingConfig.SectionName).Get<LoggingConfig>();
                if (loggingConfig != null)
                {
                    var consoleLogLevel = Enum.Parse<LogLevel>(loggingConfig.ConsoleLogLevel, true);
                    builder.SetMinimumLevel(consoleLogLevel);
                }
                else
                {
                    builder.SetMinimumLevel(LogLevel.Debug);
                }
            });

            // Add database logging services
            services.AddSingleton<DatabaseLoggingQueue>(provider => new DatabaseLoggingQueue(provider.GetRequiredService<IServiceProvider>(), true)); // isOverseer = true
            services.AddHostedService(provider => provider.GetRequiredService<DatabaseLoggingQueue>());

            services.AddSingleton<ILoggerProvider>(provider =>
            {
                var loggingConfig = provider.GetRequiredService<IOptions<LoggingConfig>>().Value;
                var instanceNameConfig = provider.GetRequiredService<IOptions<InstanceNameConfig>>().Value;
                var minLevel = Enum.Parse<LogLevel>(loggingConfig.SqlDatabaseLogLevel, true);
                return new DatabaseLoggerProvider(
                    provider.GetRequiredService<DatabaseLoggingQueue>(),
                    loggingConfig,
                    instanceNameConfig.Name,
                    minLevel,
                    null, // brainStatus
                    loggingConfig.Environment // defaultEnvironment
                );
            });

            // Register required services
            services.AddScoped<IKalshiAPIService, KalshiAPIService>();
            services.AddScoped<IWebSocketConnectionManager, WebSocketConnectionManager>();
            services.AddScoped<IMessageProcessor>(sp => new MessageProcessor(
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
            services.AddScoped<ISubscriptionManager>(sp => new SubscriptionManager(
                sp.GetRequiredService<ILogger<SubscriptionManager>>(),
                sp.GetRequiredService<IWebSocketConnectionManager>(),
                sp.GetRequiredService<IDataCache>(),
                sp.GetRequiredService<IStatusTrackerService>(),
                sp.GetRequiredService<IOptions<SubscriptionManagerConfig>>()
            ));
            services.AddScoped<IKalshiWebSocketClient>(sp => {
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
                    sp.GetRequiredService<IOptions<LoggingConfig>>().Value.StoreWebSocketEvents,
                    sp.GetRequiredService<IOptions<WebSocketConnectionManagerConfig>>().Value.BufferSize,
                    sp.GetRequiredService<IOptions<WebSocketConnectionManagerConfig>>().Value.EnablePerformanceMetrics
                );
                // Disable market-specific channels for overseer
                client.DisableChannel(KalshiConstants.ScriptType_Feed_Orderbook);
                client.DisableChannel(KalshiConstants.ScriptType_Feed_Ticker);
                client.DisableChannel(KalshiConstants.ScriptType_Feed_Trade);
                return client;
            });
            services.AddScoped<ISqlDataService>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<ISqlDataService>>();
                var dataConfig = serviceProvider.GetRequiredService<IOptions<BacklashBotDataConfig>>().Value;
                var performanceMetrics = serviceProvider.GetServices<ISqlDataServicePerformanceMetrics>();
                var connectionString = serviceProvider.GetRequiredService<string>();
                return new KalshiBotData.Data.SqlDataService(connectionString, logger, dataConfig, performanceMetrics);
            });
            services.AddScoped<BacklashBotContext>(provider =>
            {
                var connectionString = provider.GetRequiredService<string>();
                var logger = provider.GetRequiredService<ILogger<BacklashBotContext>>();
                var dataConfig = Configuration.GetSection("DBConnection:BacklashBotData").Get<BacklashBotDataConfig>();
                return new BacklashBotContext(connectionString, logger, dataConfig);
            });
            services.AddScoped<IBacklashBotContext>(provider => provider.GetRequiredService<BacklashBotContext>());
            services.AddSingleton<IStatusTrackerService, OverseerStatusTracker>();
            services.AddSingleton<IBotReadyStatus, OverseerReadyStatus>();

            services.AddScoped<IWebSocketMonitorService>(sp => new BacklashCommon.Services.OverseerWebSocketMonitorService(
                sp.GetRequiredService<ILogger<BacklashCommon.Services.OverseerWebSocketMonitorService>>(),
                sp.GetRequiredService<IKalshiWebSocketClient>(),
                sp.GetRequiredService<IServiceScopeFactory>()
            ));

            // Configure Kalshi settings with fallback
            services.AddOptions<KalshiConfig>()
                .Bind(Configuration.GetSection(KalshiConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Configure OverseerHub settings
            services.AddOptions<OverseerHubConfig>()
                .Bind(Configuration.GetSection(OverseerHubConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Configure Overseer settings
            services.AddOptions<OverseerConfig>()
                .Bind(Configuration.GetSection(OverseerConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Configure MarketWatchController settings
            services.AddOptions<MarketWatchControllerConfig>()
                .Bind(Configuration.GetSection(MarketWatchControllerConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Configure SubscriptionManager settings
            services.AddOptions<SubscriptionManagerConfig>()
                .Bind(Configuration.GetSection(SubscriptionManagerConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<MessageProcessorConfig>()
                .Bind(Configuration.GetSection(MessageProcessorConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<WebSocketConnectionManagerConfig>()
                .Bind(Configuration.GetSection(WebSocketConnectionManagerConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<KalshiWebSocketClientConfig>()
                .Bind(Configuration.GetSection(KalshiWebSocketClientConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<MarketWatchControllerConfig>()
                .Bind(Configuration.GetSection(MarketWatchControllerConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<BacklashBotDataConfig>()
                .Bind(Configuration.GetSection(BacklashBotDataConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();


            // Configure Secrets settings
            services.AddOptions<BacklashCommon.Configuration.SecretsConfig>()
                .Bind(Configuration.GetSection(BacklashCommon.Configuration.SecretsConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            // Interpolate placeholders in KalshiConfig
            services.PostConfigure<KalshiConfig>(config =>
            {
                // Interpolate KeyId and KeyFile
                var interpolatedKeyId = BacklashCommon.Configuration.ConfigurationHelper.InterpolateConfigurationValue(config.KeyId, Configuration);
                var interpolatedKeyFile = BacklashCommon.Configuration.ConfigurationHelper.InterpolateConfigurationValue(config.KeyFile, Configuration);

                // Resolve the key file path to the secrets directory
                var secretsConfig = Configuration.GetSection(BacklashCommon.Configuration.SecretsConfig.SectionName).Get<BacklashCommon.Configuration.SecretsConfig>();
                var resolvedKeyFile = BacklashCommon.Configuration.ConfigurationHelper.ResolveSecretsFilePath(interpolatedKeyFile, secretsConfig, Directory.GetCurrentDirectory());

                // Update the config with interpolated values
                config.KeyId = interpolatedKeyId;
                config.KeyFile = resolvedKeyFile;
            });

            // Optional: Manual override if configuration binding fails
            services.PostConfigure<KalshiConfig>(config =>
            {
                if (string.IsNullOrEmpty(config.KeyId) || string.IsNullOrEmpty(config.KeyFile))
                {
                    Console.WriteLine("Warning: KalshiConfig not properly bound, attempting manual override...");

                    // Try to read from local file directly
                    var localConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                    if (File.Exists(localConfigPath))
                    {
                        try
                        {
                            var localJson = File.ReadAllText(localConfigPath);
                            var localConfig = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(localJson);
                            if (localConfig.TryGetProperty("Kalshi", out var kalshiElement))
                            {
                                if (kalshiElement.TryGetProperty("Environment", out var env)) config.Environment = env.GetString() ?? "prd";
                                if (kalshiElement.TryGetProperty("KeyId", out var botKeyId)) config.KeyId = botKeyId.GetString() ?? "";
                                if (kalshiElement.TryGetProperty("KeyFile", out var botKeyFile)) config.KeyFile = botKeyFile.GetString() ?? "";
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to read local config: {ex.Message}");
                        }
                    }
                }
            });

            var connectionString = BacklashCommon.Configuration.ConfigurationHelper.BuildConnectionString(Configuration);
            services.AddSingleton(connectionString);

            services.PostConfigure<IBotReadyStatus>(status => {
                if (status is OverseerReadyStatus ors) {
                    ors.InitializationCompleted.SetResult(true);
                }
            });


            // Register the new EventSubscriber as singleton
            services.AddSingleton<Overseer>();

            // Add memory cache for data caching
            services.AddMemoryCache();

            // Register SnapshotService with dependencies
            services.AddScoped<SnapshotAggregationService>(sp =>
                new SnapshotAggregationService(
                    sp.GetRequiredService<IBacklashBotContext>(),
                    sp.GetRequiredService<IConfiguration>(),
                    sp.GetRequiredService<PerformanceMetricsService>(),
                    sp.GetRequiredService<ILogger<SnapshotAggregationService>>()
                ));

            // Register PerformanceMetricsService
            services.AddSingleton<PerformanceMetricsService>();
            services.AddSingleton<IPerformanceMonitor>(provider =>
                provider.GetRequiredService<PerformanceMetricsService>());
            services.AddSingleton<IWebSocketPerformanceMetrics>(provider =>
                (IWebSocketPerformanceMetrics)provider.GetRequiredService<PerformanceMetricsService>());
            services.AddSingleton<IMessageProcessorPerformanceMetrics>(provider =>
                (IMessageProcessorPerformanceMetrics)provider.GetRequiredService<PerformanceMetricsService>());
            services.AddSingleton<INightActivitiesPerformanceMetrics>(provider =>
                (INightActivitiesPerformanceMetrics)provider.GetRequiredService<PerformanceMetricsService>());


            // Register services needed for OvernightActivitiesHelper
            services.AddScoped<IInterestScoreService, InterestScoreService>();
            services.AddScoped<ISnapshotGroupHelper, SnapshotGroupHelper>();

            // Register OvernightActivitiesHelper
            services.AddScoped<IOvernightActivitiesHelper>(provider =>
                new OvernightActivitiesHelper(
                    provider.GetRequiredService<ILogger<IOvernightActivitiesHelper>>(),
                    provider.GetRequiredService<ISnapshotGroupHelper>(),
                    provider.GetRequiredService<IOptions<DataStorageConfig>>(),
                    provider.GetRequiredService<ISqlDataService>(),
                    provider.GetRequiredService<INightActivitiesPerformanceMetrics>()));

            // Register BrainPersistenceService
            services.AddScoped<BrainPersistenceService>(sp => new BrainPersistenceService(
                sp.GetRequiredService<IOptions<BrainPersistenceServiceConfig>>(),
                sp.GetRequiredService<IBacklashBotContext>(),
                sp.GetRequiredService<ILogger<BrainPersistenceService>>(),
                sp.GetRequiredService<PerformanceMetricsService>()
            ));


            // Add CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // Add MVC controllers
            services.AddControllers();

            // Add SignalR
            services.AddSignalR(options =>
            {
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);

            }).AddJsonProtocol(o => { o.PayloadSerializerOptions.PropertyNamingPolicy = null; });
        }

        /// <summary>
        /// Configures the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="env">The web hosting environment.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Set up static files serving from the wwwroot directory
            app.UseStaticFiles();

            app.UseCors("AllowAll");

            // Add request/response logging middleware
            app.Use(async (context, next) =>
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<Startup>>();
                logger.LogInformation("Request: {Method} {Path}", context.Request.Method, context.Request.Path);
                await next();
                logger.LogInformation("Response: {StatusCode}", context.Response.StatusCode);
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<OverseerHub>("/chartHub");

                // Add health check endpoint for overseer discovery
                endpoints.MapGet("/health", async context =>
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"status\":\"healthy\",\"service\":\"BacklashOverseer\"}");
                });
            });
        }

    }
}
