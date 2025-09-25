using Microsoft.Extensions.DependencyInjection;

namespace BacklashBot.Management.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that calculates interest scores for markets
    /// based on various trading metrics and parameters.
    /// </summary>
    public interface IInterestScoreService
    {
        /// <summary>
        /// Calculates interest scores for a collection of market tickers using weighted parameters.
        /// </summary>
        /// <param name="scopeFactory">Factory for creating service scopes for dependency injection.</param>
        /// <param name="tickers">Collection of market tickers to score.</param>
        /// <param name="spreadTightnessWeight">Weight for spread tightness in scoring (default 0.2).</param>
        /// <param name="spreadWidthWeight">Weight for spread width in scoring (default 0.2).</param>
        /// <param name="volumeWeight">Weight for volume in scoring (default 0.33).</param>
        /// <param name="volumePercentileWeight">Weight for volume percentile in scoring (default 0.15).</param>
        /// <param name="liquidityPercentileWeight">Weight for liquidity percentile in scoring (default 0.06).</param>
        /// <param name="openInterestPercentileWeight">Weight for open interest percentile in scoring (default 0.06).</param>
        /// <returns>A list of tuples containing ticker and calculated score.</returns>
        Task<List<(string Ticker, double Score)>> GetMarketInterestScores(
            IServiceScopeFactory scopeFactory,
            IEnumerable<string> tickers,
            double spreadTightnessWeight = 0.2,
            double spreadWidthWeight = 0.2,
            double volumeWeight = 0.33,
            double volumePercentileWeight = 0.15,
            double liquidityPercentileWeight = 0.06,
            double openInterestPercentileWeight = 0.06);

        /// <summary>
        /// Calculates a detailed interest score for a single market with component breakdown.
        /// </summary>
        /// <param name="market">Dynamic market object containing market data.</param>
        /// <param name="snapshotCount">Number of snapshots available for the market.</param>
        /// <param name="spreadTightnessWeight">Weight for spread tightness in scoring (default 0.2).</param>
        /// <param name="spreadWidthWeight">Weight for spread width in scoring (default 0.15).</param>
        /// <param name="volumeWeight">Weight for volume in scoring (default 0.20).</param>
        /// <param name="volumePercentileWeight">Weight for volume percentile in scoring (default 0.1).</param>
        /// <param name="liquidityPercentileWeight">Weight for liquidity percentile in scoring (default 0.09).</param>
        /// <param name="openInterestPercentileWeight">Weight for open interest percentile in scoring (default 0.065).</param>
        /// <param name="continuityWeight">Weight for continuity in scoring (default 0.145).</param>
        /// <returns>A tuple containing the total score and a tuple of individual score components.</returns>
        Task<(double score,
            (double spreadTightness, double spreadWidth, double volume, double volumePercentile, double liquidityPercentile, double openInterestPercentile, double continuity) scoreParts)>
        CalculateMarketInterestScoreAsync(
            dynamic market,
            long snapshotCount,
            double spreadTightnessWeight = 0.2,
            double spreadWidthWeight = 0.15,
            double volumeWeight = 0.20,
            double volumePercentileWeight = 0.1,
            double liquidityPercentileWeight = 0.09,
            double openInterestPercentileWeight = 0.065,
            double continuityWeight = 0.145);
    }
}
