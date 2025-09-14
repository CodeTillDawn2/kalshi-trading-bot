using System.Collections.Concurrent;
using static BacklashInterfaces.Enums.StrategyEnums;
using System.Threading.Tasks;

/// <summary>
/// Provides a centralized mechanism for logging and exporting research data on trading entries during simulation and backtesting operations.
/// This static class serves as a data collection hub for machine learning research, allowing strategies to log detailed entry metrics
/// that can be analyzed for performance evaluation and parameter optimization. The collected data includes market conditions,
/// entry timing, performance metrics, and strategy parameters to support comprehensive analysis of trading signal effectiveness.
/// </summary>
namespace TradingStrategies.ML
{
    public static class ResearchBus
    {
        /// <summary>
        /// Thread-safe collection of research entries logged during simulation runs.
        /// Entries are stored in a concurrent bag to support parallel logging from multiple strategy executions.
        /// </summary>
        public static readonly ConcurrentBag<EntryResearch> Entries = new();

        /// <summary>
        /// Clears all logged research entries from the collection.
        /// This method should be called at the beginning of each simulation run to ensure clean data collection
        /// and prevent contamination from previous runs.
        /// </summary>
        public static void Clear()
        {
            while (Entries.TryTake(out _)) { }
        }

        /// <summary>
        /// Validates the provided EntryResearch instance to ensure all parameters are within valid ranges.
        /// Throws ArgumentException if any validation fails.
        /// </summary>
        /// <param name="e">The EntryResearch instance to validate.</param>
        private static void ValidateEntry(EntryResearch e)
        {
            if (string.IsNullOrWhiteSpace(e.MarketTicker))
                throw new ArgumentException("MarketTicker cannot be null or empty.", nameof(e.MarketTicker));

            if (e.EntryTime == default)
                throw new ArgumentException("EntryTime must be a valid DateTime.", nameof(e.EntryTime));

            if (e.HorizonEnd == default)
                throw new ArgumentException("HorizonEnd must be a valid DateTime.", nameof(e.HorizonEnd));

            if (e.TauTicks <= 0)
                throw new ArgumentException("TauTicks must be greater than 0.", nameof(e.TauTicks));

            if (e.PLongAtEntry < 0 || e.PLongAtEntry > 1)
                throw new ArgumentException("PLongAtEntry must be between 0 and 1.", nameof(e.PLongAtEntry));

            if (e.PShortAtEntry < 0 || e.PShortAtEntry > 1)
                throw new ArgumentException("PShortAtEntry must be between 0 and 1.", nameof(e.PShortAtEntry));

            if (e.Score < 0 || e.Score > 1)
                throw new ArgumentException("Score must be between 0 and 1.", nameof(e.Score));

            if (string.IsNullOrWhiteSpace(e.ParameterSet))
                throw new ArgumentException("ParameterSet cannot be null or empty.", nameof(e.ParameterSet));
        }

        /// <summary>
        /// Records a research entry to the collection for later analysis.
        /// This method validates the entry and adds the provided entry research data to the concurrent collection,
        /// allowing strategies to record detailed metrics about trading signals and their outcomes.
        /// </summary>
        /// <param name="e">The entry research data to record, containing all relevant metrics and context.</param>
        public static void RecordEntry(EntryResearch e)
        {
            ValidateEntry(e);
            Entries.Add(e);
        }

        /// <summary>
        /// Exports all logged research entries to a CSV file for external analysis.
        /// The CSV file includes a header row with column names and data rows ordered by score in descending order.
        /// For large datasets, parallel processing is used to build the CSV lines for improved performance.
        /// This facilitates analysis in spreadsheet applications or statistical tools for strategy optimization.
        /// </summary>
        /// <param name="path">The file system path where the CSV file should be written.</param>
        public static void DumpCsv(string path)
        {
            var sortedEntries = Entries.OrderByDescending(x => x.Score).ToList();
            var lines = new string[sortedEntries.Count];

            // Use parallel processing to build CSV lines for better performance on large datasets
            Parallel.For(0, sortedEntries.Count, i =>
            {
                var e = sortedEntries[i];
                lines[i] = $"{e.MarketTicker},{e.EntryTime:O},{e.Side},{e.ThresholdUsed:F3}," +
                           $"{e.HitTau},{e.TauTicks},{e.DdTicks},{e.MfeTicks},{e.MaeTicks},{e.PeakSizeTicks}," +
                           $"{(int)e.TimeToPeak.TotalSeconds},{e.PLongAtEntry:F4},{e.PShortAtEntry:F4},{e.Score:F4},{e.ParameterSet}";
            });

            using var sw = new StreamWriter(path);
            sw.WriteLine("market,ts_entry,side,thr,hit_tau,tau,dd,mfe,mae,peak_size,time_to_peak_sec,p_long,p_short,score,parameter_set");
            foreach (var line in lines)
            {
                sw.WriteLine(line);
            }
        }

        /// <summary>
        /// Represents a single research entry containing detailed metrics about a trading entry point.
        /// This record captures comprehensive data about market conditions, entry timing, performance outcomes,
        /// and strategy parameters to enable thorough analysis of trading signal effectiveness and strategy optimization.
        /// </summary>
        public record EntryResearch(
            /// <summary>The ticker symbol of the market where the entry occurred.</summary>
            string MarketTicker,
            /// <summary>The trading action taken (Buy or Sell) at the entry point.</summary>
            ActionType Side,
            /// <summary>The timestamp when the trading entry was made.</summary>
            DateTime EntryTime,
            /// <summary>The end timestamp of the analysis horizon for this entry.</summary>
            DateTime HorizonEnd,
            /// <summary>The mid-price in ticks at the time of entry.</summary>
            int EntryMidTicks,
            /// <summary>The target threshold in ticks for the trading strategy.</summary>
            int TauTicks,
            /// <summary>The maximum drawdown in ticks experienced after entry.</summary>
            int DdTicks,
            /// <summary>Indicates whether the entry reached the target threshold (Tau).</summary>
            bool HitTau,
            /// <summary>The maximum favorable excursion in ticks after entry.</summary>
            int MfeTicks,
            /// <summary>The maximum adverse excursion in ticks after entry.</summary>
            int MaeTicks,
            /// <summary>The size of the peak movement in ticks during the analysis period.</summary>
            int PeakSizeTicks,
            /// <summary>The time duration from entry to the peak movement.</summary>
            TimeSpan TimeToPeak,
            /// <summary>The probability of upward movement at the time of entry.</summary>
            double PLongAtEntry,
            /// <summary>The probability of downward movement at the time of entry.</summary>
            double PShortAtEntry,
            /// <summary>The threshold value used by the strategy for this entry.</summary>
            double ThresholdUsed,
            /// <summary>The performance score assigned to this entry for ranking and analysis.</summary>
            double Score,
            /// <summary>Additional memo or notes about this entry for research purposes.</summary>
            string Memo,
            /// <summary>The parameter set identifier used for this strategy execution.</summary>
            string ParameterSet
        );
    }
}
