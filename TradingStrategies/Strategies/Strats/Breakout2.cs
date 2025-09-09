using BacklashDTOs;
using System.Globalization;
using System.Text.Json;
using static BacklashInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Strategies.Strats
{
    // - Ratio-first breakout logic with depth-capped velocity thresholds
    // - Relative spike detection (current vs previous), optional volume-weighted spike scaling
    // - Trade-rate/event-share confirmations (single thresholds applied symmetrically)
    // - Position-aware exits:
    //     * EXIT if price flattens (RSI_Short near 50 within ExitRsiDevThreshold)
    //     * Only FLIP (reverse) if opposite signal meets ExitOppositeSignalStrength
    // - Full actionMemo diagnostics: labels-first, comma-free, multi-line
    public class Breakout2 : Strat
    {
        public string Name { get; private set; }
        public override double Weight { get; }

        private readonly ActionType _defaultAction;
        private readonly Dictionary<ParamKey, double> _mlParams;

        public enum ParamKey
        {
            AbsorptionThreshold,
            MinSignalStrength,
            VelocityToDepthRatio,
            MinNumberOfPointsFromResolved,
            MaxVolumeRatio,
            MinRatioDifference,
            ReversalExtraStrength,

            // Spike detection threshold: |currentVel| >= X * |prevVel|
            SpikeMinRelativeIncrease,

            // Spike weighting (optional)
            SpikeWeightScale,
            SpikeWeightCap,
            SpikeVolumeWeightScale,

            // Confirmations (share of total) — single thresholds for both sides
            TradeRateShareMin,
            TradeEventShareMin,

            // Exit knobs
            ExitOppositeSignalStrength, // min signal strength to flip while holding
            ExitRsiDevThreshold,        // RSI_Short distance from 50 to deem "flat"

            // NEW: flatness exit tuning
            ExitFlatBidRangeMax,        // ticks/cents
            ExitFlatQuietRatio,         // fraction of thr
            ExitFlatVolContextMax,      // 0..1
            ExitFlatTradeRateMax        // trades/min
        }

        public Breakout2(string name = nameof(Breakout2), double weight = 1.0,
                  Dictionary<ParamKey, double>? mlParams = null)
        {
            Name = name;
            Weight = weight;
            _mlParams = mlParams ?? new Dictionary<ParamKey, double>
            {
                { ParamKey.AbsorptionThreshold, 10.0 },
                { ParamKey.MinSignalStrength, 2.0 },
                { ParamKey.VelocityToDepthRatio, 0.10 },
                { ParamKey.MinNumberOfPointsFromResolved, 5 },
                { ParamKey.MaxVolumeRatio, 6.0 },
                { ParamKey.MinRatioDifference, 0.05 },
                { ParamKey.ReversalExtraStrength, 1.0 },

                { ParamKey.SpikeMinRelativeIncrease, 2.0 },

                { ParamKey.SpikeWeightScale, 1.0 },
                { ParamKey.SpikeWeightCap, 5.0 },
                { ParamKey.SpikeVolumeWeightScale, 0.5 },

                { ParamKey.TradeRateShareMin, 0.55 },
                { ParamKey.TradeEventShareMin, 0.35 },

                { ParamKey.ExitOppositeSignalStrength, 3.0 },
                { ParamKey.ExitRsiDevThreshold, 5.0 },

                // NEW defaults
                { ParamKey.ExitFlatBidRangeMax, 1.0 },
                { ParamKey.ExitFlatQuietRatio, 0.40 },
                { ParamKey.ExitFlatVolContextMax, 0.12 },
                { ParamKey.ExitFlatTradeRateMax, 0.30 }
            };
            _defaultAction = ActionType.None;
        }
        public ActionType DefaultAction => _defaultAction;
        public override ActionDecision GetAction(MarketSnapshot snapshot, MarketSnapshot? previousSnapshot, int simulationPosition = 0)
        {
            if (!snapshot.ChangeMetricsMature)
                return new ActionDecision { Type = ActionType.None, Price = 0, Qty = 1, Memo = "reason not_mature" };

            var inv = CultureInfo.InvariantCulture;
            string F(double d)
            {
                if (double.IsNaN(d)) return "NaN";
                if (double.IsInfinity(d)) return "Inf";
                double ad = Math.Abs(d);
                if (ad > 0 && ad < 0.001) return "0";
                return d.ToString("0.##", inv);
            }
            string I(int x) => x.ToString(inv);
            string YN(bool b) => b ? "Yes" : "No";

            var pathTaken = new List<string>();

            double absorptionThreshold = _mlParams.GetValueOrDefault(ParamKey.AbsorptionThreshold, 10.0);
            double minSignalStrength = _mlParams.GetValueOrDefault(ParamKey.MinSignalStrength, 2.0);
            double baseThrRatio = _mlParams.GetValueOrDefault(ParamKey.VelocityToDepthRatio, 0.10);
            int minPointsFromResolved = (int)Math.Round(_mlParams.GetValueOrDefault(ParamKey.MinNumberOfPointsFromResolved, 5.0));
            double maxVolumeRatio = _mlParams.GetValueOrDefault(ParamKey.MaxVolumeRatio, 6.0);
            double minRatioDifference = _mlParams.GetValueOrDefault(ParamKey.MinRatioDifference, 0.05);
            double reversalExtraStrength = _mlParams.GetValueOrDefault(ParamKey.ReversalExtraStrength, 1.0);
            double spikeRel = _mlParams.GetValueOrDefault(ParamKey.SpikeMinRelativeIncrease, 2.0);

            double spikeWeightScale = _mlParams.GetValueOrDefault(ParamKey.SpikeWeightScale, 1.0);
            double spikeWeightCap = _mlParams.GetValueOrDefault(ParamKey.SpikeWeightCap, 5.0);
            double spikeVolScale = _mlParams.GetValueOrDefault(ParamKey.SpikeVolumeWeightScale, 0.5);

            double trShareMin = _mlParams.GetValueOrDefault(ParamKey.TradeRateShareMin, 0.55);
            double teShareMin = _mlParams.GetValueOrDefault(ParamKey.TradeEventShareMin, 0.35);

            double exitOppStrength = _mlParams.GetValueOrDefault(ParamKey.ExitOppositeSignalStrength, 3.0);
            double exitRsiDevThreshold = _mlParams.GetValueOrDefault(ParamKey.ExitRsiDevThreshold, 5.0);

            // NEW pulls
            double exitBidRangeMax = _mlParams.GetValueOrDefault(ParamKey.ExitFlatBidRangeMax, 1.0);
            double exitQuietRatio = _mlParams.GetValueOrDefault(ParamKey.ExitFlatQuietRatio, 0.40);
            double exitVolCtxMax = _mlParams.GetValueOrDefault(ParamKey.ExitFlatVolContextMax, 0.12);
            double exitTRMax = _mlParams.GetValueOrDefault(ParamKey.ExitFlatTradeRateMax, 0.30);

            double vTopYes = snapshot.VelocityPerMinute_Top_Yes_Bid;
            double vBotYes = snapshot.VelocityPerMinute_Bottom_Yes_Bid;
            double vTopNo = snapshot.VelocityPerMinute_Top_No_Bid;
            double vBotNo = snapshot.VelocityPerMinute_Bottom_No_Bid;
            double vSumYes = vTopYes + vBotYes;
            double vSumNo = vTopNo + vBotNo;

            double prevSumYes = 0.0, prevSumNo = 0.0;
            if (previousSnapshot != null)
            {
                prevSumYes = (previousSnapshot.VelocityPerMinute_Top_Yes_Bid + previousSnapshot.VelocityPerMinute_Bottom_Yes_Bid);
                prevSumNo = (previousSnapshot.VelocityPerMinute_Top_No_Bid + previousSnapshot.VelocityPerMinute_Bottom_No_Bid);
            }

            double depthYes = snapshot.TotalOrderbookDepth_Yes / 100.0;
            double depthNo = snapshot.TotalOrderbookDepth_No / 100.0;

            const double MAX_VEL_THR_FIXED = 0.35;
            double effRatio = Math.Min(baseThrRatio, MAX_VEL_THR_FIXED);
            double thrYes = depthYes * effRatio;
            double thrNo = depthNo * effRatio;

            double flowYes = vSumYes / Math.Max(depthYes, 1e-9);
            double flowNo = vSumNo / Math.Max(depthNo, 1e-9);

            double depthRatioYesToNo = depthYes / Math.Max(depthNo, 1e-9);

            bool spikeYes = false, spikeNo = false;
            double relIncYes = 1.0, relIncNo = 1.0;
            if (previousSnapshot != null)
            {
                double pYes = prevSumYes;
                double pNo = prevSumNo;
                double pAbsYes = Math.Abs(pYes);
                double pAbsNo = Math.Abs(pNo);
                double cAbsYes = Math.Abs(vSumYes);
                double cAbsNo = Math.Abs(vSumNo);
                relIncYes = (pAbsYes < 1e-12) ? (cAbsYes > 0 ? double.PositiveInfinity : 1.0) : (cAbsYes / pAbsYes);
                relIncNo = (pAbsNo < 1e-12) ? (cAbsNo > 0 ? double.PositiveInfinity : 1.0) : (cAbsNo / pAbsNo);
                spikeYes = relIncYes >= spikeRel;
                spikeNo = relIncNo >= spikeRel;
            }

            bool flipYes = previousSnapshot != null && Math.Sign(prevSumYes) != Math.Sign(vSumYes) && Math.Abs(prevSumYes) > 0 && Math.Abs(vSumYes) > 0;
            bool flipNo = previousSnapshot != null && Math.Sign(prevSumNo) != Math.Sign(vSumNo) && Math.Abs(prevSumNo) > 0 && Math.Abs(vSumNo) > 0;

            double currMinVol = snapshot.TradeVolumePerMinute_Yes + snapshot.TradeVolumePerMinute_No;
            double maxMinVol = Math.Max(snapshot.HighestVolume_Minute, 1e-9);
            double volContext = Math.Max(0.0, Math.Min(currMinVol / maxMinVol, 1.0));

            double totalTradeRate = Math.Max(snapshot.TradeRatePerMinute_Yes + snapshot.TradeRatePerMinute_No, 1e-9);
            double yesTRShare = snapshot.TradeRatePerMinute_Yes / totalTradeRate;
            double noTRShare = snapshot.TradeRatePerMinute_No / totalTradeRate;

            double totalEvents = (double)(snapshot.TradeCount_Yes + snapshot.TradeCount_No
                                 + snapshot.NonTradeRelatedOrderCount_Yes + snapshot.NonTradeRelatedOrderCount_No);
            double yesEventShare = totalEvents > 0 ? snapshot.TradeCount_Yes / totalEvents : 0.0;
            double noEventShare = totalEvents > 0 ? snapshot.TradeCount_No / totalEvents : 0.0;

            bool confirmYes = yesTRShare >= trShareMin && yesEventShare >= teShareMin;
            bool confirmNo = noTRShare  >= trShareMin && noEventShare  >= teShareMin;

            ActionType candidateAction = _defaultAction;
            double signalStrength = 0.0;

            if (vSumYes > thrYes &&
                snapshot.BestYesAsk <= 100 - minPointsFromResolved &&
                Math.Abs(depthRatioYesToNo) < maxVolumeRatio &&
                (flowYes > Math.Abs(flowNo) + minRatioDifference) &&
                confirmYes)
            {
                candidateAction = ActionType.Long;
                signalStrength += 2.0;
                pathTaken.Add("Long: accel Yes");
            }

            double scaledVNo = -vSumNo * Math.Min(Math.Max(depthYes / Math.Max(depthNo, 1e-9), 0.5), 2.0);
            if (vSumNo < -thrNo &&
                scaledVNo > thrYes &&
                snapshot.BestYesAsk <= 100 - minPointsFromResolved &&
                Math.Abs(depthRatioYesToNo) < maxVolumeRatio &&
                -flowNo > Math.Abs(flowYes) + minRatioDifference &&
                flowYes >= 0 &&
                confirmYes)
            {
                if (candidateAction == ActionType.Long || candidateAction == _defaultAction)
                {
                    candidateAction = ActionType.Long;
                    signalStrength += 2.0;
                    pathTaken.Add("Long: No weakness");
                }
            }

            if (vSumNo > thrNo &&
                snapshot.BestYesBid >= minPointsFromResolved &&
                Math.Abs(depthRatioYesToNo) < maxVolumeRatio &&
                (flowNo > Math.Abs(flowYes) + minRatioDifference) &&
                confirmNo)
            {
                candidateAction = ActionType.Short;
                signalStrength += 2.0;
                pathTaken.Add("Short: accel No");
            }

            double scaledVYes = -vSumYes * Math.Min(Math.Max(depthNo / Math.Max(depthYes, 1e-9), 0.5), 2.0);
            if (vSumYes < -thrYes &&
                scaledVYes > thrNo &&
                snapshot.BestYesBid >= minPointsFromResolved &&
                Math.Abs(depthRatioYesToNo) < maxVolumeRatio &&
                -flowYes > Math.Abs(flowNo) + minRatioDifference &&
                flowNo >= 0 &&
                confirmNo)
            {
                if (candidateAction == ActionType.Short || candidateAction == _defaultAction)
                {
                    candidateAction = ActionType.Short;
                    signalStrength += 2.0;
                    pathTaken.Add("Short: Yes weakness");
                }
            }

            double SpikeWeighted(double relInc, double relThr)
            {
                double baseW = double.IsInfinity(relInc) ? spikeWeightCap : relInc / Math.Max(relThr, 1e-9);
                double volBoost = 1.0 + (spikeVolScale * volContext);
                return Math.Min(baseW * spikeWeightScale * volBoost, spikeWeightCap);
            }

            if (spikeYes && confirmYes)
            {
                if (vSumYes > 0 && (candidateAction == ActionType.Long || candidateAction == _defaultAction))
                {
                    candidateAction = ActionType.Long;
                    signalStrength += SpikeWeighted(relIncYes, spikeRel);
                    pathTaken.Add("Spike: Yes+");
                }
                else if (vSumYes < 0 && (candidateAction == ActionType.Short || candidateAction == _defaultAction))
                {
                    candidateAction = ActionType.Short;
                    signalStrength += SpikeWeighted(relIncYes, spikeRel);
                    pathTaken.Add("Spike: Yes-");
                }
            }
            if (spikeNo && confirmNo)
            {
                if (vSumNo > 0 && (candidateAction == ActionType.Short || candidateAction == _defaultAction))
                {
                    candidateAction = ActionType.Short;
                    signalStrength += SpikeWeighted(relIncNo, spikeRel);
                    pathTaken.Add("Spike: No+");
                }
                else if (vSumNo < 0 && (candidateAction == ActionType.Long || candidateAction == _defaultAction))
                {
                    candidateAction = ActionType.Long;
                    signalStrength += SpikeWeighted(relIncNo, spikeRel);
                    pathTaken.Add("Spike: No-");
                }
            }

            double macdMedHist = snapshot.MACD_Medium.Histogram ?? 0.0;
            if (macdMedHist > 0 && candidateAction == ActionType.Long) { signalStrength += 0.5; pathTaken.Add("MACD:+"); }
            if (macdMedHist < 0 && candidateAction == ActionType.Short) { signalStrength += 0.5; pathTaken.Add("MACD:-"); }

            double yesRemovalRate = -Math.Min(vSumYes, 0.0);
            double noRemovalRate = -Math.Min(vSumNo, 0.0);
            double absorbYes = yesRemovalRate > 0 ? depthYes / yesRemovalRate : double.PositiveInfinity;
            double absorbNo = noRemovalRate > 0 ? depthNo  / noRemovalRate : double.PositiveInfinity;

            if (candidateAction == ActionType.Long  && absorbNo  < absorptionThreshold) { signalStrength += 0.5 * reversalExtraStrength; pathTaken.Add("Absorb:No<thr"); }
            if (candidateAction == ActionType.Short && absorbYes < absorptionThreshold) { signalStrength += 0.5 * reversalExtraStrength; pathTaken.Add("Absorb:Yes<thr"); }

            string BuildActionMemo()
            {
                string Pair(string a, string b) => $"{a} | {b}";
                var lines = new List<string>
                {
                    Pair($"Market: {snapshot.MarketTicker}", $","),
                    Pair($"Time: {snapshot.Timestamp} $", ","),
                    Pair($"Best Yes Bid: {F(snapshot.BestYesBid)}", $"Best Yes Ask: {F(snapshot.BestYesAsk)}"),
                    Pair($"Path: {(pathTaken.Count==0 ? "none" : string.Join(" > ", pathTaken))}", $"SimPos: {I(simulationPosition)}"),

                    Pair($"Vel/min Yes: {F(vSumYes)}", $"Vel/min No: {F(vSumNo)}"),
                    Pair($"Thr Yes: {F(thrYes)}", $"Thr No: {F(thrNo)}"),
                    Pair($"Top 10% Yes: {F(snapshot.VelocityPerMinute_Top_Yes_Bid)}", $"No: {F(snapshot.VelocityPerMinute_Top_No_Bid)}"),
                    Pair($"Bottom 90% Yes: {F(snapshot.VelocityPerMinute_Bottom_Yes_Bid)}", $"No: {F(snapshot.VelocityPerMinute_Bottom_No_Bid)}"),

                    Pair($"Depth$ Yes: {F(depthYes)}", $"Depth$ No: {F(depthNo)}"),
                    Pair($"Flow Yes: {F(flowYes)}", $"Flow No: {F(flowNo)}"),

                    Pair($"TR/min Yes: {F(snapshot.TradeRatePerMinute_Yes)}", $"TR/min No: {F(snapshot.TradeRatePerMinute_No)}"),
                    Pair($"TR share Yes: {F(yesTRShare)}", $"TR share No: {F(noTRShare)}"),
                    Pair($"TE share Yes: {F(yesEventShare)}", $"TE share No: {F(noEventShare)}"),
                    Pair($"Confirm Yes: {YN(confirmYes)}", $"Confirm No: {YN(confirmNo)}"),

                    Pair($"Spike Yes: {YN(spikeYes)}", $"Spike No: {YN(spikeNo)}"),
                    Pair($"Rel inc Yes: {(double.IsInfinity(relIncYes) ? "Inf" : F(relIncYes))}",
                         $"Rel inc No: {(double.IsInfinity(relIncNo) ? "Inf" : F(relIncNo))}"),
                    Pair($"Flip Yes: {YN(flipYes)}", $"Flip No: {YN(flipNo)}"),
                    Pair($"RSI_Short: {snapshot.RSI_Short}",$"RSI_Medium: {snapshot.RSI_Medium}"),

                    Pair($"Absorb Yes: {F(absorbYes)}", $"Absorb No: {F(absorbNo)}"),
                    $"Absorb thr: {F(absorptionThreshold)}",

                    Pair($"Depth ratio Y/N: {F(depthRatioYesToNo)}", $"Signal strength: {F(signalStrength)}"),
                    Pair($"Highest vol(1m): {F(snapshot.HighestVolume_Minute)}", $"Curr vol(1m): {F(currMinVol)}"),
                    Pair($"Vol context 0-1: {F(volContext)}", $"MACD Med hist: {F(macdMedHist)}"),

                    Pair($"Avg size Yes: {F(snapshot.AverageTradeSize_Yes)}", $"Avg size No: {F(snapshot.AverageTradeSize_No)}"),
                    Pair($"Trades Yes: {I(snapshot.TradeCount_Yes)}", $"Trades No: {I(snapshot.TradeCount_No)}"),
                    Pair($"Non-trade Yes: {I(snapshot.NonTradeRelatedOrderCount_Yes)}", $"Non-trade No: {I(snapshot.NonTradeRelatedOrderCount_No)}")
                };
                return string.Join(Environment.NewLine, lines);
            }

            // NEW: multi-signal flatness exit (RSI assists but never acts alone)
            if (simulationPosition != 0)
            {
                double rsiShort = snapshot.RSI_Short ?? 50.0;
                bool rsiFlat = Math.Abs(rsiShort - 50.0) <= exitRsiDevThreshold;

                bool rangeFlat = snapshot.BidRange_Yes <= exitBidRangeMax && snapshot.BidRange_No <= exitBidRangeMax;
                double quietYes = Math.Max(thrYes, 1e-9) * exitQuietRatio;
                double quietNo = Math.Max(thrNo, 1e-9) * exitQuietRatio;
                bool velFlat = Math.Abs(vSumYes) <= quietYes && Math.Abs(vSumNo) <= quietNo;
                bool volFlat = volContext <= exitVolCtxMax;
                bool tradeFlat = totalTradeRate <= exitTRMax;

                bool shouldExit =
                    (rangeFlat && velFlat) ||
                    (velFlat && volFlat) ||
                    (rangeFlat && tradeFlat) ||
                    (rsiFlat && (velFlat || rangeFlat));

                if (shouldExit)
                {
                    pathTaken.Add("Exit: flat");
                    string actionMemo = BuildActionMemo();
                    int exitPrice = simulationPosition > 0 ? snapshot.BestYesBid : snapshot.BestYesAsk;
                    return new ActionDecision
                    {
                        Type = ActionType.Exit,
                        Price = exitPrice,
                        Qty = Math.Abs(simulationPosition),
                        Memo = actionMemo
                    };
                }

                if (simulationPosition > 0 && candidateAction == ActionType.Short && signalStrength < exitOppStrength)
                {
                    pathTaken.Add("Hold: avoid weak flip");
                    string actionMemo = BuildActionMemo();
                    return new ActionDecision { Type = ActionType.None, Price = 0, Qty = 0, Memo = actionMemo };
                }
                if (simulationPosition < 0 && candidateAction == ActionType.Long && signalStrength < exitOppStrength)
                {
                    pathTaken.Add("Hold: avoid weak flip");
                    string actionMemo = BuildActionMemo();
                    return new ActionDecision { Type = ActionType.None, Price = 0, Qty = 0, Memo = actionMemo };
                }
            }

            if (candidateAction == ActionType.None || signalStrength < minSignalStrength)
            {
                pathTaken.Add(candidateAction == ActionType.None ? "Gate: None" : "Gate: weak");
                string actionMemo = BuildActionMemo();
                return new ActionDecision { Type = ActionType.None, Price = 0, Qty = 0, Memo = actionMemo };
            }

            int pricePoint =
                (candidateAction == ActionType.Long || candidateAction == ActionType.PostYes)
                ? snapshot.BestYesBid
                : snapshot.BestNoBid;

            pathTaken.Add($"Final: {candidateAction}");

            {
                string actionMemo = BuildActionMemo();
                return new ActionDecision
                {
                    Type = candidateAction,
                    Price = pricePoint,
                    Qty = 1,
                    Memo = actionMemo
                };
            }
        }

        public override string ToJson()
        {
            var json = JsonSerializer.Serialize(new
            {
                type = "Breakout2",
                name = Name,
                weight = Weight,
                defaultAction = _defaultAction.ToString(),
                mlParams = _mlParams.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value)
            });
            return json;
        }

    }
}
