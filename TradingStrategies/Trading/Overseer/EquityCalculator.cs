using BacklashDTOs;

namespace TradingStrategies.Trading.Overseer
{
    public class EquityCalculator
    {
        public double GetEquity(SimulationPath path, MarketSnapshot lastSnapshot)
        {
            double equity = path.Cash;
            if (path.SimulatedBook == null)
                return equity;

            int bestYesBid = path.SimulatedBook.GetBestYesBid();
            int bestNoBid = path.SimulatedBook.GetBestNoBid();
            int bestYesAsk = bestNoBid > 0 ? 100 - bestNoBid : 100;
            int bestNoAsk = bestYesBid > 0 ? 100 - bestYesBid : 100;

            bool natural = bestYesBid == 0 || bestNoBid == 0;
            if (natural)
            {
                if (path.Position > 0)
                {
                    equity += path.Position * (bestNoBid == 0 ? 1.0 : 0.0);
                }
                else if (path.Position < 0)
                {
                    equity += Math.Abs(path.Position) * (bestYesBid == 0 ? 1.0 : 0.0);
                }
            }
            else
            {
                double midYes = (bestYesBid + bestYesAsk) / 2 / 100.0;
                double midNo = (bestNoBid + bestNoAsk) / 2 / 100.0;
                if (path.Position > 0)
                    equity += path.Position * midYes;
                else if (path.Position < 0)
                    equity += Math.Abs(path.Position) * midNo;
            }
            return equity;
        }
    }
}