using BacklashDTOs;
using BacklashInterfaces.PerformanceMetrics;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using TradingStrategies.Configuration;

namespace TradingStrategies.Trading.Overseer
{
    /// <summary>
    /// Provides methods for calculating the total equity value of a trading simulation path.
    /// This class is responsible for determining the current market value of positions held in a simulated trading environment,
    /// taking into account both cash holdings and the unrealized value of open positions based on current order book data.
    /// It includes performance metrics collection for timing analysis and async versions for high-throughput scenarios.
    /// </summary>
    /// <remarks>
    /// The EquityCalculator is a critical component in the trading simulation pipeline, used by the StrategySimulation engine
    /// to evaluate the performance of trading strategies. It handles different market conditions (natural vs. non-natural markets)
    /// and provides accurate equity calculations that reflect real-world trading mechanics.
    ///
    /// Key responsibilities:
    /// - Calculate total equity as cash plus position value
    /// - Handle natural markets where one side has no liquidity
    /// - Use mid-prices for non-natural markets to estimate fair value
    /// - Support both long and short positions
    /// - Collect performance metrics for calculation timing to identify bottlenecks in high-frequency simulations
    /// - Provide async versions of calculation methods for better performance in high-throughput scenarios
    ///
    /// This class collects performance metrics and provides both synchronous and asynchronous calculation methods.
    /// While the calculation logic itself is thread-safe, the performance metrics collection should be used carefully
    /// in highly concurrent scenarios. The async methods help improve performance in high-throughput environments.
    /// </remarks>
    public class EquityCalculator
    {
        private readonly EquityCalculatorConfig _config;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly List<long> _calculationTimes = new List<long>();

        /// <summary>
        /// Initializes a new instance of the EquityCalculator class.
        /// </summary>
        /// <param name="config">The equity calculator configuration containing performance metrics settings.</param>
        /// <param name="performanceMonitor">The performance monitor for recording metrics.</param>
        public EquityCalculator(IOptions<EquityCalculatorConfig> config, IPerformanceMonitor performanceMonitor)
        {
            _config = config.Value ?? throw new ArgumentNullException(nameof(config));
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
        }

        /// <summary>
        /// Calculates the total equity value for a given simulation path at the current market snapshot.
        /// </summary>
        /// <param name="path">The simulation path containing cash, position, and order book state.</param>
        /// <param name="lastSnapshot">The most recent market snapshot containing current market data (used for context but not directly for calculation).</param>
        /// <returns>The total equity value in dollars, including cash and the current market value of all positions.</returns>
        /// <remarks>
        /// This method implements the core equity calculation logic used throughout the trading simulation system.
        /// The calculation follows these steps:
        ///
        /// 1. Start with the cash balance from the simulation path
        /// 2. If no simulated order book exists, return cash only
        /// 3. Extract best bid/ask prices from the simulated order book
        /// 4. Determine if the market is "natural" (one side has no bids)
        /// 5. For natural markets:
        ///    - Long positions (positive): Value at 1.0 if no bids on the short side, 0.0 otherwise
        ///    - Short positions (negative): Value at 1.0 if no bids on the long side, 0.0 otherwise
        /// 6. For non-natural markets:
        ///    - Use mid-prices (average of best bid and ask) to value positions
        ///    - Long positions valued at mid-price of the "Yes" side
        ///    - Short positions valued at mid-price of the "No" side
        /// 7. Record the calculation execution time for performance monitoring
        ///
        /// The method ensures accurate valuation that reflects the current state of the simulated market,
        /// providing a realistic assessment of portfolio value for strategy evaluation and backtesting purposes.
        /// Performance timing data is collected to help identify bottlenecks in high-frequency simulations.
        ///
        /// Example usage:
        /// <code>
        /// var config = new TradingConfig();
        /// var calculator = new EquityCalculator(config);
        /// double totalEquity = calculator.GetEquity(simulationPath, marketSnapshot);
        /// // Or asynchronously:
        /// double totalEquityAsync = await calculator.GetEquityAsync(simulationPath, marketSnapshot);
        /// </code>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if path is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the simulated order book data is corrupted.</exception>
        public double GetEquity(SimulationPath path, MarketSnapshot lastSnapshot)
        {
            // Validate input parameters
            if (path == null)
                throw new ArgumentNullException(nameof(path), "Simulation path cannot be null");

            var stopwatch = _config.EnablePerformanceMetrics ? Stopwatch.StartNew() : null;

            double equity = path.Cash;
            if (path.SimulatedBook == null)
            {
                if (stopwatch != null)
                {
                    stopwatch.Stop();
                    _calculationTimes.Add(stopwatch.ElapsedMilliseconds);
                }
                return equity;
            }

            int bestYesBid = path.SimulatedBook.GetBestYesBid();
            int bestNoBid = path.SimulatedBook.GetBestNoBid();
            int bestYesAsk = bestNoBid > 0 ? 100 - bestNoBid : 100;
            int bestNoAsk = bestYesBid > 0 ? 100 - bestYesBid : 100;

            bool natural = bestYesBid == 0 || bestNoBid == 0;
            if (natural)
            {
                if (path.Position > 0)
                {
                    equity += path.Position * (bestNoBid == 0 ? 1.0 : 0.0);
                }
                else if (path.Position < 0)
                {
                    equity += Math.Abs(path.Position) * (bestYesBid == 0 ? 1.0 : 0.0);
                }
            }
            else
            {
                double midYes = (bestYesBid + bestYesAsk) / 2 / 100.0;
                double midNo = (bestNoBid + bestNoAsk) / 2 / 100.0;
                if (path.Position > 0)
                    equity += path.Position * midYes;
                else if (path.Position < 0)
                    equity += Math.Abs(path.Position) * midNo;
            }

            if (stopwatch != null)
            {
                stopwatch.Stop();
                _calculationTimes.Add(stopwatch.ElapsedMilliseconds);
            }
            return equity;
        }

        /// <summary>
        /// Asynchronously calculates the total equity value for a given simulation path at the current market snapshot.
        /// </summary>
        /// <param name="path">The simulation path containing cash, position, and order book state.</param>
        /// <param name="lastSnapshot">The most recent market snapshot containing current market data.</param>
        /// <returns>A task representing the asynchronous operation, with the total equity value as result.</returns>
        /// <exception cref="ArgumentNullException">Thrown if path is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the simulated order book data is corrupted.</exception>
        public async Task<double> GetEquityAsync(SimulationPath path, MarketSnapshot lastSnapshot)
        {
            return await Task.Run(() => GetEquity(path, lastSnapshot));
        }

        /// <summary>
        /// Gets all recorded calculation times in milliseconds.
        /// </summary>
        /// <returns>An array of calculation times in milliseconds. Returns empty array if performance metrics are disabled.</returns>
        /// <remarks>
        /// This method provides access to the performance metrics collected during equity calculations.
        /// Each value represents the time taken for a single GetEquity or GetEquityAsync call.
        /// If performance metrics are disabled, returns an empty array.
        /// </remarks>
        public long[] GetCalculationTimes()
        {
            return _config.EnablePerformanceMetrics ? _calculationTimes.ToArray() : Array.Empty<long>();
        }

        /// <summary>
        /// Gets performance statistics for equity calculations.
        /// </summary>
        /// <returns>A tuple containing count, average time, min time, and max time in milliseconds. Returns (0, 0, 0, 0) if performance metrics are disabled.</returns>
        /// <remarks>
        /// Returns comprehensive statistics about the performance of equity calculations.
        /// If no calculations have been performed or metrics are disabled, returns (0, 0, 0, 0).
        /// </remarks>
        public (int Count, double AverageMs, long MinMs, long MaxMs) GetCalculationStatistics()
        {
            if (!_config.EnablePerformanceMetrics || _calculationTimes.Count == 0)
                return (0, 0, 0, 0);

            return (
                _calculationTimes.Count,
                _calculationTimes.Average(),
                _calculationTimes.Min(),
                _calculationTimes.Max()
            );
        }

        /// <summary>
        /// Clears all recorded calculation times.
        /// </summary>
        /// <remarks>
        /// This method resets the performance metrics collection, useful for starting
        /// fresh measurements or clearing accumulated data.
        /// </remarks>
        public void ClearCalculationTimes()
        {
            _calculationTimes.Clear();
        }

        /// <summary>
        /// Posts aggregated performance metrics to the PerformanceMonitor.
        /// </summary>
        /// <remarks>
        /// This method aggregates all recorded calculation times and posts them to the PerformanceMonitor
        /// for comprehensive performance analysis. Only posts if performance metrics are enabled.
        /// </remarks>
        public void PostMetrics()
        {
            if (!_config.EnablePerformanceMetrics) return;

            long totalExecutionTimeMs = _calculationTimes.Sum();
            int totalCalculations = _calculationTimes.Count;
            double averageExecutionTimeMs = totalCalculations > 0 ? totalExecutionTimeMs / (double)totalCalculations : 0;

            var metricsDict = new Dictionary<string, object>
            {
                ["MethodName"] = "EquityCalculator.GetEquity",
                ["AverageExecutionTimeMs"] = averageExecutionTimeMs,
                ["TotalItemsProcessed"] = totalCalculations,
                ["TotalItemsFound"] = 0,
                ["ItemCheckTimes"] = (Dictionary<string, long>?)null,
                ["Timestamp"] = DateTime.UtcNow
            };

            RecordSimulationMetrics(metricsDict, _config.EnablePerformanceMetrics);
        }

        /// <summary>
        /// Records simulation performance metrics using the performance monitor.
        /// </summary>
        /// <param name="metricsDict">The dictionary containing metric data.</param>
        /// <param name="enabled">Whether performance metrics are enabled.</param>
        private void RecordSimulationMetrics(Dictionary<string, object> metricsDict, bool enabled)
        {
            if (!enabled)
            {
                _performanceMonitor.RecordDisabledMetric("EquityCalculator", "AverageExecutionTime", "Average Execution Time", "Average time per equity calculation", (double)metricsDict["AverageExecutionTimeMs"], "ms", "Performance", false);
            }
            else
            {
                _performanceMonitor.RecordSpeedDialMetric("EquityCalculator", "AverageExecutionTime", "Average Execution Time", "Average time per equity calculation", (double)metricsDict["AverageExecutionTimeMs"], "ms", "Performance", null, null, null, true);
            }
        }
    }
}
