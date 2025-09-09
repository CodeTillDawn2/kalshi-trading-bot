class MarketModel {
    constructor() {
        this.subscribedMarkets = [];
        this.candlesticks = { minute: {}, hour: {}, day: {} };
        this.tickers = {};
        this.positions = {};
        this.warningCount = 0;
        this.errorCount = 0;
        this.orderbook = {};
        this.listeners = [];
        this.isLoading = false;
        this.marketInfo = {};
        this.priceData = {};
        this.historicalData = {};
        this.balance = 0;
        this.positionsValue = 0;
        this.lastWebSocketUpdate = null;
        this.orderbookLastUpdated = {};
        this.batchUpdate = false;
        this.exchangeStatus = false;
        this.tradingStatus = false;
        //console.log("MarketModel initialized");
    }

    addListener(listener) { this.listeners.push(listener); }

    updateExchangeStatus(status) {
        this.exchangeStatus = status;
        //console.log(`Updated exchange status: ${status ? 'Open' : 'Closed'}`);
        this.notifyListeners();
    }

    updateTradingStatus(status) {
        this.tradingStatus = status;
        //console.log(`Updated trading status: ${status ? 'Open' : 'Closed'}`);
        this.notifyListeners();
    }

    notifyListeners() {
        this.listeners.forEach(listener => listener());
    }

    startBatchUpdate() {
        this.batchUpdate = true;
        //console.log("Started batch update");
    }

    endBatchUpdate() {
        this.batchUpdate = false;
        this.notifyListeners();
        //console.log("Ended batch update, notifying listeners");
    }

    setLoading(loading) {
        this.isLoading = loading;
        this.notifyListeners();
    }

    setSubscribedMarkets(markets) {
        const marketsToRemove = Object.keys(this.tickers).filter(ticker => !markets.includes(ticker));
        marketsToRemove.forEach(ticker => this.clearMarketData(ticker));
        this.subscribedMarkets = markets;
        //console.log(`Subscribed markets updated: ${markets.join(", ")}, tickers retained: ${Object.keys(this.tickers).join(", ")}`);
        this.notifyListeners();
    }

    updateCandlesticks(marketTicker, interval, candlesticks) {
        if (!this.candlesticks[interval]) this.candlesticks[interval] = {};
        this.candlesticks[interval][marketTicker] = candlesticks.slice();
        //console.log(`Updated candlesticks for ${marketTicker} (${interval}):`, this.candlesticks[interval][marketTicker]);
    }

    updatePositionPriceMetadata(marketTicker, positions, batch = false) {
        if (!marketTicker || !positions || !Array.isArray(positions)) {
            console.warn(`Invalid position price metadata for ${marketTicker}:`, positions);
            return;
        }
        this.positions[marketTicker] = this.positions[marketTicker] || [];

        // Update only the relevant fields for existing positions
        positions.forEach(newPos => {
            const existingPos = this.positions[marketTicker].find(p => p.ticker === newPos.ticker) || {};
            Object.assign(existingPos, {
                ticker: newPos.ticker || marketTicker,
                positionROI: parseFloat(newPos.positionROI) || 0,
                positionROIAmt: newPos.positionROIAmt || 0,
                positionUpside: newPos.positionUpside || 0,
                positionDownside: newPos.positionDownside || 0
            });

            // If this is a new position, add it with default values for other fields
            if (!this.positions[marketTicker].some(p => p.ticker === newPos.ticker)) {
                this.positions[marketTicker].push({
                    ...existingPos,
                    totalTraded: existingPos.totalTraded || "--",
                    position: existingPos.position || 0,
                    marketExposure: existingPos.marketExposure || 0,
                    buyinPrice: existingPos.buyinPrice || 0,
                    realizedPnl: existingPos.realizedPnl || 0,
                    feesPaid: existingPos.feesPaid || 0,
                    holdTime: existingPos.holdTime || "--",
                    lastUpdatedTs: existingPos.lastUpdatedTs || "0001-01-01T00:00:00"
                });
            }
        });

        if (!batch) this.notifyListeners();
    }

    addTicker(marketTicker, ticker, batch = false) {
        if (!this.tickers[marketTicker]) this.tickers[marketTicker] = [];
        const normalizedTicker = {
            x: ticker.x || moment(ticker.timestamp).valueOf(),
            yesAsk: ticker.yesAsk || 0,
            yesBid: ticker.yesBid || 0,
            timestamp: ticker.timestamp,
            source: ticker.source || "Unknown",
            yesSpread: ticker.yesSpread || 0,
            noSpread: ticker.noSpread || 0
        };

        this.tickers[marketTicker].push(normalizedTicker);

        // Log the last few tickers to check order
        const recentTickers = this.tickers[marketTicker].slice(-3).map((t, i) => ({
            index: this.tickers[marketTicker].length - 3 + i,
            timestamp: t.timestamp,
            x: t.x
        }));
        console.log(`Tickers for ${marketTicker} after adding (last 3):`, recentTickers);

        // Check if the array is sorted at the end
        const isArraySorted = this.tickers[marketTicker].every((t, i, arr) =>
            i === 0 || t.x >= arr[i - 1].x
        );
        console.log(`Is tickers array sorted for ${marketTicker}? ${isArraySorted}`);

        if (!batch) this.notifyListeners();
    }

    updateOrderbook(data, batch = false) {
        if (!data || !data.marketTicker) return;
        const marketTicker = data.marketTicker;

        this.orderbook[marketTicker] = data.orders || [];
        this.orderbookLastUpdated[marketTicker] = data.lastUpdated ? moment.utc(data.lastUpdated).toDate() : null;

        this.priceData[marketTicker] = {
            ...this.priceData[marketTicker],
            cumulativeYesBidDepth: data.cumulativeYesBidDepth || 0,
            cumulativeNoBidDepth: data.cumulativeNoBidDepth || 0,
            yesBidRange: data.yesBidRange || 0,
            noBidRange: data.noBidRange || 0,
            yesBidRange: data.yesBidRange || 0,
            noBidRange: data.noBidRange || 0,
            depthAtBestYesBid: data.depthAtBestYesBid || 0,
            depthAtBestNoBid: data.depthAtBestNoBid || 0,
            depthAtBestYesAsk: data.depthAtBestYesAsk || 0,
            depthAtBestNoAsk: data.depthAtBestNoAsk || 0,
            totalYesBidContracts: data.totalYesBidContracts || 0,
            totalNoBidContracts: data.totalNoBidContracts || 0,
            totalYesAskContracts: data.totalYesAskContracts || 0,
            totalNoAskContracts: data.totalNoAskContracts || 0,
            bidImbalance: data.bidImbalance || 0,
            askBidImbalanceVolume: data.askBidImbalanceVolume || 0,
            depthAtTop4YesBids: data.depthAtTop4YesBids || 0,
            depthAtTop4NoBids: data.depthAtTop4NoBids || 0,
            depthAtTop4YesAsks: data.depthAtTop4YesAsks || 0,
            depthAtTop4NoAsks: data.depthAtTop4NoAsks || 0,
            yesBidCenterOfMass: data.yesBidCenterOfMass || 0,
            noBidCenterOfMass: data.noBidCenterOfMass || 0,
            yesAskCenterOfMass: data.yesAskCenterOfMass || 0,
            noAskCenterOfMass: data.noAskCenterOfMass || 0,
            fullImbalance: data.fullImbalance || 0,
            lastUpdated: data.lastUpdated || 0
        };

        if (!batch) this.notifyListeners();
    }

    updateRealTimeMetrics(data, batch = false) {
        if (!data || !data.marketTicker) return;
        const marketTicker = data.marketTicker;

        this.priceData[marketTicker] = this.priceData[marketTicker] || {};

        this.priceData[marketTicker] = {
            ...this.priceData[marketTicker],
            velocityPerMinute_Bottom_YesBid: data.velocityPerMinute_Bottom_YesBid || 0,
            velocityPerMinute_Bottom_YesAsk: data.velocityPerMinute_Bottom_YesAsk || 0,
            velocityPerMinute_Bottom_NoBid: data.velocityPerMinute_Bottom_NoBid || 0,
            velocityPerMinute_Bottom_NoAsk: data.velocityPerMinute_Bottom_NoAsk || 0,
            velocityPerMinute_Top_YesBid: data.velocityPerMinute_Top_YesBid || 0,
            velocityPerMinute_Top_NoBid: data.velocityPerMinute_Top_NoBid || 0,
            velocityPerMinute_Top_YesAsk: data.velocityPerMinute_Top_YesAsk || 0,
            velocityPerMinute_Top_NoAsk: data.velocityPerMinute_Top_NoAsk || 0,
            levelCount_Bottom_YesBid: data.levelCount_Bottom_YesBid || 0,
            levelCount_Bottom_YesAsk: data.levelCount_Bottom_YesAsk || 0,
            levelCount_Bottom_NoBid: data.levelCount_Bottom_NoBid || 0,
            levelCount_Bottom_NoAsk: data.levelCount_Bottom_NoAsk || 0,
            levelCount_Top_YesBid: data.levelCount_Top_YesBid || 0,
            levelCount_Top_NoBid: data.levelCount_Top_NoBid || 0,
            levelCount_Top_YesAsk: data.levelCount_Top_YesAsk || 0,
            levelCount_Top_NoAsk: data.levelCount_Top_NoAsk || 0,
            tradeVolumePerMinute_Yes: data.tradeVolumePerMinute_Yes || 0,
            tradeVolumePerMinute_No: data.tradeVolumePerMinute_No || 0,
            tradeRatePerMinute_Yes: data.tradeRatePerMinute_Yes || 0,
            tradeRatePerMinute_No: data.tradeRatePerMinute_No || 0,
            orderRatePerMinute_YesAsk: data.orderRatePerMinute_YesAsk || 0,
            orderRatePerMinute_YesBid: data.orderRatePerMinute_YesBid || 0,
            orderRatePerMinute_NoAsk: data.orderRatePerMinute_NoAsk || 0,
            orderRatePerMinute_NoBid: data.orderRatePerMinute_NoBid || 0,
            averageTradeSize_Yes: data.averageTradeSize_Yes || 0,
            averageTradeSize_No: data.averageTradeSize_No || 0,
            tradeCount_Yes: data.tradeCount_Yes || 0,
            tradeCount_No: data.tradeCount_No || 0,
            nonTradeRelatedOrderCount_Yes: data.nonTradeRelatedOrderCount_Yes || 0,
            nonTradeRelatedOrderCount_No: data.nonTradeRelatedOrderCount_No || 0,
            rsi: data.rsi || 0,
            macd: data.macd || { MACD: 0, Signal: 0, Histogram: 0 },
            ema: data.ema || 0,
            bollingerBands: data.bollingerBands || { lower: 0, middle: 0, upper: 0 },
            atr: data.atr || 0,
            vwap: data.vwap || 0,
            stochasticOscillator: data.stochasticOscillator || { K: 0, D: 0 },
            obv: data.obv || 0

        };

        this.marketInfo[marketTicker] = this.marketInfo[marketTicker] || {};

        this.warningCount = data.warningCount || 0;
        this.errorCount = data.errorCount || 0;

        if (!batch) this.notifyListeners();
    }

    updatePositions(marketTicker, positions, batch = false) {
        if (!marketTicker || !positions || !Array.isArray(positions)) {
            console.warn(`Invalid positions data for ${marketTicker}:`, positions);
            return;
        }
        this.positions[marketTicker] = positions.map(p => ({
            ticker: p.ticker || marketTicker,
            totalTraded: p.ticker || "--",
            position: p.position || 0,
            marketExposure: p.marketExposure / 100 || 0,
            realizedPnl: p.realizedPnl / 100 || 0,
            restingOrdersCount: p.restingOrdersCount || 0,
            feesPaid: p.feesPaid / 100 || 0,
            lastUpdatedTs: p.lastUpdatedTs || "0001-01-01T00:00:00",
            lastModified: p.lastModified || "0001-01-01T00:00:00",
            positionROI: parseFloat(p.positionROI) || 0,
            positionROIAmt: p.positionROIAmt || 0,
            holdTime: p.holdTime || "--",
            positionUpside: p.positionUpside || 0,
            positionDownside: p.positionDownside || 0,
            buyinPrice: Math.abs(p.buyinPrice)
        }));
        if (!batch) this.notifyListeners();
    }

    updateBalance(balance) {
        this.balance = balance || 0;
        this.notifyListeners();
    }

    updatePositionsValue(value) {
        this.positionsValue = value || 0;
        this.notifyListeners();
    }

    storeHistoricalData(marketTicker, interval, data) {
        if (!data || !marketTicker || !interval) return;

        this.historicalData[marketTicker] = this.historicalData[marketTicker] || {};
        this.historicalData[marketTicker][interval] = data;

        const candlestickData = data.candlesticks || {};
        if (candlestickData.data) {
            this.updateCandlesticks(marketTicker, interval, candlestickData.data);
        }

        if (data.marketInfo) {
            this.marketInfo[marketTicker] = {
                ...this.marketInfo[marketTicker],
                ...data.marketInfo,
                close_time: data.marketInfo.close_time,
                open_time: data.marketInfo.open_time
            };
        }

        if (data.priceData) {
            this.priceData[marketTicker] = {
                ...this.priceData[marketTicker],
                ...data.priceData,

                goodBadPriceYes: data.priceData.goodBadPriceYes,
                goodBadPriceNo: data.priceData.goodBadPriceNo,
                marketBehaviorYes: data.priceData.marketBehaviorYes,
                marketBehaviorNo: data.priceData.marketBehaviorNo,
                supportResistanceLevels: data.priceData.supportResistanceLevels || []
            };
        }

        //console.log(`Stored historical data for ${marketTicker} (${interval}):`, this.candlesticks[interval][marketTicker]);
        this.notifyListeners();
    }

    clearMarketData(marketTicker) {
        if (this.tickers[marketTicker]) this.tickers[marketTicker] = [];
        if (this.positions[marketTicker]) delete this.positions[marketTicker];
        if (this.orderbook[marketTicker]) delete this.orderbook[marketTicker];
        if (this.candlesticks.minute[marketTicker]) delete this.candlesticks.minute[marketTicker];
        if (this.candlesticks.hour[marketTicker]) delete this.candlesticks.hour[marketTicker];
        if (this.candlesticks.day[marketTicker]) delete this.candlesticks.day[marketTicker];
        if (this.marketInfo[marketTicker]) delete this.marketInfo[marketTicker];
        if (this.priceData[marketTicker]) delete this.priceData[marketTicker];
        if (this.historicalData[marketTicker]) delete this.historicalData[marketTicker];
        //console.log(`Cleared market data for ${marketTicker}`);
        this.notifyListeners();
    }

    isDataLoaded(marketTicker, interval) {
        return this.candlesticks[interval] && this.candlesticks[interval][marketTicker]?.length > 0;
    }

    getOrderbook(marketTicker) { return this.orderbook[marketTicker] || []; }

    getMarketData(marketTicker) {
        
        const position = this.positions[marketTicker]?.[0] || {};
        const price = this.priceData[marketTicker] || {};
        const market = this.marketInfo[marketTicker] || {};
        const latestTicker = this.tickers[marketTicker]?.slice(-1)[0] || {};

        //console.log(`Called getMarketData with marketTicker: ${marketTicker}`, price.stochasticOscillator);
        return {
            MarketInfo: market,
            Positions: this.positions[marketTicker] || [],
            PositionSize: position.position || 0,
            PositionLabel: position.position >= 0 ? "Yes" : "No",
            MarketExposure: position.marketExposure || 0,
            BuyinPrice: position.buyinPrice || 0,
            PositionUpside: position.positionUpside || 0,
            PositionDownside: position.positionDownside || 0,
            TotalTraded: position.totalTraded || "--",
            RealizedPnl: position.realizedPnl || 0,
            FeesPaid: position.feesPaid || 0,
            ExchangeStatus: this.exchangeStatus,
            TradingStatus: this.tradingStatus,
            PositionROI: position.positionROI || 0,
            PositionROIAmt: position.positionROIAmt || 0,
            HoldTime: position.holdTime || "--",
            RestingOrdersCount: position.restingOrdersCount || 0,  // Add restingOrdersCount
            CurrentPriceYes: price.currentPrice || { ask: 0, bid: 0, when: "0001-01-01T00:00:00", source: "Unknown" },
            CurrentPriceNo: price.currentPriceNo || { ask: 0, bid: 0, when: "0001-01-01T00:00:00", source: "Unknown" },
            AllTimeHighYes_Ask: price.allTimeHighYes_Ask || { ask: 0, when: "0001-01-01T00:00:00" },
            AllTimeHighYes_Bid: price.allTimeHighYes_Bid || { bid: 0, when: "0001-01-01T00:00:00" },
            AllTimeLowYes_Ask: price.allTimeLowYes_Ask || { ask: 0, when: "0001-01-01T00:00:00" },
            AllTimeLowYes_Bid: price.allTimeLowYes_Bid || { bid: 0, when: "0001-01-01T00:00:00" },
            RecentHighYes_Ask: price.recentHighYes_Ask || { ask: 0, when: "0001-01-01T00:00:00" },
            RecentHighYes_Bid: price.recentHighYes_Bid || { bid: 0, when: "0001-01-01T00:00:00" },
            RecentLowYes_Ask: price.recentLowYes_Ask || { ask: 0, when: "0001-01-01T00:00:00" },
            RecentLowYes_Bid: price.recentLowYes_Bid || { bid: 0, when: "0001-01-01T00:00:00" },
            AllTimeHighNo_Ask: price.allTimeHighNo_Ask || { ask: 0, when: "0001-01-01T00:00:00" },
            AllTimeHighNo_Bid: price.allTimeHighNo_Bid || { bid: 0, when: "0001-01-01T00:00:00" },
            AllTimeLowNo_Ask: price.allTimeLowNo_Ask || { ask: 0, when: "0001-01-01T00:00:00" },
            AllTimeLowNo_Bid: price.allTimeLowNo_Bid || { bid: 0, when: "0001-01-01T00:00:00" },
            RecentHighNo_Ask: price.recentHighNo_Ask || { ask: 0, when: "0001-01-01T00:00:00" },
            RecentHighNo_Bid: price.recentHighNo_Bid || { bid: 0, when: "0001-01-01T00:00:00" },
            RecentLowNo_Ask: price.recentLowNo_Ask || { ask: 0, when: "0001-01-01T00:00:00" },
            RecentLowNo_Bid: price.recentLowNo_Bid || { bid: 0, when: "0001-01-01T00:00:00" },
            GoodBadPriceYes: price.goodBadPriceYes || "--",
            GoodBadPriceNo: price.goodBadPriceNo || "--",
            MarketBehaviorYes: price.marketBehaviorYes || "--",
            MarketBehaviorNo: price.marketBehaviorNo || "--",
            Title: market.title || "--",
            YesSubtitle: market.yesSubtitle || "--",
            NoSubtitle: market.noSubtitle || "--",
            MarketType: market.marketType || "--",
            MarketStatus: market.marketStatus || "--",
            MarketAgeSeconds: market.marketAgeSeconds || 0,
            TimeLeftSeconds: market.timeLeftSeconds || 0,
            CumulativeYesBidDepth: price.cumulativeYesBidDepth || 0,
            CumulativeNoBidDepth: price.cumulativeNoBidDepth || 0,
            YesSpread: latestTicker.yesSpread || 0,
            NoSpread: latestTicker.noSpread || 0,
            YesBidRange: price.yesBidRange || 0,
            NoBidRange: price.noBidRange || 0,
            DepthAtBestYesBid: price.depthAtBestYesBid || 0,
            DepthAtBestNoBid: price.depthAtBestNoBid || 0,
            DepthAtBestYesAsk: price.depthAtBestYesAsk || 0,
            DepthAtBestNoAsk: price.depthAtBestNoAsk || 0,
            TotalYesBidContracts: price.totalYesBidContracts || 0,
            TotalNoBidContracts: price.totalNoBidContracts || 0,
            TotalYesAskContracts: price.totalYesAskContracts || 0,
            TotalNoAskContracts: price.totalNoAskContracts || 0,
            BidImbalance: price.bidImbalance || 0,
            askBidImbalanceVolume: price.askBidImbalanceVolume || 0,
            DepthAtTop4YesBids: price.depthAtTop4YesBids || 0,
            DepthAtTop4NoBids: price.depthAtTop4NoBids || 0,
            DepthAtTop4YesAsks: price.depthAtTop4YesAsks || 0,
            DepthAtTop4NoAsks: price.depthAtTop4NoAsks || 0,
            YesBidCenterOfMass: price.yesBidCenterOfMass || 0,
            NoBidCenterOfMass: price.noBidCenterOfMass || 0,
            YesAskCenterOfMass: price.yesAskCenterOfMass || 0,
            NoAskCenterOfMass: price.noAskCenterOfMass || 0,
            FullImbalance: price.fullImbalance || 0,
            RSI: price.rsi || 0,
            MACD: price.macd || { MACD: 0, Signal: 0, Histogram: 0 },
            EMA: price.ema || 0,
            BollingerBands: price.bollingerBands || { lower: 0, middle: 0, upper: 0 },
            ATR: price.atr || 0,
            VWAP: price.vwap || 0,
            StochasticOscillator: price.stochasticOscillator || { K: 0, D: 0 },
            OBV: price.obv || 0,
            supportResistanceLevels: price.supportResistanceLevels || [],
            VelocityPerMinute_Bottom_YesAsk: price.velocityPerMinute_Bottom_YesAsk || 0,
            LevelCount_Bottom_YesAsk: price.levelCount_Bottom_YesAsk || 0,
            VelocityPerMinute_Bottom_NoAsk: price.velocityPerMinute_Bottom_NoAsk || 0,
            LevelCount_Bottom_NoAsk: price.levelCount_Bottom_NoAsk || 0,
            VelocityPerMinute_Bottom_YesBid: price.velocityPerMinute_Bottom_YesBid || 0,
            LevelCount_Bottom_YesBid: price.levelCount_Bottom_YesBid || 0,
            VelocityPerMinute_Bottom_NoBid: price.velocityPerMinute_Bottom_NoBid || 0,
            LevelCount_Bottom_NoBid: price.levelCount_Bottom_NoBid || 0,
            VelocityPerMinute_Top_YesBid: price.velocityPerMinute_Top_YesBid || 0,
            LevelCount_Top_YesBid: price.levelCount_Top_YesBid || 0,
            VelocityPerMinute_Top_NoBid: price.velocityPerMinute_Top_NoBid || 0,
            LevelCount_Top_NoBid: price.levelCount_Top_NoBid || 0,
            VelocityPerMinute_Top_YesAsk: price.velocityPerMinute_Top_YesAsk || 0,
            LevelCount_Top_YesAsk: price.levelCount_Top_YesAsk || 0,
            VelocityPerMinute_Top_NoAsk: price.velocityPerMinute_Top_NoAsk || 0,
            LevelCount_Top_NoAsk: price.levelCount_Top_NoAsk || 0,
            NonTradeRelatedOrderCount_Yes: price.nonTradeRelatedOrderCount_Yes || 0,
            NonTradeRelatedOrderCount_No: price.nonTradeRelatedOrderCount_No || 0,
            AverageTradeSize_Yes: price.averageTradeSize_Yes || 0,
            AverageTradeSize_No: price.averageTradeSize_No || 0,
            TradeCount_Yes: price.tradeCount_Yes || 0,
            TradeCount_No: price.tradeCount_No || 0,
            OrderRatePerMinute_YesAsk: price.orderRatePerMinute_YesAsk || 0,
            OrderRatePerMinute_YesBid: price.orderRatePerMinute_YesBid || 0,
            OrderRatePerMinute_NoAsk: price.orderRatePerMinute_NoAsk || 0,
            OrderRatePerMinute_NoBid: price.orderRatePerMinute_NoBid || 0,
            tradeVolumePerMinute_Yes: price.tradeVolumePerMinute_Yes || 0,
            tradeVolumePerMinute_No: price.tradeVolumePerMinute_No || 0,
            tradeRatePerMinute_Yes: price.tradeRatePerMinute_Yes || 0,
            tradeRatePerMinute_No: price.tradeRatePerMinute_No || 0,
            highestVolume_Day: price.highestVolume_Day || 0,
            highestVolume_Hour: price.highestVolume_Hour || 0,
            highestVolume_Minute: price.highestVolume_Minute || 0,
            WarningCount: price.warningCount || 0,
            ErrorCount: price.errorCount || 0
        };
    }

    getWarningCount() {
        return this.warningCount;
    }

    getErrorCount() {
        return this.errorCount;
    }

    getLastWebSocketUpdate() {
        return this.lastWebSocketUpdate;
    }

    debugState() {
        console.log("MarketModel State:", {
            candlesticks: this.candlesticks,
            tickers: this.tickers,
            positions: this.positions,
            orderbook: this.orderbook,
            marketInfo: this.marketInfo,
            priceData: this.priceData,
            historicalData: this.historicalData,
            balance: this.balance,
            positionsValue: this.positionsValue,
            lastWebSocketUpdate: this.lastWebSocketUpdate
        });
    }
}

const marketModel = new MarketModel();