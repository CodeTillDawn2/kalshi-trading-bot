using NUnit.Framework;
using BacklashDTOs;
using BacklashPatterns;
using BacklashPatterns.PatternDefinitions;
using TradingStrategies.Strategies;
using TradingStrategies.Trading.Overseer;
using static BacklashInterfaces.Enums.StrategyEnums;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using static NUnit.Framework.Assert;

namespace KalshiBotTests
{
    [TestFixture]
    public class PatternTests
    {
        [Test]
        public async Task TwoCrowsPattern_IsPatternAsync_ValidPattern_ReturnsPattern()
        {
            // Arrange
            var prices = new CandleMids[5];
            prices[0] = new CandleMids { Open = 50, Close = 60, High = 65, Low = 45, Volume = 1000 }; // Bullish
            prices[1] = new CandleMids { Open = 65, Close = 55, High = 70, Low = 50, Volume = 1000 }; // Bearish, gaps up
            prices[2] = new CandleMids { Open = 60, Close = 45, High = 65, Low = 40, Volume = 1000 }; // Bearish, closes below
            prices[3] = new CandleMids { Open = 45, Close = 50, High = 55, Low = 40, Volume = 1000 };
            prices[4] = new CandleMids { Open = 50, Close = 55, High = 60, Low = 45, Volume = 1000 };

            var metricsCache = new Dictionary<int, CandleMetrics>();

            // Act
            var pattern = await TwoCrowsPattern.IsPatternAsync(2, 3, prices, metricsCache);

            // Assert
            IsNotNull(pattern);
            AreEqual(PatternDirection.Bearish, pattern.Direction);
            AreEqual("TwoCrows_Bearish", pattern.Name);
            AreEqual(3, pattern.Candles.Count);
            Contains(0, pattern.Candles);
            Contains(1, pattern.Candles);
            Contains(2, pattern.Candles);
        }

        [Test]
        public async Task TwoCrowsPattern_IsPatternAsync_Invalid_NoBullishFirstCandle_ReturnsNull()
        {
            // Arrange
            var prices = new CandleMids[5];
            prices[0] = new CandleMids { Open = 60, Close = 50, High = 65, Low = 45, Volume = 1000 }; // Bearish
            prices[1] = new CandleMids { Open = 65, Close = 55, High = 70, Low = 50, Volume = 1000 };
            prices[2] = new CandleMids { Open = 60, Close = 45, High = 65, Low = 40, Volume = 1000 };
            prices[3] = new CandleMids { Open = 45, Close = 50, High = 55, Low = 40, Volume = 1000 };
            prices[4] = new CandleMids { Open = 50, Close = 55, High = 60, Low = 45, Volume = 1000 };

            var metricsCache = new Dictionary<int, CandleMetrics>();

            // Act
            var pattern = await TwoCrowsPattern.IsPatternAsync(2, 3, prices, metricsCache);

            // Assert
            Assert.IsNull(pattern);
        }

        [Test]
        public async Task TwoCrowsPattern_IsPatternAsync_Invalid_InsufficientCandles_ReturnsNull()
        {
            // Arrange
            var prices = new CandleMids[2];
            prices[0] = new CandleMids { Open = 50, Close = 60, High = 65, Low = 45, Volume = 1000 };
            prices[1] = new CandleMids { Open = 65, Close = 55, High = 70, Low = 50, Volume = 1000 };

            var metricsCache = new Dictionary<int, CandleMetrics>();

            // Act
            var pattern = await TwoCrowsPattern.IsPatternAsync(2, 3, prices, metricsCache);

            // Assert
            Assert.IsNull(pattern);
        }

        [Test]
        public async Task ShootingStarPattern_IsPatternAsync_ValidPattern_ReturnsPattern()
        {
            // Arrange: Shooting star in uptrend
            var prices = new CandleMids[5];
            prices[0] = new CandleMids { Open = 50, Close = 55, High = 60, Low = 45, Volume = 1000 };
            prices[1] = new CandleMids { Open = 55, Close = 60, High = 65, Low = 50, Volume = 1000 };
            prices[2] = new CandleMids { Open = 60, Close = 61, High = 75, Low = 58, Volume = 1000 }; // Small body, long upper wick
            prices[3] = new CandleMids { Open = 61, Close = 65, High = 70, Low = 60, Volume = 1000 };
            prices[4] = new CandleMids { Open = 65, Close = 70, High = 75, Low = 60, Volume = 1000 };

            var metricsCache = new Dictionary<int, CandleMetrics>();

            // Act
            var pattern = await ShootingStarPattern.IsPatternAsync(2, 3, prices, metricsCache);

            // Assert
            Assert.IsNotNull(pattern);
            Assert.AreEqual(PatternDirection.Bearish, pattern.Direction);
        }

        [Test]
        public async Task DojiPattern_IsPatternAsync_ValidPattern_ReturnsPattern()
        {
            // Arrange: Doji with small body, balanced wicks, after trend
            var prices = new CandleMids[10];
            for (int i = 0; i < 9; i++)
            {
                prices[i] = new CandleMids { Open = 10 + i * 0.5, Close = 10.5 + i * 0.5, High = 11 + i * 0.5, Low = 9.5 + i * 0.5, Volume = 1000 }; // Uptrend
            }
            prices[9] = new CandleMids { Open = 14.75, Close = 14.8, High = 16, Low = 13.5, Volume = 1000 }; // Small body Doji

            var metricsCache = new Dictionary<int, CandleMetrics>();

            // Act
            var pattern = await DojiPattern.IsPatternAsync(metricsCache, 9, 5, prices);

            // Assert
            Assert.IsNotNull(pattern);
            Assert.AreEqual(PatternDirection.Neutral, pattern.Direction);
            Assert.AreEqual("Doji", pattern.Name);
            Assert.AreEqual(1, pattern.Candles.Count);
            Assert.Contains(9, pattern.Candles);
        }

        [Test]
        public async Task DojiPattern_IsPatternAsync_Invalid_LargeBody_ReturnsNull()
        {
            // Arrange: Candle with large body
            var prices = new CandleMids[10];
            for (int i = 0; i < 9; i++)
            {
                prices[i] = new CandleMids { Open = 10 + i * 0.5, Close = 10.5 + i * 0.5, High = 11 + i * 0.5, Low = 9.5 + i * 0.5, Volume = 1000 };
            }
            prices[9] = new CandleMids { Open = 12.5, Close = 17.5, High = 20, Low = 10, Volume = 1000 }; // Large body

            var metricsCache = new Dictionary<int, CandleMetrics>();

            // Act
            var pattern = await DojiPattern.IsPatternAsync(metricsCache, 9, 5, prices);

            // Assert
            IsNull(pattern);
        }

        [Test]
        public async Task EngulfingPattern_IsPatternAsync_BullishValidPattern_ReturnsPattern()
        {
            // Arrange: Bullish engulfing in downtrend
            var prices = new CandleMids[5];
            prices[0] = new CandleMids { Open = 60, Close = 55, High = 65, Low = 50, Volume = 1000 }; // Bearish
            prices[1] = new CandleMids { Open = 55, Close = 60, High = 65, Low = 50, Volume = 1000 }; // Bullish engulfing
            prices[2] = new CandleMids { Open = 60, Close = 65, High = 70, Low = 55, Volume = 1000 };
            prices[3] = new CandleMids { Open = 65, Close = 70, High = 75, Low = 60, Volume = 1000 };
            prices[4] = new CandleMids { Open = 70, Close = 75, High = 80, Low = 65, Volume = 1000 };

            var metricsCache = new Dictionary<int, CandleMetrics>();
            double meanTrend = -0.5; // Downtrend

            // Act
            var pattern = await EngulfingPattern.IsPatternAsync(1, metricsCache, prices, meanTrend, true);

            // Assert
            Assert.IsNotNull(pattern);
            Assert.AreEqual(PatternDirection.Bullish, pattern.Direction);
            Assert.AreEqual("Engulfing_Bullish", pattern.Name);
            Assert.AreEqual(2, pattern.Candles.Count);
        }

        // Add tests for CalculateStrength if possible, but may require mocking HistoricalPatternCache
    }
}

[TestFixture]
public class StrategyTests
{
    private class MockStrat : Strat
    {
        private readonly ActionDecision _decision;
        private readonly double _weight;

        public MockStrat(ActionDecision decision, double weight)
        {
            _decision = decision;
            _weight = weight;
        }

        public override double Weight => _weight;

        public override ActionDecision GetAction(MarketSnapshot snapshot, MarketSnapshot? previousSnapshot, int simulationPosition = 0)
        {
            return _decision;
        }

        public override string ToJson()
        {
            return "{}";
        }
    }

    [Test]
    public void GetAction_NoStrats_ReturnsNone()
    {
        // Arrange
        var strategy = new Strategy("Test", new List<Strat>());

        // Act
        var result = strategy.GetAction(null, null);

        // Assert
        Assert.AreEqual(ActionType.None, result.Type);
        Assert.AreEqual("No action votes", result.Memo);
    }

    [Test]
    public void GetAction_SingleStrat_ReturnsItsDecision()
    {
        // Arrange
        var decision = new ActionDecision { Type = ActionType.Long, Memo = "Test long" };
        var strat = new MockStrat(decision, 1.0);
        var strategy = new Strategy("Test", new List<Strat> { strat });

        // Act
        var result = strategy.GetAction(null, null);

        // Assert
        Assert.AreEqual(ActionType.Long, result.Type);
        Assert.AreEqual("Test long", result.Memo);
    }

    [Test]
    public void GetAction_MultipleStrats_SameAction_ReturnsHighestWeightDecision()
    {
        // Arrange
        var decision1 = new ActionDecision { Type = ActionType.Long, Memo = "Long low" };
        var decision2 = new ActionDecision { Type = ActionType.Long, Memo = "Long high" };
        var strat1 = new MockStrat(decision1, 0.5);
        var strat2 = new MockStrat(decision2, 1.0);
        var strategy = new Strategy("Test", new List<Strat> { strat1, strat2 });

        // Act
        var result = strategy.GetAction(null, null);

        // Assert
        Assert.AreEqual(ActionType.Long, result.Type);
        Assert.AreEqual("Long high", result.Memo); // Highest weight wins
    }

    [Test]
    public void GetAction_MultipleStrats_DifferentActions_SelectsMajorityAction()
    {
        // Arrange
        var longDecision = new ActionDecision { Type = ActionType.Long, Memo = "Long" };
        var shortDecision = new ActionDecision { Type = ActionType.Short, Memo = "Short" };
        var noneDecision = new ActionDecision { Type = ActionType.None, Memo = "None" };
        var strat1 = new MockStrat(longDecision, 1.0); // Long with 1.0
        var strat2 = new MockStrat(shortDecision, 0.5); // Short with 0.5
        var strat3 = new MockStrat(noneDecision, 0.3); // None with 0.3
        var strategy = new Strategy("Test", new List<Strat> { strat1, strat2, strat3 });

        // Act
        var result = strategy.GetAction(null, null);

        // Assert
        Assert.AreEqual(ActionType.Long, result.Type); // Long has highest total weight (1.0 > 0.5 > 0.3)
        Assert.AreEqual("Long", result.Memo);
    }
}

[TestFixture]
public class NothingEverHappensStratTests
{
    [Test]
    public void GetAction_ChangeMetricsNotMature_ReturnsNone()
    {
        // Arrange
        var strat = new NothingEverHappensStrat();
        var snapshot = new MarketSnapshot { ChangeMetricsMature = false };

        // Act
        var result = strat.GetAction(snapshot, null);

        // Assert
        Assert.AreEqual(ActionType.None, result.Type);
        Assert.AreEqual("Change metrics not mature", result.Memo);
    }

    [Test]
    public void GetAction_NoPosition_BearishConditions_ReturnsShort()
    {
        // Arrange
        var strat = new NothingEverHappensStrat();
        var snapshot = new MarketSnapshot
        {
            ChangeMetricsMature = true,
            BestYesBid = 60,
            BestYesAsk = 61,
            BestNoBid = 39,
            EMA_Medium = 58, // Price below EMA
            MACD_Medium = new MACDData { Histogram = -1 }, // Negative histogram
            RSI_Medium = 75, // Overbought
            StochasticOscillator_Medium = new StochasticData { K = 85, D = 80 }, // Overbought
            OBV_Medium = -10, // Negative OBV
            BollingerBands_Medium = new BollingerBandsData { Upper = 59 }, // Above upper
            TradeVolumePerMinute_No = 100, // High volume
            TotalBidContracts_No = 500, // For absorption
            BidCountImbalance = 0
        };

        // Act
        var result = strat.GetAction(snapshot, null, 0);

        // Assert
        Assert.AreEqual(ActionType.Short, result.Type);
    }

    [Test]
    public void GetAction_ShortPosition_ExitConditions_ReturnsExit()
    {
        // Arrange
        var strat = new NothingEverHappensStrat();
        var snapshot = new MarketSnapshot
        {
            ChangeMetricsMature = true,
            BestYesBid = 60,
            BestYesAsk = 61,
            BestNoBid = 39,
            VelocityPerMinute_Top_Yes_Bid = 50, // High velocity
            TotalBidContracts_Yes = 1000, // Dollar depth
            BidCountImbalance = 0,
            MACD_Medium = new MACDData { Histogram = 1 }, // Positive histogram
            TradeVolumePerMinute_Yes = 100 // High volume
        };

        // Act
        var result = strat.GetAction(snapshot, null, -1);

        // Assert
        Assert.AreEqual(ActionType.Exit, result.Type);
    }

    [Test]
    public void ToJson_FromJson_RoundTrip()
    {
        // Arrange
        var original = new NothingEverHappensStrat("TestStrat", 2.0);

        // Act
        var json = original.ToJson();
        var deserialized = NothingEverHappensStrat.FromJson(json);

        // Assert
        Assert.AreEqual(original.Name, deserialized.Name);
        Assert.AreEqual(original.Weight, deserialized.Weight);
    }
}

[TestFixture]
public class MarketSnapshotTests
{
    [Test]
    public void CalculateLiquidityScore_IlliquidMarket_ReturnsZero()
    {
        // Arrange
        var snapshot = new MarketSnapshot
        {
            BestYesBid = 0,
            BestNoBid = 0,
            TotalBidContracts_Yes = 0,
            TotalBidContracts_No = 0
        };

        // Act
        var score = snapshot.CalculateLiquidityScore();

        // Assert
        Assert.AreEqual(0.0, score);
    }

    [Test]
    public void CalculateLiquidityScore_LiquidMarket_ReturnsPositiveScore()
    {
        // Arrange
        var snapshot = new MarketSnapshot
        {
            BestYesBid = 60,
            BestNoBid = 40,
            YesSpread = 2,
            NoSpread = 2,
            DepthAtBestYesBid = 100,
            DepthAtBestNoBid = 100,
            TopTenPercentLevelDepth_Yes = 500,
            TopTenPercentLevelDepth_No = 500,
            RecentVolume_LastHour = 1000,
            TotalOrderbookDepth_Yes = 10000,
            TotalOrderbookDepth_No = 10000,
            TolerancePercentage = 5.0,
            PositionSize = 0,
            OrderbookData = new List<Dictionary<string, object>>
            {
                new() { ["side"] = "yes", ["price"] = 60, ["resting_contracts"] = 100 },
                new() { ["side"] = "no", ["price"] = 40, ["resting_contracts"] = 100 }
            }
        };

        // Act
        var score = snapshot.CalculateLiquidityScore();

        // Assert
        Assert.Greater(score, 0.0);
        Assert.LessOrEqual(score, 100.0);
    }
}

[TestFixture]
public class StrategyTests
{
    private class MockStrat : Strat
    {
        private readonly ActionDecision _decision;
        private readonly double _weight;

        public MockStrat(ActionDecision decision, double weight)
        {
            _decision = decision;
            _weight = weight;
        }

        public override double Weight => _weight;

        public override ActionDecision GetAction(MarketSnapshot snapshot, MarketSnapshot? previousSnapshot, int simulationPosition = 0)
        {
            return _decision;
        }

        public override string ToJson()
        {
            return "{}";
        }
    }

    [Test]
    public void GetAction_NoStrats_ReturnsNone()
    {
        // Arrange
        var strategy = new Strategy("Test", new List<Strat>());

        // Act
        var result = strategy.GetAction(null, null);

        // Assert
        Assert.AreEqual(ActionType.None, result.Type);
        Assert.AreEqual("No action votes", result.Memo);
    }

    [Test]
    public void GetAction_SingleStrat_ReturnsItsDecision()
    {
        // Arrange
        var decision = new ActionDecision { Type = ActionType.Long, Memo = "Test long" };
        var strat = new MockStrat(decision, 1.0);
        var strategy = new Strategy("Test", new List<Strat> { strat });

        // Act
        var result = strategy.GetAction(null, null);

        // Assert
        Assert.AreEqual(ActionType.Long, result.Type);
        Assert.AreEqual("Test long", result.Memo);
    }

    [Test]
    public void GetAction_MultipleStrats_SameAction_ReturnsHighestWeightDecision()
    {
        // Arrange
        var decision1 = new ActionDecision { Type = ActionType.Long, Memo = "Long low" };
        var decision2 = new ActionDecision { Type = ActionType.Long, Memo = "Long high" };
        var strat1 = new MockStrat(decision1, 0.5);
        var strat2 = new MockStrat(decision2, 1.0);
        var strategy = new Strategy("Test", new List<Strat> { strat1, strat2 });

        // Act
        var result = strategy.GetAction(null, null);

        // Assert
        Assert.AreEqual(ActionType.Long, result.Type);
        Assert.AreEqual("Long high", result.Memo); // Highest weight wins
    }

    [Test]
    public void GetAction_MultipleStrats_DifferentActions_SelectsMajorityAction()
    {
        // Arrange
        var longDecision = new ActionDecision { Type = ActionType.Long, Memo = "Long" };
        var shortDecision = new ActionDecision { Type = ActionType.Short, Memo = "Short" };
        var noneDecision = new ActionDecision { Type = ActionType.None, Memo = "None" };
        var strat1 = new MockStrat(longDecision, 1.0); // Long with 1.0
        var strat2 = new MockStrat(shortDecision, 0.5); // Short with 0.5
        var strat3 = new MockStrat(noneDecision, 0.3); // None with 0.3
        var strategy = new Strategy("Test", new List<Strat> { strat1, strat2, strat3 });

        // Act
        var result = strategy.GetAction(null, null);

        // Assert
        Assert.AreEqual(ActionType.Long, result.Type); // Long has highest total weight (1.0 > 0.5 > 0.3)
        Assert.AreEqual("Long", result.Memo);
    }
}