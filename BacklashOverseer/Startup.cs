using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Services;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using KalshiBotAPI.Configuration;
using KalshiBotAPI.KalshiAPI;
using KalshiBotAPI.WebSockets;
using KalshiBotAPI.WebSockets.Interfaces;
using KalshiBotData.Data;

using BacklashOverseer;
using BacklashOverseer.Config;
using BacklashCommon.Configuration;
using BacklashBotData.Configuration;
using KalshiBotLogging;
using Microsoft.Extensions.Options;
using BacklashOverseer.Services;
using BacklashDTOs.Configuration;
using BacklashCommon.Services;
using BacklashInterfaces.SmokehouseBot.Services;
using BacklashBot.Services.Interfaces;
using BacklashBot.Management.Interfaces;
using BacklashBot.Management;
using BacklashOverseer.State;
using BacklashInterfaces.PerformanceMetrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.IO;
using BacklashInterfaces.PerformanceMetrics;
using KalshiBotAPI.Websockets;
using BacklashBotData.Data.Interfaces;
using BacklashBotData.Data;
using BacklashCommon.Helpers;

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

            // Add logging services
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Add database logging services
            services.AddSingleton<DatabaseLoggingQueue>(provider => new DatabaseLoggingQueue(provider.GetRequiredService<IServiceProvider>(), true)); // isOverseer = true
            services.AddHostedService(provider => provider.GetRequiredService<DatabaseLoggingQueue>());

            // Create default configs for Overseer
            var overseerLoggingConfig = new LoggingConfig
            {
                Environment = "Overseer",
                StoreWebSocketEvents = false,
                SqlDatabaseLogLevel = "Information",
                ConsoleLogLevel = "Information"
            };
            var overseerExecutionConfig = new GeneralExecutionConfig
            {
                BrainInstance = "OverseerInstance",
                QueuesTargetCount = 100,
                RetryDelayMs = 1000,
                AuthTokenValidityHours = 24,
                HardDataStorageLocation = "Data",
                DecisionFrequencySeconds = 60,
                RefreshIntervalMinutes = 5,
                SnapshotSchemaVersion = 1
            };

            services.AddSingleton<ILoggerProvider>(provider =>
                new DatabaseLoggerProvider(
                    provider.GetRequiredService<DatabaseLoggingQueue>(),
                    overseerLoggingConfig,
                    overseerExecutionConfig,
                    LogLevel.Information, // Allow Information level and above
                    null, // brainStatus
                    "Overseer", // defaultEnvironment
                    "OverseerInstance" // defaultInstance
                ));

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
                sp.GetRequiredService<IMessageProcessorPerformanceMetrics>()
            ));
            services.AddScoped<ISubscriptionManager>(sp => new SubscriptionManager(
                sp.GetRequiredService<ILogger<SubscriptionManager>>(),
                sp.GetRequiredService<IWebSocketConnectionManager>(),
                sp.GetRequiredService<IDataCache>(),
                sp.GetRequiredService<IStatusTrackerService>(),
                sp.GetRequiredService<IOptions<SubscriptionManagerConfig>>()
            ));
            services.AddScoped<IKalshiWebSocketClient>(sp => new KalshiWebSocketClient(
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
                sp.GetRequiredService<IOptions<WebSocketConnectionManagerConfig>>().Value.EnablePerformanceMetrics
            ));
            services.AddScoped<ISqlDataService, SqlDataService>();
            services.AddScoped<BacklashBotContext>(provider =>
            {
                var connectionString = ConfigurationHelper.BuildConnectionString(Configuration);
                var logger = provider.GetRequiredService<ILogger<BacklashBotContext>>();
                var dataConfig = Configuration.GetSection("DBConnection:BacklashBotData").Get<BacklashBotDataConfig>();
                return new BacklashBotContext(connectionString, logger, dataConfig);
            });
            services.AddScoped<IBacklashBotContext>(provider => provider.GetRequiredService<BacklashBotContext>());
            services.AddSingleton<IStatusTrackerService, OverseerStatusTracker>();
            services.AddSingleton<IBotReadyStatus, OverseerReadyStatus>();
            services.AddScoped<IDataCache, BacklashBot.State.DataCache>();

            // Register the new WebSocketMonitorServiceLite as singleton
            services.AddSingleton<IWebSocketMonitorService, WebSocketMonitorServiceLite>();

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

            // Configure OverseerReadyConfig settings
            services.AddOptions<OverseerReadyConfig>()
                .Bind(Configuration.GetSection(OverseerReadyConfig.SectionName))
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


// Configure Secrets settings
services.AddOptions<BacklashCommon.Configuration.SecretsConfig>()
    .Bind(Configuration.GetSection(BacklashCommon.Configuration.SecretsConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
            // Resolve KeyFile path - combine secrets path with filename from secrets
            services.PostConfigure<KalshiConfig>(config =>
            {
                if (!string.IsNullOrEmpty(config.BotKeyFile) && config.BotKeyFile.Contains("{Kalshi:BotKeyFile}"))
                {
                    // Get the key file name from secrets
                    var keyFileName = Configuration.GetValue<string>("Kalshi:BotKeyFile");
                    if (!string.IsNullOrEmpty(keyFileName))
                    {
                        // Get secrets path from configuration
                        var secretsPath = Configuration.GetValue<string>("Secrets:SecretsPath") ?? "Secrets";
                        // Combine secrets directory path with filename
                        var secretsDir = Path.Combine(Directory.GetCurrentDirectory(), secretsPath);
                        config.BotKeyFile = Path.Combine(secretsDir, keyFileName);
                    }
                }
            });

            // Optional: Manual override if configuration binding fails
            services.PostConfigure<KalshiConfig>(config =>
            {
                if (string.IsNullOrEmpty(config.BotKeyId) || string.IsNullOrEmpty(config.BotKeyFile))
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
                                if (kalshiElement.TryGetProperty("KeyId", out var botKeyId)) config.BotKeyId = botKeyId.GetString() ?? "";
                                if (kalshiElement.TryGetProperty("KeyFile", out var botKeyFile)) config.BotKeyFile = botKeyFile.GetString() ?? "";
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
            services.AddSingleton<INightActivitiesPerformanceMetrics>(provider =>
                (INightActivitiesPerformanceMetrics)provider.GetRequiredService<PerformanceMetricsService>());

            // Register GeneralExecutionConfig
            services.AddOptions<GeneralExecutionConfig>()
                .Bind(Configuration.GetSection(GeneralExecutionConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Register services needed for OvernightActivitiesHelper
            services.AddScoped<IInterestScoreService, InterestScoreService>();
            services.AddScoped<ISnapshotGroupHelper, SnapshotGroupHelper>();

            // Register OvernightActivitiesHelper
            services.AddScoped<IOvernightActivitiesHelper>(provider =>
                new BacklashCommon.Services.OvernightActivitiesHelper(
                    provider.GetRequiredService<ILogger<IOvernightActivitiesHelper>>(),
                    null, // interestScoreHelper parameter not used in constructor
                    provider.GetRequiredService<ISnapshotGroupHelper>(),
                    provider.GetRequiredService<IOptions<GeneralExecutionConfig>>(),
                    provider.GetRequiredService<ISqlDataService>(),
                    provider.GetRequiredService<INightActivitiesPerformanceMetrics>()));

            // Register BrainPersistenceService
            services.AddSingleton<BrainPersistenceService>(sp => new BrainPersistenceService(
                sp.GetRequiredService<IOptions<BrainPersistenceServiceConfig>>(),
                null, // context optional for singleton
                sp.GetRequiredService<ILogger<BrainPersistenceService>>(),
                sp.GetRequiredService<PerformanceMetricsService>()
            ));

            // Add MVC for controllers
            services.AddControllers()
                .AddJsonOptions(o =>
                {
                    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    o.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                });

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
