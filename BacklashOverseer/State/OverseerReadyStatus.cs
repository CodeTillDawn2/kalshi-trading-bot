using BacklashBot.State.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.Metrics;

namespace BacklashOverseer.State
{
    /// <summary>
    /// Manages the readiness status of different components within the Kalshi overseer system.
    /// This class provides TaskCompletionSource objects to signal when various parts of the overseer
    /// have completed their initialization or are ready for operation, with comprehensive logging and metrics.
    /// </summary>
    /// <remarks>
    /// The readiness status system allows different components to signal their completion state:
    /// - InitializationCompleted: Signals when the overseer's core initialization is finished
    /// - BrowserReady: Signals when browser-related components are ready (if applicable)
    ///
    /// Components can await these TaskCompletionSource objects to coordinate startup sequences,
    /// and can set the results to signal completion to waiting components.
    ///
    /// Features include:
    /// - Comprehensive logging for all state changes and operations
    /// - Input validation to prevent null reference exceptions
    /// - Configurable default states through OverseerReadyConfig
    /// - Metrics collection for readiness timing and operation performance
    /// </remarks>
    public class OverseerReadyStatus : IBotReadyStatus
    {
        private readonly ILogger<OverseerReadyStatus> _logger;
        private readonly OverseerReadyConfig _config;
        private readonly Meter _meter;
        private readonly Histogram<double> _readinessTimingHistogram;

        /// <summary>
        /// Gets or sets the TaskCompletionSource that signals when the overseer's core initialization is complete.
        /// Components can await this task to wait for initialization to finish, or set its result to signal completion.
        /// </summary>
        /// <value>The TaskCompletionSource for initialization completion signaling.</value>
        public TaskCompletionSource<bool> InitializationCompleted { get; set; } = new TaskCompletionSource<bool>();

        /// <summary>
        /// Gets or sets the TaskCompletionSource that signals when browser-related components are ready.
        /// This is used for components that depend on browser initialization or UI readiness.
        /// </summary>
        /// <value>The TaskCompletionSource for browser readiness signaling.</value>
        public TaskCompletionSource<bool> BrowserReady { get; set; } = new TaskCompletionSource<bool>();

        /// <summary>
        /// Initializes a new instance of the OverseerReadyStatus class.
        /// Creates the initial TaskCompletionSource objects and sets up the default state.
        /// </summary>
        /// <param name="logger">The logger for tracking readiness state changes.</param>
        /// <param name="config">The configuration for default readiness states.</param>
        public OverseerReadyStatus(ILogger<OverseerReadyStatus> logger, IOptions<OverseerReadyConfig> config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config?.Value ?? new OverseerReadyConfig
            {
                DefaultInitializationState = false,
                DefaultBrowserReadyState = false
            };
            _meter = new Meter("BacklashOverseer.ReadyStatus");
            _readinessTimingHistogram = _meter.CreateHistogram<double>("readiness_timing", unit: "ms", description: "Timing for readiness state changes");
            ResetAll();
        }

        /// <summary>
        /// Resets all readiness status indicators to their initial state.
        /// This creates new TaskCompletionSource objects and sets the initialization status to the configured default.
        /// </summary>
        /// <remarks>
        /// This method is typically called when restarting the overseer or when a reset is needed
        /// to clear all previous readiness signals and start fresh.
        /// </remarks>
        public void ResetAll()
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Resetting all readiness status indicators.");

            InitializationCompleted = new TaskCompletionSource<bool>();
            if (InitializationCompleted != null)
            {
                InitializationCompleted.SetResult(_config.DefaultInitializationState);
                _logger.LogDebug("InitializationCompleted set to {State}.", _config.DefaultInitializationState);
            }

            BrowserReady = new TaskCompletionSource<bool>();
            if (BrowserReady != null)
            {
                BrowserReady.SetResult(_config.DefaultBrowserReadyState);
                _logger.LogDebug("BrowserReady set to {State}.", _config.DefaultBrowserReadyState);
            }

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _readinessTimingHistogram.Record(duration);
            _logger.LogInformation("Readiness status reset completed in {Duration}ms.", duration);
        }
    }
}
