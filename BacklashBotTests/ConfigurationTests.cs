using BacklashBot.Configuration;
using BacklashBot.Services;
using BacklashBot.State;
using BacklashBotData.Configuration;
using BacklashCommon.Configuration;
using KalshiBotAPI.Configuration;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using TradingStrategies.Configuration;

namespace BacklashBotTests
{
    /// <summary>
    /// Test fixture for validating configuration settings and secrets interpolation.
    /// </summary>
    [TestFixture]
    public class ConfigurationTests
    {
        private IConfiguration _configuration;

        /// <summary>
        /// Sets up the test fixture by loading the configuration from appsettings.json.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            var basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot"));
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

            _configuration = builder.Build();
        }

        /// <summary>
        /// Validates that all configuration classes can be successfully bound from appsettings.json
        /// using reflection to discover config types with SectionName fields.
        /// </summary>
        [Test]
        public void ValidateAllConfigs_FromAppsettings_Valid_Reflective()
        {
            TestContext.Out.WriteLine("Testing validation of all configurations from appsettings.json using reflection.");
            var configInstances = new Dictionary<string, object>();

            // Get all config types with SectionName from assemblies referenced by BacklashBot.csproj
            var assemblies = new[] {
                typeof(BacklashBotDataConfig).Assembly, // BacklashBotData
                typeof(KalshiAPIServiceConfig).Assembly, // KalshiBotAPI
                typeof(GeneralExecutionConfig).Assembly, // BacklashDTOs
                typeof(SecretsConfig).Assembly, // BacklashCommon
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

            TestContext.Out.WriteLine("Result: All configurations validated successfully.");
        }

        /// <summary>
        /// Validates that there are no unused configuration sections in appsettings.json
        /// by comparing all configuration keys against known SectionName values from config classes.
        /// </summary>
        [Test]
        public void ValidateNoUnusedSections_InAppsettings_Reflective()
        {
            TestContext.Out.WriteLine("Testing for unused configuration sections in appsettings.json using reflection.");
            var usedSections = new HashSet<string>();

            // Automatically collect all SectionName values from assemblies referenced by BacklashBot.csproj
            var assemblies = new[] {
                typeof(BacklashBotDataConfig).Assembly, // BacklashBotData
                typeof(KalshiAPIServiceConfig).Assembly, // KalshiBotAPI
                typeof(SecretsConfig).Assembly, // BacklashCommon
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

            TestContext.Out.WriteLine("Reflective: Unused configuration keys found:");
            foreach (var key in unusedKeys)
            {
                TestContext.Out.WriteLine($"  {key}");
            }

            Assert.That(unusedKeys, Is.Empty, $"Reflective: Unused configuration keys found in appsettings.json: {string.Join(", ", unusedKeys)}");
            TestContext.Out.WriteLine("Result: No unused configuration sections found.");
        }

        /// <summary>
        /// Validates that secrets interpolation works correctly and that the interpolated key file exists.
        /// Tests the ConfigurationHelper.InterpolateConfigurationValue method and verifies
        /// that secrets are properly loaded and interpolated from configuration placeholders.
        /// </summary>
        [Test]
        public void ValidateSecretsInterpolationAndKeyFileExists()
        {
            TestContext.Out.WriteLine("Testing secrets interpolation and key file existence.");
            // Set up configuration with secrets loaded
            var basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot"));
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

            var baseConfig = builder.Build();

            // Debug: Check what the secrets path is
            var secretsPath = baseConfig.GetValue<string>("Secrets:SecretsPath") ?? "Secrets";
            TestContext.Out.WriteLine($"Secrets path from config: {secretsPath}");

            builder.AddSecretsConfiguration(basePath, baseConfig);
            var configuration = builder.Build();

            // Debug: Check if secrets were loaded
            var botKeyId = configuration["Kalshi:BotKeyId"];
            var botKeyFile = configuration["Kalshi:BotKeyFile"];
            TestContext.Out.WriteLine($"Kalshi:BotKeyId from config: {MaskKeyId(botKeyId)}");
            TestContext.Out.WriteLine($"Kalshi:BotKeyFile from config: {botKeyFile}");

            // Test KalshiConfig binding (this will still have placeholders because binding doesn't interpolate)
            var kalshiConfig = new KalshiConfig();
            var kalshiSection = configuration.GetSection(KalshiConfig.SectionName);
            kalshiSection.Bind(kalshiConfig);

            TestContext.Out.WriteLine($"KalshiConfig.KeyId (raw): {kalshiConfig.KeyId}");
            TestContext.Out.WriteLine($"KalshiConfig.KeyFile (raw): {kalshiConfig.KeyFile}");

            // The raw binding will still have placeholders - this is expected
            Assert.That(kalshiConfig.KeyId, Does.Contain("{"),
                $"Raw KalshiConfig.KeyId should contain placeholders: {kalshiConfig.KeyId}");
            Assert.That(kalshiConfig.KeyFile, Does.Contain("{"),
                $"Raw KalshiConfig.KeyFile should contain placeholders: {kalshiConfig.KeyFile}");

            // Test manual interpolation using ConfigurationHelper
            var rawKeyId = configuration["Kalshi:KeyId"];
            var rawKeyFile = configuration["Kalshi:KeyFile"];
            TestContext.Out.WriteLine($"Raw Kalshi:KeyId from config: {rawKeyId}");
            TestContext.Out.WriteLine($"Raw Kalshi:KeyFile from config: {rawKeyFile}");

            var interpolatedKeyId = ConfigurationHelper.InterpolateConfigurationValue(rawKeyId, configuration);
            var interpolatedKeyFileName = ConfigurationHelper.InterpolateConfigurationValue(rawKeyFile, configuration);

            TestContext.Out.WriteLine($"Interpolated KeyId: {interpolatedKeyId}");
            TestContext.Out.WriteLine($"Interpolated KeyFile: {interpolatedKeyFileName}");

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
            TestContext.Out.WriteLine($"Checking key file at: {keyFilePath}");
            Assert.That(File.Exists(keyFilePath),
                $"Interpolated key file should exist at: {keyFilePath}");

            TestContext.Out.WriteLine($"✓ Secrets loaded successfully");
            TestContext.Out.WriteLine($"✓ Interpolation working correctly");
            TestContext.Out.WriteLine($"✓ Key file exists: {keyFilePath}");
            TestContext.Out.WriteLine($"✓ Kalshi KeyId: {MaskKeyId(interpolatedKeyId)}");
            TestContext.Out.WriteLine($"✓ Kalshi KeyFile: {interpolatedKeyFileName}");
            TestContext.Out.WriteLine("Result: Secrets interpolation and key file validation completed successfully.");
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