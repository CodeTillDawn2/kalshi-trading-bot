using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace SmokehouseBot.Management.Interfaces
{
    public interface IInterestScoreService
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

        Task<(double score,
            (double spreadTightness, double spreadWidth, double volume, double volumePercentile, double liquidityPercentile, double openInterestPercentile, double continuity) scoreParts)>
        CalculateMarketInterestScoreAsync(
            IKalshiBotContext dbContext,
            string marketTicker,
            double spreadTightnessWeight = 0.2,
            double spreadWidthWeight = 0.15,
            double volumeWeight = 0.28,
            double volumePercentileWeight = 0.125,
            double liquidityPercentileWeight = 0.05,
            double openInterestPercentileWeight = 0.05,
            double continuityWeight = 0.145);
    }
}