namespace BacklashInterfaces.Enums
{
    /// <summary>
    /// Contains enumeration types used throughout the trading strategy system
    /// for defining market conditions, actions, and various trading parameters.
    /// </summary>
    public class StrategyEnums
    {

        public enum MarketType
        {
            /// <summary>
            /// Default for unmapped or undefined market conditions
            /// </summary>
            Undefined,
            /// <summary>
            /// Market with low liquidity, difficult to trade without impacting prices
            /// </summary>
            LowLiquidity,
            /// <summary>
            /// Market with prices oscillating within a range, suitable for mean-reversion strategies
            /// </summary>
            Bouncing,
            /// <summary>
            /// Market with consistent price movement in one direction, suitable for momentum strategies
            /// </summary>
            Trending,
            /// <summary>
            /// Market is experiencing significant fluctuations
            /// </summary>
            Volatile,
            /// <summary>
            /// Market with minimal price movement, low activity, and stable conditions
            /// </summary>
            Stagnant,
            /// <summary>
            /// Market with high uncertainty, often in balanced or unclear outcomes
            /// </summary>
            HighUncertainty,
            /// <summary>
            /// Trending market with high activity levels
            /// </summary>
            TrendingActive,
            /// <summary>
            /// Volatile market driven by specific events
            /// </summary>
            VolatileEvents,
            /// <summary>
            /// Stable market influenced by macroeconomic factors
            /// </summary>
            StableMacro,
            /// <summary>
            /// Volatile market with seasonal or predictable patterns
            /// </summary>
            SeasonalVolatile,
            /// <summary>
            /// Market driven by news and sentiment
            /// </summary>
            NewsDriven,
            /// <summary>
            /// Trending market in technology or innovation sectors
            /// </summary>
            TechTrend,
            /// <summary>
            /// Momentum-driven market in financial assets
            /// </summary>
            FinancialMomentum,
            /// <summary>
            /// Market close to resolution with high volatility
            /// </summary>
            ImminentCloseVolatile,
            /// <summary>
            /// Long-term stable market
            /// </summary>
            FarStable,
            /// <summary>
            /// Event-specific market like awards or launches
            /// </summary>
            EventDriven
        }

        public enum ActionType
        {
            /// <summary>
            /// No action to be taken
            /// </summary>
            None,
            /// <summary>
            /// Exit an existing position
            /// </summary>
            Exit,
            /// <summary>
            /// Enter a long position (buy)
            /// </summary>
            Long,
            /// <summary>
            /// Enter a short position (sell)
            /// </summary>
            Short,
            /// <summary>
            /// Limit buy yes (or equiv)
            /// </summary>
            PostYes,
            /// <summary>
            /// Limit sell yes
            /// </summary>
            PostAsk,
            /// <summary>
            /// Cancel resting
            /// </summary>
            Cancel,
            /// <summary>Market long, then immediately post a YES ask (resting sell) for what you just bought</summary>
            LongPostAsk,
            /// <summary>Market short, then immediately post a YES bid (resting buy) for what you just sold</summary>
            ShortPostYes
        }

        public enum ComparisonOperator
        {
            /// <summary>
            /// Condition where value is greater than a threshold
            /// </summary>
            GreaterThan,
            /// <summary>
            /// Condition where value is less than a threshold
            /// </summary>
            LessThan,
            /// <summary>
            /// Condition where value is equal to a threshold
            /// </summary>
            EqualTo,
            /// <summary>
            /// Condition where value is greater than or equal to a threshold
            /// </summary>
            GreaterThanOrEqual,
            /// <summary>
            /// Condition where value is less than or equal to a threshold
            /// </summary>
            LessThanOrEqual,
            /// <summary>
            /// Condition where a value exists
            /// </summary>
            Exists,
            /// <summary>
            /// Condition where a value does not exist
            /// </summary>
            NotExists
        }

        public enum Conditions_PriceMovement
        {
            /// <summary>
            /// Price is consistently moving in one direction (up or down)
            /// </summary>
            Trending,
            /// <summary>
            /// Price is oscillating within a defined range without a clear trend
            /// </summary>
            Bouncing,
            /// <summary>
            /// Price is experiencing significant fluctuations
            /// </summary>
            Volatile,
            /// <summary>
            /// Price is relatively steady with minimal movement
            /// </summary>
            Stable
        }

        public enum Conditions_Liquidity
        {
            /// <summary>
            /// Easy to trade without significantly affecting prices
            /// </summary>
            High,
            /// <summary>
            /// Difficult to trade without affecting prices
            /// </summary>
            Low,
            /// <summary>
            /// Average liquidity, neither particularly high nor low
            /// </summary>
            Moderate
        }

        public enum Conditions_ClosingTime
        {
            /// <summary>
            /// A long time remains until expiry (e.g., weeks or months)
            /// </summary>
            Far,
            /// <summary>
            /// Close to expiry (e.g., days or hours)
            /// </summary>
            Near,
            /// <summary>
            /// Very close to expiry (e.g., minutes or hours)
            /// </summary>
            Imminent
        }

        public enum Conditions_ActivityLevel
        {
            /// <summary>
            /// Lots of trading activity (e.g., high volume or frequent trades)
            /// </summary>
            High,
            /// <summary>
            /// Little trading activity (e.g., low volume or infrequent trades)
            /// </summary>
            Low,
            /// <summary>
            /// Average trading activity, neither particularly high nor low
            /// </summary>
            Moderate
        }

        public enum Conditions_UncertaintySignal
        {
            /// <summary>
            /// Outcome is unclear or highly uncertain
            /// </summary>
            High,
            /// <summary>
            /// Outcome is relatively clear or certain
            /// </summary>
            Low
        }


        public enum Conditions_MarketCategory
        {
            /// <summary>
            /// Markets related to climate and weather conditions.
            /// </summary>
            ClimateAndWeather,

            /// <summary>
            /// Markets related to companies and corporate events.
            /// </summary>
            Companies,

            /// <summary>
            /// Markets related to COVID-19 and pandemic conditions.
            /// </summary>
            COVID19,

            /// <summary>
            /// Markets related to cryptocurrency and blockchain.
            /// </summary>
            Crypto,

            /// <summary>
            /// Markets related to economic indicators and conditions.
            /// </summary>
            Economics,

            /// <summary>
            /// Markets related to education and academic topics.
            /// </summary>
            Education,

            /// <summary>
            /// Markets related to elections and political voting.
            /// </summary>
            Elections,

            /// <summary>
            /// Markets related to entertainment and media.
            /// </summary>
            Entertainment,

            /// <summary>
            /// Markets related to financial markets and instruments.
            /// </summary>
            Financials,

            /// <summary>
            /// Markets related to health and medical topics.
            /// </summary>
            Health,

            /// <summary>
            /// Markets related to mentions and social media buzz.
            /// </summary>
            Mentions,

            /// <summary>
            /// Markets related to politics and government.
            /// </summary>
            Politics,

            /// <summary>
            /// Markets related to science and technology.
            /// </summary>
            ScienceAndTechnology,

            /// <summary>
            /// Markets related to social issues and culture.
            /// </summary>
            Social,

            /// <summary>
            /// Markets related to sports and athletics.
            /// </summary>
            Sports,

            /// <summary>
            /// Markets related to transportation and travel.
            /// </summary>
            Transportation,

            /// <summary>
            /// Markets related to world events and global topics.
            /// </summary>
            World
        }
    }
}
