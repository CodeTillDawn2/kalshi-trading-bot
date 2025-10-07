using BacklashDTOs.Data;
using BacklashBotData.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BacklashBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between Snapshot model and SnapshotDTO,
    /// supporting market snapshot data transfer for analysis and historical record keeping.
    /// Includes performance metrics collection and batch transformation capabilities.
    /// </summary>
    public static class SnapshotExtensions
    {
        private static readonly ConcurrentDictionary<string, List<TimeSpan>> _performanceMetrics = new();

        /// <summary>
        /// Gets the performance metrics for transformation operations
        /// </summary>
        public static IReadOnlyDictionary<string, List<TimeSpan>> GetPerformanceMetrics()
        {
            return _performanceMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        /// <summary>
        /// Converts a Snapshot model to its DTO representation,
        /// mapping all snapshot properties including market data, velocities, and validation status.
        /// </summary>
        /// <param name="snapshot">The Snapshot model to convert.</param>
        /// <returns>A new SnapshotDTO with all snapshot properties mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when snapshot is null.</exception>
        public static SnapshotDTO ToSnapshotDTO(this Snapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            var stopwatch = Stopwatch.StartNew();

            var result = new SnapshotDTO
            {
                MarketTicker = snapshot.MarketTicker,
                SnapshotDate = snapshot.SnapshotDate,
                JSONSchemaVersion = snapshot.JSONSchemaVersion,
                ChangeMetricsMature = snapshot.ChangeMetricsMature,
                PositionSize = snapshot.PositionSize,
                VelocityPerMinute_Top_Yes_Bid = snapshot.VelocityPerMinute_Top_Yes_Bid,
                VelocityPerMinute_Top_No_Bid = snapshot.VelocityPerMinute_Top_No_Bid,
                VelocityPerMinute_Bottom_Yes_Bid = snapshot.VelocityPerMinute_Bottom_Yes_Bid,
                VelocityPerMinute_Bottom_No_Bid = snapshot.VelocityPerMinute_Bottom_No_Bid,
                OrderVolume_Yes_Bid = snapshot.OrderVolume_Yes_Bid,
                OrderVolume_No_Bid = snapshot.OrderVolume_No_Bid,
                TradeVolume_Yes = snapshot.TradeVolume_Yes,
                TradeVolume_No = snapshot.TradeVolume_No,
                AverageTradeSize_Yes = snapshot.AverageTradeSize_Yes,
                AverageTradeSize_No = snapshot.AverageTradeSize_No,
                MarketTypeID = snapshot.MarketTypeID,
                IsValidated = snapshot.IsValidated,
                RawJSON = snapshot.RawJSON,
                BrainInstance = snapshot.BrainInstance
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSnapshotDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a SnapshotDTO to its model representation,
        /// creating a new Snapshot with all properties mapped from the DTO.
        /// </summary>
        /// <param name="snapshotDTO">The SnapshotDTO to convert.</param>
        /// <returns>A new Snapshot model with all properties mapped from the DTO.</returns>
        /// <exception cref="ArgumentNullException">Thrown when snapshotDTO is null.</exception>
        public static Snapshot ToSnapshot(this SnapshotDTO snapshotDTO)
        {
            if (snapshotDTO == null)
                throw new ArgumentNullException(nameof(snapshotDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new Snapshot
            {
                MarketTicker = snapshotDTO.MarketTicker,
                SnapshotDate = snapshotDTO.SnapshotDate,
                JSONSchemaVersion = snapshotDTO.JSONSchemaVersion,
                ChangeMetricsMature = snapshotDTO.ChangeMetricsMature,
                PositionSize = snapshotDTO.PositionSize,
                VelocityPerMinute_Top_Yes_Bid = snapshotDTO.VelocityPerMinute_Top_Yes_Bid,
                VelocityPerMinute_Top_No_Bid = snapshotDTO.VelocityPerMinute_Top_No_Bid,
                VelocityPerMinute_Bottom_Yes_Bid = snapshotDTO.VelocityPerMinute_Bottom_Yes_Bid,
                VelocityPerMinute_Bottom_No_Bid = snapshotDTO.VelocityPerMinute_Bottom_No_Bid,
                OrderVolume_Yes_Bid = snapshotDTO.OrderVolume_Yes_Bid,
                OrderVolume_No_Bid = snapshotDTO.OrderVolume_No_Bid,
                TradeVolume_Yes = snapshotDTO.TradeVolume_Yes,
                TradeVolume_No = snapshotDTO.TradeVolume_No,
                AverageTradeSize_Yes = snapshotDTO.AverageTradeSize_Yes,
                AverageTradeSize_No = snapshotDTO.AverageTradeSize_No,
                MarketTypeID = snapshotDTO.MarketTypeID,
                IsValidated = snapshotDTO.IsValidated,
                RawJSON = snapshotDTO.RawJSON,
                BrainInstance = snapshotDTO.BrainInstance
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSnapshot", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Updates an existing Snapshot model with data from a SnapshotDTO,
        /// validating market ticker and snapshot date match before applying all property changes.
        /// </summary>
        /// <param name="snapshot">The Snapshot model to update.</param>
        /// <param name="snapshotDTO">The SnapshotDTO containing updated data.</param>
        /// <returns>The updated Snapshot model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when snapshot or snapshotDTO is null.</exception>
        /// <exception cref="ArgumentException">Thrown when market tickers or snapshot dates do not match.</exception>
        public static Snapshot UpdateSnapshot(this Snapshot snapshot, SnapshotDTO snapshotDTO)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (snapshotDTO == null)
                throw new ArgumentNullException(nameof(snapshotDTO));

            if (snapshot.MarketTicker != snapshotDTO.MarketTicker || snapshot.SnapshotDate != snapshotDTO.SnapshotDate)
            {
                throw new ArgumentException("MarketTicker or SnapshotDate don't match for Update Snapshot", nameof(snapshotDTO));
            }

            var stopwatch = Stopwatch.StartNew();

            snapshot.JSONSchemaVersion = snapshotDTO.JSONSchemaVersion;
            snapshot.ChangeMetricsMature = snapshotDTO.ChangeMetricsMature;
            snapshot.PositionSize = snapshotDTO.PositionSize;
            snapshot.VelocityPerMinute_Top_Yes_Bid = snapshotDTO.VelocityPerMinute_Top_Yes_Bid;
            snapshot.VelocityPerMinute_Top_No_Bid = snapshotDTO.VelocityPerMinute_Top_No_Bid;
            snapshot.VelocityPerMinute_Bottom_Yes_Bid = snapshotDTO.VelocityPerMinute_Bottom_Yes_Bid;
            snapshot.VelocityPerMinute_Bottom_No_Bid = snapshotDTO.VelocityPerMinute_Bottom_No_Bid;
            snapshot.OrderVolume_Yes_Bid = snapshotDTO.OrderVolume_Yes_Bid;
            snapshot.OrderVolume_No_Bid = snapshotDTO.OrderVolume_No_Bid;
            snapshot.TradeVolume_Yes = snapshotDTO.TradeVolume_Yes;
            snapshot.TradeVolume_No = snapshotDTO.TradeVolume_No;
            snapshot.AverageTradeSize_Yes = snapshotDTO.AverageTradeSize_Yes;
            snapshot.AverageTradeSize_No = snapshotDTO.AverageTradeSize_No;
            snapshot.MarketTypeID = snapshotDTO.MarketTypeID;
            snapshot.IsValidated = snapshotDTO.IsValidated;
            snapshot.RawJSON = snapshotDTO.RawJSON;
            snapshot.BrainInstance = snapshotDTO.BrainInstance;

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("UpdateSnapshot", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return snapshot;
        }

        /// <summary>
        /// Converts a collection of Snapshot models to their corresponding DTO representations.
        /// </summary>
        /// <param name="snapshots">The collection of Snapshot models to convert.</param>
        /// <returns>A list of SnapshotDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when snapshots is null.</exception>
        public static List<SnapshotDTO> ToSnapshotDTOs(this IEnumerable<Snapshot> snapshots)
        {
            if (snapshots == null)
                throw new ArgumentNullException(nameof(snapshots));

            var stopwatch = Stopwatch.StartNew();

            var result = snapshots.Select(s => s.ToSnapshotDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSnapshotDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of SnapshotDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="snapshotDTOs">The collection of SnapshotDTOs to convert.</param>
        /// <returns>A list of Snapshot models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when snapshotDTOs is null.</exception>
        public static List<Snapshot> ToSnapshots(this IEnumerable<SnapshotDTO> snapshotDTOs)
        {
            if (snapshotDTOs == null)
                throw new ArgumentNullException(nameof(snapshotDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = snapshotDTOs.Select(dto => dto.ToSnapshot()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSnapshots", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of a Snapshot model to prevent unintended mutations.
        /// </summary>
        /// <param name="snapshot">The Snapshot model to clone.</param>
        /// <returns>A new Snapshot instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when snapshot is null.</exception>
        public static Snapshot DeepClone(this Snapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            return new Snapshot
            {
                MarketTicker = snapshot.MarketTicker,
                SnapshotDate = snapshot.SnapshotDate,
                JSONSchemaVersion = snapshot.JSONSchemaVersion,
                ChangeMetricsMature = snapshot.ChangeMetricsMature,
                PositionSize = snapshot.PositionSize,
                VelocityPerMinute_Top_Yes_Bid = snapshot.VelocityPerMinute_Top_Yes_Bid,
                VelocityPerMinute_Top_No_Bid = snapshot.VelocityPerMinute_Top_No_Bid,
                VelocityPerMinute_Bottom_Yes_Bid = snapshot.VelocityPerMinute_Bottom_Yes_Bid,
                VelocityPerMinute_Bottom_No_Bid = snapshot.VelocityPerMinute_Bottom_No_Bid,
                OrderVolume_Yes_Bid = snapshot.OrderVolume_Yes_Bid,
                OrderVolume_No_Bid = snapshot.OrderVolume_No_Bid,
                TradeVolume_Yes = snapshot.TradeVolume_Yes,
                TradeVolume_No = snapshot.TradeVolume_No,
                AverageTradeSize_Yes = snapshot.AverageTradeSize_Yes,
                AverageTradeSize_No = snapshot.AverageTradeSize_No,
                MarketTypeID = snapshot.MarketTypeID,
                IsValidated = snapshot.IsValidated,
                RawJSON = snapshot.RawJSON,
                BrainInstance = snapshot.BrainInstance
            };
        }

        /// <summary>
        /// Creates a deep clone of a SnapshotDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="snapshotDTO">The SnapshotDTO to clone.</param>
        /// <returns>A new SnapshotDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when snapshotDTO is null.</exception>
        public static SnapshotDTO DeepClone(this SnapshotDTO snapshotDTO)
        {
            if (snapshotDTO == null)
                throw new ArgumentNullException(nameof(snapshotDTO));

            return new SnapshotDTO
            {
                MarketTicker = snapshotDTO.MarketTicker,
                SnapshotDate = snapshotDTO.SnapshotDate,
                JSONSchemaVersion = snapshotDTO.JSONSchemaVersion,
                ChangeMetricsMature = snapshotDTO.ChangeMetricsMature,
                PositionSize = snapshotDTO.PositionSize,
                VelocityPerMinute_Top_Yes_Bid = snapshotDTO.VelocityPerMinute_Top_Yes_Bid,
                VelocityPerMinute_Top_No_Bid = snapshotDTO.VelocityPerMinute_Top_No_Bid,
                VelocityPerMinute_Bottom_Yes_Bid = snapshotDTO.VelocityPerMinute_Bottom_Yes_Bid,
                VelocityPerMinute_Bottom_No_Bid = snapshotDTO.VelocityPerMinute_Bottom_No_Bid,
                OrderVolume_Yes_Bid = snapshotDTO.OrderVolume_Yes_Bid,
                OrderVolume_No_Bid = snapshotDTO.OrderVolume_No_Bid,
                TradeVolume_Yes = snapshotDTO.TradeVolume_Yes,
                TradeVolume_No = snapshotDTO.TradeVolume_No,
                AverageTradeSize_Yes = snapshotDTO.AverageTradeSize_Yes,
                AverageTradeSize_No = snapshotDTO.AverageTradeSize_No,
                MarketTypeID = snapshotDTO.MarketTypeID,
                IsValidated = snapshotDTO.IsValidated,
                RawJSON = snapshotDTO.RawJSON,
                BrainInstance = snapshotDTO.BrainInstance
            };
        }
    }
}
