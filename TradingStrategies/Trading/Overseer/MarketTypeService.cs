using BacklashDTOs;
using TradingStrategies.Trading.Helpers;
using static BacklashInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Trading.Overseer
{
    public class MarketTypeService
    {
        private readonly MarketTypeHelper _marketTypeHelper;
        private readonly Dictionary<(string Ticker, DateTime Timestamp), MarketType> _marketTypeCache;

        public MarketTypeService()
        {
            _marketTypeHelper = new MarketTypeHelper();
            _marketTypeCache = new Dictionary<(string, DateTime), MarketType>();
        }

        public void SetMarketType(MarketSnapshot snapshot)
        {
            try
            {
                var key = (snapshot.MarketTicker, snapshot.Timestamp);
                if (!_marketTypeCache.TryGetValue(key, out var cachedType))
                {
                    cachedType = _marketTypeHelper.GetMarketType(snapshot);
                    _marketTypeCache[key] = cachedType;
                }
                snapshot.MarketType = cachedType.ToString();
            }
            catch
            {
                snapshot.MarketType = "Unknown";
            }
        }

        public MarketType ParseMarketConditions(string marketType)
        {
            if (!Enum.TryParse<MarketType>(marketType, true, out var currentMarketConditions))
            {
                currentMarketConditions = MarketType.Undefined;
            }
            return currentMarketConditions;
        }
    }
}