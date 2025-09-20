using BacklashBot.State.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace BacklashBot.State
{
    /// <summary>
    /// Manages the readiness status of different components within the Kalshi trading bot system.
    /// This class provides TaskCompletionSource objects to signal when various parts of the bot
    /// have completed their initialization or are ready for operation.
    /// </summary>
    /// <remarks>
    /// The readiness status system allows different components to signal their completion state:
    /// - InitializationCompleted: Signals when the bot's core initialization is finished
    /// - BrowserReady: Signals when browser-related components are ready (if applicable)
    ///
    /// Components can await these TaskCompletionSource objects to coordinate startup sequences,
    /// and can set the results to signal completion to waiting components.
    ///
    /// This class includes logging for state changes, input validation, and metrics collection
    /// for better debugging and monitoring.
    /// </remarks>
    public class KalshiBotReadyStatus : IBotReadyStatus
    {
        private readonly ILogger<KalshiBotReadyStatus> _logger;
        private readonly Meter _meter;
        private readonly Counter<long> _readinessStateChanges;
        private readonly Histogram<double> _readinessTiming;

        private DateTime _initializationStartTime = DateTime.UtcNow;
        private DateTime _browserStartTime = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the TaskCompletionSource that signals when the bot's core initialization is complete.
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
        /// Initializes a new instance of the KalshiBotReadyStatus class.
        /// Creates the initial TaskCompletionSource objects and sets up the default state.
        /// </summary>
        /// <param name="logger">The logger for tracking state changes and operations.</param>
        public KalshiBotReadyStatus(ILogger<KalshiBotReadyStatus> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize metrics
            _meter = new Meter("KalshiBot.ReadyStatus");
            _readinessStateChanges = _meter.CreateCounter<long>("readiness_state_changes", "count", "Number of readiness state changes");
            _readinessTiming = _meter.CreateHistogram<double>("readiness_timing", "ms", "Time taken for readiness operations");

            _logger.LogInformation("KalshiBotReadyStatus initialized");
        }

        /// <summary>
        /// Resets all readiness status indicators to their initial state.
        /// This creates new TaskCompletionSource objects and sets the initialization status to false.
        /// </summary>
        /// <remarks>
        /// This method is typically called when restarting the bot or when a reset is needed
        /// to clear all previous readiness signals and start fresh.
        /// </remarks>
        public void ResetAll()
        {
            _logger.LogInformation("Resetting all readiness status indicators");

            // Validate and dispose existing TaskCompletionSources
            if (InitializationCompleted != null && !InitializationCompleted.Task.IsCompleted)
            {
                _logger.LogWarning("Resetting InitializationCompleted while task is not completed");
            }
            if (BrowserReady != null && !BrowserReady.Task.IsCompleted)
            {
                _logger.LogWarning("Resetting BrowserReady while task is not completed");
            }

            InitializationCompleted = new TaskCompletionSource<bool>();
            BrowserReady = new TaskCompletionSource<bool>();

            // Set initial results
            InitializationCompleted.SetResult(false);
            BrowserReady.SetResult(false);

            _initializationStartTime = DateTime.UtcNow;
            _browserStartTime = DateTime.UtcNow;

            _readinessStateChanges.Add(1, new KeyValuePair<string, object?>("operation", "reset"));
            _logger.LogInformation("All readiness status indicators reset to initial state");
        }

        /// <summary>
        /// Sets the initialization completion status with validation and logging.
        /// </summary>
        /// <param name="result">The result to set for initialization completion.</param>
        /// <exception cref="ArgumentNullException">Thrown when TaskCompletionSource is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when trying to set result on already completed task.</exception>
        public void SetInitializationCompleted(bool result)
        {
            if (InitializationCompleted == null)
            {
                _logger.LogError("Attempted to set result on null InitializationCompleted TaskCompletionSource");
                throw new ArgumentNullException(nameof(InitializationCompleted));
            }

            if (InitializationCompleted.Task.IsCompleted)
            {
                _logger.LogWarning("Attempted to set result on already completed InitializationCompleted task");
                throw new InvalidOperationException("TaskCompletionSource is already completed");
            }

            var timing = (DateTime.UtcNow - _initializationStartTime).TotalMilliseconds;
            _readinessTiming.Record(timing, new KeyValuePair<string, object?>("operation", "initialization"));

            InitializationCompleted.SetResult(result);
            _readinessStateChanges.Add(1, new KeyValuePair<string, object?>("operation", "initialization_completed"));
            _logger.LogInformation("Initialization completed with result: {Result}, timing: {Timing}ms", result, timing);
        }

        /// <summary>
        /// Sets the browser ready status with validation and logging.
        /// </summary>
        /// <param name="result">The result to set for browser readiness.</param>
        /// <exception cref="ArgumentNullException">Thrown when TaskCompletionSource is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when trying to set result on already completed task.</exception>
        public void SetBrowserReady(bool result)
        {
            if (BrowserReady == null)
            {
                _logger.LogError("Attempted to set result on null BrowserReady TaskCompletionSource");
                throw new ArgumentNullException(nameof(BrowserReady));
            }

            if (BrowserReady.Task.IsCompleted)
            {
                _logger.LogWarning("Attempted to set result on already completed BrowserReady task");
                throw new InvalidOperationException("TaskCompletionSource is already completed");
            }

            var timing = (DateTime.UtcNow - _browserStartTime).TotalMilliseconds;
            _readinessTiming.Record(timing, new KeyValuePair<string, object?>("operation", "browser_ready"));

            BrowserReady.SetResult(result);
            _readinessStateChanges.Add(1, new KeyValuePair<string, object?>("operation", "browser_ready"));
            _logger.LogInformation("Browser ready with result: {Result}, timing: {Timing}ms", result, timing);
        }
    }
}
