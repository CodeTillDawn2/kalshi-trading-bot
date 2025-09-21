// Updated Program.cs with web hosting
using BacklashCommon.Configuration;
using BacklashOverseer;
using Microsoft.AspNetCore.Hosting;
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
        await overseer.Start();

        Console.WriteLine("Services started. Web server running at http://localhost:5000");
        Console.WriteLine("MarketWatch GUI available at: http://localhost:5000/marketwatch");
        Console.WriteLine("Press Ctrl+C to stop...");

        // Wait for shutdown signal
        var shutdownTokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true; // Prevent immediate termination
            shutdownTokenSource.Cancel();
        };

        try
        {
            await Task.Delay(Timeout.Infinite, shutdownTokenSource.Token);
        }
        catch (TaskCanceledException)
        {
            // Expected when Ctrl+C is pressed
        }

        Console.WriteLine("Shutting down...");

        overseer.Stop();
        await host.StopAsync();
    }

    static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                var currentDir = Directory.GetCurrentDirectory();
                var configurationBuilder = ConfigurationHelper.CreateConfigurationBuilder(currentDir, args);

                // Copy the sources to the host configuration
                foreach (var source in configurationBuilder.Sources)
                {
                    config.Sources.Add(source);
                }
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseUrls("http://0.0.0.0:5000");
                webBuilder.UseStartup<BacklashOverseer.Startup>();
            });
    }
}
