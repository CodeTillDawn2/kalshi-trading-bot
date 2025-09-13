using BacklashDTOs.Configuration;
using BacklashDTOs.Data;
using BacklashDTOs;
using BacklashBot.Management.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BacklashBot.Management
{
    /// <summary>
    /// Service for calculating optimal target number of markets to watch based on performance metrics.
    /// Implements configurable queue limits and provides detailed logging of target calculations.
    /// </summary>
    public class TargetCalculationService : ITargetCalculationService
    {
        private readonly ILogger<TargetCalculationService> _logger;
        private readonly ExecutionConfig _executionConfig;

        /// <summary>
        /// Initializes a new instance of the TargetCalculationService class.
        /// </summary>
        /// <param name="logger">Logger for recording target calculation operations</param>
        /// <param name="executionConfig">Configuration options containing queue limits</param>
        public TargetCalculationService(ILogger<TargetCalculationService> logger, IOptions<ExecutionConfig> executionConfig)
        {
            _logger = logger;
            _executionConfig = executionConfig.Value;
        }

        /// <summary>
        /// Calculates the optimal target number of markets to watch based on current performance metrics.
        /// Uses multiple factors including usage targets, queue sizes, and notification patterns to
        /// determine the ideal market count. Returns the minimum valid target from all calculated options.
        /// </summary>
        /// <param name="metrics">Current performance metrics including usage, counts, and queue sizes</param>
        /// <param name="brain">Brain instance configuration containing usage limits and targets</param>
        /// <returns>The calculated target number of markets to watch</returns>
        public int CalculateTarget(PerformanceMetrics metrics, BrainInstanceDTO brain)
        {
            const int MaxValidValue = int.MaxValue;
            const int MinValidValue = 0; // Assuming non-negative targets

            // Validate brain configuration
            if (brain == null)
            {
                _logger.LogWarning("Brain configuration is null, using default target of 10");
                return 10;
            }

            if (brain.UsageTarget <= 0)
            {
                _logger.LogWarning("Invalid UsageTarget in brain configuration: {UsageTarget}. Using default target of 10", brain.UsageTarget);
                return 10;
            }

            // Validate queue limits from config
            if (_executionConfig.NotificationQueueLimit <= 0)
            {
                _logger.LogWarning("Invalid NotificationQueueLimit in configuration: {Limit}. Using default of 50", _executionConfig.NotificationQueueLimit);
                _executionConfig.NotificationQueueLimit = 50;
            }

            if (_executionConfig.OrderbookQueueLimit <= 0)
            {
                _logger.LogWarning("Invalid OrderbookQueueLimit in configuration: {Limit}. Using default of 50", _executionConfig.OrderbookQueueLimit);
                _executionConfig.OrderbookQueueLimit = 50;
            }

            if (_executionConfig.EventQueueLimit <= 0)
            {
                _logger.LogWarning("Invalid EventQueueLimit in configuration: {Limit}. Using default of 50", _executionConfig.EventQueueLimit);
                _executionConfig.EventQueueLimit = 50;
            }

            if (_executionConfig.TickerQueueLimit <= 0)
            {
                _logger.LogWarning("Invalid TickerQueueLimit in configuration: {Limit}. Using default of 50", _executionConfig.TickerQueueLimit);
                _executionConfig.TickerQueueLimit = 50;
            }

            // Helper function to calculate target and validate result
            int CalculateAndValidateTarget(double limit, double avg, int count)
            {
                if (count == 0 || avg == 0) return MaxValidValue; // Skip invalid cases
                double perEachUsage = avg / count;
                if (perEachUsage == 0) return MaxValidValue; // Avoid division by zero
                double result = limit / perEachUsage;
                int target = (int)Math.Floor(result);
                if (target <= MinValidValue || target == int.MinValue) return MaxValidValue; // Skip overflow/invalid
                return target;
            }

            // Target count by usage
            int targetCountUsage = CalculateAndValidateTarget(brain.UsageTarget, metrics.CurrentUsage, metrics.CurrentCount);

            // Target count by Notification Queue
            int targetCountNotificationQueue = CalculateAndValidateTarget(_executionConfig.NotificationQueueLimit, metrics.NotificationQueueAvg, metrics.CurrentCount);

            // Target count by Orderbook Queue
            int targetCountOrderbookQueue = CalculateAndValidateTarget(_executionConfig.OrderbookQueueLimit, metrics.OrderbookQueueAvg, metrics.CurrentCount);

            // Target count by Event Queue
            int targetCountEventQueue = CalculateAndValidateTarget(_executionConfig.EventQueueLimit, metrics.EventQueueAvg, metrics.CurrentCount);

            // Target count by Ticker Queue
            int targetCountTickerQueue = CalculateAndValidateTarget(_executionConfig.TickerQueueLimit, metrics.TickerQueueAvg, metrics.CurrentCount);

            // Collect valid targets
            var validTargets = new List<int>();
            if (targetCountUsage < MaxValidValue) validTargets.Add(targetCountUsage);
            if (targetCountNotificationQueue < MaxValidValue) validTargets.Add(targetCountNotificationQueue);
            if (targetCountOrderbookQueue < MaxValidValue) validTargets.Add(targetCountOrderbookQueue);
            if (targetCountEventQueue < MaxValidValue) validTargets.Add(targetCountEventQueue);
            if (targetCountTickerQueue < MaxValidValue) validTargets.Add(targetCountTickerQueue);

            // Final target: Use minimum of valid targets, or start with 10 if none are valid
            int actualTarget = validTargets.Any() ? validTargets.Min() : 10;

            _logger.LogInformation("Calculated market targets - Usage: {Usage}, Notification: {Notification}, Orderbook: {Orderbook}, Event: {Event}, Ticker: {Ticker}, Final: {Selected}",
                targetCountUsage, targetCountNotificationQueue, targetCountOrderbookQueue, targetCountEventQueue, targetCountTickerQueue, actualTarget);

            return actualTarget;
        }
    }
}