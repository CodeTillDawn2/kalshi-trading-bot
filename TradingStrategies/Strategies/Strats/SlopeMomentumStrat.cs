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
            // Entry gating
            MinDistanceFromBounds,          // ticks away from 0/100
            VelocityToDepthRatio,           // normalized flow threshold (v/depth)
            MinRatioDifference,             // min |flowYes - flowNo|
            MinConsecutiveBars,             // sustain N bars

            // Confirmations
            TradeRateShareMin,              // share of trade rate (chosen side)
            TradeEventShareMin,             // share of trade events (chosen side)

            // Exits / slope
            ExitOppositeSignalStrength,     // min opposite flow needed to allow flip
            MinSlope,                       // min slope required for entry
            ExitMinSlopeRequirement         // min slope to keep holding (symmetric)
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
                { ParamKey.MinRatioDifference, 0.10 },
                { ParamKey.MinConsecutiveBars, 2 },

                { ParamKey.TradeRateShareMin, 0.60 },
                { ParamKey.TradeEventShareMin, 0.40 },

                { ParamKey.ExitOppositeSignalStrength, 0.12 },
                { ParamKey.MinSlope, 0.10 },
                { ParamKey.ExitMinSlopeRequirement, 0.02 }
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
            double minDiff = _params[ParamKey.MinRatioDifference];
            int minBars = (int)Math.Round(_params[ParamKey.MinConsecutiveBars]);

            double shareTRMin = _params[ParamKey.TradeRateShareMin];
            double shareTEMin = _params[ParamKey.TradeEventShareMin];

            double exitOpp = _params[ParamKey.ExitOppositeSignalStrength];
            double minSlope = _params[ParamKey.MinSlope];
            double exitMinSlope = _params[ParamKey.ExitMinSlopeRequirement];

            // ---- static gates: stay away from 0/100 edges
            if (snapshot.BestYesAsk >= 100 - minDist || snapshot.BestYesBid <= minDist)
                return AD(ActionType.None, 0, 0, "gate:near_boundary");

            if (prev is null)
                return AD(ActionType.None, 0, 0, "warmup:no_prev");

            // ---- flows (per-minute velocities you have)
            double vYes = snapshot.VelocityPerMinute_Top_Yes_Bid + snapshot.VelocityPerMinute_Bottom_Yes_Bid;
            double vNo = snapshot.VelocityPerMinute_Top_No_Bid  + snapshot.VelocityPerMinute_Bottom_No_Bid;

            // ---- depth (scale to avoid huge ratios; avoid zero)
            double depthYes = Math.Max(snapshot.TotalOrderbookDepth_Yes / 100.0, 1e-9);
            double depthNo = Math.Max(snapshot.TotalOrderbookDepth_No  / 100.0, 1e-9);

            // ---- normalized flows
            double flowYes = vYes / depthYes;
            double flowNo = vNo  / depthNo;

            // ---- “edge versus other side”
            double ratioEdge = Math.Abs(flowYes - flowNo);

            double totalRate = Math.Max(snapshot.TradeRatePerMinute_Yes + snapshot.TradeRatePerMinute_No, 1e-9);
            double trShareYes = snapshot.TradeRatePerMinute_Yes / totalRate;
            double trShareNo = snapshot.TradeRatePerMinute_No  / totalRate;

            double totalTrades = Math.Max((double)(snapshot.TradeCount_Yes + snapshot.TradeCount_No), 1e-9);
            double teShareYes = snapshot.TradeCount_Yes / totalTrades;
            double teShareNo = snapshot.TradeCount_No  / totalTrades;

            // ---- slope
            double yesSlope = snapshot.YesBidSlopePerMinute;
            double noSlope = snapshot.NoBidSlopePerMinute;

            // ---- consecutive bars state
            bool yesPassThisBar =
                (flowYes >= baseVD) &&
                (ratioEdge >= minDiff) &&
                (trShareYes >= shareTRMin) &&
                (teShareYes >= shareTEMin) &&
                (yesSlope >= minSlope);

            bool noPassThisBar =
                (flowNo >= baseVD) &&
                (ratioEdge >= minDiff) &&
                (trShareNo >= shareTRMin) &&
                (teShareNo >= shareTEMin) &&
                (noSlope <= -minSlope);

            _consecYes = yesPassThisBar ? (_consecYes + 1) : 0;
            _consecNo  = noPassThisBar ? (_consecNo  + 1) : 0;

            // ---- entry candidate
            ActionType candidate = ActionType.None;
            if (_consecYes >= minBars && _consecNo == 0) candidate = ActionType.Long;
            if (_consecNo  >= minBars && _consecYes == 0) candidate = ActionType.Short;

            // ---- slope-based exits while holding
            if (simulationPosition > 0 && yesSlope < exitMinSlope)
            {
                return AD(
                    ActionType.Exit,
                    snapshot.BestYesBid,
                    Math.Abs(simulationPosition),
                    Memo(inv, "exit:slope_below_threshold_long",
                         minDist, baseVD, minDiff, minBars,
                         shareTRMin, shareTEMin, exitOpp, minSlope, exitMinSlope,
                         vYes, vNo, depthYes, depthNo, flowYes, flowNo, ratioEdge,
                         trShareYes, trShareNo, teShareYes, teShareNo,
                         _consecYes, _consecNo, simulationPosition, candidate,
                         yesSlope, noSlope)
                );
            }
            if (simulationPosition < 0 && noSlope > -exitMinSlope)
            {
                return AD(
                    ActionType.Exit,
                    snapshot.BestNoBid,
                    Math.Abs(simulationPosition),
                    Memo(inv, "exit:slope_above_threshold_short",
                         minDist, baseVD, minDiff, minBars,
                         shareTRMin, shareTEMin, exitOpp, minSlope, exitMinSlope,
                         vYes, vNo, depthYes, depthNo, flowYes, flowNo, ratioEdge,
                         trShareYes, trShareNo, teShareYes, teShareNo,
                         _consecYes, _consecNo, simulationPosition, candidate,
                         yesSlope, noSlope)
                );
            }

            // ---- flip safety: require opposite flow strength to flip
            if (simulationPosition > 0 && candidate == ActionType.Short && Math.Abs(flowNo)  < exitOpp) candidate = ActionType.None;
            if (simulationPosition < 0 && candidate == ActionType.Long  && Math.Abs(flowYes) < exitOpp) candidate = ActionType.None;

            if (candidate == ActionType.None)
            {
                return AD(ActionType.None, 0, 0,
                    Memo(inv, "entry:pass",
                         minDist, baseVD, minDiff, minBars,
                         shareTRMin, shareTEMin, exitOpp, minSlope, exitMinSlope,
                         vYes, vNo, depthYes, depthNo, flowYes, flowNo, ratioEdge,
                         trShareYes, trShareNo, teShareYes, teShareNo,
                         _consecYes, _consecNo, simulationPosition, candidate,
                         yesSlope, noSlope));
            }

            int pricePoint = (candidate == ActionType.Long) ? snapshot.BestYesBid : snapshot.BestNoBid;

            return AD(candidate, pricePoint, 1,
                Memo(inv, "entry:go",
                     minDist, baseVD, minDiff, minBars,
                     shareTRMin, shareTEMin, exitOpp, minSlope, exitMinSlope,
                     vYes, vNo, depthYes, depthNo, flowYes, flowNo, ratioEdge,
                     trShareYes, trShareNo, teShareYes, teShareNo,
                     _consecYes, _consecNo, simulationPosition, candidate,
                     yesSlope, noSlope));
        }

        private static ActionDecision AD(ActionType t, int p, int q, string memo)
            => new ActionDecision { Type = t, Price = p, Qty = q, Memo = memo };

        private static string Memo(
            IFormatProvider inv, string tail,
            int minDist, double baseVD, double minDiff, int minBars,
            double shareTRMin, double shareTEMin, double exitOpp, double minSlope, double exitMinSlope,
            double vYes, double vNo, double depthYes, double depthNo, double flowYes, double flowNo, double ratioEdge,
            double trShareYes, double trShareNo, double teShareYes, double teShareNo,
            int consecY, int consecN, int position, ActionType candidate,
            double yesSlope, double noSlope)
        {
            string F(double d) => d.ToString("0.###", inv);
            var parts = new List<string>
            {
                $"gate:minDist={minDist}",

                $"vYes={F(vYes)}","vNo={F(vNo)}",
                $"depthYes={F(depthYes)}","depthNo={F(depthNo)}",
                $"flowYes={F(flowYes)}","flowNo={F(flowNo)}",
                $"flowThr(base)={F(baseVD)}",
                $"ratioEdge={F(ratioEdge)}>=minDiff:{F(minDiff)}",

                $"TRshareYes={F(trShareYes)}>=min:{F(shareTRMin)}",
                $"TRshareNo={F(trShareNo)}>=min:{F(shareTRMin)}",
                $"TEshareYes={F(teShareYes)}>=min:{F(shareTEMin)}",
                $"TEshareNo={F(teShareNo)}>=min:{F(shareTEMin)}",

                $"barsY={consecY}","barsN={consecN}","minBars={minBars}",

                $"oppFlipThr={F(exitOpp)}",
                $"yesSlope={F(yesSlope)}","noSlope={F(noSlope)}",
                $"minSlope={F(minSlope)}","exitMinSlope={F(exitMinSlope)}",

                $"pos={position}","candidate={candidate}",
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
    // ---------------- G1: Balanced ----------------
    (
        "G1_Balanced_S0p00_XS0p00",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.00 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.00 }
        }
    ),
    (
        "G1_Balanced_S0p00_XS0p04",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.00 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
        }
    ),
    (
        "G1_Balanced_S0p00_XS0p08",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.00 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
        }
    ),
    (
        "G1_Balanced_S0p04_XS0p00",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.00 }
        }
    ),
    (
        "G1_Balanced_S0p04_XS0p04",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
        }
    ),
    (
        "G1_Balanced_S0p04_XS0p08",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
        }
    ),
    (
        "G1_Balanced_S0p08_XS0p00",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.00 }
        }
    ),
    (
        "G1_Balanced_S0p08_XS0p04",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
        }
    ),
    (
        "G1_Balanced_S0p08_XS0p08",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
        }
    ),
    (
        "G1_Balanced_S0p12_XS0p00",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.00 }
        }
    ),
    (
        "G1_Balanced_S0p12_XS0p04",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
        }
    ),
    (
        "G1_Balanced_S0p12_XS0p08",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
        }
    ),

    // ---------------- G2: Loose & Fast ----------------
    (
        "G2_LooseFast_S0p00_XS0p00",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 4 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.55 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.35 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.00 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.00 }
        }
    ),
    (
        "G2_LooseFast_S0p00_XS0p04",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 4 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.55 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.35 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.00 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
        }
    ),
    (
        "G2_LooseFast_S0p00_XS0p08",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 4 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.55 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.35 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.00 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
        }
    ),
    (
        "G2_LooseFast_S0p04_XS0p00",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 4 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.55 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.35 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.00 }
        }
    ),
    (
        "G2_LooseFast_S0p04_XS0p04",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 4 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.55 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.35 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
        }
    ),
    (
        "G2_LooseFast_S0p04_XS0p08",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 4 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.55 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.35 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
        }
    ),
    (
        "G2_LooseFast_S0p08_XS0p00",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 4 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.55 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.35 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.00 }
        }
    ),
    (
        "G2_LooseFast_S0p08_XS0p04",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 4 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.55 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.35 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
        }
    ),
    (
        "G2_LooseFast_S0p08_XS0p08",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 4 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.55 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.35 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
        }
    ),
    (
        "G2_LooseFast_S0p12_XS0p00",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 4 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.55 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.35 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.00 }
        }
    ),
    (
        "G2_LooseFast_S0p12_XS0p04",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 4 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.55 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.35 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
        }
    ),
    (
        "G2_LooseFast_S0p12_XS0p08",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 4 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.08 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.55 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.35 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
        }
    ),

    // ---------------- G3: Tight & Strict ----------------
    (
        "G3_TightStrict_S0p00_XS0p00",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.00 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.00 }
        }
    ),
    (
        "G3_TightStrict_S0p00_XS0p04",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.00 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
        }
    ),
    (
        "G3_TightStrict_S0p00_XS0p08",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.00 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
        }
    ),
    (
        "G3_TightStrict_S0p04_XS0p00",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.00 }
        }
    ),
    (
        "G3_TightStrict_S0p04_XS0p04",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
        }
    ),
    (
        "G3_TightStrict_S0p04_XS0p08",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
        }
    ),
    (
        "G3_TightStrict_S0p08_XS0p00",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.00 }
        }
    ),
    (
        "G3_TightStrict_S0p08_XS0p04",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
        }
    ),
    (
        "G3_TightStrict_S0p08_XS0p08",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
        }
    ),
    (
        "G3_TightStrict_S0p12_XS0p00",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.00 }
        }
    ),
    (
        "G3_TightStrict_S0p12_XS0p04",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
        }
    ),
    (
        "G3_TightStrict_S0p12_XS0p08",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.12 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
        }
    ),

    // ---------------- G4: Depth-Biased (edge-diff emphasis) ----------------
    (
        "G4_DepthBiased_S0p00_XS0p00",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.65 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.45 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.00 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.00 }
        }
    ),
    (
        "G4_DepthBiased_S0p00_XS0p04",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.65 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.45 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.00 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
        }
    ),
    (
        "G4_DepthBiased_S0p00_XS0p08",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.65 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.45 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.00 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
        }
    ),
    (
        "G4_DepthBiased_S0p04_XS0p00",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.65 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.45 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.00 }
        }
    ),
    (
        "G4_DepthBiased_S0p04_XS0p04",
        new Dictionary<SlopeMomentumStrat.ParamKey, double>
        {
            { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
            { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
            { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
            { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
            { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.65 },
            { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.45 },
            { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
            { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
            { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
        }
    ),
    // ========== 50 ADDITIONAL SETS (ready to paste into your existing SlopeMomentumParameterSets list) ==========

// ---------------- F1: EdgeScout (edge/distance emphasis, moderate flow) ----------------
(
    "EdgeScout_Lite_S0p02_XS0p04",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.62 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.42 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.02 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
    }
),
(
    "EdgeScout_Lite_S0p04_XS0p02",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.02 }
    }
),
(
    "EdgeScout_Core_S0p06_XS0p06",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 8 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.62 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.42 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.06 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.06 }
    }
),
(
    "EdgeScout_Core_S0p08_XS0p04",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 8 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.64 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.46 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
    }
),
(
    "EdgeScout_Core_S0p04_XS0p08",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 8 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.64 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.44 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
    }
),
(
    "EdgeScout_HardEdge_S0p10_XS0p06",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.66 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.48 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.10 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.06 }
    }
),
(
    "EdgeScout_HardEdge_S0p06_XS0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.66 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.48 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.18 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.06 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.10 }
    }
),
(
    "EdgeScout_Probe_S0p00_XS0p06",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.05 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.58 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.38 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.00 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.06 }
    }
),
(
    "EdgeScout_Filtered_S0p06_XS0p00",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.06 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.00 }
    }
),
(
    "EdgeScout_Filtered_S0p08_XS0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.62 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.42 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.10 }
    }
),

// ---------------- F2: FlowChaser (flow/participation emphasis, faster entries) ----------------
(
    "FlowChaser_Swift_S0p02_XS0p02",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.68 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.48 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.02 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.02 }
    }
),
(
    "FlowChaser_Swift_S0p06_XS0p02",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.06 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.02 }
    }
),
(
    "FlowChaser_Swift_S0p02_XS0p06",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.46 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.02 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.06 }
    }
),
(
    "FlowChaser_Core_S0p04_XS0p04",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.48 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
    }
),
(
    "FlowChaser_Core_S0p08_XS0p04",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.11 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.72 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
    }
),
(
    "FlowChaser_Core_S0p04_XS0p08",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.11 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.72 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
    }
),
(
    "FlowChaser_Plus_S0p10_XS0p06",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.74 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.52 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.10 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.06 }
    }
),
(
    "FlowChaser_Plus_S0p06_XS0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.74 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.52 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.06 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.10 }
    }
),
(
    "FlowChaser_Filtered_S0p00_XS0p08",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.68 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.46 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.00 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
    }
),
(
    "FlowChaser_Filtered_S0p08_XS0p00",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.68 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.46 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.00 }
    }
),

// ---------------- F3: StrictSentinel (strict confirmations, heavier exits) ----------------
(
    "StrictSentinel_Base_S0p04_XS0p06",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 10 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.72 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.52 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.06 }
    }
),
(
    "StrictSentinel_Base_S0p06_XS0p04",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 10 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.72 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.52 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.06 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
    }
),
(
    "StrictSentinel_HardExit_S0p08_XS0p08",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 4 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.74 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.54 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.18 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
    }
),
(
    "StrictSentinel_HardExit_S0p10_XS0p06",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 4 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.74 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.54 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.18 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.10 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.06 }
    }
),
(
    "StrictSentinel_SoftExit_S0p06_XS0p02",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.06 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.02 }
    }
),
(
    "StrictSentinel_SoftExit_S0p02_XS0p06",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.02 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.06 }
    }
),
(
    "StrictSentinel_Filtered_S0p12_XS0p04",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 10 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.15 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 4 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.76 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.56 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.18 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.12 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
    }
),
(
    "StrictSentinel_Filtered_S0p04_XS0p12",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 10 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.15 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 4 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.76 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.56 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.18 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.12 }
    }
),
(
    "StrictSentinel_Guard_S0p08_XS0p02",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 8 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.13 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.72 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.52 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.02 }
    }
),
(
    "StrictSentinel_Guard_S0p02_XS0p08",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 8 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.13 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.72 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.52 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.02 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
    }
),

// ---------------- F4: BounceGuard (closer to bounds, mean-revert filters) ----------------
(
    "BounceGuard_Near_S0p00_XS0p04",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 4 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.05 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.58 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.38 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.00 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
    }
),
(
    "BounceGuard_Near_S0p04_XS0p00",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 4 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.05 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.58 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.38 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.00 }
    }
),
(
    "BounceGuard_Core_S0p06_XS0p06",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.06 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.06 }
    }
),
(
    "BounceGuard_Core_S0p08_XS0p02",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.62 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.42 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.02 }
    }
),
(
    "BounceGuard_Core_S0p02_XS0p08",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.62 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.42 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.02 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
    }
),
(
    "BounceGuard_Filter_S0p04_XS0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.05 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.64 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.46 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.10 }
    }
),
(
    "BounceGuard_Filter_S0p10_XS0p04",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.05 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.64 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.46 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.10 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
    }
),
(
    "BounceGuard_Tight_S0p06_XS0p08",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.15 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.66 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.48 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.06 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
    }
),
(
    "BounceGuard_Tight_S0p08_XS0p06",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.15 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.66 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.48 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.06 }
    }
),
(
    "BounceGuard_Probe_S0p00_XS0p02",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 4 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.56 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.36 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.00 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.02 }
    }
),
(
    "BounceGuard_Probe_S0p02_XS0p00",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 4 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.56 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.36 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.02 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.00 }
    }
),

// ---------------- F5: TurboProbe (aggressive V/D, short holds) ----------------
(
    "TurboProbe_Sprint_S0p04_XS0p02",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.02 }
    }
),
(
    "TurboProbe_Sprint_S0p02_XS0p04",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.02 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
    }
),
(
    "TurboProbe_Mix_S0p06_XS0p04",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.11 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.62 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.42 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.06 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
    }
),
(
    "TurboProbe_Mix_S0p04_XS0p06",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.11 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.62 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.42 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.06 }
    }
),
(
    "TurboProbe_Core_S0p08_XS0p08",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.64 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.44 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
    }
),
(
    "TurboProbe_Core_S0p10_XS0p04",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.15 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.66 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.46 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.10 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
    }
),
(
    "TurboProbe_Core_S0p04_XS0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.15 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.66 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.46 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.10 }
    }
),
(
    "TurboProbe_Plus_S0p06_XS0p06",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.68 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.48 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.06 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.06 }
    }
),
(
    "TurboProbe_Plus_S0p12_XS0p06",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.12 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.06 }
    }
),
(
    "TurboProbe_Plus_S0p06_XS0p12",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.06 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.12 }
    }
),

// ---------------- F6: Hybrid samplers (mix of regimes to fill out grid) ----------------
(
    "Hybrid_Balanced_S0p03_XS0p03",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.11 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.63 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.43 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.03 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.03 }
    }
),
(
    "Hybrid_Balanced_S0p05_XS0p07",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.11 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.63 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.43 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.05 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.07 }
    }
),
(
    "Hybrid_Balanced_S0p07_XS0p05",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.11 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.63 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.43 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.07 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.05 }
    }
),
(
    "Hybrid_Loose_S0p01_XS0p05",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 4 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.09 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.57 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.37 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.01 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.05 }
    }
),
(
    "Hybrid_Loose_S0p05_XS0p01",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 4 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.09 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.57 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.37 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.05 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.01 }
    }
),
(
    "Hybrid_Tight_S0p06_XS0p02",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 8 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.13 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.68 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.48 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.06 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.02 }
    }
),
(
    "Hybrid_Tight_S0p02_XS0p06",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 8 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.13 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.68 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.48 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.02 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.06 }
    }
),
(
    "Hybrid_Depth_S0p08_XS0p00",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.64 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.44 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.00 }
    }
),
(
    "Hybrid_Flow_S0p00_XS0p08",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.00 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
    }
),
(
    "Hybrid_Flow_S0p10_XS0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.72 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.10 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.10 }
    }
),

// ---------------- F7: EdgeFlow Cross (explicit cross-axes variety) ----------------
(
    "EdgeFlow_X1_S0p01_XS0p03",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.09 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.61 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.41 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.01 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.03 }
    }
),
(
    "EdgeFlow_X2_S0p03_XS0p01",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.09 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.61 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.41 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.03 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.01 }
    }
),
(
    "EdgeFlow_X3_S0p05_XS0p09",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.11 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.65 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.47 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.05 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.09 }
    }
),
(
    "EdgeFlow_X4_S0p09_XS0p05",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.11 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.65 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.47 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.09 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.05 }
    }
),
(
    "EdgeFlow_X5_S0p07_XS0p07",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.67 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.49 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.07 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.07 }
    }
),
(
    "EdgeFlow_X6_S0p11_XS0p03",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.13 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.67 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.49 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.11 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.03 }
    }
),
(
    "EdgeFlow_X7_S0p03_XS0p11",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.13 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.67 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.49 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.03 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.11 }
    }
),
(
    "EdgeFlow_X8_S0p00_XS0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.00 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.10 }
    }
),
(
    "EdgeFlow_X9_S0p10_XS0p00",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.06 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.10 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.00 }
    }
),
(
    "EdgeFlow_X10_S0p12_XS0p12",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 8 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.70 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.12 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.12 }
    }
),

// ---------------- F8: Fence testers (distance sweeps w/ slope gates) ----------------
(
    "FenceTest_D5_S0p05_XS0p03",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.11 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.62 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.42 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.05 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.03 }
    }
),
(
    "FenceTest_D6_S0p03_XS0p05",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.11 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.62 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.42 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.03 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.05 }
    }
),
(
    "FenceTest_D7_S0p07_XS0p01",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.64 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.44 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.07 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.01 }
    }
),
(
    "FenceTest_D8_S0p01_XS0p07",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 8 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.64 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.44 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.01 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.07 }
    }
),
(
    "FenceTest_D9_S0p09_XS0p03",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.13 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.66 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.46 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.09 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.03 }
    }
),
(
    "FenceTest_D10_S0p03_XS0p09",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 10 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.13 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.66 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.46 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.03 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.09 }
    }
),
(
    "FenceTest_D6_S0p08_XS0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.45 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.10 }
    }
),
(
    "FenceTest_D6_S0p10_XS0p08",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.45 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.10 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
    }
),
(
    "FenceTest_D7_S0p12_XS0p06",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.11 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.68 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.12 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.06 }
    }
),
(
    "FenceTest_D7_S0p06_XS0p12",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.11 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.68 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.50 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.06 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.12 }
    }
),

// ---------------- F9: Exit gate shapers (vary the flip strength vs slope) ----------------
(
    "ExitShaper_SoftFlip_S0p04_XS0p04_Flip0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
    }
),
(
    "ExitShaper_MedFlip_S0p04_XS0p04_Flip0p14",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
    }
),
(
    "ExitShaper_HardFlip_S0p04_XS0p04_Flip0p18",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.18 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.04 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.04 }
    }
),
(
    "ExitShaper_SoftFlip_S0p08_XS0p02_Flip0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.11 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.62 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.42 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.02 }
    }
),
(
    "ExitShaper_HardFlip_S0p02_XS0p08_Flip0p18",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.11 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.62 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.42 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.18 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.02 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
    }
),
(
    "ExitShaper_MedFlip_S0p10_XS0p10_Flip0p14",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 8 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.66 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.46 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.10 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.10 }
    }
),
(
    "ExitShaper_SoftFlip_S0p06_XS0p08_Flip0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.13 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.64 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.44 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.06 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.08 }
    }
),
(
    "ExitShaper_HardFlip_S0p08_XS0p06_Flip0p18",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.13 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.64 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.44 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.18 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.08 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.06 }
    }
),
(
    "ExitShaper_SoftFlip_S0p00_XS0p06_Flip0p10",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.09 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.58 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.00 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.06 }
    }
),
(
    "ExitShaper_HardFlip_S0p06_XS0p00_Flip0p18",
    new Dictionary<SlopeMomentumStrat.ParamKey, double>
    {
        { SlopeMomentumStrat.ParamKey.MinDistanceFromBounds, 5 },
        { SlopeMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { SlopeMomentumStrat.ParamKey.MinRatioDifference, 0.09 },
        { SlopeMomentumStrat.ParamKey.MinConsecutiveBars, 1 },
        { SlopeMomentumStrat.ParamKey.TradeRateShareMin, 0.58 },
        { SlopeMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
        { SlopeMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.18 },
        { SlopeMomentumStrat.ParamKey.MinSlope, 0.06 },
        { SlopeMomentumStrat.ParamKey.ExitMinSlopeRequirement, 0.00 }
    }
)

};

    }
}
