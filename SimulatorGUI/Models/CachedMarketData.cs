using System.Collections.Generic;

namespace SimulatorGUI.Models
{
    /// <summary>
    /// A serializable DTO for caching computed market results to disk.  When
    /// running backtests from the GUI the simulator will produce lists of
    /// price points for each market; persisting those lists allows the
    /// application to quickly load and plot previously computed results
    /// without reprocessing the raw snapshots.  The PnL is stored so the
    /// summary grid can be populated lazily on startup.
    /// </summary>
    public class CachedMarketData
    {
        public string Market { get; set; } = string.Empty;
        public double PnL { get; set; }
        public List<PricePoint> BidPoints { get; set; } = new();
        public List<PricePoint> AskPoints { get; set; } = new();
        public List<PricePoint> BuyPoints { get; set; } = new();
        public List<PricePoint> SellPoints { get; set; } = new();
        public List<PricePoint> EventPoints { get; set; } = new();
        public List<PricePoint> IntendedLongPoints { get; set; } = new();
        public List<PricePoint> IntendedShortPoints { get; set; } = new();
    }
}