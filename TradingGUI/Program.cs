using BacklashBotData.Data;
using BacklashBotData.Configuration;
using BacklashCommon.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Management;
using TradingGUI.Configuration;
using TradingSimulator;
using TradingStrategies.Configuration;
using TradingStrategies.Trading.Overseer;

namespace TradingGUI
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (IsUnderTestHost()) return;

            // Set up configuration using the standard helper
            var configuration = ConfigurationHelper.CreateConfigurationBuilder(AppDomain.CurrentDomain.BaseDirectory, args).Build();

            // Set up DI container
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<BacklashInterfaces.PerformanceMetrics.IPerformanceMonitor, PerformanceMonitor>();

            // Add logging
            services.AddLogging(logging =>
            {
                logging.AddConfiguration(configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();
            });

            // Add connection string access (matching BacklashBot pattern)
            var connectionString = ConfigurationHelper.BuildConnectionString(configuration);
            if (!string.IsNullOrEmpty(connectionString))
            {
                services.AddSingleton(connectionString);
            }

            // Register BacklashBotData configuration
            services.AddOptions<BacklashBotDataConfig>()
                .Bind(configuration.GetSection(BacklashBotDataConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Register database context with factory method
            services.AddDbContext<BacklashBotContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });
            services.AddTransient<BacklashBotContext>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<BacklashBotContext>>();
                var dataConfig = provider.GetRequiredService<IOptions<BacklashBotDataConfig>>().Value;
                return new BacklashBotContext(connectionString, logger, dataConfig);
            });

            // Register TradingSimulatorService
            services.AddScoped<TradingSimulatorService>();

            // Add configuration bindings
            services.AddOptions<MarketTypeServiceConfig>()
                .Bind(configuration.GetSection(MarketTypeServiceConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<EquityCalculatorConfig>()
                .Bind(configuration.GetSection(EquityCalculatorConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<StrategySelectionHelperConfig>()
                .Bind(configuration.GetSection(StrategySelectionHelperConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<SimulationEngineConfig>()
                .Bind(configuration.GetSection(SimulationEngineConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<PatternDetectionServiceConfig>()
                .Bind(configuration.GetSection(PatternDetectionServiceConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<DataLoaderConfig>()
                .Bind(configuration.GetSection(DataLoaderConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<SnapshotViewerConfig>()
                .Bind(configuration.GetSection(SnapshotViewerConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Register SnapshotViewer with dependencies
            services.AddTransient<SnapshotViewer>(sp =>
                new SnapshotViewer(
                    sp.GetRequiredService<BacklashBotContext>(),
                    sp.GetRequiredService<IOptions<SnapshotViewerConfig>>()));

            // Register MainForm with dependencies
            services.AddTransient<MainForm>(sp =>
                new MainForm(
                    sp.GetRequiredService<TradingSimulatorService>(),
                    sp.GetRequiredService<BacklashBotContext>(),
                    sp));

            var serviceProvider = services.BuildServiceProvider();

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(serviceProvider.GetRequiredService<MainForm>());
        }

        private static bool IsUnderTestHost()
        {
            try
            {
                var self = Process.GetCurrentProcess();
                string parentName = "";
                string parentCmd = "";

                using var q = new ManagementObjectSearcher(
                    $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {self.Id}");
                foreach (ManagementObject mo in q.Get())
                {
                    var ppid = (int)(uint)mo["ParentProcessId"];
                    using var parent = Process.GetProcessById(ppid);
                    parentName = parent.ProcessName ?? "";

                    using var q2 = new ManagementObjectSearcher(
                        $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {ppid}");
                    foreach (ManagementObject mo2 in q2.Get())
                        parentCmd = (string)(mo2["CommandLine"] ?? "");
                }

                var s1 = parentName.ToLowerInvariant();
                var s2 = parentCmd.ToLowerInvariant();
                return
                    s1.Contains("servicehub.testwindow") ||
                    s1.Contains("vstest") ||
                    s1.Contains("testhost") ||
                    s2.Contains("vstest") ||
                    s2.Contains("testwindow");
            }
            catch
            {
                return false;
            }
        }
    }
}
