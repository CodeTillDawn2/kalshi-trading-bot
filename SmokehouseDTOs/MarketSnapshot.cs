using SmokehouseDTOs.Converters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmokehouseDTOs
{
    public class MarketSnapshot
    {
        public MarketSnapshot()
        {
            // Required for JSON deserialization
        }

        public MarketSnapshot(
            DateTime marketTimestamp,
            string marketTicker,
            string marketCategory,
            string marketStatus,
            string marketType,
            int bestYesBid,
            int bestNoBid,
            List<Dictionary<string, object>> orderbookData,
            (int Bid, DateTime When) allTimeHighYesBid,
            (int Bid, DateTime When) allTimeLowYesBid,
            (int Bid, DateTime When) allTimeHighNoBid,
            (int Bid, DateTime When) allTimeLowNoBid,
            (int Bid, DateTime When) recentHighYesBid,
            (int Bid, DateTime When) recentLowYesBid,
            (int Bid, DateTime When) recentHighNoBid,
            (int Bid, DateTime When) recentLowNoBid,
            List<SupportResistanceLevel> allSupportResistanceLevels,
            int positionSize,
            double marketExposure,
            double buyinPrice,
            double positionUpside,
            double positionDownside,
            long totalTraded,
            List<(string action, string side, string type, int count, int price, DateTime? expiration)> restingOrders,
            double realizedPnl,
            double feesPaid,
            double positionROI,
            double expectedFees,
            double positionROIAmt,
            double tradeRatePerMinuteYes,
            double tradeRatePerMinuteNo,
            double tradeVolumePerMinuteYes,
            double tradeVolumePerMinuteNo,
            int tradeCountYes,
            int tradeCountNo,
            double orderVolumePerMinuteYesBid,
            double orderVolumePerMinuteNoBid,
            int nonTradeRelatedOrderCountYes,
            int nonTradeRelatedOrderCountNo,
            double averageTradeSizeYes,
            double averageTradeSizeNo,
            double highestVolumeDay,
            double highestVolumeHour,
            double highestVolumeMinute,
            double recentVolumeLastHour,
            double recentVolumeLastThreeHours,
            double recentVolumeLastMonth,
            double velocityPerMinuteBottomYesBid,
            int levelCountBottomYesBid,
            double velocityPerMinuteBottomNoBid,
            int levelCountBottomNoBid,
            double velocityPerMinuteTopYesBid,
            int levelCountTopYesBid,
            double velocityPerMinuteTopNoBid,
            int levelCountTopNoBid,
            int yesSpread,
            int noSpread,
            int depthAtBestYesBid,
            int depthAtBestNoBid,
            int cumulativeYesBidDepth,
            int cumulativeNoBidDepth,
            int yesBidRange,
            int noBidRange,
            int totalYesBidContracts,
            int totalNoBidContracts,
            int bidCountImbalance,
            double bidVolumeImbalance,
            int depthAtTop4YesBids,
            int depthAtTop4NoBids,
            double? rsiShort,
            double? rsiMedium,
            double? rsiLong,
            (double? MACD, double? Signal, double? Histogram) macdMedium,
            (double? MACD, double? Signal, double? Histogram) macdLong,
            double? emaMedium,
            double? emaLong,
            (double? lower, double? middle, double? upper) bollingerBandsMedium,
            (double? lower, double? middle, double? upper) bollingerBandsLong,
            double? atrMedium,
            double? atrLong,
            double? vwapShort,
            double? vwapMedium,
            (double? K, double? D) stochasticOscillatorShort,
            (double? K, double? D) stochasticOscillatorMedium,
            (double? K, double? D) stochasticOscillatorLong,
            long obvMedium,
            long obvLong,
            bool changeMetricsMature,
            TimeSpan? marketAge,
            TimeSpan? timeLeft,
            bool canCloseEarly,
            DateTime lastWebSocketMessageReceived,
            string marketBehaviorYes,
            string marketBehaviorNo,
            string goodBadPriceYes,
            string goodBadPriceNo,
            TimeSpan? holdTime,
            double yesBidCenterOfMass,
            double noBidCenterOfMass,
            double tolerancePercentage,
            int snapshotSchemaVersion,
            long totalOrderbookDepth_Yes,
            long totalOrderbookDepth_No,
            double totalBidVolume_Yes,
            double totalBidVolume_No,
            double yesBidSlopePerMinute_Short,
            double noBidSlopePerMinute_Short,
            double yesBidSlopePerMinute_Medium,
            double noBidSlopePerMinute_Medium,
            double? psar,
            double? adx)
        {
            Timestamp = marketTimestamp;
            MarketTicker = marketTicker;
            MarketType = marketType;
            MarketCategory = marketCategory;
            MarketStatus = marketStatus;
            BestYesBid = bestYesBid;
            BestNoBid = bestNoBid;
            OrderbookData = orderbookData;
            AllTimeHighYes_Bid = allTimeHighYesBid;
            AllTimeLowYes_Bid = allTimeLowYesBid;
            AllTimeHighNo_Bid = allTimeHighNoBid;
            AllTimeLowNo_Bid = allTimeLowNoBid;
            RecentHighYes_Bid = recentHighYesBid;
            RecentLowYes_Bid = recentLowYesBid;
            RecentHighYes_Bid = recentHighNoBid;
            RecentLowYes_Bid = recentLowNoBid;
            AllSupportResistanceLevels = allSupportResistanceLevels;
            PositionSize = positionSize;
            MarketExposure = marketExposure;
            BuyinPrice = buyinPrice;
            PositionUpside = positionUpside;
            PositionDownside = positionDownside;
            TotalTraded = totalTraded;
            RestingOrders = restingOrders;
            RealizedPnl = realizedPnl;
            FeesPaid = feesPaid;
            PositionROI = positionROI;
            ExpectedFees = expectedFees;
            PositionROIAmt = positionROIAmt;
            TradeRatePerMinute_Yes = tradeRatePerMinuteYes;
            TradeRatePerMinute_No = tradeRatePerMinuteNo;
            TradeVolumePerMinute_Yes = tradeVolumePerMinuteYes;
            TradeVolumePerMinute_No = tradeVolumePerMinuteNo;
            TradeCount_Yes = tradeCountYes;
            TradeCount_No = tradeCountNo;
            OrderVolumePerMinute_YesBid = orderVolumePerMinuteYesBid;
            OrderVolumePerMinute_NoBid = orderVolumePerMinuteNoBid;
            NonTradeRelatedOrderCount_Yes = nonTradeRelatedOrderCountYes;
            NonTradeRelatedOrderCount_No = nonTradeRelatedOrderCountNo;
            AverageTradeSize_Yes = averageTradeSizeYes;
            AverageTradeSize_No = averageTradeSizeNo;
            HighestVolume_Day = highestVolumeDay;
            HighestVolume_Hour = highestVolumeHour;
            HighestVolume_Minute = highestVolumeMinute;
            RecentVolume_LastHour = recentVolumeLastHour;
            RecentVolume_LastThreeHours = recentVolumeLastThreeHours;
            RecentVolume_LastMonth = recentVolumeLastMonth;
            VelocityPerMinute_Bottom_Yes_Bid = velocityPerMinuteBottomYesBid;
            LevelCount_Bottom_Yes_Bid = levelCountBottomYesBid;
            VelocityPerMinute_Bottom_No_Bid = velocityPerMinuteBottomNoBid;
            LevelCount_Bottom_No_Bid = levelCountBottomNoBid;
            VelocityPerMinute_Top_Yes_Bid = velocityPerMinuteTopYesBid;
            LevelCount_Top_Yes_Bid = levelCountTopYesBid;
            VelocityPerMinute_Top_No_Bid = velocityPerMinuteTopNoBid;
            LevelCount_Top_No_Bid = levelCountTopNoBid;
            YesSpread = yesSpread;
            NoSpread = noSpread;
            DepthAtBestYesBid = depthAtBestYesBid;
            DepthAtBestNoBid = depthAtBestNoBid;
            TopTenPercentLevelDepth_Yes = cumulativeYesBidDepth;
            TopTenPercentLevelDepth_No = cumulativeNoBidDepth;
            BidRange_Yes = yesBidRange;
            BidRange_No = noBidRange;
            TotalBidContracts_Yes = totalYesBidContracts;
            TotalBidContracts_No = totalNoBidContracts;
            BidCountImbalance = bidCountImbalance;
            BidVolumeImbalance = bidVolumeImbalance;
            DepthAtTop4YesBids = depthAtTop4YesBids;
            DepthAtTop4NoBids = depthAtTop4NoBids;
            RSI_Short = rsiShort;
            RSI_Medium = rsiMedium;
            RSI_Long = rsiLong;
            MACD_Medium = macdMedium;
            MACD_Long = macdLong;
            EMA_Medium = emaMedium;
            EMA_Long = emaLong;
            BollingerBands_Medium = bollingerBandsMedium;
            BollingerBands_Long = bollingerBandsLong;
            ATR_Medium = atrMedium;
            ATR_Long = atrLong;
            VWAP_Short = vwapShort;
            VWAP_Medium = vwapMedium;
            StochasticOscillator_Short = stochasticOscillatorShort;
            StochasticOscillator_Medium = stochasticOscillatorMedium;
            StochasticOscillator_Long = stochasticOscillatorLong;
            OBV_Medium = obvMedium;
            OBV_Long = obvLong;
            ChangeMetricsMature = changeMetricsMature;
            MarketAge = marketAge;
            TimeLeft = timeLeft;
            CanCloseEarly = canCloseEarly;
            LastWebSocketMessageReceived = lastWebSocketMessageReceived;
            MarketBehaviorYes = marketBehaviorYes;
            MarketBehaviorNo = marketBehaviorNo;
            GoodBadPriceYes = goodBadPriceYes;
            GoodBadPriceNo = goodBadPriceNo;
            HoldTime = holdTime;
            YesBidCenterOfMass = yesBidCenterOfMass;
            NoBidCenterOfMass = noBidCenterOfMass;
            TolerancePercentage = tolerancePercentage;
            SnapshotSchemaVersion = snapshotSchemaVersion;
            TotalOrderbookDepth_Yes = totalOrderbookDepth_Yes;
            TotalOrderbookDepth_No = totalOrderbookDepth_No;
            TotalBidVolume_Yes = totalBidVolume_Yes;
            TotalBidVolume_No = totalBidVolume_No;
            YesBidSlopePerMinute_Short = yesBidSlopePerMinute_Short;
            NoBidSlopePerMinute_Short = noBidSlopePerMinute_Short;
            YesBidSlopePerMinute_Medium = yesBidSlopePerMinute_Medium;
            NoBidSlopePerMinute_Medium = noBidSlopePerMinute_Medium;
            ADX = adx;
            PSAR = psar;
        }

        public DateTime Timestamp { get; set; }

        public string MarketTicker { get; set; }
        public string MarketCategory { get; set; }

        public string MarketStatus { get; set; }
        public int SnapshotSchemaVersion { get; set; }

        #region Order Book Data
        /// <summary>
        /// Gets or sets the current order book data, containing active buy and sell orders.
        /// </summary>
        /// <remarks>
        /// Stores a list of dictionaries with order details (price, side, resting contracts, last modified date).
        /// Sourced from <see cref="MarketData.OrderbookData"/> and converted via <see cref="ConvertOrderbookData"/>.
        /// Updated via WebSocket events by <see cref="OrderBookService"/>. Null if no data is available.
        /// </remarks>
        [JsonPropertyName("Orderbook")]
        [JsonConverter(typeof(OrderbookSlimConverter))]
        public List<Dictionary<string, object>> OrderbookData { get; set; }
        #endregion

        #region Historical Prices

        public double YesBidSlopePerMinute_Short { get; set; }
        public double NoBidSlopePerMinute_Short { get; set; }
        public double YesBidSlopePerMinute_Medium { get; set; }
        public double NoBidSlopePerMinute_Medium { get; set; }

        /// <summary>
        /// Gets or sets the all-time high bid price for the "Yes" contract and its timestamp.
        /// </summary>
        /// <remarks>
        /// Highest bid price recorded, in cents (e.g., 72 = $0.72).
        /// Sourced from <see cref="MarketData.AllTimeHighYes_Bid"/>, calculated by <see cref="CandlestickService.PopulateMarketDataAsync"/> using minute candlesticks' <see cref="CandlestickData.BidHigh"/>.
        /// Default is (0, DateTime.MinValue) if no data. Skips initial candles with zero volume.
        /// </remarks>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public (int Bid, DateTime When) AllTimeHighYes_Bid { get; set; }

        /// <summary>
        /// Gets or sets the all-time low bid price for the "Yes" contract and its timestamp.
        /// </summary>
        /// <remarks>
        /// Lowest bid price recorded, in cents (e.g., 28 = $0.28).
        /// Sourced from <see cref="MarketData.AllTimeLowYes_Bid"/>, calculated by <see cref="CandlestickService.PopulateMarketDataAsync"/> using minute candlesticks' <see cref="CandlestickData.BidLow"/>.
        /// Default is (0, DateTime.MinValue) if no data. Skips initial candles with zero volume.
        /// </remarks>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public (int Bid, DateTime When) AllTimeLowYes_Bid { get; set; }

        /// <summary>
        /// Gets or sets the all-time high bid price for the "Yes" contract and its timestamp.
        /// </summary>
        /// <remarks>
        /// Highest bid price recorded, in cents (e.g., 72 = $0.72).
        /// Sourced from <see cref="MarketData.AllTimeHighNo_Bid"/>, calculated by <see cref="CandlestickService.PopulateMarketDataAsync"/> using minute candlesticks' <see cref="CandlestickData.BidHigh"/>.
        /// Default is (0, DateTime.MinValue) if no data. Skips initial candles with zero volume.
        /// </remarks>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public (int Bid, DateTime When) AllTimeHighNo_Bid { get; set; }

        /// <summary>
        /// Gets or sets the all-time low bid price for the "Yes" contract and its timestamp.
        /// </summary>
        /// <remarks>
        /// Lowest bid price recorded, in cents (e.g., 28 = $0.28).
        /// Sourced from <see cref="MarketData.AllTimeLowNo_Bid"/>, calculated by <see cref="CandlestickService.PopulateMarketDataAsync"/> using minute candlesticks' <see cref="CandlestickData.BidLow"/>.
        /// Default is (0, DateTime.MinValue) if no data. Skips initial candles with zero volume.
        /// </remarks>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public (int Bid, DateTime When) AllTimeLowNo_Bid { get; set; }

        /// <summary>
        /// Gets or sets the recent high bid price for the "Yes" contract (last 3 months) and its timestamp.
        /// </summary>
        /// <remarks>
        /// Highest bid price in the last 3 months, in cents (e.g., 68 = $0.68).
        /// Sourced from <see cref="MarketData.RecentHighYes_Bid"/>, calculated by <see cref="CandlestickService.PopulateMarketDataAsync"/> using minute candlesticks' <see cref="CandlestickData.BidHigh"/>.
        /// Default is (0, DateTime.MinValue) if no recent data.
        /// </remarks>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public (int Bid, DateTime When) RecentHighYes_Bid { get; set; }

        /// <summary>
        /// Gets or sets the recent low bid price for the "Yes" contract (last 3 months) and its timestamp.
        /// </summary>
        /// <remarks>
        /// Lowest bid price in the last 3 months, in cents (e.g., 32 = $0.32).
        /// Sourced from <see cref="MarketData.RecentLowYes_Bid"/>, calculated by <see cref="CandlestickService.PopulateMarketDataAsync"/> using minute candlesticks' <see cref="CandlestickData.BidLow"/>.
        /// Default is (0, DateTime.MinValue) if no recent data.
        /// </remarks>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public (int Bid, DateTime When) RecentLowYes_Bid { get; set; }

        /// <summary>
        /// Gets or sets the recent high bid price for the "Yes" contract (last 3 months) and its timestamp.
        /// </summary>
        /// <remarks>
        /// Highest bid price in the last 3 months, in cents (e.g., 68 = $0.68).
        /// Sourced from <see cref="MarketData.RecentHighNo_Bid"/>, calculated by <see cref="CandlestickService.PopulateMarketDataAsync"/> using minute candlesticks' <see cref="CandlestickData.BidHigh"/>.
        /// Default is (0, DateTime.MinValue) if no recent data.
        /// </remarks>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public (int Bid, DateTime When) RecentHighNo_Bid { get; set; }

        /// <summary>
        /// Gets or sets the recent low bid price for the "Yes" contract (last 3 months) and its timestamp.
        /// </summary>
        /// <remarks>
        /// Lowest bid price in the last 3 months, in cents (e.g., 32 = $0.32).
        /// Sourced from <see cref="MarketData.RecentLowNo_Bid"/>, calculated by <see cref="CandlestickService.PopulateMarketDataAsync"/> using minute candlesticks' <see cref="CandlestickData.BidLow"/>.
        /// Default is (0, DateTime.MinValue) if no recent data.
        /// </remarks>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public (int Bid, DateTime When) RecentLowNo_Bid { get; set; }
        #endregion

        #region Support and Resistance Levels
        /// <summary>
        /// Gets or sets the list of historical support and resistance levels for the market.
        /// </summary>
        /// <remarks>
        /// Contains key price levels (in cents) with strength, touches, and volume data.
        /// Sourced from <see cref="MarketData.AllSupportResistanceLevels"/>, calculated by <see cref="TradingCalculator.CalculateHistoricalSupportResistance"/> using minute candlesticks.
        /// Levels are filtered for statistical significance, excluding data from 07:00:00 to 11:59:00 UTC.
        /// </remarks>
        public List<SupportResistanceLevel> AllSupportResistanceLevels { get; set; }
        #endregion

        #region Position Metrics
        /// <summary>
        /// Gets or sets the current position size (number of contracts held).
        /// </summary>
        /// <remarks>
        /// Positive for "Yes" (long), negative for "No" (short) positions.
        /// Sourced from <see cref="MarketData.PositionSize"/>, calculated by <see cref="MarketData.RefreshPositionMetadata"/> from <see cref="MarketData.Positions"/>.
        /// 0 if no position exists. Side is indicated by <see cref="PositionSide"/>.
        /// </remarks>
        public int PositionSize { get; set; }

        /// <summary>
        /// Gets or sets the total market exposure of the current position in dollars.
        /// </summary>
        /// <remarks>
        /// Total cost of contracts held, in dollars (cents / 100).
        /// Sourced from <see cref="MarketData.MarketExposure"/>, calculated by <see cref="MarketData.RefreshPositionMetadata"/> from <see cref="MarketPosition.MarketExposure"/>.
        /// Absolute value, 0 if no position.
        /// </remarks>
        public double MarketExposure { get; set; }

        /// <summary>
        /// Gets or sets the average buy-in price per contract in dollars.
        /// </summary>
        /// <remarks>
        /// Average price paid per contract (MarketExposure / |PositionSize|), in dollars.
        /// Sourced from <see cref="MarketData.BuyinPrice"/>, calculated by <see cref="MarketData.RefreshPositionMetadata"/>.
        /// 0 if no position or PositionSize is 0.
        /// </remarks>
        public double BuyinPrice { get; set; }

        /// <summary>
        /// Gets or sets the potential upside of the current position in dollars.
        /// </summary>
        /// <remarks>
        /// Potential gain if held to resolution and outcome favors position, in dollars.
        /// Sourced from <see cref="MarketData.PositionUpside"/>, calculated by <see cref="MarketData.RefreshPositionMetadata"/> as max payout ($1/contract * |PositionSize|) minus liquidation value.
        /// Uses order book or current prices. 0 if no position or data unavailable.
        /// </remarks>
        public double PositionUpside { get; set; }

        /// <summary>
        /// Gets or sets the potential downside of the current position in dollars.
        /// </summary>
        /// <remarks>
        /// Potential loss if liquidated at current prices, as negative liquidation value in dollars.
        /// Sourced from <see cref="MarketData.PositionDownside"/>, calculated by <see cref="MarketData.RefreshPositionMetadata"/> using order book or current prices.
        /// Typically negative, 0 if no position or data unavailable.
        /// </remarks>
        public double PositionDownside { get; set; }

        /// <summary>
        /// Gets or sets the total number of contracts traded for the current position.
        /// </summary>
        /// <remarks>
        /// Cumulative contracts traded (bought/sold) for the position.
        /// Sourced from <see cref="MarketData.TotalPositionTraded"/>, retrieved by <see cref="MarketData.RefreshPositionMetadata"/> from <see cref="MarketPosition.TotalTraded"/>.
        /// 0 if no position. Non-negative integer.
        /// </remarks>
        public long TotalTraded { get; set; }

        /// <summary>
        /// Gets or sets the number of resting orders for the market.
        /// </summary>
        /// <remarks>
        /// Count of open orders (e.g., limit orders) waiting to be filled.
        /// Sourced from <see cref="MarketData.RestingOrders"/>, retrieved by <see cref="MarketData.RefreshPositionMetadata"/> from <see cref="MarketPosition.RestingOrdersCount"/>.
        /// 0 if no orders or position. Non-negative integer.
        /// </remarks>
        public List<(string action, string side, string type, int count, int price, DateTime? expiration)> RestingOrders { get; set; }

        /// <summary>
        /// Gets or sets the realized profit and loss (PnL) for the position in dollars.
        /// </summary>
        /// <remarks>
        /// Net profit/loss from closed trades, in dollars (cents / 100).
        /// Sourced from <see cref="MarketData.RealizedPnl"/>, retrieved by <see cref="MarketData.RefreshPositionMetadata"/> from <see cref="MarketPosition.RealizedPnl"/>.
        /// Positive for profit, negative for loss, 0 if no closed trades.
        /// </remarks>
        public double RealizedPnl { get; set; }

        /// <summary>
        /// Gets or sets the total fees paid for trades in dollars.
        /// </summary>
        /// <remarks>
        /// Cumulative trading fees for all trades, in dollars (cents / 100).
        /// Sourced from <see cref="MarketData.FeesPaid"/>, retrieved by <see cref="MarketData.RefreshPositionMetadata"/> from <see cref="MarketPosition.FeesPaid"/>.
        /// Non-negative, 0 if no trades.
        /// </remarks>
        public double FeesPaid { get; set; }

        /// <summary>
        /// Gets or sets the return on investment (ROI) for the position as a percentage.
        /// </summary>
        /// <remarks>
        /// Percentage return: (liquidation price - BuyinPrice) / BuyinPrice * 100.
        /// Sourced from <see cref="MarketData.PositionROI"/>, calculated by <see cref="MarketData.RefreshPositionMetadata"/> using order book or current prices.
        /// 0 if no position, BuyinPrice is 0, or data unavailable. Positive for profit, negative for loss.
        /// </remarks>
        public double PositionROI { get; set; }

        /// <summary>
        /// Gets or sets the return on investment (ROI) for the position as a dollar amount.
        /// </summary>
        /// <remarks>
        /// Sourced from <see cref="MarketData.PositionROIAmount"/>, calculated by <see cref="MarketData.RefreshPositionMetadata"/> using order book or current prices.
        /// </remarks>
        public double PositionROIAmt { get; set; }

        /// <summary>
        /// Gets the expected trading fees for selling the current position in dollars.
        /// </summary>
        /// <remarks>
        /// Calculated using the formula: fees = roundup(0.07 * C * P * (1-P)), where C is the number of contracts (absolute position size),
        /// P is the liquidation price in dollars, and roundup rounds to the next cent. Fees are summed across price levels in the order book
        /// when available, or based on the current bid (for "yes" positions) or ask (for "no" positions) price if no orders exist.
        /// Only applied for takers, not makers.
        /// Sourced from <see cref="MarketData.RefreshPositionMetadata"/>. Returns 0 if no position exists, position size is 0, or data is unavailable.
        /// </remarks>
        public double ExpectedFees { get; private set; }
        #endregion

        #region Trade Metrics

        public double? ADX { get; set; }
        public double? PSAR { get; set; }


        /// <summary>
        /// Gets or sets the trade rate for "Yes" side trades, in trades per minute.
        /// </summary>
        /// <remarks>
        /// Rate of "Yes" side trades (taker side = "no") within the change window.
        /// Sourced from <see cref="MarketData.TradeRatePerMinute_Yes"/>, computed by <see cref="OrderbookChangeTracker.RefreshTradeChangeOverTimeMetrics"/> using <see cref="OrderbookChangeTracker.GetTradeRatePerMinute_MakerYes"/>.
        /// Number of trades divided by elapsed minutes (capped at <see cref="OrderbookChangeTracker.ChangeWindowDuration"/>). 0 if no trades.
        /// </remarks>
        public double TradeRatePerMinute_Yes { get; set; }

        /// <summary>
        /// Gets or sets the trade rate for "No" side trades, in trades per minute.
        /// </summary>
        /// <remarks>
        /// Rate of "No" side trades (taker side = "yes") within the change window.
        /// Sourced from <see cref="MarketData.TradeRatePerMinute_No"/>, computed by <see cref="OrderbookChangeTracker.RefreshTradeChangeOverTimeMetrics"/> using <see cref="OrderbookChangeTracker.GetTradeRatePerMinute_MakerNo"/>.
        /// Number of trades divided by elapsed minutes (capped at <see cref="OrderbookChangeTracker.ChangeWindowDuration"/>). 0 if no trades.
        /// </remarks>
        public double TradeRatePerMinute_No { get; set; }

        /// <summary>
        /// Gets or sets the trade volume rate for "Yes" side trades, in dollars per minute.
        /// </summary>
        /// <remarks>
        /// Rate of "Yes" side trade volume within the change window.
        /// Sourced from <see cref="MarketData.TradeVolumePerMinute_Yes"/>, computed by <see cref="OrderbookChangeTracker.RefreshTradeChangeOverTimeMetrics"/> using <see cref="OrderbookChangeTracker.GetTradeRatePerMinute_MakerYes"/>.
        /// Sums dollar value (price/100 * |delta contracts|) of trade-related changes, divided by elapsed minutes. 0 if no trades.
        /// </remarks>
        public double TradeVolumePerMinute_Yes { get; set; }

        /// <summary>
        /// Gets or sets the trade volume rate for "No" side trades, in dollars per minute.
        /// </summary>
        /// <remarks>
        /// Rate of "No" side trade volume within the change window.
        /// Sourced from <see cref="MarketData.TradeVolumePerMinute_No"/>, computed by <see cref="OrderbookChangeTracker.RefreshTradeChangeOverTimeMetrics"/> using <see cref="OrderbookChangeTracker.GetTradeRatePerMinute_MakerNo"/>.
        /// Sums dollar value (price/100 * |delta contracts|) of trade-related changes, divided by elapsed minutes. 0 if no trades.
        /// </remarks>
        public double TradeVolumePerMinute_No { get; set; }

        /// <summary>
        /// Gets or sets the average dollar size of "Yes" side trades.
        /// </summary>
        /// <remarks>
        /// Average dollar value per "Yes" side trade (taker side = "no").
        /// Sourced from <see cref="MarketData.AverageTradeSize_Yes"/>, computed by <see cref="OrderbookChangeTracker.GetAverageTradeSize_MakerYes"/>.
        /// Sums dollar value (price/100 * |delta contracts|) divided by trade count. 0 if no trades. Trade count in <see cref="TradeCount_Yes"/>.
        /// </remarks>
        public double AverageTradeSize_Yes { get; set; }

        /// <summary>
        /// Gets or sets the average dollar size of "No" side trades.
        /// </summary>
        /// <remarks>
        /// Average dollar value per "No" side trade (taker side = "yes").
        /// Sourced from <see cref="MarketData.AverageTradeSize_No"/>, computed by <see cref="OrderbookChangeTracker.GetAverageTradeSize_MakerNo"/>.
        /// Sums dollar value (price/100 * |delta contracts|) divided by trade count. 0 if no trades. Trade count in <see cref="TradeCount_No"/>.
        /// </remarks>
        public double AverageTradeSize_No { get; set; }

        /// <summary>
        /// Gets or sets the total number of "Yes" side trades.
        /// </summary>
        /// <remarks>
        /// Count of trades with "Yes" as the maker side (taker side = "no").
        /// Sourced from <see cref="MarketData.TradeCount_Yes"/>, computed by <see cref="OrderbookChangeTracker.GetTradeCount_MakerYes"/>.
        /// Tracks trades in <see cref="OrderbookChangeTracker.TradeEvent"/>. 0 if no trades.
        /// </remarks>
        public int TradeCount_Yes { get; set; }

        /// <summary>
        /// Gets or sets the total number of "No" side trades.
        /// </summary>
        /// <remarks>
        /// Count of trades with "No" as the maker side (taker side = "yes").
        /// Sourced from <see cref="MarketData.TradeCount_No"/>, computed by <see cref="OrderbookChangeTracker.GetTradeCount_MakerNo"/>.
        /// Tracks trades in <see cref="OrderbookChangeTracker.TradeEvent"/>. 0 if no trades.
        /// </remarks>
        public int TradeCount_No { get; set; }

        /// <summary>
        /// Gets or sets the count of non-trade-related "Yes" side order book changes.
        /// </summary>
        /// <remarks>
        /// Number of "Yes" side order book changes not linked to trades.
        /// Sourced from <see cref="MarketData.NonTradeRelatedOrderCount_Yes"/>, computed by <see cref="OrderbookChangeTracker.RefreshTradeChangeOverTimeMetrics"/> using <see cref="OrderbookChangeTracker.GetYesNetOrderRatePerMinute"/>.
        /// Tracks changes in <see cref="OrderbookChangeTracker.OrderbookChange"/> with <see cref="OrderbookChangeTracker.OrderbookChange.IsTradeRelated"/> set to false.
        /// </remarks>
        public int NonTradeRelatedOrderCount_Yes { get; set; }

        /// <summary>
        /// Gets or sets the count of non-trade-related "No" side order book changes.
        /// </summary>
        /// <remarks>
        /// Number of "No" side order book changes not linked to trades.
        /// Sourced from <see cref="MarketData.NonTradeRelatedOrderCount_No"/>, computed by <see cref="OrderbookChangeTracker.RefreshTradeChangeOverTimeMetrics"/> using <see cref="OrderbookChangeTracker.GetNoNetOrderRatePerMinute"/>.
        /// Tracks changes in <see cref="OrderbookChangeTracker.OrderbookChange"/> with <see cref="OrderbookChangeTracker.OrderbookChange.IsTradeRelated"/> set to false.
        /// </remarks>
        public int NonTradeRelatedOrderCount_No { get; set; }
        #endregion

        #region Volume Metrics
        /// <summary>
        /// Gets or sets the highest daily trading volume for the market.
        /// </summary>
        /// <remarks>
        /// Maximum volume traded in a single day, in contracts.
        /// Sourced from <see cref="MarketData.HighestVolume_Day"/>, calculated by <see cref="CandlestickService.PopulateMarketDataAsync"/> using day candlesticks' <see cref="CandlestickData.Volume"/>.
        /// 0 if no day candlesticks or volume data.
        /// </remarks>
        public double HighestVolume_Day { get; set; }

        /// <summary>
        /// Gets or sets the highest hourly trading volume for the market.
        /// </summary>
        /// <remarks>
        /// Maximum volume traded in a single hour, in contracts.
        /// Sourced from <see cref="MarketData.HighestVolume_Hour"/>, calculated by <see cref="CandlestickService.PopulateMarketDataAsync"/> using hour candlesticks' <see cref="CandlestickData.Volume"/>.
        /// 0 if no hour candlesticks or volume data.
        /// </remarks>
        public double HighestVolume_Hour { get; set; }

        /// <summary>
        /// Gets or sets the highest minute trading volume for the market.
        /// </summary>
        /// <remarks>
        /// Maximum volume traded in a single minute, in contracts.
        /// Sourced from <see cref="MarketData.HighestVolume_Minute"/>, calculated by <see cref="CandlestickService.PopulateMarketDataAsync"/> using minute candlesticks' <see cref="CandlestickData.Volume"/>.
        /// 0 if no minute candlesticks or volume data.
        /// </remarks>
        public double HighestVolume_Minute { get; set; }

        /// <summary>
        /// Gets or sets the total trading volume in the last hour.
        /// </summary>
        /// <remarks>
        /// Sum of volumes from minute candlesticks in the last hour, in contracts.
        /// Sourced from <see cref="MarketData.RecentVolume_LastHour"/>, calculated by <see cref="CandlestickService.PopulateMarketDataAsync"/> using minute candlesticks' <see cref="CandlestickData.Volume"/>.
        /// 0 if no recent data or volume.
        /// </remarks>
        public double RecentVolume_LastHour { get; set; }

        /// <summary>
        /// Gets or sets the total trading volume in the last three hours.
        /// </summary>
        /// <remarks>
        /// Sum of volumes from minute candlesticks in the last three hours, in contracts.
        /// Sourced from <see cref="MarketData.RecentVolume_LastThreeHours"/>, calculated by <see cref="CandlestickService.PopulateMarketDataAsync"/> using minute candlesticks' <see cref="CandlestickData.Volume"/>.
        /// 0 if no recent data or volume.
        /// </remarks>
        public double RecentVolume_LastThreeHours { get; set; }

        /// <summary>
        /// Gets or sets the total trading volume in the last month.
        /// </summary>
        /// <remarks>
        /// Sum of volumes from minute candlesticks in the last month, in contracts.
        /// Sourced from <see cref="MarketData.RecentVolume_LastMonth"/>, calculated by <see cref="CandlestickService.PopulateMarketDataAsync"/> using minute candlesticks' <see cref="CandlestickData.Volume"/>.
        /// 0 if no recent data or volume.
        /// </remarks>
        public double RecentVolume_LastMonth { get; set; }
        #endregion

        #region Order Book Velocities

        /// <summary>
        /// Gets or sets the velocity of order book changes for bottom "Yes" bid prices, in dollars per minute.
        /// </summary>
        /// <remarks>
        /// Rate of change for "Yes" bid prices below 90% of <see cref="MarketData.BestYesBid"/>.
        /// Sourced from <see cref="MarketData.VelocityPerMinute_Bottom_Yes_Bid"/>, computed by <see cref="OrderbookChangeTracker.GetBottomYesVelocityPerMinute"/>.
        /// Sums dollar value (price/100 * delta contracts) of changes, divided by elapsed minutes. 0 if no changes. Levels in <see cref="LevelCount_Bottom_Yes_Bid"/>.
        /// </remarks>
        public double VelocityPerMinute_Bottom_Yes_Bid { get; set; }

        /// <summary>
        /// Gets or sets the number of price levels for bottom "Yes" bid velocity.
        /// </summary>
        /// <remarks>
        /// Count of "Yes" bid price levels below 90% of <see cref="MarketData.BestYesBid"/> used in <see cref="VelocityPerMinute_Bottom_Yes_Bid"/>.
        /// Sourced from <see cref="MarketData.LevelCount_Bottom_Yes_Bid"/>, computed by <see cref="OrderbookChangeTracker.GetBottomYesVelocityPerMinute"/>.
        /// 0 if no relevant orders.
        /// </remarks>
        public int LevelCount_Bottom_Yes_Bid { get; set; }

        /// <summary>
        /// Gets or sets the velocity of order book changes for bottom "No" bid prices, in dollars per minute.
        /// </summary>
        /// <remarks>
        /// Rate of change for "No" bid prices below 90% of <see cref="MarketData.BestNoBid"/>.
        /// Sourced from <see cref="MarketData.VelocityPerMinute_Bottom_No_Bid"/>, computed by <see cref="OrderbookChangeTracker.GetBottomNoVelocityPerMinute"/>.
        /// Sums dollar value (price/100 * delta contracts) of changes, divided by elapsed minutes. 0 if no changes. Levels in <see cref="LevelCount_Bottom_No_Bid"/>.
        /// </remarks>
        public double VelocityPerMinute_Bottom_No_Bid { get; set; }

        /// <summary>
        /// Gets or sets the number of price levels for bottom "No" bid velocity.
        /// </summary>
        /// <remarks>
        /// Count of "No" bid price levels below 90% of <see cref="MarketData.BestNoBid"/> used in <see cref="VelocityPerMinute_Bottom_No_Bid"/>.
        /// Sourced from <see cref="MarketData.LevelCount_Bottom_No_Bid"/>, computed by <see cref="OrderbookChangeTracker.GetBottomNoVelocityPerMinute"/>.
        /// 0 if no relevant orders.
        /// </remarks>
        public int LevelCount_Bottom_No_Bid { get; set; }

        /// <summary>
        /// Gets or sets the velocity of order book changes for top "Yes" bid prices, in dollars per minute.
        /// </summary>
        /// <remarks>
        /// Rate of change for "Yes" bid prices at or above 90% of <see cref="MarketData.BestYesBid"/>.
        /// Sourced from <see cref="MarketData.VelocityPerMinute_Top_Yes_Bid"/>, computed by <see cref="OrderbookChangeTracker.GetTopYesVelocityPerMinute"/>.
        /// Sums dollar value (price/100 * delta contracts) of changes, divided by elapsed minutes. 0 if no changes. Levels in <see cref="LevelCount_Top_Yes_Bid"/>.
        /// </remarks>
        public double VelocityPerMinute_Top_Yes_Bid { get; set; }

        /// <summary>
        /// Gets or sets the number of price levels for top "Yes" bid velocity.
        /// </summary>
        /// <remarks>
        /// Count of "Yes" bid price levels at or above 90% of <see cref="MarketData.BestYesBid"/> used in <see cref="VelocityPerMinute_Top_Yes_Bid"/>.
        /// Sourced from <see cref="MarketData.LevelCount_Top_Yes_Bid"/>, computed by <see cref="OrderbookChangeTracker.GetTopYesVelocityPerMinute"/>.
        /// 0 if no relevant orders.
        /// </remarks>
        public int LevelCount_Top_Yes_Bid { get; set; }

        /// <summary>
        /// Gets or sets the velocity of order book changes for top "No" bid prices, in dollars per minute.
        /// </summary>
        /// <remarks>
        /// Rate of change for "No" bid prices at or above 90% of <see cref="MarketData.BestNoBid"/>.
        /// Sourced from <see cref="MarketData.VelocityPerMinute_Top_No_Bid"/>, computed by <see cref="OrderbookChangeTracker.GetTopNoVelocityPerMinute"/>.
        /// Sums dollar value (price/100 * delta contracts) of changes, divided by elapsed minutes. 0 if no changes. Levels in <see cref="LevelCount_Top_No_Bid"/>.
        /// </remarks>
        public double VelocityPerMinute_Top_No_Bid { get; set; }

        /// <summary>
        /// Gets or sets the number of price levels for top "No" bid velocity.
        /// </summary>
        /// <remarks>
        /// Count of "No" bid price levels at or above 90% of <see cref="MarketData.BestNoBid"/> used in <see cref="VelocityPerMinute_Top_No_Bid"/>.
        /// Sourced from <see cref="MarketData.LevelCount_Top_No_Bid"/>, computed by <see cref="OrderbookChangeTracker.GetTopNoVelocityPerMinute"/>.
        /// 0 if no relevant orders.
        /// </remarks>
        public int LevelCount_Top_No_Bid { get; set; }

        #endregion

        #region Order Book Depth

        /// <summary>
        /// Gets or sets the total orderbook depth.
        /// </summary>
        /// <remarks>
        /// Total orderbook depth, in cents
        /// Sourced from <see cref="MarketData.OrderbookData"/>.
        /// 0 if no "Yes" bid orders.
        /// </remarks>
        public long TotalOrderbookDepth_Yes { get; set; }

        /// <summary>
        /// Gets or sets the total orderbook depth.
        /// </summary>
        /// <remarks>
        /// Total orderbook depth, in cents
        /// Sourced from <see cref="MarketData.OrderbookData"/>.
        /// 0 if no "Yes" bid orders.
        /// </remarks>
        public long TotalOrderbookDepth_No { get; set; }

        /// <summary>
        /// Gets or sets the best "Yes" bid price in the order book.
        /// </summary>
        /// <remarks>
        /// Highest price offered to buy "Yes" contracts, in cents.
        /// Sourced from <see cref="MarketData.BestYesBid"/>, computed from <see cref="MarketData.OrderbookData"/> using <see cref="MarketData.GetBids"/>.
        /// 0 if no "Yes" bid orders.
        /// </remarks>
        public int BestYesBid { get; set; }

        /// <summary>
        /// Gets or sets the best "No" bid price in the order book.
        /// </summary>
        /// <remarks>
        /// Highest price offered to buy "No" contracts, in cents.
        /// Sourced from <see cref="MarketData.BestNoBid"/>, computed from <see cref="MarketData.OrderbookData"/> using <see cref="MarketData.GetBids"/>.
        /// 0 if no "No" bid orders.
        /// </remarks>
        public int BestNoBid { get; set; }

        /// <summary>
        /// Gets or sets the best "Yes" ask price in the order book.
        /// </summary>
        /// <remarks>
        /// Lowest price offered to sell "Yes" contracts (equals best "No" bid), in cents.
        /// Sourced from <see cref="MarketData.BestYesAsk"/>, computed from <see cref="MarketData.OrderbookData"/>.
        /// 0 if no "No" bid orders.
        /// </remarks>
        public int BestYesAsk { get { return (100 - BestNoBid); } }

        /// <summary>
        /// Gets or sets the best "No" ask price in the order book.
        /// </summary>
        /// <remarks>
        /// Lowest price offered to sell "No" contracts (equals best "Yes" bid), in cents.
        /// Sourced from <see cref="MarketData.BestNoAsk"/>, computed from <see cref="MarketData.OrderbookData"/>.
        /// 0 if no "Yes" bid orders.
        /// </remarks>
        public int BestNoAsk { get { return (100 - BestYesBid); } }

        /// <summary>
        /// Gets or sets the spread between "Yes" ask and bid prices.
        /// </summary>
        /// <remarks>
        /// Difference between <see cref="BestYesAsk"/> and <see cref="BestYesBid"/>, in cents.
        /// Sourced from <see cref="MarketData.YesSpread"/>. 0 if no orders.
        /// </remarks>
        public int YesSpread { get; set; }

        /// <summary>
        /// Gets or sets the spread between "No" ask and bid prices.
        /// </summary>
        /// <remarks>
        /// Difference between <see cref="BestNoAsk"/> and <see cref="BestNoBid"/>, in cents.
        /// Sourced from <see cref="MarketData.NoSpread"/>. 0 if no orders.
        /// </remarks>
        public int NoSpread { get; set; }

        /// <summary>
        /// Gets or sets the number of resting contracts at the best "Yes" bid price.
        /// </summary>
        /// <remarks>
        /// Total contracts at <see cref="BestYesBid"/>.
        /// Sourced from <see cref="MarketData.DepthAtBestYesBid"/>, computed from <see cref="MarketData.OrderbookData"/> using <see cref="MarketData.GetBids"/>.
        /// 0 if no "Yes" bid orders.
        /// </remarks>
        public int DepthAtBestYesBid { get; set; }

        /// <summary>
        /// Gets or sets the number of resting contracts at the best "No" bid price.
        /// </summary>
        /// <remarks>
        /// Total contracts at <see cref="BestNoBid"/>.
        /// Sourced from <see cref="MarketData.DepthAtBestNoBid"/>, computed from <see cref="MarketData.OrderbookData"/> using <see cref="MarketData.GetBids"/>.
        /// 0 if no "No" bid orders.
        /// </remarks>
        public int DepthAtBestNoBid { get; set; }


        /// <summary>
        /// Gets or sets the cumulative resting contracts for "Yes" bid orders within tolerance.
        /// </summary>
        /// <remarks>
        /// Total contracts for "Yes" bids within <see cref="TolerancePercentage"/> below <see cref="BestYesBid"/>.
        /// Sourced from <see cref="MarketData.TopTenPercentLevelDepth_Yes"/>, computed by <see cref="MarketData.CalculateCumulativeDepth"/>.
        /// 0 if no orders within range.
        /// </remarks>
        public int TopTenPercentLevelDepth_Yes { get; set; }

        /// <summary>
        /// Gets or sets the cumulative resting contracts for "No" bid orders within tolerance.
        /// </summary>
        /// <remarks>
        /// Total contracts for "No" bids within <see cref="TolerancePercentage"/> below <see cref="BestNoBid"/>.
        /// Sourced from <see cref="MarketData.TopTenPercentLevelDepth_No"/>, computed by <see cref="MarketData.CalculateCumulativeDepth"/>.
        /// 0 if no orders within range.
        /// </remarks>
        public int TopTenPercentLevelDepth_No { get; set; }

        /// <summary>
        /// Gets or sets the price range of "Yes" bid orders.
        /// </summary>
        /// <remarks>
        /// Difference between highest and lowest "Yes" bid prices, in cents.
        /// Sourced from <see cref="MarketData.BidRange_Yes"/>, computed from <see cref="MarketData.OrderbookData"/> using <see cref="MarketData.GetBids"/>.
        /// 0 if no "Yes" bid orders.
        /// </remarks>
        public int BidRange_Yes { get; set; }

        /// <summary>
        /// Gets or sets the price range of "No" bid orders.
        /// </summary>
        /// <remarks>
        /// Difference between highest and lowest "No" bid prices, in cents.
        /// Sourced from <see cref="MarketData.BidRange_No"/>, computed from <see cref="MarketData.OrderbookData"/> using <see cref="MarketData.GetBids"/>.
        /// 0 if no "No" bid orders.
        /// </remarks>
        public int BidRange_No { get; set; }

        /// <summary>
        /// Gets or sets the total resting contracts for "Yes" bid orders.
        /// </summary>
        /// <remarks>
        /// Total contracts available for "Yes" bids.
        /// Sourced from <see cref="MarketData.TotalBidContracts_Yes"/>, computed from <see cref="MarketData.OrderbookData"/> using <see cref="MarketData.GetBids"/>.
        /// 0 if no "Yes" bid orders.
        /// </remarks>
        public int TotalBidContracts_Yes { get; set; }

        /// <summary>
        /// Gets or sets the total resting contracts for "No" bid orders.
        /// </summary>
        /// <remarks>
        /// Total contracts available for "No" bids.
        /// Sourced from <see cref="MarketData.TotalBidContracts_No"/>, computed from <see cref="MarketData.OrderbookData"/> using <see cref="MarketData.GetBids"/>.
        /// 0 if no "No" bid orders.
        /// </remarks>
        public int TotalBidContracts_No { get; set; }

        /// <summary>
        /// Gets or sets the imbalance between "Yes" and "No" bid contracts.
        /// </summary>
        /// <remarks>
        /// Difference: <see cref="TotalBidContracts_Yes"/> - <see cref="TotalBidContracts_No"/>.
        /// Sourced from <see cref="MarketData.BidImbalance"/>. Positive for "Yes" dominance, negative for "No".
        /// 0 if no bid orders.
        /// </remarks>
        public int BidCountImbalance { get; set; }

        /// <summary>
        /// Gets or sets the imbalance between "Yes" and "No" bid contracts * price.
        /// </summary>
        /// <remarks>
        /// Difference: <see cref="TotalBidContracts_Yes"/> - <see cref="TotalBidContracts_No"/>.
        /// Sourced from <see cref="MarketData.BidImbalance"/>. Positive for "Yes" dominance, negative for "No".
        /// 0 if no bid orders.
        /// </remarks>
        public double BidVolumeImbalance { get; set; }

        /// <summary>
        /// Gets or sets the total resting contracts for top four "Yes" bid price levels.
        /// </summary>
        /// <remarks>
        /// Sum of contracts at four highest "Yes" bid prices.
        /// Sourced from <see cref="MarketData.DepthAtTop4YesBids"/>, computed from <see cref="MarketData.OrderbookData"/> using <see cref="MarketData.GetBids"/>.
        /// Sums all levels if fewer than four. 0 if no "Yes" bid orders.
        /// </remarks>
        public int DepthAtTop4YesBids { get; set; }

        /// <summary>
        /// Gets or sets the total resting contracts for top four "No" bid price levels.
        /// </summary>
        /// <remarks>
        /// Sum of contracts at four highest "No" bid prices.
        /// Sourced from <see cref="MarketData.DepthAtTop4NoBids"/>, computed from <see cref="MarketData.OrderbookData"/> using <see cref="MarketData.GetBids"/>.
        /// Sums all levels if fewer than four. 0 if no "No" bid orders.
        /// </remarks>
        public int DepthAtTop4NoBids { get; set; }



        public double TotalBidVolume_Yes { get; set; }
        public double TotalBidVolume_No { get; set; }
        #endregion

        #region Technical Indicators
        /// <summary>
        /// Gets or sets the short-term RSI (14 periods, minute candlesticks).
        /// </summary>
        /// <remarks>
        /// Momentum indicator (0-100) for "Yes" side, using minute candlesticks.
        /// Sourced from <see cref="MarketData.RSI_Short"/>, computed by <see cref="TradingCalculator.CalculateRSI"/>.
        /// Null if < 15 candlesticks or invalid. >70 overbought, <30 oversold.
        /// </remarks>
        public double? RSI_Short { get; set; }

        /// <summary>
        /// Gets or sets the medium-term RSI (14 periods, hour candlesticks).
        /// </summary>
        /// <remarks>
        /// Momentum indicator (0-100) for "Yes" side, using hour candlesticks.
        /// Sourced from <see cref="MarketData.RSI_Medium"/>, computed by <see cref="TradingCalculator.CalculateRSI"/>.
        /// Null if < 15 candlesticks or invalid. >70 overbought, <30 oversold.
        /// </remarks>
        public double? RSI_Medium { get; set; }

        /// <summary>
        /// Gets or sets the long-term RSI (14 periods, day candlesticks).
        /// </summary>
        /// <remarks>
        /// Momentum indicator (0-100) for "Yes" side, using day candlesticks.
        /// Sourced from <see cref="MarketData.RSI_Long"/>, computed by <see cref="TradingCalculator.CalculateRSI"/>.
        /// Null if < 15 candlesticks or invalid. >70 overbought, <30 oversold.
        /// </remarks>
        public double? RSI_Long { get; set; }

        /// <summary>
        /// Gets or sets the medium-term MACD (12, 26, 9 periods, hour candlesticks).
        /// </summary>
        /// <remarks>
        /// MACD line, signal line, and histogram for trend and momentum.
        /// Sourced from <see cref="MarketData.MACD_Medium"/>, computed by <see cref="TradingCalculator.CalculateMACD"/>.
        /// Null if < 35 candlesticks or invalid. Uses <see cref="MACDConverter"/> for JSON.
        /// </remarks>
        [JsonConverter(typeof(MACDConverter))]
        public (double? MACD, double? Signal, double? Histogram) MACD_Medium { get; set; }

        /// <summary>
        /// Gets or sets the long-term MACD (12, 26, 9 periods, day candlesticks).
        /// </summary>
        /// <remarks>
        /// MACD line, signal line, and histogram for trend and momentum.
        /// Sourced from <see cref="MarketData.MACD_Long"/>, computed by <see cref="TradingCalculator.CalculateMACD"/>.
        /// Null if < 35 candlesticks or invalid. Uses <see cref="MACDConverter"/> for JSON.
        /// </remarks>
        [JsonConverter(typeof(MACDConverter))]
        public (double? MACD, double? Signal, double? Histogram) MACD_Long { get; set; }

        /// <summary>
        /// Gets or sets the medium-term EMA (14 periods, hour candlesticks).
        /// </summary>
        /// <remarks>
        /// Weighted moving average for "Yes" side, in cents.
        /// Sourced from <see cref="MarketData.EMA_Medium"/>, computed by <see cref="TradingCalculator.CalculateEMA"/>.
        /// Null if < 14 candlesticks or invalid.
        /// </remarks>
        public double? EMA_Medium { get; set; }

        /// <summary>
        /// Gets or sets the long-term EMA (14 periods, day candlesticks).
        /// </summary>
        /// <remarks>
        /// Weighted moving average for "Yes" side, in cents.
        /// Sourced from <see cref="MarketData.EMA_Long"/>, computed by <see cref="TradingCalculator.CalculateEMA"/>.
        /// Null if < 14 candlesticks or invalid.
        /// </remarks>
        public double? EMA_Long { get; set; }

        /// <summary>
        /// Gets or sets the medium-term Bollinger Bands (20 periods, hour candlesticks, 2 std dev).
        /// </summary>
        /// <remarks>
        /// SMA (Middle), Upper, and Lower bands for volatility.
        /// Sourced from <see cref="MarketData.BollingerBands_Medium"/>, computed by <see cref="TradingCalculator.CalculateBollingerBands"/>.
        /// Upper/Lower null if < 20 candlesticks or invalid, Middle may be valid. In cents. Uses <see cref="BollingerBandsConverter"/> for JSON.
        /// </remarks>
        [JsonConverter(typeof(BollingerBandsConverter))]
        public (double? Lower, double? Middle, double? Upper) BollingerBands_Medium { get; set; }

        /// <summary>
        /// Gets or sets the long-term Bollinger Bands (20 periods, day candlesticks, 2 std dev).
        /// </summary>
        /// <remarks>
        /// SMA (Middle), Upper, and Lower bands for volatility.
        /// Sourced from <see cref="MarketData.BollingerBands_Long"/>, computed by <see cref="TradingCalculator.CalculateBollingerBands"/>.
        /// Upper/Lower null if < 20 candlesticks or invalid, Middle may be valid. In cents. Uses <see cref="BollingerBandsConverter"/> for JSON.
        /// </remarks>
        [JsonConverter(typeof(BollingerBandsConverter))]
        public (double? Lower, double? Middle, double? Upper) BollingerBands_Long { get; set; }

        /// <summary>
        /// Gets or sets the medium-term ATR (14 periods, hour candlesticks).
        /// </summary>
        /// <remarks>
        /// Average price range for volatility, in cents.
        /// Sourced from <see cref="MarketData.ATR_Medium"/>, computed by <see cref="TradingCalculator.CalculateATR"/>.
        /// Null if < 15 candlesticks or invalid.
        /// </remarks>
        public double? ATR_Medium { get; set; }

        /// <summary>
        /// Gets or sets the long-term ATR (14 periods, day candlesticks).
        /// </summary>
        /// <remarks>
        /// Average price range for volatility, in cents.
        /// Sourced from <see cref="MarketData.ATR_Long"/>, computed by <see cref="TradingCalculator.CalculateATR"/>.
        /// Null if < 15 candlesticks or invalid.
        /// </remarks>
        public double? ATR_Long { get; set; }

        /// <summary>
        /// Gets or sets the short-term VWAP (15 periods, minute candlesticks).
        /// </summary>
        /// <remarks>
        /// Volume-weighted average price, in cents.
        /// Sourced from <see cref="MarketData.VWAP_Short"/>, computed by <see cref="TradingCalculator.CalculateVWAP"/>.
        /// Null if insufficient data, zero volume, or invalid.
        /// </remarks>
        public double? VWAP_Short { get; set; }

        /// <summary>
        /// Gets or sets the medium-term VWAP (15 periods, hour candlesticks).
        /// </summary>
        /// <remarks>
        /// Volume-weighted average price, in cents.
        /// Sourced from <see cref="MarketData.VWAP_Medium"/>, computed by <see cref="TradingCalculator.CalculateVWAP"/>.
        /// Null if insufficient data, zero volume, or invalid.
        /// </remarks>
        public double? VWAP_Medium { get; set; }

        /// <summary>
        /// Gets or sets the short-term Stochastic Oscillator (K=14, D=3, minute candlesticks).
        /// </summary>
        /// <remarks>
        /// Momentum indicator (%K, %D, 0-100) for price range.
        /// Sourced from <see cref="MarketData.StochasticOscillator_Short"/>, computed by <see cref="TradingCalculator.CalculateStochastic"/>.
        /// %D null if < 14 candlesticks or invalid; %K computed if possible. Uses <see cref="StochasticOscillatorConverter"/> for JSON.
        /// </remarks>
        [JsonConverter(typeof(StochasticOscillatorConverter))]
        public (double? K, double? D) StochasticOscillator_Short { get; set; }

        /// <summary>
        /// Gets or sets the medium-term Stochastic Oscillator (K=14, D=3, hour candlesticks).
        /// </summary>
        /// <remarks>
        /// Momentum indicator (%K, %D, 0-100) for price range.
        /// Sourced from <see cref="MarketData.StochasticOscillator_Medium"/>, computed by <see cref="TradingCalculator.CalculateStochastic"/>.
        /// %D null if < 14 candlesticks or invalid; %K computed if possible. Uses <see cref="StochasticOscillatorConverter"/> for JSON.
        /// </remarks>
        [JsonConverter(typeof(StochasticOscillatorConverter))]
        public (double? K, double? D) StochasticOscillator_Medium { get; set; }

        /// <summary>
        /// Gets or sets the long-term Stochastic Oscillator (K=14, D=3, day candlesticks).
        /// </summary>
        /// <remarks>
        /// Momentum indicator (%K, %D, 0-100) for price range.
        /// Sourced from <see cref="MarketData.StochasticOscillator_Long"/>, computed by <see cref="TradingCalculator.CalculateStochastic"/>.
        /// %D null if < 14 candlesticks or invalid; %K computed if possible. Uses <see cref="StochasticOscillatorConverter"/> for JSON.
        /// </remarks>
        [JsonConverter(typeof(StochasticOscillatorConverter))]
        public (double? K, double? D) StochasticOscillator_Long { get; set; }

        /// <summary>
        /// Gets or sets the medium-term OBV (hour candlesticks).
        /// </summary>
        /// <remarks>
        /// Cumulative volume based on price direction.
        /// Sourced from <see cref="MarketData.OBV_Medium"/>, computed by <see cref="TradingCalculator.CalculateOBV"/>.
        /// 0 if < 2 candlesticks. Positive/negative for buying/selling pressure.
        /// </remarks>
        public long OBV_Medium { get; set; }

        /// <summary>
        /// Gets or sets the long-term OBV (day candlesticks).
        /// </summary>
        /// <remarks>
        /// Cumulative volume based on price direction.
        /// Sourced from <see cref="MarketData.OBV_Long"/>, computed by <see cref="TradingCalculator.CalculateOBV"/>.
        /// 0 if < 2 candlesticks. Positive/negative for buying/selling pressure.
        /// </remarks>
        public long OBV_Long { get; set; }
        #endregion

        #region Market Metadata
        /// <summary>
        /// Gets or sets whether change metrics are mature.
        /// </summary>
        /// <remarks>
        /// Indicates if order book change metrics are stable based on market activity duration.
        /// Sourced from <see cref="MarketData.ChangeMetricsMature"/>, determined by <see cref="OrderbookChangeTracker.IsMature"/>.
        /// </remarks>
        public bool ChangeMetricsMature { get; set; }

        /// <summary>
        /// Gets or sets the market's age.
        /// </summary>
        /// <remarks>
        /// Time since market opened, in days.
        /// Sourced from <see cref="MarketData.MarketAge"/>. Used to assess market maturity.
        /// </remarks>
        public TimeSpan? MarketAge { get; set; }

        /// <summary>
        /// Gets or sets the time remaining until market close.
        /// </summary>
        /// <remarks>
        /// Time until market resolution, in days.
        /// Sourced from <see cref="MarketData.TimeLeft"/>. Negative if market is closed.
        /// </remarks>
        public TimeSpan? TimeLeft { get; set; }

        /// <summary>
        /// Gets or sets whether the market can close early.
        /// </summary>
        /// <remarks>
        /// Indicates if the market may resolve before its scheduled close.
        /// Sourced from <see cref="MarketData.CanCloseEarly"/>. Affects trading strategy.
        /// </remarks>
        public bool CanCloseEarly { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last WebSocket message received.
        /// </summary>
        /// <remarks>
        /// Indicates the most recent update from the exchange.
        /// Sourced from <see cref="MarketData.LastWebSocketMessageReceived"/>. MinValue if no messages.
        /// </remarks>
        public DateTime LastWebSocketMessageReceived { get; set; }

        /// <summary>
        /// Gets or sets the "Yes" side market behavior classification.
        /// </summary>
        public string MarketBehaviorYes { get; set; }

        /// <summary>
        /// Gets or sets the "No" side market behavior classification.
        /// </summary>
        public string MarketBehaviorNo { get; set; }

        /// <summary>
        /// Gets or sets the "Yes" side price quality assessment.
        /// </summary>
        public string GoodBadPriceYes { get; set; }

        /// <summary>
        /// Gets or sets the "No" side price quality assessment.
        /// </summary>
        public string GoodBadPriceNo { get; set; }

        public string MarketType { get; set; }


        #endregion

        #region Additional Metrics
        /// <summary>
        /// Gets or sets the holding period for the current position, in days.
        /// </summary>
        /// <remarks>
        /// Time since position was opened.
        /// Sourced from <see cref="MarketData.HoldTime"/>. 0 if no position.
        /// </remarks>
        public TimeSpan? HoldTime { get; set; }

        /// <summary>
        /// Gets or sets the order rate for "Yes" ask orders, in dollars per minute.
        /// </summary>
        /// <remarks>
        /// Rate of non-trade-related "Yes" ask (No bid) order changes.
        /// Sourced from <see cref="MarketData.OrderVolumePerMinute_YesAsk"/>, computed by <see cref="OrderbookChangeTracker.GetNoNetOrderRatePerMinute"/> (negated).
        /// Sums dollar value (price/100 * delta contracts) divided by elapsed minutes.
        /// </remarks>
        public double OrderVolumePerMinute_YesAsk { get; set; }

        /// <summary>
        /// Gets or sets the order rate for "Yes" bid orders, in dollars per minute.
        /// </summary>
        /// <remarks>
        /// Rate of non-trade-related "Yes" bid order changes.
        /// Sourced from <see cref="MarketData.OrderVolumePerMinute_YesBid"/>, computed by <see cref="OrderbookChangeTracker.GetYesNetOrderRatePerMinute"/>.
        /// Sums dollar value (price/100 * delta contracts) divided by elapsed minutes.
        /// </remarks>
        public double OrderVolumePerMinute_YesBid { get; set; }

        /// <summary>
        /// Gets or sets the order rate for "No" ask orders, in dollars per minute.
        /// </summary>
        /// <remarks>
        /// Rate of non-trade-related "No" ask (Yes bid) order changes.
        /// Sourced from <see cref="MarketData.OrderVolumePerMinute_NoAsk"/>, computed by <see cref="OrderbookChangeTracker.GetYesNetOrderRatePerMinute"/> (negated).
        /// Sums dollar value (price/100 * delta contracts) divided by elapsed minutes.
        /// </remarks>
        public double OrderVolumePerMinute_NoAsk { get; set; }

        /// <summary>
        /// Gets or sets the order rate for "No" bid orders, in dollars per minute.
        /// </summary>
        /// <remarks>
        /// Rate of non-trade-related "No" bid order changes.
        /// Sourced from <see cref="MarketData.OrderVolumePerMinute_NoBid"/>, computed by <see cref="OrderbookChangeTracker.GetNoNetOrderRatePerMinute"/>.
        /// Sums dollar value (price/100 * delta contracts) divided by elapsed minutes.
        /// </remarks>
        public double OrderVolumePerMinute_NoBid { get; set; }

        /// <summary>
        /// Gets or sets the center of mass for "Yes" bid orders.
        /// </summary>
        /// <remarks>
        /// Weighted average price of "Yes" bid orders, in cents.
        /// Sourced from <see cref="MarketData.YesBidCenterOfMass"/>, computed from <see cref="MarketData.OrderbookData"/>.
        /// 0 if no orders.
        /// </remarks>
        public double YesBidCenterOfMass { get; set; }

        /// <summary>
        /// Gets or sets the center of mass for "No" bid orders.
        /// </summary>
        /// <remarks>
        /// Weighted average price of "No" bid orders, in cents.
        /// Sourced from <see cref="MarketData.NoBidCenterOfMass"/>, computed from <see cref="MarketData.OrderbookData"/>.
        /// 0 if no orders.
        /// </remarks>
        public double NoBidCenterOfMass { get; set; }

        /// <summary>
        /// Gets or sets the tolerance percentage for cumulative depth calculations.
        /// </summary>
        /// <remarks>
        /// Percentage (e.g., 5%) used to define price range for <see cref="TopTenPercentLevelDepth_Yes"/> and <see cref="TopTenPercentLevelDepth_No"/>.
        /// Sourced from <see cref="MarketData.TolerancePercentage"/>. Typically 5%.
        /// </remarks>
        public double TolerancePercentage { get; set; }
        #endregion


        public double CalculateLiquidityScore()
        {
            // Early exit for illiquid markets
            if (BestYesBid == 0 || BestNoBid == 0 || TotalBidContracts_Yes == 0 || TotalBidContracts_No == 0)
            {
                return 0.0;
            }

            // Hypothetical order size: $10 (1000 cents) adjusted by price, or current position if non-zero
            int hypotheticalContracts = PositionSize != 0 ? Math.Abs(PositionSize) : Math.Max(10, 1000 / Math.Max(BestYesBid, BestNoBid));

            // Helper: Normalize a value between 0 and 1 (non-inverted: higher = better; inverted: lower = better)
            double Normalize(double value, double maxCap, bool inverted = false)
            {
                if (maxCap <= 0) return inverted ? 1.0 : 0.0; // Avoid division by zero
                double normalized = Math.Min(1.0, value / maxCap);
                return inverted ? 1.0 - normalized : normalized;
            }

            // Metric 1: Spread score (inverted: lower spread = higher score, in cents)
            double spreadCapYes = Math.Max(YesSpread * 2, 5); // Dynamic cap, minimum 5 cents
            double spreadScoreYes = Normalize(YesSpread, spreadCapYes, true);
            double spreadCapNo = Math.Max(NoSpread * 2, 5);
            double spreadScoreNo = Normalize(NoSpread, spreadCapNo, true);

            // Metric 2: Top-level depth score (non-inverted, in cents)
            double depthYesCents = DepthAtBestYesBid * BestYesBid; // Yes side: price * contracts
            double depthCapYes = Math.Max(depthYesCents * 2, TotalOrderbookDepth_Yes / 10); // Cap at 10% of total depth, min ~$100
            double depthScoreYes = Normalize(depthYesCents, depthCapYes);
            double depthNoCents = DepthAtBestNoBid * BestNoBid; // Corrected to use BestNoBid price
            double depthCapNo = Math.Max(depthNoCents * 2, TotalOrderbookDepth_No / 10);
            double depthScoreNo = Normalize(depthNoCents, depthCapNo);

            // Metric 3: Cumulative depth score within tolerance (non-inverted, in cents)
            double cumDepthYesCents = 0;
            foreach (var bid in GetYesBids().Where(bid => bid.Key >= BestYesBid * (1 - TolerancePercentage / 100)))
            {
                cumDepthYesCents += bid.Key * bid.Value; // Price * contracts in cents
            }
            double cumDepthCapYes = Math.Max(cumDepthYesCents * 2, TotalOrderbookDepth_Yes / 5); // Cap at 20% of total depth, min ~$500
            double cumDepthScoreYes = Normalize(cumDepthYesCents, cumDepthCapYes);
            double cumDepthNoCents = 0;
            foreach (var bid in GetNoBids().Where(bid => bid.Key >= BestNoBid * (1 - TolerancePercentage / 100)))
            {
                cumDepthNoCents += bid.Key * bid.Value; // Corrected to use bid price
            }
            double cumDepthCapNo = Math.Max(cumDepthNoCents * 2, TotalOrderbookDepth_No / 5);
            double cumDepthScoreNo = Normalize(cumDepthNoCents, cumDepthCapNo);

            // Metric 4: Recent volume score (non-inverted, in contracts)
            double volumeCap = Math.Max(HighestVolume_Hour * 2, 100); // Dynamic cap, minimum 100 contracts
            double volumeScore = Normalize(RecentVolume_LastHour, volumeCap);

            // Metric 5: Imbalance score (inverted: lower imbalance = higher score, in cents)
            double imbalanceCents = Math.Abs(TotalOrderbookDepth_Yes - TotalOrderbookDepth_No);
            double imbalanceCap = Math.Max(imbalanceCents * 2, 50000); // Dynamic cap, maximum ~$500
            double imbalanceScore = Normalize(imbalanceCents, imbalanceCap, true);

            // Metric 6: Slippage estimate (price impact for hypothetical order, in cents; inverted)
            double SlippageForSize(string side)
            {
                var bids = side == "yes" ? GetYesBids() : GetNoBids();
                var sortedBids = new SortedDictionary<int, int>(bids, new DescendingComparer<int>()); // Highest prices first
                double totalCostCents = 0;
                int remaining = hypotheticalContracts;
                int startPrice = side == "yes" ? BestYesBid : BestNoBid;
                foreach (var level in sortedBids)
                {
                    int take = Math.Min(remaining, level.Value);
                    totalCostCents += take * level.Key; // Cost in cents
                    remaining -= take;
                    if (remaining <= 0) break;
                }
                // Penalize insufficient depth by assuming remaining unfilled (slippage to 0 cents)
                double avgPriceCents = remaining > 0 ? 0 : totalCostCents / (hypotheticalContracts - remaining);
                return Math.Abs(startPrice - avgPriceCents); // Slippage in cents
            }
            double slippageYes = SlippageForSize("yes");
            double slippageNo = SlippageForSize("no");
            double slippageCap = Math.Max(3, Math.Max(slippageYes, slippageNo) * 2); // Dynamic cap, minimum 3 cents
            double slippageScoreYes = Normalize(slippageYes, slippageCap, true);
            double slippageScoreNo = Normalize(slippageNo, slippageCap, true);

            // Weighted average for each side (weights sum to 1.0)
            double yesLiquidity = 0.25 * spreadScoreYes + 0.25 * depthScoreYes + 0.15 * cumDepthScoreYes +
                                  0.05 * volumeScore + 0.10 * imbalanceScore + 0.20 * slippageScoreYes;
            double noLiquidity = 0.25 * spreadScoreNo + 0.25 * depthScoreNo + 0.15 * cumDepthScoreNo +
                                 0.05 * volumeScore + 0.10 * imbalanceScore + 0.20 * slippageScoreNo;

            // Average sides and scale to 0-100
            double totalLiquidity = (yesLiquidity + noLiquidity) / 2.0 * 100.0;
            return Math.Round(totalLiquidity, 2);
        }

        internal class DescendingComparer<T> : IComparer<T> where T : IComparable<T>
        {
            public int Compare(T x, T y) => y.CompareTo(x);
        }

        public void UpgradeSnapshot(int CurrentVersion)
        {
            while (SnapshotSchemaVersion < CurrentVersion)
            {
                switch (SnapshotSchemaVersion)
                {
                    case 23:
                        throw new NotImplementedException();
                    case 22:
                        long totalYesDepth = 0;
                        long totalNoDepth = 0;

                        foreach (var level in GetYesBids())
                        {
                            totalYesDepth += level.Value * level.Key;
                        }
                        foreach (var level in GetNoBids())
                        {
                            totalNoDepth += level.Value * level.Key;
                        }
                        TotalOrderbookDepth_Yes = totalYesDepth;
                        TotalOrderbookDepth_No = totalNoDepth;
                        SnapshotSchemaVersion = 23;
                        break;
                    case 21:
                        BollingerBands_Medium = (
                            BollingerBands_Medium.Upper,   // Correct Lower (was calculated lower)
                            BollingerBands_Medium.Lower,   // Correct Middle (was calculated middle)
                            BollingerBands_Medium.Middle   // Correct Upper (was calculated upper)
                        );

                        BollingerBands_Long = (
                            BollingerBands_Long.Upper,     // Correct Lower
                            BollingerBands_Long.Lower,     // Correct Middle
                            BollingerBands_Long.Middle     // Correct Upper
                        );
                        SnapshotSchemaVersion = 22;
                        break;
                    default:
                        SnapshotSchemaVersion = 21;
                        break;
                }
            }
        }

        public Dictionary<int, int> GetYesBids()
        {
            var dict = new Dictionary<int, int>();
            foreach (var entry in OrderbookData.Where(e => (e["side"].ToString()) == "yes"))
            {
                int price = Int32.Parse(entry["price"].ToString());
                int rc = Int32.Parse(entry["resting_contracts"].ToString());
                dict[price] = rc;
            }
            return dict;
        }

        public Dictionary<int, int> GetNoBids()
        {
            var dict = new Dictionary<int, int>();
            foreach (var entry in OrderbookData.Where(e => (e["side"].ToString()) == "no"))
            {
                int price = Int32.Parse(entry["price"].ToString());
                int rc = Int32.Parse(entry["resting_contracts"].ToString());
                dict[price] = rc;
            }
            return dict;
        }

        public MarketSnapshot Clone()
        {
            return new MarketSnapshot
            {
                Timestamp = this.Timestamp,
                MarketTicker = this.MarketTicker,
                MarketCategory = this.MarketCategory,
                MarketStatus = this.MarketStatus,
                SnapshotSchemaVersion = this.SnapshotSchemaVersion,
                OrderbookData = this.OrderbookData?.Select(dict => new Dictionary<string, object>(dict)).ToList(),  // Deep copy dictionaries
                AllTimeHighYes_Bid = this.AllTimeHighYes_Bid,
                AllTimeLowYes_Bid = this.AllTimeLowYes_Bid,
                AllTimeHighNo_Bid = this.AllTimeHighNo_Bid,
                AllTimeLowNo_Bid = this.AllTimeLowNo_Bid,
                RecentHighYes_Bid = this.RecentHighYes_Bid,
                RecentLowYes_Bid = this.RecentLowYes_Bid,
                RecentHighNo_Bid = this.RecentHighNo_Bid,
                RecentLowNo_Bid = this.RecentLowNo_Bid,
                AllSupportResistanceLevels = this.AllSupportResistanceLevels?.Select(l => new SupportResistanceLevel
                {
                    Price = l.Price,
                    Strength = l.Strength,
                    TestCount = l.TestCount,
                    TotalVolume = l.TotalVolume,
                    CandlestickCount = l.CandlestickCount
                }).ToList(),
                PositionSize = this.PositionSize,
                MarketExposure = this.MarketExposure,
                BuyinPrice = this.BuyinPrice,
                PositionUpside = this.PositionUpside,
                PositionDownside = this.PositionDownside,
                TotalTraded = this.TotalTraded,
                RestingOrders = this.RestingOrders?.Select(o => o).ToList(),  // Value tuple copy
                RealizedPnl = this.RealizedPnl,
                FeesPaid = this.FeesPaid,
                PositionROI = this.PositionROI,
                PositionROIAmt = this.PositionROIAmt,
                TradeRatePerMinute_Yes = this.TradeRatePerMinute_Yes,
                TradeRatePerMinute_No = this.TradeRatePerMinute_No,
                TradeVolumePerMinute_Yes = this.TradeVolumePerMinute_Yes,
                TradeVolumePerMinute_No = this.TradeVolumePerMinute_No,
                TradeCount_Yes = this.TradeCount_Yes,
                TradeCount_No = this.TradeCount_No,
                AverageTradeSize_Yes = this.AverageTradeSize_Yes,
                AverageTradeSize_No = this.AverageTradeSize_No,
                HighestVolume_Day = this.HighestVolume_Day,
                HighestVolume_Hour = this.HighestVolume_Hour,
                HighestVolume_Minute = this.HighestVolume_Minute,
                RecentVolume_LastHour = this.RecentVolume_LastHour,
                RecentVolume_LastThreeHours = this.RecentVolume_LastThreeHours,
                RecentVolume_LastMonth = this.RecentVolume_LastMonth,
                VelocityPerMinute_Bottom_Yes_Bid = this.VelocityPerMinute_Bottom_Yes_Bid,
                LevelCount_Bottom_Yes_Bid = this.LevelCount_Bottom_Yes_Bid,
                VelocityPerMinute_Bottom_No_Bid = this.VelocityPerMinute_Bottom_No_Bid,
                LevelCount_Bottom_No_Bid = this.LevelCount_Bottom_No_Bid,
                VelocityPerMinute_Top_Yes_Bid = this.VelocityPerMinute_Top_Yes_Bid,
                LevelCount_Top_Yes_Bid = this.LevelCount_Top_Yes_Bid,
                VelocityPerMinute_Top_No_Bid = this.VelocityPerMinute_Top_No_Bid,
                LevelCount_Top_No_Bid = this.LevelCount_Top_No_Bid,
                YesSpread = this.YesSpread,
                NoSpread = this.NoSpread,
                DepthAtBestYesBid = this.DepthAtBestYesBid,
                DepthAtBestNoBid = this.DepthAtBestNoBid,
                TopTenPercentLevelDepth_Yes = this.TopTenPercentLevelDepth_Yes,
                TopTenPercentLevelDepth_No = this.TopTenPercentLevelDepth_No,
                BidRange_Yes = this.BidRange_Yes,
                BidRange_No = this.BidRange_No,
                TotalBidContracts_Yes = this.TotalBidContracts_Yes,
                TotalBidContracts_No = this.TotalBidContracts_No,
                BidCountImbalance = this.BidCountImbalance,
                BidVolumeImbalance = this.BidVolumeImbalance,
                DepthAtTop4YesBids = this.DepthAtTop4YesBids,
                DepthAtTop4NoBids = this.DepthAtTop4NoBids,
                TotalBidVolume_Yes = this.TotalBidVolume_Yes,
                TotalBidVolume_No = this.TotalBidVolume_No,
                RSI_Short = this.RSI_Short,
                RSI_Medium = this.RSI_Medium,
                RSI_Long = this.RSI_Long,
                MACD_Medium = this.MACD_Medium,
                MACD_Long = this.MACD_Long,
                EMA_Medium = this.EMA_Medium,
                EMA_Long = this.EMA_Long,
                BollingerBands_Medium = this.BollingerBands_Medium,
                BollingerBands_Long = this.BollingerBands_Long,
                ATR_Medium = this.ATR_Medium,
                ATR_Long = this.ATR_Long,
                VWAP_Short = this.VWAP_Short,
                VWAP_Medium = this.VWAP_Medium,
                StochasticOscillator_Short = this.StochasticOscillator_Short,
                StochasticOscillator_Medium = this.StochasticOscillator_Medium,
                StochasticOscillator_Long = this.StochasticOscillator_Long,
                OBV_Medium = this.OBV_Medium,
                OBV_Long = this.OBV_Long,
                ChangeMetricsMature = this.ChangeMetricsMature,
                MarketAge = this.MarketAge,
                TimeLeft = this.TimeLeft,
                CanCloseEarly = this.CanCloseEarly,
                LastWebSocketMessageReceived = this.LastWebSocketMessageReceived,
                MarketBehaviorYes = this.MarketBehaviorYes,
                MarketBehaviorNo = this.MarketBehaviorNo,
                GoodBadPriceYes = this.GoodBadPriceYes,
                GoodBadPriceNo = this.GoodBadPriceNo,
                MarketType = this.MarketType,
                HoldTime = this.HoldTime,
                OrderVolumePerMinute_YesAsk = this.OrderVolumePerMinute_YesAsk,
                OrderVolumePerMinute_YesBid = this.OrderVolumePerMinute_YesBid,
                OrderVolumePerMinute_NoAsk = this.OrderVolumePerMinute_NoAsk,
                OrderVolumePerMinute_NoBid = this.OrderVolumePerMinute_NoBid,
                NonTradeRelatedOrderCount_Yes = this.NonTradeRelatedOrderCount_Yes,
                NonTradeRelatedOrderCount_No = this.NonTradeRelatedOrderCount_No,
                YesBidCenterOfMass = this.YesBidCenterOfMass,
                NoBidCenterOfMass = this.NoBidCenterOfMass,
                TolerancePercentage = this.TolerancePercentage,
                ExpectedFees = this.ExpectedFees,
                TotalOrderbookDepth_Yes = this.TotalOrderbookDepth_Yes,
                TotalOrderbookDepth_No = this.TotalOrderbookDepth_No,
                YesBidSlopePerMinute_Short = this.YesBidSlopePerMinute_Short,
                NoBidSlopePerMinute_Short = this.NoBidSlopePerMinute_Short,
                YesBidSlopePerMinute_Medium = this.YesBidSlopePerMinute_Medium,
                NoBidSlopePerMinute_Medium = this.NoBidSlopePerMinute_Medium,
                PSAR = this.PSAR,
                ADX = this.ADX
            };
        }

        public sealed record Difference(string Path, object? Left, object? Right);

        public List<Difference> Diff(MarketSnapshot other)
        {
            var diffs = new List<Difference>();
            if (other is null) { diffs.Add(new Difference("$", this, null)); return diffs; }

            var props = typeof(MarketSnapshot).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (var p in props)
            {
                if (!p.CanRead || p.GetIndexParameters().Length > 0) continue;
                var a = p.GetValue(this);
                var b = p.GetValue(other);
                Compare($"{p.Name}", a, b, diffs);
            }
            return diffs;

            static bool IsSimple(System.Type t)
            {
                t = System.Nullable.GetUnderlyingType(t) ?? t;
                if (t.IsPrimitive || t.IsEnum) return true;
                return t == typeof(string) || t == typeof(decimal) || t == typeof(System.DateTime) ||
                       t == typeof(System.DateTimeOffset) || t == typeof(System.TimeSpan) || t == typeof(System.Guid);
            }

            static bool IsValueTuple(System.Type t) =>
                t.IsValueType && t.FullName != null && t.FullName.StartsWith("System.ValueTuple`", System.StringComparison.Ordinal);

            static void Compare(string path, object? a, object? b, List<Difference> diffs)
            {
                if (object.ReferenceEquals(a, b)) return;
                if (a is null || b is null) { diffs.Add(new Difference(path, a, b)); return; }

                var ta = a.GetType();
                var tb = b.GetType();
                if (ta != tb) { diffs.Add(new Difference(path, a, b)); return; }

                if (IsSimple(ta))
                {
                    if (!object.Equals(a, b)) diffs.Add(new Difference(path, a, b));
                    return;
                }

                if (ta == typeof(string))
                {
                    if (!System.String.Equals((string)a, (string)b, System.StringComparison.Ordinal))
                        diffs.Add(new Difference(path, a, b));
                    return;
                }

                if (IsValueTuple(ta))
                {
                    var fs = ta.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    foreach (var f in fs)
                        Compare($"{path}.{f.Name}", f.GetValue(a), f.GetValue(b), diffs);
                    return;
                }

                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(ta))
                {
                    if (ta == typeof(string)) { if (!object.Equals(a, b)) diffs.Add(new Difference(path, a, b)); return; }

                    var la = new System.Collections.Generic.List<object?>();
                    var lb = new System.Collections.Generic.List<object?>();

                    var ea = ((System.Collections.IEnumerable)a).GetEnumerator();
                    while (ea.MoveNext()) la.Add(ea.Current);
                    var eb = ((System.Collections.IEnumerable)b).GetEnumerator();
                    while (eb.MoveNext()) lb.Add(eb.Current);

                    if (la.Count != lb.Count) diffs.Add(new Difference($"{path}.Count", la.Count, lb.Count));

                    var n = System.Math.Min(la.Count, lb.Count);
                    for (int i = 0; i < n; i++)
                        Compare($"{path}[{i}]", la[i], lb[i], diffs);
                    return;
                }

                var props = ta.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                foreach (var p in props)
                {
                    if (!p.CanRead || p.GetIndexParameters().Length > 0) continue;
                    Compare($"{path}.{p.Name}", p.GetValue(a), p.GetValue(b), diffs);
                }
            }
        }

    }

    public class MACDConverter : JsonConverter<(double? MACD, double? Signal, double? Histogram)>
    {
        public override (double? MACD, double? Signal, double? Histogram) Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();
            double? macd = null, signal = null, histogram = null;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return (macd, signal, histogram);
                if (reader.TokenType != JsonTokenType.PropertyName) continue;
                string? prop = reader.GetString();
                reader.Read();
                switch (prop)
                {
                    case "macd":
                        macd = reader.TokenType == JsonTokenType.Null ? null : reader.GetDouble();
                        break;
                    case "signal":
                        signal = reader.TokenType == JsonTokenType.Null ? null : reader.GetDouble();
                        break;
                    case "histogram":
                        histogram = reader.TokenType == JsonTokenType.Null ? null : reader.GetDouble();
                        break;
                }
            }
            throw new JsonException();
        }

        public override void Write(
            Utf8JsonWriter writer,
            (double? MACD, double? Signal, double? Histogram) value,
            JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("macd");
            if (value.MACD.HasValue) writer.WriteNumberValue(value.MACD.Value);
            else writer.WriteNullValue();

            writer.WritePropertyName("signal");
            if (value.Signal.HasValue) writer.WriteNumberValue(value.Signal.Value);
            else writer.WriteNullValue();

            writer.WritePropertyName("histogram");
            if (value.Histogram.HasValue) writer.WriteNumberValue(value.Histogram.Value);
            else writer.WriteNullValue();

            writer.WriteEndObject();
        }
    }

    public class BollingerBandsConverter : JsonConverter<(double? Lower, double? Middle, double? Upper)>
    {
        public override (double? Lower, double? Middle, double? Upper) Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();
            double? lower = null, middle = null, upper = null;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) return (lower, middle, upper);
                if (reader.TokenType != JsonTokenType.PropertyName) continue;
                string prop = reader.GetString();
                reader.Read();
                switch (prop)
                {
                    case "lower":
                        lower = reader.TokenType == JsonTokenType.Null ? null : reader.GetDouble();
                        break;
                    case "middle":
                        middle = reader.TokenType == JsonTokenType.Null ? null : reader.GetDouble();
                        break;
                    case "upper":
                        upper = reader.TokenType == JsonTokenType.Null ? null : reader.GetDouble();
                        break;
                }
            }
            throw new JsonException();
        }

        public override void Write(
            Utf8JsonWriter writer,
            (double? Lower, double? Middle, double? Upper) value,
            JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("lower");
            if (value.Lower.HasValue) writer.WriteNumberValue(value.Lower.Value);
            else writer.WriteNullValue();

            writer.WritePropertyName("middle");
            if (value.Middle.HasValue) writer.WriteNumberValue(value.Middle.Value);
            else writer.WriteNullValue();

            writer.WritePropertyName("upper");
            if (value.Upper.HasValue) writer.WriteNumberValue(value.Upper.Value);
            else writer.WriteNullValue();

            writer.WriteEndObject();
        }
    }

    public class StochasticOscillatorConverter : JsonConverter<(double? K, double? D)>
    {
        public override (double? K, double? D) Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();
            double? k = null, d = null;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) return (k, d);
                if (reader.TokenType != JsonTokenType.PropertyName) continue;
                string? prop = reader.GetString();
                reader.Read();
                switch (prop)
                {
                    case "k":
                        k = reader.TokenType == JsonTokenType.Null ? null : reader.GetDouble();
                        break;
                    case "d":
                        d = reader.TokenType == JsonTokenType.Null ? null : reader.GetDouble();
                        break;
                }
            }
            throw new JsonException();
        }

        public override void Write(
            Utf8JsonWriter writer,
            (double? K, double? D) value,
            JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("k");
            if (value.K.HasValue) writer.WriteNumberValue(value.K.Value);
            else writer.WriteNullValue();

            writer.WritePropertyName("d");
            if (value.D.HasValue) writer.WriteNumberValue(value.D.Value);
            else writer.WriteNullValue();

            writer.WriteEndObject();
        }

    }
}