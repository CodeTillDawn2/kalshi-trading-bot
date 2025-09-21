using BacklashBot.Configuration;
using BacklashBot.Helpers;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Data;
/// <summary>
/// Represents the comprehensive data model for a specific Kalshi market, aggregating real-time and historical market data
/// from WebSocket feeds, API responses, and calculated metrics. This class serves as the central hub for all market-related
/// information used in trading decisions, technical analysis, and snapshot creation.
///
/// The MarketData class maintains:
/// - Current order book state with bid/ask prices and depths
/// - Historical candlestick data across multiple timeframes (minute, hour, day)
/// - Real-time ticker updates and price movements
/// - Position information and P&amp;L calculations
/// - Technical indicators (RSI, MACD, Bollinger Bands, etc.)
/// - Order book change velocities and trade metrics
/// - Support/resistance levels and market metadata
///
/// Data integrity is ensured through comprehensive input validation:
/// - Candlestick data validation for non-negative prices (0-100), valid timestamps, and reasonable values
/// - Ticker data validation for price ranges, volume, and timestamp integrity
/// - Orderbook data validation for price bounds, quantities, and side validity
/// - Position data validation for size reasonableness, exposure bounds, and update timestamps
/// Invalid data triggers informative warnings via logging but processing continues to maintain system resilience.
///
/// Data is updated through various mechanisms:
/// - WebSocket events for real-time order book and ticker updates
/// - Periodic API calls for position and market information
/// - Asynchronous background calculations for technical indicators and metrics to avoid blocking
/// - Historical data loading from candlestick storage
///
/// Performance optimizations include:
/// - Efficient data structures and minimized LINQ queries
/// - Parallel processing for CPU-intensive calculations
/// - Binary search for large dataset lookups
/// - Asynchronous operations to maintain responsiveness
///
/// Calculation parameters such as tolerance percentages, fee rates, time periods for slope calculations,
/// and lookback periods are configurable via CalculationConfig to allow runtime customization without code changes.
///
/// This class implements the IMarketData interface and is used throughout the trading bot
/// for market analysis, strategy execution, and data persistence via MarketSnapshot serialization.
/// </summary>
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using TradingStrategies.Helpers.Interfaces;

namespace BacklashBot.State
{
    public class MarketData : IMarketData
    {
        private readonly MarketServiceDataConfig _marketDataConfig;

        private readonly ILogger<IMarketData> _logger;
        private readonly IOrderbookChangeTracker _changeTracker;
        private readonly ITradingCalculator _tradingCalculator;
        private string _marketTicker = "";
        private MarketDTO _marketInfo = null!;
        private readonly Dictionary<string, List<CandlestickData>> _candlesticks;
        private ConcurrentBag<TickerDTO> _tickers;
        private List<MarketPositionDTO> _positions;
        private List<OrderbookData> _orderbookData;
        private DateTime _lastWebSocketMessageReceived;
        private DateTime _lastOrderbookEventTimestamp;
        private DateTime _lastSnapshotTaken;
        private string _marketCategory = "";

        public string MarketCategory { get => _marketCategory; set => _marketCategory = value; }

        private string _marketStatus = "";
        public string MarketStatus
        {
            get => _marketStatus;
            set
            {
                if (_marketStatus != value)
                {
                    _marketStatus = value;
                    _logger.LogDebug("Market status updated for {MarketTicker}: {MarketStatus}", _marketTicker, _marketStatus);
                }
            }
        }

        public DateTime LastSuccessfulSync { get; set; } = DateTime.MinValue;

        private (int Ask, int Bid, DateTime When) _tickerPrice = (0, 0, DateTime.MinValue);
        private string _currentPriceSource = "Unknown";
        private (int Bid, DateTime When) _allTimeHighYesBid = (0, DateTime.MinValue);
        private (int Bid, DateTime When) _allTimeHighNoBid = (0, DateTime.MinValue);
        private (int Bid, DateTime When) _allTimeLowYesBid = (0, DateTime.MinValue);
        private (int Bid, DateTime When) _allTimeLowNoBid = (0, DateTime.MinValue);
        private (int Bid, DateTime When) _recentHighYesBid = (0, DateTime.MinValue);
        private (int Bid, DateTime When) _recentHighNoBid = (0, DateTime.MinValue);
        private (int Bid, DateTime When) _recentLowYesBid = (0, DateTime.MinValue);
        private (int Bid, DateTime When) _recentLowNoBid = (0, DateTime.MinValue);

        private string _goodBadPriceYes = "";
        private string _goodBadPriceNo = "";
        private string _marketBehaviorYes = "";
        private string _marketBehaviorNo = "";

        private DateTime? _mostRecentCandlestick = null;
        private DateTime? _mostRecentTicker = null;

        private int _positionSize;
        private double _marketExposure;
        private double _buyinPrice;
        private double _positionUpside;
        private double _positionDownside;
        private long _totalpositionTraded;
        private double _realizedPnl;
        private double _feesPaid;
        private List<OrderDTO> _restingOrders = new List<OrderDTO>();
        private double _positionROI;
        private double _positionROIAmt;
        private double _expectedFees;
        private double _yesBidSlopePerMinute_Short = 0;
        private double _noBidSlopePerMinute_Short = 0;
        private double _yesBidSlopePerMinute_Medium = 0;
        private double _noBidSlopePerMinute_Medium = 0;

        /// <summary>
        /// Gets or sets the short-term slope of yes bid prices per minute, calculated from recent ticker data.
        /// Represents the rate of change for yes bid prices over a short time period.
        /// </summary>
        public double YesBidSlopePerMinute_Short { get { return _yesBidSlopePerMinute_Short; } set { _yesBidSlopePerMinute_Short = value; } }
        /// <summary>
        /// Gets or sets the short-term slope of no bid prices per minute, calculated from recent ticker data.
        /// Represents the rate of change for no bid prices over a short time period.
        /// </summary>
        public double NoBidSlopePerMinute_Short { get { return _noBidSlopePerMinute_Short; } set { _noBidSlopePerMinute_Short = value; } }
        /// <summary>
        /// Gets or sets the medium-term slope of yes bid prices per minute, calculated from recent ticker data.
        /// Represents the rate of change for yes bid prices over a medium time period.
        /// </summary>
        public double YesBidSlopePerMinute_Medium { get { return _yesBidSlopePerMinute_Medium; } set { _yesBidSlopePerMinute_Medium = value; } }
        /// <summary>
        /// Gets or sets the medium-term slope of no bid prices per minute, calculated from recent ticker data.
        /// Represents the rate of change for no bid prices over a medium time period.
        /// </summary>
        public double NoBidSlopePerMinute_Medium { get { return _noBidSlopePerMinute_Medium; } set { _noBidSlopePerMinute_Medium = value; } }

        public string MarketType { get; set; } = "";

        private double _tolerancePercentage;

        public bool ChangeMetricsMature => _changeTracker.IsMature;

        /// <summary>
        /// Initializes a new instance of the MarketData class for a specific market.
        /// Sets up all data collections, dependencies, and initial state for market data tracking.
        /// Initializes candlestick dictionaries for minute, hour, and day timeframes, and configures
        /// calculation parameters from the provided configuration.
        /// </summary>
        /// <param name="market">The market DTO containing basic market information (ticker, category, status).</param>
        /// <param name="logger">Logger instance for recording market data operations and events.</param>
        /// <param name="tradingCalculator">Calculator service for technical indicators and trading metrics.</param>
        /// <param name="changeTracker">Tracker for monitoring order book changes and velocities.</param>
        /// <param name="marketDataConfig">Configuration options for calculation parameters and thresholds, allowing runtime customization.</param>
        /// <exception cref="ArgumentNullException">Thrown when any required dependency is null.</exception>
        public MarketData(
            MarketDTO market,
            ILogger<IMarketData> logger,
            ITradingCalculator tradingCalculator,
            IOrderbookChangeTracker changeTracker,
            IOptions<MarketServiceDataConfig> marketDataConfig)
        {
            _marketDataConfig = marketDataConfig?.Value ?? throw new ArgumentNullException(nameof(marketDataConfig));
            _tolerancePercentage = _marketDataConfig.Calculations.TolerancePercentage;
            _marketTicker = market.market_ticker ?? "";
            _marketCategory = market.category ?? "";
            _marketInfo = market;
            _marketStatus = market.status ?? "";
            _tradingCalculator = tradingCalculator ?? throw new ArgumentNullException(nameof(tradingCalculator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _changeTracker = changeTracker ?? throw new ArgumentNullException(nameof(changeTracker));
            _candlesticks = new Dictionary<string, List<CandlestickData>>
            {
                ["minute"] = new List<CandlestickData>(),
                ["hour"] = new List<CandlestickData>(),
                ["day"] = new List<CandlestickData>()
            };
            _tickers = new ConcurrentBag<TickerDTO>();
            _positions = new List<MarketPositionDTO>();
            _orderbookData = new List<OrderbookData>();
            _lastWebSocketMessageReceived = DateTime.UtcNow;

            _logger.LogDebug("MarketData initialized for ticker {MarketTicker}", _marketTicker);
        }
        /// <summary>
        /// Validates candlestick data for integrity before processing.
        /// Checks for non-negative prices within valid range (0-100), non-negative volume, and reasonable timestamps.
        /// Logs warnings for invalid data but continues processing to maintain system resilience.
        /// </summary>
        /// <param name="candlesticks">The list of candlestick data to validate.</param>
        private void ValidateCandlesticks(List<CandlestickData> candlesticks)
        {
            foreach (var candle in candlesticks)
            {
                bool hasInvalidPrice = candle.AskClose < 0 || candle.AskClose > 100 ||
                                       candle.BidClose < 0 || candle.BidClose > 100 ||
                                       candle.AskHigh < 0 || candle.AskHigh > 100 ||
                                       candle.BidHigh < 0 || candle.BidHigh > 100 ||
                                       candle.AskLow < 0 || candle.AskLow > 100 ||
                                       candle.BidLow < 0 || candle.BidLow > 100;
                if (hasInvalidPrice)
                {
                    _logger.LogWarning("Invalid candlestick data for {MarketTicker}: prices out of range (0-100). AskClose={AskClose}, BidClose={BidClose}, AskHigh={AskHigh}, BidHigh={BidHigh}, AskLow={AskLow}, BidLow={BidLow}",
                        _marketTicker, candle.AskClose, candle.BidClose, candle.AskHigh, candle.BidHigh, candle.AskLow, candle.BidLow);
                }
                if (candle.Volume < 0)
                {
                    _logger.LogWarning("Invalid candlestick data for {MarketTicker}: negative volume {Volume}", _marketTicker, candle.Volume);
                }
                if (candle.Date > DateTime.UtcNow.AddMinutes(1)) // Allow small future tolerance
                {
                    _logger.LogWarning("Invalid candlestick data for {MarketTicker}: future timestamp {Timestamp}", _marketTicker, candle.Date);
                }
                if (candle.Date < DateTime.UtcNow.AddYears(-10)) // Not too old
                {
                    _logger.LogWarning("Invalid candlestick data for {MarketTicker}: unreasonably old timestamp {Timestamp}", _marketTicker, candle.Date);
                }
            }
        }

        /// <summary>
        /// Validates ticker data for integrity before processing.
        /// Checks for non-negative prices within valid range (0-100), non-negative volume, and reasonable timestamps.
        /// Logs warnings for invalid data but continues processing to maintain system resilience.
        /// </summary>
        /// <param name="tickers">The list of ticker data to validate.</param>
        private void ValidateTickers(List<TickerDTO> tickers)
        {
            foreach (var ticker in tickers)
            {
                int no_bid = 100 - ticker.yes_bid;
                int no_ask = 100 - ticker.yes_ask;
                bool hasInvalidPrice = ticker.yes_bid < 0 || ticker.yes_bid > 100 ||
                                        ticker.yes_ask < 0 || ticker.yes_ask > 100 ||
                                        no_bid < 0 || no_bid > 100 ||
                                        no_ask < 0 || no_ask > 100;
                if (hasInvalidPrice)
                {
                    _logger.LogWarning("Invalid ticker data for {MarketTicker}: prices out of range (0-100). yes_bid={yes_bid}, yes_ask={yes_ask}, no_bid={no_bid}, no_ask={no_ask}",
                        _marketTicker, ticker.yes_bid, ticker.yes_ask, no_bid, no_ask);
                }
                if (ticker.volume < 0)
                {
                    _logger.LogWarning("Invalid ticker data for {MarketTicker}: negative volume {Volume}", _marketTicker, ticker.volume);
                }
                if (ticker.LoggedDate > DateTime.UtcNow.AddMinutes(1))
                {
                    _logger.LogWarning("Invalid ticker data for {MarketTicker}: future timestamp {Timestamp}", _marketTicker, ticker.LoggedDate);
                }
                if (ticker.LoggedDate < DateTime.UtcNow.AddYears(-10))
                {
                    _logger.LogWarning("Invalid ticker data for {MarketTicker}: unreasonably old timestamp {Timestamp}", _marketTicker, ticker.LoggedDate);
                }
            }
        }

        /// <summary>
        /// Validates orderbook data for integrity before processing.
        /// Checks for non-negative prices within valid range (0-100), non-negative quantities, and valid sides.
        /// Logs warnings for invalid data but continues processing to maintain system resilience.
        /// </summary>
        /// <param name="orderbookData">The list of orderbook data to validate.</param>
        private void ValidateOrderbook(List<OrderbookData> orderbookData)
        {
            foreach (var order in orderbookData)
            {
                if (order.Price < 0 || order.Price > 100)
                {
                    _logger.LogWarning("Invalid orderbook data for {MarketTicker}: price out of range (0-100) {Price}", _marketTicker, order.Price);
                }
                if (order.RestingContracts < 0)
                {
                    _logger.LogWarning("Invalid orderbook data for {MarketTicker}: negative resting contracts {RestingContracts}", _marketTicker, order.RestingContracts);
                }
                if (order.Side != "yes" && order.Side != "no")
                {
                    _logger.LogWarning("Invalid orderbook data for {MarketTicker}: invalid side '{Side}'", _marketTicker, order.Side);
                }
            }
        }

        /// <summary>
        /// Validates position data for integrity before processing.
        /// Checks for reasonable position sizes, market exposure within bounds, and valid timestamps.
        /// Logs warnings for invalid data but continues processing to maintain system resilience.
        /// </summary>
        /// <param name="positions">The list of position data to validate.</param>
        private void ValidatePositions(List<MarketPositionDTO> positions)
        {
            foreach (var position in positions)
            {
                if (Math.Abs(position.Position) > 1000000) // Arbitrary large number, adjust as needed
                {
                    _logger.LogWarning("Invalid position data for {MarketTicker}: unreasonably large position size {Position}", _marketTicker, position.Position);
                }
                if (position.MarketExposure < 0 || position.MarketExposure > 1)
                {
                    _logger.LogWarning("Invalid position data for {MarketTicker}: market exposure out of range (0-1) {MarketExposure}", _marketTicker, position.MarketExposure);
                }
                if (position.TotalTraded < 0)
                {
                    _logger.LogWarning("Invalid position data for {MarketTicker}: negative total traded {TotalTraded}", _marketTicker, position.TotalTraded);
                }
                if (position.LastUpdatedUTC > DateTime.UtcNow.AddMinutes(1))
                {
                    _logger.LogWarning("Invalid position data for {MarketTicker}: future last updated timestamp {Timestamp}", _marketTicker, position.LastUpdatedUTC);
                }
                if (position.LastUpdatedUTC < DateTime.UtcNow.AddYears(-10))
                {
                    _logger.LogWarning("Invalid position data for {MarketTicker}: unreasonably old last updated timestamp {Timestamp}", _marketTicker, position.LastUpdatedUTC);
                }
            }
        }

        /// <summary>
        /// Retrieves order book bid data filtered by side.
        /// </summary>
        /// <param name="side">The side to filter by ("yes", "no", or empty string for all).</param>
        /// <returns>A list of OrderbookData objects matching the specified side criteria.</returns>
        public List<OrderbookData> GetBids(string side = "")
        {
            if (_orderbookData == null) return new List<OrderbookData>();
            return _orderbookData.Where(o => o.Side == side || side == "").ToList();
        }


        private int CalculateDepthWithinTolerance(string side, double tolerancePercentage)
        {
            var bids = GetBids(side);
            if (!bids.Any()) return 0;

            int bestBid = bids.Max(o => o.Price);
            int toleranceCents = (int)(bestBid * (tolerancePercentage / 100.0));
            int minPrice = bestBid - toleranceCents;

            return bids
                .Where(o => o.Price >= minPrice)
                .Sum(o => o.RestingContracts);
        }

        public double ExpectedFees { get { return _expectedFees; } set { _expectedFees = value; } }

        /// <summary>
        /// Gets or sets the tolerance percentage used for depth calculations within a price range.
        /// This value is initialized from CalculationConfig but can be overridden locally.
        /// Determines the percentage range around the best bid to include in depth calculations.
        /// Must be non-negative.
        /// </summary>
        public double TolerancePercentage
        {
            get => _tolerancePercentage;
            set => _tolerancePercentage = Math.Max(0, value);
        }
        public DateTime LastOrderbookEventTimestamp
        {
            get => _lastOrderbookEventTimestamp;
            set => _lastOrderbookEventTimestamp = value;
        }
        public DateTime LastSnapshotTaken
        {
            get => _lastSnapshotTaken;
            set => _lastSnapshotTaken = value;
        }

        /// <summary>
        /// Recalculates all order book change metrics using the change tracker.
        /// This method triggers a full recalculation of velocities, trade rates, and other dynamic metrics.
        /// </summary>
        public void RecalculateOrderbookChangeMetrics()
        {
            _changeTracker.RecalculateAllMetrics();
        }

        public IOrderbookChangeTracker ChangeTracker => _changeTracker;

        private double _AverageTradeSize_Yes = 0;
        private double _AverageTradeSize_No = 0;
        private int _TradeCount_Yes = 0;
        private int _TradeCount_No = 0;
        private int _NonTradeRelatedOrderCount_Yes = 0;
        private int _NonTradeRelatedOrderCount_No = 0;

        public double AverageTradeSize_Yes { get { return _AverageTradeSize_Yes; } set { _AverageTradeSize_Yes = value; } }
        public double AverageTradeSize_No { get { return _AverageTradeSize_No; } set { _AverageTradeSize_No = value; } }
        public int TradeCount_Yes { get { return _TradeCount_Yes; } set { _TradeCount_Yes = value; } }
        public int TradeCount_No { get { return _TradeCount_No; } set { _TradeCount_No = value; } }
        public int NonTradeRelatedOrderCount_Yes { get { return _NonTradeRelatedOrderCount_Yes; } set { _NonTradeRelatedOrderCount_Yes = value; } }
        public int NonTradeRelatedOrderCount_No { get { return _NonTradeRelatedOrderCount_No; } set { _NonTradeRelatedOrderCount_No = value; } }

        private double _velocityPerMinute_Top_Yes_Bid = 0;
        private double _velocityPerMinute_Top_No_Bid = 0;
        private double _VelocityPerMinute_Bottom_Yes_Bid = 0;
        private double _VelocityPerMinute_Bottom_No_Bid = 0;

        private int _yesBidTopLevelCount = 0;
        private int _noBidTopLevelCount = 0;
        private int _yesBidBottomLevelCount = 0;
        private int _noBidBottomLevelCount = 0;

        public int LevelCount_Top_Yes_Bid { get { return _yesBidTopLevelCount; } set { _yesBidTopLevelCount = value; } }
        public int LevelCount_Top_No_Bid { get { return _noBidTopLevelCount; } set { _noBidTopLevelCount = value; } }
        public int LevelCount_Bottom_No_Bid { get { return _yesBidBottomLevelCount; } set { _yesBidBottomLevelCount = value; } }
        public int LevelCount_Bottom_Yes_Bid { get { return _noBidBottomLevelCount; } set { _noBidBottomLevelCount = value; } }

        /// <summary>
        /// Gets or sets the velocity (rate of change) of the top yes bid price per minute.
        /// Calculated from order book change metrics to track market momentum.
        /// </summary>
        public double VelocityPerMinute_Top_Yes_Bid { get { return _velocityPerMinute_Top_Yes_Bid; } set { _velocityPerMinute_Top_Yes_Bid = value; } }
        /// <summary>
        /// Gets or sets the velocity (rate of change) of the top no bid price per minute.
        /// Calculated from order book change metrics to track market momentum.
        /// </summary>
        public double VelocityPerMinute_Top_No_Bid { get { return _velocityPerMinute_Top_No_Bid; } set { _velocityPerMinute_Top_No_Bid = value; } }
        /// <summary>
        /// Gets or sets the velocity (rate of change) of the bottom yes bid price per minute.
        /// Calculated from order book change metrics to track market momentum.
        /// </summary>
        public double VelocityPerMinute_Bottom_Yes_Bid { get { return _VelocityPerMinute_Bottom_Yes_Bid; } set { _VelocityPerMinute_Bottom_Yes_Bid = value; } }
        /// <summary>
        /// Gets or sets the velocity (rate of change) of the bottom no bid price per minute.
        /// Calculated from order book change metrics to track market momentum.
        /// </summary>
        public double VelocityPerMinute_Bottom_No_Bid { get { return _VelocityPerMinute_Bottom_No_Bid; } set { _VelocityPerMinute_Bottom_No_Bid = value; } }

        private double _tradeVolumePerMinute_Yes = 0;
        private double _tradeVolumePerMinute_No = 0;
        private double _tradeRatePerMinute_Yes = 0;
        private double _tradeRatePerMinute_No = 0;

        /// <summary>
        /// Gets or sets the trade volume per minute for yes positions.
        /// Represents the volume of trades executed for yes contracts in the last minute.
        /// </summary>
        public double TradeVolumePerMinute_Yes { get { return _tradeVolumePerMinute_Yes; } set { _tradeVolumePerMinute_Yes = value; } }
        /// <summary>
        /// Gets or sets the trade volume per minute for no positions.
        /// Represents the volume of trades executed for no contracts in the last minute.
        /// </summary>
        public double TradeVolumePerMinute_No { get { return _tradeVolumePerMinute_No; } set { _tradeVolumePerMinute_No = value; } }
        /// <summary>
        /// Gets or sets the trade rate per minute for yes positions.
        /// Represents the number of trades executed for yes contracts in the last minute.
        /// </summary>
        public double TradeRatePerMinute_Yes { get { return _tradeRatePerMinute_Yes; } set { _tradeRatePerMinute_Yes = value; } }
        /// <summary>
        /// Gets or sets the trade rate per minute for no positions.
        /// Represents the number of trades executed for no contracts in the last minute.
        /// </summary>
        public double TradeRatePerMinute_No { get { return _tradeRatePerMinute_No; } set { _tradeRatePerMinute_No = value; } }

        private double _yesBidOrderRatePerMinute = 0;
        private double _noBidOrderRatePerMinute = 0;

        public double OrderVolumePerMinute_YesBid { get { return _yesBidOrderRatePerMinute; } set { _yesBidOrderRatePerMinute = value; } }
        public double OrderVolumePerMinute_NoBid { get { return _noBidOrderRatePerMinute; } set { _noBidOrderRatePerMinute = value; } }

        public int BestYesBid => GetBids("yes").Any() ? GetBids("yes").Max(o => o.Price) : 0;
        public int BestNoBid => GetBids("no").Any() ? GetBids("no").Max(o => o.Price) : 0;
        public int BestYesAsk => 100 - BestNoBid;
        public int BestNoAsk => 100 - BestYesBid;

        public int YesSpread => BestYesAsk - BestYesBid;
        public int NoSpread => BestNoAsk - BestNoBid;

        public bool ReceivedFirstSnapshot { get; set; } = false;

        public int DepthAtBestYesBid => GetBids("yes")
            .Where(o => o.Price == BestYesBid)
            .Sum(o => o.RestingContracts);
        public int DepthAtBestNoBid => GetBids("no")
            .Where(o => o.Price == BestNoBid)
            .Sum(o => o.RestingContracts);
        public int DepthAtBestYesAsk => DepthAtBestNoBid;
        public int DepthAtBestNoAsk => DepthAtBestYesBid;

        public int TopTenPercentLevelDepth_Yes => CalculateDepthWithinTolerance("yes", _tolerancePercentage);
        public int TopTenPercentLevelDepth_No => CalculateDepthWithinTolerance("no", _tolerancePercentage);

        public int BidRange_Yes => GetBids("yes").Any() ?
            GetBids("yes").Max(o => o.Price) - GetBids("yes").Min(o => o.Price) : 0;
        public int BidRange_No => GetBids("no").Any() ?
            GetBids("no").Max(o => o.Price) - GetBids("no").Min(o => o.Price) : 0;

        public int TotalBidContracts_Yes => GetBids("yes").Sum(o => o.RestingContracts);
        public int TotalBidContracts_No => GetBids("no").Sum(o => o.RestingContracts);
        public double TotalBidVolume_Yes => GetBids("yes").Sum(o => o.RestingContracts * o.Price);
        public double TotalBidVolume_No => GetBids("no").Sum(o => o.RestingContracts * o.Price);
        public int BidCountImbalance => TotalBidContracts_Yes - TotalBidContracts_No;
        public double BidVolumeImbalance => TotalBidVolume_Yes - TotalBidVolume_No;

        public double? PSAR { get; set; }
        public double? ADX { get; set; }
        public double? PlusDI { get; set; }
        public double? MinusDI { get; set; }

        private double _highestVolume_Day = 0;
        private double _highestVolume_Hour = 0;
        private double _highestVolume_Minute = 0;
        private double _recentVolume_LastHour = 0;
        private double _recentVolume_LastThreeHours = 0;
        private double _recentVolume_LastMonth = 0;


        public double HighestVolume_Day { get { return _highestVolume_Day; } set { _highestVolume_Day = value; } }
        public double HighestVolume_Hour { get { return _highestVolume_Hour; } set { _highestVolume_Hour = value; } }
        public double HighestVolume_Minute { get { return _highestVolume_Minute; } set { _highestVolume_Minute = value; } }
        public double RecentVolume_LastHour { get { return _recentVolume_LastHour; } set { _recentVolume_LastHour = value; } }
        public double RecentVolume_LastThreeHours { get { return _recentVolume_LastThreeHours; } set { _recentVolume_LastThreeHours = value; } }
        public double RecentVolume_LastMonth { get { return _recentVolume_LastMonth; } set { _recentVolume_LastMonth = value; } }


        public int DepthAtTop4YesBids => GetBids("yes")
            .OrderByDescending(o => o.Price)
            .Take(4)
            .Sum(o => o.RestingContracts);

        public int DepthAtTop4NoBids => GetBids("no")
            .OrderByDescending(o => o.Price)
            .Take(4)
            .Sum(o => o.RestingContracts);


        public double YesBidCenterOfMass => GetBids("yes").Any() ?
            Math.Round(GetBids("yes").Sum(o => o.Price * (double)o.Price * o.RestingContracts) /
            GetBids("yes").Sum(o => o.Price * (double)o.RestingContracts), 2) : 0;

        public double NoBidCenterOfMass => GetBids("no").Any() ?
            Math.Round(GetBids("no").Sum(o => o.Price * (double)o.Price * o.RestingContracts) /
            GetBids("no").Sum(o => o.Price * (double)o.RestingContracts), 2) : 0;

        /// <summary>
        /// Updates the current price information if the timestamp is newer than the existing price data.
        /// This method ensures that only the most recent price updates are retained, based on timestamp comparison.
        /// </summary>
        /// <param name="yesAsk">The ask price for yes contracts.</param>
        /// <param name="yesBid">The bid price for yes contracts.</param>
        /// <param name="timestamp">The timestamp of the price update.</param>
        /// <param name="source">The source of the price update (e.g., "Candlestick", "Ticker").</param>
        public void UpdateCurrentPrice(int yesAsk, int yesBid, DateTime timestamp, string source)
        {

            if (timestamp >= _tickerPrice.When && (yesAsk != _tickerPrice.Ask || yesBid != _tickerPrice.Bid))
            {

                _tickerPrice = (yesAsk, yesBid, timestamp);
                _currentPriceSource = source;
                _logger.LogDebug("Price updated for {MarketTicker}: Ask={Ask}, Bid={Bid}, Source={Source}",
                    _marketTicker, yesAsk, yesBid, source);
            }
            else
            {
                _logger.LogDebug("Price not updated for {MarketTicker}: Ask={Ask}, Bid={Bid}, Source={Source}, newTime={newTime}, currentTime={currentTime}",
                    _marketTicker, yesAsk, yesBid, source, timestamp, _tickerPrice.When);
            }
        }

        /// <summary>
        /// Asynchronously refreshes all metadata for the market, including candlestick, ticker, and position data.
        /// This method coordinates the refresh process, calling synchronous methods for candlestick and position data,
        /// and asynchronous methods for ticker data to ensure non-blocking updates.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task RefreshAllMetadata()
        {
            RefreshCandlestickMetadata();
            await RefreshTickerMetadata();
            RefreshPositionMetadata();
        }

        public void RefreshCandlestickMetadata()
        {
            // Validate candlestick data integrity
            ValidateCandlesticks(_candlesticks["minute"]);

            if (_candlesticks["minute"].Count > 0)
            {
                var latestCandlestick = _candlesticks["minute"].OrderByDescending(c => c.Date).First();
                _mostRecentCandlestick = latestCandlestick.Date.ToUniversalTime();
                if (_mostRecentCandlestick > DateTime.UtcNow)
                {
                    _logger.LogWarning("Candlestick timestamp {Timestamp} is in the future for {MarketTicker}, adjusting to current UTC time",
                        _mostRecentCandlestick.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), _marketTicker);
                    _mostRecentCandlestick = DateTime.UtcNow;
                }

                CandlestickData? candle = _candlesticks["minute"].MaxBy(x => x.BidHigh);
                if (candle != null) _allTimeHighYesBid = (candle.BidHigh, candle.Date);
                candle = _candlesticks["minute"].MaxBy(x => 100 - x.AskHigh);
                if (candle != null) _allTimeHighNoBid = (100 - candle.AskHigh, candle.Date);
                candle = _candlesticks["minute"].MinBy(x => x.AskLow);
                if (candle != null) _allTimeLowYesBid = (candle.AskHigh, candle.Date);
                candle = _candlesticks["minute"].MinBy(x => 100 - x.AskLow);
                if (candle != null) _allTimeLowNoBid = (candle.AskHigh, candle.Date);


                List<CandlestickData> recentCandlesticks = _candlesticks["minute"].Where(x => x.Date >= DateTime.UtcNow.AddDays(-_marketDataConfig.Calculations.RecentCandlestickDays)).ToList();
                candle = recentCandlesticks.MaxBy(x => x.BidHigh);
                if (candle != null) _recentHighYesBid = (candle.BidHigh, candle.Date);
                candle = recentCandlesticks.MaxBy(x => 100 - x.AskHigh);
                if (candle != null) _recentHighNoBid = (100 - candle.AskHigh, candle.Date);
                candle = recentCandlesticks.MinBy(x => x.AskLow);
                if (candle != null) _recentLowYesBid = (candle.AskHigh, candle.Date);
                candle = recentCandlesticks.MinBy(x => 100 - x.AskLow);
                if (candle != null) _recentLowNoBid = (candle.AskHigh, candle.Date);

                _logger.LogDebug("Candlestick metadata for {MarketTicker}: MostRecentCandlestick={Timestamp}, AskClose={Ask}, BidClose={Bid}",
                    _marketTicker, _mostRecentCandlestick.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), latestCandlestick.AskClose, latestCandlestick.BidClose);

                // Only update price if no recent tickers or candlestick is older than ticker
                if (_tickers.Count == 0 || _mostRecentTicker != null && _mostRecentCandlestick > _mostRecentTicker)
                {
                    int newAsk = latestCandlestick.AskClose;
                    int newBid = latestCandlestick.BidClose;
                    _logger.LogDebug("Candlestick metadata for {MarketTicker} is newer than ticker data, updating current price. Most recent candlestick date: {candlestickDate}, tickers count: {count}",
                        _marketTicker, _mostRecentTicker.ToString(), _tickers.Count);
                    UpdateCurrentPrice(newAsk, newBid, _mostRecentCandlestick.Value, "Candlestick");
                }
            }
        }

        /// <summary>
        /// Asynchronously refreshes ticker metadata, validates ticker data integrity, and updates trading metrics.
        /// This method performs validation on ticker data and triggers asynchronous calculation of technical indicators.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task RefreshTickerMetadata()
        {
            // Validate ticker data integrity
            ValidateTickers(_tickers.ToList());

            if (!_tickers.IsEmpty)
            {
                _mostRecentTicker = _tickers.OrderByDescending(t => t.LoggedDate).FirstOrDefault()?.LoggedDate ?? default;
            }

            await UpdateTradingMetrics();

        }

        private void CalculateSlope()
        {
            var now = DateTime.UtcNow;
            var shortMinAgo = now.AddMinutes(-_marketDataConfig.Calculations.SlopeShortMinutes);
            var mediumMinAgo = now.AddMinutes(-_marketDataConfig.Calculations.SlopeMediumMinutes);

            var recentTickers = _tickers
                .Where(t => t.LoggedDate >= shortMinAgo && t.LoggedDate <= now)
                .OrderBy(t => t.LoggedDate)
                .ToList();

            var recentTickers_m = _tickers
                .Where(t => t.LoggedDate >= mediumMinAgo && t.LoggedDate <= now)
                .OrderBy(t => t.LoggedDate)
                .ToList();

            // Short-term slope
            if (recentTickers.Count >= 2)
            {
                var first = recentTickers.First();
                var last = recentTickers.Last();
                var dt = (now - first.LoggedDate).TotalMinutes;

                if (dt > 0)
                {
                    _yesBidSlopePerMinute_Short = Math.Round((last.yes_bid - first.yes_bid) / dt, 2, MidpointRounding.AwayFromZero);
                    _noBidSlopePerMinute_Short  = Math.Round(((100 - last.yes_ask) - (100 - first.yes_ask)) / dt, 2, MidpointRounding.AwayFromZero);
                }
                else
                {
                    _yesBidSlopePerMinute_Short = 0;
                    _noBidSlopePerMinute_Short  = 0;
                }
            }
            else
            {
                _yesBidSlopePerMinute_Short = 0;
                _noBidSlopePerMinute_Short  = 0;
            }

            // Medium-term slope
            if (recentTickers_m.Count >= 2)
            {
                var first_m = recentTickers_m.First();
                var last_m = recentTickers_m.Last();
                var dtm = (now - first_m.LoggedDate).TotalMinutes;

                if (dtm > 0)
                {
                    _yesBidSlopePerMinute_Medium = Math.Round((last_m.yes_bid - first_m.yes_bid) / dtm, 2, MidpointRounding.AwayFromZero);
                    _noBidSlopePerMinute_Medium  = Math.Round(((100 - last_m.yes_ask) - (100 - first_m.yes_ask)) / dtm, 2, MidpointRounding.AwayFromZero);
                }
                else
                {
                    _yesBidSlopePerMinute_Medium = 0;
                    _noBidSlopePerMinute_Medium  = 0;
                }
            }
            else
            {
                _yesBidSlopePerMinute_Medium = 0;
                _noBidSlopePerMinute_Medium  = 0;
            }


        }

        private double? _rsi_Short;
        private double? _rsi_Medium;
        private double? _rsi_Long;
        private (double? MACD, double? Signal, double? Histogram) _macd_Medium;
        private (double? MACD, double? Signal, double? Histogram) _macd_Long;
        private double? _ema_Medium;
        private double? _ema_Long;
        private (double? lower, double? middle, double? upper) _bollingerbands_Medium;
        private (double? lower, double? middle, double? upper) _bollingerbands_Long;
        private double? _atr_Medium;
        private double? _atr_Long;
        private double? _vwap_Short;
        private double? _vwap_Medium;
        private (double? K, double? D) _stochasticoscilator_Short;
        private (double? K, double? D) _stochasticoscilator_Medium;
        private (double? K, double? D) _stochasticoscilator_Long;
        private long _obv_Medium;
        private long _obv_Long;

        /// <summary>
        /// Asynchronously updates trading metrics for the market, including technical indicators and pseudo candlesticks.
        /// This method performs CPU-intensive calculations for RSI, MACD, Bollinger Bands, and other indicators using parallel processing
        /// to maintain system responsiveness. All calculations are offloaded to background threads to avoid blocking the main thread.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task UpdateTradingMetrics()
        {
            _logger.LogDebug("**Started updating trading metrics for {marketTicker}**", _marketTicker);
            _minutePseudoCandlesticks = await BuildPseudoCandlesticks("minute", _marketDataConfig.PseudoCandlestickLookbackPeriods);
            _hourPseudoCandlesticks = await BuildPseudoCandlesticks("hour", _marketDataConfig.PseudoCandlestickLookbackPeriods);
            _dayPseudoCandlesticks = await BuildPseudoCandlesticks("day", _marketDataConfig.PseudoCandlestickLookbackPeriods);
            var minuteCopy = _minutePseudoCandlesticks.ToList();
            var hourCopy = _hourPseudoCandlesticks.ToList();
            var dayCopy = _dayPseudoCandlesticks.ToList();

            await Task.Run(() =>
            {
                _rsi_Short = _tradingCalculator.CalculateRSI(minuteCopy, _marketDataConfig.Calculations.RSI_Short_Periods);
                if (_rsi_Short != null) _rsi_Short = Math.Round((double)_rsi_Short, 2);
                _rsi_Medium = _tradingCalculator.CalculateRSI(hourCopy, _marketDataConfig.Calculations.RSI_Medium_Periods);
                if (_rsi_Medium != null) _rsi_Medium = Math.Round((double)_rsi_Medium, 2);
                _rsi_Long = _tradingCalculator.CalculateRSI(dayCopy, _marketDataConfig.Calculations.RSI_Long_Periods);
                if (_rsi_Long != null) _rsi_Long = Math.Round((double)_rsi_Long, 2);

                _macd_Medium = _tradingCalculator.CalculateMACD(hourCopy,
                    _marketDataConfig.Calculations.MACD_Medium_FastPeriod,
                    _marketDataConfig.Calculations.MACD_Medium_SlowPeriod,
                    _marketDataConfig.Calculations.MACD_Medium_SignalPeriod);

                if (_macd_Medium.MACD != null) _macd_Medium.MACD = Math.Round((double)_macd_Medium.MACD, 2);
                if (_macd_Medium.Signal != null) _macd_Medium.Signal = Math.Round((double)_macd_Medium.Signal, 2);
                if (_macd_Medium.Histogram != null) _macd_Medium.Histogram = Math.Round((double)_macd_Medium.Histogram, 2);

                _macd_Long = _tradingCalculator.CalculateMACD(dayCopy,
                    _marketDataConfig.Calculations.MACD_Long_FastPeriod,
                    _marketDataConfig.Calculations.MACD_Long_SlowPeriod,
                    _marketDataConfig.Calculations.MACD_Long_SignalPeriod);

                if (_macd_Long.MACD != null) _macd_Long.MACD = Math.Round((double)_macd_Long.MACD, 2);
                if (_macd_Long.Signal != null) _macd_Long.Signal = Math.Round((double)_macd_Long.Signal, 2);
                if (_macd_Long.Histogram != null) _macd_Long.Histogram = Math.Round((double)_macd_Long.Histogram, 2);


                _ema_Medium = TradingCalculations.CalculateEMA(hourCopy.Select(c => c.MidClose).ToList(),
                _marketDataConfig.Calculations.EMA_Medium_Periods);
                if (_ema_Medium != null) _ema_Medium = Math.Round((double)_ema_Medium, 2);


                _ema_Long = TradingCalculations.CalculateEMA(dayCopy.Select(c => c.MidClose).ToList(),
                    _marketDataConfig.Calculations.EMA_Long_Periods);
                if (_ema_Long != null) _ema_Long = Math.Round((double)_ema_Long, 2);

                _bollingerbands_Medium = _tradingCalculator.CalculateBollingerBands(hourCopy,
                    _marketDataConfig.Calculations.BollingerBands_Medium_Periods,
                    _marketDataConfig.Calculations.BollingerBands_Medium_StdDev);

                if (_bollingerbands_Medium.lower != null) _bollingerbands_Medium.lower = Math.Round((double)_bollingerbands_Medium.lower, 2);
                if (_bollingerbands_Medium.middle != null) _bollingerbands_Medium.middle = Math.Round((double)_bollingerbands_Medium.middle, 2);
                if (_bollingerbands_Medium.upper != null) _bollingerbands_Medium.upper = Math.Round((double)_bollingerbands_Medium.upper, 2);

                _bollingerbands_Long = _tradingCalculator.CalculateBollingerBands(dayCopy,
                    _marketDataConfig.Calculations.BollingerBands_Long_Periods,
                    _marketDataConfig.Calculations.BollingerBands_Long_StdDev);
                if (_bollingerbands_Long.lower != null) _bollingerbands_Long.lower = Math.Round((double)_bollingerbands_Long.lower, 2);
                if (_bollingerbands_Long.middle != null) _bollingerbands_Long.middle = Math.Round((double)_bollingerbands_Long.middle, 2);
                if (_bollingerbands_Long.upper != null) _bollingerbands_Long.upper = Math.Round((double)_bollingerbands_Long.upper, 2);

                _atr_Medium = _tradingCalculator.CalculateATR(hourCopy, _marketDataConfig.Calculations.ATR_Medium_Periods);
                if (_atr_Medium != null) _atr_Medium = Math.Round((double)_atr_Medium, 2);
                _atr_Long = _tradingCalculator.CalculateATR(dayCopy, _marketDataConfig.Calculations.ATR_Long_Periods);
                if (_atr_Long != null) _atr_Long = Math.Round((double)_atr_Long, 2);
                _vwap_Short = (double?)_tradingCalculator.CalculateVWAP(minuteCopy, periods: _marketDataConfig.Calculations.VWAP_Short_Periods);
                if (_vwap_Short != null) _vwap_Short = Math.Round((double)_vwap_Short, 2);
                _vwap_Medium = (double?)_tradingCalculator.CalculateVWAP(hourCopy, periods: _marketDataConfig.Calculations.VWAP_Medium_Periods);
                if (_vwap_Medium != null) _vwap_Medium = Math.Round((double)_vwap_Medium, 2);
                _stochasticoscilator_Short = _tradingCalculator.CalculateStochastic(minuteCopy,
                    _marketDataConfig.Calculations.Stochastic_Short_Periods,
                    _marketDataConfig.Calculations.Stochastic_Short_DPeriods);
                if (_stochasticoscilator_Short.K != null) _stochasticoscilator_Short.K = Math.Round((double)_stochasticoscilator_Short.K, 2);
                if (_stochasticoscilator_Short.D != null) _stochasticoscilator_Short.D = Math.Round((double)_stochasticoscilator_Short.D, 2);
                _stochasticoscilator_Medium = _tradingCalculator.CalculateStochastic(hourCopy,
                    _marketDataConfig.Calculations.Stochastic_Medium_Periods,
                    _marketDataConfig.Calculations.Stochastic_Medium_DPeriods);
                if (_stochasticoscilator_Medium.K != null) _stochasticoscilator_Medium.K = Math.Round((double)_stochasticoscilator_Medium.K, 2);
                if (_stochasticoscilator_Medium.D != null) _stochasticoscilator_Medium.D = Math.Round((double)_stochasticoscilator_Medium.D, 2);
                _stochasticoscilator_Long = _tradingCalculator.CalculateStochastic(dayCopy,
                    _marketDataConfig.Calculations.Stochastic_Long_Periods,
                    _marketDataConfig.Calculations.Stochastic_Long_DPeriods);
                if (_stochasticoscilator_Long.K != null) _stochasticoscilator_Long.K = Math.Round((double)_stochasticoscilator_Long.K, 2);
                if (_stochasticoscilator_Long.D != null) _stochasticoscilator_Long.D = Math.Round((double)_stochasticoscilator_Long.D, 2);
                _obv_Medium = (long)_tradingCalculator.CalculateOBV(hourCopy);

                _obv_Long = (long)_tradingCalculator.CalculateOBV(dayCopy);

                var adxResult = _tradingCalculator.CalculateADX(minuteCopy);
                ADX = adxResult.ADX;
                PlusDI = adxResult.PlusDI;
                MinusDI = adxResult.MinusDI;

                if (ADX != null) ADX = Math.Round((double)ADX, 2);
                if (PlusDI != null) PlusDI = Math.Round((double)PlusDI, 2);
                if (MinusDI != null) MinusDI = Math.Round((double)MinusDI, 2);

                CalculateSlope();

                RecentCandlesticks = minuteCopy.TakeLast(_marketDataConfig.Calculations.RecentCandlesticksCount).ToList();
                var psarValue = _tradingCalculator.CalculatePSAR(minuteCopy);
                PSAR = psarValue.HasValue ? Math.Round((double)psarValue.Value, 2) : null;
                _logger.LogDebug("**Ended updating trading metrics for {marketTicker}**", _marketTicker);
            });
        }

        public double? RSI_Short
        {
            get => _rsi_Short;
            set => _rsi_Short = value;
        }
        public double? RSI_Medium
        {
            get => _rsi_Medium;
            set => _rsi_Medium = value;
        }
        public double? RSI_Long
        {
            get => _rsi_Long;
            set => _rsi_Long = value;
        }
        public (double? MACD, double? Signal, double? Histogram) MACD_Medium
        {
            get => _macd_Medium;
            set
            {
                _macd_Medium.MACD = value.MACD;
                _macd_Medium.Signal = value.Signal;
                _macd_Medium.Histogram = value.Histogram;
            }
        }
        public (double? MACD, double? Signal, double? Histogram) MACD_Long
        {
            get => _macd_Long;
            set
            {
                _macd_Long.MACD = value.MACD;
                _macd_Long.Signal = value.Signal;
                _macd_Long.Histogram = value.Histogram;
            }
        }
        public double? EMA_Medium
        {
            get => _ema_Medium;
            set => _ema_Medium = value;
        }
        public double? EMA_Long
        {
            get => _ema_Long;
            set => _ema_Long = value;
        }
        public (double? lower, double? middle, double? upper) BollingerBands_Medium
        {
            get => _bollingerbands_Medium;
            set
            {
                _bollingerbands_Medium.lower = value.lower;
                _bollingerbands_Medium.middle = value.middle;
                _bollingerbands_Medium.upper = value.upper;
            }
        }
        public (double? lower, double? middle, double? upper) BollingerBands_Long
        {
            get => _bollingerbands_Long;
            set
            {
                _bollingerbands_Long.lower = value.lower;
                _bollingerbands_Long.middle = value.middle;
                _bollingerbands_Long.upper = value.upper;
            }
        }
        public double? ATR_Medium
        {
            get => _atr_Medium;
            set => _atr_Medium = value;
        }
        public double? ATR_Long
        {
            get => _atr_Long;
            set => _atr_Long = value;
        }
        public double? VWAP_Short
        {
            get => _vwap_Short;
            set => _vwap_Short = value;
        }
        public double? VWAP_Medium
        {
            get => _vwap_Medium;
            set => _vwap_Medium = value;
        }
        public (double? K, double? D) StochasticOscillator_Short
        {
            get => _stochasticoscilator_Short;
            set
            {
                _stochasticoscilator_Short.K = value.K;
                _stochasticoscilator_Short.D = value.D;
            }
        }
        public (double? K, double? D) StochasticOscillator_Medium
        {
            get => _stochasticoscilator_Medium;
            set
            {
                _stochasticoscilator_Medium.K = value.K;
                _stochasticoscilator_Medium.D = value.D;
            }
        }
        public (double? K, double? D) StochasticOscillator_Long
        {
            get => _stochasticoscilator_Long;
            set
            {
                _stochasticoscilator_Long.K = value.K;
                _stochasticoscilator_Long.D = value.D;
            }
        }
        public long OBV_Medium
        {
            get => _obv_Medium;
            set => _obv_Medium = value;
        }
        public long OBV_Long
        {
            get => _obv_Long;
            set => _obv_Long = value;
        }

        public void RefreshPositionMetadata()
        {
            // Validate position and orderbook data integrity
            if (_positions != null)
            {
                ValidatePositions(_positions);
            }
            if (_orderbookData != null)
            {
                ValidateOrderbook(_orderbookData);
            }

            if (_positions == null || _positions.Count == 0)
            {
                _positionSize = 0;
                _marketExposure = 0;
                _buyinPrice = 0;
                _positionUpside = 0;
                _positionDownside = 0;
                _totalpositionTraded = 0;
                _realizedPnl = 0;
                _feesPaid = 0;
                _positionROI = 0;
                _positionROIAmt = 0;
                _expectedFees = 0;
                return;
            }

            var position = _positions[0];
            _positionSize = position.Position;
            _marketExposure = double.TryParse(position.MarketExposure.ToString(), out double exposure) ? exposure / 100 : 0;
            _buyinPrice = TradingCalculations.CalculateBuyinPrice(_marketExposure, _positionSize);

            bool isYesPosition = _positionSize >= 0;
            string side = isYesPosition ? "yes" : "no";

            // Extract orderbook levels
            var orderbookLevels = _orderbookData
                ?.Where(od => od.Side == side)
                .Select(od => (Price: od.Price, Quantity: od.RestingContracts))
                .OrderByDescending(level => level.Price)
                .ToList() ?? new List<(int Price, int Quantity)>();

            // Calculate liquidation price and fees
            double liquidationPrice = TradingCalculations.CalculateLiquidationPrice(_positionSize, orderbookLevels);
            _expectedFees = TradingCalculations.CalculateExpectedFees(_positionSize, orderbookLevels);

            // Calculate ROI
            var (roiAmount, roiPercentage) = TradingCalculations.CalculateROI(_positionSize, liquidationPrice, _buyinPrice, _expectedFees);
            _positionROIAmt = roiAmount;
            _positionROI = roiPercentage;

            // Calculate upside and downside
            var (upside, downside) = TradingCalculations.CalculateUpsideDownside(_positionSize, liquidationPrice, _expectedFees);
            _positionUpside = upside;
            _positionDownside = downside;

            // Update remaining fields
            _totalpositionTraded = position.TotalTraded;
            _realizedPnl = position.RealizedPnl / 100.0;
            _feesPaid = position.FeesPaid / 100.0;
        }

        private double CalculateTradingFees(int contracts, double priceInDollars)
        {
            // fees = roundup(fee_rate * C * P * (1-P))
            double fee = _marketDataConfig.Calculations.TradingFeeRate * contracts * priceInDollars * (1 - priceInDollars);
            return Math.Ceiling(fee * 100) / 100; // Round up to the next cent
        }

        public string MarketTicker { get => _marketTicker; set => _marketTicker = value; }
        public MarketDTO MarketInfo { get => _marketInfo; set => _marketInfo = value; }
        public Dictionary<string, List<CandlestickData>> Candlesticks => _candlesticks;
        public ConcurrentBag<TickerDTO> Tickers { get => _tickers; set => _tickers = value; }
        public List<OrderbookData> OrderbookData { get => _orderbookData; set => _orderbookData = value; }
        public DateTime LastWebSocketMessageReceived { get => _lastWebSocketMessageReceived; set => _lastWebSocketMessageReceived = value; }
        public string CurrentPriceSource => _currentPriceSource;
        public (int Ask, int Bid, DateTime When) TickerPriceYes => _tickerPrice;
        public (int Ask, int Bid, DateTime When) TickerPriceNo => (100 - _tickerPrice.Bid, 100 - _tickerPrice.Ask, _tickerPrice.When);
        public (int Bid, DateTime When) AllTimeHighYes_Bid { get => _allTimeHighYesBid; set => _allTimeHighYesBid = value; }
        public (int Bid, DateTime When) AllTimeHighNo_Bid => (_allTimeHighNoBid.Bid, _allTimeHighNoBid.When);
        public (int Bid, DateTime When) AllTimeLowYes_Bid { get => _allTimeLowYesBid; set => _allTimeLowYesBid = value; }
        public (int Bid, DateTime When) AllTimeLowNo_Bid => (_allTimeLowNoBid.Bid, _allTimeHighYesBid.When);
        public (int Bid, DateTime When) RecentHighYes_Bid { get => _recentHighYesBid; set => _recentHighYesBid = value; }
        public (int Bid, DateTime When) RecentHighNo_Bid => (_recentHighNoBid.Bid, _recentHighNoBid.When);
        public (int Bid, DateTime When) RecentLowYes_Bid { get => _recentLowYesBid; set => _recentLowYesBid = value; }
        public (int Bid, DateTime When) RecentLowNo_Bid => (_recentLowNoBid.Bid, _recentLowNoBid.When);
        public string GoodBadPriceYes { get => _goodBadPriceYes; set => _goodBadPriceYes = value; }
        public string GoodBadPriceNo { get => _goodBadPriceNo; set => _goodBadPriceNo = value; }
        public string MarketBehaviorYes { get => _marketBehaviorYes; set => _marketBehaviorYes = value; }
        public string MarketBehaviorNo { get => _marketBehaviorNo; set => _marketBehaviorNo = value; }
        public List<MarketPositionDTO> Positions { get => _positions; set => _positions = value; }
        public int PositionSize => _positionSize;
        public string PositionSide => _positionSize >= 0 ? "Yes" : "No";
        public double MarketExposure => _marketExposure;
        public double BuyinPrice => _buyinPrice;
        public double PositionUpside => _positionUpside;
        public double PositionDownside => _positionDownside;
        public double PositionROIAmt => _positionROIAmt;
        public long TotalPositionTraded => _totalpositionTraded;
        public double RealizedPnl => _realizedPnl;
        public double FeesPaid => _feesPaid;
        public List<OrderDTO> RestingOrders { get { return _restingOrders; } set { _restingOrders = value; } }
        public double PositionROI => _positionROI;

        /// <summary>
        /// Asynchronously builds pseudo candlesticks for the specified period and lookback periods.
        /// This method performs CPU-intensive calculations to generate candlestick data from historical and real-time market data.
        /// Optimized for high-frequency scenarios with reduced redundant operations, efficient data structures, minimized LINQ queries,
        /// binary search for large dataset lookups, and parallel processing where appropriate to improve performance for large datasets
        /// while maintaining correctness. All processing is offloaded to background threads to ensure non-blocking operation.
        /// </summary>
        /// <param name="period">The time period for the candlesticks ("minute", "hour", or "day").</param>
        /// <param name="lookbackPeriods">The number of periods to look back for data aggregation. Defaults to 0, which uses the configured default.</param>
        /// <returns>A task that represents the asynchronous operation, containing the list of pseudo candlesticks.</returns>
        public async Task<List<PseudoCandlestick>> BuildPseudoCandlesticks(string period, int lookbackPeriods = 0)
        {
            _logger.LogDebug("Starting BuildPseudoCandlesticks: period={Period}, lookbackPeriods={Lookback}", period, lookbackPeriods);
            return await Task.Run(() =>
            {
                // Determine interval based on period
                TimeSpan interval;
                string candlestickKey;
                Func<DateTime, DateTime> truncateFunc;
                switch (period.ToLower())
                {
                    case "minute":
                        candlestickKey = "minute";
                        interval = TimeSpan.FromMinutes(1);
                        truncateFunc = dt => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, DateTimeKind.Utc);
                        break;
                    case "hour":
                        candlestickKey = "hour";
                        interval = TimeSpan.FromHours(1);
                        truncateFunc = dt => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, DateTimeKind.Utc);
                        break;
                    case "day":
                        candlestickKey = "day";
                        interval = TimeSpan.FromDays(1);
                        truncateFunc = dt => new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Utc);
                        break;
                    default:
                        throw new ArgumentException("Period must be 'minute', 'hour', or 'day'.");
                }
                _logger.LogDebug("Selected candlestickKey={Key}, interval={Interval}", candlestickKey, interval);

                // Use default lookback if not specified
                if (lookbackPeriods == 0)
                {
                    lookbackPeriods = _marketDataConfig.Calculations.PseudoCandlestickLookbackPeriods;
                }

                // Retrieve and sort data once (ascending for binary search efficiency)
                var candlesticks = _candlesticks.ContainsKey(candlestickKey)
                    ? _candlesticks[candlestickKey].OrderBy(c => c.Date).ToList()
                    : new List<CandlestickData>();
                var tickers = _tickers.OrderBy(t => t.LoggedDate).ToList();
                _logger.LogDebug("Retrieved {Count} candlesticks, {TickerCount} tickers", candlesticks.Count, tickers.Count);

                // Truncate current time to the start of the current period
                DateTime now = DateTime.UtcNow;
                DateTime currentPeriodStart = truncateFunc(now);
                _logger.LogDebug("Current period start={CurrentPeriodStart}", currentPeriodStart);

                // Calculate lookback to cover lookbackPeriods intervals
                TimeSpan lookback = interval * lookbackPeriods;
                DateTime startTime = truncateFunc(currentPeriodStart - lookback);
                _logger.LogDebug("StartTime={StartTime}, lookback={Lookback}", startTime, lookback);

                // Filter candlesticks to the lookback timeframe using binary search for efficiency
                int candlestickStartIndex = BinarySearchLeftmost(candlesticks, startTime, c => c.Date);
                int candlestickEndIndex = BinarySearchRightmost(candlesticks, currentPeriodStart, c => c.Date);
                var filteredCandlesticks = candlesticks.GetRange(candlestickStartIndex, candlestickEndIndex - candlestickStartIndex + 1);
                _logger.LogDebug("Filtered {Count} candlesticks within lookback timeframe", filteredCandlesticks.Count);

                // Generate intervals as array for efficiency
                int intervalCount = (int)((currentPeriodStart - startTime).TotalSeconds / interval.TotalSeconds) + 1;
                DateTime[] allIntervals = new DateTime[intervalCount];
                for (int i = 0; i < intervalCount; i++)
                {
                    allIntervals[i] = startTime + interval * i;
                }
                _logger.LogDebug("Generated {Count} intervals", allIntervals.Length);

                // Take the most recent lookbackPeriods intervals
                int startIdx = Math.Max(0, allIntervals.Length - lookbackPeriods);
                DateTime[] targetIntervals = new DateTime[allIntervals.Length - startIdx];
                Array.Copy(allIntervals, startIdx, targetIntervals, 0, targetIntervals.Length);
                if (targetIntervals.Length < lookbackPeriods)
                {
                    _logger.LogWarning("Only {Count} intervals available, expected {Expected}", targetIntervals.Length, lookbackPeriods);
                }

                var result = new List<PseudoCandlestick>(targetIntervals.Length);

                // Find the last available data before startTime for forward filling gaps at the beginning
                PseudoCandlestick initialPrevious = null;
                DateTime beforeStart = startTime - TimeSpan.FromTicks(1);

                // Find last candlestick before startTime
                int lastCandleIdx = BinarySearchRightmost(candlesticks, beforeStart, c => c.Date);
                if (lastCandleIdx >= 0)
                {
                    var lastCandle = candlesticks[lastCandleIdx];
                    initialPrevious = new PseudoCandlestick
                    {
                        Timestamp = lastCandle.Date,
                        MidClose = (lastCandle.AskClose + lastCandle.BidClose) / 2.0,
                        MidHigh = (lastCandle.AskHigh + lastCandle.BidHigh) / 2.0,
                        MidLow = (lastCandle.AskLow + lastCandle.BidLow) / 2.0,
                        Volume = lastCandle.Volume,
                        IsFromCandlestick = true
                    };
                }

                // Find last ticker before startTime
                int lastTickerIdx = BinarySearchRightmost(tickers, beforeStart, t => t.LoggedDate);
                if (lastTickerIdx >= 0)
                {
                    var lastTicker = tickers[lastTickerIdx];
                    var tickerPseudo = new PseudoCandlestick
                    {
                        Timestamp = lastTicker.LoggedDate,
                        MidClose = (lastTicker.yes_ask + lastTicker.yes_bid) / 2.0,
                        MidHigh = (lastTicker.yes_ask + lastTicker.yes_bid) / 2.0,
                        MidLow = (lastTicker.yes_ask + lastTicker.yes_bid) / 2.0,
                        Volume = lastTicker.volume,
                        IsFromCandlestick = false
                    };
                    // If both exist, take the more recent
                    if (initialPrevious == null || tickerPseudo.Timestamp > initialPrevious.Timestamp)
                    {
                        initialPrevious = tickerPseudo;
                    }
                }

                PseudoCandlestick previousCandlestick = initialPrevious;

                // Process intervals sequentially to maintain carry-forward logic
                foreach (var periodStart in targetIntervals)
                {
                    DateTime periodEnd = periodStart + interval;
                    DateTime periodTimestamp = periodEnd - TimeSpan.FromMilliseconds(1);
                    _logger.LogDebug("Processing interval: {Start} to {End}, Timestamp={Timestamp}", periodStart, periodEnd, periodTimestamp);

                    bool isCurrentInterval = periodStart == currentPeriodStart;
                    DateTime endTime = isCurrentInterval ? now : periodEnd;

                    // Find candlestick using binary search
                    int candleIdx = BinarySearchRightmost(filteredCandlesticks, endTime, c => c.Date);
                    CandlestickData candle = null;
                    if (candleIdx >= 0 && filteredCandlesticks[candleIdx].Date >= periodStart)
                    {
                        candle = filteredCandlesticks[candleIdx];
                    }

                    if (candle != null)
                    {
                        var currentPseudoCandlestick = new PseudoCandlestick
                        {
                            Timestamp = periodTimestamp,
                            MidClose = (candle.AskClose + candle.BidClose) / 2.0,
                            MidHigh = (candle.AskHigh + candle.BidHigh) / 2.0,
                            MidLow = (candle.AskLow + candle.BidLow) / 2.0,
                            Volume = candle.Volume,
                            IsFromCandlestick = true
                        };
                        _logger.LogDebug("PseudoCandlestick: Time={Time}, MidClose={MidClose}, Source=Candlestick",
                            currentPseudoCandlestick.Timestamp, currentPseudoCandlestick.MidClose);
                        result.Add(currentPseudoCandlestick);
                        previousCandlestick = currentPseudoCandlestick;
                    }
                    else
                    {
                        // Aggregate tickers using binary search for range
                        int tickerStartIdx = BinarySearchLeftmost(tickers, periodStart, t => t.LoggedDate);
                        int tickerEndIdx = BinarySearchRightmost(tickers, endTime, t => t.LoggedDate);
                        if (tickerStartIdx <= tickerEndIdx && tickerEndIdx >= 0)
                        {
                            // Aggregate in parallel if many tickers
                            var intervalTickers = tickers.GetRange(tickerStartIdx, tickerEndIdx - tickerStartIdx + 1);
                            if (intervalTickers.Count > 0)
                            {
                                var lastTicker = intervalTickers[intervalTickers.Count - 1]; // Since sorted descending, last is oldest? Wait, no: sorted descending, so [0] is most recent, [Count-1] is oldest
                                double tickerMidClose = (double)(lastTicker.yes_ask + lastTicker.yes_bid) / 2;
                                double maxMid = double.MinValue;
                                double minMid = double.MaxValue;
                                decimal totalVolume = 0;
                                foreach (var t in intervalTickers)
                                {
                                    double mid = (double)(t.yes_ask + t.yes_bid) / 2;
                                    if (mid > maxMid) maxMid = mid;
                                    if (mid < minMid) minMid = mid;
                                    totalVolume += (decimal)t.volume;
                                }
                                var currentPseudoCandlestick = new PseudoCandlestick
                                {
                                    Timestamp = periodTimestamp,
                                    MidClose = tickerMidClose,
                                    MidHigh = maxMid,
                                    MidLow = minMid,
                                    Volume = totalVolume,
                                    IsFromCandlestick = false
                                };
                                _logger.LogDebug("PseudoCandlestick: Time={Time}, MidClose={MidClose}, Source=Ticker",
                                    currentPseudoCandlestick.Timestamp, currentPseudoCandlestick.MidClose);
                                result.Add(currentPseudoCandlestick);
                                previousCandlestick = currentPseudoCandlestick;
                            }
                            else if (previousCandlestick != null)
                            {
                                var currentPseudoCandlestick = new PseudoCandlestick
                                {
                                    Timestamp = periodTimestamp,
                                    MidClose = previousCandlestick.MidClose,
                                    MidHigh = previousCandlestick.MidHigh,
                                    MidLow = previousCandlestick.MidLow,
                                    Volume = 0,
                                    IsFromCandlestick = false
                                };
                                _logger.LogDebug("PseudoCandlestick: Time={Time}, MidClose={MidClose}, Source=CarryForward",
                                    currentPseudoCandlestick.Timestamp, currentPseudoCandlestick.MidClose);
                                result.Add(currentPseudoCandlestick);
                                previousCandlestick = currentPseudoCandlestick;
                            }
                            else
                            {
                                _logger.LogDebug("No data for interval {Start}, skipping", periodStart);
                            }
                        }
                        else if (previousCandlestick != null)
                        {
                            var currentPseudoCandlestick = new PseudoCandlestick
                            {
                                Timestamp = periodTimestamp,
                                MidClose = previousCandlestick.MidClose,
                                MidHigh = previousCandlestick.MidHigh,
                                MidLow = previousCandlestick.MidLow,
                                Volume = 0,
                                IsFromCandlestick = false
                            };
                            _logger.LogDebug("PseudoCandlestick: Time={Time}, MidClose={MidClose}, Source=CarryForward",
                                currentPseudoCandlestick.Timestamp, currentPseudoCandlestick.MidClose);
                            result.Add(currentPseudoCandlestick);
                            previousCandlestick = currentPseudoCandlestick;
                        }
                        else
                        {
                            _logger.LogDebug("No data for interval {Start}, skipping", periodStart);
                        }
                    }
                }

                _logger.LogDebug("Produced {Count} candlesticks", result.Count);
                return result;
            });
        }

        /// <summary>
        /// Performs binary search to find the leftmost index where the key is greater than or equal to the target.
        /// </summary>
        private static int BinarySearchLeftmost<T>(List<T> list, DateTime target, Func<T, DateTime> selector)
        {
            int low = 0;
            int high = list.Count - 1;
            int result = list.Count;
            while (low <= high)
            {
                int mid = low + (high - low) / 2;
                if (selector(list[mid]) >= target)
                {
                    result = mid;
                    high = mid - 1;
                }
                else
                {
                    low = mid + 1;
                }
            }
            return result;
        }

        /// <summary>
        /// Performs binary search to find the rightmost index where the key is less than or equal to the target.
        /// </summary>
        private static int BinarySearchRightmost<T>(List<T> list, DateTime target, Func<T, DateTime> selector)
        {
            int low = 0;
            int high = list.Count - 1;
            int result = -1;
            while (low <= high)
            {
                int mid = low + (high - low) / 2;
                if (selector(list[mid]) <= target)
                {
                    result = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }
            return result;
        }

        public List<PseudoCandlestick> RecentCandlesticks { get { return _recentCandlesticks; } set { _recentCandlesticks = value; } }
        private List<PseudoCandlestick> _recentCandlesticks = new List<PseudoCandlestick>();

        private List<PseudoCandlestick> _minutePseudoCandlesticks = new List<PseudoCandlestick>();
        private List<PseudoCandlestick> _hourPseudoCandlesticks = new List<PseudoCandlestick>();
        private List<PseudoCandlestick> _dayPseudoCandlesticks = new List<PseudoCandlestick>();

        public TimeSpan? HoldTime
        {
            get
            {
                if (_positions != null && _positions.Count > 0 && _positions[0].LastUpdatedUTC != null && _positions[0].Position != 0)
                {
                    return DateTime.UtcNow - _positions[0].LastUpdatedUTC;
                }
                return null;
            }
        }

        public TimeSpan? MarketAge
        {
            get
            {
                return DateTime.UtcNow - _marketInfo.open_time;
            }
        }
        public TimeSpan? TimeLeft
        {
            get
            {
                return _marketInfo.close_time - DateTime.UtcNow;
            }
        }

        public bool CanCloseEarly
        {
            get { return _marketInfo.can_close_early; }
        }

        private List<SupportResistanceLevel> _allSupportResistanceLevels = new List<SupportResistanceLevel>();


        public List<SupportResistanceLevel> AllSupportResistanceLevels
        {
            get => _allSupportResistanceLevels;
            set
            {
                _allSupportResistanceLevels = value;
            }
        }

        private double _currentTradeRatePerMinute_Yes;
        private double _currentTradeRatePerMinute_No;
        private double _currentTradeVolumePerMinute_No;
        private double _currentTradeVolumePerMinute_Yes;
        private double _currentTradeCount_Yes;
        private double _currentTradeCount_No;
        private double _currentOrderVolumePerMinute_YesBid;
        private double _currentOrderVolumePerMinute_NoBid;
        private double _currentNonTradeRelatedOrderCount_Yes;
        private double _currentNonTradeRelatedOrderCount_No;
        private double _currentAverageTradeSize_Yes;
        private double _currentAverageTradeSize_No;

        /// <summary>
        /// Gets or sets the current trade rate per minute for yes positions.
        /// Represents the most recent trade rate calculated from live data.
        /// </summary>
        public double CurrentTradeRatePerMinute_Yes
        {
            get => _currentTradeRatePerMinute_Yes;
            set => _currentTradeRatePerMinute_Yes = value;
        }

        /// <summary>
        /// Gets or sets the current trade rate per minute for no positions.
        /// Represents the most recent trade rate calculated from live data.
        /// </summary>
        public double CurrentTradeRatePerMinute_No
        {
            get => _currentTradeRatePerMinute_No;
            set => _currentTradeRatePerMinute_No = value;
        }

        public double CurrentTradeVolumePerMinute_No
        {
            get => _currentTradeVolumePerMinute_No;
            set => _currentTradeVolumePerMinute_No = value;
        }

        public double CurrentTradeVolumePerMinute_Yes
        {
            get => _currentTradeVolumePerMinute_Yes;
            set => _currentTradeVolumePerMinute_Yes = value;
        }

        public double CurrentTradeCount_Yes
        {
            get => _currentTradeCount_Yes;
            set => _currentTradeCount_Yes = value;
        }

        public double CurrentTradeCount_No
        {
            get => _currentTradeCount_No;
            set => _currentTradeCount_No = value;
        }

        public double CurrentOrderVolumePerMinute_YesBid
        {
            get => _currentOrderVolumePerMinute_YesBid;
            set => _currentOrderVolumePerMinute_YesBid = value;
        }

        public double CurrentOrderVolumePerMinute_NoBid
        {
            get => _currentOrderVolumePerMinute_NoBid;
            set => _currentOrderVolumePerMinute_NoBid = value;
        }

        public double CurrentNonTradeRelatedOrderCount_Yes
        {
            get => _currentNonTradeRelatedOrderCount_Yes;
            set => _currentNonTradeRelatedOrderCount_Yes = value;
        }

        public double CurrentNonTradeRelatedOrderCount_No
        {
            get => _currentNonTradeRelatedOrderCount_No;
            set => _currentNonTradeRelatedOrderCount_No = value;
        }

        public double CurrentAverageTradeSize_Yes
        {
            get => _currentAverageTradeSize_Yes;
            set => _currentAverageTradeSize_Yes = value;
        }

        public double CurrentAverageTradeSize_No
        {
            get => _currentAverageTradeSize_No;
            set => _currentAverageTradeSize_No = value;
        }


        public List<SupportResistanceLevel> GetFilteredSupportResistanceLevels()
        {
            if (AllSupportResistanceLevels == null)
            {
                _logger.LogDebug("AllSupportResistanceLevels is null for {MarketTicker}", MarketTicker);
                return new List<SupportResistanceLevel>();
            }
            return AllSupportResistanceLevels.ToList();
        }


    }
}
