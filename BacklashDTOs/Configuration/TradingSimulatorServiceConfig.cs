using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration class for trading simulator settings.
    /// </summary>
    public class TradingSimulatorServiceConfig
    {
        /// <summary>
        /// The configuration section name for TradingSimulatorServiceConfig.
        /// </summary>
        public const string SectionName = "TradingSimulatorService";

        /// <summary>
        /// Gets or sets the directory path where simulation cache files and reports are stored.
        /// This directory is created automatically if it doesn't exist.
        /// </summary>
        [Required(ErrorMessage = "The 'CacheDirectory' is missing in the configuration.")]
        public string CacheDirectory { get; set; } = null!;

        /// <summary>
        /// Gets or sets the timeout in seconds for processing market data during simulation.
        /// Default is 300 seconds (5 minutes).
        /// </summary>
        [Required(ErrorMessage = "The 'ProcessingTimeoutSeconds' is missing in the configuration.")]
        public int ProcessingTimeoutSeconds { get; set; }

        /// <summary>
        /// Gets or sets the file naming pattern for market data JSON files.
        /// Supports placeholders: {market}, {label}, {strategy}, {timestamp}.
        /// Default pattern: "{market}_{label}_{strategy}_{timestamp}.json"
        /// </summary>
        [Required(ErrorMessage = "The 'MarketDataFileNamePattern' is missing in the configuration.")]
        public string MarketDataFileNamePattern { get; set; } = null!;

        /// <summary>
        /// Gets or sets the file naming pattern for ResearchBus CSV files.
        /// Supports placeholders: {label}, {timestamp}.
        /// Default pattern: "{label}_ResearchBus_{timestamp}.csv"
        /// </summary>
        [Required(ErrorMessage = "The 'ResearchBusFileNamePattern' is missing in the configuration.")]
        public string ResearchBusFileNamePattern { get; set; } = null!;

        /// <summary>
        /// Gets or sets the file naming pattern for best-fit report CSV files.
        /// Supports placeholders: {label}, {timestamp}.
        /// Default pattern: "{label}_BestFitReport_{timestamp}.csv"
        /// </summary>
        [Required(ErrorMessage = "The 'BestFitReportFileNamePattern' is missing in the configuration.")]
        public string BestFitReportFileNamePattern { get; set; } = null!;

        /// <summary>
        /// Gets or sets the percentage threshold for detecting widespread discrepancies in snapshots.
        /// Used to determine when discrepancies indicate a systemic issue.
        /// Default is 10%.
        /// </summary>
        [Required(ErrorMessage = "The 'DiscrepancyThresholdPercentage' is missing in the configuration.")]
        public int DiscrepancyThresholdPercentage { get; set; }

    }
}
