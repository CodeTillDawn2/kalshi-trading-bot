using KalshiBotAPI.Configuration;
using KalshiBotAPI.KalshiAPI;
using KalshiBotAPI.Websockets;
using KalshiBotAPI.WebSockets.Interfaces;
using KalshiBotData.Data;
using KalshiBotData.Data.Interfaces;
using KalshiBotOverseer;
using KalshiBotOverseer.State;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmokehouseBot.KalshiAPI.Interfaces;
using SmokehouseBot.Services;
using SmokehouseBot.Services.Interfaces;
using SmokehouseBot.State.Interfaces;

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
            services.AddScoped<KalshiBotContext>(provider => new KalshiBotContext(Configuration));
            services.AddScoped<IKalshiBotContext>(provider => provider.GetService<KalshiBotContext>());
            services.AddSingleton<IStatusTrackerService, OverseerStatusTracker>();
            services.AddSingleton<IBotReadyStatus, OverseerReadyStatus>();

            // Register the new WebSocketMonitorServiceLite as singleton
            services.AddSingleton<IWebSocketMonitorService, WebSocketMonitorServiceLite>();

            // Configure Kalshi settings with fallback
            services.Configure<KalshiConfig>(Configuration.GetSection("Kalshi"));

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

            // Register SignalRService (optional - for real-time messaging)
            services.AddSingleton<SignalRService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<SignalRService>>();
                // TODO: Get hub URL from configuration
                var hubUrl = "http://localhost:5000/chartHub"; // Adjust as needed for your SignalR hub
                return new SignalRService(hubUrl, logger);
            });

            // Register the new EventSubscriber as singleton
            services.AddSingleton<Overseer>();

            // Add memory cache for data caching
            services.AddMemoryCache();

            // Add MVC for controllers
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Set up static files serving from the wwwroot directory
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}