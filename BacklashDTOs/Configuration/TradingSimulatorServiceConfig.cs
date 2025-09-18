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
        public required string CacheDirectory { get; set; }

        /// <summary>
        /// Gets or sets the timeout in seconds for processing market data during simulation.
        /// Default is 300 seconds (5 minutes).
        /// </summary>
        public int ProcessingTimeoutSeconds { get; set; } = 300; // 5 minutes default

        /// <summary>
        /// Gets or sets the file naming pattern for market data JSON files.
        /// Supports placeholders: {market}, {label}, {strategy}, {timestamp}.
        /// Default pattern: "{market}_{label}_{strategy}_{timestamp}.json"
        /// </summary>
        public string MarketDataFileNamePattern { get; set; } = "{market}_{label}_{strategy}_{timestamp}.json";

        /// <summary>
        /// Gets or sets the file naming pattern for ResearchBus CSV files.
        /// Supports placeholders: {label}, {timestamp}.
        /// Default pattern: "{label}_ResearchBus_{timestamp}.csv"
        /// </summary>
        public string ResearchBusFileNamePattern { get; set; } = "{label}_ResearchBus_{timestamp}.csv";

        /// <summary>
        /// Gets or sets the file naming pattern for best-fit report CSV files.
        /// Supports placeholders: {label}, {timestamp}.
        /// Default pattern: "{label}_BestFitReport_{timestamp}.csv"
        /// </summary>
        public string BestFitReportFileNamePattern { get; set; } = "{label}_BestFitReport_{timestamp}.csv";

        /// <summary>
        /// Gets or sets the percentage threshold for detecting widespread discrepancies in snapshots.
        /// Used to determine when discrepancies indicate a systemic issue.
        /// Default is 10%.
        /// </summary>
        public int DiscrepancyThresholdPercentage { get; set; } = 10; // Threshold for widespread discrepancies

    }
}