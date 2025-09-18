using System.Diagnostics;
using System.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingStrategies.Trading.Overseer;
using BacklashInterfaces.PerformanceMetrics;
using TradingStrategies.Configuration;
using TradingStrategies.Trading.Helpers;
using BacklashDTOs.Configuration;

namespace TradingGUI
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            if (IsUnderTestHost()) return;

            // Set up configuration
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
                .Build();

            // Set up DI container
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<BacklashInterfaces.PerformanceMetrics.IPerformanceMonitor, PerformanceMonitor>();

            // Add connection string access (matching BacklashBot pattern)
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(connectionString))
            {
                services.AddSingleton(connectionString);
            }

            // Add configuration bindings
            services.Configure<MarketTypeServiceConfig>(configuration.GetSection("Simulator:MarketTypeService"));
            services.Configure<EquityCalculatorConfig>(configuration.GetSection("Simulator:EquityCalculator"));
            services.Configure<StrategySelectionHelperConfig>(configuration.GetSection("Simulator:StrategySelectionHelper"));
            services.Configure<DataLoaderConfig>(configuration.GetSection("SnapshotHandling:DataLoader"));

            var serviceProvider = services.BuildServiceProvider();

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(serviceProvider));
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
