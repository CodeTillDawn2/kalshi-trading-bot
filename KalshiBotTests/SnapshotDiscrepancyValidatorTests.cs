using NUnit.Framework;
using BacklashDTOs;
using BacklashBot.Helpers;
using System.Collections.Generic;

namespace KalshiBotTests
{
    [TestFixture]
    public class SnapshotDiscrepancyValidatorTests
    {
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