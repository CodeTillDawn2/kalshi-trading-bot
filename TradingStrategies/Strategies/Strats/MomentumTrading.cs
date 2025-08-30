using SmokehouseDTOs;
using System.Globalization;
using System.Text.Json;
using static SmokehouseInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Strategies.Strats
{
    // MomentumTrading — breakout-style momentum with optional top-decile overweighting.
    public class MomentumTrading : Strat
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

            // Overweight top 10% velocity
            TopDecileWeightFactor
        }

        public MomentumTrading(
            string name = "MomentumTrading",
            double weight = 1.0,
            ActionType defaultAction = ActionType.None,
            Dictionary<ParamKey, double>? mlParams = null
        )
        {
            Name = name;
            Weight = weight;
            _defaultAction = defaultAction;

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

                // new knob
                { ParamKey.TopDecileWeightFactor, 2.0 }
            };
        }

        public override ActionDecision GetAction(MarketSnapshot snapshot, MarketSnapshot? previousSnapshot, int simulationPosition = 0)
        {
            if (snapshot == null || !snapshot.ChangeMetricsMature)
                return new ActionDecision { Type = ActionType.None, Price = 0, Qty = 0, Memo = "not_mature" };

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

            // params
            double absorptionThreshold = _mlParams.GetValueOrDefault(ParamKey.AbsorptionThreshold, 10.0);
            double minSignalStrength = _mlParams.GetValueOrDefault(ParamKey.MinSignalStrength, 2.0);
            double baseThrRatio = _mlParams.GetValueOrDefault(ParamKey.VelocityToDepthRatio, 0.10);
            int minPtsFromResolved = (int)Math.Round(_mlParams.GetValueOrDefault(ParamKey.MinNumberOfPointsFromResolved, 5.0));
            double maxVolumeRatio = _mlParams.GetValueOrDefault(ParamKey.MaxVolumeRatio, 6.0);
            double minRatioDiff = _mlParams.GetValueOrDefault(ParamKey.MinRatioDifference, 0.05);
            double reversalExtraStrength = _mlParams.GetValueOrDefault(ParamKey.ReversalExtraStrength, 1.0);

            double spikeRelThr = _mlParams.GetValueOrDefault(ParamKey.SpikeMinRelativeIncrease, 2.0);
            double spikeWeightScale = _mlParams.GetValueOrDefault(ParamKey.SpikeWeightScale, 1.0);
            double spikeWeightCap = _mlParams.GetValueOrDefault(ParamKey.SpikeWeightCap, 5.0);
            double spikeVolScale = _mlParams.GetValueOrDefault(ParamKey.SpikeVolumeWeightScale, 0.5);

            double trShareMin = _mlParams.GetValueOrDefault(ParamKey.TradeRateShareMin, 0.55);
            double teShareMin = _mlParams.GetValueOrDefault(ParamKey.TradeEventShareMin, 0.35);

            double topDecileW = _mlParams.GetValueOrDefault(ParamKey.TopDecileWeightFactor, 2.0);

            // velocities (existing fields only) + overweight top decile
            double vTopYes = snapshot.VelocityPerMinute_Top_Yes_Bid;
            double vBotYes = snapshot.VelocityPerMinute_Bottom_Yes_Bid;
            double vTopNo = snapshot.VelocityPerMinute_Top_No_Bid;
            double vBotNo = snapshot.VelocityPerMinute_Bottom_No_Bid;

            double vSumYes = (topDecileW * vTopYes) + vBotYes;
            double vSumNo = (topDecileW * vTopNo) + vBotNo;

            // prev sums (no new fields)
            double prevSumYes = 0.0, prevSumNo = 0.0;
            if (previousSnapshot != null)
            {
                prevSumYes = previousSnapshot.VelocityPerMinute_Top_Yes_Bid + previousSnapshot.VelocityPerMinute_Bottom_Yes_Bid;
                prevSumNo = previousSnapshot.VelocityPerMinute_Top_No_Bid + previousSnapshot.VelocityPerMinute_Bottom_No_Bid;
            }

            // depths (cents*contracts -> dollars)
            double depthYes = snapshot.TotalOrderbookDepth_Yes / 100.0;
            double depthNo = snapshot.TotalOrderbookDepth_No / 100.0;

            // thresholds
            const double MAX_VEL_THR_FIXED = 0.35;
            double effRatio = Math.Min(baseThrRatio, MAX_VEL_THR_FIXED);
            double thrYes = depthYes * effRatio;
            double thrNo = depthNo * effRatio;

            // flow proxies
            double flowYes = vSumYes / Math.Max(depthYes, 1e-9);
            double flowNo = vSumNo / Math.Max(depthNo, 1e-9);

            double depthRatioYN = depthYes / Math.Max(depthNo, 1e-9);

            // spikes
            bool spikeYes = false, spikeNo = false;
            double relIncYes = 1.0, relIncNo = 1.0;
            if (previousSnapshot != null)
            {
                double pAbsYes = Math.Abs(prevSumYes);
                double pAbsNo = Math.Abs(prevSumNo);
                double cAbsYes = Math.Abs(vSumYes);
                double cAbsNo = Math.Abs(vSumNo);
                relIncYes = (pAbsYes < 1e-12) ? (cAbsYes > 0 ? double.PositiveInfinity : 1.0) : (cAbsYes / pAbsYes);
                relIncNo = (pAbsNo < 1e-12) ? (cAbsNo > 0 ? double.PositiveInfinity : 1.0) : (cAbsNo / pAbsNo);
                spikeYes = relIncYes >= spikeRelThr;
                spikeNo = relIncNo >= spikeRelThr;
            }

            bool flipYes = previousSnapshot != null && Math.Sign(prevSumYes) != Math.Sign(vSumYes) && Math.Abs(prevSumYes) > 0 && Math.Abs(vSumYes) > 0;
            bool flipNo = previousSnapshot != null && Math.Sign(prevSumNo) != Math.Sign(vSumNo) && Math.Abs(prevSumNo) > 0 && Math.Abs(vSumNo) > 0;

            // context
            double currMinVol = snapshot.TradeVolumePerMinute_Yes + snapshot.TradeVolumePerMinute_No;
            double maxMinVol = Math.Max(snapshot.HighestVolume_Minute, 1e-9);
            double volContext = Math.Max(0.0, Math.Min(currMinVol / maxMinVol, 1.0));

            // confirmations
            double totalTR = Math.Max(snapshot.TradeRatePerMinute_Yes + snapshot.TradeRatePerMinute_No, 1e-9);
            double yesTRShare = snapshot.TradeRatePerMinute_Yes / totalTR;
            double noTRShare = snapshot.TradeRatePerMinute_No / totalTR;

            double totalEvents = (double)(snapshot.TradeCount_Yes + snapshot.TradeCount_No
                                 + snapshot.NonTradeRelatedOrderCount_Yes + snapshot.NonTradeRelatedOrderCount_No);
            double yesEventShare = totalEvents > 0 ? snapshot.TradeCount_Yes / totalEvents : 0.0;
            double noEventShare = totalEvents > 0 ? snapshot.TradeCount_No / totalEvents : 0.0;

            bool confirmYes = yesTRShare >= trShareMin && yesEventShare >= teShareMin;
            bool confirmNo = noTRShare >= trShareMin && noEventShare >= teShareMin;

            // base signals
            bool yesBreak = vSumYes >= thrYes
                            && (snapshot.BestYesAsk <= 100 - minPtsFromResolved)
                            && Math.Abs(depthRatioYN) < maxVolumeRatio
                            && (flowYes > Math.Abs(flowNo) + minRatioDiff);

            bool noBreak = vSumNo >= thrNo
                            && (snapshot.BestYesBid >= minPtsFromResolved)
                            && Math.Abs(depthRatioYN) < maxVolumeRatio
                            && (flowNo > Math.Abs(flowYes) + minRatioDiff);

            // decision
            ActionType candidateAction = ActionType.None;
            double signalStrength = 0.0;

            if (yesBreak && confirmYes)
            {
                candidateAction = ActionType.Long;
                signalStrength += (thrYes > 0) ? (vSumYes - thrYes) / thrYes : 0.0;
                pathTaken.Add("YesBreak+Confirm");
            }

            double scaledVNo = -vSumNo * Math.Min(Math.Max(depthYes / Math.Max(depthNo, 1e-9), 0.5), 2.0);
            if (vSumNo < -thrNo &&
                scaledVNo > thrYes &&
                snapshot.BestYesAsk <= 100 - minPtsFromResolved &&
                Math.Abs(depthRatioYN) < maxVolumeRatio &&
                -flowNo > Math.Abs(flowYes) + minRatioDiff &&
                flowYes >= 0 &&
                confirmYes)
            {
                if (candidateAction == ActionType.Long || candidateAction == ActionType.None)
                {
                    candidateAction = ActionType.Long;
                    signalStrength += 2.0;
                    pathTaken.Add("Long: No weakness");
                }
            }

            if (noBreak && confirmNo)
            {
                candidateAction = ActionType.Short;
                signalStrength += (thrNo > 0) ? (vSumNo - thrNo) / thrNo : 0.0;
                pathTaken.Add("NoBreak+Confirm");
            }

            double scaledVYes = -vSumYes * Math.Min(Math.Max(depthNo / Math.Max(depthYes, 1e-9), 0.5), 2.0);
            if (vSumYes < -thrYes &&
                scaledVYes > thrNo &&
                snapshot.BestYesBid >= minPtsFromResolved &&
                Math.Abs(depthRatioYN) < maxVolumeRatio &&
                -flowYes > Math.Abs(flowNo) + minRatioDiff &&
                flowNo >= 0 &&
                confirmNo)
            {
                if (candidateAction == ActionType.Short || candidateAction == ActionType.None)
                {
                    candidateAction = ActionType.Short;
                    signalStrength += 2.0;
                    pathTaken.Add("Short: Yes weakness");
                }
            }

            // spike-weighted boosts
            double SpikeWeighted(double relInc)
            {
                double baseW = double.IsInfinity(relInc) ? spikeWeightCap : (relInc / Math.Max(spikeRelThr, 1e-9));
                double volBoost = 1.0 + (spikeVolScale * volContext);
                return Math.Min(baseW * spikeWeightScale * volBoost, spikeWeightCap);
            }
            if (spikeYes && confirmYes)
            {
                if (vSumYes > 0 && (candidateAction == ActionType.Long || candidateAction == ActionType.None))
                {
                    candidateAction = ActionType.Long;
                    signalStrength += SpikeWeighted(relIncYes);
                    pathTaken.Add("Spike: Yes+");
                }
                else if (vSumYes < 0 && (candidateAction == ActionType.Short || candidateAction == ActionType.None))
                {
                    candidateAction = ActionType.Short;
                    signalStrength += SpikeWeighted(relIncYes);
                    pathTaken.Add("Spike: Yes-");
                }
            }
            if (spikeNo && confirmNo)
            {
                if (vSumNo > 0 && (candidateAction == ActionType.Short || candidateAction == ActionType.None))
                {
                    candidateAction = ActionType.Short;
                    signalStrength += SpikeWeighted(relIncNo);
                    pathTaken.Add("Spike: No+");
                }
                else if (vSumNo < 0 && (candidateAction == ActionType.Long || candidateAction == ActionType.None))
                {
                    candidateAction = ActionType.Long;
                    signalStrength += SpikeWeighted(relIncNo);
                    pathTaken.Add("Spike: No-");
                }
            }

            // MACD alignment bonus
            double macdMedHist = snapshot.MACD_Medium.Histogram ?? 0.0;
            if (macdMedHist > 0 && candidateAction == ActionType.Long) { signalStrength += 0.5; pathTaken.Add("MACD:+"); }
            if (macdMedHist < 0 && candidateAction == ActionType.Short) { signalStrength += 0.5; pathTaken.Add("MACD:-"); }

            // absorption timing
            double yesRemovalRate = -Math.Min(vSumYes, 0.0);
            double noRemovalRate = -Math.Min(vSumNo, 0.0);
            double absorbYes = yesRemovalRate > 0 ? depthYes / yesRemovalRate : double.PositiveInfinity;
            double absorbNo = noRemovalRate > 0 ? depthNo / noRemovalRate : double.PositiveInfinity;

            if (candidateAction == ActionType.Long && absorbNo < absorptionThreshold) { signalStrength += 0.5 * reversalExtraStrength; pathTaken.Add("Absorb:No<thr"); }
            if (candidateAction == ActionType.Short && absorbYes < absorptionThreshold) { signalStrength += 0.5 * reversalExtraStrength; pathTaken.Add("Absorb:Yes<thr"); }

            // ---- memo ----
            string BuildActionMemo(ActionType cand)
            {
                string Pair(string a, string b) => $"{a} | {b}";
                var lines = new List<string>
        {
            Pair($"Action: {cand.ToString()}", ","),
            Pair($"Market: {snapshot.MarketTicker}", ","),
            Pair($"Time: {snapshot.Timestamp}", ","),
            Pair($"Best Yes Bid: {F(snapshot.BestYesBid)}", $"Best Yes Ask: {F(snapshot.BestYesAsk)}"),
            Pair($"Path: {(pathTaken.Count==0 ? "none" : string.Join(" > ", pathTaken))}", $"SimPos: {I(simulationPosition)}"),

            Pair($"Depth$ Yes: {F(depthYes)}", $"Depth$ No: {F(depthNo)}"),
            Pair($"Vel/min Yes: {F(vSumYes)}", $"Vel/min No: {F(vSumNo)}"),
            Pair($"Thr Yes: {F(thrYes)}", $"Thr No: {F(thrNo)}"),
            Pair($"Top 10% Yes: {F(snapshot.VelocityPerMinute_Top_Yes_Bid)}", $"No: {F(snapshot.VelocityPerMinute_Top_No_Bid)}"),
            Pair($"Bottom 90% Yes: {F(snapshot.VelocityPerMinute_Bottom_Yes_Bid)}", $"No: {F(snapshot.VelocityPerMinute_Bottom_No_Bid)}"),
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

            Pair($"Depth ratio Y/N: {F(depthRatioYN)}", $"Signal strength: {F(signalStrength)}"),
            Pair($"Highest vol(1m): {F(snapshot.HighestVolume_Minute)}", $"Curr vol(1m): {F(currMinVol)}"),
            Pair($"Vol context 0-1: {F(volContext)}", $"MACD Med hist: {F(macdMedHist)}"),

            Pair($"Avg size Yes: {F(snapshot.AverageTradeSize_Yes)}", $"Avg size No: {F(snapshot.AverageTradeSize_No)}"),
            Pair($"Trades Yes: {I(snapshot.TradeCount_Yes)}", $"Trades No: {I(snapshot.TradeCount_No)}"),
            Pair($"Non-trade Yes: {I(snapshot.NonTradeRelatedOrderCount_Yes)}", $"Non-trade No: {I(snapshot.NonTradeRelatedOrderCount_No)}")
        };
                return string.Join(Environment.NewLine, lines);
            }

            // -------- Resolved proximity gating (centralized) --------
            bool nearResLong = snapshot.BestYesAsk > 100 - minPtsFromResolved; // cents
            bool nearResShort = snapshot.BestYesBid < minPtsFromResolved;      // cents

            if (candidateAction == ActionType.Long && nearResLong)
            {
                pathTaken.Add("Gate NearResolved L");
                return new ActionDecision { Type = ActionType.None, Price = 0, Qty = 0, Memo = BuildActionMemo(ActionType.None) };
            }
            if (candidateAction == ActionType.Short && nearResShort)
            {
                pathTaken.Add("Gate NearResolved S");
                return new ActionDecision { Type = ActionType.None, Price = 0, Qty = 0, Memo = BuildActionMemo(ActionType.None) };
            }
            // ---------------------------------------------------------

            // reversal gating only (no special exits)
            if (simulationPosition > 0 && candidateAction == ActionType.Short && signalStrength < (minSignalStrength + reversalExtraStrength))
            {
                pathTaken.Add("Hold weak flip L->S");
                return new ActionDecision { Type = ActionType.None, Price = 0, Qty = 0, Memo = BuildActionMemo(ActionType.None) };
            }
            if (simulationPosition < 0 && candidateAction == ActionType.Long && signalStrength < (minSignalStrength + reversalExtraStrength))
            {
                pathTaken.Add("Hold weak flip S->L");
                return new ActionDecision { Type = ActionType.None, Price = 0, Qty = 0, Memo = BuildActionMemo(ActionType.None) };
            }

            if (candidateAction == ActionType.None || signalStrength < minSignalStrength)
            {
                pathTaken.Add(candidateAction == ActionType.None ? "Gate None" : "Gate Weak");
                return new ActionDecision { Type = ActionType.None, Price = 0, Qty = 0, Memo = BuildActionMemo(ActionType.None) };
            }

            int px = (candidateAction == ActionType.Long) ? snapshot.BestYesBid : snapshot.BestNoBid;
            pathTaken.Add("Final " + candidateAction.ToString());
            return new ActionDecision { Type = candidateAction, Price = px, Qty = 1, Memo = BuildActionMemo(candidateAction) };
        }


        public override string ToJson()
        {
            return JsonSerializer.Serialize(new
            {
                type = "Momentum",
                name = Name,
                weight = Weight,
                defaultAction = _defaultAction.ToString(),
                mlParams = _mlParams.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value)
            });
        }


        public static MomentumTrading FromJson(string json)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if ((string)data["type"] != "Momentum")
                throw new ArgumentException("Invalid strategy type");

            var name = (string)data["name"];
            var weight = Convert.ToDouble(data["weight"]);
            var mlParamsJson = (JsonElement)data["mlParams"];
            var mlParams = mlParamsJson.Deserialize<Dictionary<string, double>>()
                .ToDictionary(kv => Enum.Parse<ParamKey>(kv.Key), kv => kv.Value);

            return new MomentumTrading(name, weight, ActionType.None, mlParams);
        }
    }
}
