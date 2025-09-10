using BacklashDTOs;
using BacklashPatterns;
using TradingStrategies.Extensions;

namespace TradingStrategies.Trading.Overseer
{
    public class PatternDetectionService
    {
        public List<BacklashPatterns.PatternDefinitions.PatternDefinition> DetectPatterns(MarketSnapshot snapshot)
        {
            if (snapshot.RecentCandlesticks == null || snapshot.RecentCandlesticks.Count == 0)
            {
                return new List<BacklashPatterns.PatternDefinitions.PatternDefinition>();
            }

            try
            {
                var mids = snapshot.RecentCandlesticks.ToCandleMids(snapshot.MarketTicker);
                var patterns = PatternSearch.DetectPatterns(mids, 10);
                if (patterns.Keys.Count > 0)
                {
                    return patterns[patterns.Keys.Last()];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error detecting patterns: {ex.Message}");
            }

            return new List<BacklashPatterns.PatternDefinitions.PatternDefinition>();
        }
    }
}