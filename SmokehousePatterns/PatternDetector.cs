
using Newtonsoft.Json;
using ParquetSharp;
using SmokehousePatterns.Helpers;
using SmokehousePatterns.PatternDefinitions;

namespace SmokehousePatterns
{
    public class PatternDetector
    {
        // Detects patterns and exports them to JSON, skipping existing files
        //public async Task DetectAndExportPatterns(string parquetFilePath, string outputDirectory, int lookback, int lookforward, int intervalType)
        //{
        //    string marketName = Path.GetFileNameWithoutExtension(parquetFilePath).Replace("_MarketStates", "");
        //    string patternsFilePath = Path.Combine(outputDirectory, $"{marketName}_patterns.json");

        //    if (File.Exists(patternsFilePath)) return;

        //    var marketStates = await ReadParquetFile(parquetFilePath);
        //    if (marketStates == null || marketStates.Count < 2) return;

        //    int trendLookback = GetTrendLookback(intervalType);
        //    var detailedPatterns = await PatternSearch.DetectPatterns(marketStates, trendLookback, lookforward);

        //    Directory.CreateDirectory(outputDirectory);
        //    using (var writer = new StreamWriter(patternsFilePath))
        //    using (var jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented })
        //    {
        //        jsonWriter.WriteStartArray();
        //        foreach (var patternEntry in detailedPatterns)
        //        {
        //            int index = patternEntry.Key;
        //            var current = marketStates[index];

        //            foreach (var pattern in patternEntry.Value)
        //            {
        //                int lookbackStart = Math.Max(0, index - (pattern.Candles.Count - 1) - lookback);
        //                int lookbackCount = Math.Min(lookback, index - (pattern.Candles.Count - 1) - lookbackStart);
        //                var lookbackStates = lookbackCount > 0 ? marketStates.GetRange(lookbackStart, lookbackCount) : new List<MarketState>();

        //                int forwardCount = Math.Min(lookforward, marketStates.Count - index - 1);
        //                var forwardStates = forwardCount > 0 ? marketStates.GetRange(index + 1, forwardCount) : new List<MarketState>();

        //                var occurrence = CollectPatternOccurrence(pattern, current, marketStates, index, lookbackStates, forwardStates, lookback, lookforward, intervalType);
        //                if (occurrence != null)
        //                    JsonSerializer.CreateDefault().Serialize(jsonWriter, occurrence);
        //            }
        //        }
        //        jsonWriter.WriteEndArray();
        //    }
        //}

        public async Task BuildPatternConfig(string patternsFilePath, string configOutputDir, int lookback, int lookforward, int intervalType)
        {
            if (!File.Exists(patternsFilePath))
            {
                Console.WriteLine($"Pattern file {patternsFilePath} does not exist. Skipping config build.");
                return;
            }

            if (intervalType < 1 || intervalType > 3)
            {
                throw new ArgumentException("Interval type must be 1 (minute), 2 (hour), or 3 (day).");
            }

            var patternMetrics = new Dictionary<string, PatternMetric>();
            string marketName = Path.GetFileNameWithoutExtension(patternsFilePath).Replace("_patterns", "");
            string intervalLabel = intervalType == 1 ? "minute" : intervalType == 2 ? "hour" : "day";
            string configPath = Path.Combine(configOutputDir, $"Config_{marketName}_{intervalType}{intervalLabel}_{DateTime.Now:yyyyMMdd_HHmmss}.json");

            using (var fileStream = new FileStream(patternsFilePath, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(fileStream))
            using (var jsonReader = new JsonTextReader(reader))
            {
                var serializer = JsonSerializer.CreateDefault();
                jsonReader.SupportMultipleContent = true;

                if (!jsonReader.Read() || jsonReader.TokenType != JsonToken.StartArray)
                {
                    throw new JsonException($"Expected JSON array in patterns file: {patternsFilePath}");
                }

                while (jsonReader.Read() && jsonReader.TokenType != JsonToken.EndArray)
                {
                    var occurrence = serializer.Deserialize<MarketPatternOccurrence>(jsonReader);
                    string pattern = occurrence.PatternName;

                    if (!patternMetrics.TryGetValue(pattern, out var metric))
                    {
                        metric = new PatternMetric(pattern);
                        patternMetrics[pattern] = metric;
                    }

                    UpdatePatternMetricsFromOccurrence(metric, occurrence, lookback, lookforward);
                }
            }

            if (patternMetrics.Count > 0)
            {
                await NormalizeAndSavePatternMetricsAsync(patternMetrics, configPath);
            }
            else
            {
                Console.WriteLine($"No patterns found in {patternsFilePath} for interval {intervalType} to build config.");
            }
        }

        private async Task NormalizeAndSavePatternMetricsAsync(Dictionary<string, PatternMetric> patternMetrics, string outputConfigPath)
        {
            double maxStrength = patternMetrics.Values.Max(m => m.Strengths.Any() ? Math.Abs(m.Strengths.Average()) : 0);
            double maxUncertainty = patternMetrics.Values.Max(m => m.Uncertainties.Any() ? m.Uncertainties.Average() : 0);

            var metrics = patternMetrics.ToDictionary(
                kvp => kvp.Key,
                kvp =>
                {
                    var strengths = kvp.Value.Strengths.Select(s => maxStrength > 0 ? s / maxStrength * 100 : 0).ToList();
                    var uncertainties = kvp.Value.Uncertainties.Select(u => maxUncertainty > 0 ? u / maxUncertainty * 100 : 0).ToList();

                    return new PatternMetricWithCount
                    {
                        Strength = strengths.Any() ? strengths.Average() : 0,
                        Certainty = kvp.Value.Certainties.Any() ? kvp.Value.Certainties.Average() : 0,
                        //Uncertainty = uncertainties.Any() ? uncertainties.Any() : 0,
                        OccurrenceCount = kvp.Value.Strengths.Count
                    };
                });

            Directory.CreateDirectory(Path.GetDirectoryName(outputConfigPath));
            await File.WriteAllTextAsync(outputConfigPath, JsonConvert.SerializeObject(metrics, Formatting.Indented));
        }

        private void UpdatePatternMetricsFromOccurrence(PatternMetric metric, MarketPatternOccurrence occurrence, int lookback, int lookforward)
        {
            var current = occurrence.Candles.Last();
            var lookbackCandles = occurrence.LookbackCandles.Take(lookback).ToList();
            var forwardCandles = occurrence.LookForwardCandles.Take(lookforward).ToList();
            bool isBullish = IsBullishPattern(metric.PatternName);

            double strength = 0;
            if (forwardCandles.Any())
            {

                double currentMid = current.MidClose();
                double lastMid = forwardCandles.Last().MidClose();
                strength = isBullish
                    ? (lastMid - currentMid) / (100 - currentMid) * 100
                    : (currentMid - lastMid) / currentMid * 100;
            }
            metric.Strengths.Add(strength);

            int confirmationCount = forwardCandles.Count(s => (isBullish && s.MidClose() > current.MidClose()) || (!isBullish && s.MidClose() < current.MidClose()));
            metric.Certainties.Add(forwardCandles.Any() ? (double)confirmationCount / Math.Max(1, forwardCandles.Count) * 100 : 0);
        }

        private MarketPatternOccurrence CollectPatternOccurrence(PatternDefinition pattern, MarketState current,
            List<MarketState> marketStates, int index, List<MarketState> lookbackStates, List<MarketState> forwardStates,
            int lookback, int lookforward, int intervalType)
        {
            if (marketStates == null || pattern == null || pattern.Candles == null)
            {
                Console.WriteLine($"Error: Null input in CollectPatternOccurrence for pattern '{pattern?.Name ?? "null"}'");
                return null;
            }

            string marketSuffix = "";
            if (intervalType == 2) marketSuffix += "_Hourly";
            if (intervalType == 3) marketSuffix += "_Daily";

            var patternCandles = pattern.Candles.Select(j => new CandleData
            {
                Timestamp = marketStates[j].Timestamp,
                AskOpen = marketStates[j].AskOpen,
                AskHigh = marketStates[j].AskHigh,
                AskLow = marketStates[j].AskLow,
                AskClose = marketStates[j].AskClose,
                BidOpen = marketStates[j].BidOpen,
                BidHigh = marketStates[j].BidHigh,
                BidLow = marketStates[j].BidLow,
                BidClose = marketStates[j].BidClose,
                Volume = marketStates[j].Volume
            }).ToList();

            var lookbackCandles = lookbackStates.Select(state => new CandleData
            {
                Timestamp = state.Timestamp,
                AskOpen = state.AskOpen,
                AskHigh = state.AskHigh,
                AskLow = state.AskLow,
                AskClose = state.AskClose,
                BidOpen = state.BidOpen,
                BidHigh = state.BidHigh,
                BidLow = state.BidLow,
                BidClose = state.BidClose,
                Volume = state.Volume
            }).ToList();

            var lookForwardCandles = forwardStates.Select(state => new CandleData
            {
                Timestamp = state.Timestamp,
                AskOpen = state.AskOpen,
                AskHigh = state.AskHigh,
                AskLow = state.AskLow,
                AskClose = state.AskClose,
                BidOpen = state.BidOpen,
                BidHigh = state.BidHigh,
                BidLow = state.BidLow,
                BidClose = state.BidClose,
                Volume = state.Volume
            }).ToList();

            return new MarketPatternOccurrence
            {
                PatternName = pattern.Name,
                Timestamp = current.Timestamp,
                Candles = patternCandles,
                LookbackCandles = lookbackCandles,
                LookForwardCandles = lookForwardCandles,
                Indices = new List<int>(pattern.Candles), // Defensive copy
                LookbackPeriod = lookback,
                LookForwardPeriod = lookforward,
                MarketName = current.MarketTicker
            };
        }

        private void UpdatePatternMetrics(PatternMetric metric, List<MarketState> lookbackStates, List<MarketState> forwardStates, MarketState current, int lookback, int lookforward)
        {
            bool isBullish = IsBullishPattern(metric.PatternName);


            double currentClose = current.MidClose();

            double confirmationPriceChange = forwardStates.Any() ? (forwardStates.Last().MidClose() - currentClose) / currentClose * 100 : 0;
            double signedStrength = isBullish ? confirmationPriceChange : -confirmationPriceChange;
            metric.Strengths.Add(forwardStates.Any() ? signedStrength : 0);

            int confirmationCount = forwardStates.Count(s => (isBullish && s.MidClose() > currentClose) || (!isBullish && s.MidClose() < currentClose));
            metric.Certainties.Add(forwardStates.Any() ? (double)confirmationCount / lookforward * 100 : 0);

            double avgRange = lookbackStates.Any() ? lookbackStates.Select(s => (double)(s.AskHigh - s.AskLow)).Average() : 0;
            metric.Uncertainties.Add(avgRange);
        }

        private bool IsBullishPattern(string pattern)
        {
            return pattern.Contains("Bullish") || pattern == "Hammer" || pattern == "InvertedHammer" ||
                   pattern == "PiercingPattern" || pattern == "Takuri" || pattern == "Unique3River" ||
                   pattern.Contains("Morning") || pattern.Contains("ThreeAdvancing");
        }

        public async Task<List<MarketState>> ReadParquetFile(string filePath)
        {
            try
            {
                var marketStates = new List<MarketState>();
                using var file = new ParquetFileReader(filePath);
                for (int rowGroup = 0; rowGroup < file.FileMetaData.NumRowGroups; ++rowGroup)
                {
                    using var rowGroupReader = file.RowGroup(rowGroup);
                    long groupNumRows = rowGroupReader.MetaData.NumRows;

                    var groupMarketTickers = rowGroupReader.Column(0).LogicalReader<string>().ReadAll((int)groupNumRows);
                    var groupTimestamps = rowGroupReader.Column(1).LogicalReader<DateTime>().ReadAll((int)groupNumRows);
                    var groupOpenInterests = rowGroupReader.Column(2).LogicalReader<long>().ReadAll((int)groupNumRows);
                    var groupAskOpens = rowGroupReader.Column(3).LogicalReader<long>().ReadAll((int)groupNumRows);
                    var groupAskHighs = rowGroupReader.Column(4).LogicalReader<long>().ReadAll((int)groupNumRows);
                    var groupAskLows = rowGroupReader.Column(5).LogicalReader<long>().ReadAll((int)groupNumRows);
                    var groupAskCloses = rowGroupReader.Column(6).LogicalReader<long>().ReadAll((int)groupNumRows);
                    var groupBidOpens = rowGroupReader.Column(7).LogicalReader<long>().ReadAll((int)groupNumRows);
                    var groupBidHighs = rowGroupReader.Column(8).LogicalReader<long>().ReadAll((int)groupNumRows);
                    var groupBidLows = rowGroupReader.Column(9).LogicalReader<long>().ReadAll((int)groupNumRows);
                    var groupBidCloses = rowGroupReader.Column(10).LogicalReader<long>().ReadAll((int)groupNumRows);
                    var groupVolumes = rowGroupReader.Column(11).LogicalReader<long>().ReadAll((int)groupNumRows);
                    var groupSpreads = rowGroupReader.Column(12).LogicalReader<long>().ReadAll((int)groupNumRows);
                    //var groupMidPrices = rowGroupReader.Column(13).LogicalReader<double>().ReadAll((int)groupNumRows);
                    var groupHours = rowGroupReader.Column(14).LogicalReader<int>().ReadAll((int)groupNumRows);
                    var groupMinutes = rowGroupReader.Column(15).LogicalReader<int>().ReadAll((int)groupNumRows);
                    var groupDays = rowGroupReader.Column(16).LogicalReader<int>().ReadAll((int)groupNumRows);
                    var groupPivotPoints = rowGroupReader.Column(17).LogicalReader<double>().ReadAll((int)groupNumRows);
                    var groupSupport1s = rowGroupReader.Column(18).LogicalReader<double>().ReadAll((int)groupNumRows);
                    var groupSupport2s = rowGroupReader.Column(19).LogicalReader<double>().ReadAll((int)groupNumRows);
                    var groupSupport3s = rowGroupReader.Column(20).LogicalReader<double>().ReadAll((int)groupNumRows);
                    var groupResistance1s = rowGroupReader.Column(21).LogicalReader<double>().ReadAll((int)groupNumRows);
                    var groupResistance2s = rowGroupReader.Column(22).LogicalReader<double>().ReadAll((int)groupNumRows);
                    var groupResistance3s = rowGroupReader.Column(23).LogicalReader<double>().ReadAll((int)groupNumRows);
                    var groupVolumeSupports = rowGroupReader.Column(24).LogicalReader<double?>().ReadAll((int)groupNumRows);
                    var groupVolumeResistances = rowGroupReader.Column(25).LogicalReader<double?>().ReadAll((int)groupNumRows);
                    var groupSMA5s = rowGroupReader.Column(26).LogicalReader<double?>().ReadAll((int)groupNumRows);
                    var groupSMA10s = rowGroupReader.Column(27).LogicalReader<double?>().ReadAll((int)groupNumRows);
                    var groupATRs = rowGroupReader.Column(28).LogicalReader<double?>().ReadAll((int)groupNumRows);
                    var groupRSIs = rowGroupReader.Column(29).LogicalReader<double?>().ReadAll((int)groupNumRows);
                    var groupMACDs = rowGroupReader.Column(30).LogicalReader<double?>().ReadAll((int)groupNumRows);
                    var groupMACDSignals = rowGroupReader.Column(31).LogicalReader<double?>().ReadAll((int)groupNumRows);

                    for (int i = 0; i < groupNumRows; i++)
                    {
                        marketStates.Add(new MarketState
                        {
                            MarketTicker = groupMarketTickers[i],
                            Timestamp = groupTimestamps[i],
                            OpenInterest = groupOpenInterests[i],
                            AskOpen = groupAskOpens[i],
                            AskHigh = groupAskHighs[i],
                            AskLow = groupAskLows[i],
                            AskClose = groupAskCloses[i],
                            BidOpen = groupBidOpens[i],
                            BidHigh = groupBidHighs[i],
                            BidLow = groupBidLows[i],
                            BidClose = groupBidCloses[i],
                            Volume = groupVolumes[i],
                            Spread = groupSpreads[i],
                            Hour = groupHours[i],
                            Minute = groupMinutes[i],
                            Day = groupDays[i],
                            PivotPoint = groupPivotPoints[i],
                            Support1 = groupSupport1s[i],
                            Support2 = groupSupport2s[i],
                            Support3 = groupSupport3s[i],
                            Resistance1 = groupResistance1s[i],
                            Resistance2 = groupResistance2s[i],
                            Resistance3 = groupResistance3s[i],
                            VolumeSupport = groupVolumeSupports[i],
                            VolumeResistance = groupVolumeResistances[i],
                            SMA5 = groupSMA5s[i],
                            SMA10 = groupSMA10s[i],
                            ATR = groupATRs[i],
                            RSI = groupRSIs[i],
                            MACD = groupMACDs[i],
                            MACDSignal = groupMACDSignals[i]
                        });
                    }
                }
                return marketStates;
            }
            catch (Exception ex)
            {
                // Log the exception (optional but recommended)
                Console.WriteLine($"Error reading parquet file: {ex.Message}");

                // Delete the file if it exists
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        Console.WriteLine($"Deleted file: {filePath}");
                    }
                }
                catch (Exception deleteEx)
                {
                    Console.WriteLine($"Error deleting file: {deleteEx.Message}");
                }
            }
            return null;
        }

        //Returns the lookback period for trends, rather than xml
        private static int GetTrendLookback(int intervalType)
        {


            switch (intervalType)
            {
                case 1: return ConfigService.GetMinutePatternSettings().Lookback;
                case 2: return ConfigService.GetHourPatternSettings().Lookback;
                case 3: return ConfigService.GetDayPatternSettings().Lookback;
                default: throw new ArgumentException("Interval type must be 1 (minute), 2 (hour), or 3 (day).");
            }
        }


        private static int GetPatternCandleCount(string pattern)
        {
            switch (pattern)
            {
                // 1-Candle Patterns
                case "Doji":
                case "DragonflyDoji":
                case "GravestoneDoji":
                case "LongLeggedDoji":
                case "RickshawMan":
                case "Hammer":
                case "HangingMan":
                case "InvertedHammer":
                case "ShootingStar":
                case "Takuri":
                case "Unique3River":
                case "SpinningTop":
                case "HighWaveCandle":
                    return 1;

                // 2-Candle Patterns (Fixed)
                case "Engulfing_Bullish":
                case "Engulfing_Bearish":
                case "ClosingMarubozu_Bullish":
                case "ClosingMarubozu_Bearish":
                case "Counterattack_Bullish":
                case "Counterattack_Bearish":
                case "DarkCloudCover":
                case "DojiStar":
                case "Harami_Bullish":
                case "Harami_Bearish":
                case "HomingPigeon":
                case "InNeck":
                case "Kicking_Bullish":
                case "Kicking_Bearish":
                case "KickingByLength_Bullish":
                case "KickingByLength_Bearish":
                case "LongLineCandle_Bullish":
                case "LongLineCandle_Bearish":
                case "Marubozu_Bullish":
                case "Marubozu_Bearish":
                case "OnNeck":
                case "Piercing":
                case "SeparatingLines_Bullish":
                case "SeparatingLines_Bearish":
                case "Thrusting":
                case "TwoCrows":
                case "UpDownGapSideBySideWhiteLines":
                case "BeltHold_Bullish_Reversal":
                case "BeltHold_Bullish_Continuation":
                case "BeltHold_Bearish_Reversal":
                case "BeltHold_Bearish_Continuation":
                    return 2;

                // 3-Candle Patterns
                case "IdenticalThreeCrows":
                case "MorningDojiStar":
                case "DownsideGapThreeMethods":
                case "EveningDojiStar":
                case "EveningStar":
                case "Hikkake_Bullish":
                case "Hikkake_Bearish":
                case "StickSandwich_Bullish":
                case "StickSandwich_Bearish":
                case "TasukiGap_Bullish":
                case "TasukiGap_Bearish":
                case "Tristar_Bullish":
                case "Tristar_Bearish":
                case "ThreeInsideUp":
                case "ThreeInsideDown":
                case "ThreeOutsideUp":
                case "ThreeOutsideDown":
                case "ShortLineCandle_Bullish":
                case "ShortLineCandle_Bearish":
                case "StalledPattern":
                case "ThreeAdvancingWhiteSoldiers":
                case "ThreeBlackCrows":
                case "ThreeStarsInTheSouth":
                case "UpsideGapTwoCrows":
                    return 3;

                // 4-Candle Patterns
                case "ConcealingBabySwallow":
                case "ThreeLineStrike_Bullish":
                case "ThreeLineStrike_Bearish":
                    return 4;

                // 5-Candle Patterns
                case "Breakaway_Bullish":
                case "Breakaway_Bearish":
                case "LadderBottom":
                case "MatHold_Bullish":
                case "MatHold_Bearish":
                case "RisingFallingThreeMethods":
                    return 5;

                // Variable-Length Pattern
                case "AbandonedBaby_Bullish":
                case "AbandonedBaby_Bearish":
                    throw new InvalidOperationException($"Pattern '{pattern}' has a variable candle count determined by FindAbandonedBaby at runtime.");

                default:
                    throw new NotImplementedException($"Pattern '{pattern}' is not implemented in GetPatternCandleCount.");
            }
        }
    }
}