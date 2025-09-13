using NUnit.Framework;
using BacklashDTOs;
using BacklashPatterns;
using Microsoft.Extensions.Logging;
using Moq;
using System;

namespace TradingSimulator.Tests
{
    /// <summary>
    /// Unit test suite for TrendCalcs class.
    /// Tests calculation accuracy, input validation, and edge cases.
    /// </summary>
    [TestFixture]
    public class TrendCalcsTests
    {
        private Mock<ILogger> _loggerMock;
        private TrendCalculationConfig _config;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger>();
            _config = new TrendCalculationConfig();
            TrendCalcs.SetConfig(_config);
            TrendCalcs.SetLogger(_loggerMock.Object);
        }

        [Test]
        public void TestCalculateBullishCandleRatio_ValidInput()
        {
            // Arrange
            var prices = new[]
            {
                new CandleMids { Open = 100, Close = 105, High = 106, Low = 99, Volume = 1000 },
                new CandleMids { Open = 105, Close = 102, High = 107, Low = 101, Volume = 1100 },
                new CandleMids { Open = 102, Close = 108, High = 109, Low = 100, Volume = 1200 },
                new CandleMids { Open = 108, Close = 106, High = 110, Low = 105, Volume = 1300 },
                new CandleMids { Open = 106, Close = 104, High = 108, Low = 103, Volume = 1400 }
            };
            int index = 4;
            int lookback = 3;

            // Act
            var result = TrendCalcs.CalculateBullishCandleRatio(index, lookback, prices);

            // Assert
            // Candles 1, 3 are bullish (2 out of 3)
            var expected = (2.0 / 3.0) + (_config.SmoothingOffset / 3.0) / 2.0;
            expected = Math.Min(expected, 1.0);
            Assert.That(result, Is.EqualTo(expected).Within(0.001));
        }

        [Test]
        public void TestCalculateBullishCandleRatio_NullPrices()
        {
            // Arrange
            CandleMids[] prices = null;
            int index = 0;
            int lookback = 5;

            // Act
            var result = TrendCalcs.CalculateBullishCandleRatio(index, lookback, prices);

            // Assert
            Assert.That(result, Is.EqualTo(0.0));
            _loggerMock.Verify(l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Test]
        public void TestCalculateBullishCandleRatio_InvalidIndex()
        {
            // Arrange
            var prices = new[] { new CandleMids { Open = 100, Close = 105 } };
            int index = 5; // Out of bounds
            int lookback = 5;

            // Act
            var result = TrendCalcs.CalculateBullishCandleRatio(index, lookback, prices);

            // Assert
            Assert.That(result, Is.EqualTo(0.0));
            _loggerMock.Verify(l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Test]
        public void TestCalculateBullishCandleRatio_LookbackOutOfRange()
        {
            // Arrange
            var prices = new[] { new CandleMids { Open = 100, Close = 105 } };
            int index = 0;
            int lookback = 2000; // Above max

            // Act
            var result = TrendCalcs.CalculateBullishCandleRatio(index, lookback, prices);

            // Assert
            Assert.That(result, Is.EqualTo(0.0)); // Since lookback gets clamped and no data
            _loggerMock.Verify(l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Test]
        public async Task TestCalculateBullishCandleRatioAsync()
        {
            // Arrange
            var prices = new[]
            {
                new CandleMids { Open = 100, Close = 105 },
                new CandleMids { Open = 105, Close = 102 },
                new CandleMids { Open = 102, Close = 108 }
            };
            int index = 2;
            int lookback = 2;

            // Act
            var result = await TrendCalcs.CalculateBullishCandleRatioAsync(index, lookback, prices);

            // Assert
            var expected = (2.0 / 2.0) + (_config.SmoothingOffset / 2.0) / 2.0;
            expected = Math.Min(expected, 1.0);
            Assert.That(result, Is.EqualTo(expected).Within(0.001));
        }

        [Test]
        public void TestCalculateTrendConsistencyRatio_ValidInput()
        {
            // Arrange
            var prices = new[]
            {
                new CandleMids { Open = 100, Close = 105 }, // Up
                new CandleMids { Open = 105, Close = 102 }, // Down
                new CandleMids { Open = 102, Close = 108 }, // Up
                new CandleMids { Open = 108, Close = 106 }, // Down
                new CandleMids { Open = 106, Close = 104 }  // Down
            };
            int index = 4;
            int lookback = 3;
            int patternSize = 1;

            // Act
            var result = TrendCalcs.CalculateTrendConsistencyRatio(index, lookback, prices, patternSize);

            // Assert
            // Changes: +5, -3, +6, -2 -> consistent count = 4 (all non-zero)
            var unweighted = 4.0 / 3.0;
            var expected = unweighted + (_config.SmoothingOffset / 3.0) / 2.0;
            expected = Math.Min(expected, 1.0);
            Assert.That(result, Is.EqualTo(expected).Within(0.001));
        }

        [Test]
        public void TestCalculateTrendDirectionRatio_Bullish()
        {
            // Arrange
            var prices = new[]
            {
                new CandleMids { Open = 100, Close = 105 }, // Up
                new CandleMids { Open = 105, Close = 102 }, // Down
                new CandleMids { Open = 102, Close = 108 }, // Up
                new CandleMids { Open = 108, Close = 106 }, // Down
                new CandleMids { Open = 106, Close = 104 }  // Down
            };
            int index = 4;
            int lookback = 3;
            int patternSize = 1;
            bool isBullish = true;

            // Act
            var result = TrendCalcs.CalculateTrendDirectionRatio(index, lookback, prices, patternSize, isBullish);

            // Assert
            // Bullish changes: 2 out of 3
            var unweighted = 2.0 / 3.0;
            var expected = unweighted + (_config.SmoothingOffset / 3.0) / 2.0;
            expected = Math.Min(expected, 1.0);
            Assert.That(result, Is.EqualTo(expected).Within(0.001));
        }
    }
}