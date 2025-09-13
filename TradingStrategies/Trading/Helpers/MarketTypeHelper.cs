using BacklashDTOs;
using static BacklashInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Trading.Helpers
{
    /// <summary>
    /// Provides functionality to classify market types based on various trading conditions extracted from market snapshots.
    /// This helper uses a rule-based system with priority ordering to determine the most appropriate market type
    /// for a given set of market conditions, enabling trading strategies to adapt their behavior accordingly.
    /// </summary>
    /// <remarks>
    /// The classification process involves:
    /// 1. Extracting condition values (price movement, liquidity, closing time, etc.) from a MarketSnapshot
    /// 2. Matching these conditions against predefined rules with associated priorities
    /// 3. Returning the highest-priority matching market type
    ///
    /// This class is instantiated as needed and used in simulation and analysis pipelines to categorize
    /// market states for strategy selection and performance evaluation.
    /// </remarks>
    public class MarketTypeHelper
    {
        /// <summary>
        /// Stores the collection of market type mappings, each containing condition requirements,
        /// the resulting market type, and a priority level for conflict resolution.
        /// </summary>
        private readonly List<(Dictionary<string, Enum> conditions, MarketType marketType, int priority)> _marketTypeMappings;

        /// <summary>
        /// Initializes a new instance of the MarketTypeHelper class.
        /// </summary>
        /// <remarks>
        /// Sets up the internal mappings collection and populates it with predefined market type rules
        /// based on various combinations of market conditions and their associated priorities.
        /// </remarks>
        public MarketTypeHelper()
        {
            _marketTypeMappings = new List<(Dictionary<string, Enum>, MarketType, int)>();
            InitializeMarketTypeMap();
        }

        /// <summary>
        /// Adds a market type mapping using individual condition parameters for convenience.
        /// </summary>
        /// <param name="marketType">The market type to assign when conditions are met.</param>
        /// <param name="priceMovement">Optional price movement condition requirement.</param>
        /// <param name="liquidity">Optional liquidity condition requirement.</param>
        /// <param name="closingTime">Optional closing time condition requirement.</param>
        /// <param name="activityLevel">Optional activity level condition requirement.</param>
        /// <param name="uncertaintySignal">Optional uncertainty signal condition requirement.</param>
        /// <param name="marketCategory">Optional market category condition requirement.</param>
        /// <param name="priority">Priority level for this mapping (higher values take precedence in conflicts).</param>
        /// <remarks>
        /// This method provides a convenient way to add mappings by specifying conditions as individual parameters
        /// rather than as a collection. Only non-null conditions are included in the mapping. This is the primary
        /// method used in InitializeMarketTypeMap for setting up the predefined rules.
        /// </remarks>
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

            _marketTypeMappings.Add((conditions, marketType, priority));
        }

        /// <summary>
        /// Adds a market type mapping using a collection of condition-value pairs.
        /// </summary>
        /// <param name="marketType">The market type to assign when conditions are met.</param>
        /// <param name="conditions">Collection of condition names and their required enum values.</param>
        /// <param name="priority">Priority level for this mapping (higher values take precedence in conflicts).</param>
        /// <remarks>
        /// This method allows for more flexible condition specification compared to AddBroadMapping,
        /// supporting any combination of conditions as key-value pairs. The conditions are converted
        /// to a dictionary for efficient matching during resolution.
        /// </remarks>
        public void AddPriorityMapping(
            MarketType marketType,
            IEnumerable<(string condition, Enum value)> conditions,
            int priority = 0)
        {
            var conditionDict = conditions.ToDictionary(c => c.condition, c => c.value);
            _marketTypeMappings.Add((conditionDict, marketType, priority));
        }

        /// <summary>
        /// Initializes the market type mappings with predefined rules based on domain knowledge.
        /// </summary>
        /// <remarks>
        /// This method sets up a comprehensive set of market type classifications that combine various
        /// market conditions (price movement, liquidity, activity, etc.) with specific market categories.
        /// Each mapping includes a priority level to handle conflicts when multiple rules match.
        ///
        /// The rules are organized by:
        /// - Basic price movement patterns (Bouncing, Trending, Volatile, Stagnant)
        /// - Liquidity conditions
        /// - Uncertainty and activity combinations
        /// - Category-specific rules for different market types (Politics, Economics, Crypto, etc.)
        /// - Time-sensitive conditions (closing time proximity)
        ///
        /// Higher priority values (e.g., 25 for imminent close volatile) ensure critical conditions
        /// take precedence over general patterns.
        /// </remarks>
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

        /// <summary>
        /// Determines the market type for a given market snapshot by extracting conditions and resolving them against mappings.
        /// </summary>
        /// <param name="snapshot">The market snapshot containing all relevant trading data and indicators.</param>
        /// <returns>The most appropriate market type based on current conditions, or MarketType.Undefined if no match is found.</returns>
        /// <remarks>
        /// This is the primary public method for market type classification. It extracts six key condition types
        /// from the snapshot (price movement, liquidity, closing time, activity level, uncertainty signal, and market category)
        /// and uses them to find the best matching market type rule based on priority and condition specificity.
        ///
        /// The method is designed to be called during market analysis, simulation, and strategy selection phases.
        /// </remarks>
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

        /// <summary>
        /// Extracts the price movement condition from a market snapshot using Bollinger Band analysis.
        /// </summary>
        /// <param name="snapshot">The market snapshot containing price and Bollinger Band data.</param>
        /// <returns>The appropriate price movement condition based on current price relative to Bollinger Bands.</returns>
        /// <remarks>
        /// Analyzes price movement by:
        /// 1. Calculating current price as midpoint between best bid and ask
        /// 2. Checking if price is outside Bollinger Bands (indicating trending)
        /// 3. If within bands, evaluating band width for volatility/stable/bouncing classification
        ///
        /// Band width thresholds:
        /// - > 0.1: Volatile (wide bands indicate high volatility)
        /// - < 0.05: Stable (narrow bands indicate low volatility)
        /// - 0.05-0.1: Bouncing (moderate volatility with price oscillation)
        ///
        /// Falls back to Stable if Bollinger Band data is unavailable.
        /// </remarks>
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

        /// <summary>
        /// Extracts the liquidity condition from a market snapshot using the calculated liquidity score.
        /// </summary>
        /// <param name="snapshot">The market snapshot containing liquidity calculation data.</param>
        /// <returns>The appropriate liquidity condition based on the computed liquidity score.</returns>
        /// <remarks>
        /// Uses the snapshot's CalculateLiquidityScore() method which evaluates multiple factors
        /// including spread, depth, volume, and slippage estimates. The score is categorized as:
        ///
        /// - High: Score > 80 (excellent liquidity, easy to trade large volumes)
        /// - Low: Score < 30 (poor liquidity, difficult to trade without price impact)
        /// - Moderate: Score 30-80 (acceptable liquidity for normal trading)
        ///
        /// This condition helps identify markets where trading costs and execution quality vary significantly.
        /// </remarks>
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

        /// <summary>
        /// Extracts the closing time condition from a market snapshot based on time remaining until market resolution.
        /// </summary>
        /// <param name="snapshot">The market snapshot containing time-to-close information.</param>
        /// <returns>The appropriate closing time condition based on days remaining.</returns>
        /// <remarks>
        /// Categorizes time pressure based on days until market close:
        ///
        /// - Far: > 14 days (long-term market, stable conditions expected)
        /// - Near: 1-14 days (medium-term, some urgency but not immediate)
        /// - Imminent: < 1 day (short-term, high urgency, potential volatility spikes)
        ///
        /// Markets without time data (e.g., perpetual markets) default to Far.
        /// This condition is crucial for strategies that need to account for time decay and closing volatility.
        /// </remarks>
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

        /// <summary>
        /// Extracts the activity level condition from a market snapshot based on trade frequency.
        /// </summary>
        /// <param name="snapshot">The market snapshot containing trade rate metrics.</param>
        /// <returns>The appropriate activity level condition based on combined yes/no trade rates.</returns>
        /// <remarks>
        /// Measures market activity by summing trade rates for both sides (yes and no contracts).
        /// Activity levels are categorized as:
        ///
        /// - High: > 5 trades/minute (very active market with frequent trading)
        /// - Low: < 1 trade/minute (quiet market with infrequent trading)
        /// - Moderate: 1-5 trades/minute (normal activity levels)
        ///
        /// This condition helps identify markets with different liquidity and momentum characteristics,
        /// where high activity often correlates with better execution but potentially higher volatility.
        /// </remarks>
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

        /// <summary>
        /// Extracts the uncertainty signal condition from a market snapshot based on current price position.
        /// </summary>
        /// <param name="snapshot">The market snapshot containing current price information.</param>
        /// <returns>The appropriate uncertainty signal condition based on price proximity to 50% (fair odds).</returns>
        /// <remarks>
        /// Determines market uncertainty by checking if the current price (midpoint of bid/ask)
        /// falls within the 40-60 range, indicating high uncertainty about the outcome.
        ///
        /// - High: Price between 40-60 (market is uncertain, close to 50/50 probability)
        /// - Low: Price outside 40-60 (market has clearer directional bias)
        ///
        /// This simple but effective heuristic identifies markets where participants have
        /// roughly equal confidence in yes/no outcomes, often indicating breaking news or
        /// rapidly changing conditions.
        /// </remarks>
        private Conditions_UncertaintySignal GetUncertaintySignal(MarketSnapshot snapshot)
        {
            double currentPrice = (snapshot.BestYesBid + snapshot.BestYesAsk) / 2.0;
            if (currentPrice >= 40 && currentPrice <= 60)
            {
                return Conditions_UncertaintySignal.High;
            }
            return Conditions_UncertaintySignal.Low;
        }

        /// <summary>
        /// Extracts the market category condition from a market snapshot by parsing the category string.
        /// </summary>
        /// <param name="snapshot">The market snapshot containing market category information.</param>
        /// <returns>The parsed market category enum value.</returns>
        /// <exception cref="Exception">Thrown when the market category string cannot be parsed to a known enum value.</exception>
        /// <remarks>
        /// Converts the snapshot's MarketCategory string to the corresponding Conditions_MarketCategory enum.
        /// The string is normalized by removing spaces before parsing. Case-insensitive parsing is used.
        ///
        /// Supported categories include Politics, Economics, Crypto, Sports, Entertainment, etc.
        /// This condition enables category-specific market type rules that account for different
        /// behavioral patterns across market types (e.g., crypto markets tend to be more volatile).
        ///
        /// Throws an exception for unrecognized categories to prevent silent failures in classification.
        /// </remarks>
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

        /// <summary>
        /// Resolves the market type by matching current conditions against predefined mappings using priority-based selection.
        /// </summary>
        /// <param name="priceMovement">Current price movement condition.</param>
        /// <param name="liquidity">Current liquidity condition.</param>
        /// <param name="closingTime">Current closing time condition.</param>
        /// <param name="activityLevel">Current activity level condition.</param>
        /// <param name="uncertaintySignal">Current uncertainty signal condition.</param>
        /// <param name="marketCategory">Current market category condition.</param>
        /// <returns>The highest-priority matching market type, or MarketType.Undefined if no matches found.</returns>
        /// <remarks>
        /// The resolution algorithm:
        /// 1. Creates a condition dictionary from the input parameters
        /// 2. Filters mappings to those where all specified conditions match the current state
        /// 3. Orders matches by priority (descending), then by condition count (more specific rules first)
        /// 4. Returns the market type of the top match, or Undefined if no rules match
        ///
        /// This ensures that more specific, higher-priority rules override general patterns.
        /// For example, a market closing imminently with high volatility will be classified as
        /// ImminentCloseVolatile rather than just Volatile.
        /// </remarks>
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

            var matches = _marketTypeMappings
                .Where(m => m.conditions.All(c => currentConditions[c.Key].Equals(c.Value)))
                .OrderByDescending(m => m.priority)
                .ThenByDescending(m => m.conditions.Count);

            var match = matches.FirstOrDefault();
            return match.conditions != null ? match.marketType : MarketType.Undefined;
        }
    }
}
