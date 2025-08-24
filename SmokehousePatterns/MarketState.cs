using ParquetSharp;

namespace SmokehousePatterns
{
    public class MarketState
    {
        public string MarketTicker { get; set; }
        public DateTime Timestamp { get; set; }
        public long OpenInterest { get; set; }
        public long AskOpen { get; set; }
        public long AskHigh { get; set; }
        public long AskLow { get; set; }
        public long AskClose { get; set; }
        public long BidOpen { get; set; }
        public long BidHigh { get; set; }
        public long BidLow { get; set; }
        public long BidClose { get; set; }
        public long Volume { get; set; }
        public long Spread { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public int Day { get; set; }
        public double PivotPoint { get; set; }
        public double Support1 { get; set; }
        public double Support2 { get; set; }
        public double Support3 { get; set; }
        public double Resistance1 { get; set; }
        public double Resistance2 { get; set; }
        public double Resistance3 { get; set; }
        public double? VolumeSupport { get; set; }
        public double? VolumeResistance { get; set; }
        public double? SMA5 { get; set; }
        public double? SMA10 { get; set; }
        public double? ATR { get; set; }
        public double? RSI { get; set; }
        public double? MACD { get; set; }
        public double? MACDSignal { get; set; }


        /// <summary>
        /// Calculates the mid price using Open prices
        /// </summary>
        public double MidOpen()
        {
            return (AskOpen + BidOpen) / 2.0;
        }

        /// <summary>
        /// Calculates the mid price using High prices
        /// </summary>
        public double MidHigh()
        {
            return (AskHigh + BidHigh) / 2.0;
        }

        /// <summary>
        /// Calculates the mid price using Low prices
        /// </summary>
        public double MidLow()
        {
            return (AskLow + BidLow) / 2.0;
        }

        /// <summary>
        /// Calculates the mid price using Close prices
        /// </summary>
        public double MidClose()
        {
            return (AskClose + BidClose) / 2.0;
        }

        public static void SaveToParquet(List<MarketState> marketStates, string filePath)
        {
            if (!marketStates.Any()) return;

            const int maxAttempts = 3;
            const long minFileSize = 8 * 1024; // 8 KB in bytes
            int attempt = 0;
            bool success = false;

            while (attempt < maxAttempts && !success)
            {
                attempt++;

                // Define the columns with their logical types, matching MarketState properties
                var columns = new Column[]
                {
            new Column<string>("market_ticker"),
            new Column<DateTime>("timestamp"),
            new Column<long>("open_interest"),
            new Column<long>("ask_open"),
            new Column<long>("ask_high"),
            new Column<long>("ask_low"),
            new Column<long>("ask_close"),
            new Column<long>("bid_open"),
            new Column<long>("bid_high"),
            new Column<long>("bid_low"),
            new Column<long>("bid_close"),
            new Column<long>("volume"),
            new Column<long>("spread"),
            new Column<double>("mid_price"),
            new Column<int>("hour"),
            new Column<int>("minute"),
            new Column<int>("day"),
            new Column<double>("pivot_point"),
            new Column<double>("support1"),
            new Column<double>("support2"),
            new Column<double>("support3"),
            new Column<double>("resistance1"),
            new Column<double>("resistance2"),
            new Column<double>("resistance3"),
            new Column<double?>("volume_support"),
            new Column<double?>("volume_resistance"),
            new Column<double?>("SMA_5"),
            new Column<double?>("SMA_10"),
            new Column<double?>("ATR"),
            new Column<double?>("RSI"),
            new Column<double?>("MACD"),
            new Column<double?>("MACD_signal")
                };

                try
                {
                    // Create and write to the Parquet file using ParquetFileWriter
                    using (var file = new ParquetFileWriter(filePath, columns))
                    {
                        using (var rowGroup = file.AppendRowGroup())
                        {
                            using (var marketTickerWriter = rowGroup.NextColumn().LogicalWriter<string>())
                            {
                                marketTickerWriter.WriteBatch(marketStates.Select(s => s.MarketTicker).ToArray());
                            }
                            using (var timestampWriter = rowGroup.NextColumn().LogicalWriter<DateTime>())
                            {
                                timestampWriter.WriteBatch(marketStates.Select(s => s.Timestamp).ToArray());
                            }
                            using (var openInterestWriter = rowGroup.NextColumn().LogicalWriter<long>())
                            {
                                openInterestWriter.WriteBatch(marketStates.Select(s => s.OpenInterest).ToArray());
                            }
                            using (var askOpenWriter = rowGroup.NextColumn().LogicalWriter<long>())
                            {
                                askOpenWriter.WriteBatch(marketStates.Select(s => s.AskOpen).ToArray());
                            }
                            using (var askHighWriter = rowGroup.NextColumn().LogicalWriter<long>())
                            {
                                askHighWriter.WriteBatch(marketStates.Select(s => s.AskHigh).ToArray());
                            }
                            using (var askLowWriter = rowGroup.NextColumn().LogicalWriter<long>())
                            {
                                askLowWriter.WriteBatch(marketStates.Select(s => s.AskLow).ToArray());
                            }
                            using (var askCloseWriter = rowGroup.NextColumn().LogicalWriter<long>())
                            {
                                askCloseWriter.WriteBatch(marketStates.Select(s => s.AskClose).ToArray());
                            }
                            using (var bidOpenWriter = rowGroup.NextColumn().LogicalWriter<long>())
                            {
                                bidOpenWriter.WriteBatch(marketStates.Select(s => s.BidOpen).ToArray());
                            }
                            using (var bidHighWriter = rowGroup.NextColumn().LogicalWriter<long>())
                            {
                                bidHighWriter.WriteBatch(marketStates.Select(s => s.BidHigh).ToArray());
                            }
                            using (var bidLowWriter = rowGroup.NextColumn().LogicalWriter<long>())
                            {
                                bidLowWriter.WriteBatch(marketStates.Select(s => s.BidLow).ToArray());
                            }
                            using (var bidCloseWriter = rowGroup.NextColumn().LogicalWriter<long>())
                            {
                                bidCloseWriter.WriteBatch(marketStates.Select(s => s.BidClose).ToArray());
                            }
                            using (var volumeWriter = rowGroup.NextColumn().LogicalWriter<long>())
                            {
                                volumeWriter.WriteBatch(marketStates.Select(s => s.Volume).ToArray());
                            }
                            using (var spreadWriter = rowGroup.NextColumn().LogicalWriter<long>())
                            {
                                spreadWriter.WriteBatch(marketStates.Select(s => s.Spread).ToArray());
                            }
                            using (var midtWriter = rowGroup.NextColumn().LogicalWriter<double>())
                            {
                                midtWriter.WriteBatch(marketStates.Select(s => s.MidClose()).ToArray());
                            }
                            using (var hourWriter = rowGroup.NextColumn().LogicalWriter<int>())
                            {
                                hourWriter.WriteBatch(marketStates.Select(s => s.Hour).ToArray());
                            }
                            using (var minuteWriter = rowGroup.NextColumn().LogicalWriter<int>())
                            {
                                minuteWriter.WriteBatch(marketStates.Select(s => s.Minute).ToArray());
                            }
                            using (var dayWriter = rowGroup.NextColumn().LogicalWriter<int>())
                            {
                                dayWriter.WriteBatch(marketStates.Select(s => s.Day).ToArray());
                            }
                            using (var pivotPointWriter = rowGroup.NextColumn().LogicalWriter<double>())
                            {
                                pivotPointWriter.WriteBatch(marketStates.Select(s => s.PivotPoint).ToArray());
                            }
                            using (var support1Writer = rowGroup.NextColumn().LogicalWriter<double>())
                            {
                                support1Writer.WriteBatch(marketStates.Select(s => s.Support1).ToArray());
                            }
                            using (var support2Writer = rowGroup.NextColumn().LogicalWriter<double>())
                            {
                                support2Writer.WriteBatch(marketStates.Select(s => s.Support2).ToArray());
                            }
                            using (var support3Writer = rowGroup.NextColumn().LogicalWriter<double>())
                            {
                                support3Writer.WriteBatch(marketStates.Select(s => s.Support3).ToArray());
                            }
                            using (var resistance1Writer = rowGroup.NextColumn().LogicalWriter<double>())
                            {
                                resistance1Writer.WriteBatch(marketStates.Select(s => s.Resistance1).ToArray());
                            }
                            using (var resistance2Writer = rowGroup.NextColumn().LogicalWriter<double>())
                            {
                                resistance2Writer.WriteBatch(marketStates.Select(s => s.Resistance2).ToArray());
                            }
                            using (var resistance3Writer = rowGroup.NextColumn().LogicalWriter<double>())
                            {
                                resistance3Writer.WriteBatch(marketStates.Select(s => s.Resistance3).ToArray());
                            }
                            using (var volumeSupportWriter = rowGroup.NextColumn().LogicalWriter<double?>())
                            {
                                volumeSupportWriter.WriteBatch(marketStates.Select(s => s.VolumeSupport).ToArray());
                            }
                            using (var volumeResistanceWriter = rowGroup.NextColumn().LogicalWriter<double?>())
                            {
                                volumeResistanceWriter.WriteBatch(marketStates.Select(s => s.VolumeResistance).ToArray());
                            }
                            using (var sma5Writer = rowGroup.NextColumn().LogicalWriter<double?>())
                            {
                                sma5Writer.WriteBatch(marketStates.Select(s => s.SMA5).ToArray());
                            }
                            using (var sma10Writer = rowGroup.NextColumn().LogicalWriter<double?>())
                            {
                                sma10Writer.WriteBatch(marketStates.Select(s => s.SMA10).ToArray());
                            }
                            using (var atrWriter = rowGroup.NextColumn().LogicalWriter<double?>())
                            {
                                atrWriter.WriteBatch(marketStates.Select(s => s.ATR).ToArray());
                            }
                            using (var rsiWriter = rowGroup.NextColumn().LogicalWriter<double?>())
                            {
                                rsiWriter.WriteBatch(marketStates.Select(s => s.RSI).ToArray());
                            }
                            using (var macdWriter = rowGroup.NextColumn().LogicalWriter<double?>())
                            {
                                macdWriter.WriteBatch(marketStates.Select(s => s.MACD).ToArray());
                            }
                            using (var macdSignalWriter = rowGroup.NextColumn().LogicalWriter<double?>())
                            {
                                macdSignalWriter.WriteBatch(marketStates.Select(s => s.MACDSignal).ToArray());
                            }
                        }
                    }

                    // Check file size after writing
                    long fileSize = new FileInfo(filePath).Length;
                    if (fileSize >= minFileSize)
                    {
                        success = true;
                        Console.WriteLine($"Saved {marketStates.Count} rows to {filePath} (Size: {fileSize / 1024} KB)");
                    }
                    else
                    {
                        Console.WriteLine($"Attempt {attempt} failed: File size {fileSize / 1024} KB is less than minimum {minFileSize / 1024} KB");
                        if (attempt < maxAttempts)
                        {
                            // Delete the corrupted file before retrying
                            File.Delete(filePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attempt {attempt} failed with error: {ex.Message}");
                    if (attempt < maxAttempts)
                    {
                        // Delete the file if it exists before retrying
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }
                }
            }

            if (!success)
            {
                Console.WriteLine($"Failed to save valid Parquet file after {maxAttempts} attempts");
            }
        }
    }
}
