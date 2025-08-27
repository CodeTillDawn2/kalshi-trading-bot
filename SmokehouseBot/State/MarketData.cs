// TradingStrategies.Models/MarketData.cs
using Microsoft.Extensions.Options;
using SmokehouseBot.Helpers;
using SmokehouseBot.Services.Interfaces;
using SmokehouseBot.State.Interfaces;
using SmokehouseDTOs;
using SmokehouseDTOs.Data;
using System.Collections.Concurrent;
using TradingStrategies.Configuration;
using TradingStrategies.Helpers.Interfaces;

namespace SmokehouseBot.State
{
    public class MarketData : IMarketData
    {
        private readonly CalculationConfig _calculationConfig;

        private readonly ILogger<IMarketData> _logger;
        private readonly IOrderbookChangeTracker _changeTracker;
        private readonly ITradingCalculator _tradingCalculator;
        private string _marketTicker;
        private MarketDTO _marketInfo;
        private readonly Dictionary<string, List<CandlestickData>> _candlesticks;
        private ConcurrentBag<TickerDTO> _tickers;
        private List<MarketPositionDTO> _positions;
        private List<OrderbookData> _orderbookData;
        private DateTime _lastWebSocketMessageReceived;
        private DateTime _lastOrderbookEventTimestamp;
        private string _marketCategory = "";

        public string MarketCategory { get => _marketCategory; set => _marketCategory = value; }

        private string _marketStatus;
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

        private string _goodBadPriceYes = "test1";
        private string _goodBadPriceNo = "test2";
        private string _marketBehaviorYes = "yest2";
        private string _marketBehaviorNo = "not2";

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
        private List<OrderDTO> _restingOrders;
        private double _positionROI;
        private double _positionROIAmt;
        private double _expectedFees;
        private double _yesBidSlopePerMinute = 0;
        private double _noBidSlopePerMinute = 0;

        public double YesBidSlopePerMinute { get { return _yesBidSlopePerMinute; } set { _yesBidSlopePerMinute = value; } }
        public double NoBidSlopePerMinute { get { return _noBidSlopePerMinute; } set { _noBidSlopePerMinute = value; } }

        public string MarketType { get; set; }

        private double _tolerancePercentage = 10.0;

        public bool ChangeMetricsMature => _changeTracker.IsMature;

        public MarketData(
            MarketDTO market,
            ILogger<IMarketData> logger,
            ITradingCalculator tradingCalculator,
            IOrderbookChangeTracker changeTracker,
            IOptions<CalculationConfig> calculationConfig)
        {
            _calculationConfig = calculationConfig?.Value ?? throw new ArgumentNullException(nameof(calculationConfig));
            _marketTicker = market.market_ticker;
            _marketCategory = market.category;
            _marketInfo = market;
            _marketStatus = market.status;
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

        public double VelocityPerMinute_Top_Yes_Bid { get { return _velocityPerMinute_Top_Yes_Bid; } set { _velocityPerMinute_Top_Yes_Bid = value; } }
        public double VelocityPerMinute_Top_No_Bid { get { return _velocityPerMinute_Top_No_Bid; } set { _velocityPerMinute_Top_No_Bid = value; } }
        public double VelocityPerMinute_Bottom_Yes_Bid { get { return _VelocityPerMinute_Bottom_Yes_Bid; } set { _VelocityPerMinute_Bottom_Yes_Bid = value; } }
        public double VelocityPerMinute_Bottom_No_Bid { get { return _VelocityPerMinute_Bottom_No_Bid; } set { _VelocityPerMinute_Bottom_No_Bid = value; } }

        private double _tradeVolumePerMinute_Yes = 0;
        private double _tradeVolumePerMinute_No = 0;
        private double _tradeRatePerMinute_Yes = 0;
        private double _tradeRatePerMinute_No = 0;

        public double TradeVolumePerMinute_Yes { get { return _tradeVolumePerMinute_Yes; } set { _tradeVolumePerMinute_Yes = value; } }
        public double TradeVolumePerMinute_No { get { return _tradeVolumePerMinute_No; } set { _tradeVolumePerMinute_No = value; } }
        public double TradeRatePerMinute_Yes { get { return _tradeRatePerMinute_Yes; } set { _tradeRatePerMinute_Yes = value; } }
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

        public int DepthAtTop4YesAsks => GetBids("no")
            .OrderByDescending(o => o.Price)
            .Take(4)
            .Sum(o => o.RestingContracts);

        public int DepthAtTop4NoAsks => GetBids("yes")
            .OrderByDescending(o => o.Price)
            .Take(4)
            .Sum(o => o.RestingContracts);

        public double YesBidCenterOfMass => GetBids("yes").Any() ?
            Math.Round(GetBids("yes").Sum(o => o.Price * (double)o.Price * o.RestingContracts) /
            GetBids("yes").Sum(o => o.Price * (double)o.RestingContracts), 2) : 0;

        public double NoBidCenterOfMass => GetBids("no").Any() ?
            Math.Round(GetBids("no").Sum(o => o.Price * (double)o.Price * o.RestingContracts) /
            GetBids("no").Sum(o => o.Price * (double)o.RestingContracts), 2) : 0;

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

        public void RefreshAllMetadata()
        {
            RefreshCandlestickMetadata();
            RefreshTickerMetadata();
            RefreshPositionMetadata();
        }

        public void RefreshCandlestickMetadata()
        {
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


                List<CandlestickData> recentCandlesticks = _candlesticks["minute"].Where(x => x.Date >= DateTime.UtcNow.AddDays(-1)).ToList();
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

        public void RefreshTickerMetadata()
        {
            if (!_tickers.IsEmpty)
            {
                _mostRecentTicker = _tickers.OrderByDescending(t => t.LoggedDate).FirstOrDefault()?.LoggedDate ?? default;
            }

            var now = DateTime.UtcNow;
            var fiveMinAgo = now.AddMinutes(-5);
            var recentTickers = _tickers
                .Where(t => t.LoggedDate >= fiveMinAgo && t.LoggedDate <= now)
                .OrderBy(t => t.LoggedDate)
                .ToList();

            if (recentTickers.Count < 2)
            {
                _yesBidSlopePerMinute = 0;
                _noBidSlopePerMinute = 0;
                return;
            }

            var first = recentTickers.First();
            var last = recentTickers.Last();
            var timeDiffMin = (last.LoggedDate - first.LoggedDate).TotalMinutes;

            if (timeDiffMin <= 0)
            {
                _yesBidSlopePerMinute = 0;
                _noBidSlopePerMinute = 0;
                return;
            }

            _yesBidSlopePerMinute = (last.yes_bid - first.yes_bid) / timeDiffMin;
            _noBidSlopePerMinute = ((100 - last.yes_ask) - (100 - first.yes_ask)) / timeDiffMin;

            UpdateTradingMetrics();
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

        public void UpdateTradingMetrics()
        {
            _logger.LogDebug("**Started updating trading metrics for {marketTicker}**", _marketTicker);
            _minutePseudoCandlesticks = BuildPseudoCandlesticks("minute");
            _hourPseudoCandlesticks = BuildPseudoCandlesticks("hour");
            _dayPseudoCandlesticks = BuildPseudoCandlesticks("day");
            _logger.LogDebug("MinutePseudoCandlesticks: [{MidCloses}]",
                string.Join(", ", _minutePseudoCandlesticks.Select(pc => pc.MidClose)));
            _logger.LogDebug("HourPseudoCandlesticks: [{MidCloses}]",
                string.Join(", ", _hourPseudoCandlesticks.Select(pc => pc.MidClose)));
            _logger.LogDebug("DayPseudoCandlesticks: [{MidCloses}]",
                string.Join(", ", _dayPseudoCandlesticks.Select(pc => pc.MidClose)));
            var minuteCopy = _minutePseudoCandlesticks.ToList();
            var hourCopy = _hourPseudoCandlesticks.ToList();
            var dayCopy = _dayPseudoCandlesticks.ToList();

            _rsi_Short = _tradingCalculator.CalculateRSI(minuteCopy, _calculationConfig.RSI_Short_Periods);
            if (_rsi_Short != null) _rsi_Short = Math.Round((double)_rsi_Short, 2);
            _rsi_Medium = _tradingCalculator.CalculateRSI(hourCopy, _calculationConfig.RSI_Medium_Periods);
            if (_rsi_Medium != null) _rsi_Medium = Math.Round((double)_rsi_Medium, 2);
            _rsi_Long = _tradingCalculator.CalculateRSI(dayCopy, _calculationConfig.RSI_Long_Periods);
            if (_rsi_Long != null) _rsi_Long = Math.Round((double)_rsi_Long, 2);

            _macd_Medium = _tradingCalculator.CalculateMACD(hourCopy,
                _calculationConfig.MACD_Medium_FastPeriod,
                _calculationConfig.MACD_Medium_SlowPeriod,
                _calculationConfig.MACD_Medium_SignalPeriod);

            if (_macd_Medium.MACD != null) _macd_Medium.MACD = Math.Round((double)_macd_Medium.MACD, 2);
            if (_macd_Medium.Signal != null) _macd_Medium.Signal = Math.Round((double)_macd_Medium.Signal, 2);
            if (_macd_Medium.Histogram != null) _macd_Medium.Histogram = Math.Round((double)_macd_Medium.Histogram, 2);

            _macd_Long = _tradingCalculator.CalculateMACD(dayCopy,
                _calculationConfig.MACD_Long_FastPeriod,
                _calculationConfig.MACD_Long_SlowPeriod,
                _calculationConfig.MACD_Long_SignalPeriod);

            if (_macd_Long.MACD != null) _macd_Long.MACD = Math.Round((double)_macd_Long.MACD, 2);
            if (_macd_Long.Signal != null) _macd_Long.Signal = Math.Round((double)_macd_Long.Signal, 2);
            if (_macd_Long.Histogram != null) _macd_Long.Histogram = Math.Round((double)_macd_Long.Histogram, 2);


            _ema_Medium = TradingCalculations.CalculateEMA(hourCopy.Select(c => c.MidClose).ToList(),
            _calculationConfig.EMA_Medium_Periods);
            if (_ema_Medium != null) _ema_Medium = Math.Round((double)_ema_Medium, 2);


            _ema_Long = TradingCalculations.CalculateEMA(dayCopy.Select(c => c.MidClose).ToList(),
                _calculationConfig.EMA_Long_Periods);
            if (_ema_Long != null) _ema_Long = Math.Round((double)_ema_Long, 2);

            _bollingerbands_Medium = _tradingCalculator.CalculateBollingerBands(hourCopy,
                _calculationConfig.BollingerBands_Medium_Periods,
                _calculationConfig.BollingerBands_Medium_StdDev);

            if (_bollingerbands_Medium.lower != null) _bollingerbands_Medium.lower = Math.Round((double)_bollingerbands_Medium.lower, 2);
            if (_bollingerbands_Medium.middle != null) _bollingerbands_Medium.middle = Math.Round((double)_bollingerbands_Medium.middle, 2);
            if (_bollingerbands_Medium.upper != null) _bollingerbands_Medium.upper = Math.Round((double)_bollingerbands_Medium.upper, 2);

            _bollingerbands_Long = _tradingCalculator.CalculateBollingerBands(dayCopy,
                _calculationConfig.BollingerBands_Long_Periods,
                _calculationConfig.BollingerBands_Long_StdDev);
            if (_bollingerbands_Long.lower != null) _bollingerbands_Long.lower = Math.Round((double)_bollingerbands_Long.lower, 2);
            if (_bollingerbands_Long.middle != null) _bollingerbands_Long.middle = Math.Round((double)_bollingerbands_Long.middle, 2);
            if (_bollingerbands_Long.upper != null) _bollingerbands_Long.upper = Math.Round((double)_bollingerbands_Long.upper, 2);

            _atr_Medium = _tradingCalculator.CalculateATR(hourCopy, _calculationConfig.ATR_Medium_Periods);
            if (_atr_Medium != null) _atr_Medium = Math.Round((double)_atr_Medium, 2);
            _atr_Long = _tradingCalculator.CalculateATR(dayCopy, _calculationConfig.ATR_Long_Periods);
            if (_atr_Long != null) _atr_Long = Math.Round((double)_atr_Long, 2);
            _vwap_Short = (double?)_tradingCalculator.CalculateVWAP(minuteCopy, periods: _calculationConfig.VWAP_Short_Periods);
            if (_vwap_Short != null) _vwap_Short = Math.Round((double)_vwap_Short, 2);
            _vwap_Medium = (double?)_tradingCalculator.CalculateVWAP(hourCopy, periods: _calculationConfig.VWAP_Medium_Periods);
            if (_vwap_Medium != null) _vwap_Medium = Math.Round((double)_vwap_Medium, 2);
            _stochasticoscilator_Short = _tradingCalculator.CalculateStochastic(minuteCopy,
                _calculationConfig.Stochastic_Short_Periods,
                3);
            if (_stochasticoscilator_Short.K != null) _stochasticoscilator_Short.K = Math.Round((double)_stochasticoscilator_Short.K, 2);
            if (_stochasticoscilator_Short.D != null) _stochasticoscilator_Short.D = Math.Round((double)_stochasticoscilator_Short.D, 2);
            _stochasticoscilator_Medium = _tradingCalculator.CalculateStochastic(hourCopy,
                _calculationConfig.Stochastic_Medium_Periods,
                3);
            if (_stochasticoscilator_Medium.K != null) _stochasticoscilator_Medium.K = Math.Round((double)_stochasticoscilator_Medium.K, 2);
            if (_stochasticoscilator_Medium.D != null) _stochasticoscilator_Medium.D = Math.Round((double)_stochasticoscilator_Medium.D, 2);
            _stochasticoscilator_Long = _tradingCalculator.CalculateStochastic(dayCopy,
                _calculationConfig.Stochastic_Long_Periods,
                3);
            if (_stochasticoscilator_Long.K != null) _stochasticoscilator_Long.K = Math.Round((double)_stochasticoscilator_Long.K, 2);
            if (_stochasticoscilator_Long.D != null) _stochasticoscilator_Long.D = Math.Round((double)_stochasticoscilator_Long.D, 2);
            _obv_Medium = (long)_tradingCalculator.CalculateOBV(hourCopy);

            _obv_Long = (long)_tradingCalculator.CalculateOBV(dayCopy);
            
            ADX = _tradingCalculator.CalculateADX(minuteCopy, _calculationConfig.ADX_Periods);

            _logger.LogDebug("**Ended updating trading metrics for {marketTicker}**", _marketTicker);
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
            // fees = roundup(0.07 * C * P * (1-P))
            double fee = 0.07 * contracts * priceInDollars * (1 - priceInDollars);
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

        public List<PseudoCandlestick> BuildPseudoCandlesticks(string period, int lookbackPeriods = 34)
        {
            _logger.LogDebug("Starting BuildPseudoCandlesticks: period={Period}, lookbackPeriods={Lookback}", period, lookbackPeriods);

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

            // Retrieve and sort data
            var candlesticks = _candlesticks.ContainsKey(candlestickKey)
                ? _candlesticks[candlestickKey].OrderByDescending(c => c.Date).ToList()
                : new List<CandlestickData>();
            var tickers = _tickers.OrderByDescending(t => t.LoggedDate).ToList();
            _logger.LogDebug("Retrieved {Count} candlesticks, {TickerCount} tickers", candlesticks.Count, tickers.Count);

            // Truncate current time to the start of the current period
            DateTime now = DateTime.UtcNow;
            DateTime currentPeriodStart = truncateFunc(now);
            _logger.LogDebug("Current period start={CurrentPeriodStart}", currentPeriodStart);

            // Calculate lookback to cover lookbackPeriods intervals
            TimeSpan lookback = interval * lookbackPeriods;
            DateTime startTime = truncateFunc(currentPeriodStart - lookback);
            _logger.LogDebug("StartTime={StartTime}, lookback={Lookback}", startTime, lookback);

            // Filter candlesticks to the lookback timeframe
            var filteredCandlesticks = candlesticks
                .Where(c => c.Date >= startTime && c.Date <= currentPeriodStart)
                .ToList();
            _logger.LogDebug("Filtered {Count} candlesticks within lookback timeframe", filteredCandlesticks.Count);

            // Generate all intervals within the lookback timeframe
            var allIntervals = new List<DateTime>();
            for (DateTime t = startTime; t <= currentPeriodStart; t += interval)
            {
                allIntervals.Add(t);
            }
            _logger.LogDebug("Generated {Count} intervals", allIntervals.Count);

            // Take the most recent lookbackPeriods intervals
            var targetIntervals = allIntervals.OrderByDescending(t => t).Take(lookbackPeriods).OrderBy(t => t).ToList();
            if (targetIntervals.Count < lookbackPeriods)
            {
                _logger.LogWarning("Only {Count} intervals available, expected {Expected}", targetIntervals.Count, lookbackPeriods);
            }

            var result = new List<PseudoCandlestick>();
            PseudoCandlestick previousCandlestick = null;

            foreach (var periodStart in targetIntervals)
            {
                DateTime periodEnd = periodStart + interval;
                // Set timestamp to the last millisecond of the period (e.g., 12:29:59.999 for minute period)
                DateTime periodTimestamp = periodEnd - TimeSpan.FromMilliseconds(1);
                _logger.LogDebug("Processing interval: {Start} to {End}, Timestamp={Timestamp}", periodStart, periodEnd, periodTimestamp);

                // For the current interval, include partial data if it's the latest
                bool isCurrentInterval = periodStart == currentPeriodStart;
                var candle = filteredCandlesticks
                    .Where(c => c.Date >= periodStart && (isCurrentInterval ? c.Date <= now : c.Date < periodEnd))
                    .OrderByDescending(c => c.Date)
                    .FirstOrDefault();

                if (candle != null)
                {
                    var currentPseudoCandlestick = new PseudoCandlestick
                    {
                        Timestamp = periodTimestamp, // Last millisecond of the period
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
                    // Aggregate tickers within the interval
                    var intervalTickers = tickers
                        .Where(t => t.LoggedDate >= periodStart && (isCurrentInterval ? t.LoggedDate <= now : t.LoggedDate < periodEnd))
                        .OrderBy(t => t.LoggedDate)
                        .ToList();

                    if (intervalTickers.Any())
                    {
                        var lastTicker = intervalTickers.Last();
                        double tickerMidClose = (double)(lastTicker.yes_ask + lastTicker.yes_bid) / 2;
                        decimal totalVolume = intervalTickers.Sum(t => (decimal)t.volume);
                        var currentPseudoCandlestick = new PseudoCandlestick
                        {
                            Timestamp = periodTimestamp, // Last millisecond of the period
                            MidClose = tickerMidClose,
                            MidHigh = intervalTickers.Max(t => (double)(t.yes_ask + t.yes_bid) / 2),
                            MidLow = intervalTickers.Min(t => (double)(t.yes_ask + t.yes_bid) / 2),
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
                            Timestamp = periodTimestamp, // Last millisecond of the period
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
            return result.OrderBy(pc => pc.Timestamp).ToList();
        }

       

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

        private List<SupportResistanceLevel> _allSupportResistanceLevels;


        public List<SupportResistanceLevel> AllSupportResistanceLevels
        {
            get => _allSupportResistanceLevels;
            set
            {
                _allSupportResistanceLevels = value;
            }
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