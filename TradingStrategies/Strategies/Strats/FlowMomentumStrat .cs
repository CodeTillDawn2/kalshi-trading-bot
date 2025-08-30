// FlowMomentumStrat.cs (fixed gating; realistic flow thresholds; detailed memo)
using SmokehouseDTOs;
using System.Globalization;
using System.Text.Json;
using static SmokehouseInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Strategies.Strats
{
    public class FlowMomentumStrat : Strat
    {
        public string Name { get; private set; }
        public override double Weight { get; }

        private readonly Dictionary<ParamKey, double> _params;
        private int _consecYes = 0;
        private int _consecNo = 0;

        public enum ParamKey
        {
            // Entry gating (symmetric)
            MinDistanceFromBounds,          // ticks away from 0/100
            VelocityToDepthRatio,           // base flow threshold (on normalized flow = v/depth)
            MaxVelocityThresholdRatio,      // clamp (safety; usually same or slightly higher than base)
            MinRatioDifference,             // min |flowYes - flowNo|
            MinConsecutiveBars,             // sustain 2–4 bars
            MinSignalStrength,              // additional flow floor (normalized), e.g., 0.02–0.20

            // Confirmations (shares among trades)
            TradeRateShareMin,              // min share of trade *rate* for chosen side
            TradeEventShareMin,             // min share of trade *events* for chosen side

            // Spikes (context)
            SpikeMinRelativeIncrease,       // vNow >= X * vPrev

            // Exits
            ExitOppositeSignalStrength,     // min opposite flow to allow a flip
            ExitRsiDevThreshold             // |RSI_Short - 50| <= this => flatten
        }

        public FlowMomentumStrat(
            string name = nameof(FlowMomentumStrat),
            double weight = 1.0,
            Dictionary<ParamKey, double> mlParams = null)
        {
            Name = name;
            Weight = weight;
            _params = mlParams ?? new()
            {
                { ParamKey.MinDistanceFromBounds, 6 },
                { ParamKey.VelocityToDepthRatio, 0.08 },
                { ParamKey.MaxVelocityThresholdRatio, 0.18 },
                { ParamKey.MinRatioDifference, 0.10 },
                { ParamKey.MinConsecutiveBars, 2 },
                { ParamKey.MinSignalStrength, 0.06 },

                { ParamKey.TradeRateShareMin, 0.60 },
                { ParamKey.TradeEventShareMin, 0.40 },

                { ParamKey.SpikeMinRelativeIncrease, 2.8 },

                { ParamKey.ExitOppositeSignalStrength, 0.12 },
                { ParamKey.ExitRsiDevThreshold, 5.6 }
            };
        }

        public override ActionDecision GetAction(MarketSnapshot snapshot, MarketSnapshot? prev, int simulationPosition = 0)
        {
            if (!snapshot.ChangeMetricsMature)
                return AD(ActionType.None, 0, 0, "warmup:not_mature");

            var inv = CultureInfo.InvariantCulture;

            // ---- knobs
            int minDist = (int)Math.Round(_params[ParamKey.MinDistanceFromBounds]);
            double baseVD = _params[ParamKey.VelocityToDepthRatio];
            double maxVD = _params[ParamKey.MaxVelocityThresholdRatio];
            double minDiff = _params[ParamKey.MinRatioDifference];
            int minBars = (int)Math.Round(_params[ParamKey.MinConsecutiveBars]);
            double flowFloor = _params[ParamKey.MinSignalStrength];

            double shareTRMin = _params[ParamKey.TradeRateShareMin];
            double shareTEMin = _params[ParamKey.TradeEventShareMin];

            double spikeRelMin = _params[ParamKey.SpikeMinRelativeIncrease];

            double exitOpp = _params[ParamKey.ExitOppositeSignalStrength];
            double exitRsiDev = _params[ParamKey.ExitRsiDevThreshold];

            // ---- static gates: stay away from 0/100 edges
            if (snapshot.BestYesAsk >= 100 - minDist || snapshot.BestYesBid <= minDist)
                return AD(ActionType.None, 0, 0, "gate:near_boundary");

            if (prev is null)
                return AD(ActionType.None, 0, 0, "warmup:no_prev");

            // ---- flows (per-minute velocities you have)
            double vYes = snapshot.VelocityPerMinute_Top_Yes_Bid + snapshot.VelocityPerMinute_Bottom_Yes_Bid;
            double vNo = snapshot.VelocityPerMinute_Top_No_Bid + snapshot.VelocityPerMinute_Bottom_No_Bid;

            // ---- depth (scale to avoid huge ratios; avoid zero)
            double depthYes = Math.Max(snapshot.TotalOrderbookDepth_Yes / 100.0, 1e-9);
            double depthNo = Math.Max(snapshot.TotalOrderbookDepth_No / 100.0, 1e-9);

            // normalized flow (THIS is what we gate on)
            double flowYes = vYes / depthYes;
            double flowNo = vNo / depthNo;

            // clamp the threshold once
            double useVD = Math.Min(baseVD, Math.Max(baseVD, 0.0) + (maxVD - baseVD)); // effectively: useVD = Math.Min(baseVD, maxVD)
            useVD = Math.Min(baseVD, maxVD);

            // edge
            double ratioEdge = Math.Abs(flowYes - flowNo);

            // ---- confirmations (TRATE share from rates; TE share from trade counts ONLY)
            double totalRate = Math.Max(snapshot.TradeRatePerMinute_Yes + snapshot.TradeRatePerMinute_No, 1e-9);
            double trShareYes = snapshot.TradeRatePerMinute_Yes / totalRate;
            double trShareNo = snapshot.TradeRatePerMinute_No / totalRate;

            double totalTrades = Math.Max((double)(snapshot.TradeCount_Yes + snapshot.TradeCount_No), 1e-9);
            double teShareYes = snapshot.TradeCount_Yes / totalTrades;
            double teShareNo = snapshot.TradeCount_No / totalTrades;

            bool confirmYes = trShareYes >= shareTRMin && teShareYes >= shareTEMin;
            bool confirmNo = trShareNo >= shareTRMin && teShareNo >= shareTEMin;

            // ---- spikes (relative)
            double pvYes = prev.VelocityPerMinute_Top_Yes_Bid + prev.VelocityPerMinute_Bottom_Yes_Bid;
            double pvNo = prev.VelocityPerMinute_Top_No_Bid + prev.VelocityPerMinute_Bottom_No_Bid;
            bool spikeYes = pvYes > 0 && (vYes / Math.Max(pvYes, 1e-9)) >= spikeRelMin;
            bool spikeNo = pvNo > 0 && (vNo / Math.Max(pvNo, 1e-9)) >= spikeRelMin;

            // ---- sustained momentum candidate (single consistent gating)
            ActionType candidate = ActionType.None;

            bool longOk = (flowYes >= useVD) && (flowYes > flowNo + minDiff) && (ratioEdge >= minDiff) && confirmYes && (flowYes >= flowFloor);
            bool shortOk = (flowNo >= useVD) && (flowNo > flowYes + minDiff) && (ratioEdge >= minDiff) && confirmNo && (flowNo >= flowFloor);

            if (longOk)
            {
                _consecYes++; _consecNo = 0;
                if (_consecYes >= minBars) candidate = ActionType.Long;
            }
            else if (shortOk)
            {
                _consecNo++; _consecYes = 0;
                if (_consecNo >= minBars) candidate = ActionType.Short;
            }
            else
            {
                _consecYes = 0; _consecNo = 0;
            }

            // ---- exits (flatten on stall, flip only if opposite strong)
            double rsiShort = snapshot.RSI_Short ?? 50.0;
            double prevRsi = prev.RSI_Short ?? 50.0;
            double rsiDelta = rsiShort - prevRsi;

            if (simulationPosition != 0 && Math.Abs(rsiShort - 50.0) <= exitRsiDev)
            {
                return AD(ActionType.Exit,
                          simulationPosition > 0 ? snapshot.BestYesBid : snapshot.BestYesAsk,
                          Math.Abs(simulationPosition),
                          Memo(inv, "exit:RSI_flat",
                               minDist, baseVD, maxVD, minDiff, minBars, flowFloor,
                               shareTRMin, shareTEMin, spikeRelMin, exitOpp, exitRsiDev,
                               vYes, vNo, depthYes, depthNo, flowYes, flowNo, ratioEdge,
                               trShareYes, trShareNo, teShareYes, teShareNo, spikeYes, spikeNo,
                               _consecYes, _consecNo, rsiShort, rsiDelta, simulationPosition, candidate));
            }

            if (simulationPosition > 0 && candidate == ActionType.Short && Math.Abs(flowNo) < exitOpp) candidate = ActionType.None;
            if (simulationPosition < 0 && candidate == ActionType.Long && Math.Abs(flowYes) < exitOpp) candidate = ActionType.None;

            if (candidate == ActionType.None)
            {
                return AD(ActionType.None, 0, 0,
                    Memo(inv, "entry:pass",
                         minDist, baseVD, maxVD, minDiff, minBars, flowFloor,
                         shareTRMin, shareTEMin, spikeRelMin, exitOpp, exitRsiDev,
                         vYes, vNo, depthYes, depthNo, flowYes, flowNo, ratioEdge,
                         trShareYes, trShareNo, teShareYes, teShareNo, spikeYes, spikeNo,
                         _consecYes, _consecNo, rsiShort, rsiDelta, simulationPosition, candidate));
            }

            int pricePoint = (candidate == ActionType.Long) ? snapshot.BestYesBid : snapshot.BestNoBid;

            return AD(candidate, pricePoint, 1,
                Memo(inv, "entry:go",
                     minDist, baseVD, maxVD, minDiff, minBars, flowFloor,
                     shareTRMin, shareTEMin, spikeRelMin, exitOpp, exitRsiDev,
                     vYes, vNo, depthYes, depthNo, flowYes, flowNo, ratioEdge,
                     trShareYes, trShareNo, teShareYes, teShareNo, spikeYes, spikeNo,
                     _consecYes, _consecNo, rsiShort, rsiDelta, simulationPosition, candidate));
        }

        private static ActionDecision AD(ActionType t, int p, int q, string memo)
            => new ActionDecision { Type = t, Price = p, Qty = q, Memo = memo };

        private static string Memo(
            IFormatProvider inv, string tail,
            int minDist, double baseVD, double maxVD, double minDiff, int minBars, double flowFloor,
            double shareTRMin, double shareTEMin, double spikeRelMin, double exitOpp, double exitRsiDev,
            double vYes, double vNo, double depthYes, double depthNo, double flowYes, double flowNo, double ratioEdge,
            double trShareYes, double trShareNo, double teShareYes, double teShareNo, bool spikeYes, bool spikeNo,
            int consecY, int consecN, double rsiShort, double rsiDelta, int position, ActionType candidate)
        {
            string F(double d) => d.ToString("0.###", inv);

            var parts = new List<string>
            {
                // (1) gates
                $"gate:minDist={minDist}",

                // (2) normalized flow
                $"vYes={F(vYes)}","vNo={F(vNo)}",
                $"depthYes={F(depthYes)}","depthNo={F(depthNo)}",
                $"flowYes={F(flowYes)}","flowNo={F(flowNo)}",
                $"flowThr(base)={F(baseVD)}","flowThr(max)={F(maxVD)}",
                $"ratioEdge={F(ratioEdge)}>=minDiff:{F(minDiff)}",

                // (3) confirmations
                $"TRshareYes={F(trShareYes)}>=min:{F(shareTRMin)}",
                $"TRshareNo={F(trShareNo)}>=min:{F(shareTRMin)}",
                $"TEshareYes={F(teShareYes)}>=min:{F(shareTEMin)}",
                $"TEshareNo={F(teShareNo)}>=min:{F(shareTEMin)}",

                // (4) spike context
                $"spikeY={(spikeYes ? "Yes" : "No")}",
                $"spikeN={(spikeNo  ? "Yes" : "No")}",
                $"spikeRelMin={F(spikeRelMin)}",

                // (5) sustain
                $"barsY={consecY}","barsN={consecN}","minBars={minBars}",
                $"flowFloor={F(flowFloor)}",

                // (6) exits
                $"rsi={F(rsiShort)}","rsiDelta={F(rsiDelta)}","rsiDevThr={F(exitRsiDev)}","oppFlipThr={F(exitOpp)}",

                // (7) pos + candidate
                $"pos={position}","candidate={candidate}",

                // (8) tail
                tail
            };

            return string.Join(",", parts);
        }

        public override string ToJson()
        {
            var dict = _params.ToDictionary(k => k.Key.ToString(), v => v.Value);
            return JsonSerializer.Serialize(new
            {
                type = "FlowMomentumStrat",
                name = Name,
                weight = Weight,
                parameters = dict
            });
        }
    }
}
