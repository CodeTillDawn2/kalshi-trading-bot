using NUnit.Framework;
using BacklashDTOs;
using BacklashBot.Helpers;
using System.Collections.Generic;

namespace KalshiBotTests
{
    /// <summary>
    /// NUnit test fixture for validating the SnapshotDiscrepancyValidator functionality.
    /// This test class provides comprehensive testing for snapshot validation logic including
    /// orderbook validation, price overlap detection, and rate discrepancy analysis.
    /// Tests cover various edge cases and validation scenarios to ensure data integrity.
    /// </summary>
    [TestFixture]
    public class SnapshotDiscrepancyValidatorTests
    {
        /// <summary>
        /// Tests that a valid snapshot with all required data passes validation.
        /// Verifies that the validator correctly identifies a well-formed snapshot
        /// with proper orderbook, non-overlapping prices, and acceptable rate discrepancies.
        /// </summary>
        [Test]
        public void ValidateDiscrepancies_ValidSnapshot_ReturnsValidResult()
        {
            // Arrange: Create a valid snapshot with all checks passing
            var snapshot = new MarketSnapshot
            {
                OrderbookData = new List<Dictionary<string, object>> { new Dictionary<string, object>() },
                BestYesBid = 10,
                BestNoBid = 9,
                ChangeMetricsMature = true,
                VelocityPerMinute_Top_Yes_Bid = 1.0,
                VelocityPerMinute_Bottom_Yes_Bid = 1.0,
                OrderVolumePerMinute_YesBid = 1.0,
                TradeVolumePerMinute_Yes = 1.0,
                VelocityPerMinute_Top_No_Bid = 1.0,
                VelocityPerMinute_Bottom_No_Bid = 1.0,
                OrderVolumePerMinute_NoBid = 1.0,
                TradeVolumePerMinute_No = 1.0
            };

            // Act
            var result = SnapshotDiscrepancyValidator.ValidateDiscrepancies(snapshot);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsOrderbookMissing, Is.False);
            Assert.That(result.DoPricesOverlap, Is.False);
            Assert.That(result.IsRateDiscrepancy, Is.False);
        }

        /// <summary>
        /// Tests that a snapshot with missing orderbook data fails validation.
        /// Verifies that the validator correctly identifies when orderbook data is null
        /// and marks the snapshot as invalid with appropriate error flags.
        /// </summary>
        [Test]
        public void ValidateDiscrepancies_MissingOrderbook_ReturnsInvalidResult()
        {
            // Arrange: Snapshot with null orderbook
            var snapshot = new MarketSnapshot
            {
                OrderbookData = null,
                BestYesBid = 10,
                BestNoBid = 9,
                ChangeMetricsMature = true,
                VelocityPerMinute_Top_Yes_Bid = 1.0,
                VelocityPerMinute_Bottom_Yes_Bid = 1.0,
                OrderVolumePerMinute_YesBid = 1.0,
                TradeVolumePerMinute_Yes = 1.0,
                VelocityPerMinute_Top_No_Bid = 1.0,
                VelocityPerMinute_Bottom_No_Bid = 1.0,
                OrderVolumePerMinute_NoBid = 1.0,
                TradeVolumePerMinute_No = 1.0
            };

            // Act
            var result = SnapshotDiscrepancyValidator.ValidateDiscrepancies(snapshot);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsOrderbookMissing, Is.True);
            Assert.That(result.DoPricesOverlap, Is.False);
            Assert.That(result.IsRateDiscrepancy, Is.False);
        }

        /// <summary>
        /// Tests that a snapshot with empty orderbook data fails validation.
        /// Verifies that the validator correctly identifies when orderbook data exists
        /// but contains no entries, marking the snapshot as invalid.
        /// </summary>
        [Test]
        public void ValidateDiscrepancies_EmptyOrderbook_ReturnsInvalidResult()
        {
            // Arrange: Snapshot with empty orderbook
            var snapshot = new MarketSnapshot
            {
                OrderbookData = new List<Dictionary<string, object>>(),
                BestYesBid = 10,
                BestNoBid = 9,
                ChangeMetricsMature = true,
                VelocityPerMinute_Top_Yes_Bid = 1.0,
                VelocityPerMinute_Bottom_Yes_Bid = 1.0,
                OrderVolumePerMinute_YesBid = 1.0,
                TradeVolumePerMinute_Yes = 1.0,
                VelocityPerMinute_Top_No_Bid = 1.0,
                VelocityPerMinute_Bottom_No_Bid = 1.0,
                OrderVolumePerMinute_NoBid = 1.0,
                TradeVolumePerMinute_No = 1.0
            };

            // Act
            var result = SnapshotDiscrepancyValidator.ValidateDiscrepancies(snapshot);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsOrderbookMissing, Is.True);
            Assert.That(result.DoPricesOverlap, Is.False);
            Assert.That(result.IsRateDiscrepancy, Is.False);
        }

        /// <summary>
        /// Tests that a snapshot with overlapping bid/ask prices fails validation.
        /// Verifies that the validator correctly identifies when bid prices overlap with ask prices,
        /// which indicates invalid market data that should be rejected.
        /// </summary>
        [Test]
        public void ValidateDiscrepancies_OverlappingPrices_ReturnsInvalidResult()
        {
            // Arrange: BestYesBid >= BestYesAsk (BestYesAsk = 100 - BestNoBid = 91, so BestYesBid = 95 >= 91)
            var snapshot = new MarketSnapshot
            {
                OrderbookData = new List<Dictionary<string, object>> { new Dictionary<string, object>() },
                BestYesBid = 95,
                BestNoBid = 9,
                ChangeMetricsMature = true,
                VelocityPerMinute_Top_Yes_Bid = 1.0,
                VelocityPerMinute_Bottom_Yes_Bid = 1.0,
                OrderVolumePerMinute_YesBid = 1.0,
                TradeVolumePerMinute_Yes = 1.0,
                VelocityPerMinute_Top_No_Bid = 1.0,
                VelocityPerMinute_Bottom_No_Bid = 1.0,
                OrderVolumePerMinute_NoBid = 1.0,
                TradeVolumePerMinute_No = 1.0
            };

            // Act
            var result = SnapshotDiscrepancyValidator.ValidateDiscrepancies(snapshot);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsOrderbookMissing, Is.False);
            Assert.That(result.DoPricesOverlap, Is.True);
            Assert.That(result.IsRateDiscrepancy, Is.False);
        }

        /// <summary>
        /// Tests that a snapshot with rate discrepancy exceeding default threshold fails validation.
        /// Verifies that the validator correctly identifies when order/trade volume rates
        /// differ significantly from velocity metrics, indicating potential data inconsistency.
        /// </summary>
        [Test]
        public void ValidateDiscrepancies_RateDiscrepancyExceedsDefaultThreshold_ReturnsInvalidResult()
        {
            // Arrange: Discrepancy > 0.1
            var snapshot = new MarketSnapshot
            {
                OrderbookData = new List<Dictionary<string, object>> { new Dictionary<string, object>() },
                BestYesBid = 10,
                BestNoBid = 9,
                ChangeMetricsMature = true,
                VelocityPerMinute_Top_Yes_Bid = 1.0,
                VelocityPerMinute_Bottom_Yes_Bid = 1.0,
                OrderVolumePerMinute_YesBid = 0.0,
                TradeVolumePerMinute_Yes = 0.0,
                VelocityPerMinute_Top_No_Bid = 1.0,
                VelocityPerMinute_Bottom_No_Bid = 1.0,
                OrderVolumePerMinute_NoBid = 0.0,
                TradeVolumePerMinute_No = 0.0
            };

            // Act
            var result = SnapshotDiscrepancyValidator.ValidateDiscrepancies(snapshot);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsOrderbookMissing, Is.False);
            Assert.That(result.DoPricesOverlap, Is.False);
            Assert.That(result.IsRateDiscrepancy, Is.True);
        }

        /// <summary>
        /// Tests that a snapshot with rate discrepancy below custom threshold passes validation.
        /// Verifies that the validator accepts snapshots when discrepancies are within
        /// acceptable custom threshold limits, allowing for configurable validation rules.
        /// </summary>
        [Test]
        public void ValidateDiscrepancies_RateDiscrepancyBelowCustomThreshold_ReturnsValidResult()
        {
            // Arrange: Discrepancy < custom threshold (0.5)
            var snapshot = new MarketSnapshot
            {
                OrderbookData = new List<Dictionary<string, object>> { new Dictionary<string, object>() },
                BestYesBid = 10,
                BestNoBid = 9,
                ChangeMetricsMature = true,
                VelocityPerMinute_Top_Yes_Bid = 1.0,
                VelocityPerMinute_Bottom_Yes_Bid = 1.0,
                OrderVolumePerMinute_YesBid = 0.6,
                TradeVolumePerMinute_Yes = 0.6,
                VelocityPerMinute_Top_No_Bid = 1.0,
                VelocityPerMinute_Bottom_No_Bid = 1.0,
                OrderVolumePerMinute_NoBid = 0.6,
                TradeVolumePerMinute_No = 0.6
            };

            // Act
            var result = SnapshotDiscrepancyValidator.ValidateDiscrepancies(snapshot, 0.5);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsOrderbookMissing, Is.False);
            Assert.That(result.DoPricesOverlap, Is.False);
            Assert.That(result.IsRateDiscrepancy, Is.False);
        }

        /// <summary>
        /// Tests that a snapshot with rate discrepancy exceeding custom threshold fails validation.
        /// Verifies that the validator correctly rejects snapshots when discrepancies exceed
        /// user-defined threshold limits, ensuring strict data quality control.
        /// </summary>
        [Test]
        public void ValidateDiscrepancies_RateDiscrepancyExceedsCustomThreshold_ReturnsInvalidResult()
        {
            // Arrange: Discrepancy > custom threshold (0.05)
            var snapshot = new MarketSnapshot
            {
                OrderbookData = new List<Dictionary<string, object>> { new Dictionary<string, object>() },
                BestYesBid = 10,
                BestNoBid = 9,
                ChangeMetricsMature = true,
                VelocityPerMinute_Top_Yes_Bid = 1.0,
                VelocityPerMinute_Bottom_Yes_Bid = 1.0,
                OrderVolumePerMinute_YesBid = 0.0,
                TradeVolumePerMinute_Yes = 0.0,
                VelocityPerMinute_Top_No_Bid = 1.0,
                VelocityPerMinute_Bottom_No_Bid = 1.0,
                OrderVolumePerMinute_NoBid = 0.0,
                TradeVolumePerMinute_No = 0.0
            };

            // Act
            var result = SnapshotDiscrepancyValidator.ValidateDiscrepancies(snapshot, 0.05);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsOrderbookMissing, Is.False);
            Assert.That(result.DoPricesOverlap, Is.False);
            Assert.That(result.IsRateDiscrepancy, Is.True);
        }

        /// <summary>
        /// Tests that rate discrepancy checks are skipped when change metrics are not mature.
        /// Verifies that the validator bypasses rate validation for immature data,
        /// allowing snapshots to pass validation during early market periods.
        /// </summary>
        [Test]
        public void ValidateDiscrepancies_ChangeMetricsNotMature_SkipsRateCheck()
        {
            // Arrange: ChangeMetricsMature false, large discrepancy but should be valid
            var snapshot = new MarketSnapshot
            {
                OrderbookData = new List<Dictionary<string, object>> { new Dictionary<string, object>() },
                BestYesBid = 10,
                BestNoBid = 9,
                ChangeMetricsMature = false,
                VelocityPerMinute_Top_Yes_Bid = 1.0,
                VelocityPerMinute_Bottom_Yes_Bid = 1.0,
                OrderVolumePerMinute_YesBid = 0.0,
                TradeVolumePerMinute_Yes = 0.0,
                VelocityPerMinute_Top_No_Bid = 1.0,
                VelocityPerMinute_Bottom_No_Bid = 1.0,
                OrderVolumePerMinute_NoBid = 0.0,
                TradeVolumePerMinute_No = 0.0
            };

            // Act
            var result = SnapshotDiscrepancyValidator.ValidateDiscrepancies(snapshot);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsOrderbookMissing, Is.False);
            Assert.That(result.DoPricesOverlap, Is.False);
            Assert.That(result.IsRateDiscrepancy, Is.False);
        }

        /// <summary>
        /// Tests that overlap checks are skipped when best prices are zero.
        /// Verifies that the validator handles edge cases where bid prices are zero,
        /// preventing false overlap detection in illiquid or invalid market conditions.
        /// </summary>
        [Test]
        public void ValidateDiscrepancies_ZeroBestPrices_SkipsOverlapCheck()
        {
            // Arrange: BestYesBid = 0, should not trigger overlap
            var snapshot = new MarketSnapshot
            {
                OrderbookData = new List<Dictionary<string, object>> { new Dictionary<string, object>() },
                BestYesBid = 0,
                BestNoBid = 9,
                ChangeMetricsMature = true,
                VelocityPerMinute_Top_Yes_Bid = 1.0,
                VelocityPerMinute_Bottom_Yes_Bid = 1.0,
                OrderVolumePerMinute_YesBid = 1.0,
                TradeVolumePerMinute_Yes = 1.0,
                VelocityPerMinute_Top_No_Bid = 1.0,
                VelocityPerMinute_Bottom_No_Bid = 1.0,
                OrderVolumePerMinute_NoBid = 1.0,
                TradeVolumePerMinute_No = 1.0
            };

            // Act
            var result = SnapshotDiscrepancyValidator.ValidateDiscrepancies(snapshot);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsOrderbookMissing, Is.False);
            Assert.That(result.DoPricesOverlap, Is.False);
            Assert.That(result.IsRateDiscrepancy, Is.False);
        }
    }
}