// Updated Program.cs with web hosting
using KalshiBotOverseer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

class Program
{
    static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        // Start the web host
        await host.StartAsync();

        // Get services and start overseer
        using var scope = host.Services.CreateScope();
        var overseer = scope.ServiceProvider.GetRequiredService<Overseer>();
        overseer.Start();

        Console.WriteLine("Services started. Press any key to stop...");
        Console.ReadKey();

        overseer.Stop();
        await host.StopAsync();
    }

    static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                var currentDir = Directory.GetCurrentDirectory();
                Console.WriteLine($"Current working directory: {currentDir}");

                // Check if appsettings files exist
                var appsettingsPath = Path.Combine(currentDir, "appsettings.json");
                var localAppsettingsPath = Path.Combine(currentDir, "appsettings.local.json");
                Console.WriteLine($"appsettings.json exists: {File.Exists(appsettingsPath)}");
                Console.WriteLine($"appsettings.local.json exists: {File.Exists(localAppsettingsPath)}");

                config.SetBasePath(currentDir)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<KalshiBotOverseer.Startup>();
            });
    }
}