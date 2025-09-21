using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

using BacklashBot.Configuration;
using BacklashBot.State;
using BacklashBot.Services;
using BacklashBotData.Configuration;
using BacklashCommon.Configuration;
using BacklashDTOs.Configuration;
using KalshiBotAPI.Configuration;
using TradingStrategies.Configuration;
using BacklashCommon.Configuration;

namespace BacklashBotTests
{
    [TestFixture]
    public class ConfigValidationTests
    {
        private IConfiguration _configuration;

        [SetUp]
        public void SetUp()
        {
            var basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot"));
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

            _configuration = builder.Build();
        }

        [Test]
        public void ValidateAllConfigs_FromAppsettings_Valid()
        {
            // SecretsConfig - "Secrets"
            var secretsConfig = new SecretsConfig();
            var secretsSection = _configuration.GetSection("Secrets");
            secretsSection.Bind(secretsConfig);
            ValidateConfig(secretsConfig, secretsSection);

            // LoggingConfig - "Communications:Logging"
            var loggingConfig = new LoggingConfig();
            var loggingSection = _configuration.GetSection("Communications:Logging");
            loggingSection.Bind(loggingConfig);
            ValidateConfig(loggingConfig, loggingSection);

            // KalshiConfig
            var kalshiConfig = new KalshiConfig();
            var kalshiSection = _configuration.GetSection(GetSectionName(typeof(KalshiConfig)));
            kalshiSection.Bind(kalshiConfig);
            ValidateConfig(kalshiConfig, kalshiSection);

            // KalshiAPIServiceConfig - "API:KalshiAPIService"
            var kalshiAPIServiceConfig = new KalshiAPIServiceConfig();
            var kalshiAPIServiceSection = _configuration.GetSection("API:KalshiAPIService");
            kalshiAPIServiceSection.Bind(kalshiAPIServiceConfig);
            ValidateConfig(kalshiAPIServiceConfig, kalshiAPIServiceSection);

            // WebSocketConnectionManagerConfig
            var webSocketConnectionManagerConfig = new WebSocketConnectionManagerConfig();
            var webSocketConnectionManagerSection = _configuration.GetSection(GetSectionName(typeof(WebSocketConnectionManagerConfig)));
            webSocketConnectionManagerSection.Bind(webSocketConnectionManagerConfig);
            ValidateConfig(webSocketConnectionManagerConfig, webSocketConnectionManagerSection);

            // MessageProcessorConfig - "Websockets:MessageProcessor"
            var messageProcessorConfig = new MessageProcessorConfig();
            var messageProcessorSection = _configuration.GetSection("Websockets:MessageProcessor");
            messageProcessorSection.Bind(messageProcessorConfig);
            ValidateConfig(messageProcessorConfig, messageProcessorSection);

            // SubscriptionManagerConfig - "Websockets:SubscriptionManager"
            var subscriptionManagerConfig = new SubscriptionManagerConfig();
            var subscriptionManagerSection = _configuration.GetSection("Websockets:SubscriptionManager");
            subscriptionManagerSection.Bind(subscriptionManagerConfig);
            ValidateConfig(subscriptionManagerConfig, subscriptionManagerSection);

            // WebSocketMonitorConfig - "Websockets:WebSocketMonitor"
            var webSocketMonitorConfig = new WebSocketMonitorConfig();
            var webSocketMonitorSection = _configuration.GetSection("Websockets:WebSocketMonitor");
            webSocketMonitorSection.Bind(webSocketMonitorConfig);
            ValidateConfig(webSocketMonitorConfig, webSocketMonitorSection);

            // KalshiWebSocketClientConfig - "Websockets:KalshiWebSocketClient"
            var kalshiWebSocketClientConfig = new KalshiWebSocketClientConfig();
            var kalshiWebSocketClientSection = _configuration.GetSection("Websockets:KalshiWebSocketClient");
            kalshiWebSocketClientSection.Bind(kalshiWebSocketClientConfig);
            ValidateConfig(kalshiWebSocketClientConfig, kalshiWebSocketClientSection);

            // TradingSnapshotServiceConfig - "WatchedMarkets:TradingSnapshotService"
            var tradingSnapshotServiceConfig = new TradingSnapshotServiceConfig();
            var tradingSnapshotServiceSection = _configuration.GetSection("WatchedMarkets:TradingSnapshotService");
            tradingSnapshotServiceSection.Bind(tradingSnapshotServiceConfig);
            ValidateConfig(tradingSnapshotServiceConfig, tradingSnapshotServiceSection);

            // SnapshotPeriodHelperConfig - "WatchedMarkets:SnapshotPeriodHelper"
            var snapshotPeriodHelperConfig = new SnapshotPeriodHelperConfig();
            var snapshotPeriodHelperSection = _configuration.GetSection("WatchedMarkets:SnapshotPeriodHelper");
            snapshotPeriodHelperSection.Bind(snapshotPeriodHelperConfig);
            ValidateConfig(snapshotPeriodHelperConfig, snapshotPeriodHelperSection);

            // OrderbookChangeTrackerConfig - "WatchedMarkets:OrderbookChangeTracker"
            var orderbookChangeTrackerConfig = new OrderbookChangeTrackerConfig();
            var orderbookChangeTrackerSection = _configuration.GetSection("WatchedMarkets:OrderbookChangeTracker");
            orderbookChangeTrackerSection.Bind(orderbookChangeTrackerConfig);
            ValidateConfig(orderbookChangeTrackerConfig, orderbookChangeTrackerSection);

            // MarketRefreshServiceConfig - "WatchedMarkets:MarketRefreshService"
            var marketRefreshServiceConfig = new MarketRefreshServiceConfig();
            var marketRefreshServiceSection = _configuration.GetSection("WatchedMarkets:MarketRefreshService");
            marketRefreshServiceSection.Bind(marketRefreshServiceConfig);
            ValidateConfig(marketRefreshServiceConfig, marketRefreshServiceSection);

            // GeneralExecutionConfig - "Central:GeneralExecution"
            var generalExecutionConfig = new GeneralExecutionConfig();
            var generalExecutionSection = _configuration.GetSection("Central:GeneralExecution");
            generalExecutionSection.Bind(generalExecutionConfig);
            ValidateConfig(generalExecutionConfig, generalExecutionSection);

            // OverseerClientServiceConfig - "Communications:OverseerClientService"
            var overseerClientServiceConfig = new OverseerClientServiceConfig();
            var overseerClientServiceSection = _configuration.GetSection("Communications:OverseerClientService");
            overseerClientServiceSection.Bind(overseerClientServiceConfig);
            ValidateConfig(overseerClientServiceConfig, overseerClientServiceSection);

            // CandlestickServiceConfig - "WatchedMarkets:CandlestickService"
            var candlestickServiceConfig = new CandlestickServiceConfig();
            var candlestickServiceSection = _configuration.GetSection("WatchedMarkets:CandlestickService");
            candlestickServiceSection.Bind(candlestickServiceConfig);
            ValidateConfig(candlestickServiceConfig, candlestickServiceSection);

            // BroadcastServiceConfig - "Communications:BroadcastService"
            var broadcastServiceConfig = new BroadcastServiceConfig();
            var broadcastServiceSection = _configuration.GetSection("Communications:BroadcastService");
            broadcastServiceSection.Bind(broadcastServiceConfig);
            ValidateConfig(broadcastServiceConfig, broadcastServiceSection);

            // MarketDataInitializerConfig - "WatchedMarkets:MarketDataInitializer"
            var marketDataInitializerConfig = new MarketDataInitializerConfig();
            var marketDataInitializerSection = _configuration.GetSection("WatchedMarkets:MarketDataInitializer");
            marketDataInitializerSection.Bind(marketDataInitializerConfig);
            ValidateConfig(marketDataInitializerConfig, marketDataInitializerSection);

            // CentralPerformanceMonitorConfig - "Central:CentralPerformanceMonitor"
            var centralPerformanceMonitorConfig = new CentralPerformanceMonitorConfig();
            var centralPerformanceMonitorSection = _configuration.GetSection("Central:CentralPerformanceMonitor");
            centralPerformanceMonitorSection.Bind(centralPerformanceMonitorConfig);
            ValidateConfig(centralPerformanceMonitorConfig, centralPerformanceMonitorSection);

            // KalshiBotScopeManagerServiceConfig - "Central:KalshiBotScopeManagerService"
            var kalshiBotScopeManagerServiceConfig = new KalshiBotScopeManagerServiceConfig();
            var kalshiBotScopeManagerServiceSection = _configuration.GetSection("Central:KalshiBotScopeManagerService");
            kalshiBotScopeManagerServiceSection.Bind(kalshiBotScopeManagerServiceConfig);
            ValidateConfig(kalshiBotScopeManagerServiceConfig, kalshiBotScopeManagerServiceSection);

            // CalculationsConfig
            var calculationConfig = new CalculationsConfig();
            var calculationSection = _configuration.GetSection(GetSectionName(typeof(CalculationsConfig)));
            calculationSection.Bind(calculationConfig);
            ValidateConfig(calculationConfig, calculationSection);

            // MarketDataConfig - "WatchedMarkets:MarketData"
            var marketDataConfig = new MarketServiceDataConfig();
            var marketDataSection = _configuration.GetSection("WatchedMarkets:MarketData");
            marketDataSection.Bind(marketDataConfig);
            marketDataConfig.Calculations = calculationConfig; // Set nested after binding
            ValidateConfig(marketDataConfig, marketDataSection);

            // CentralBrainConfig - "Central:CentralBrain"
            var centralBrainConfig = new CentralBrainConfig();
            var centralBrainSection = _configuration.GetSection("Central:CentralBrain");
            centralBrainSection.Bind(centralBrainConfig);
            ValidateConfig(centralBrainConfig, centralBrainSection);

            // TargetCalculationServiceConfig - "WatchedMarkets:TargetCalculationService"
            var targetCalculationServiceConfig = new TargetCalculationServiceConfig();
            var targetCalculationServiceSection = _configuration.GetSection("WatchedMarkets:TargetCalculationService");
            targetCalculationServiceSection.Bind(targetCalculationServiceConfig);
            ValidateConfig(targetCalculationServiceConfig, targetCalculationServiceSection);

            // BrainStatusServiceConfig - "Central:BrainStatusService"
            var brainStatusServiceConfig = new BrainStatusServiceConfig();
            var brainStatusServiceSection = _configuration.GetSection("Central:BrainStatusService");
            brainStatusServiceSection.Bind(brainStatusServiceConfig);
            ValidateConfig(brainStatusServiceConfig, brainStatusServiceSection);

            // SnapshotGroupHelperConfig - "SnapshotGroupHelper"
            var snapshotGroupHelperConfig = new SnapshotGroupHelperConfig();
            var snapshotGroupHelperSection = _configuration.GetSection("SnapshotGroupHelper");
            snapshotGroupHelperSection.Bind(snapshotGroupHelperConfig);
            ValidateConfig(snapshotGroupHelperConfig, snapshotGroupHelperSection);

            // QueueMonitoringConfig
            var queueMonitoringConfig = new QueueMonitoringConfig();
            var queueMonitoringSection = _configuration.GetSection(GetSectionName(typeof(QueueMonitoringConfig)));
            queueMonitoringSection.Bind(queueMonitoringConfig);
            ValidateConfig(queueMonitoringConfig, queueMonitoringSection);

            // InterestScoreConfig - "WatchedMarkets:InterestScore"
            var interestScoreConfig = new InterestScoreConfig();
            var interestScoreSection = _configuration.GetSection("WatchedMarkets:InterestScore");
            interestScoreSection.Bind(interestScoreConfig);
            ValidateConfig(interestScoreConfig, interestScoreSection);

            // ErrorHandlerConfig - "Central:ErrorHandler"
            var errorHandlerConfig = new ErrorHandlerConfig();
            var errorHandlerSection = _configuration.GetSection("Central:ErrorHandler");
            errorHandlerSection.Bind(errorHandlerConfig);
            ValidateConfig(errorHandlerConfig, errorHandlerSection);

            // OrderBookServiceConfig - "WatchedMarkets:OrderBookService"
            var orderBookServiceConfig = new OrderBookServiceConfig();
            var orderBookServiceSection = _configuration.GetSection("WatchedMarkets:OrderBookService");
            orderBookServiceSection.Bind(orderBookServiceConfig);
            ValidateConfig(orderBookServiceConfig, orderBookServiceSection);

            // BacklashBotDataConfig - "DBConnection:BacklashBotData"
            var backlashBotDataConfig = new BacklashBotDataConfig();
            var backlashBotDataSection = _configuration.GetSection("DBConnection:BacklashBotData");
            backlashBotDataSection.Bind(backlashBotDataConfig);
            ValidateConfig(backlashBotDataConfig, backlashBotDataSection);
        }

        [Test]
        public void ValidateAllConfigs_FromAppsettings_Valid_Reflective()
        {
            var configInstances = new Dictionary<string, object>();

            // Get all config types with SectionName from assemblies referenced by BacklashBot.csproj
            var assemblies = new[]
            {
                typeof(BacklashBotData.Configuration.BacklashBotDataConfig).Assembly, // BacklashBotData
                typeof(KalshiBotAPI.Configuration.KalshiAPIServiceConfig).Assembly, // KalshiBotAPI
                typeof(BacklashDTOs.Configuration.GeneralExecutionConfig).Assembly, // BacklashDTOs
                typeof(BacklashCommon.Configuration.SecretsConfig).Assembly, // BacklashCommon
                typeof(MarketServiceDataConfig).Assembly // BacklashBot itself
            };

            var configTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetField("SectionName", BindingFlags.Public | BindingFlags.Static) != null)
                .ToList();

            foreach (var configType in configTypes)
            {
                var sectionName = GetSectionName(configType);
                var section = _configuration.GetSection(sectionName);
                var instance = Activator.CreateInstance(configType);
                section.Bind(instance);
                configInstances[sectionName] = instance;
                ValidateConfig(instance, section);
            }

            // Special handling for nested configs
            if (configInstances.TryGetValue(MarketServiceDataConfig.SectionName, out var marketDataInstance) &&
                configInstances.TryGetValue(CalculationsConfig.SectionName, out var calculationsInstance))
            {
                ((MarketServiceDataConfig)marketDataInstance).Calculations = (CalculationsConfig)calculationsInstance;
                ValidateConfig(marketDataInstance, _configuration.GetSection(MarketServiceDataConfig.SectionName));
            }
        }

        [Test]
        public void ValidateNoUnusedSections_InAppsettings_Reflective()
        {
            var usedSections = new HashSet<string>();

            // Automatically collect all SectionName values from assemblies referenced by BacklashBot.csproj
            var assemblies = new[]
            {
                typeof(BacklashBotData.Configuration.BacklashBotDataConfig).Assembly, // BacklashBotData
                typeof(KalshiBotAPI.Configuration.KalshiAPIServiceConfig).Assembly, // KalshiBotAPI
                typeof(BacklashDTOs.Configuration.GeneralExecutionConfig).Assembly, // BacklashDTOs
                typeof(BacklashCommon.Configuration.SecretsConfig).Assembly, // BacklashCommon
                typeof(MarketServiceDataConfig).Assembly // BacklashBot itself
            };

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    var sectionNameField = type.GetField("SectionName", BindingFlags.Public | BindingFlags.Static);
                    if (sectionNameField != null)
                    {
                        usedSections.Add((string)sectionNameField.GetValue(null));
                    }
                }
            }

            // Add hardcoded sections not tied to configs
            usedSections.Add("DBConnection:DefaultConnection");

            var allConfigurationKeys = GetAllConfigurationKeys(_configuration);

            var unusedKeys = allConfigurationKeys.Where(key =>
                !usedSections.Any(used => key == used || key.StartsWith(used + ":") || used.StartsWith(key + ":"))
            ).ToList();

            TestContext.WriteLine("Reflective: Unused configuration keys found:");
            foreach (var key in unusedKeys)
            {
                TestContext.WriteLine($"  {key}");
            }

            Assert.That(unusedKeys, Is.Empty, $"Reflective: Unused configuration keys found in appsettings.json: {string.Join(", ", unusedKeys)}");
        }

        [Test]
        public void ValidateSecretsInterpolationAndKeyFileExists()
        {
            // Set up configuration with secrets loaded
            var basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot"));
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

            var baseConfig = builder.Build();

            // Debug: Check what the secrets path is
            var secretsPath = baseConfig.GetValue<string>("Secrets:SecretsPath") ?? "Secrets";
            TestContext.WriteLine($"Secrets path from config: {secretsPath}");

            builder.AddSecretsConfiguration(basePath, baseConfig);
            var configuration = builder.Build();

            // Debug: Check if secrets were loaded
            var botKeyId = configuration["Kalshi:BotKeyId"];
            var botKeyFile = configuration["Kalshi:BotKeyFile"];
            TestContext.WriteLine($"Kalshi:BotKeyId from config: {MaskKeyId(botKeyId)}");
            TestContext.WriteLine($"Kalshi:BotKeyFile from config: {botKeyFile}");

            // Test KalshiConfig binding (this will still have placeholders because binding doesn't interpolate)
            var kalshiConfig = new KalshiConfig();
            var kalshiSection = configuration.GetSection(KalshiConfig.SectionName);
            kalshiSection.Bind(kalshiConfig);

            TestContext.WriteLine($"KalshiConfig.KeyId (raw): {kalshiConfig.KeyId}");
            TestContext.WriteLine($"KalshiConfig.KeyFile (raw): {kalshiConfig.KeyFile}");

            // The raw binding will still have placeholders - this is expected
            // We need to test the interpolation separately
            Assert.That(kalshiConfig.KeyId, Does.Contain("{"),
                $"Raw KalshiConfig.KeyId should contain placeholders: {kalshiConfig.KeyId}");
            Assert.That(kalshiConfig.KeyFile, Does.Contain("{"),
                $"Raw KalshiConfig.KeyFile should contain placeholders: {kalshiConfig.KeyFile}");

            // Test manual interpolation using ConfigurationHelper
            var rawKeyId = configuration["Kalshi:KeyId"];
            var rawKeyFile = configuration["Kalshi:KeyFile"];
            TestContext.WriteLine($"Raw Kalshi:KeyId from config: {rawKeyId}");
            TestContext.WriteLine($"Raw Kalshi:KeyFile from config: {rawKeyFile}");

            var interpolatedKeyId = ConfigurationHelper.InterpolateConfigurationValue(rawKeyId, configuration);
            var interpolatedKeyFileName = ConfigurationHelper.InterpolateConfigurationValue(rawKeyFile, configuration);

            TestContext.WriteLine($"Interpolated KeyId: {interpolatedKeyId}");
            TestContext.WriteLine($"Interpolated KeyFile: {interpolatedKeyFileName}");

            // Verify interpolation worked
            Assert.That(interpolatedKeyId, Does.Not.Contain("{"),
                $"Interpolated KeyId should not contain placeholders: {interpolatedKeyId}");
            Assert.That(interpolatedKeyFileName, Does.Not.Contain("{"),
                $"Interpolated KeyFile should not contain placeholders: {interpolatedKeyFileName}");

            // Verify the interpolated values match the secrets
            Assert.That(interpolatedKeyId, Is.EqualTo(botKeyId),
                $"Interpolated KeyId should match secret value");
            Assert.That(interpolatedKeyFileName, Is.EqualTo(botKeyFile),
                $"Interpolated KeyFile should match secret value");

            // Additional test of interpolation method (using already declared rawKeyFile variable)
            var interpolatedKeyFileAgain = ConfigurationHelper.InterpolateConfigurationValue(rawKeyFile, configuration);

            Assert.That(interpolatedKeyFileAgain, Does.Not.Contain("{"),
                $"ConfigurationHelper.InterpolateConfigurationValue should interpolate placeholders but result contains: {interpolatedKeyFileAgain}");

            // Verify both interpolation calls produce the same result
            Assert.That(interpolatedKeyFileAgain, Is.EqualTo(interpolatedKeyFileName),
                "Multiple calls to InterpolateConfigurationValue should produce consistent results");

            // Verify the interpolated key file exists
            var keyFilePath = Path.Combine(secretsPath, interpolatedKeyFileName);
            TestContext.WriteLine($"Checking key file at: {keyFilePath}");
            Assert.That(File.Exists(keyFilePath),
                $"Interpolated key file should exist at: {keyFilePath}");

            TestContext.WriteLine($"✓ Secrets loaded successfully");
            TestContext.WriteLine($"✓ Interpolation working correctly");
            TestContext.WriteLine($"✓ Key file exists: {keyFilePath}");
            TestContext.WriteLine($"✓ Kalshi KeyId: {MaskKeyId(interpolatedKeyId)}");
            TestContext.WriteLine($"✓ Kalshi KeyFile: {interpolatedKeyFileName}");
        }

        [Test]
        public void ValidateNoUnusedSections_InAppsettings()
        {
            var usedSections = new HashSet<string>
            {
                SecretsConfig.SectionName,
                LoggingConfig.SectionName,
                KalshiConfig.SectionName,
                KalshiAPIServiceConfig.SectionName,
                WebSocketConnectionManagerConfig.SectionName,
                MessageProcessorConfig.SectionName,
                SubscriptionManagerConfig.SectionName,
                WebSocketMonitorConfig.SectionName,
                KalshiWebSocketClientConfig.SectionName,
                TradingSnapshotServiceConfig.SectionName,
                SnapshotPeriodHelperConfig.SectionName,
                OrderbookChangeTrackerConfig.SectionName,
                MarketRefreshServiceConfig.SectionName,
                PseudoCandlestickExtensionsConfig.SectionName,
                GeneralExecutionConfig.SectionName,
                OverseerClientServiceConfig.SectionName,
                CandlestickServiceConfig.SectionName,
                BroadcastServiceConfig.SectionName,
                MarketDataInitializerConfig.SectionName,
                CentralPerformanceMonitorConfig.SectionName,
                KalshiBotScopeManagerServiceConfig.SectionName,
                CalculationsConfig.SectionName,
                MarketServiceDataConfig.SectionName,
                CentralBrainConfig.SectionName,
                TargetCalculationServiceConfig.SectionName,
                BrainStatusServiceConfig.SectionName,
                SnapshotGroupHelperConfig.SectionName,
                InterestScoreConfig.SectionName,
                ErrorHandlerConfig.SectionName,
                OrderBookServiceConfig.SectionName,
                BacklashBotDataConfig.SectionName,
                QueueMonitoringConfig.SectionName,
                "DBConnection:DefaultConnection"
            };

            var allConfigurationKeys = GetAllConfigurationKeys(_configuration);

            var unusedKeys = allConfigurationKeys.Where(key =>
                !usedSections.Any(used => key == used || key.StartsWith(used + ":") || used.StartsWith(key + ":"))
            ).ToList();

            TestContext.WriteLine("Manual: Unused configuration keys found:");
            foreach (var key in unusedKeys)
            {
                TestContext.WriteLine($"  {key}");
            }

            Assert.That(unusedKeys, Is.Empty, $"Manual: Unused configuration keys found in appsettings.json: {string.Join(", ", unusedKeys)}");
        }

        private void ValidateConfig(object config, IConfigurationSection section)
        {
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(config, new ValidationContext(config), validationResults, true);

            // Check for missing properties in the configuration section
            var properties = config.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                var subSection = section.GetSection(prop.Name);
                bool isMissing = subSection.Value == null && !subSection.GetChildren().Any();

                if (isMissing)
                {
                    var requiredAttr = prop.GetCustomAttribute<RequiredAttribute>();
                    var message = requiredAttr != null
                        ? $"Required property '{prop.Name}' is missing in the configuration section '{section.Path}'."
                        : $"Property '{prop.Name}' is defined in the config class but missing in the configuration section '{section.Path}'.";
                    validationResults.Add(new ValidationResult(message, new[] { prop.Name }));
                }
            }

            Assert.That(isValid && validationResults.Count == 0, Is.True, $"Validation failed for {config.GetType().Name}: {string.Join(", ", validationResults.Select(r => r.ErrorMessage))}");
        }

        private string GetSectionName(Type configType)
        {
            return (string)configType.GetField("SectionName")?.GetValue(null) ?? throw new InvalidOperationException($"SectionName not found for {configType.Name}");
        }

        private List<string> GetAllConfigurationKeys(IConfiguration config, string prefix = "")
        {
            var keys = new List<string>();
            foreach (var child in config.GetChildren())
            {
                var currentPath = string.IsNullOrEmpty(prefix) ? child.Key : $"{prefix}:{child.Key}";
                keys.Add(currentPath);
                keys.AddRange(GetAllConfigurationKeys(child, currentPath));
            }
            return keys;
        }

        private string MaskKeyId(string keyId)
        {
            if (string.IsNullOrEmpty(keyId))
                return keyId;

            // Find the last dash and mask everything before it with asterisks
            var lastDashIndex = keyId.LastIndexOf('-');
            if (lastDashIndex >= 0 && lastDashIndex < keyId.Length - 1)
            {
                var visiblePart = keyId.Substring(lastDashIndex + 1);
                var maskedPart = new string('*', lastDashIndex + 1);
                return maskedPart + visiblePart;
            }

            // If no dash found, mask the entire string except last 4 characters
            if (keyId.Length > 4)
            {
                var visiblePart = keyId.Substring(keyId.Length - 4);
                var maskedPart = new string('*', keyId.Length - 4);
                return maskedPart + visiblePart;
            }

            return keyId; // Return as-is if too short
        }
    }
}