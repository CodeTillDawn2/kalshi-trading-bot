namespace BacklashInterfaces.Enums
{
    /// <summary>
    /// Contains enumeration types used throughout the trading strategy system
    /// for defining market conditions, actions, and various trading parameters.
    /// </summary>
    public class StrategyEnums
    {

        /// <summary>
        /// Defines different types of market conditions and behaviors for trading strategies.
        /// </summary>
        public enum MarketType
        {
            /// <summary>
            /// Represents an undefined or unmapped market condition, used as a default value when the market type cannot be determined.
            /// </summary>
            Undefined,
            /// <summary>
            /// Represents a market with low liquidity, where trading large volumes can significantly impact prices due to limited market depth.
            /// </summary>
            LowLiquidity,
            /// <summary>
            /// Represents a market with prices oscillating within a defined range, making it suitable for mean-reversion trading strategies.
            /// </summary>
            Bouncing,
            /// <summary>
            /// Represents a market with consistent price movement in one direction, ideal for momentum-based trading strategies.
            /// </summary>
            Trending,
            /// <summary>
            /// Represents a market experiencing significant price fluctuations and high volatility.
            /// </summary>
            Volatile,
            /// <summary>
            /// Represents a market with minimal price movement, low trading activity, and stable conditions.
            /// </summary>
            Stagnant,
            /// <summary>
            /// Represents a market with high uncertainty, often characterized by balanced probabilities or unclear outcomes.
            /// </summary>
            HighUncertainty,
            /// <summary>
            /// Represents a trending market with high levels of trading activity and volume.
            /// </summary>
            TrendingActive,
            /// <summary>
            /// Represents a volatile market driven by specific events or news catalysts.
            /// </summary>
            VolatileEvents,
            /// <summary>
            /// Represents a stable market influenced by macroeconomic factors and indicators.
            /// </summary>
            StableMacro,
            /// <summary>
            /// Represents a volatile market with seasonal or predictable patterns in price movement.
            /// </summary>
            SeasonalVolatile,
            /// <summary>
            /// Represents a market primarily driven by news events and market sentiment.
            /// </summary>
            NewsDriven,
            /// <summary>
            /// Represents a trending market in technology or innovation-related sectors.
            /// </summary>
            TechTrend,
            /// <summary>
            /// Represents a momentum-driven market in financial assets and instruments.
            /// </summary>
            FinancialMomentum,
            /// <summary>
            /// Represents a market close to resolution with high volatility due to impending settlement.
            /// </summary>
            ImminentCloseVolatile,
            /// <summary>
            /// Represents a long-term stable market with predictable behavior over extended periods.
            /// </summary>
            FarStable,
            /// <summary>
            /// Represents an event-specific market such as awards, launches, or one-time occurrences.
            /// </summary>
            EventDriven

        }
        /// <summary>
        /// Defines different types of trading actions that can be taken in response to market conditions.
        /// </summary>
        public enum ActionType
        {
            /// <summary>
            /// Indicates that no trading action should be taken at this time.
            /// </summary>
            None,
            /// <summary>
            /// Indicates the action to exit an existing trading position completely.
            /// </summary>
            Exit,
            /// <summary>
            /// Indicates the action to enter a long position by buying the asset.
            /// </summary>
            Long,
            /// <summary>
            /// Indicates the action to enter a short position by selling the asset.
            /// </summary>
            Short,
            /// <summary>
            /// Indicates the action to place a limit buy order for the 'yes' outcome.
            /// </summary>
            PostYes,
            /// <summary>
            /// Indicates the action to place a limit sell order for the 'yes' outcome.
            /// </summary>
            PostAsk,
            /// <summary>
            /// Indicates the action to cancel any resting orders in the market.
            /// </summary>
            Cancel,
            /// <summary>
            /// Indicates a market long position followed by immediately posting a resting sell order for the purchased amount.
            /// </summary>
            LongPostAsk,
            /// <summary>
            /// Indicates a market short position followed by immediately posting a resting buy order for the sold amount.
            /// </summary>
            ShortPostYes
        }

        /// <summary>
        /// Defines different types of comparison operators used in trading strategy conditions.
        /// </summary>
        public enum ComparisonOperator
        {
            /// <summary>
            /// Represents a condition where a value is greater than a specified threshold.
            /// </summary>
            GreaterThan,
            /// <summary>
            /// Represents a condition where a value is less than a specified threshold.
            /// </summary>
            LessThan,
            /// <summary>
            /// Represents a condition where a value is equal to a specified threshold.
            /// </summary>
            EqualTo,
            /// <summary>
            /// Represents a condition where a value is greater than or equal to a specified threshold.
            /// </summary>
            GreaterThanOrEqual,
            /// <summary>
            /// Represents a condition where a value is less than or equal to a specified threshold.
            /// </summary>
            LessThanOrEqual,
            /// <summary>
            /// Represents a condition where a value exists or is present.
            /// </summary>
            Exists,
            /// <summary>
            /// Represents a condition where a value does not exist or is absent.
            /// </summary>
            NotExists
        }

        /// <summary>
        /// Defines different types of price movement conditions for trading strategies.
        /// </summary>
        public enum Conditions_PriceMovement
        {
            /// <summary>
            /// Indicates that the price is consistently moving in one direction, either upward or downward.
            /// </summary>
            Trending,
            /// <summary>
            /// Indicates that the price is oscillating within a defined range without establishing a clear trend.
            /// </summary>
            Bouncing,
            /// <summary>
            /// Indicates that the price is experiencing significant and rapid fluctuations.
            /// </summary>
            Volatile,
            /// <summary>
            /// Indicates that the price is relatively steady with minimal movement or changes.
            /// </summary>
            Stable
        }

        /// <summary>
        /// Defines different levels of market liquidity conditions for trading strategies.
        /// </summary>
        public enum Conditions_Liquidity
        {
            /// <summary>
            /// Indicates high liquidity, making it easy to trade large volumes without significantly affecting prices.
            /// </summary>
            High,
            /// <summary>
            /// Indicates low liquidity, making it difficult to trade without impacting market prices.
            /// </summary>
            Low,
            /// <summary>
            /// Indicates moderate liquidity, providing average trading conditions neither particularly high nor low.
            /// </summary>
            Moderate
        }

        /// <summary>
        /// Defines different time-based conditions relative to market closing for trading strategies.
        /// </summary>
        public enum Conditions_ClosingTime
        {
            /// <summary>
            /// Indicates that a long time remains until market expiry, such as weeks or months.
            /// </summary>
            Far,
            /// <summary>
            /// Indicates that the market is close to expiry, such as days or hours remaining.
            /// </summary>
            Near,
            /// <summary>
            /// Indicates that the market is very close to expiry, such as minutes or hours remaining.
            /// </summary>
            Imminent
        }

        /// <summary>
        /// Defines different levels of market activity conditions for trading strategies.
        /// </summary>
        public enum Conditions_ActivityLevel
        {
            /// <summary>
            /// Indicates high trading activity, characterized by high volume or frequent trades.
            /// </summary>
            High,
            /// <summary>
            /// Indicates low trading activity, characterized by low volume or infrequent trades.
            /// </summary>
            Low,
            /// <summary>
            /// Indicates moderate trading activity, neither particularly high nor low in volume or frequency.
            /// </summary>
            Moderate
        }

        /// <summary>
        /// Defines different levels of market uncertainty conditions for trading strategies.
        /// </summary>
        public enum Conditions_UncertaintySignal
        {
            /// <summary>
            /// Indicates that the market outcome is unclear or highly uncertain.
            /// </summary>
            High,
            /// <summary>
            /// Indicates that the market outcome is relatively clear or certain.
            /// </summary>
            Low
        }

        /// <summary>
        /// Defines different categories of markets for trading strategies.
        /// </summary>
        public enum Conditions_MarketCategory
        {
            /// <summary>
            /// Represents markets related to climate and weather conditions and predictions.
            /// </summary>
            ClimateAndWeather,

            /// <summary>
            /// Represents markets related to companies and corporate events or performance.
            /// </summary>
            Companies,

            /// <summary>
            /// Represents markets related to COVID-19 and pandemic conditions and impacts.
            /// </summary>
            COVID19,

            /// <summary>
            /// Represents markets related to cryptocurrency and blockchain technology.
            /// </summary>
            Crypto,

            /// <summary>
            /// Represents markets related to economic indicators and macroeconomic conditions.
            /// </summary>
            Economics,

            /// <summary>
            /// Represents markets related to education and academic topics or events.
            /// </summary>
            Education,

            /// <summary>
            /// Represents markets related to elections and political voting outcomes.
            /// </summary>
            Elections,

            /// <summary>
            /// Represents markets related to entertainment and media industry events.
            /// </summary>
            Entertainment,

            /// <summary>
            /// Represents markets related to financial markets and investment instruments.
            /// </summary>
            Financials,

            /// <summary>
            /// Represents markets related to health and medical topics or advancements.
            /// </summary>
            Health,

            /// <summary>
            /// Represents markets related to mentions and social media buzz or trends.
            /// </summary>
            Mentions,

            /// <summary>
            /// Represents markets related to politics and government actions or policies.
            /// </summary>
            Politics,

            /// <summary>
            /// Represents markets related to science and technology innovations or discoveries.
            /// </summary>
            ScienceAndTechnology,

            /// <summary>
            /// Represents markets related to social issues and cultural topics or events.
            /// </summary>
            Social,

            /// <summary>
            /// Represents markets related to sports and athletic competitions or events.
            /// </summary>
            Sports,

            /// <summary>
            /// Represents markets related to transportation and travel industry conditions.
            /// </summary>
            Transportation,

            /// <summary>
            /// Represents markets related to world events and global topics or occurrences.
            /// </summary>
            World
        }
    }
}
