using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    public static class SnapshotExtensions
    {
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
