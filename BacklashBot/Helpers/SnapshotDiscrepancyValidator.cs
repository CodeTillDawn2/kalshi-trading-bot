using BacklashDTOs;

namespace BacklashBot.Helpers
{
    /// <summary>
    /// Provides static methods for validating market snapshots to detect data discrepancies and ensure data integrity.
    /// This validator is used to filter out invalid snapshots before processing or saving them, preventing downstream issues
    /// from corrupted or inconsistent market data.
    /// </summary>
    public static class SnapshotDiscrepancyValidator
    {
        /// <summary>
        /// Represents the result of a snapshot validation operation, containing flags for specific discrepancy types
        /// and an overall validity indicator.
        /// </summary>
        public record ValidationResult
        {
            /// <summary>
            /// Gets a value indicating whether the snapshot passed all validation checks.
            /// True if no discrepancies were found; false otherwise.
            /// </summary>
            public bool IsValid { get; init; }

            /// <summary>
            /// Gets a value indicating whether the orderbook data is missing or empty.
            /// This is a critical issue as orderbook data is essential for trading decisions.
            /// </summary>
            public bool IsOrderbookMissing { get; init; }

            /// <summary>
            /// Gets a value indicating whether the best bid and ask prices overlap, which is an invalid state.
            /// In a properly functioning market, the best bid should be less than the best ask.
            /// </summary>
            public bool DoPricesOverlap { get; init; }

            /// <summary>
            /// Gets a value indicating whether there are significant discrepancies between velocity metrics
            /// and actual trade/order volumes, suggesting potential data corruption or calculation errors.
            /// </summary>
            public bool IsRateDiscrepancy { get; init; }
        }

        /// <summary>
        /// Validates a market snapshot for various types of discrepancies that could indicate data corruption
        /// or invalid market states. This method performs multiple checks including orderbook presence,
        /// price overlap detection, and rate consistency validation.
        /// </summary>
        /// <param name="snapshot">The market snapshot to validate.</param>
        /// <param name="threshold">The discrepancy threshold for rate validation. Default is 0.1.</param>
        /// <returns>A ValidationResult containing the validation outcome and specific discrepancy flags.</returns>
        /// <remarks>
        /// The validation process includes:
        /// - Checking for missing or empty orderbook data
        /// - Detecting overlapping bid and ask prices
        /// - Validating consistency between velocity metrics and actual volumes when change metrics are mature
        ///
        /// The default 0.1 threshold is considered significant as per user specification.
        ///
        /// This method is designed to be fast and lightweight, suitable for real-time snapshot processing.
        /// Invalid snapshots are logged as warnings in the calling service for monitoring purposes.
        /// </remarks>
        public static ValidationResult ValidateDiscrepancies(MarketSnapshot snapshot, double threshold = 0.1)
        {
            var result = new ValidationResult
            {
                IsValid = true,
                IsOrderbookMissing = false,
                DoPricesOverlap = false,
                IsRateDiscrepancy = false
            };

            // Check if orderbook data is missing or empty
            if (snapshot.OrderbookData == null || !snapshot.OrderbookData.Any())
            {
                result.IsOrderbookMissing = true;
                result.IsValid = false;
            }

            // Check if best prices overlap (BestYesBid >= BestYesAsk, indicating invalid market state)
            if (snapshot.BestYesBid != 0 && snapshot.BestNoBid != 0 && snapshot.BestYesBid >= snapshot.BestYesAsk)
            {
                result.DoPricesOverlap = true;
                result.IsValid = false;
            }

            // Check rate discrepancy for bids when metrics are mature
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

                // Calculate absolute differences between velocity sums and volume sums
                double diff = Math.Abs(velocityYesSum - rateYesSum);
                double diff2 = Math.Abs(velocityYesSum - (orderYesVolume + tradeYesVolume));
                double diff3 = Math.Abs(velocityNoSum - rateNoSum);
                double diff4 = Math.Abs(velocityNoSum - (orderNoVolume + tradeNoVolume));

                // If any discrepancy exceeds the threshold, flag it
                if (diff > threshold || diff2 > threshold || diff3 > threshold || diff4 > threshold)
                {
                    result.IsRateDiscrepancy = true;
                    result.IsValid = false;
                }
            }

            return result;
        }
    }
}
