using System;
using BacklashDTOs;
using TradingStrategies.Strategies;
using static BacklashInterfaces.Enums.StrategyEnums;
using static TradingStrategies.Trading.Overseer.ReportGenerator;

namespace TradingStrategies.Trading.Overseer
{
    /// <summary>
    /// Represents the complete state of a single trading simulation path in the Kalshi trading bot system.
    /// This class encapsulates all the essential data needed to track the evolution of a trading strategy
    /// simulation over time, including position, cash balance, risk metrics, and order book state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SimulationPath serves as the core data container for the trading simulation engine, maintaining
    /// the state that evolves as strategies are executed against historical market data. It provides
    /// a snapshot of the trading account at any point during the simulation, enabling accurate
    /// performance evaluation and risk assessment.
    /// </para>
    /// <para>
    /// Key responsibilities include:
    /// - Tracking current position (positive for long, negative for short)
    /// - Maintaining cash balance and cumulative trading costs
    /// - Storing strategy configurations by market conditions
    /// - Managing simulated order book and resting orders
    /// - Recording simulation events for analysis
    /// - Calculating derived metrics like average entry price
    /// </para>
    /// <para>
    /// This class is used extensively by the SimulationEngine for branching simulations,
    /// by EquityCalculator for portfolio valuation, and by TradingOverseer for performance reporting.
    /// The immutable StrategiesByMarketConditions ensures consistent strategy application throughout
    /// the simulation lifecycle.
    /// </para>
    /// </remarks>
    public class SimulationPath
    {
        /// <summary>
        /// Gets the mapping of market conditions to available trading strategies.
        /// This dictionary determines which strategies are eligible for execution based on current market type.
        /// </summary>
        /// <value>
        /// A read-only dictionary where keys are <see cref="MarketType"/> values and values are
        /// sets of <see cref="Strategy"/> objects that can be applied in those market conditions.
        /// </value>
        /// <remarks>
        /// This mapping is established at simulation initialization and remains constant throughout
        /// the simulation. It enables the simulation engine to select appropriate strategies
        /// based on detected market conditions, supporting adaptive trading behavior.
        /// </remarks>
        public Dictionary<MarketType, HashSet<Strategy>> StrategiesByMarketConditions { get; }

        /// <summary>
        /// Gets or sets the current position size in the simulated market.
        /// </summary>
        /// <value>
        /// Positive values indicate long positions, negative values indicate short positions.
        /// Zero indicates no position. The magnitude represents the number of contracts held.
        /// </value>
        /// <remarks>
        /// This value is updated by the simulation engine as trades are executed. It directly
        /// affects equity calculations and risk assessments throughout the simulation.
        /// </remarks>
        private int _position;
        public int Position
        {
            get => _position;
            set
            {
                _position = value;
            }
        }

        /// <summary>
        /// Gets or sets the current cash balance available for trading.
        /// </summary>
        /// <value>The cash balance in dollars, updated after each trade execution.</value>
        /// <remarks>
        /// Cash is reduced when buying positions and increased when selling. Trading fees
        /// are also deducted from this balance. This value is critical for determining
        /// buying power and overall portfolio value. This property cannot be set to a negative value.
        /// </remarks>
        private double _cash;
        public double Cash
        {
            get => _cash;
            set
            {
                if (value < 0)
                    throw new ArgumentException("Cash cannot be negative.", nameof(Cash));
                _cash = value;
            }
        }

        /// <summary>
        /// Gets or sets the current risk level of the position.
        /// </summary>
        /// <value>The risk metric, defaulting to 0.0. Higher values indicate greater exposure.</value>
        /// <remarks>
        /// This property tracks the risk associated with the current position. It may be
        /// calculated based on position size, volatility, or other risk factors. Used by
        /// risk management components to enforce position limits. This property cannot be set to a negative value.
        /// </remarks>
        private double _currentRisk = 0.0;
        public double CurrentRisk
        {
            get => _currentRisk;
            set
            {
                if (value < 0)
                    throw new ArgumentException("CurrentRisk cannot be negative.", nameof(CurrentRisk));
                _currentRisk = value;
            }
        }

        /// <summary>
        /// Gets or sets the cumulative amount paid for establishing long positions.
        /// </summary>
        /// <value>The total dollars paid for buying contracts in long positions.</value>
        /// <remarks>
        /// This accumulator tracks all cash outflows for position entries. Combined with
        /// <see cref="TotalReceived"/>, it enables calculation of net trading costs and
        /// average entry prices for performance analysis.
        /// </remarks>
        private double _totalPaid = 0.0;
        public double TotalPaid
        {
            get => _totalPaid;
            set
            {
                _totalPaid = value;
            }
        }

        /// <summary>
        /// Gets or sets the cumulative amount received from establishing short positions.
        /// </summary>
        /// <value>The total dollars received from selling contracts in short positions.</value>
        /// <remarks>
        /// This accumulator tracks all cash inflows from position entries. For short positions,
        /// this represents the entry price received when selling. Used alongside
        /// <see cref="TotalPaid"/> for comprehensive cost tracking.
        /// </remarks>
        private double _totalReceived = 0.0;
        public double TotalReceived
        {
            get => _totalReceived;
            set
            {
                _totalReceived = value;
            }
        }

        /// <summary>
        /// Gets or sets the list of events that occurred during the simulation.
        /// </summary>
        /// <value>A list of <see cref="ReportGenerator.SimulationEventLog"/> entries recording simulation events.</value>
        /// <remarks>
        /// Events capture significant occurrences during simulation such as trades, strategy
        /// decisions, market condition changes, and errors. This log is used for performance
        /// analysis, debugging, and generating detailed simulation reports.
        /// </remarks>
        public List<SimulationEventLog> Events { get; set; }

        /// <summary>
        /// Gets or sets the simulated order book representing current market liquidity.
        /// </summary>
        /// <value>
        /// A <see cref="SimulatedOrderbook"/> instance containing bid/ask levels, or null if
        /// no order book has been initialized for this simulation path.
        /// </value>
        /// <remarks>
        /// The simulated order book tracks available liquidity at different price levels.
        /// It evolves during simulation as orders are placed, filled, and cancelled.
        /// Used by the simulation engine for realistic trade execution and by equity
        /// calculator for position valuation.
        /// </remarks>
        public SimulatedOrderbook? SimulatedBook { get; set; }

        /// <summary>
        /// Gets or sets the list of resting orders currently active in the simulation.
        /// </summary>
        /// <value>
        /// A list of tuples representing resting limit orders, where each tuple contains:
        /// - action: "buy" or "sell"
        /// - side: "yes" or "no"
        /// - type: order type (typically "limit")
        /// - count: number of contracts
        /// - price: limit price (1-99)
        /// - expiration: optional expiration timestamp
        /// </value>
        /// <remarks>
        /// Resting orders represent unfilled limit orders that remain in the order book.
        /// They can be filled by market movements or cancelled by strategy decisions.
        /// This list is maintained in parallel with the simulated order book for consistency.
        /// </remarks>
        public List<(string action, string side, string type, int count, int price, DateTime? expiration)> SimulatedRestingOrders { get; set; } = new List<(string, string, string, int, int, DateTime?)>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SimulationPath"/> class.
        /// </summary>
        /// <param name="strategiesByMarketConditions">
        /// The mapping of market conditions to available strategies that will be used throughout the simulation.
        /// </param>
        /// <param name="position">The initial position size (positive for long, negative for short).</param>
        /// <param name="cash">The initial cash balance in dollars.</param>
        /// <remarks>
        /// Creates a new simulation path with the specified strategy configuration and starting conditions.
        /// The strategy mapping is stored as read-only to ensure consistency throughout the simulation.
        /// Input validation is performed to ensure strategiesByMarketConditions is not null and cash is non-negative.
        /// Other properties are initialized to default values and will be updated as the simulation progresses.
        /// </remarks>
        public SimulationPath(Dictionary<MarketType, HashSet<Strategy>> strategiesByMarketConditions, int position, double cash)
        {
            if (strategiesByMarketConditions == null)
                throw new ArgumentNullException(nameof(strategiesByMarketConditions));
            if (cash < 0)
                throw new ArgumentException("Cash cannot be negative.", nameof(cash));
            StrategiesByMarketConditions = strategiesByMarketConditions;
            _position = position;
            _cash = cash;
            _currentRisk = 0.0;
            _totalPaid = 0.0;
            _totalReceived = 0.0;
            Events = new List<SimulationEventLog>();
        }

        /// <summary>
        /// Gets the average entry price for the current position, regardless of direction.
        /// </summary>
        /// <value>
        /// The average price paid/received per contract for the current position.
        /// Returns 0.0 if no position is held.
        /// </value>
        /// <remarks>
        /// <para>
        /// This calculated property provides the average cost basis for the current position,
        /// which is essential for performance analysis and risk management. The calculation
        /// differs based on position direction:
        /// </para>
        /// <para>
        /// For long positions (positive): Returns TotalPaid divided by position size,
        /// representing the average price paid to acquire the contracts.
        /// </para>
        /// <para>
        /// For short positions (negative): Returns TotalReceived divided by absolute position size,
        /// representing the average price received when selling the contracts (entry price for shorts).
        /// </para>
        /// <para>
        /// This metric is used by trading strategies and performance reports to assess
        /// entry efficiency and calculate unrealized gains/losses relative to entry price.
        /// </para>
        /// </remarks>
        public double AverageCost
        {
            get
            {
                if (Position > 0)
                {
                    // Long position: average price paid to buy
                    return TotalPaid / Position;
                }
                else if (Position < 0)
                {
                    // Short position: average price received from selling (entry price for short)
                    double avgEntryPrice = TotalReceived / Math.Abs(Position);
                    return avgEntryPrice > 0 ? avgEntryPrice : 0.0;
                }
                return 0.0;
            }
        }

        /// <summary>
        /// Creates a deep clone of the current SimulationPath instance.
        /// </summary>
        /// <returns>A new SimulationPath instance with deep-copied collections.</returns>
        public SimulationPath Clone()
        {
            var clonedStrategies = StrategiesByMarketConditions.ToDictionary(
                kvp => kvp.Key,
                kvp => new HashSet<Strategy>(kvp.Value)
            );
            var clonedEvents = new List<SimulationEventLog>(Events);
            var clonedRestingOrders = new List<(string, string, string, int, int, DateTime?)>(SimulatedRestingOrders);
            var clonedBook = SimulatedBook; // Shallow copy
            return new SimulationPath(clonedStrategies, _position, _cash)
            {
                CurrentRisk = _currentRisk,
                TotalPaid = _totalPaid,
                TotalReceived = _totalReceived,
                Events = clonedEvents,
                SimulatedBook = clonedBook,
                SimulatedRestingOrders = clonedRestingOrders
            };
        }
    }
}
