using BacklashBot.Configuration;
using BacklashBotData.Configuration;
using BacklashCommon.Configuration;
using KalshiBotAPI.Configuration;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using TradingGUI.Configuration;
using TradingStrategies.Configuration;

namespace TradingGUITests
{
    [TestFixture]
    public class ConfigValidationTests
    {
        private IConfiguration _configuration;

        [SetUp]
        public void SetUp()
        {
            var basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "TradingGUI"));
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

            _configuration = builder.Build();
        }

        [Test]
        public void ValidateAllConfigs_FromAppsettings_Valid_Reflective()
        {
            TestContext.WriteLine("Testing validation of all config classes from appsettings.json using reflection.");
            var configInstances = new Dictionary<string, object>();

            // Get all config types with SectionName from assemblies referenced by TradingGUI.csproj
            var assemblies = new[]
            {
                typeof(BacklashCommon.Configuration.SecretsConfig).Assembly, // BacklashCommon
                typeof(KalshiBotAPI.Configuration.KalshiConfig).Assembly, // KalshiBotAPI
                typeof(TradingStrategies.Configuration.DataLoaderConfig).Assembly, // TradingStrategies
                typeof(TradingGUI.Configuration.SnapshotViewerConfig).Assembly, // TradingGUI itself
                typeof(BacklashBotData.Configuration.BacklashBotDataConfig).Assembly, // BacklashBotData
                typeof(BacklashBot.Configuration.BrainStatusServiceConfig).Assembly // BacklashBot
            };

            var configTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetField("SectionName", BindingFlags.Public | BindingFlags.Static) != null)
                .ToList();

            TestContext.WriteLine($"Step: Found {configTypes.Count} config types with SectionName.");
            foreach (var configType in configTypes)
            {
                var sectionName = GetSectionName(configType);
                var section = _configuration.GetSection(sectionName);
                var instance = Activator.CreateInstance(configType);
                section.Bind(instance);
                configInstances[sectionName] = instance;
                TestContext.WriteLine($"Step: Validating config {configType.Name} from section {sectionName}.");
                ValidateConfig(instance, section);
            }
            TestContext.WriteLine("Result: All configs validated successfully.");
        }

        [Test]
        public void ValidateNoUnusedSections_InAppsettings_Reflective()
        {
            TestContext.WriteLine("Testing for unused configuration sections in appsettings.json using reflection.");
            var usedSections = new HashSet<string>();

            // Automatically collect all SectionName values from assemblies referenced by TradingGUI.csproj
            var assemblies = new[]
            {
                typeof(BacklashCommon.Configuration.SecretsConfig).Assembly, // BacklashCommon
                typeof(KalshiBotAPI.Configuration.KalshiConfig).Assembly, // KalshiBotAPI
                typeof(TradingStrategies.Configuration.DataLoaderConfig).Assembly, // TradingStrategies
                typeof(TradingGUI.Configuration.SnapshotViewerConfig).Assembly, // TradingGUI itself
                typeof(BacklashBotData.Configuration.BacklashBotDataConfig).Assembly, // BacklashBotData
                typeof(BacklashBot.Configuration.BrainStatusServiceConfig).Assembly // BacklashBot
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

            TestContext.WriteLine($"Step: Collected {usedSections.Count} used sections, found {allConfigurationKeys.Count} total keys.");
            TestContext.WriteLine("Reflective: Unused configuration keys found:");
            foreach (var key in unusedKeys)
            {
                TestContext.WriteLine($"  {key}");
            }

            if (unusedKeys.Any())
            {
                TestContext.WriteLine($"Error: Found {unusedKeys.Count} unused keys.");
            }
            Assert.That(unusedKeys, Is.Empty, $"Reflective: Unused configuration keys found in appsettings.json: {string.Join(", ", unusedKeys)}");
            TestContext.WriteLine("Result: No unused sections found.");
        }

        [Test]
        public void ValidateSecretsInterpolationAndKeyFileExists()
        {
            TestContext.WriteLine("Testing secrets interpolation and key file existence.");
            // Set up configuration with secrets loaded
            var basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "TradingGUI"));
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

            var baseConfig = builder.Build();

            // Debug: Check what the secrets path is
            var secretsPath = baseConfig.GetValue<string>("Secrets:SecretsPath") ?? "Secrets";
            TestContext.WriteLine($"Step: Secrets path resolved to {secretsPath}.");

            builder.AddSecretsConfiguration(basePath, baseConfig);
            var configuration = builder.Build();

            // Debug: Check if secrets were loaded
            var botKeyId = configuration["Kalshi:BotKeyId"];
            var botKeyFile = configuration["Kalshi:BotKeyFile"];
            TestContext.WriteLine($"Step: Secrets loaded - KeyId: {MaskKeyId(botKeyId)}, KeyFile: {botKeyFile}.");

            // Test KalshiConfig binding (this will still have placeholders because binding doesn't interpolate)
            var kalshiConfig = new KalshiConfig();
            var kalshiSection = configuration.GetSection(KalshiConfig.SectionName);
            kalshiSection.Bind(kalshiConfig);

            TestContext.WriteLine($"Step: Bound KalshiConfig - KeyId contains placeholders: {kalshiConfig.KeyId.Contains("{")}, KeyFile contains placeholders: {kalshiConfig.KeyFile.Contains("{")}.");

            // The raw binding will still have placeholders - this is expected
            // We need to test the interpolation separately
            Assert.That(kalshiConfig.KeyId, Does.Contain("{"),
                $"Raw KalshiConfig.KeyId should contain placeholders: {kalshiConfig.KeyId}");
            Assert.That(kalshiConfig.KeyFile, Does.Contain("{"),
                $"Raw KalshiConfig.KeyFile should contain placeholders: {kalshiConfig.KeyFile}");

            // Test manual interpolation using ConfigurationHelper
            var rawKeyId = configuration["Kalshi:KeyId"];
            var rawKeyFile = configuration["Kalshi:KeyFile"];
            TestContext.WriteLine($"Step: Retrieved raw values - KeyId: {rawKeyId}, KeyFile: {rawKeyFile}.");

            var interpolatedKeyId = ConfigurationHelper.InterpolateConfigurationValue(rawKeyId, configuration);
            var interpolatedKeyFileName = ConfigurationHelper.InterpolateConfigurationValue(rawKeyFile, configuration);

            TestContext.WriteLine($"Step: Interpolated values - KeyId: {MaskKeyId(interpolatedKeyId)}, KeyFile: {interpolatedKeyFileName}.");

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
            TestContext.WriteLine($"Step: Checking key file existence at {keyFilePath}.");
            Assert.That(File.Exists(keyFilePath),
                $"Interpolated key file should exist at: {keyFilePath}");

            TestContext.WriteLine("Result: Secrets interpolation and key file validation successful.");
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