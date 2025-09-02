using SmokehouseDTOs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using static SmokehouseInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Strategies.Strats
{
    public class YetiStrat : Strat
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
            VelocityToDepthRatio,           // |net flow| threshold: |(vYes/depthYes) - (vNo/depthNo)|
            MinRatioDifference,             // additional |net flow| min
            MinConsecutiveBars,             // sustain N bars

            // Confirmations
            TradeRateShareMin,              // share of trade rate for the chosen side
            TradeEventShareMin,             // share of trade events for the chosen side

            // Exits / slope
            ExitOppositeSignalStrength,     // min |net flow| needed to allow flip
            MinSlope,                       // min NET slope (yesSlope - noSlope) for entry (SHORT horizon)
            MinSlope_Medium,                // min NET slope (yesSlope - noSlope) for entry (MEDIUM horizon)
            ExitMinSlopeRequirement,        // min NET slope magnitude to exit (SHORT horizon)
            ExitMinSlopeRequirement_Medium, // min NET slope magnitude to exit (MEDIUM horizon)

            Top10VelocityWeight             // multiplier for Top-10% velocity (1.0 = neutral; >1 upweights Top bucket)
        }


        public YetiStrat(
    string name = nameof(YetiStrat),
    double weight = 1.0,
    Dictionary<ParamKey, double> mlParams = null)
        {
            Name = name;
            Weight = weight;

            var defaults = new Dictionary<ParamKey, double>
    {
        { ParamKey.MinDistanceFromBounds, 6 },
        { ParamKey.VelocityToDepthRatio, 0.08 },
        { ParamKey.MinRatioDifference, 0.10 },
        { ParamKey.MinConsecutiveBars, 2 },

        { ParamKey.TradeRateShareMin, 0.60 },
        { ParamKey.TradeEventShareMin, 0.40 },

        { ParamKey.ExitOppositeSignalStrength, 0.12 },
        { ParamKey.MinSlope, 0.10 },
        { ParamKey.MinSlope_Medium, 0.0 },
        { ParamKey.ExitMinSlopeRequirement, 0.02 },
        { ParamKey.ExitMinSlopeRequirement_Medium, 0.02 },

        { ParamKey.Top10VelocityWeight, 1.00 } // neutral weighting
    };

            if (mlParams != null)
                foreach (var kv in mlParams) defaults[kv.Key] = kv.Value;

            _params = defaults;
        }

        public override ActionDecision GetAction(MarketSnapshot snapshot, MarketSnapshot? prev, int simulationPosition = 0)
        {
            var inv = CultureInfo.InvariantCulture;

            int minDist = (int)Math.Round(_params[ParamKey.MinDistanceFromBounds]);
            double baseVD = _params[ParamKey.VelocityToDepthRatio];
            double minDiff = _params[ParamKey.MinRatioDifference];
            int minBars = (int)Math.Round(_params[ParamKey.MinConsecutiveBars]);
            double shareTRMin = _params[ParamKey.TradeRateShareMin];
            double shareTEMin = _params[ParamKey.TradeEventShareMin];
            double exitOpp = _params[ParamKey.ExitOppositeSignalStrength];
            double minSlope = _params[ParamKey.MinSlope];
            double minSlopeMed = _params[ParamKey.MinSlope_Medium];
            double exitMinShort = _params[ParamKey.ExitMinSlopeRequirement];
            double exitMinMed = _params[ParamKey.ExitMinSlopeRequirement_Medium];
            double wTop = _params[ParamKey.Top10VelocityWeight];

            // split velocities; then apply upweight to the Top-10% buckets only
            double vTopYes = snapshot.VelocityPerMinute_Top_Yes_Bid;
            double vBotYes = snapshot.VelocityPerMinute_Bottom_Yes_Bid;
            double vTopNo = snapshot.VelocityPerMinute_Top_No_Bid;
            double vBotNo = snapshot.VelocityPerMinute_Bottom_No_Bid;

            double vYesRaw = vTopYes + vBotYes;
            double vNoRaw = vTopNo  + vBotNo;

            double vYes = (wTop * vTopYes) + vBotYes; // upweighted
            double vNo = (wTop * vTopNo)  + vBotNo;  // upweighted

            double depthYes = Math.Max(snapshot.TotalOrderbookDepth_Yes / 100.0, 1e-9);
            double depthNo = Math.Max(snapshot.TotalOrderbookDepth_No  / 100.0, 1e-9);
            double flowYes = vYes / depthYes;
            double flowNo = vNo  / depthNo;

            // unified NET metric: Yes positive, No negative
            double netFlow = flowYes - flowNo;
            double absNetFlow = Math.Abs(netFlow);
            double ratioEdge = Math.Abs(flowYes - flowNo);

            // diagnostics (memo)
            double totalRate = Math.Max(snapshot.TradeRatePerMinute_Yes + snapshot.TradeRatePerMinute_No, 1e-9);
            double trShareYes = snapshot.TradeRatePerMinute_Yes / totalRate;
            double trShareNo = snapshot.TradeRatePerMinute_No  / totalRate;

            double totalTrades = Math.Max((double)(snapshot.TradeCount_Yes + snapshot.TradeCount_No), 1e-9);
            double teShareYes = snapshot.TradeCount_Yes / totalTrades;
            double teShareNo = snapshot.TradeCount_No  / totalTrades;

            // slopes
            double yS = snapshot.YesBidSlopePerMinute_Short, nS = snapshot.NoBidSlopePerMinute_Short, netS = yS - nS;
            double yM = snapshot.YesBidSlopePerMinute_Medium, nM = snapshot.NoBidSlopePerMinute_Medium, netM = yM - nM;

            if (!snapshot.ChangeMetricsMature)
                return AD(ActionType.None, 0, 0,
                    Memo(inv, "warmup:not_mature",
                         minDist, baseVD, minDiff, minBars, shareTRMin, shareTEMin, exitOpp, minSlope, minSlopeMed,
                         exitMinShort, exitMinMed,
                         vYes, vNo, vYesRaw, vNoRaw, vTopYes, vBotYes, vTopNo, vBotNo, wTop,
                         depthYes, depthNo, flowYes, flowNo, ratioEdge,
                         trShareYes, trShareNo, teShareYes, teShareNo,
                         _consecYes, _consecNo, simulationPosition, ActionType.None,
                         yS, nS, netS, yM, nM, netM, false, false, false, netFlow,
                         0, 0, 0, 0, ActionType.None, false, false, false, false,
                         false, false));

            if (snapshot.BestYesAsk >= 100 - minDist || snapshot.BestYesBid <= minDist)
                return AD(ActionType.None, 0, 0,
                    Memo(inv, "gate:near_boundary",
                         minDist, baseVD, minDiff, minBars, shareTRMin, shareTEMin, exitOpp, minSlope, minSlopeMed,
                         exitMinShort, exitMinMed,
                         vYes, vNo, vYesRaw, vNoRaw, vTopYes, vBotYes, vTopNo, vBotNo, wTop,
                         depthYes, depthNo, flowYes, flowNo, ratioEdge,
                         trShareYes, trShareNo, teShareYes, teShareNo,
                         _consecYes, _consecNo, simulationPosition, ActionType.None,
                         yS, nS, netS, yM, nM, netM, false, false, false, netFlow,
                         0, 0, 0, 0, ActionType.None, false, false, false, false,
                         false, false));

            if (prev is null)
                return AD(ActionType.None, 0, 0,
                    Memo(inv, "warmup:no_prev",
                         minDist, baseVD, minDiff, minBars, shareTRMin, shareTEMin, exitOpp, minSlope, minSlopeMed,
                         exitMinShort, exitMinMed,
                         vYes, vNo, vYesRaw, vNoRaw, vTopYes, vBotYes, vTopNo, vBotNo, wTop,
                         depthYes, depthNo, flowYes, flowNo, ratioEdge,
                         trShareYes, trShareNo, teShareYes, teShareNo,
                         _consecYes, _consecNo, simulationPosition, ActionType.None,
                         yS, nS, netS, yM, nM, netM, false, false, false, netFlow,
                         0, 0, 0, 0, ActionType.None, false, false, false, false,
                         false, false));

            // YES price drives both directions (short = bearish YES)
            int yesPrev = prev.BestYesBid, yesNow = snapshot.BestYesBid;
            int noPrev = prev.BestNoBid, noNow = snapshot.BestNoBid;

            // ---- COUNTER LOGIC (relaxed) ----
            bool counterYesPass = (yesNow >= yesPrev) && (absNetFlow >= baseVD) && (netS >=  minSlope);
            bool counterNoPass = (yesNow <= yesPrev) && (absNetFlow >= baseVD) && (netS <= -minSlope);

            _consecYes = counterYesPass ? (_consecYes + 1) : 0;
            _consecNo  = counterNoPass ? (_consecNo  + 1) : 0;

            // ---- ENTRY CHECK (strict) ----
            bool longSlopeOK = (netS >= minSlope) || (netM >= minSlopeMed && netS >= minSlopeMed);
            bool shortSlopeOK = (netS <= -minSlope) || (netM <= -minSlopeMed && netS <= -minSlopeMed);

            bool entryYesStrict =
                (yesNow > yesPrev) &&
                (absNetFlow >= baseVD) && (absNetFlow >= minDiff) &&
                (trShareYes >= shareTRMin) && (teShareYes >= shareTEMin) &&
                longSlopeOK;

            bool entryNoStrict =
                (yesNow < yesPrev) &&
                (absNetFlow >= baseVD) && (absNetFlow >= minDiff) &&
                (trShareNo  >= shareTRMin) && (teShareNo  >= shareTEMin) &&
                shortSlopeOK;

            bool countersReadyLong = (_consecYes >= minBars) && (_consecNo == 0);
            bool countersReadyShort = (_consecNo  >= minBars) && (_consecYes == 0);

            bool wouldEnterLong = countersReadyLong  && entryYesStrict;
            bool wouldEnterShort = countersReadyShort && entryNoStrict;

            // ---- EXIT (OR: Short or Medium slope; adverse price) ----
            bool priceDownLongExit = yesNow < yesPrev;
            bool priceUpShortExit = yesNow > yesPrev;

            bool longExitSlopeOK = (netS <= -exitMinShort) || (netM <= -exitMinMed);
            bool shortExitSlopeOK = (netS >=  exitMinShort) || (netM >=  exitMinMed);

            bool wouldExitLong = (simulationPosition > 0) && longExitSlopeOK  && priceDownLongExit;
            bool wouldExitShort = (simulationPosition < 0) && shortExitSlopeOK && priceUpShortExit;

            if (wouldExitLong)
                return AD(ActionType.Exit, snapshot.BestYesBid, Math.Abs(simulationPosition),
                    Memo(inv, "exit:slope_long",
                         minDist, baseVD, minDiff, minBars, shareTRMin, shareTEMin, exitOpp, minSlope, minSlopeMed,
                         exitMinShort, exitMinMed,
                         vYes, vNo, vYesRaw, vNoRaw, vTopYes, vBotYes, vTopNo, vBotNo, wTop,
                         depthYes, depthNo, flowYes, flowNo, ratioEdge,
                         trShareYes, trShareNo, teShareYes, teShareNo,
                         _consecYes, _consecNo, simulationPosition, ActionType.Exit,
                         yS, nS, netS, yM, nM, netM, counterYesPass, counterNoPass, false, netFlow,
                         yesPrev, yesNow, noPrev, noNow, ActionType.Exit, wouldEnterLong, wouldEnterShort, true, false,
                         entryYesStrict, entryNoStrict));

            if (wouldExitShort)
                return AD(ActionType.Exit, snapshot.BestNoBid, Math.Abs(simulationPosition),
                    Memo(inv, "exit:slope_short",
                         minDist, baseVD, minDiff, minBars, shareTRMin, shareTEMin, exitOpp, minSlope, minSlopeMed,
                         exitMinShort, exitMinMed,
                         vYes, vNo, vYesRaw, vNoRaw, vTopYes, vBotYes, vTopNo, vBotNo, wTop,
                         depthYes, depthNo, flowYes, flowNo, ratioEdge,
                         trShareYes, trShareNo, teShareYes, teShareNo,
                         _consecYes, _consecNo, simulationPosition, ActionType.Exit,
                         yS, nS, netS, yM, nM, netM, counterYesPass, counterNoPass, false, netFlow,
                         yesPrev, yesNow, noPrev, noNow, ActionType.Exit, wouldEnterLong, wouldEnterShort, false, true,
                         entryYesStrict, entryNoStrict));

            var candidate = ActionType.None;
            if (wouldEnterLong) candidate = ActionType.Long;
            if (wouldEnterShort) candidate = ActionType.Short;

            // Flip protection
            var candidateBeforeFlip = candidate;
            bool flipBlocked = false;
            if (simulationPosition > 0 && candidate == ActionType.Short && absNetFlow < exitOpp) { flipBlocked = true; candidate = ActionType.None; }
            if (simulationPosition < 0 && candidate == ActionType.Long  && absNetFlow < exitOpp) { flipBlocked = true; candidate = ActionType.None; }

            if (candidate == ActionType.None)
                return AD(ActionType.None, 0, 0,
                    Memo(inv, "entry:pass",
                         minDist, baseVD, minDiff, minBars, shareTRMin, shareTEMin, exitOpp, minSlope, minSlopeMed,
                         exitMinShort, exitMinMed,
                         vYes, vNo, vYesRaw, vNoRaw, vTopYes, vBotYes, vTopNo, vBotNo, wTop,
                         depthYes, depthNo, flowYes, flowNo, ratioEdge,
                         trShareYes, trShareNo, teShareYes, teShareNo,
                         _consecYes, _consecNo, simulationPosition, candidateBeforeFlip,
                         yS, nS, netS, yM, nM, netM, counterYesPass, counterNoPass, flipBlocked, netFlow,
                         yesPrev, yesNow, noPrev, noNow, ActionType.None, wouldEnterLong, wouldEnterShort, false, false,
                         entryYesStrict, entryNoStrict));

            int pricePoint = (candidate == ActionType.Long) ? snapshot.BestYesBid : snapshot.BestNoBid;

            return AD(candidate, pricePoint, 1,
                Memo(inv, "entry:go",
                     minDist, baseVD, minDiff, minBars, shareTRMin, shareTEMin, exitOpp, minSlope, minSlopeMed,
                     exitMinShort, exitMinMed,
                     vYes, vNo, vYesRaw, vNoRaw, vTopYes, vBotYes, vTopNo, vBotNo, wTop,
                     depthYes, depthNo, flowYes, flowNo, ratioEdge,
                     trShareYes, trShareNo, teShareYes, teShareNo,
                     _consecYes, _consecNo, simulationPosition, candidate,
                     yS, nS, netS, yM, nM, netM, counterYesPass, counterNoPass, flipBlocked, netFlow,
                     yesPrev, yesNow, noPrev, noNow, candidate, wouldEnterLong, wouldEnterShort, false, false,
                     entryYesStrict, entryNoStrict));
        }
        private static string Memo(
    IFormatProvider inv, string tail,
    int minDist, double baseVD, double minDiff, int minBars,
    double shareTRMin, double shareTEMin, double exitOpp, double minSlope, double minSlopeMed,
    double exitMinShort, double exitMinMed,
    double vYesWeighted, double vNoWeighted, double vYesRaw, double vNoRaw,
    double vTopYes, double vBotYes, double vTopNo, double vBotNo, double wTop,
    double depthYesScaled, double depthNoScaled, double flowYes, double flowNo, double ratioEdge,
    double trShareYes, double trShareNo, double teShareYes, double teShareNo,
    int consecY, int consecN, int position, ActionType candidateBeforeFlip,
    double yesSlopeShort, double noSlopeShort, double netSlopeShort,
    double yesSlopeMed, double noSlopeMed, double netSlopeMed,
    bool counterYesPass, bool counterNoPass, bool flipBlocked, double netFlow,
    int yesBidPrev, int yesBidNow, int noBidPrev, int noBidNow,
    ActionType actionTaken, bool wouldEnterLong, bool wouldEnterShort, bool wouldExitLong, bool wouldExitShort,
    bool entryYesStrict, bool entryNoStrict)
        {
            string F(double d) => d.ToString("0.###", inv);

            double depthYesTrue = depthYesScaled * 100.0;
            double depthNoTrue = depthNoScaled  * 100.0;

            // price moves
            bool longPriceUpStrict = yesBidNow >  yesBidPrev;
            bool shortPriceDownStrict = yesBidNow <  yesBidPrev;
            bool longPriceUpOrEq = yesBidNow >= yesBidPrev; // counter-only relaxation
            bool shortPriceDownOrEq = yesBidNow <= yesBidPrev; // counter-only relaxation
            bool longPriceDown = yesBidNow <  yesBidPrev; // adverse for long exit
            bool shortPriceUp = yesBidNow >  yesBidPrev; // adverse for short exit

            // Counter criteria (relaxed)
            bool LongCounterOK() => longPriceUpOrEq    && (Math.Abs(netFlow) >= baseVD) && (netSlopeShort >=  minSlope);
            bool ShortCounterOK() => shortPriceDownOrEq && (Math.Abs(netFlow) >= baseVD) && (netSlopeShort <= -minSlope);

            // Entry bar checks (strict) — updated slope logic:
            // S ≥ minSlope  OR  (M ≥ minSlopeMed AND S ≥ minSlopeMed)
            bool LongSlopeOK() => (netSlopeShort >= minSlope) || (netSlopeMed >= minSlopeMed && netSlopeShort >= minSlopeMed);
            bool ShortSlopeOK() => (netSlopeShort <= -minSlope) || (netSlopeMed <= -minSlopeMed && netSlopeShort <= -minSlopeMed);

            bool LongEntryOK() =>
                longPriceUpStrict &&
                (Math.Abs(netFlow) >= baseVD) && (Math.Abs(netFlow) >= minDiff) &&
                (trShareYes >= shareTRMin) && (teShareYes >= shareTEMin) &&
                LongSlopeOK();

            bool ShortEntryOK() =>
                shortPriceDownStrict &&
                (Math.Abs(netFlow) >= baseVD) && (Math.Abs(netFlow) >= minDiff) &&
                (trShareNo >= shareTRMin) && (teShareNo >= shareTEMin) &&
                ShortSlopeOK();

            string ActionLine() => actionTaken switch
            {
                ActionType.Long => "Action: Enter LONG",
                ActionType.Short => "Action: Enter SHORT",
                ActionType.Exit => position > 0 ? "Action: EXIT LONG" : "Action: EXIT SHORT",
                _ => "Action: None"
            };

            // ---- Explicit blocker text helpers ----
            string BlockerLong()
            {
                var rs = new List<string>();
                if (consecY < minBars) rs.Add($"consecY {consecY} < {minBars}");
                if (consecN > 0) rs.Add($"opp counter > 0 (consecN={consecN})");
                if (consecY >= minBars && consecN == 0 && !entryYesStrict)
                {
                    if (!longPriceUpStrict) rs.Add($"price not > (YesBid {yesBidPrev}->{yesBidNow})");
                    else if (Math.Abs(netFlow) < baseVD) rs.Add($"|net| {F(Math.Abs(netFlow))} < {F(baseVD)}");
                    else if (Math.Abs(netFlow) < minDiff) rs.Add($"|net| {F(Math.Abs(netFlow))} < minDiff {F(minDiff)}");
                    else if (trShareYes < shareTRMin) rs.Add($"TRᵧ {F(trShareYes)} < {F(shareTRMin)}");
                    else if (teShareYes < shareTEMin) rs.Add($"TEᵧ {F(teShareYes)} < {F(shareTEMin)}");
                    else if (!LongSlopeOK()) rs.Add($"slope rule fail: need S ≥ {F(minSlope)} OR (M ≥ {F(minSlopeMed)} AND S ≥ {F(minSlopeMed)}); got S {F(netSlopeShort)}, M {F(netSlopeMed)}");
                }
                if (candidateBeforeFlip == ActionType.Long && flipBlocked)
                    rs.Add($"flip blocked: |net| {F(Math.Abs(netFlow))} < opp thr {F(exitOpp)}");
                return rs.Count == 0 ? null : string.Join("; ", rs);
            }

            string BlockerShort()
            {
                var rs = new List<string>();
                if (consecN < minBars) rs.Add($"consecN {consecN} < {minBars}");
                if (consecY > 0) rs.Add($"opp counter > 0 (consecY={consecY})");
                if (consecN >= minBars && consecY == 0 && !entryNoStrict)
                {
                    if (!shortPriceDownStrict) rs.Add($"price not < (YesBid {yesBidPrev}->{yesBidNow})");
                    else if (Math.Abs(netFlow) < baseVD) rs.Add($"|net| {F(Math.Abs(netFlow))} < {F(baseVD)}");
                    else if (Math.Abs(netFlow) < minDiff) rs.Add($"|net| {F(Math.Abs(netFlow))} < minDiff {F(minDiff)}");
                    else if (trShareNo < shareTRMin) rs.Add($"TRₙ {F(trShareNo)} < {F(shareTRMin)}");
                    else if (teShareNo < shareTEMin) rs.Add($"TEₙ {F(teShareNo)} < {F(shareTEMin)}");
                    else if (!ShortSlopeOK()) rs.Add($"slope rule fail: need S ≤ -{F(minSlope)} OR (M ≤ -{F(minSlopeMed)} AND S ≤ -{F(minSlopeMed)}); got S {F(netSlopeShort)}, M {F(netSlopeMed)}");
                }
                if (candidateBeforeFlip == ActionType.Short && flipBlocked)
                    rs.Add($"flip blocked: |net| {F(Math.Abs(netFlow))} < opp thr {F(exitOpp)}");
                return rs.Count == 0 ? null : string.Join("; ", rs);
            }

            // Summaries (explanatory, unchanged tone)
            string CounterSummaryLong() =>
                LongCounterOK() ? $"Counter LONG: OK (price ≥, |net| ≥ {F(baseVD)}, slope(S) ≥ {F(minSlope)}; consecY={consecY})"
                                : $"Counter LONG: No — {(longPriceUpOrEq ? (Math.Abs(netFlow) >= baseVD ? $"netSlope(S) {F(netSlopeShort)} < {F(minSlope)}" : $"|net| {F(Math.Abs(netFlow))} < {F(baseVD)}") : $"price not ≥ (YesBid {yesBidPrev}->{yesBidNow})")} (consecY={consecY})";

            string CounterSummaryShort() =>
                ShortCounterOK() ? $"Counter SHORT: OK (price ≤, |net| ≥ {F(baseVD)}, slope(S) ≤ -{F(minSlope)}; consecN={consecN})"
                                 : $"Counter SHORT: No — {(shortPriceDownOrEq ? (Math.Abs(netFlow) >= baseVD ? $"netSlope(S) {F(netSlopeShort)} > -{F(minSlope)}" : $"|net| {F(Math.Abs(netFlow))} < {F(baseVD)}") : $"price not ≤ (YesBid {yesBidPrev}->{yesBidNow})")} (consecN={consecN})";

            string EntrySummaryLong() =>
                entryYesStrict ? "EntryCheck LONG: OK (price >, |net| ≥ base & minDiff, shares OK, slope OK: S≥min or (M≥med & S≥med))"
                               : $"EntryCheck LONG: No — {(longPriceUpStrict ? (Math.Abs(netFlow) >= baseVD ? (Math.Abs(netFlow) >= minDiff ? (trShareYes >= shareTRMin ? (teShareYes >= shareTEMin ? (LongSlopeOK() ? "unexpected" : $"slope rule fail: need S ≥ {F(minSlope)} OR (M ≥ {F(minSlopeMed)} AND S ≥ {F(minSlopeMed)}); got S {F(netSlopeShort)}, M {F(netSlopeMed)}") : $"TEᵧ {F(teShareYes)} < {F(shareTEMin)}") : $"TRᵧ {F(trShareYes)} < {F(shareTRMin)}") : $"|net| {F(Math.Abs(netFlow))} < minDiff {F(minDiff)}") : $"|net| {F(Math.Abs(netFlow))} < {F(baseVD)}") : $"price not > (YesBid {yesBidPrev}->{yesBidNow})")}";

            string EntrySummaryShort() =>
                entryNoStrict ? "EntryCheck SHORT: OK (price <, |net| ≥ base & minDiff, shares OK, slope OK: S≤-min or (M≤-med & S≤-med))"
                              : $"EntryCheck SHORT: No — {(shortPriceDownStrict ? (Math.Abs(netFlow) >= baseVD ? (Math.Abs(netFlow) >= minDiff ? (trShareNo >= shareTRMin ? (teShareNo >= shareTEMin ? (ShortSlopeOK() ? "unexpected" : $"slope rule fail: need S ≤ -{F(minSlope)} OR (M ≤ -{F(minSlopeMed)} AND S ≤ -{F(minSlopeMed)}); got S {F(netSlopeShort)}, M {F(netSlopeMed)}") : $"TEₙ {F(teShareNo)} < {F(shareTEMin)}") : $"TRₙ {F(trShareNo)} < {F(shareTRMin)}") : $"|net| {F(Math.Abs(netFlow))} < minDiff {F(minDiff)}") : $"|net| {F(Math.Abs(netFlow))} < {F(baseVD)}") : $"price not < (YesBid {yesBidPrev}->{yesBidNow})")}";

            // Would-enter lines with explicit blockers
            string longBlock = BlockerLong();
            string shortBlock = BlockerShort();

            string wouldLong = wouldEnterLong
                ? $"Would Enter LONG: Yes — {CounterSummaryLong()} | {EntrySummaryLong()}"
                : $"Would Enter LONG: No — {CounterSummaryLong()} | {EntrySummaryLong()} | Blocker: {longBlock ?? "n/a"}";

            string wouldShort = wouldEnterShort
                ? $"Would Enter SHORT: Yes — {CounterSummaryShort()} | {EntrySummaryShort()}"
                : $"Would Enter SHORT: No — {CounterSummaryShort()} | {EntrySummaryShort()} | Blocker: {shortBlock ?? "n/a"}";

            // Exits (unchanged explanation; OR across short/medium slopes)
            string wouldExit = null;
            if (position > 0)
                wouldExit = wouldExitLong
                    ? $"Would EXIT LONG: Yes — (S ≤ -{F(exitMinShort)} OR M ≤ -{F(exitMinMed)}), price ↓ (YesBid {yesBidPrev}->{yesBidNow})"
                    : $"Would EXIT LONG: No — need (S ≤ -{F(exitMinShort)} OR M ≤ -{F(exitMinMed)}) and price ↓; got S {F(netSlopeShort)}, M {F(netSlopeMed)}, price {(longPriceDown ? "↓" : "↑/=")}";
            else if (position < 0)
                wouldExit = wouldExitShort
                    ? $"Would EXIT SHORT: Yes — (S ≥ {F(exitMinShort)} OR M ≥ {F(exitMinMed)}), price ↑ (YesBid {yesBidPrev}->{yesBidNow})"
                    : $"Would EXIT SHORT: No — need (S ≥ {F(exitMinShort)} OR M ≥ {F(exitMinMed)}) and price ↑; got S {F(netSlopeShort)}, M {F(netSlopeMed)}, price {(shortPriceUp ? "↑" : "↓/=")}";

            var lines = new List<string> { ActionLine() };
            if (flipBlocked) lines.Add("Flip blocked: |net flow| below opposite threshold.");
            lines.Add(wouldLong);
            lines.Add(wouldShort);
            if (position != 0) lines.Add(wouldExit);

            lines.Add("");
            lines.Add("Top10% Velocity Weighting:");
            double deltaYes = (wTop - 1.0) * vTopYes;
            double deltaNo = (wTop - 1.0) * vTopNo;
            lines.Add($"Weight w={F(wTop)} applied to Top10% buckets only.");
            lines.Add($"Raw Velocities (Y/N): top10 {F(vTopYes)}/{F(vTopNo)} + bot90 {F(vBotYes)}/{F(vBotNo)} = {F(vYesRaw)}/{F(vNoRaw)}");
            lines.Add($"Weighted Velocities (Y/N): {F(vYesWeighted)}/{F(vNoWeighted)} (Δ from weight: +{F(deltaYes)} / +{F(deltaNo)})");

            lines.Add("");
            lines.Add("Key Calculations:");
            lines.Add($"YesBid prev→now: {yesBidPrev}→{yesBidNow} | NoBid prev→now: {noBidPrev}→{noBidNow}");
            lines.Add($"Depth (Yes/No): {F(depthYesTrue)} / {F(depthNoTrue)}");
            lines.Add($"Normalized Flow (Yes/No): {F(flowYes)} / {F(flowNo)}");
            lines.Add($"Net Normalized Flow (Yes-No): {F(netFlow)} (|net| thr: {F(baseVD)}; min diff: {F(minDiff)})");
            lines.Add($"Flow Difference: {F(ratioEdge)}");
            lines.Add($"Trade Rate Share (Yes/No): {F(trShareYes)} / {F(trShareNo)} (min side: {F(shareTRMin)})");
            lines.Add($"Trade Event Share (Yes/No): {F(teShareYes)} / {F(teShareNo)} (min side: {F(shareTEMin)})");
            lines.Add($"Consecutive Bars (Yes/No): {consecY} / {consecN} (min: {minBars})");
            lines.Add($"Slopes Short (Y/N; NetS): {F(yesSlopeShort)} / {F(noSlopeShort)}; {F(netSlopeShort)}  |  Slopes Medium (Y/N; NetM): {F(yesSlopeMed)} / {F(noSlopeMed)}; {F(netSlopeMed)}");
            lines.Add($"Exit mins: Short {F(exitMinShort)}, Medium {F(exitMinMed)}");
            lines.Add($"Current Position: {position}");
            lines.Add($"Flip Blocked: {(flipBlocked ? "Yes" : "No")} (opp |net| thr: {F(exitOpp)})");

            return string.Join("\r\n", lines);
        }


        private static ActionDecision AD(ActionType t, int p, int q, string memo)
            => new ActionDecision { Type = t, Price = p, Qty = q, Memo = memo };

        public override string ToJson()
        {
            var dict = _params.ToDictionary(k => k.Key.ToString(), v => v.Value);
            return JsonSerializer.Serialize(new
            {
                type = "YetiStrat",
                name = Name,
                weight = Weight,
                parameters = dict
            });
        }

        public static readonly List<(string Name, Dictionary<YetiStrat.ParamKey, double> Parameters)>
            YetiStratParameterSets = new()
            {
               ("Yeti_Testing",
               new Dictionary<YetiStrat.ParamKey, double>
               {
                   { YetiStrat.ParamKey.MinDistanceFromBounds, 6 },
                   { YetiStrat.ParamKey.VelocityToDepthRatio, 0.6 },
                   { YetiStrat.ParamKey.MinRatioDifference, 0.05 },
                   { YetiStrat.ParamKey.MinConsecutiveBars, 2 },
                   { YetiStrat.ParamKey.TradeRateShareMin, 0.1 },
                   { YetiStrat.ParamKey.TradeEventShareMin, 0.10 },
                   { YetiStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
                   { YetiStrat.ParamKey.MinSlope, 0.80 },
                   { YetiStrat.ParamKey.MinSlope_Medium, 0.20 },
                   { YetiStrat.ParamKey.ExitMinSlopeRequirement_Medium, 0.1 },
                   { YetiStrat.ParamKey.ExitMinSlopeRequirement, 1 },
                   { YetiStrat.ParamKey.Top10VelocityWeight, 5 }
               }),
               ("Default", new Dictionary<YetiStrat.ParamKey, double>
               {
                   { YetiStrat.ParamKey.MinDistanceFromBounds, 6 },
                   { YetiStrat.ParamKey.VelocityToDepthRatio, 0.08 },
                   { YetiStrat.ParamKey.MinRatioDifference, 0.10 },
                   { YetiStrat.ParamKey.MinConsecutiveBars, 2 },
                   { YetiStrat.ParamKey.TradeRateShareMin, 0.60 },
                   { YetiStrat.ParamKey.TradeEventShareMin, 0.40 },
                   { YetiStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
                   { YetiStrat.ParamKey.MinSlope, 0.10 },
                   { YetiStrat.ParamKey.ExitMinSlopeRequirement, 0.02 }
               }),
               ("Aggressive", new Dictionary<YetiStrat.ParamKey, double>
               {
                   { YetiStrat.ParamKey.MinDistanceFromBounds, 4 },
                   { YetiStrat.ParamKey.VelocityToDepthRatio, 0.06 },
                   { YetiStrat.ParamKey.MinRatioDifference, 0.10 },
                   { YetiStrat.ParamKey.MinConsecutiveBars, 1 },
                   { YetiStrat.ParamKey.TradeRateShareMin, 0.60 },
                   { YetiStrat.ParamKey.TradeEventShareMin, 0.40 },
                   { YetiStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
                   { YetiStrat.ParamKey.MinSlope, 0.20 },
                   { YetiStrat.ParamKey.ExitMinSlopeRequirement, 0.75 }
               })
            };

        public static List<(string Name, Dictionary<YetiStrat.ParamKey, double> Parameters)> YetiDefaultGridList_Under10GB()
        {
            return BuildYetiParamGrid(
                (YetiStrat.ParamKey.MinDistanceFromBounds, 2, 8, 3),     // 3: 2,5,8
                (YetiStrat.ParamKey.VelocityToDepthRatio, 0.20, 1.20, 0.30),   // 4: 0.20,0.50,0.80,1.10
                (YetiStrat.ParamKey.MinRatioDifference, 0.10, 0.60, 0.25),   // 3: 0.10,0.35,0.60
                (YetiStrat.ParamKey.MinConsecutiveBars, 2, 5, 1.5),   // 3: 2.0,3.5,5.0
                (YetiStrat.ParamKey.TradeRateShareMin, 0.45, 0.65, 0.10),   // 3
                (YetiStrat.ParamKey.TradeEventShareMin, 0.45, 0.65, 0.10),   // 3
                (YetiStrat.ParamKey.ExitOppositeSignalStrength, 0.30, 1.20, 0.45),   // 3: 0.30,0.75,1.20
                (YetiStrat.ParamKey.MinSlope, 0.50, 2.00, 0.75),   // 3: 0.50,1.25,2.00
                (YetiStrat.ParamKey.MinSlope_Medium, 0.20, 1.00, 0.25),   // 4: 0.20,0.45,0.70,0.95
                (YetiStrat.ParamKey.ExitMinSlopeRequirement, 0.20, 1.00, 0.25),   // 4
                (YetiStrat.ParamKey.ExitMinSlopeRequirement_Medium, 0.10, 0.80, 0.20),   // 4: 0.10,0.30,0.50,0.70
                (YetiStrat.ParamKey.Top10VelocityWeight, 1.00, 2.00, 0.30)    // 4: 1.00,1.30,1.60,1.90
            );
        }



        public static List<(string Name, Dictionary<YetiStrat.ParamKey, double> Parameters)> BuildYetiParamGrid(
            params (YetiStrat.ParamKey Key, double Min, double Max, double Step)[] specs)
        {
            var result = new List<(string, Dictionary<YetiStrat.ParamKey, double>)>();
            if (specs == null || specs.Length == 0) return result;

            var keys = new YetiStrat.ParamKey[specs.Length];
            var ranges = new List<double[]>(specs.Length);

            for (int i = 0; i < specs.Length; i++)
            {
                var (key, min, max, step) = specs[i];
                if (step <= 0) throw new ArgumentOutOfRangeException(nameof(specs), "Step must be > 0.");
                if (min > max) throw new ArgumentException("Min must be ≤ Max.", nameof(specs));
                keys[i] = key;

                var vals = new List<double>(Math.Max(1, (int)Math.Ceiling((max - min) / step) + 1));
                double eps = Math.Abs(step) * 1e-9;
                for (double v = min; v <= max + eps; v += step)
                {
                    double clipped = (v > max && v <= max + eps) ? max : v;
                    vals.Add(clipped);
                }
                ranges.Add(vals.ToArray());
            }

            var idx = new int[ranges.Count];

            while (true)
            {
                var map = new Dictionary<YetiStrat.ParamKey, double>(ranges.Count);
                for (int i = 0; i < ranges.Count; i++) map[keys[i]] = ranges[i][idx[i]];
                string name = $"G{result.Count:000000}";
                result.Add((name, map));

                int p = ranges.Count - 1;
                while (p >= 0 && ++idx[p] >= ranges[p].Length) { idx[p] = 0; p--; }
                if (p < 0) break;
            }

            return result;
        }


    }
}