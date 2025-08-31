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
    )
};

    }
}
