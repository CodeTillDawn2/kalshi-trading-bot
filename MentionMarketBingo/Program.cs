using BacklashBotData.Configuration;
using BacklashBotData.Data;
using BacklashBotData.Data.Interfaces;
using BacklashCommon.Configuration;
using BacklashInterfaces.PerformanceMetrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KalshiBotAPI.Configuration;
using System.Threading.Tasks;

namespace MentionMarketBingo;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // ## Configuration Setup - match test setup for proper secrets loading
        string basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot"));
        var configuration = BacklashCommon.Configuration.ConfigurationHelper.CreateConfigurationBuilder(basePath, Array.Empty<string>()).Build();

        // Set up DI container
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IPerformanceMonitor>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<SimplePerformanceMonitor>>();
            return new SimplePerformanceMonitor(logger);
        });

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add connection string access - match BacklashBot exactly
        var connectionString = ConfigurationHelper.BuildConnectionString(configuration);
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddSingleton(connectionString);
            services.AddSingleton(new BacklashCommon.Configuration.ConnectionStringProvider(connectionString));
        }

        // Register configuration options - match BacklashBot exactly
        services.AddOptions<SecretsConfig>()
            .Bind(configuration.GetSection(SecretsConfig.SectionName))
            .ValidateOnStart();

        services.AddOptions<BacklashBotDataConfig>()
            .Bind(configuration.GetSection(BacklashBotDataConfig.SectionName))
            .ValidateOnStart();

        // Register Kalshi API configurations - match BacklashBot exactly
        services.AddOptions<KalshiBotAPI.Configuration.KalshiConfig>()
            .Bind(configuration.GetSection("Kalshi"))
            .ValidateOnStart();
        services.PostConfigure<KalshiBotAPI.Configuration.KalshiConfig>(kalshiConfig =>
        {
            kalshiConfig.KeyId = ConfigurationHelper.InterpolateConfigurationValue(kalshiConfig.KeyId, configuration);
            var interpolatedKeyFile = ConfigurationHelper.InterpolateConfigurationValue(kalshiConfig.KeyFile, configuration);
            var secretsConfig = configuration.GetSection(SecretsConfig.SectionName).Get<SecretsConfig>();
            kalshiConfig.KeyFile = ConfigurationHelper.ResolveSecretsFilePath(interpolatedKeyFile, secretsConfig, basePath);
        });

        services.AddOptions<KalshiBotAPI.Configuration.KalshiAPIServiceConfig>()
            .Bind(configuration.GetSection("API:KalshiAPIService"))
            .ValidateOnStart();

        // Register database context with scoped lifetime (new instance per operation) - match BacklashBot
        services.AddScoped<BacklashBotData.Data.BacklashBotContext>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<BacklashBotData.Data.BacklashBotContext>>();
            var dataConfig = provider.GetRequiredService<IOptions<BacklashBotData.Configuration.BacklashBotDataConfig>>().Value;
            var performanceMonitor = provider.GetRequiredService<IPerformanceMonitor>();
            var connStr = provider.GetRequiredService<BacklashCommon.Configuration.ConnectionStringProvider>().Value;
            return new BacklashBotData.Data.BacklashBotContext(connStr, logger, dataConfig, performanceMonitor);
        });
        services.AddScoped<BacklashBotData.Data.Interfaces.IBacklashBotContext>(provider => provider.GetRequiredService<BacklashBotData.Data.BacklashBotContext>());

        // Register simple status tracker service
        services.AddSingleton<BacklashBot.State.Interfaces.IStatusTrackerService, SimpleStatusTrackerService>();

        // Register Kalshi API service - exact match to BacklashBot registration
        services.AddScoped<BacklashBot.KalshiAPI.Interfaces.IKalshiAPIService>(sp => new KalshiBotAPI.KalshiAPI.KalshiAPIService(
            sp.GetRequiredService<ILogger<BacklashBot.KalshiAPI.Interfaces.IKalshiAPIService>>(),
            sp.GetRequiredService<BacklashCommon.Configuration.ConnectionStringProvider>().Value, // connectionString as string from provider
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<BacklashBot.State.Interfaces.IStatusTrackerService>(),
            sp.GetRequiredService<IOptions<KalshiBotAPI.Configuration.KalshiConfig>>(),
            sp.GetRequiredService<IOptions<KalshiBotAPI.Configuration.KalshiAPIServiceConfig>>(),
            sp.GetRequiredService<IPerformanceMonitor>()
        ));

        // Register Form1 with dependencies
        services.AddTransient<Form1>();

        var serviceProvider = services.BuildServiceProvider();

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        var form = serviceProvider.GetRequiredService<Form1>();

        Application.Run(form);
    }
}


// Simple status tracker service for the GUI app
public class SimpleStatusTrackerService : BacklashBot.State.Interfaces.IStatusTrackerService
{
    private readonly CancellationTokenSource _cts = new();

    public void Dispose()
    {
        _cts.Dispose();
    }

    public CancellationToken GetCancellationToken()
    {
        return _cts.Token;
    }

    public void CancelAll()
    {
        _cts.Cancel();
    }

    public void ResetAll()
    {
        // For GUI app, just create a new CTS
        _cts.Cancel();
        // Note: In a real implementation, you'd create a new CTS, but for simplicity we'll just leave it cancelled
    }
}
