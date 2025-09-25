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
    /// <summary>
    /// Test fixture for validating configuration settings in the TradingGUI application.
    /// </summary>
    /// <remarks>
    /// Tests ensure that all configuration classes are properly bound from appsettings.json,
    /// no unused configuration sections exist, and secrets interpolation works correctly.
    /// </remarks>
    [TestFixture]
    public class ConfigValidationTests
    {
        private IConfiguration _configuration;

        /// <summary>
        /// Sets up the test fixture by loading the configuration from appsettings.json.
        /// </summary>
        /// <remarks>
        /// This method is called before each test method to initialize the configuration builder and load the appsettings.json file.
        /// </remarks>
        [SetUp]
        public void SetUp()
        {
            var basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "TradingGUI"));
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

            _configuration = builder.Build();
        }

        /// <summary>
        /// Validates that all configuration classes can be successfully bound from appsettings.json
        /// using reflection to discover config types with SectionName fields.
        /// </summary>
        /// <remarks>
        /// This test method uses reflection to find all config types with SectionName fields from referenced assemblies,
        /// binds them from the configuration, and validates that the binding is successful and all required properties are present.
        /// </remarks>
        [Test]
        public void ValidateAllConfigs_FromAppsettings_Valid_Reflective()
        {
            TestContext.Out.WriteLine("Testing validation of all config classes from appsettings.json using reflection.");
            var configInstances = new Dictionary<string, object>();

            // Get all config types with SectionName from assemblies referenced by TradingGUI.csproj
            var assemblies = new[]
            {
                typeof(SecretsConfig).Assembly, // BacklashCommon
                typeof(KalshiConfig).Assembly, // KalshiBotAPI
                typeof(DataLoaderConfig).Assembly, // TradingStrategies
                typeof(SnapshotViewerConfig).Assembly, // TradingGUI itself
                typeof(BacklashBotDataConfig).Assembly, // BacklashBotData
            };

            var configTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetField("SectionName", BindingFlags.Public | BindingFlags.Static) != null)
                .ToList();

            TestContext.Out.WriteLine($"Step: Found {configTypes.Count} config types with SectionName.");
            foreach (var configType in configTypes)
            {
                var sectionName = GetSectionName(configType);
                var section = _configuration.GetSection(sectionName);
                var instance = Activator.CreateInstance(configType);
                section.Bind(instance);
                configInstances[sectionName] = instance;
                TestContext.Out.WriteLine($"Step: Validating config {configType.Name} from section {sectionName}.");
                ValidateConfig(instance, section);
            }
            TestContext.Out.WriteLine("Result: All configs validated successfully.");
        }

        /// <summary>
        /// Validates that there are no unused configuration sections in appsettings.json
        /// by comparing all configuration keys against known SectionName values from config classes.
        /// </summary>
        /// <remarks>
        /// This test method collects all SectionName values from config classes using reflection,
        /// retrieves all configuration keys from appsettings.json, and asserts that no unused sections exist.
        /// </remarks>
        [Test]
        public void ValidateNoUnusedSections_InAppsettings_Reflective()
        {
            TestContext.Out.WriteLine("Testing for unused configuration sections in appsettings.json using reflection.");
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

            TestContext.Out.WriteLine($"Step: Collected {usedSections.Count} used sections, found {allConfigurationKeys.Count} total keys.");
            TestContext.Out.WriteLine("Reflective: Unused configuration keys found:");
            foreach (var key in unusedKeys)
            {
                TestContext.Out.WriteLine($"  {key}");
            }

            if (unusedKeys.Any())
            {
                TestContext.Out.WriteLine($"Error: Found {unusedKeys.Count} unused keys.");
            }
            Assert.That(unusedKeys, Is.Empty, $"Reflective: Unused configuration keys found in appsettings.json: {string.Join(", ", unusedKeys)}");
            TestContext.Out.WriteLine("Result: No unused sections found.");
        }

        /// <summary>
        /// Validates that secrets interpolation works correctly and that the interpolated key file exists.
        /// </summary>
        /// <remarks>
        /// Tests the ConfigurationHelper.InterpolateConfigurationValue method and verifies
        /// that secrets are properly loaded and interpolated from configuration placeholders.
        /// Also checks that the interpolated key file path exists on the file system.
        /// </remarks>
        [Test]
        public void ValidateSecretsInterpolationAndKeyFileExists()
        {
            TestContext.Out.WriteLine("Testing secrets interpolation and key file existence.");
            // Set up configuration with secrets loaded
            var basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "TradingGUI"));
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

            var baseConfig = builder.Build();

            // Debug: Check what the secrets path is
            var secretsPath = baseConfig.GetValue<string>("Secrets:SecretsPath") ?? "Secrets";
            TestContext.Out.WriteLine($"Step: Secrets path resolved to {secretsPath}.");

            builder.AddSecretsConfiguration(basePath, baseConfig);
            var configuration = builder.Build();

            // Debug: Check if secrets were loaded
            var botKeyId = configuration["Kalshi:BotKeyId"];
            var botKeyFile = configuration["Kalshi:BotKeyFile"];
            TestContext.Out.WriteLine($"Step: Secrets loaded - KeyId: {MaskKeyId(botKeyId)}, KeyFile: {botKeyFile}.");

            // Test KalshiConfig binding (this will still have placeholders because binding doesn't interpolate)
            var kalshiConfig = new KalshiConfig();
            var kalshiSection = configuration.GetSection(KalshiConfig.SectionName);
            kalshiSection.Bind(kalshiConfig);

            TestContext.Out.WriteLine($"Step: Bound KalshiConfig - KeyId contains placeholders: {kalshiConfig.KeyId.Contains("{")}, KeyFile contains placeholders: {kalshiConfig.KeyFile.Contains("{")}.");

            // The raw binding will still have placeholders - this is expected
            // We need to test the interpolation separately
            Assert.That(kalshiConfig.KeyId, Does.Contain("{"),
                $"Raw KalshiConfig.KeyId should contain placeholders: {kalshiConfig.KeyId}");
            Assert.That(kalshiConfig.KeyFile, Does.Contain("{"),
                $"Raw KalshiConfig.KeyFile should contain placeholders: {kalshiConfig.KeyFile}");

            // Test manual interpolation using ConfigurationHelper
            var rawKeyId = configuration["Kalshi:KeyId"];
            var rawKeyFile = configuration["Kalshi:KeyFile"];
            TestContext.Out.WriteLine($"Step: Retrieved raw values - KeyId: {rawKeyId}, KeyFile: {rawKeyFile}.");

            var interpolatedKeyId = ConfigurationHelper.InterpolateConfigurationValue(rawKeyId, configuration);
            var interpolatedKeyFileName = ConfigurationHelper.InterpolateConfigurationValue(rawKeyFile, configuration);

            TestContext.Out.WriteLine($"Step: Interpolated values - KeyId: {MaskKeyId(interpolatedKeyId)}, KeyFile: {interpolatedKeyFileName}.");

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
            TestContext.Out.WriteLine($"Step: Checking key file existence at {keyFilePath}.");
            Assert.That(File.Exists(keyFilePath),
                $"Interpolated key file should exist at: {keyFilePath}");

            TestContext.Out.WriteLine("Result: Secrets interpolation and key file validation successful.");
        }
        /// <summary>
        /// Validates a configuration object using data annotations and checks for missing properties in the configuration section.
        /// </summary>
        /// <param name="config">The configuration object to validate.</param>
        /// <param name="section">The configuration section to check for missing properties.</param>
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

        /// <summary>
        /// Gets the SectionName field value from a configuration type.
        /// </summary>
        /// <param name="configType">The configuration type that contains the SectionName field.</param>
        /// <returns>The section name string.</returns>
        private string GetSectionName(Type configType)
        {
            return (string)configType.GetField("SectionName")?.GetValue(null) ?? throw new InvalidOperationException($"SectionName not found for {configType.Name}");
        }

        /// <summary>
        /// Recursively retrieves all configuration keys from the provided configuration.
        /// </summary>
        /// <param name="config">The configuration to extract keys from.</param>
        /// <param name="prefix">The prefix to prepend to the keys (used for recursion).</param>
        /// <returns>A list of all configuration keys with their full paths.</returns>
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

        /// <summary>
        /// Masks a key ID string for secure logging by replacing most characters with asterisks.
        /// </summary>
        /// <param name="keyId">The key ID to mask.</param>
        /// <returns>The masked key ID, showing only the last part or last few characters.</returns>
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