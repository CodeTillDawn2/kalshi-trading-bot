using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Services;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using KalshiBotAPI.Configuration;
using KalshiBotAPI.KalshiAPI;
using KalshiBotAPI.Websockets;
using KalshiBotAPI.WebSockets.Interfaces;
using KalshiBotData.Data;
using KalshiBotData.Data.Interfaces;
using KalshiBotOverseer;
using KalshiBotLogging;
using KalshiBotOverseer.Services;
using BacklashDTOs.Configuration;
using KalshiBotOverseer.State;
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

namespace KalshiBotOverseer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

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
            services.AddSingleton<ILoggerProvider>(provider =>
                new DatabaseLoggerProvider(
                    provider.GetRequiredService<DatabaseLoggingQueue>(),
                    LogLevel.Information, // Allow Information level and above
                    null, // loggingConfig
                    null, // executionConfig
                    null, // brainStatus
                    "Overseer", // defaultEnvironment
                    "OverseerInstance" // defaultInstance
                ));
            services.AddHostedService(provider => provider.GetRequiredService<DatabaseLoggingQueue>());

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
                sp.GetRequiredService<IOptions<KalshiConfig>>().Value
            ));
            services.AddScoped<ISubscriptionManager>(sp => new SubscriptionManager(
                sp.GetRequiredService<ILogger<SubscriptionManager>>(),
                sp.GetRequiredService<IWebSocketConnectionManager>(),
                sp.GetRequiredService<IDataCache>(),
                sp.GetRequiredService<IStatusTrackerService>(),
                sp.GetRequiredService<IConfiguration>()
            ));
            services.AddScoped<IKalshiWebSocketClient>(sp => new KalshiWebSocketClient(
                sp.GetRequiredService<IOptions<KalshiConfig>>(),
                sp.GetRequiredService<ILogger<IKalshiWebSocketClient>>(),
                sp.GetRequiredService<IStatusTrackerService>(),
                sp.GetRequiredService<IBotReadyStatus>(),
                sp.GetRequiredService<ISqlDataService>(),
                sp.GetRequiredService<IWebSocketConnectionManager>(),
                sp.GetRequiredService<ISubscriptionManager>(),
                sp.GetRequiredService<IMessageProcessor>(),
                sp.GetRequiredService<IDataCache>(),
                false
            ));
            services.AddScoped<ISqlDataService, SqlDataService>();
            services.AddScoped<KalshiBotContext>(provider => new KalshiBotContext(Configuration));
            services.AddScoped<IKalshiBotContext>(provider => provider.GetRequiredService<KalshiBotContext>());
            services.AddSingleton<IStatusTrackerService, OverseerStatusTracker>();
            services.AddSingleton<IBotReadyStatus, OverseerReadyStatus>();
            services.AddScoped<IDataCache, BacklashBot.State.DataCache>();

            // Register the new WebSocketMonitorServiceLite as singleton
            services.AddSingleton<IWebSocketMonitorService, WebSocketMonitorServiceLite>();

            // Configure Kalshi settings with fallback
            services.Configure<KalshiConfig>(Configuration.GetSection("Kalshi"));

            // Configure OverseerHub settings
            services.Configure<OverseerHubConfig>(Configuration.GetSection("OverseerHub"));

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

            // Add memory cache for data caching
            services.AddMemoryCache();

            // Register SnapshotService with dependencies
            services.AddScoped<SnapshotAggregationService>(sp =>
                new SnapshotAggregationService(
                    sp.GetRequiredService<IKalshiBotContext>(),
                    sp.GetRequiredService<IConfiguration>(),
                    sp.GetRequiredService<PerformanceMetricsService>(),
                    sp.GetRequiredService<ILogger<SnapshotAggregationService>>()
                ));

            // Register PerformanceMetricsService
            services.AddSingleton<PerformanceMetricsService>();

            // Register BrainPersistenceService
            services.AddSingleton<BrainPersistenceService>();

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
                    await context.Response.WriteAsync("{\"status\":\"healthy\",\"service\":\"KalshiBotOverseer\"}");
                });
            });
        }

    }
}
