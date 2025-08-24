using SmokehouseDTOs;

namespace TradingSimulator.Tests
{
    public class TradingMetricScenarios
    {
        public class TestScenario
        {
            public string Name { get; set; }
            public List<PseudoCandlestick> Candlesticks { get; set; }
            public double? ExpectedRSI { get; set; }
            public (double? macdLine, double? signalLine, double? histogram) ExpectedMACD { get; set; }
            public double? ExpectedEMA { get; set; }
            public (double? middle, double? upper, double? lower) ExpectedBollingerBands { get; set; }
            public double? ExpectedATR { get; set; }
            public double? ExpectedVWAP { get; set; }
            public (double? k, double? d) ExpectedStochastic { get; set; }
            public long? ExpectedOBV { get; set; }
        }

        public static List<TestScenario> GetRSIScenarios()
        {
            var scenarios = new List<TestScenario>();
            var baseTimestamp = DateTime.Parse("2025-04-22 16:00:00");

            var rsiCandlesticks = Enumerable.Range(0, 14).Select(i => new PseudoCandlestick
            {
                Timestamp = baseTimestamp.AddHours(i),
                MidClose = 100.0 + (i % 2 == 0 ? 2.0 : -1.0),
                MidHigh = 100.5 + (i % 2 == 0 ? 2.0 : -1.0),
                MidLow = 99.5 + (i % 2 == 0 ? 2.0 : -1.0),
                Volume = 1000,
                IsFromCandlestick = true
            }).ToList();
            scenarios.Add(new TestScenario
            {
                Name = "RSI_SeekingAlpha",
                Candlesticks = rsiCandlesticks,
                ExpectedRSI = 46.15
            });

            var rsiAaplPrices = new double[] { 441.36, 439.88, 439.66, 439.19, 440.36, 441.40, 442.66, 443.82, 446.45, 447.88, 449.39, 451.00, 452.73, 454.56 };
            var rsiAaplCandlesticks = rsiAaplPrices.Select((price, i) => new PseudoCandlestick
            {
                Timestamp = baseTimestamp.AddHours(i),
                MidClose = price,
                MidHigh = price + 1.0,
                MidLow = price - 1.0,
                Volume = 1000 + i * 100,
                IsFromCandlestick = true
            }).ToList();
            scenarios.Add(new TestScenario
            {
                Name = "RSI_InvestExcel",
                Candlesticks = rsiAaplCandlesticks,
                ExpectedRSI = 87.62
            });

            return scenarios;
        }

        public static List<TestScenario> GetMACDScenarios()
        {
            var scenarios = new List<TestScenario>();
            var baseTimestamp = DateTime.Parse("2025-04-22 16:00:00");

            var macdPrices = new double[] { 459.99, 448.85, 444.57, 448.97, 453.28, 452.08, 451.91, 450.81, 450.30, 448.97, 447.58, 446.06, 445.52, 445.15, 441.40, 442.80, 443.66, 441.36, 439.88, 439.66, 439.19, 440.36, 441.40, 442.66, 443.82, 445.11, 446.45, 447.88, 449.39, 451.00, 452.73, 454.56, 456.50, 458.27 };
            var macdCandlesticks = macdPrices.Select((price, i) => new PseudoCandlestick
            {
                Timestamp = baseTimestamp.AddHours(i),
                MidClose = price,
                MidHigh = price + 1.0,
                MidLow = price - 1.0,
                Volume = 1000 + i * 100,
                IsFromCandlestick = true
            }).ToList();
            scenarios.Add(new TestScenario
            {
                Name = "MACD_InvestExcel",
                Candlesticks = macdCandlesticks,
                ExpectedMACD = (1.718136, -0.250365, 1.968501)
            });

            return scenarios;
        }

        public static List<TestScenario> GetEMAScenarios()
        {
            var scenarios = new List<TestScenario>();
            var baseTimestamp = DateTime.Parse("2025-04-22 16:00:00");

            var intelPrices = new double[] { 22.81, 22.87, 23.03, 23.17, 23.21 };
            var emaCandlesticks = Enumerable.Range(0, 5).Select(i => new PseudoCandlestick
            {
                Timestamp = baseTimestamp.AddHours(i),
                MidClose = intelPrices[i],
                MidHigh = intelPrices[i] + 0.5,
                MidLow = intelPrices[i] - 0.5,
                Volume = 1000 + i * 100,
                IsFromCandlestick = true
            }).ToList();
            scenarios.Add(new TestScenario
            {
                Name = "EMA_Dummies",
                Candlesticks = emaCandlesticks,
                ExpectedEMA = 23.018
            });

            var emaAaplPrices = new double[] { 451.00, 452.73, 454.56, 456.50, 458.27 };
            var emaAaplCandlesticks = emaAaplPrices.Select((price, i) => new PseudoCandlestick
            {
                Timestamp = baseTimestamp.AddHours(i),
                MidClose = price,
                MidHigh = price + 1.0,
                MidLow = price - 1.0,
                Volume = 1000 + i * 100,
                IsFromCandlestick = true
            }).ToList();
            scenarios.Add(new TestScenario
            {
                Name = "EMA_InvestExcel",
                Candlesticks = emaAaplCandlesticks,
                ExpectedEMA = 454.61
            });

            return scenarios;
        }

        public static List<TestScenario> GetBollingerBandsScenarios()
        {
            var scenarios = new List<TestScenario>();
            var baseTimestamp = DateTime.Parse("2025-04-22 16:00:00");

            var bollingerPrices = new double[] { 10, 11, 12, 11, 14, 13, 12, 11, 10, 12, 13, 14, 12, 11 };
            var bollingerCandlesticks = bollingerPrices.Select((price, i) => new PseudoCandlestick
            {
                Timestamp = baseTimestamp.AddHours(i),
                MidClose = price,
                MidHigh = price + 1,
                MidLow = price - 1,
                Volume = 1000,
                IsFromCandlestick = true
            }).ToList();
            scenarios.Add(new TestScenario
            {
                Name = "Bollinger_Investopedia",
                Candlesticks = bollingerCandlesticks,
                ExpectedBollingerBands = (11.86, 14.35, 9.37)
            });

            var bollingerAaplPrices = new double[] { 441.36, 439.88, 439.66, 439.19, 440.36, 441.40, 442.66, 443.82, 446.45, 447.88, 449.39, 451.00, 452.73, 454.56 };
            var bollingerAaplCandlesticks = bollingerAaplPrices.Select((price, i) => new PseudoCandlestick
            {
                Timestamp = baseTimestamp.AddHours(i),
                MidClose = price,
                MidHigh = price + 1.0,
                MidLow = price - 1.0,
                Volume = 1000 + i * 100,
                IsFromCandlestick = true
            }).ToList();
            scenarios.Add(new TestScenario
            {
                Name = "Bollinger_InvestExcel",
                Candlesticks = bollingerAaplCandlesticks,
                ExpectedBollingerBands = (445.0, 455.0, 435.0) // Approximate, based on SMA and volatility
            });

            return scenarios;
        }

        public static List<TestScenario> GetATRScenarios()
        {
            var scenarios = new List<TestScenario>();
            var baseTimestamp = DateTime.Parse("2025-04-22 16:00:00");

            var atrData = new (double high, double low, double close)[]
            {
                (51, 49, 50), (52, 50, 51), (53, 51, 52), (53, 51, 52),
                (52, 50, 51), (51, 49, 50), (52, 50, 51), (53, 51, 52),
                (54, 52, 53), (54, 52, 53), (53, 51, 52), (52, 50, 51),
                (53, 51, 52), (53, 51, 52)
            };
            var atrCandlesticks = atrData.Select((data, i) => new PseudoCandlestick
            {
                Timestamp = baseTimestamp.AddHours(i),
                MidClose = data.close,
                MidHigh = data.high,
                MidLow = data.low,
                Volume = 1000,
                IsFromCandlestick = true
            }).ToList();
            scenarios.Add(new TestScenario
            {
                Name = "ATR_Investopedia",
                Candlesticks = atrCandlesticks,
                ExpectedATR = 2
            });

            var atrAaplPrices = new (double high, double low, double close)[]
            {
                (442.36, 440.36, 441.36), (440.88, 438.88, 439.88), (440.66, 438.66, 439.66),
                (440.19, 438.19, 439.19), (441.36, 439.36, 440.36), (442.40, 440.40, 441.40),
                (443.66, 441.66, 442.66), (444.82, 442.82, 443.82), (447.45, 445.45, 446.45),
                (448.88, 446.88, 447.88), (450.39, 448.39, 449.39), (452.00, 450.00, 451.00),
                (453.73, 451.73, 452.73), (455.56, 453.56, 454.56)
            };
            var atrAaplCandlesticks = atrAaplPrices.Select((data, i) => new PseudoCandlestick
            {
                Timestamp = baseTimestamp.AddHours(i),
                MidClose = data.close,
                MidHigh = data.high,
                MidLow = data.low,
                Volume = 1000 + i * 100,
                IsFromCandlestick = true
            }).ToList();
            scenarios.Add(new TestScenario
            {
                Name = "ATR_InvestExcel",
                Candlesticks = atrAaplCandlesticks,
                ExpectedATR = 2.45
            });

            return scenarios;
        }

        public static List<TestScenario> GetVWAPScenarios()
        {
            var scenarios = new List<TestScenario>();
            var baseTimestamp = DateTime.Parse("2025-04-22 16:00:00");

            var vwapData = new (double high, double low, double close, int volume)[]
            {
                (150.39, 150.22, 150.31, 380),
                (150.47, 150.38, 150.41, 5270),
                (150.41, 150.31, 150.36, 1000),
                (150.41, 150.31, 150.36, 1000),
                (150.41, 150.31, 150.36, 1000),
                (150.41, 150.31, 150.36, 1000),
                (150.41, 150.31, 150.36, 1000),
                (150.41, 150.31, 150.36, 1000),
                (150.41, 150.31, 150.36, 1000),
                (150.41, 150.31, 150.36, 1000),
                (150.41, 150.31, 150.36, 1000),
                (150.41, 150.31, 150.36, 1000),
                (150.41, 150.31, 450.36, 1000),
                (150.41, 150.31, 150.36, 1000),
                (150.41, 150.31, 150.36, 1000),
                (150.41, 150.31, 150.36, 1000),
                (150.41, 150.31, 150.36, 1000)
            };
            var vwapCandlesticks = vwapData.Select((data, i) => new PseudoCandlestick
            {
                Timestamp = baseTimestamp.AddMinutes(i),
                MidClose = data.close,
                MidHigh = data.high,
                MidLow = data.low,
                Volume = data.volume,
                IsFromCandlestick = true
            }).ToList();
            scenarios.Add(new TestScenario
            {
                Name = "VWAP_Investing",
                Candlesticks = vwapCandlesticks,
                ExpectedVWAP = 155.22
            });

            var vwapAaplPrices = new (double high, double low, double close, int volume)[]
            {
                (442.36, 440.36, 441.36, 1000), (440.88, 438.88, 439.88, 1100),
                (440.66, 438.66, 439.66, 1200), (440.19, 438.19, 439.19, 1300),
                (441.36, 439.36, 440.36, 1400), (442.40, 440.40, 441.40, 1500),
                (443.66, 441.66, 442.66, 1600), (444.82, 442.82, 443.82, 1700),
                (447.45, 445.45, 446.45, 1800), (448.88, 446.88, 447.88, 1900),
                (450.39, 448.39, 449.39, 2000), (452.00, 450.00, 451.00, 2100),
                (453.73, 451.73, 452.73, 2200), (455.56, 453.56, 454.56, 2300),
                (457.50, 455.50, 456.50, 2400), (459.27, 457.27, 458.27, 2500),
                (458.27, 456.27, 458.27, 2600)
            };
            var vwapAaplCandlesticks = vwapAaplPrices.Select((data, i) => new PseudoCandlestick
            {
                Timestamp = baseTimestamp.AddHours(i),
                MidClose = data.close,
                MidHigh = data.high,
                MidLow = data.low,
                Volume = data.volume,
                IsFromCandlestick = true
            }).ToList();
            scenarios.Add(new TestScenario
            {
                Name = "VWAP_InvestExcel",
                Candlesticks = vwapAaplCandlesticks,
                ExpectedVWAP = 449.0
            });

            var vwapStableData = new (double high, double low, double close, int volume)[]
            {
                (100.0, 99.0, 99.5, 500),
                (101.0, 99.5, 100.0, 1000),
                (100.5, 99.5, 100.0, 1000),
                (100.5, 99.5, 100.0, 1000),
                (100.5, 99.5, 100.0, 1000)
            };

            var vwapStableCandlesticks = vwapStableData.Select((data, i) => new PseudoCandlestick
            {
                Timestamp = baseTimestamp.AddMinutes(i),
                MidClose = data.close,
                MidHigh = data.high,
                MidLow = data.low,
                Volume = data.volume,
                IsFromCandlestick = true
            }).ToList();

            scenarios.Add(new TestScenario
            {
                Name = "VWAP_Stable",
                Candlesticks = vwapStableCandlesticks,
                ExpectedVWAP = 99.94
            });


            return scenarios;
        }

        public static List<TestScenario> GetStochasticScenarios()
        {
            var scenarios = new List<TestScenario>();
            var baseTimestamp = DateTime.Parse("2025-04-22 16:00:00");

            var stochasticCandlesticks = Enumerable.Repeat(new PseudoCandlestick
            {
                Timestamp = baseTimestamp,
                MidClose = 145,
                MidHigh = 150,
                MidLow = 125,
                Volume = 1000,
                IsFromCandlestick = true
            }, 17).Select((c, i) => { c.Timestamp = baseTimestamp.AddHours(i); return c; }).ToList();
            scenarios.Add(new TestScenario
            {
                Name = "Stochastic_Investopedia",
                Candlesticks = stochasticCandlesticks,
                ExpectedStochastic = (80.0, 80.0)
            });


            baseTimestamp = DateTime.Parse("2025-04-22 16:00:00");

            var stochasticVaryingCandlesticks = new (double high, double low, double close)[]
            {
            (10.5, 9.5, 10.0),
            (10.8, 9.8, 10.6),
            (11.0, 10.0, 10.8),
            (11.2, 10.2, 11.1),
            (11.4, 10.4, 11.3),
            (11.5, 10.5, 11.0),
            (11.3, 10.3, 10.9),
            (11.2, 10.2, 10.5),
            (11.0, 10.0, 10.2),
            (10.8, 9.8, 10.0),
            (10.7, 9.7, 9.9),
            (10.5, 9.5, 9.7),
            (10.3, 9.3, 9.5),
            (10.2, 9.2, 9.4),
            (10.0, 9.0, 9.2),
            (10.3, 9.3, 9.8),
            (10.5, 9.5, 10.2)
            }.Select((data, i) => new PseudoCandlestick
            {
                Timestamp = baseTimestamp.AddHours(i),
                MidClose = data.close,
                MidHigh = data.high,
                MidLow = data.low,
                Volume = 1000,
                IsFromCandlestick = true
            }).ToList();

            scenarios.Add(new TestScenario
            {
                Name = "Stochastic_Varying",
                Candlesticks = stochasticVaryingCandlesticks,
                ExpectedStochastic = (48.0, 29.33)
            });
            var stochasticRealisticCandlesticks = new (double high, double low, double close)[]
                {
                    (50.5, 49.5, 50.0), (50.8, 49.8, 50.6), (51.0, 50.0, 50.8), (51.2, 50.2, 51.1), (51.4, 50.4, 51.3),
                    (51.5, 50.5, 51.0), (51.3, 50.3, 50.9), (51.2, 50.2, 50.5), (51.0, 50.0, 50.2), (50.8, 49.8, 50.0),
                    (50.7, 49.7, 49.9), (50.5, 49.5, 49.7), (50.3, 49.3, 49.5), (50.2, 49.2, 49.4), (50.0, 49.0, 49.2),
                    (50.3, 49.3, 49.8), (50.5, 49.5, 50.2), (50.7, 49.7, 50.6), (51.0, 50.0, 50.9), (51.2, 50.2, 51.0),
                    (51.5, 50.5, 51.3), (51.7, 50.7, 51.5), (51.8, 50.8, 51.7), (52.0, 51.0, 52.0), (52.3, 51.3, 52.2),
                    (52.5, 51.5, 52.4), (52.7, 51.7, 52.6), (52.8, 51.8, 52.5), (52.6, 51.6, 52.3), (52.5, 51.5, 52.0),
                    (52.3, 51.3, 51.8), (52.2, 51.2, 51.7), (52.0, 51.0, 51.5), (51.8, 50.8, 51.2), (51.7, 50.7, 51.0),
                    (51.5, 50.5, 50.8), (51.2, 50.2, 50.6), (51.0, 50.0, 50.3), (50.8, 49.8, 50.0), (50.5, 49.5, 49.8),
                    (50.3, 49.3, 49.5), (50.0, 49.0, 49.3), (49.8, 48.8, 49.0), (49.6, 48.6, 48.8), (49.5, 48.5, 48.5),
                    (49.3, 48.3, 48.3), (49.0, 48.0, 48.0), (48.8, 47.8, 47.8), (48.5, 47.5, 47.5), (48.3, 47.3, 47.3)
                }.Select((data, i) => new PseudoCandlestick
                {
                    Timestamp = baseTimestamp.AddHours(i),
                    MidClose = data.close,
                    MidHigh = data.high,
                    MidLow = data.low,
                    Volume = 1000,
                    IsFromCandlestick = true
                }).ToList();

            scenarios.Add(new TestScenario
            {
                Name = "Stochastic_Realistic",
                Candlesticks = stochasticRealisticCandlesticks,
                ExpectedStochastic = (0, 0)
            });
            var stochasticMiddleCandlesticks = new (double high, double low, double close)[]
                {
                    (50.5, 49.5, 50.0), (51.0, 50.0, 50.5), (51.5, 50.5, 51.0), (52.0, 51.0, 51.5), (52.5, 51.5, 52.0),
                    (52.0, 51.0, 51.5), (51.5, 50.5, 51.0), (51.0, 50.0, 50.5), (50.5, 49.5, 50.0), (50.0, 49.0, 49.5),
                    (50.5, 49.5, 50.0), (51.0, 50.0, 50.5), (51.5, 50.5, 51.0), (52.0, 51.0, 51.5), (52.5, 51.5, 52.0),
                    (52.0, 51.0, 51.5), (51.5, 50.5, 51.0), (51.0, 50.0, 50.5), (50.5, 49.5, 50.0), (50.0, 49.0, 49.5),
                    (49.5, 48.5, 49.0), (49.0, 48.0, 48.5), (48.5, 47.5, 48.0), (48.0, 47.0, 47.5), (48.5, 47.5, 48.0),
                    (49.0, 48.0, 48.5), (49.5, 48.5, 49.0), (50.0, 49.0, 49.5), (50.5, 49.5, 50.0), (51.0, 50.0, 50.5),
                    (51.5, 50.5, 51.0), (52.0, 51.0, 51.5), (52.5, 51.5, 52.0), (52.0, 51.0, 51.5), (51.5, 50.5, 51.0),
                    (51.0, 50.0, 50.5), (50.5, 49.5, 50.0), (50.0, 49.0, 49.5), (49.5, 48.5, 49.0), (49.0, 48.0, 48.5),
                    (48.5, 47.5, 48.0), (48.0, 47.0, 47.5), (47.5, 46.5, 47.0), (47.0, 46.0, 46.5), (47.5, 46.5, 47.0),
                    (48.0, 47.0, 47.5), (48.5, 47.5, 48.0), (49.0, 48.0, 48.5), (49.5, 48.5, 49.0), (50.0, 49.0, 49.5)
                }.Select((data, i) => new PseudoCandlestick
                {
                    Timestamp = baseTimestamp.AddHours(i),
                    MidClose = data.close,
                    MidHigh = data.high,
                    MidLow = data.low,
                    Volume = 1000,
                    IsFromCandlestick = true
                }).ToList();

            scenarios.Add(new TestScenario
            {
                Name = "Stochastic_Middle",
                Candlesticks = stochasticMiddleCandlesticks,
                ExpectedStochastic = (77.78, 61.08)
            });


            return scenarios;
        }

        public static List<TestScenario> GetOBVScenarios()
        {
            var scenarios = new List<TestScenario>();
            var baseTimestamp = DateTime.Parse("2025-04-22 16:00:00");

            var obvData = new (double close, int volume)[]
            {
                (100, 10000),
                (105, 14000),
                (110, 12000),
                (102, 10000),
                (99, 6000),
                (120, 27000),
                (117, 17000),
                (114, 14000),
                (110, 10000),
                (102, 8000)
            };
            var obvCandlesticks = obvData.Select((data, i) => new PseudoCandlestick
            {
                Timestamp = baseTimestamp.AddHours(i),
                MidClose = data.close,
                MidHigh = data.close + 1,
                MidLow = data.close - 1,
                Volume = data.volume,
                IsFromCandlestick = true
            }).ToList();
            scenarios.Add(new TestScenario
            {
                Name = "OBV_IG",
                Candlesticks = obvCandlesticks,
                ExpectedOBV = -12000
            });

            var obvAaplData = new (double close, int volume)[]
            {
                (446.45, 1800), (447.88, 1900), (449.39, 2000), (451.00, 2100),
                (452.73, 2200), (454.56, 2300), (456.50, 2400), (458.27, 2500),
                (458.27, 2600), (458.27, 2700)
            };
            var obvAaplCandlesticks = obvAaplData.Select((data, i) => new PseudoCandlestick
            {
                Timestamp = baseTimestamp.AddHours(i),
                MidClose = data.close,
                MidHigh = data.close + 1.0,
                MidLow = data.close - 1.0,
                Volume = data.volume,
                IsFromCandlestick = true
            }).ToList();
            scenarios.Add(new TestScenario
            {
                Name = "OBV_InvestExcel",
                Candlesticks = obvAaplCandlesticks,
                ExpectedOBV = 15400
            });

            return scenarios;
        }

    }
}