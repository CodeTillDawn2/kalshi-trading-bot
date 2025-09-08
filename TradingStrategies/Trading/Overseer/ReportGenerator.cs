using System.Text;

namespace TradingStrategies.Trading.Overseer
{
    public class ReportGenerator
    {
        public class EventLog
        {
            public DateTime Timestamp { get; set; }
            public string MarketType { get; set; }
            public string Action { get; set; }
            public int Position { get; set; }
            public double Cash { get; set; }
            public double Liquidity { get; set; }
            public int YesSpread { get; set; }
            public double TradeRate { get; set; }
            public double RSI { get; set; }
            public string Strategies { get; set; }

            // Fields for Yes and No sides
            public int BestYesBid { get; set; }
            public int BestYesAsk { get; set; }
            public int BestNoBid { get; set; }
            public int BestNoAsk { get; set; }
            public int NoSpread { get; set; }
            public int DepthAtBestYesBid { get; set; }
            public int DepthAtBestYesAsk { get; set; }
            public int DepthAtBestNoBid { get; set; }
            public int DepthAtBestNoAsk { get; set; }
            public int TotalYesBidContracts { get; set; }
            public int TotalNoBidContracts { get; set; }
            public int BidImbalance { get; set; }
            public double TradeRatePerMinute_Yes { get; set; }
            public double TradeRatePerMinute_No { get; set; }
            public double TradeVolumePerMinute_Yes { get; set; }
            public double TradeVolumePerMinute_No { get; set; }
            public int TradeCount_Yes { get; set; }
            public int TradeCount_No { get; set; }
            public double AverageTradeSize_Yes { get; set; }
            public double AverageTradeSize_No { get; set; }

            public string RestingYesBids { get; set; } = "";  // Summary of resting bids on yes side, e.g., "price:qty, price:qty"
            public string RestingNoBids { get; set; } = "";   // Summary of resting bids on no side, e.g., "price:qty, price:qty"
            public string Memo { get; set; } = "";
            public double AverageCost { get; set; } = 0.0;  // Average cost of the position
            public List<SmokehousePatterns.PatternDefinitions.PatternDefinition> Patterns { get; set; } = new List<SmokehousePatterns.PatternDefinitions.PatternDefinition>();  // Detected patterns for this snapshot
        }

        public class EventGroup
        {
            public string MarketType { get; set; }
            public string Action { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public int SnapshotCount { get; set; }
            public int StartPosition { get; set; }
            public int EndPosition { get; set; }
            public double StartCash { get; set; }
            public double EndCash { get; set; }
            public double AvgLiquidity { get; set; }
            public double AvgRSI { get; set; }
            public string Strategies { get; set; }
            // Fields for Yes and No sides
            public double AvgYesSpread { get; set; }
            public double AvgNoSpread { get; set; }
            public double AvgTradeRateYes { get; set; }
            public double AvgTradeRateNo { get; set; }
            public double AvgDepthNoBid { get; set; }
            public double AvgBidImbalance { get; set; }
            public double AvgYesBidPrice { get; set; } // In dollars
            public double AvgNoBidPrice { get; set; } // In dollars

            public double AvgRestingYesQty { get; set; }
            public double AvgRestingNoQty { get; set; }
        }

        public class PathInfo
        {
            public List<string> Strats { get; set; } = new List<string>();
        }

        public class PathPerformance
        {
            public string MarketId { get; set; }
            public string PathTaken { get; set; }
            public Dictionary<string, int> SnapshotsPerType { get; set; }
            public double PnL { get; set; }
            public double Equity { get; set; }
            public int Trades { get; set; }
            public int StartYesBid { get; set; }
            public int StartNoBid { get; set; }
            public int EndYesBid { get; set; }
            public int EndNoBid { get; set; }
            public string EndType { get; set; }
            public int SimulatedPosition { get; set; }
            public double AverageCost { get; set; }
        }

        public string GenerateDetailedPerformanceReport(string marketId, List<EventLog> events, double initialCash, Dictionary<string, PathInfo> paths, bool writeToFile, string outputDir = @"C:\Users\Peter\Documents\GitHub\TestingOutput")
        {
            if (events == null || events.Count == 0) return "No events to report.";

            var sb = new StringBuilder();

            // Correct spreads and asks/bids for all events
            foreach (var ev in events)
            {
                ev.BestYesAsk = ev.BestNoBid > 0 ? 100 - ev.BestNoBid : 100;
                ev.BestNoAsk = ev.BestYesBid > 0 ? 100 - ev.BestYesBid : 100;
                ev.YesSpread = ev.BestYesAsk - ev.BestYesBid;
                ev.NoSpread = ev.BestNoAsk - ev.BestNoBid;
            }

            // Summary Section as CSV
            sb.AppendLine("Section,Key,Value");
            sb.AppendLine($"Summary,Market ID,{marketId}");
            var start = events.First().Timestamp;
            var end = events.Last().Timestamp;
            sb.AppendLine($"Summary,Simulation Period Start,{start}");
            sb.AppendLine($"Summary,Simulation Period End,{end}");
            sb.AppendLine($"Summary,Initial Cash,{initialCash:F2}");

            var finalEvent = events.Last();
            bool natural = finalEvent.BestYesBid == 0 || finalEvent.BestNoBid == 0;
            double finalEquity;
            if (natural)
            {
                bool resolvedYes = finalEvent.BestNoBid == 0;
                if (finalEvent.Position > 0)
                {
                    finalEquity = finalEvent.Cash + (resolvedYes ? finalEvent.Position * 1.0 : 0.0);
                }
                else if (finalEvent.Position < 0)
                {
                    finalEquity = finalEvent.Cash + (resolvedYes ? 0.0 : Math.Abs(finalEvent.Position) * 1.0);
                }
                else
                {
                    finalEquity = finalEvent.Cash;
                }
            }
            else
            {
                double midYes = (finalEvent.BestYesBid + finalEvent.BestYesAsk) / 100.0;
                double midNo = (finalEvent.BestNoBid + finalEvent.BestNoAsk) / 100.0;
                if (finalEvent.Position > 0)
                {
                    finalEquity = finalEvent.Cash + finalEvent.Position * midYes;
                }
                else if (finalEvent.Position < 0)
                {
                    finalEquity = finalEvent.Cash + Math.Abs(finalEvent.Position) * midNo;
                }
                else
                {
                    finalEquity = finalEvent.Cash;
                }
            }
            double pnl = finalEquity - initialCash;
            double returnPct = initialCash > 0 ? (pnl / initialCash * 100) : 0;

            sb.AppendLine($"Summary,Final Cash,{finalEvent.Cash:F2}");
            sb.AppendLine($"Summary,Final Position,{finalEvent.Position}");
            sb.AppendLine($"Summary,Final Held,{(finalEvent.Position > 0 ? "Yes" : finalEvent.Position < 0 ? "No" : "None")},{Math.Abs(finalEvent.Position)}");
            sb.AppendLine($"Summary,Final Yes Bid Price,{finalEvent.BestYesBid / 100.0:F2}");
            sb.AppendLine($"Summary,Final No Bid Price,{finalEvent.BestNoBid / 100.0:F2}");
            sb.AppendLine($"Summary,Final Equity,{finalEquity:F2}");
            sb.AppendLine($"Summary,P&L,{pnl:F2}");
            sb.AppendLine($"Summary,Return %,{returnPct:F2}");

            // Statistics
            sb.AppendLine($"Statistics,Total Snapshots,{events.Count}");

            int totalTrades = 0;
            double avgTradeSize = 0;
            for (int i = 1; i < events.Count; i++)
            {
                int delta = Math.Abs(events[i].Position - events[i - 1].Position);
                if (delta > 0)
                {
                    totalTrades++;
                    avgTradeSize += delta;
                }
            }
            avgTradeSize = totalTrades > 0 ? avgTradeSize / totalTrades : 0;
            sb.AppendLine($"Statistics,Total Trades,{totalTrades}");
            sb.AppendLine($"Statistics,Average Trade Size,{avgTradeSize:F2}");

            int maxPos = events.Max(e => e.Position);
            int minPos = events.Min(e => e.Position);
            sb.AppendLine($"Statistics,Max Position,{maxPos}");
            sb.AppendLine($"Statistics,Min Position,{minPos}");

            double avgLiquidity = events.Average(e => e.Liquidity);
            double avgYesSpread = events.Average(e => e.YesSpread);
            double avgNoSpread = events.Average(e => e.NoSpread);
            double avgTradeRateYes = events.Average(e => e.TradeRatePerMinute_Yes);
            double avgTradeRateNo = events.Average(e => e.TradeRatePerMinute_No);
            double avgTradeVolumeYes = events.Average(e => e.TradeVolumePerMinute_Yes);
            double avgTradeVolumeNo = events.Average(e => e.TradeVolumePerMinute_No);
            sb.AppendLine($"Statistics,Average Liquidity,{avgLiquidity:F1}");
            sb.AppendLine($"Statistics,Average YesSpread,{avgYesSpread:F1}");
            sb.AppendLine($"Statistics,Average NoSpread,{avgNoSpread:F1}");
            sb.AppendLine($"Statistics,Average TradeRate Yes,{avgTradeRateYes:F2}");
            sb.AppendLine($"Statistics,Average TradeRate No,{avgTradeRateNo:F2}");
            sb.AppendLine($"Statistics,Average TradeVolume Yes,{avgTradeVolumeYes:F2}");
            sb.AppendLine($"Statistics,Average TradeVolume No,{avgTradeVolumeNo:F2}");

            // Market Type Distribution
            sb.AppendLine();
            sb.AppendLine("Market Type Distribution");
            sb.AppendLine("Market Type,Snapshots,Percentage");
            var marketDist = events.GroupBy(e => e.MarketType).ToDictionary(g => g.Key, g => g.Count());
            foreach (var kv in marketDist)
            {
                var pct = (kv.Value * 100.0 / events.Count);
                sb.AppendLine($"\"{kv.Key}\",{kv.Value},{pct:F1}");
            }

            // Order Book Summary (Averages)
            sb.AppendLine();
            sb.AppendLine("Order Book Summary (Averages)");
            sb.AppendLine("Metric,Yes Side,No Side");
            sb.AppendLine($"Average Best Bid,{events.Average(e => e.BestYesBid):F0},{events.Average(e => e.BestNoBid):F0}");
            sb.AppendLine($"Average Best Ask,{events.Average(e => e.BestYesAsk):F0},{events.Average(e => e.BestNoAsk):F0}");
            sb.AppendLine($"Average Depth at Best Bid,{events.Average(e => e.DepthAtBestYesBid):F0},{events.Average(e => e.DepthAtBestNoBid):F0}");
            sb.AppendLine($"Average Depth at Best Ask,{events.Average(e => e.DepthAtBestYesAsk):F0},{events.Average(e => e.DepthAtBestNoAsk):F0}");
            sb.AppendLine($"Average Total Contracts,{events.Average(e => e.TotalYesBidContracts):F0},{events.Average(e => e.TotalNoBidContracts):F0}");
            sb.AppendLine($"Average Bid Imbalance,{events.Average(e => e.BidImbalance):F0},N/A");
            sb.AppendLine($"Average Trade Count,{events.Average(e => e.TradeCount_Yes):F0},{events.Average(e => e.TradeCount_No):F0}");
            sb.AppendLine($"Average Average Trade Size,{events.Average(e => e.AverageTradeSize_Yes):F2},{events.Average(e => e.AverageTradeSize_No):F2}");

            // Full Event Log per Snapshot (Added buy/sold columns after resting bids)
            sb.AppendLine();
            sb.AppendLine("Full Event Log (Per Snapshot)");
            sb.AppendLine("Timestamp,Market Type,Action,Position,Cash,Equity,BestYesAsk,BestYesBid,BestNoAsk,BestNoBid,RestingYesBids,RestingNoBids,YesBought,YesSold,NoBought,NoSold,YesSpread,NoSpread,Liquidity,TradeRateYes,TradeRateNo,TradeVolumeYes,TradeVolumeNo,TradeCountYes,TradeCountNo,AvgTradeSizeYes,AvgTradeSizeNo,DepthBestYesBid,DepthBestNoBid,TotalYesBidContracts,TotalNoBidContracts,BidImbalance,RSI,PnL Since Start,Strategies,Arbitrage Note");
            double cumulativePnL = 0;
            int prevYesBought = 0, prevYesSold = 0, prevNoBought = 0, prevNoSold = 0; // Track for deltas
            for (int i = 0; i < events.Count; i++)
            {
                var ev = events[i];
                int yesBought = 0, yesSold = 0, noBought = 0, noSold = 0;
                if (i > 0)
                {
                    var prevEv = events[i - 1];
                    // Calculate deltas (buys positive, sells negative for each side)
                    int yesDelta = ev.BestYesBid - prevEv.BestYesBid; // Simplified proxy; adjust based on actual fill data if available
                    yesBought = Math.Max(yesDelta, 0);
                    yesSold = Math.Abs(Math.Min(yesDelta, 0));
                    int noDelta = ev.BestNoBid - prevEv.BestNoBid;
                    noBought = Math.Max(noDelta, 0);
                    noSold = Math.Abs(Math.Min(noDelta, 0));
                }
                if (string.IsNullOrEmpty(ev.Strategies)) ev.Strategies = "N/A";
                if (string.IsNullOrEmpty(ev.RestingYesBids)) ev.RestingYesBids = "N/A";
                if (string.IsNullOrEmpty(ev.RestingNoBids)) ev.RestingNoBids = "N/A";
                double equity = CalculateEquity(ev);
                cumulativePnL = equity - initialCash;
                string arbNote = (ev.YesSpread < 0 || ev.NoSpread < 0) ? "Potential Arbitrage (Negative Spread)" : (ev.BestYesAsk == 100 ? "No Yes Ask Available" : (ev.BestNoAsk == 100 ? "No No Ask Available" : ""));
                if (string.IsNullOrEmpty(arbNote)) arbNote = "N/A";
                sb.AppendLine($"{ev.Timestamp},\"{ev.MarketType}\",\"{ev.Action}\",{ev.Position},{ev.Cash:F2},{equity:F2},{ev.BestYesAsk},{ev.BestYesBid},{ev.BestNoAsk},{ev.BestNoBid},\"{ev.RestingYesBids}\",\"{ev.RestingNoBids}\",{yesBought},{yesSold},{noBought},{noSold},{ev.YesSpread},{ev.NoSpread},{ev.Liquidity:F1},{ev.TradeRatePerMinute_Yes:F2},{ev.TradeRatePerMinute_No:F2},{ev.TradeVolumePerMinute_Yes:F2},{ev.TradeVolumePerMinute_No:F2},{ev.TradeCount_Yes},{ev.TradeCount_No},{ev.AverageTradeSize_Yes:F2},{ev.AverageTradeSize_No:F2},{ev.DepthAtBestYesBid},{ev.DepthAtBestNoBid},{ev.TotalYesBidContracts},{ev.TotalNoBidContracts},{ev.BidImbalance},{ev.RSI:F2},{cumulativePnL:F2},\"{ev.Strategies}\",\"{arbNote}\"");
                prevYesBought = yesBought;
                prevYesSold = yesSold;
                prevNoBought = noBought;
                prevNoSold = noSold;
            }

            // Summarized Event Timeline
            sb.AppendLine();
            sb.AppendLine("Summarized Event Timeline");
            sb.AppendLine("Market Type,Action,Start Time,End Time,Snapshots,Start Position,End Position,Start Cash,End Cash,Avg YesBidPrice,Avg NoBidPrice,Avg Liquidity,Avg RSI,Avg YesSpread,Avg NoSpread,Avg TradeRateYes,Avg TradeRateNo,Avg DepthYesBid,Avg DepthNoBid,Avg BidImbalance,Strategies,AvgRestingYesQty,AvgRestingNoQty,Nuance");
            var groups = GroupEvents(events);
            foreach (var group in groups)
            {
                string nuance = GenerateNuance(group);
                sb.AppendLine($"\"{group.MarketType}\",\"{group.Action}\",{group.StartTime},{group.EndTime},{group.SnapshotCount},{group.StartPosition},{group.EndPosition},{group.StartCash:F2},{group.EndCash:F2},{group.AvgYesBidPrice:F2},{group.AvgNoBidPrice:F2},{group.AvgLiquidity:F1},{group.AvgRSI:F2},{group.AvgYesSpread:F1},{group.AvgNoSpread:F1},{group.AvgTradeRateYes:F2},{group.AvgTradeRateNo:F2},{group.AvgDepthNoBid:F0},{group.AvgBidImbalance:F0},\"{group.Strategies}\",{Math.Round(group.AvgRestingYesQty)},{Math.Round(group.AvgRestingNoQty)},\"{nuance}\"");
            }

            // Path Definitions
            sb.AppendLine();
            sb.AppendLine("Path Definitions");
            var actualPath = string.Join(" → ", events.Select(e => e.MarketType).Distinct().ToArray());
            sb.AppendLine($"Actual Path Taken: {actualPath}");
            sb.AppendLine("Path,Strategies");
            foreach (var kv in paths)
            {
                var stratsStr = string.Join(";", kv.Value.Strats);
                if (string.IsNullOrEmpty(stratsStr)) stratsStr = "N/A";
                sb.AppendLine($"\"{kv.Key}\",\"{stratsStr}\"");
            }

            var detailedReport = sb.ToString();

            if (writeToFile)
            {
                var performanceFile = Path.Combine(outputDir, $"{marketId}_DetailedPerformance.csv");
                File.WriteAllText(performanceFile, detailedReport);
            }

            return detailedReport;
        }

        private double CalculateEquity(EventLog ev)
        {
            if (ev.Position > 0)
            {
                return ev.Cash + ev.Position * (ev.BestYesBid / 100.0);
            }
            else if (ev.Position < 0)
            {
                return ev.Cash + Math.Abs(ev.Position) * (ev.BestNoBid / 100.0);
            }
            return ev.Cash;
        }

        private List<EventGroup> GroupEvents(List<EventLog> events)
        {
            var groups = new List<EventGroup>();
            if (events.Count == 0) return groups;

            EventLog current = events[0];
            var group = new EventGroup
            {
                MarketType = current.MarketType,
                Action = current.Action,
                StartTime = current.Timestamp,
                EndTime = current.Timestamp,
                SnapshotCount = 1,
                StartPosition = current.Position,
                EndPosition = current.Position,
                StartCash = current.Cash,
                EndCash = current.Cash,
                AvgLiquidity = current.Liquidity,
                AvgRSI = current.RSI,
                Strategies = current.Strategies,
                AvgYesSpread = current.YesSpread,
                AvgNoSpread = current.NoSpread,
                AvgTradeRateYes = current.TradeRatePerMinute_Yes,
                AvgTradeRateNo = current.TradeRatePerMinute_No,
                AvgDepthNoBid = current.DepthAtBestNoBid,
                AvgBidImbalance = current.BidImbalance,
                AvgYesBidPrice = current.BestYesBid / 100.0,
                AvgNoBidPrice = current.BestNoBid / 100.0,
                AvgRestingYesQty = string.IsNullOrEmpty(current.RestingYesBids) || current.RestingYesBids == "N/A" ? 0 : current.RestingYesBids.Split(',').Sum(s =>
                {
                    var qtyStr = s.Trim().Split(':').LastOrDefault() ?? "0";
                    qtyStr = qtyStr.Trim();
                    return int.TryParse(qtyStr, out int qty) ? qty : 0;
                }),
                AvgRestingNoQty = string.IsNullOrEmpty(current.RestingNoBids) || current.RestingNoBids == "N/A" ? 0 : current.RestingNoBids.Split(',').Sum(s =>
                {
                    var qtyStr = s.Trim().Split(':').LastOrDefault() ?? "0";
                    qtyStr = qtyStr.Trim();
                    return int.TryParse(qtyStr, out int qty) ? qty : 0;
                })
            };
            if (string.IsNullOrEmpty(group.Strategies)) group.Strategies = "N/A";

            for (int i = 1; i < events.Count; i++)
            {
                var next = events[i];
                bool sameGroup = next.MarketType == group.MarketType &&
                                 next.Action == group.Action &&
                                 next.Position == group.EndPosition &&
                                 next.Cash == group.EndCash &&
                                 next.RSI == group.AvgRSI;

                if (sameGroup)
                {
                    group.SnapshotCount++;
                    group.EndTime = next.Timestamp;
                    group.AvgLiquidity = (group.AvgLiquidity * (group.SnapshotCount - 1) + next.Liquidity) / group.SnapshotCount;
                    group.AvgRSI = (group.AvgRSI * (group.SnapshotCount - 1) + next.RSI) / group.SnapshotCount;
                    group.AvgYesSpread = (group.AvgYesSpread * (group.SnapshotCount - 1) + next.YesSpread) / group.SnapshotCount;
                    group.AvgNoSpread = (group.AvgNoSpread * (group.SnapshotCount - 1) + next.NoSpread) / group.SnapshotCount;
                    group.AvgTradeRateYes = (group.AvgTradeRateYes * (group.SnapshotCount - 1) + next.TradeRatePerMinute_Yes) / group.SnapshotCount;
                    group.AvgTradeRateNo = (group.AvgTradeRateNo * (group.SnapshotCount - 1) + next.TradeRatePerMinute_No) / group.SnapshotCount;
                    group.AvgDepthNoBid = (group.AvgDepthNoBid * (group.SnapshotCount - 1) + next.DepthAtBestNoBid) / group.SnapshotCount;
                    group.AvgBidImbalance = (group.AvgBidImbalance * (group.SnapshotCount - 1) + next.BidImbalance) / group.SnapshotCount;
                    group.AvgYesBidPrice = (group.AvgYesBidPrice * (group.SnapshotCount - 1) + (next.BestYesBid / 100.0)) / group.SnapshotCount;
                    group.AvgNoBidPrice = (group.AvgNoBidPrice * (group.SnapshotCount - 1) + (next.BestNoBid / 100.0)) / group.SnapshotCount;
                    group.AvgRestingYesQty = (group.AvgRestingYesQty * (group.SnapshotCount - 1) + (string.IsNullOrEmpty(next.RestingYesBids) || next.RestingYesBids == "N/A" ? 0 : next.RestingYesBids.Split(',').Sum(s =>
                    {
                        var qtyStr = s.Trim().Split(':').LastOrDefault() ?? "0";
                        qtyStr = qtyStr.Trim();
                        return int.TryParse(qtyStr, out int qty) ? qty : 0;
                    }))) / group.SnapshotCount;
                    group.AvgRestingNoQty = (group.AvgRestingNoQty * (group.SnapshotCount - 1) + (string.IsNullOrEmpty(next.RestingNoBids) || next.RestingNoBids == "N/A" ? 0 : next.RestingNoBids.Split(',').Sum(s =>
                    {
                        var qtyStr = s.Trim().Split(':').LastOrDefault() ?? "0";
                        qtyStr = qtyStr.Trim();
                        return int.TryParse(qtyStr, out int qty) ? qty : 0;
                    }))) / group.SnapshotCount;
                }
                else
                {
                    groups.Add(group);
                    group = new EventGroup
                    {
                        MarketType = next.MarketType,
                        Action = next.Action,
                        StartTime = next.Timestamp,
                        EndTime = next.Timestamp,
                        SnapshotCount = 1,
                        StartPosition = next.Position,
                        EndPosition = next.Position,
                        StartCash = next.Cash,
                        EndCash = next.Cash,
                        AvgLiquidity = next.Liquidity,
                        AvgRSI = next.RSI,
                        Strategies = next.Strategies,
                        AvgYesSpread = next.YesSpread,
                        AvgNoSpread = next.NoSpread,
                        AvgTradeRateYes = next.TradeRatePerMinute_Yes,
                        AvgTradeRateNo = next.TradeRatePerMinute_No,
                        AvgDepthNoBid = next.DepthAtBestNoBid,
                        AvgBidImbalance = next.BidImbalance,
                        AvgYesBidPrice = next.BestYesBid / 100.0,
                        AvgNoBidPrice = next.BestNoBid / 100.0,
                        AvgRestingYesQty = string.IsNullOrEmpty(next.RestingYesBids) || next.RestingYesBids == "N/A" ? 0 : next.RestingYesBids.Split(',').Sum(s =>
                        {
                            var qtyStr = s.Trim().Split(':').LastOrDefault() ?? "0";
                            qtyStr = qtyStr.Trim();
                            return int.TryParse(qtyStr, out int qty) ? qty : 0;
                        }),
                        AvgRestingNoQty = string.IsNullOrEmpty(next.RestingNoBids) || next.RestingNoBids == "N/A" ? 0 : next.RestingNoBids.Split(',').Sum(s =>
                        {
                            var qtyStr = s.Trim().Split(':').LastOrDefault() ?? "0";
                            qtyStr = qtyStr.Trim();
                            return int.TryParse(qtyStr, out int qty) ? qty : 0;
                        })
                    };
                    if (string.IsNullOrEmpty(group.Strategies)) group.Strategies = "N/A";
                }
                current = next;
            }
            groups.Add(group);

            return groups;
        }

        private string GenerateNuance(EventGroup group)
        {
            var nuance = new List<string> { "Gradual position changes; liquidity trends." };
            if (group.AvgTradeRateNo > 1.0) nuance.Add("High No trade activity.");
            if (group.AvgBidImbalance > 500) nuance.Add("Strong Yes bid imbalance.");
            else if (group.AvgBidImbalance < -500) nuance.Add("Strong No bid imbalance.");
            if (group.AvgNoSpread > 5) nuance.Add("Wide No spread - low liquidity on No side.");
            if (group.AvgYesBidPrice > 0.9) nuance.Add("High Yes bid price.");
            if (group.AvgNoBidPrice < 0.1) nuance.Add("Low No bid price.");
            return string.Join("; ", nuance);
        }

        public string GenerateFinalPerformanceReport(string marketId, string pathTaken, Dictionary<string, int> snapshotsPerType, double pnl, double finalEquity, string notes, bool writeToFile, string outputDir = @"C:\Users\Peter\Documents\GitHub\TestingOutput")
        {
            var sb = new StringBuilder();
            sb.AppendLine("Market,Path Taken,Snapshots per Type,Final P&L,Final Equity,Notes");

            string snapshotsStr = string.Join(";", snapshotsPerType.Select(kv => $"{kv.Key}:{kv.Value}"));
            sb.AppendLine($"\"{marketId}\",\"{pathTaken}\",\"{snapshotsStr}\",{pnl:F2},{finalEquity:F2},\"{notes}\"");
            var finalReport = sb.ToString();

            if (writeToFile)
            {
                var performanceFile = Path.Combine(outputDir, $"{marketId}_FinalPerformance.csv");
                File.WriteAllText(performanceFile, finalReport);
            }

            return finalReport;
        }

        public string GenerateRollupReport(List<PathPerformance> allPerformances, bool writeToFile, string outputDir = @"C:\Users\Peter\Documents\GitHub\TestingOutput")
        {
            var sb = new StringBuilder();
            sb.AppendLine("Market,Path Taken,Snapshots per Type,Final P&L,Final Equity,Trades,Start Yes Bid,Start No Bid,End Yes Bid,End No Bid,End Type");

            var groupedByMarket = allPerformances.GroupBy(p => p.MarketId).OrderBy(g => g.Key);
            foreach (var marketGroup in groupedByMarket)
            {
                foreach (var perf in marketGroup.OrderByDescending(p => p.Equity))
                {
                    string snapshotsStr = string.Join(";", perf.SnapshotsPerType.Select(kv => $"{kv.Key}:{kv.Value}"));
                    sb.AppendLine($"\"{perf.MarketId}\",\"{perf.PathTaken}\",\"{snapshotsStr}\",${perf.PnL:F2},${perf.Equity:F2},{perf.Trades},{perf.StartYesBid},{perf.StartNoBid},{perf.EndYesBid},{perf.EndNoBid},{perf.EndType}");
                }
            }

            // Aggregate Statistics as additional lines (commented in CSV)
            sb.AppendLine();
            sb.AppendLine("# Aggregate Statistics");
            if (allPerformances.Count > 0)
            {
                double avgPnl = allPerformances.Average(p => p.PnL);
                double avgEquity = allPerformances.Average(p => p.Equity);
                int totalTrades = allPerformances.Sum(p => p.Trades);
                int totalPaths = allPerformances.Count;
                int totalMarkets = groupedByMarket.Count();
                sb.AppendLine($"# Total Markets: {totalMarkets}");
                sb.AppendLine($"# Total Paths: {totalPaths}");
                sb.AppendLine($"# Average P&L: ${avgPnl:F2}");
                sb.AppendLine($"# Average Final Equity: ${avgEquity:F2}");
                sb.AppendLine($"# Total Trades: {totalTrades}");
                sb.AppendLine($"# Average Trades per Path: {(double)totalTrades / totalPaths:F2}");
            }
            else
            {
                sb.AppendLine("# No performances to aggregate.");
            }

            var rollupReport = sb.ToString();

            if (writeToFile)
            {
                var rollupFile = Path.Combine(outputDir, "RollupReport.csv");
                File.WriteAllText(rollupFile, rollupReport);
            }

            return rollupReport;
        }
    }
}