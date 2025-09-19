using System.Text.Json.Serialization;

namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration class for trading simulator settings.
    /// </summary>
    public class TradingSimulatorServiceConfig
    {
        /// <summary>
        /// Gets or sets the directory path where simulation cache files and reports are stored.
        /// This directory is created automatically if it doesn't exist.
        /// </summary>
        [JsonRequired]
        public string CacheDirectory { get; set; }

        /// <summary>
        /// Gets or sets the timeout in seconds for processing market data during simulation.
        /// Default is 300 seconds (5 minutes).
        /// </summary>
        [JsonRequired]
        public int ProcessingTimeoutSeconds { get; set; }

        /// <summary>
        /// Gets or sets the file naming pattern for market data JSON files.
        /// Supports placeholders: {market}, {label}, {strategy}, {timestamp}.
        /// Default pattern: "{market}_{label}_{strategy}_{timestamp}.json"
        /// </summary>
        [JsonRequired]
        public string MarketDataFileNamePattern { get; set; }

        /// <summary>
        /// Gets or sets the file naming pattern for ResearchBus CSV files.
        /// Supports placeholders: {label}, {timestamp}.
        /// Default pattern: "{label}_ResearchBus_{timestamp}.csv"
        /// </summary>
        [JsonRequired]
        public string ResearchBusFileNamePattern { get; set; }

        /// <summary>
        /// Gets or sets the file naming pattern for best-fit report CSV files.
        /// Supports placeholders: {label}, {timestamp}.
        /// Default pattern: "{label}_BestFitReport_{timestamp}.csv"
        /// </summary>
        [JsonRequired]
        public string BestFitReportFileNamePattern { get; set; }

        /// <summary>
        /// Gets or sets the percentage threshold for detecting widespread discrepancies in snapshots.
        /// Used to determine when discrepancies indicate a systemic issue.
        /// Default is 10%.
        /// </summary>
        [JsonRequired]
        public int DiscrepancyThresholdPercentage { get; set; }

    }
}