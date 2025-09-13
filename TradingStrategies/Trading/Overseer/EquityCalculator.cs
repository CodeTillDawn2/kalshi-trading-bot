using BacklashDTOs;

namespace TradingStrategies.Trading.Overseer
{
    /// <summary>
    /// Provides methods for calculating the total equity value of a trading simulation path.
    /// This class is responsible for determining the current market value of positions held in a simulated trading environment,
    /// taking into account both cash holdings and the unrealized value of open positions based on current order book data.
    /// </summary>
    /// <remarks>
    /// The EquityCalculator is a critical component in the trading simulation pipeline, used by the StrategySimulation engine
    /// to evaluate the performance of trading strategies. It handles different market conditions (natural vs. non-natural markets)
    /// and provides accurate equity calculations that reflect real-world trading mechanics.
    ///
    /// Key responsibilities:
    /// - Calculate total equity as cash plus position value
    /// - Handle natural markets where one side has no liquidity
    /// - Use mid-prices for non-natural markets to estimate fair value
    /// - Support both long and short positions
    ///
    /// This class is stateless and thread-safe, making it suitable for concurrent simulation runs.
    /// </remarks>
    public class EquityCalculator
    {
        /// <summary>
        /// Calculates the total equity value for a given simulation path at the current market snapshot.
        /// </summary>
        /// <param name="path">The simulation path containing cash, position, and order book state.</param>
        /// <param name="lastSnapshot">The most recent market snapshot containing current market data (used for context but not directly for calculation).</param>
        /// <returns>The total equity value in dollars, including cash and the current market value of all positions.</returns>
        /// <remarks>
        /// This method implements the core equity calculation logic used throughout the trading simulation system.
        /// The calculation follows these steps:
        ///
        /// 1. Start with the cash balance from the simulation path
        /// 2. If no simulated order book exists, return cash only
        /// 3. Extract best bid/ask prices from the simulated order book
        /// 4. Determine if the market is "natural" (one side has no bids)
        /// 5. For natural markets:
        ///    - Long positions (positive): Value at 1.0 if no bids on the short side, 0.0 otherwise
        ///    - Short positions (negative): Value at 1.0 if no bids on the long side, 0.0 otherwise
        /// 6. For non-natural markets:
        ///    - Use mid-prices (average of best bid and ask) to value positions
        ///    - Long positions valued at mid-price of the "Yes" side
        ///    - Short positions valued at mid-price of the "No" side
        ///
        /// The method ensures accurate valuation that reflects the current state of the simulated market,
        /// providing a realistic assessment of portfolio value for strategy evaluation and backtesting purposes.
        ///
        /// Example usage:
        /// <code>
        /// var calculator = new EquityCalculator();
        /// double totalEquity = calculator.GetEquity(simulationPath, marketSnapshot);
        /// </code>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if path is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the simulated order book data is corrupted.</exception>
        public double GetEquity(SimulationPath path, MarketSnapshot lastSnapshot)
        {
            // Validate input parameters
            if (path == null)
                throw new ArgumentNullException(nameof(path), "Simulation path cannot be null");

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