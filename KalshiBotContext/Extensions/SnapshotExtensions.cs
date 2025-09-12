using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between Snapshot model and SnapshotDTO,
    /// supporting market snapshot data transfer for analysis and historical record keeping.
    /// </summary>
    public static class SnapshotExtensions
    {
        /// <summary>
        /// Converts a Snapshot model to its DTO representation,
        /// mapping all snapshot properties including market data, velocities, and validation status.
        /// </summary>
        /// <param name="snapshot">The Snapshot model to convert.</param>
        /// <returns>A new SnapshotDTO with all snapshot properties mapped.</returns>
        public static SnapshotDTO ToSnapshotDTO(this Snapshot snapshot)
        {
            return new SnapshotDTO
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
        /// Converts a SnapshotDTO to its model representation,
        /// creating a new Snapshot with all properties mapped from the DTO.
        /// </summary>
        /// <param name="snapshotDTO">The SnapshotDTO to convert.</param>
        /// <returns>A new Snapshot model with all properties mapped from the DTO.</returns>
        public static Snapshot ToSnapshot(this SnapshotDTO snapshotDTO)
        {
            return new Snapshot
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

        /// <summary>
        /// Updates an existing Snapshot model with data from a SnapshotDTO,
        /// validating market ticker and snapshot date match before applying all property changes.
        /// </summary>
        /// <param name="snapshot">The Snapshot model to update.</param>
        /// <param name="snapshotDTO">The SnapshotDTO containing updated data.</param>
        /// <returns>The updated Snapshot model.</returns>
        /// <exception cref="Exception">Thrown when market tickers or snapshot dates do not match.</exception>
        public static Snapshot UpdateSnapshot(this Snapshot snapshot, SnapshotDTO snapshotDTO)
        {
            if (snapshot.MarketTicker != snapshotDTO.MarketTicker || snapshot.SnapshotDate != snapshotDTO.SnapshotDate)
            {
                throw new Exception("MarketTicker or SnapshotDate don't match for Update Snapshot");
            }
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
            return snapshot;
        }
    }
}
