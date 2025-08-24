using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SmokehouseBot.Helpers;
using SmokehouseBot.Services;
using TradingStrategies.Configuration;

namespace TradingSimulator.Tests
{
    public class CalculationTests
    {
        private TradingSnapshotService _snapshotService;
        private TradingCalculator _tradingCalculator;
        private string _tempSnapshotDir;
        private Mock<ILogger<TradingSnapshotService>> _tradingSnapshotServiceLoggerMock;
        private Mock<ILogger<TradingCalculator>> _tradingCalculatorLoggerMock;
        private IOptions<SnapshotConfig> _snapshotOptions;
        private IOptions<CalculationConfig> _calculationOptions;
        private IOptions<TradingConfig> _tradingOptions;
        private double _marginFactor;
        private IServiceScopeFactory _scopeFactory;

        [SetUp]
        public void Setup()
        {
            _tempSnapshotDir = Path.Combine(Path.GetTempPath(), $"TradingSimulator_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempSnapshotDir);

            _scopeFactory = new Mock<IServiceScopeFactory>().Object;
            _tradingSnapshotServiceLoggerMock = new Mock<ILogger<TradingSnapshotService>>();
            _tradingCalculatorLoggerMock = new Mock<ILogger<TradingCalculator>>();
            _tradingCalculator = new TradingCalculator(_tradingCalculatorLoggerMock.Object);
            _tradingOptions = TestHelper.GetTradingConfig();
            _calculationOptions = TestHelper.GetCalculationConfig();
            _marginFactor = 0.001; // 0.1% margin factor

            _snapshotService = new TradingSnapshotService(_tradingSnapshotServiceLoggerMock.Object, _snapshotOptions, _tradingOptions, _scopeFactory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempSnapshotDir))
            {
                Directory.Delete(_tempSnapshotDir, true);
            }
        }



        [Test]
        public void TestRSICalculation()
        {
            foreach (var scenario in TradingMetricScenarios.GetRSIScenarios())
            {
                // Arrange
                var candlesticks = scenario.Candlesticks.TakeLast(13 + 1).ToList();
                Console.WriteLine($"Scenario: {scenario.Name}, Candlesticks Count: {candlesticks.Count}, Periods: 13");
                Console.WriteLine($"Prices: [{string.Join(", ", candlesticks.Select(c => c.MidClose))}]");

                // Act
                var rsi = _tradingCalculator.CalculateRSI(candlesticks, 13);
                Console.WriteLine($"Calculated RSI: {rsi}");

                // Assert
                if (!scenario.ExpectedRSI.HasValue)
                    throw new InvalidOperationException($"{scenario.Name}: Expected RSI value is null");
                var expected = scenario.ExpectedRSI.Value;
                var absoluteDiff = Math.Abs((decimal)(rsi - expected));
                var percentageDiff = expected != 0 ? (absoluteDiff / Math.Abs((decimal)(scenario.ExpectedRSI.Value))) * 100 : absoluteDiff;
                Console.WriteLine($"{scenario.Name}: RSI Percentage Difference: {(expected != 0 ? $"{percentageDiff:F6}%" : $"Absolute Diff: {absoluteDiff:F6}")}");
                var margin = _marginFactor * Math.Abs(scenario.ExpectedRSI.Value);
                Assert.That(rsi, Is.EqualTo((double)scenario.ExpectedRSI.Value).Within(margin), $"{scenario.Name}: RSI mismatch. Expected: {expected}, Actual: {rsi}");
            }
        }

        [Test]
        public void TestMACDCalculation()
        {
            foreach (var scenario in TradingMetricScenarios.GetMACDScenarios())
            {
                // Arrange
                var candlesticks = scenario.Candlesticks; // Use full 34 candlesticks for MACD

                // Act
                var (macdLine, signalLine, histogram) = _tradingCalculator.CalculateMACD(candlesticks, 12, 26, 9);

                // Assert
                if (!scenario.ExpectedMACD.macdLine.HasValue || !scenario.ExpectedMACD.signalLine.HasValue || !scenario.ExpectedMACD.histogram.HasValue)
                    throw new InvalidOperationException($"{scenario.Name}: Expected MACD values contain null");

                var expectedMacd = scenario.ExpectedMACD.macdLine.Value;
                var macdDiff = Math.Abs((decimal)(macdLine - expectedMacd));
                var macdPercentageDiff = expectedMacd != 0 ? (macdDiff / Math.Abs((decimal)(scenario.ExpectedMACD.macdLine.Value))) * 100 : macdDiff;
                Console.WriteLine($"{scenario.Name}: MACD Line Percentage Difference: {(expectedMacd != 0 ? $"{macdPercentageDiff:F6}%" : $"Absolute Diff: {macdDiff:F6}")}");
                var macdMargin = _marginFactor * Math.Abs(scenario.ExpectedMACD.macdLine.Value);
                Assert.That(macdLine, Is.EqualTo((double)scenario.ExpectedMACD.macdLine.Value).Within(macdMargin), $"{scenario.Name}: MACD Line mismatch");

                var expectedSignal = scenario.ExpectedMACD.signalLine.Value;
                var signalDiff = Math.Abs((decimal)(signalLine - expectedSignal));
                var signalPercentageDiff = expectedSignal != 0 ? (signalDiff / Math.Abs((decimal)(scenario.ExpectedMACD.signalLine.Value))) * 100 : signalDiff;
                Console.WriteLine($"{scenario.Name}: Signal Line Percentage Difference: {(expectedSignal != 0 ? $"{signalPercentageDiff:F6}%" : $"Absolute Diff: {signalDiff:F6}")}");
                var signalMargin = _marginFactor * Math.Abs(scenario.ExpectedMACD.signalLine.Value);
                Assert.That(signalLine, Is.EqualTo((double)scenario.ExpectedMACD.signalLine.Value).Within(signalMargin), $"{scenario.Name}: Signal Line mismatch");

                var expectedHistogram = scenario.ExpectedMACD.histogram.Value;
                var histogramDiff = Math.Abs((decimal)(histogram - expectedHistogram));
                var histogramPercentageDiff = expectedHistogram != 0 ? (histogramDiff / Math.Abs((decimal)(scenario.ExpectedMACD.histogram.Value))) * 100 : histogramDiff;
                Console.WriteLine($"{scenario.Name}: Histogram Percentage Difference: {(expectedHistogram != 0 ? $"{histogramPercentageDiff:F6}%" : $"Absolute Diff: {histogramDiff:F6}")}");
                var histogramMargin = _marginFactor * Math.Abs(scenario.ExpectedMACD.histogram.Value);
                Assert.That(histogram, Is.EqualTo((double)scenario.ExpectedMACD.histogram.Value).Within(histogramMargin), $"{scenario.Name}: Histogram mismatch");
            }
        }

        [Test]
        public void TestEMACalculation()
        {
            foreach (var scenario in TradingMetricScenarios.GetEMAScenarios())
            {
                // Arrange
                var candlesticks = scenario.Candlesticks; // Use 5 candlesticks from source

                // Act
                var result = TradingCalculations.CalculateEMA(candlesticks.Select(c => c.MidClose).ToList(), 5);

                // Assert
                if (!scenario.ExpectedEMA.HasValue)
                    throw new InvalidOperationException($"{scenario.Name}: Expected EMA value is null");
                var expected = scenario.ExpectedEMA.Value;
                var absoluteDiff = Math.Abs((decimal)(result - expected));
                var percentageDiff = expected != 0 ? (absoluteDiff / Math.Abs((decimal)(scenario.ExpectedEMA.Value))) * 100 : absoluteDiff;
                Console.WriteLine($"{scenario.Name}: EMA Percentage Difference: {(expected != 0 ? $"{percentageDiff:F6}%" : $"Absolute Diff: {absoluteDiff:F6}")}");
                var margin = _marginFactor * Math.Abs(scenario.ExpectedEMA.Value);
                Assert.That(result, Is.EqualTo((double)scenario.ExpectedEMA.Value).Within(margin), $"{scenario.Name}: EMA mismatch. Expected: {expected}, Actual: {result}");
            }
        }

        [Test]
        public void TestBollingerBandsCalculation()
        {
            foreach (var scenario in TradingMetricScenarios.GetBollingerBandsScenarios())
            {
                // Arrange
                var candlesticks = scenario.Candlesticks; // Use full 14 candlesticks from source

                // Act
                var (middle, upper, lower) = _tradingCalculator.CalculateBollingerBands(candlesticks, 14, 2);

                // Assert
                if (!scenario.ExpectedBollingerBands.middle.HasValue || !scenario.ExpectedBollingerBands.upper.HasValue || !scenario.ExpectedBollingerBands.lower.HasValue)
                    throw new InvalidOperationException($"{scenario.Name}: Expected Bollinger Bands values contain null");

                var expectedMiddle = scenario.ExpectedBollingerBands.middle.Value;
                var middleDiff = Math.Abs((decimal)(middle - expectedMiddle));
                var middlePercentageDiff = expectedMiddle != 0 ? (middleDiff / Math.Abs((decimal)(scenario.ExpectedBollingerBands.middle.Value))) * 100 : middleDiff;
                Console.WriteLine($"{scenario.Name}: Middle Band Percentage Difference: {(expectedMiddle != 0 ? $"{middlePercentageDiff:F6}%" : $"Absolute Diff: {middleDiff:F6}")}");
                var middleMargin = _marginFactor * Math.Abs(scenario.ExpectedBollingerBands.middle.Value);
                Assert.That(middle, Is.EqualTo((double)scenario.ExpectedBollingerBands.middle.Value).Within(middleMargin), $"{scenario.Name}: Middle band mismatch");

                var expectedUpper = scenario.ExpectedBollingerBands.upper.Value;
                var upperDiff = Math.Abs((decimal)(upper - expectedUpper));
                var upperPercentageDiff = expectedUpper != 0 ? (upperDiff / Math.Abs((decimal)(scenario.ExpectedBollingerBands.upper.Value))) * 100 : upperDiff;
                Console.WriteLine($"{scenario.Name}: Upper Band Percentage Difference: {(expectedUpper != 0 ? $"{upperPercentageDiff:F6}%" : $"Absolute Diff: {upperDiff:F6}")}");
                var upperMargin = _marginFactor * Math.Abs(scenario.ExpectedBollingerBands.upper.Value);
                Assert.That(upper, Is.EqualTo((double)scenario.ExpectedBollingerBands.upper.Value).Within(upperMargin), $"{scenario.Name}: Upper band mismatch");

                var expectedLower = scenario.ExpectedBollingerBands.lower.Value;
                var lowerDiff = Math.Abs((decimal)(lower - expectedLower));
                var lowerPercentageDiff = expectedLower != 0 ? (lowerDiff / Math.Abs((decimal)(scenario.ExpectedBollingerBands.lower.Value))) * 100 : lowerDiff;
                Console.WriteLine($"{scenario.Name}: Lower Band Percentage Difference: {(expectedLower != 0 ? $"{lowerPercentageDiff:F6}%" : $"Absolute Diff: {lowerDiff:F6}")}");
                var lowerMargin = _marginFactor * Math.Abs(scenario.ExpectedBollingerBands.lower.Value);
                Assert.That(lower, Is.EqualTo((double)scenario.ExpectedBollingerBands.lower.Value).Within(lowerMargin), $"{scenario.Name}: Lower band mismatch");
            }
        }

        [Test]
        public void TestATRCalculation()
        {
            foreach (var scenario in TradingMetricScenarios.GetATRScenarios())
            {
                // Arrange
                var candlesticks = scenario.Candlesticks; // Use full 14 candlesticks from source

                // Act
                var result = _tradingCalculator.CalculateATR(candlesticks, 13); //Use one less than full number, it needs one more for history

                // Assert
                if (!scenario.ExpectedATR.HasValue)
                    throw new InvalidOperationException($"{scenario.Name}: Expected ATR value is null");
                var expected = scenario.ExpectedATR.Value;
                var absoluteDiff = Math.Abs((decimal)(result - expected));
                var percentageDiff = expected != 0 ? (absoluteDiff / Math.Abs((decimal)(scenario.ExpectedATR.Value))) * 100 : absoluteDiff;
                Console.WriteLine($"{scenario.Name}: ATR Percentage Difference: {(expected != 0 ? $"{percentageDiff:F6}%" : $"Absolute Diff: {absoluteDiff:F6}")}");
                var margin = _marginFactor * Math.Abs(scenario.ExpectedATR.Value);
                Assert.That(result, Is.EqualTo((double)scenario.ExpectedATR.Value).Within(margin), $"{scenario.Name}: ATR mismatch. Expected: {expected}, Actual: {result}");
            }
        }

        [Test]
        public void TestVWAPCalculation()
        {
            foreach (var scenario in TradingMetricScenarios.GetVWAPScenarios())
            {
                // Arrange
                var candlesticks = scenario.Candlesticks; // Use full 17 candlesticks from source

                // Act
                var result = _tradingCalculator.CalculateVWAP(candlesticks, 17);

                // Assert
                if (!scenario.ExpectedVWAP.HasValue)
                    throw new InvalidOperationException($"{scenario.Name}: Expected VWAP value is null");
                var expected = scenario.ExpectedVWAP.Value;
                var absoluteDiff = Math.Abs((result.Value - (decimal)expected));
                var percentageDiff = expected != 0 ? (absoluteDiff / Math.Abs((decimal)(scenario.ExpectedVWAP.Value))) * 100 : absoluteDiff;
                Console.WriteLine($"{scenario.Name}: VWAP Percentage Difference: {(expected != 0 ? $"{percentageDiff:F6}%" : $"Absolute Diff: {absoluteDiff:F6}")}");
                var margin = _marginFactor * Math.Abs(scenario.ExpectedVWAP.Value);
                Assert.That(result, Is.EqualTo((double)scenario.ExpectedVWAP.Value).Within(margin), $"{scenario.Name}: VWAP mismatch. Expected: {expected}, Actual: {result}");
            }
        }

        [Test]
        public void TestStochasticOscillatorCalculation()
        {
            foreach (var scenario in TradingMetricScenarios.GetStochasticScenarios())
            {
                // Arrange
                var candlesticks = scenario.Candlesticks; // Use full 17 candlesticks from source

                // Act
                var (kResult, dResult) = _tradingCalculator.CalculateStochastic(candlesticks, 14, 3);

                // Assert
                if (!scenario.ExpectedStochastic.k.HasValue)
                    throw new InvalidOperationException($"{scenario.Name}: Expected %K value is null");
                var expectedK = scenario.ExpectedStochastic.k.Value;
                var kDiff = Math.Abs((decimal)(kResult - expectedK));
                var kPercentageDiff = expectedK != 0 ? (kDiff / Math.Abs((decimal)(scenario.ExpectedStochastic.k.Value))) * 100 : kDiff;
                Console.WriteLine($"{scenario.Name}: %K Percentage Difference: {(expectedK != 0 ? $"{kPercentageDiff:F6}%" : $"Absolute Diff: {kDiff:F6}")}");
                var kMargin = _marginFactor * Math.Abs(scenario.ExpectedStochastic.k.Value);
                Assert.That(kResult, Is.EqualTo((double)scenario.ExpectedStochastic.k.Value).Within(kMargin), $"{scenario.Name}: %K mismatch");

                var dMargin = scenario.ExpectedStochastic.d.HasValue ? _marginFactor * Math.Abs(scenario.ExpectedStochastic.d.Value) : 0;
                if (scenario.ExpectedStochastic.d.HasValue)
                {
                    var expectedD = scenario.ExpectedStochastic.d.Value;
                    var dDiff = Math.Abs(dResult.GetValueOrDefault() - expectedD);
                    var dPercentageDiff = expectedD != 0 ? (dDiff / Math.Abs(scenario.ExpectedStochastic.d.Value)) * 100 : dDiff;
                    Console.WriteLine($"{scenario.Name}: %D Percentage Difference: {(expectedD != 0 ? $"{dPercentageDiff:F6}%" : $"Absolute Diff: {dDiff:F6}")}");
                }
                Assert.That(dResult, Is.EqualTo(scenario.ExpectedStochastic.d.HasValue ? (double)scenario.ExpectedStochastic.d.Value : null).Within(dMargin).Or.Null, $"{scenario.Name}: %D mismatch");
            }
        }

        [Test]
        public void TestOBVCalculation()
        {
            foreach (var scenario in TradingMetricScenarios.GetOBVScenarios())
            {
                // Arrange
                var candlesticks = scenario.Candlesticks; // Use full 10 candlesticks from source

                // Act
                var result = _tradingCalculator.CalculateOBV(candlesticks);

                // Assert
                Assert.That(result, Is.EqualTo(scenario.ExpectedOBV), $"{scenario.Name}: OBV mismatch. Expected: {scenario.ExpectedOBV}, Actual: {result}");
            }
        }
    }
}