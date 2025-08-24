using System;

namespace SimulatorGUI.Models
{
    /// <summary>
    /// A lightweight record representing a price at a moment in time.  The
    /// simulator emits sequences of PricePoint objects which are consumed by
    /// the chart to plot bid/ask, buy/sell and event markers.  The optional
    /// Memo property may contain a comma delimited list of annotations
    /// describing what occurred at that snapshot (e.g. signals or actions).
    /// </summary>
    public class PricePoint
    {
        public DateTime Date { get; set; }
        public double Price { get; set; }
        public string? Memo { get; set; }

        public PricePoint() { }

        public PricePoint(DateTime date, double price, string? memo = null)
        {
            Date = date;
            Price = price;
            Memo = memo;
        }
    }
}