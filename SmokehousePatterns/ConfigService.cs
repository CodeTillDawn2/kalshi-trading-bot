using Microsoft.Extensions.Configuration;

namespace SmokehousePatterns
{
    public static class ConfigService
    {
        private static readonly IConfigurationRoot _configuration;

        static ConfigService()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

            _configuration = builder.Build();
        }

        // Existing methods
        public static string GetEnvironment() => _configuration["Environment"] ?? "dev";

        //public static string GetCandlestickRetrievalLength_Minute() =>
        //    _configuration.GetSection("CandlestickRetrievalLengths")["Minute"]?.ToString() ?? "60";
        //public static string GetCandlestickRetrievalLength_Hour() =>
        //    _configuration.GetSection("CandlestickRetrievalLengths")["Hour"]?.ToString() ?? "24";
        //public static string GetCandlestickRetrievalLength_Day() =>
        //    _configuration.GetSection("CandlestickRetrievalLengths")["Day"]?.ToString() ?? "7";

        // New specific pattern detection settings methods
        public static (int Lookback, int Lookforward) GetMinutePatternSettings() =>
            GetPatternDetectionSettings("Minute");

        public static (int Lookback, int Lookforward) GetHourPatternSettings() =>
            GetPatternDetectionSettings("Hour");

        public static (int Lookback, int Lookforward) GetDayPatternSettings() =>
            GetPatternDetectionSettings("Day");

        // Generic method for getting pattern settings
        public static (int Lookback, int Lookforward) GetPatternDetectionSettings(string intervalType)
        {
            if (string.IsNullOrWhiteSpace(intervalType))
                throw new ArgumentNullException(nameof(intervalType), "Interval type cannot be null or empty");

            var section = _configuration.GetSection("PatternDetectionSettings").GetSection(intervalType);

            if (!section.Exists())
                throw new ArgumentException($"No pattern detection settings found for interval type '{intervalType}'");

            string lookbackStr = section["Lookback"];
            string lookforwardStr = section["Lookforward"];

            if (!int.TryParse(lookbackStr, out int lookback))
                throw new InvalidOperationException(
                    $"Invalid or missing 'Lookback' value for interval type '{intervalType}'");

            if (!int.TryParse(lookforwardStr, out int lookforward))
                throw new InvalidOperationException(
                    $"Invalid or missing 'Lookforward' value for interval type '{intervalType}'");

            return (lookback, lookforward);
        }

        public static Dictionary<string, (int Lookback, int Lookforward)> GetAllPatternSettings()
        {
            var result = new Dictionary<string, (int Lookback, int Lookforward)>();
            var settingsSection = _configuration.GetSection("PatternDetectionSettings");

            foreach (var section in settingsSection.GetChildren())
            {
                if (int.TryParse(section["Lookback"], out int lookback) &&
                    int.TryParse(section["Lookforward"], out int lookforward))
                {
                    result[section.Key] = (lookback, lookforward);
                }
            }

            return result;
        }
    }
}