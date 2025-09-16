using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace BacklashCommon.Configuration
{
    /// <summary>
    /// Helper class for configuration management, including secrets loading.
    /// </summary>
    public static class ConfigurationHelper
    {
        /// <summary>
        /// Adds secrets configuration to the configuration builder.
        /// This method loads secrets from a separate JSON file based on the SecretsPath configuration.
        /// </summary>
        /// <param name="config">The configuration builder to add secrets to.</param>
        /// <param name="currentDir">The current working directory of the application.</param>
        /// <param name="baseConfig">The base configuration that contains the SecretsPath setting.</param>
        /// <returns>The configuration builder with secrets added.</returns>
        public static IConfigurationBuilder AddSecretsConfiguration(
            this IConfigurationBuilder config,
            string currentDir,
            IConfiguration baseConfig)
        {
            var secretsPath = baseConfig.GetValue<string>("Secrets:SecretsPath") ?? "Secrets";
            var secretsFilePath = Path.Combine(currentDir, secretsPath, "secrets.json");

            Console.WriteLine($"Secrets path: {secretsPath}");
            Console.WriteLine($"Secrets file exists: {File.Exists(secretsFilePath)}");

            if (File.Exists(secretsFilePath))
            {
                config.AddJsonFile(secretsFilePath, optional: false, reloadOnChange: true);
                Console.WriteLine("Secrets file loaded successfully");
            }
            else
            {
                Console.WriteLine($"Warning: Secrets file not found at {secretsFilePath}");
            }

            return config;
        }

        /// <summary>
        /// Resolves the full path for a file referenced in configuration relative to the secrets directory.
        /// </summary>
        /// <param name="relativePath">The relative path from the secrets configuration.</param>
        /// <param name="secretsConfig">The secrets configuration containing the SecretsPath.</param>
        /// <param name="currentDir">The current working directory of the application.</param>
        /// <returns>The fully resolved path.</returns>
        public static string ResolveSecretsFilePath(string relativePath, SecretsConfig secretsConfig, string currentDir)
        {
            if (string.IsNullOrEmpty(relativePath))
                return relativePath;

            var secretsDir = Path.Combine(currentDir, secretsConfig.SecretsPath);
            return Path.Combine(secretsDir, relativePath);
        }

        /// <summary>
        /// Creates a configuration builder with standard settings including secrets support.
        /// This is a convenience method that sets up the complete configuration pipeline.
        /// </summary>
        /// <param name="currentDir">The current working directory of the application.</param>
        /// <param name="args">Command line arguments.</param>
        /// <returns>A configured IConfigurationBuilder.</returns>
        public static IConfigurationBuilder CreateConfigurationBuilder(string currentDir, string[] args)
        {
            // Check if appsettings files exist
            var appsettingsPath = Path.Combine(currentDir, "appsettings.json");
            var localAppsettingsPath = Path.Combine(currentDir, "appsettings.local.json");
            Console.WriteLine($"Current working directory: {currentDir}");
            Console.WriteLine($"appsettings.json exists: {File.Exists(appsettingsPath)}");
            Console.WriteLine($"appsettings.local.json exists: {File.Exists(localAppsettingsPath)}");

            // Load main configuration first
            var config = new ConfigurationBuilder()
                .SetBasePath(currentDir)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

            // Load secrets configuration
            var baseConfig = config.Build();
            config.AddSecretsConfiguration(currentDir, baseConfig);

            // Add remaining providers
            config.AddEnvironmentVariables()
                  .AddCommandLine(args);

            return config;
        }
    }
}