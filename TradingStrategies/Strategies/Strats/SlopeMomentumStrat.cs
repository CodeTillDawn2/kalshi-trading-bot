using SmokehouseDTOs;
using System.Globalization;
using System.Text.Json;
using static SmokehouseInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Strategies.Strats
{
    public class SlopeMomentumStrat : Strat
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
            ExitRsiDevThreshold,            // |RSI_Short - 50| <= this => flatten

            // Slope gating
            MinSlope                        // minimum slope required for entry
        }

        public SlopeMomentumStrat(
            string name = nameof(SlopeMomentumStrat),
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
                { ParamKey.ExitRsiDevThreshold, 5.6 },
                { ParamKey.MinSlope, 0.1 }
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

            double minSlope = _params[ParamKey.MinSlope];

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
            double useVD = Math.Min(baseVD, maxVD);

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

            bool longOk = (flowYes >= useVD) && (flowYes > flowNo + minDiff) && (ratioEdge >= minDiff) && confirmYes && (flowYes >= flowFloor) && (snapshot.YesBidSlopePerMinute >= minSlope);
            bool shortOk = (flowNo >= useVD) && (flowNo > flowYes + minDiff) && (ratioEdge >= minDiff) && confirmNo && (flowNo >= flowFloor) && (snapshot.NoBidSlopePerMinute >= minSlope);

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
                return AD(
                    ActionType.Exit,
                    simulationPosition > 0 ? snapshot.BestYesBid : snapshot.BestNoBid, // FIX: use No book for short
                    Math.Abs(simulationPosition),
                    Memo(inv, "exit:RSI_flat",
                         minDist, baseVD, maxVD, minDiff, minBars, flowFloor,
                         shareTRMin, shareTEMin, spikeRelMin, exitOpp, exitRsiDev, minSlope,
                         vYes, vNo, depthYes, depthNo, flowYes, flowNo, ratioEdge,
                         trShareYes, trShareNo, teShareYes, teShareNo, spikeYes, spikeNo,
                         _consecYes, _consecNo, rsiShort, rsiDelta, simulationPosition, candidate,
                         snapshot.YesBidSlopePerMinute, snapshot.NoBidSlopePerMinute)
                );
            }
            if (simulationPosition > 0 && (snapshot.YesBidSlopePerMinute <= 0))
            {
                return AD(
                    ActionType.Exit,
                    snapshot.BestYesBid,
                    Math.Abs(simulationPosition),
                    Memo(inv, "exit:slope_zero_cross_long",
                         minDist, baseVD, maxVD, minDiff, minBars, flowFloor,
                         shareTRMin, shareTEMin, spikeRelMin, exitOpp, exitRsiDev, minSlope,
                         vYes, vNo, depthYes, depthNo, flowYes, flowNo, ratioEdge,
                         trShareYes, trShareNo, teShareYes, teShareNo, spikeYes, spikeNo,
                         _consecYes, _consecNo, rsiShort, rsiDelta, simulationPosition, candidate,
                         snapshot.YesBidSlopePerMinute, snapshot.NoBidSlopePerMinute)
                );
            }
            if (simulationPosition < 0 && (snapshot.NoBidSlopePerMinute >= 0))
            {
                return AD(
                    ActionType.Exit,
                    snapshot.BestNoBid,
                    Math.Abs(simulationPosition),
                    Memo(inv, "exit:slope_zero_cross_short",
                         minDist, baseVD, maxVD, minDiff, minBars, flowFloor,
                         shareTRMin, shareTEMin, spikeRelMin, exitOpp, exitRsiDev, minSlope,
                         vYes, vNo, depthYes, depthNo, flowYes, flowNo, ratioEdge,
                         trShareYes, trShareNo, teShareYes, teShareNo, spikeYes, spikeNo,
                         _consecYes, _consecNo, rsiShort, rsiDelta, simulationPosition, candidate,
                         snapshot.YesBidSlopePerMinute, snapshot.NoBidSlopePerMinute)
                );
            }


            if (simulationPosition > 0 && candidate == ActionType.Short && Math.Abs(flowNo) < exitOpp) candidate = ActionType.None;
            if (simulationPosition < 0 && candidate == ActionType.Long && Math.Abs(flowYes) < exitOpp) candidate = ActionType.None;

            if (candidate == ActionType.None)
            {
                return AD(ActionType.None, 0, 0,
                    Memo(inv, "entry:pass",
                         minDist, baseVD, maxVD, minDiff, minBars, flowFloor,
                         shareTRMin, shareTEMin, spikeRelMin, exitOpp, exitRsiDev, minSlope,
                         vYes, vNo, depthYes, depthNo, flowYes, flowNo, ratioEdge,
                         trShareYes, trShareNo, teShareYes, teShareNo, spikeYes, spikeNo,
                         _consecYes, _consecNo, rsiShort, rsiDelta, simulationPosition, candidate,
                         snapshot.YesBidSlopePerMinute, snapshot.NoBidSlopePerMinute));
            }

            int pricePoint = (candidate == ActionType.Long) ? snapshot.BestYesBid : snapshot.BestNoBid;

            return AD(candidate, pricePoint, 1,
                Memo(inv, "entry:go",
                     minDist, baseVD, maxVD, minDiff, minBars, flowFloor,
                     shareTRMin, shareTEMin, spikeRelMin, exitOpp, exitRsiDev, minSlope,
                     vYes, vNo, depthYes, depthNo, flowYes, flowNo, ratioEdge,
                     trShareYes, trShareNo, teShareYes, teShareNo, spikeYes, spikeNo,
                     _consecYes, _consecNo, rsiShort, rsiDelta, simulationPosition, candidate,
                     snapshot.YesBidSlopePerMinute, snapshot.NoBidSlopePerMinute));
        }

        private static ActionDecision AD(ActionType t, int p, int q, string memo)
            => new ActionDecision { Type = t, Price = p, Qty = q, Memo = memo };

        private static string Memo(
            IFormatProvider inv, string tail,
            int minDist, double baseVD, double maxVD, double minDiff, int minBars, double flowFloor,
            double shareTRMin, double shareTEMin, double spikeRelMin, double exitOpp, double exitRsiDev, double minSlope,
            double vYes, double vNo, double depthYes, double depthNo, double flowYes, double flowNo, double ratioEdge,
            double trShareYes, double trShareNo, double teShareYes, double teShareNo, bool spikeYes, bool spikeNo,
            int consecY, int consecN, double rsiShort, double rsiDelta, int position, ActionType candidate, double yesSlope, double noSlope)
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

                // (8) slopes
                $"yesSlope={F(yesSlope)}","noSlope={F(noSlope)}","minSlope={F(minSlope)}",

                // (9) tail
                tail
            };

            return string.Join(",", parts);
        }

        public override string ToJson()
        {
            var dict = _params.ToDictionary(k => k.Key.ToString(), v => v.Value);
            return JsonSerializer.Serialize(new
            {
                type = "SlopeMomentumStrat",
                name = Name,
                weight = Weight,
                parameters = dict
            });
        }

        public static readonly List<(string Name, Dictionary<SlopeMomentumStrat.ParamKey, double> Parameters)>
SlopeMomentumParameterSets = new List<(string, Dictionary<SlopeMomentumStrat.ParamKey, double>)>
{
    (
        "SlopeMomentum_Default",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MinDist_Low1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 3 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MinDist_Low2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 4 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MinDist_High1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MinDist_High2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 8 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_VelDepth_Low1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.05 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_VelDepth_Low2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_VelDepth_High1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_VelDepth_High2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.12 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MaxVelThr_Low1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MaxVelThr_Low2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.15 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MaxVelThr_High1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.20 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MaxVelThr_High2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.25 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MinRatioDiff_Low1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.05 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MinRatioDiff_Low2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MinRatioDiff_High1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MinRatioDiff_High2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.15 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MinConsBars_Low1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MinConsBars_High1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MinConsBars_High2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 4 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MinSigStr_Low1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.02 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MinSigStr_Low2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.04 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MinSigStr_High1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.08 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MinSigStr_High2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.10 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_TradeRateMin_Low1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.50 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_TradeRateMin_Low2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.55 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_TradeRateMin_High1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.65 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_TradeRateMin_High2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_TradeEventMin_Low1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.30 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_TradeEventMin_Low2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.35 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_TradeEventMin_High1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.45 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_TradeEventMin_High2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_SpikeRel_Low1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.0 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_SpikeRel_Low2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.5 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_SpikeRel_High1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.0 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_SpikeRel_High2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.5 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_ExitOppSig_Low1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.08 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_ExitOppSig_Low2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_ExitOppSig_High1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.15 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_ExitOppSig_High2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.18 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_ExitRsiDev_Low1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 3.0 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_ExitRsiDev_Low2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 4.0 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_ExitRsiDev_High1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 6.5 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_ExitRsiDev_High2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 7.5 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MinSlope_Low1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, -0.1 }
        }
    ),
    (
        "SlopeMomentum_MinSlope_Low2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, -0.05 }
        }
    ),
    (
        "SlopeMomentum_MinSlope_High1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.05 }
        }
    ),
    (
        "SlopeMomentum_MinSlope_High2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.1 }
        }
    ),
    (
        "SlopeMomentum_MinSlope_High3",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.2 }
        }
    ),
    (
        "SlopeMomentum_Combo1",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.16 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.09 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.05 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.55 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.35 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.5 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 4.5 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.05 }
        }
    ),
    (
        "SlopeMomentum_Combo2",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.20 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.07 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.65 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.45 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.0 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 6.0 },
            { SlopeMomentumStrat.ParamKey.MinSlope, -0.05 }
        }
    ),
    (
        "SlopeMomentum_Combo3",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 4 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.22 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.15 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 4 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.08 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.5 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 7.0 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.1 }
        }
    ),
    (
        "SlopeMomentum_Combo4",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 8 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.14 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.04 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.50 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.30 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.0 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.08 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 4.0 },
            { SlopeMomentumStrat.ParamKey.MinSlope, -0.1 }
        }
    ),
    (
        "SlopeMomentum_Combo5",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.11 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.24 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.13 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.09 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.62 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.42 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.9 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.13 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 6.2 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.15 }
        }
    ),
    (
        "SlopeMomentum_MinDist_High3",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MinDist_High4",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 10 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_VelDepth_High3",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.14 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_VelDepth_High4",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.15 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MinRatioDiff_High3",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_MinRatioDiff_High4",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.20 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_SpikeRel_High3",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 4.0 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_ExitOppSig_High3",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.20 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_ExitRsiDev_High3",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 8.0 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.0 }
        }
    ),
    (
        "SlopeMomentum_Combo6",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 3 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.05 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.05 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.02 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.50 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.30 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.0 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.08 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 3.0 },
            { SlopeMomentumStrat.ParamKey.MinSlope, -0.1 }
        }
    ),
    (
        "SlopeMomentum_Combo7",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 10 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.15 },
            { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.25 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.20 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 4 },
            { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.10 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
            { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 4.0 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.20 },
            { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 8.0 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.2 }
        }
    ),
    (
    "SlopeMomentum_SlopeSweep_0p0",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0 } }
),
(
    "SlopeMomentum_SlopeSweep_0p01",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.01 } }
),
(
    "SlopeMomentum_SlopeSweep_0p02",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.02 } }
),
(
    "SlopeMomentum_SlopeSweep_0p03",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.03 } }
),
(
    "SlopeMomentum_SlopeSweep_0p04",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 } }
),
(
    "SlopeMomentum_SlopeSweep_0p05",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.05 } }
),
(
    "SlopeMomentum_SlopeSweep_0p06",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.06 } }
),
(
    "SlopeMomentum_SlopeSweep_0p07",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.07 } }
),
(
    "SlopeMomentum_SlopeSweep_0p08",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 } }
),
(
    "SlopeMomentum_SlopeSweep_0p09",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.09 } }
),
(
    "SlopeMomentum_SlopeSweep_0p1",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.1 } }
),
(
    "SlopeMomentum_SlopeSweep_0p11",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.11 } }
),
(
    "SlopeMomentum_SlopeSweep_0p12",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.12 } }
),
(
    "SlopeMomentum_SlopeSweep_0p13",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.13 } }
),
(
    "SlopeMomentum_SlopeSweep_0p14",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.14 } }
),
(
    "SlopeMomentum_SlopeSweep_0p15",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.15 } }
),
(
    "SlopeMomentum_SlopeSweep_0p16",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.16 } }
),
(
    "SlopeMomentum_SlopeSweep_0p17",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.17 } }
),
(
    "SlopeMomentum_SlopeSweep_0p18",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.18 } }
),
(
    "SlopeMomentum_SlopeSweep_0p19",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.19 } }
),
(
    "SlopeMomentum_SlopeSweep_0p2",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.2 } }
),
(
    "SlopeMomentum_SlopeSweep_0p21",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.21 } }
),
(
    "SlopeMomentum_SlopeSweep_0p22",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.22 } }
),
// … SNIP …
(
    "SlopeMomentum_Dist5_Slope0p05",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.05 } }
),
(
    "SlopeMomentum_Dist5_Slope0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.1 } }
),
(
    "SlopeMomentum_Dist5_Slope0p15",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.15 } }
),
(
    "SlopeMomentum_Dist5_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.2 } }
),
(
    "SlopeMomentum_Signal0p04_Slope0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.04 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.1 } }
),
(
    "SlopeMomentum_Signal0p04_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.04 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.2 } }
),
(
    "SlopeMomentum_VDR0p06_Slope0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.14 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.1 } }
),
(
    "SlopeMomentum_VDR0p06_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.14 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.2 } }
),
(
    "SlopeMomentum_Aggressive_LowSlope",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.16 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.06 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.04 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.55 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.35 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 1.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 4 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0 } }
),
(
    "SlopeMomentum_Aggressive_HighSlope",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.16 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.06 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.04 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.55 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.35 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 1.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 4 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.3 } }
),
(
    "SlopeMomentum_Conservative_LowSlope",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 10 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.12 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.24 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.16 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 4 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.10 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.5 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.18 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 8 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0 } }
),
(
    "SlopeMomentum_Conservative_HighSlope",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 10 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.12 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.24 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.16 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 4 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.10 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.5 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.18 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 8 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.3 } }
),
(
    "SlopeMomentum_Extreme_Slope0p50",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.22 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.08 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.65 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.45 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.2 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 7 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.5 } }
),
(
    "SlopeMomentum_Extreme_Slope1p00",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 8 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.12 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.26 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.18 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 4 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.75 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.55 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 4 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.20 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 8 }, { SlopeMomentumStrat.ParamKey.MinSlope, 1 } }
),
(
    "SlopeMomentum_Extreme_NegSlope0p02",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, -0.02 } }
),
(
    "SlopeMomentum_ExitRsi4p0_Bars4_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 4 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 4 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.2 } }
),
(
    "SlopeMomentum_ExitRsi5p0_Bars4_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 4 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.2 } }
),
(
    "SlopeMomentum_ExitRsi6p0_Bars4_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 4 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.2 } }
),
(
    "SlopeMomentum_ExitRsi7p0_Bars4_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 4 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 7 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.2 } }
),
(
    "SlopeMomentum_ExitRsi8p0_Bars4_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 4 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 8 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.2 } }
),
(
    "SlopeMomentum_OppFlip0p10_Slope0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.1 } }
),
(
    "SlopeMomentum_OppFlip0p12_Slope0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.1 } }
),
(
    "SlopeMomentum_OppFlip0p15_Slope0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.15 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.1 } }
),
(
    "SlopeMomentum_OppFlip0p18_Slope0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.18 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.1 } }
),
(
    "SlopeMomentum_OppFlip0p20_Slope0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double> { { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 }, { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 }, { SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 }, { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 }, { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 }, { SlopeMomentumStrat.ParamKey.MinSignalStrength, 0.06 }, { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 }, { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 }, { SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 }, { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.20 }, { SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 }, { SlopeMomentumStrat.ParamKey.MinSlope, 0.1 } }
),
// -------- SlopeSweep fill (0.23 → 0.50 by 0.01, plus 0.60, 0.80, 1.00) : 31 --------
(
    "SlopeMomentum_SlopeSweep_0p23",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.23} }
),
(
    "SlopeMomentum_SlopeSweep_0p24",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.24} }
),
(
    "SlopeMomentum_SlopeSweep_0p25",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.25} }
),
(
    "SlopeMomentum_SlopeSweep_0p26",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.26} }
),
(
    "SlopeMomentum_SlopeSweep_0p27",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.27} }
),
(
    "SlopeMomentum_SlopeSweep_0p28",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.28} }
),
(
    "SlopeMomentum_SlopeSweep_0p29",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.29} }
),
(
    "SlopeMomentum_SlopeSweep_0p30",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.30} }
),
(
    "SlopeMomentum_SlopeSweep_0p31",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.31} }
),
(
    "SlopeMomentum_SlopeSweep_0p32",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.32} }
),
(
    "SlopeMomentum_SlopeSweep_0p33",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.33} }
),
(
    "SlopeMomentum_SlopeSweep_0p34",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.34} }
),
(
    "SlopeMomentum_SlopeSweep_0p35",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.35} }
),
(
    "SlopeMomentum_SlopeSweep_0p36",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.36} }
),
(
    "SlopeMomentum_SlopeSweep_0p37",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.37} }
),
(
    "SlopeMomentum_SlopeSweep_0p38",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.38} }
),
(
    "SlopeMomentum_SlopeSweep_0p39",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.39} }
),
(
    "SlopeMomentum_SlopeSweep_0p40",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.40} }
),
(
    "SlopeMomentum_SlopeSweep_0p41",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.41} }
),
(
    "SlopeMomentum_SlopeSweep_0p42",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.42} }
),
(
    "SlopeMomentum_SlopeSweep_0p43",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.43} }
),
(
    "SlopeMomentum_SlopeSweep_0p44",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.44} }
),
(
    "SlopeMomentum_SlopeSweep_0p45",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.45} }
),
(
    "SlopeMomentum_SlopeSweep_0p46",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.46} }
),
(
    "SlopeMomentum_SlopeSweep_0p47",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.47} }
),
(
    "SlopeMomentum_SlopeSweep_0p48",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.48} }
),
(
    "SlopeMomentum_SlopeSweep_0p49",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.49} }
),
(
    "SlopeMomentum_SlopeSweep_0p50",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.50} }
),
(
    "SlopeMomentum_SlopeSweep_0p60",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.60} }
),
(
    "SlopeMomentum_SlopeSweep_0p80",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.80} }
),
(
    "SlopeMomentum_SlopeSweep_1p00",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,1.00} }
),

// -------- Distance-from-bounds grid Dist6–10 × Slope {0.05,0.10,0.15,0.20} : 20 --------
(
    "SlopeMomentum_Dist6_Slope0p05",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.05} }
),
(
    "SlopeMomentum_Dist6_Slope0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.10} }
),
(
    "SlopeMomentum_Dist6_Slope0p15",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.15} }
),
(
    "SlopeMomentum_Dist6_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.20} }
),
(
    "SlopeMomentum_Dist7_Slope0p05",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,7},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.05} }
),
(
    "SlopeMomentum_Dist7_Slope0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,7},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.10} }
),
(
    "SlopeMomentum_Dist7_Slope0p15",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,7},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.15} }
),
(
    "SlopeMomentum_Dist7_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,7},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.20} }
),
(
    "SlopeMomentum_Dist8_Slope0p05",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,8},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.05} }
),
(
    "SlopeMomentum_Dist8_Slope0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,8},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.10} }
),
(
    "SlopeMomentum_Dist8_Slope0p15",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,8},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.15} }
),
(
    "SlopeMomentum_Dist8_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,8},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.20} }
),
(
    "SlopeMomentum_Dist9_Slope0p05",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,9},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.05} }
),
(
    "SlopeMomentum_Dist9_Slope0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,9},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.10} }
),
(
    "SlopeMomentum_Dist9_Slope0p15",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,9},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.15} }
),
(
    "SlopeMomentum_Dist9_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,9},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.20} }
),
(
    "SlopeMomentum_Dist10_Slope0p05",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,10},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.05} }
),
(
    "SlopeMomentum_Dist10_Slope0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,10},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.10} }
),
(
    "SlopeMomentum_Dist10_Slope0p15",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,10},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.15} }
),
(
    "SlopeMomentum_Dist10_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,10},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.20} }
),

// -------- Signal×Slope (signal {0.06,0.08,0.10,0.12} × slope {0.10,0.20,0.30}) : 12 --------
(
    "SlopeMomentum_Signal0p06_Slope0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.10} }
),
(
    "SlopeMomentum_Signal0p06_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.20} }
),
(
    "SlopeMomentum_Signal0p06_Slope0p30",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.30} }
),
(
    "SlopeMomentum_Signal0p08_Slope0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.08},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.10} }
),
(
    "SlopeMomentum_Signal0p08_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.08},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.20} }
),
(
    "SlopeMomentum_Signal0p08_Slope0p30",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.08},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.30} }
),
(
    "SlopeMomentum_Signal0p10_Slope0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.10},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.10} }
),
(
    "SlopeMomentum_Signal0p10_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.10},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.20} }
),
(
    "SlopeMomentum_Signal0p10_Slope0p30",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.10},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.30} }
),
(
    "SlopeMomentum_Signal0p12_Slope0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.10} }
),
(
    "SlopeMomentum_Signal0p12_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.20} }
),
(
    "SlopeMomentum_Signal0p12_Slope0p30",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.30} }
),

// -------- VDR×Slope (VDR {0.08,0.10,0.12,0.14,0.16} × slope {0.10,0.20}) : 10 --------
(
    "SlopeMomentum_VDR0p08_Slope0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.16},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.10} }
),
(
    "SlopeMomentum_VDR0p08_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.16},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.20} }
),
(
    "SlopeMomentum_VDR0p10_Slope0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.10},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.10} }
),
(
    "SlopeMomentum_VDR0p10_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.10},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.20} }
),
(
    "SlopeMomentum_VDR0p12_Slope0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.12},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.20},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.10} }
),
(
    "SlopeMomentum_VDR0p12_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.12},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.20},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.20} }
),
(
    "SlopeMomentum_VDR0p14_Slope0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.14},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.22},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.10} }
),
(
    "SlopeMomentum_VDR0p14_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.14},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.22},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.20} }
),
(
    "SlopeMomentum_VDR0p16_Slope0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.16},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.24},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.10} }
),
(
    "SlopeMomentum_VDR0p16_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.16},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.24},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.20} }
),

// -------- OppFlip strength sweep @ slope 0.20 : 5 --------
(
    "SlopeMomentum_OppFlip0p10_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.10},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.20} }
),
(
    "SlopeMomentum_OppFlip0p12_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.20} }
),
(
    "SlopeMomentum_OppFlip0p15_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.15},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.20} }
),
(
    "SlopeMomentum_OppFlip0p18_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.18},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.20} }
),
(
    "SlopeMomentum_OppFlip0p20_Slope0p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.20},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.20} }
),

// -------- Additional extremes to round out coverage : 2 --------
(
    "SlopeMomentum_Extreme_Slope0p60",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,7},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.10},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.24},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.12},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,3},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.08},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.65},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.45},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,3.2},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.16},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,7},{ SlopeMomentumStrat.ParamKey.MinSlope,0.60} }
),
(
    "SlopeMomentum_Extreme_Slope0p80",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,8},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.12},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.26},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.18},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,4},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.10},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.75},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.55},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,4.0},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.20},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,8},{ SlopeMomentumStrat.ParamKey.MinSlope,0.80} }
),

// -------- Single missing Signal×Slope noted (0.04 × 0.30) : 1 --------
(
    "SlopeMomentum_Signal0p04_Slope0p30",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.04},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.30} }
),
(
    "SlopeMomentum_SlopeSweep_0p52",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.52} }
),
(
    "SlopeMomentum_SlopeSweep_0p54",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.54} }
),
(
    "SlopeMomentum_SlopeSweep_0p56",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.56} }
),
(
    "SlopeMomentum_SlopeSweep_0p58",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.58} }
),
(
    "SlopeMomentum_SlopeSweep_0p62",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.62} }
),
(
    "SlopeMomentum_SlopeSweep_0p64",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.64} }
),
(
    "SlopeMomentum_SlopeSweep_0p66",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.66} }
),
(
    "SlopeMomentum_SlopeSweep_0p68",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.68} }
),
(
    "SlopeMomentum_SlopeSweep_0p70",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.70} }
),
(
    "SlopeMomentum_SlopeSweep_0p72",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.72} }
),
(
    "SlopeMomentum_SlopeSweep_0p74",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.74} }
),
(
    "SlopeMomentum_SlopeSweep_0p76",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.76} }
),
(
    "SlopeMomentum_SlopeSweep_0p78",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.78} }
),
(
    "SlopeMomentum_SlopeSweep_0p82",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.82} }
),
(
    "SlopeMomentum_SlopeSweep_0p84",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.84} }
),
(
    "SlopeMomentum_SlopeSweep_0p86",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.86} }
),
(
    "SlopeMomentum_SlopeSweep_0p88",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.88} }
),
(
    "SlopeMomentum_SlopeSweep_0p90",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.90} }
),
(
    "SlopeMomentum_SlopeSweep_0p92",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.92} }
),
(
    "SlopeMomentum_SlopeSweep_0p94",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.94} }
),
(
    "SlopeMomentum_SlopeSweep_0p96",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,0.96} }
),
(
    "SlopeMomentum_SlopeSweep_1p02",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,1.02} }
),
(
    "SlopeMomentum_SlopeSweep_1p04",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,1.04} }
),
(
    "SlopeMomentum_SlopeSweep_1p06",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,1.06} }
),
(
    "SlopeMomentum_SlopeSweep_1p08",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,1.08} }
),
(
    "SlopeMomentum_SlopeSweep_1p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,1.10} }
),
(
    "SlopeMomentum_SlopeSweep_1p20",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,1.20} }
),
(
    "SlopeMomentum_SlopeSweep_1p30",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,1.30} }
),
(
    "SlopeMomentum_SlopeSweep_1p40",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,1.40} }
),
(
    "SlopeMomentum_SlopeSweep_1p50",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>{{ SlopeMomentumStrat.ParamKey.MinDistanceFromBounds,6},{ SlopeMomentumStrat.ParamKey.VelocityToDepthRatio,0.08},{ SlopeMomentumStrat.ParamKey.MaxVelocityThresholdRatio,0.18},{ SlopeMomentumStrat.ParamKey.MinRatioDifference,0.10},{ SlopeMomentumStrat.ParamKey.MinConsecutiveBars,2},{ SlopeMomentumStrat.ParamKey.MinSignalStrength,0.06},{ SlopeMomentumStrat.ParamKey.TradeRateShareMin,0.60},{ SlopeMomentumStrat.ParamKey.TradeEventShareMin,0.40},{ SlopeMomentumStrat.ParamKey.SpikeMinRelativeIncrease,2.8},{ SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength,0.12},{ SlopeMomentumStrat.ParamKey.ExitRsiDevThreshold,5.6},{ SlopeMomentumStrat.ParamKey.MinSlope,1.50} }
)

};
    }
}
