using BacklashDTOs;

namespace BacklashBot.Helpers
{
    public static class SnapshotDiscrepancyValidator
    {
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            // New properties
            public bool IsOrderbookMissing { get; set; }
            public bool DoPricesOverlap { get; set; }
            public bool IsRateDiscrepancy { get; set; }
        }

        public static ValidationResult ValidateDiscrepancies(MarketSnapshot snapshot)
        {
            var result = new ValidationResult
            {
                IsValid = true,
                IsOrderbookMissing = false,
                DoPricesOverlap = false
            };

            // Check if orderbook data is missing or empty
            if (snapshot.OrderbookData == null || !snapshot.OrderbookData.Any())
            {
                result.IsOrderbookMissing = true;
                result.IsValid = false;
            }

            // Check if best prices overlap (BestYesBid == BestNoBid)
            if (snapshot.BestYesBid != 0 && snapshot.BestNoBid != 0 && snapshot.BestYesBid == snapshot.BestYesAsk)
            {
                result.DoPricesOverlap = true;
                result.IsValid = false;
            }

            // Check rate discrepancy for Yes bids when metrics are mature
            if (snapshot.ChangeMetricsMature)
            {
                // Ensure all fields have values; treat null as 0
                double velocityYesTop = snapshot.VelocityPerMinute_Top_Yes_Bid;
                double velocityYesBottom = snapshot.VelocityPerMinute_Bottom_Yes_Bid;
                double orderYesVolume = snapshot.OrderVolumePerMinute_YesBid;
                double tradeYesVolume = snapshot.TradeVolumePerMinute_Yes;
                double velocityNoTop = snapshot.VelocityPerMinute_Top_No_Bid;
                double velocityNoBottom = snapshot.VelocityPerMinute_Bottom_No_Bid;
                double orderNoVolume = snapshot.OrderVolumePerMinute_NoBid;
                double tradeNoVolume = snapshot.TradeVolumePerMinute_No;

                double velocityYesSum = velocityYesTop + velocityYesBottom;
                double rateYesSum = orderYesVolume + tradeYesVolume;
                double velocityNoSum = velocityNoTop + velocityNoBottom;
                double rateNoSum = orderNoVolume + tradeNoVolume;

                // Check both Diff and Diff2 from SQL query
                double diff = Math.Abs(velocityYesSum - rateYesSum);
                double diff2 = Math.Abs(velocityYesSum - (orderYesVolume + tradeYesVolume));
                double diff3 = Math.Abs(velocityNoSum - rateNoSum);
                double diff4 = Math.Abs(velocityNoSum - (orderNoVolume + tradeNoVolume));

                // If any discrepancy is too large, flag it
                if (diff > 0.1 || diff2 > 0.1 || diff3 > 0.1 || diff4 > 0.1)
                {
                    result.IsRateDiscrepancy = true;
                    result.IsValid = false;
                }
            }

            return result;
        }
    }
}
