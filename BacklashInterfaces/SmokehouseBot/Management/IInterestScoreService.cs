using Microsoft.Extensions.DependencyInjection;

namespace BacklashCommon.Services.Interfaces
{
    /// <summary>IInterestScoreService</summary>
    /// <summary>IInterestScoreService</summary>
    public interface IInterestScoreService
    /// <summary>GetMarketInterestScores</summary>
    {
        Task<List<(string Ticker, double Score)>> GetMarketInterestScores(
            IServiceScopeFactory scopeFactory,
            IEnumerable<string> tickers,
            double spreadTightnessWeight = 0.2,
            double spreadWidthWeight = 0.2,
            double volumeWeight = 0.33,
            double volumePercentileWeight = 0.15,
            double liquidityPercentileWeight = 0.06,
            double openInterestPercentileWeight = 0.06);
        /// <summary>CalculateMarketInterestScoreAsync</summary>

        /// <summary>CalculateMarketInterestScoreAsync</summary>
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
