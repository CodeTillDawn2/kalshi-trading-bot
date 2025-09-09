using static BacklashInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Strategies
{
    public class Scenario
    {
        public Dictionary<MarketType, List<Strategy>> StrategiesByMarketConditions { get; set; }

        public Scenario(Dictionary<MarketType, List<Strategy>> strategiesByMarketConditions)
        {
            StrategiesByMarketConditions = strategiesByMarketConditions;
        }
    }
}
