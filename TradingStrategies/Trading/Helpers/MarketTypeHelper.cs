// Full class: TradingStrategies/Trading/Helpers/MarketTypeHelper.cs (ensure it's instantiated)
using SmokehouseDTOs;
using static SmokehouseInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Trading.Helpers
{
    public class MarketTypeHelper
    {
        private readonly List<(Dictionary<string, Enum> conditions, MarketType marketType, int priority)> _marketTypeMap;

        public MarketTypeHelper()
        {
            _marketTypeMap = new List<(Dictionary<string, Enum>, MarketType, int)>();
            InitializeMarketTypeMap();
        }

        public void AddBroadMapping(
            MarketType marketType,
            Conditions_PriceMovement? priceMovement = null,
            Conditions_Liquidity? liquidity = null,
            Conditions_ClosingTime? closingTime = null,
            Conditions_ActivityLevel? activityLevel = null,
            Conditions_UncertaintySignal? uncertaintySignal = null,
            Conditions_MarketCategory? marketCategory = null,
            int priority = 0)
        {
            var conditions = new Dictionary<string, Enum>();
            if (priceMovement.HasValue) conditions["PriceMovement"] = priceMovement.Value;
            if (liquidity.HasValue) conditions["Liquidity"] = liquidity.Value;
            if (closingTime.HasValue) conditions["ClosingTime"] = closingTime.Value;
            if (activityLevel.HasValue) conditions["ActivityLevel"] = activityLevel.Value;
            if (uncertaintySignal.HasValue) conditions["UncertaintySignal"] = uncertaintySignal.Value;
            if (marketCategory.HasValue) conditions["MarketCategory"] = marketCategory.Value;

            _marketTypeMap.Add((conditions, marketType, priority));
        }

        public void AddPriorityMapping(
            MarketType marketType,
            IEnumerable<(string condition, Enum value)> conditions,
            int priority = 0)
        {
            var conditionDict = conditions.ToDictionary(c => c.condition, c => c.value);
            _marketTypeMap.Add((conditionDict, marketType, priority));
        }

        private void InitializeMarketTypeMap()
        {
            AddBroadMapping(MarketType.Bouncing, priceMovement: Conditions_PriceMovement.Bouncing);
            AddBroadMapping(MarketType.Trending, priceMovement: Conditions_PriceMovement.Trending);
            AddBroadMapping(MarketType.Volatile, priceMovement: Conditions_PriceMovement.Volatile);
            AddBroadMapping(MarketType.Stagnant, priceMovement: Conditions_PriceMovement.Stable);
            AddBroadMapping(MarketType.LowLiquidity, liquidity: Conditions_Liquidity.Low, priority: 10);

            AddBroadMapping(MarketType.HighUncertainty, uncertaintySignal: Conditions_UncertaintySignal.High, priority: 15);
            AddBroadMapping(MarketType.TrendingActive, priceMovement: Conditions_PriceMovement.Trending, activityLevel: Conditions_ActivityLevel.High, priority: 12);

            AddBroadMapping(MarketType.EventDriven, marketCategory: Conditions_MarketCategory.Politics, closingTime: Conditions_ClosingTime.Near, uncertaintySignal: Conditions_UncertaintySignal.High, priority: 20);
            AddBroadMapping(MarketType.NewsDriven, marketCategory: Conditions_MarketCategory.Politics, activityLevel: Conditions_ActivityLevel.High, priority: 18);
            AddBroadMapping(MarketType.StableMacro, marketCategory: Conditions_MarketCategory.Economics, priceMovement: Conditions_PriceMovement.Stable, priority: 15);
            AddBroadMapping(MarketType.FinancialMomentum, marketCategory: Conditions_MarketCategory.Economics, priceMovement: Conditions_PriceMovement.Trending, priority: 16);
            AddBroadMapping(MarketType.VolatileEvents, marketCategory: Conditions_MarketCategory.Crypto, priceMovement: Conditions_PriceMovement.Volatile, activityLevel: Conditions_ActivityLevel.High, priority: 20);
            AddBroadMapping(MarketType.TrendingActive, marketCategory: Conditions_MarketCategory.Crypto, priceMovement: Conditions_PriceMovement.Trending, priority: 18);
            AddBroadMapping(MarketType.EventDriven, marketCategory: Conditions_MarketCategory.Sports, closingTime: Conditions_ClosingTime.Imminent, priority: 22);
            AddBroadMapping(MarketType.VolatileEvents, marketCategory: Conditions_MarketCategory.Sports, activityLevel: Conditions_ActivityLevel.High, priority: 19);
            AddBroadMapping(MarketType.SeasonalVolatile, marketCategory: Conditions_MarketCategory.ClimateAndWeather, priceMovement: Conditions_PriceMovement.Volatile, priority: 17);
            AddBroadMapping(MarketType.Stagnant, marketCategory: Conditions_MarketCategory.ClimateAndWeather, priceMovement: Conditions_PriceMovement.Stable, priority: 14);
            AddBroadMapping(MarketType.NewsDriven, marketCategory: Conditions_MarketCategory.Entertainment, activityLevel: Conditions_ActivityLevel.Moderate, priority: 16);
            AddBroadMapping(MarketType.EventDriven, marketCategory: Conditions_MarketCategory.Entertainment, closingTime: Conditions_ClosingTime.Near, priority: 18);
            AddBroadMapping(MarketType.FinancialMomentum, marketCategory: Conditions_MarketCategory.Financials, priceMovement: Conditions_PriceMovement.Trending, priority: 17);
            AddBroadMapping(MarketType.Volatile, marketCategory: Conditions_MarketCategory.Financials, liquidity: Conditions_Liquidity.High, priority: 15);
            AddBroadMapping(MarketType.TechTrend, marketCategory: Conditions_MarketCategory.ScienceAndTechnology, priceMovement: Conditions_PriceMovement.Trending, priority: 16);
            AddBroadMapping(MarketType.NewsDriven, marketCategory: Conditions_MarketCategory.ScienceAndTechnology, activityLevel: Conditions_ActivityLevel.High, priority: 18);
            AddBroadMapping(MarketType.Bouncing, marketCategory: Conditions_MarketCategory.Companies, priceMovement: Conditions_PriceMovement.Bouncing, priority: 14);
            AddBroadMapping(MarketType.Trending, marketCategory: Conditions_MarketCategory.COVID19, uncertaintySignal: Conditions_UncertaintySignal.High, priority: 15);
            AddBroadMapping(MarketType.VolatileEvents, marketCategory: Conditions_MarketCategory.Education, activityLevel: Conditions_ActivityLevel.Low, priority: 13);
            AddBroadMapping(MarketType.StableMacro, marketCategory: Conditions_MarketCategory.Health, priceMovement: Conditions_PriceMovement.Stable, priority: 14);
            AddBroadMapping(MarketType.NewsDriven, marketCategory: Conditions_MarketCategory.Mentions, activityLevel: Conditions_ActivityLevel.High, priority: 16);
            AddBroadMapping(MarketType.EventDriven, marketCategory: Conditions_MarketCategory.Social, closingTime: Conditions_ClosingTime.Near, priority: 17);
            AddBroadMapping(MarketType.Volatile, marketCategory: Conditions_MarketCategory.Transportation, liquidity: Conditions_Liquidity.Moderate, priority: 15);
            AddBroadMapping(MarketType.NewsDriven, marketCategory: Conditions_MarketCategory.World, activityLevel: Conditions_ActivityLevel.High, priority: 18);

            AddBroadMapping(MarketType.ImminentCloseVolatile, closingTime: Conditions_ClosingTime.Imminent, priceMovement: Conditions_PriceMovement.Volatile, priority: 25);
            AddBroadMapping(MarketType.FarStable, closingTime: Conditions_ClosingTime.Far, priceMovement: Conditions_PriceMovement.Stable, priority: 10);
        }

        public MarketType GetMarketType(MarketSnapshot snapshot)
        {
            Conditions_PriceMovement priceMovement = GetPriceMovement(snapshot);
            Conditions_Liquidity liquidity = GetLiquidity(snapshot);
            Conditions_ClosingTime closingTime = GetClosingTime(snapshot);
            Conditions_ActivityLevel activityLevel = GetActivityLevel(snapshot);
            Conditions_UncertaintySignal uncertaintySignal = GetUncertaintySignal(snapshot);
            Conditions_MarketCategory marketCategory = GetMarketCategory(snapshot);
            return ResolveMarketType(
                priceMovement,
                liquidity,
                closingTime,
                activityLevel,
                uncertaintySignal,
                marketCategory);
        }

        private Conditions_PriceMovement GetPriceMovement(MarketSnapshot snapshot)
        {
            double currentPrice = (snapshot.BestYesBid + snapshot.BestYesAsk) / 2.0;
            double? upper = snapshot.BollingerBands_Medium.Upper;
            double? lower = snapshot.BollingerBands_Medium.Lower;
            double? middle = snapshot.BollingerBands_Medium.Middle;

            if (upper.HasValue && lower.HasValue && middle.HasValue)
            {
                if (currentPrice > upper.Value || currentPrice < lower.Value)
                {
                    return Conditions_PriceMovement.Trending;
                }

                double bandWidth = (upper.Value - lower.Value) / middle.Value;
                if (bandWidth > 0.1)
                {
                    return Conditions_PriceMovement.Volatile;
                }
                else if (bandWidth < 0.05)
                {
                    return Conditions_PriceMovement.Stable;
                }
                return Conditions_PriceMovement.Bouncing;
            }
            return Conditions_PriceMovement.Stable;
        }

        private Conditions_Liquidity GetLiquidity(MarketSnapshot snapshot)
        {
            double liquidityScore = snapshot.CalculateLiquidityScore();

            if (liquidityScore > 80)
            {
                return Conditions_Liquidity.High;
            }
            else if (liquidityScore < 30)
            {
                return Conditions_Liquidity.Low;
            }
            return Conditions_Liquidity.Moderate;
        }

        private Conditions_ClosingTime GetClosingTime(MarketSnapshot snapshot)
        {
            if (snapshot.TimeLeft.HasValue)
            {
                double daysLeft = snapshot.TimeLeft.Value.TotalDays;
                if (daysLeft > 14)
                {
                    return Conditions_ClosingTime.Far;
                }
                else if (daysLeft < 1)
                {
                    return Conditions_ClosingTime.Imminent;
                }
                return Conditions_ClosingTime.Near;
            }
            return Conditions_ClosingTime.Far;
        }

        private Conditions_ActivityLevel GetActivityLevel(MarketSnapshot snapshot)
        {
            double totalTradeRate = snapshot.TradeRatePerMinute_Yes + snapshot.TradeRatePerMinute_No;
            if (totalTradeRate > 5)
            {
                return Conditions_ActivityLevel.High;
            }
            else if (totalTradeRate < 1)
            {
                return Conditions_ActivityLevel.Low;
            }
            return Conditions_ActivityLevel.Moderate;
        }

        private Conditions_UncertaintySignal GetUncertaintySignal(MarketSnapshot snapshot)
        {
            double currentPrice = (snapshot.BestYesBid + snapshot.BestYesAsk) / 2.0;
            if (currentPrice >= 40 && currentPrice <= 60)
            {
                return Conditions_UncertaintySignal.High;
            }
            return Conditions_UncertaintySignal.Low;
        }

        private Conditions_MarketCategory GetMarketCategory(MarketSnapshot snapshot)
        {
            string marketCategory = snapshot.MarketCategory.Replace(" ", "");
            Conditions_MarketCategory category;
            if (Enum.TryParse<Conditions_MarketCategory>(marketCategory, true, out category))
            {
                return category;
            }
            throw new Exception($"Unknown market category = {snapshot.MarketCategory}");
        }

        private MarketType ResolveMarketType(
            Conditions_PriceMovement priceMovement,
            Conditions_Liquidity liquidity,
            Conditions_ClosingTime closingTime,
            Conditions_ActivityLevel activityLevel,
            Conditions_UncertaintySignal uncertaintySignal,
            Conditions_MarketCategory marketCategory)
        {
            var currentConditions = new Dictionary<string, Enum>
            {
                ["PriceMovement"] = priceMovement,
                ["Liquidity"] = liquidity,
                ["ClosingTime"] = closingTime,
                ["ActivityLevel"] = activityLevel,
                ["UncertaintySignal"] = uncertaintySignal,
                ["MarketCategory"] = marketCategory
            };

            var matches = _marketTypeMap
                .Where(m => m.conditions.All(c => currentConditions[c.Key].Equals(c.Value)))
                .OrderByDescending(m => m.priority)
                .ThenByDescending(m => m.conditions.Count);

            var match = matches.FirstOrDefault();
            return match.conditions != null ? match.marketType : MarketType.Undefined;
        }
    }
}