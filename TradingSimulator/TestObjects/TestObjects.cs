// Updated TradingSimulator.TestObjects with EventPoints in CachedMarketData
using TradingSimulator.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TradingSimulator.TestObjects
{
    public record PricePoint(DateTime Date, double Price, string? Memo = null)
    {
        public List<string> MemoParts => string.IsNullOrEmpty(Memo)
            ? new List<string>()
            : Memo.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(part => part.Trim())
                  .Where(part => !string.IsNullOrEmpty(part))
                  .ToList();
    }

    public record TradePoint(DateTime Date, double Price, TradingDecisionEnum Decision);

    public class CachedMarketData
    {
        public string Market { get; set; }
        public double PnL { get; set; }
        public List<PricePoint> BidPoints { get; set; }
        public List<PricePoint> AskPoints { get; set; }
        public List<PricePoint> BuyPoints { get; set; }
        public List<PricePoint> SellPoints { get; set; }
        public List<PricePoint> EventPoints { get; set; }
        public List<PricePoint> IntendedLongPoints { get; set; }
        public List<PricePoint> IntendedShortPoints { get; set; }
        public List<PricePoint> ExitPoints { get; set; }
    }

}