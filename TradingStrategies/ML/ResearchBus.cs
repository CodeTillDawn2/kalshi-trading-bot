// ResearchBus.cs
using System.Collections.Concurrent;
using static SmokehouseInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.ML
{
    public static class ResearchBus
    {
        public static readonly ConcurrentBag<EntryResearch> Entries = new();

        public static void Clear()
        {
            while (Entries.TryTake(out _)) { }
        }

        public static void Log(EntryResearch e) => Entries.Add(e);

        public static void DumpCsv(string path)
        {
            using var sw = new StreamWriter(path);
            sw.WriteLine("market,ts_entry,side,thr,hit_tau,tau,dd,mfe,mae,peak_size,time_to_peak_sec,p_long,p_short,score,parameter_set");
            foreach (var e in Entries.OrderByDescending(x => x.Score))
            {
                sw.WriteLine($"{e.MarketTicker},{e.EntryTime:O},{e.Side},{e.ThresholdUsed:F3}," +
                             $"{e.HitTau},{e.TauTicks},{e.DdTicks},{e.MfeTicks},{e.MaeTicks},{e.PeakSizeTicks}," +
                             $"{(int)e.TimeToPeak.TotalSeconds},{e.PLongAtEntry:F4},{e.PShortAtEntry:F4},{e.Score:F4},{e.ParameterSet}");
            }
        }

        public record EntryResearch(
            string MarketTicker,
            ActionType Side,
            DateTime EntryTime,
            DateTime HorizonEnd,
            int EntryMidTicks,
            int TauTicks,
            int DdTicks,
            bool HitTau,
            int MfeTicks,
            int MaeTicks,
            int PeakSizeTicks,
            TimeSpan TimeToPeak,
            double PLongAtEntry,
            double PShortAtEntry,
            double ThresholdUsed,
            double Score,
            string Memo,
            string ParameterSet
        );
    }
}