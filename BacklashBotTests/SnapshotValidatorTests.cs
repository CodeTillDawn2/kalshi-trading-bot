using BacklashBot.Services;
using BacklashBot.State;
using BacklashCommon.Helpers;
using BacklashDTOs;

namespace BacklashBotTests
{
    /// <summary>
    /// Unit test suite for SnapshotDiscrepancyValidator functionality.
    /// Tests validation logic for market snapshot data integrity and consistency.
    /// </summary>
    [TestFixture]
    public class SnapshotValidatorTests
    {
        /// <summary>
        /// Tests that a valid snapshot with proper orderbook data passes all validation checks.
        /// Verifies that snapshots with complete orderbook information and reasonable price relationships
        /// are accepted as valid for trading decisions.
        /// </summary>
        [Test]
        public void ValidateDiscrepancies_ValidSnapshot_ReturnsValidResult()
        {
            TestContext.Out.WriteLine("Testing validation of a valid snapshot with proper orderbook data.");
            // Arrange: Valid snapshot with orderbook data and reasonable prices
            var snapshot = new MarketSnapshot
            {
                OrderbookData = new List<Dictionary<string, object>> { new Dictionary<string, object>() },
                BestYesBid = 45,
                BestNoBid = 55,
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
            TestContext.Out.WriteLine("Result: Valid snapshot passed all validation checks.");
        }

        /// <summary>
        /// Tests that a snapshot with missing orderbook data fails validation.
        /// Verifies that snapshots without orderbook information are properly rejected
        /// to prevent trading decisions based on incomplete market data.
        /// </summary>
        [Test]
        public void ValidateDiscrepancies_MissingOrderbook_ReturnsInvalidResult()
        {
            TestContext.Out.WriteLine("Testing validation of a snapshot with missing orderbook data.");
            // Arrange: Snapshot with null/empty orderbook
            var snapshot = new MarketSnapshot
            {
                OrderbookData = null,
                BestYesBid = 45,
                BestNoBid = 55,
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
            TestContext.Out.WriteLine("Result: Snapshot with missing orderbook correctly failed validation.");
        }

        /// <summary>
        /// Tests that a snapshot with overlapping bid prices fails validation.
        /// Verifies that snapshots where the best yes bid is higher than or equal to
        /// the best no bid are properly rejected as they indicate market data corruption.
        /// </summary>
        [Test]
        public void ValidateDiscrepancies_OverlappingPrices_ReturnsInvalidResult()
        {
            TestContext.Out.WriteLine("Testing validation of a snapshot with overlapping bid prices.");
            // Arrange: Yes bid >= No bid (invalid market condition)
            var snapshot = new MarketSnapshot
            {
                OrderbookData = new List<Dictionary<string, object>> { new Dictionary<string, object>() },
                BestYesBid = 55,
                BestNoBid = 45,
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
            TestContext.Out.WriteLine("Result: Snapshot with overlapping prices correctly failed validation.");
        }

        /// <summary>
        /// Tests that a snapshot with rate discrepancy above default threshold fails validation.
        /// Verifies that snapshots with significant imbalances between order and trade volumes
        /// are properly rejected to ensure data quality for trading decisions.
        /// </summary>
        [Test]
        public void ValidateDiscrepancies_RateDiscrepancyAboveThreshold_ReturnsInvalidResult()
        {
            TestContext.Out.WriteLine("Testing validation of a snapshot with rate discrepancy above default threshold.");
            // Arrange: Discrepancy > default threshold (0.1)
            var snapshot = new MarketSnapshot
            {
                OrderbookData = new List<Dictionary<string, object>> { new Dictionary<string, object>() },
                BestYesBid = 8,
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
            TestContext.Out.WriteLine("Result: Snapshot with rate discrepancy above threshold correctly failed validation.");
        }

        /// <summary>
        /// Tests that a snapshot with rate discrepancy below custom threshold passes validation.
        /// Verifies that the validator accepts snapshots when discrepancies are within
        /// acceptable custom threshold limits, allowing for configurable validation rules.
        /// </summary>
        [Test]
        public void ValidateDiscrepancies_RateDiscrepancyBelowCustomThreshold_ReturnsValidResult()
        {
            TestContext.Out.WriteLine("Testing validation of a snapshot with rate discrepancy below custom threshold.");
            // Arrange: Discrepancy < custom threshold (0.5)
            var snapshot = new MarketSnapshot
            {
                OrderbookData = new List<Dictionary<string, object>> { new Dictionary<string, object>() },
                BestYesBid = 8,
                BestNoBid = 9,
                ChangeMetricsMature = true,
                VelocityPerMinute_Top_Yes_Bid = 1.0,
                VelocityPerMinute_Bottom_Yes_Bid = 1.0,
                OrderVolumePerMinute_YesBid = 0.8,
                TradeVolumePerMinute_Yes = 0.8,
                VelocityPerMinute_Top_No_Bid = 1.0,
                VelocityPerMinute_Bottom_No_Bid = 1.0,
                OrderVolumePerMinute_NoBid = 0.8,
                TradeVolumePerMinute_No = 0.8
            };

            // Act
            var result = SnapshotDiscrepancyValidator.ValidateDiscrepancies(snapshot, 0.5);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsOrderbookMissing, Is.False);
            Assert.That(result.DoPricesOverlap, Is.False);
            Assert.That(result.IsRateDiscrepancy, Is.False);
            TestContext.Out.WriteLine("Result: Snapshot with rate discrepancy below custom threshold correctly passed validation.");
        }

        /// <summary>
        /// Tests that a snapshot with rate discrepancy exceeding custom threshold fails validation.
        /// Verifies that the validator correctly rejects snapshots when discrepancies exceed
        /// user-defined threshold limits, ensuring strict data quality control.
        /// </summary>
        [Test]
        public void ValidateDiscrepancies_RateDiscrepancyExceedsCustomThreshold_ReturnsInvalidResult()
        {
            TestContext.Out.WriteLine("Testing validation of a snapshot with rate discrepancy exceeding custom threshold.");
            // Arrange: Discrepancy > custom threshold (0.05)
            var snapshot = new MarketSnapshot
            {
                OrderbookData = new List<Dictionary<string, object>> { new Dictionary<string, object>() },
                BestYesBid = 8,
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
            TestContext.Out.WriteLine("Result: Snapshot with rate discrepancy exceeding custom threshold correctly failed validation.");
        }

        /// <summary>
        /// Tests that rate discrepancy checks are skipped when change metrics are not mature.
        /// Verifies that the validator bypasses rate validation for immature data,
        /// allowing snapshots to pass validation during early market periods.
        /// </summary>
        [Test]
        public void ValidateDiscrepancies_ChangeMetricsNotMature_SkipsRateCheck()
        {
            TestContext.Out.WriteLine("Testing that rate discrepancy checks are skipped when change metrics are not mature.");
            // Arrange: ChangeMetricsMature false, large discrepancy but should be valid
            var snapshot = new MarketSnapshot
            {
                OrderbookData = new List<Dictionary<string, object>> { new Dictionary<string, object>() },
                BestYesBid = 8,
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
            TestContext.Out.WriteLine("Result: Rate discrepancy checks correctly skipped for immature change metrics.");
        }

        /// <summary>
        /// Tests that overlap checks are skipped when best prices are zero.
        /// Verifies that the validator handles edge cases where bid prices are zero,
        /// preventing false overlap detection in illiquid or invalid market conditions.
        /// </summary>
        [Test]
        public void ValidateDiscrepancies_ZeroBestPrices_SkipsOverlapCheck()
        {
            TestContext.Out.WriteLine("Testing that overlap checks are skipped when best prices are zero.");
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
            TestContext.Out.WriteLine("Result: Overlap checks correctly skipped for zero best prices.");
        }
    }
}