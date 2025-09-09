class MarketViewModel {
    constructor(marketModel) {
        this.marketModel = marketModel;
        this.currentMarket = null;
        this.currentTimeframe = "1d";
        this.marketView = null;
        this.isYes = true;
        this.isUpdatingMarket = false;
        this.marketModel.addListener(() => {
            if (this.marketView) this.marketView.update();
        });
        // Call setInitialYesNoState after ensuring data is loaded
        if (this.marketModel.subscribedMarkets.length > 0) {
            this.currentMarket = this.marketModel.subscribedMarkets[0];
            this.refreshData(); // Ensure data is loaded before setting Yes/No state
        }
    }

    setView(marketView) {
        this.marketView = marketView;
    }

    setInitialYesNoState() {
        if (this.currentMarket) {
            const positionSize = this.getPositionSize();
            this.isYes = positionSize >= 0;
            //console.log(`Set initial Yes/No state for ${this.currentMarket}: isYes=${this.isYes}, positionSize=${positionSize}`);
        }
    }

    onModelChanged() {
        if (this.isUpdatingMarket) {
            //console.log("Skipping onModelChanged during market update");
            return;
        }
        //console.log("onModelChanged called, isUpdatingMarket:", this.isUpdatingMarket, "currentMarket:", this.currentMarket);
        //this.marketModel.debugState();

        if (!this.currentMarket && this.marketModel.subscribedMarkets.length > 0) {
            this.currentMarket = this.marketModel.subscribedMarkets[0];
            this.refreshData();
        }
        if (this.currentMarket && !this.marketModel.subscribedMarkets.includes(this.currentMarket)) {
            this.currentMarket = this.marketModel.subscribedMarkets[0] || null;
            if (this.currentMarket) {
                this.refreshData();
            }
        }
        if (this.marketView) {
            //console.log("Updating view from marketViewModel");
            this.marketView.update();
        } else {
            console.warn("View not set, cannot update");
        }
    }

    subscribeToMarket() {
        const ticker = document.getElementById("marketTicker")?.value.trim();
        if (!ticker) {
            console.warn("No market ticker provided for subscription");
            return;
        }
        // Immediately add to subscribed markets and update dropdown
        if (!this.marketModel.subscribedMarkets.includes(ticker)) {
            this.marketModel.subscribedMarkets.push(ticker);
            this.currentMarket = ticker;
            this.isYes = true; // Default to Yes for new market
            this.isUpdatingMarket = true;
            this.marketModel.setLoading(true);
            if (this.marketView) {
                this.marketView.update(); // Update dropdown immediately
            }
        }
        // Initiate subscription
        signalRService.invoke("SubscribeToMarket", ticker);
        // Check if data is loaded
        const checkDataLoaded = () => {
            const interval = this.getIntervalKey(this.currentTimeframe);
            if (this.marketModel.isDataLoaded(ticker, interval)) {
                this.isUpdatingMarket = false;
                this.marketModel.setLoading(false);
                if (this.marketView) {
                    this.marketView.update();
                }
            } else {
                setTimeout(checkDataLoaded, 100);
            }
        };
        checkDataLoaded();
        // Clear the input box
        document.getElementById("marketTicker").value = "";
    }

    unsubscribeFromMarket() {
        const ticker = document.getElementById("marketTicker")?.value.trim();
        if (!ticker) {
            console.warn("No market ticker provided for unsubscription");
            return;
        }
        if (this.marketModel.subscribedMarkets.includes(ticker)) {
            // Immediately remove from subscribed markets and update marketView
            this.marketModel.subscribedMarkets = this.marketModel.subscribedMarkets.filter(m => m !== ticker);
            if (this.currentMarket === ticker) {
                this.currentMarket = this.marketModel.subscribedMarkets[0] || null;
                if (this.currentMarket) {
                    this.refreshData();
                }
            }
            if (this.marketView) {
                this.marketView.update(); // Update dropdown immediately
            }
            signalRService.invoke("UnsubscribeFromMarket", ticker);
        }
        // Clear the input box
        document.getElementById("marketTicker").value = "";
    }

    changeMarket(ticker) {
        //console.log("Changing market to:", ticker);
        ticker = String(ticker).trim();
        if (this.marketModel.subscribedMarkets.includes(ticker)) {
            this.isUpdatingMarket = true;
            this.currentMarket = ticker;
            const positionSize = this.getPositionSize();
            this.isYes = positionSize >= 0;
            this.marketModel.setLoading(true);
            this.refreshData();
            const checkDataLoaded = () => {
                if (!this.marketModel.isLoading && !this.isDataLoaded(ticker, this.getIntervalKey(this.currentTimeframe))) {
                    setTimeout(checkDataLoaded, 100);
                } else {
                    this.isUpdatingMarket = false;
                    this.marketModel.setLoading(false);
                    this.onModelChanged();
                }
            };
            checkDataLoaded();
        } else {
            console.warn(`Market ${ticker} not in subscribed markets`);
        }
    }

    changeTimeframe(timeframe) {
        this.currentTimeframe = timeframe;
        this.refreshData();
    }

    refreshData() {
        if (!this.currentMarket) {
            this.marketModel.setLoading(false);
            this.isUpdatingMarket = false;
            return;
        }
        //console.log("Refreshing data for market:", this.currentMarket, "with timeframe:", this.currentTimeframe);
        this.marketModel.setLoading(true);
        setTimeout(() => {
            this.marketModel.setLoading(false);
            this.isUpdatingMarket = false;
            this.setInitialYesNoState(); // Call setInitialYesNoState after data is loaded
            this.onModelChanged();
        }, 100);
    }

    toggleYesNo(isChecked) {
        this.isYes = isChecked;
        //console.log(`Toggled to ${this.isYes ? "Yes" : "No"}`);
        if (this.marketView) this.marketView.update();
    }

    getChartData() {
        if (!this.currentMarket) return { candlesticks: [], tickers: [], positions: [] };
        const interval = this.getIntervalKey(this.currentTimeframe);
        const historicalData = this.marketModel.candlesticks[interval][this.currentMarket] || [];

        var now = moment();
        now = now.utc();
        let startTime;
        if (this.currentTimeframe === "All") {
            // For "All", use the earliest possible time to include all data
            startTime = moment.utc(0); // Effectively no start filter
        } else {
            startTime = now.clone().subtract(this.getTimeframeDuration(this.currentTimeframe));
        }
        var HasFoundValue = false;
        const candlesticks = historicalData.filter(c => {
            const candlestickTime = moment.utc(c.x);
            const isValid = candlestickTime.isAfter(startTime) && candlestickTime.isBefore(now) && c.x > 0;
            let isNotCurrentSegment = false;

            if (interval === "minute" && this.currentTimeframe !== "All") {
                isNotCurrentSegment = !candlestickTime.isSame(now, "minute");
            } else if (interval === "hour" && this.currentTimeframe !== "All") {
                isNotCurrentSegment = !candlestickTime.isSame(now, "hour");
            } else if (interval === "day" && this.currentTimeframe !== "All") {
                isNotCurrentSegment = !candlestickTime.isSame(now, "day");
            }

            
            if (isValid) {
                HasFoundValue = true;
            }

            if (isValid === false && HasFoundValue === true) {
                console.log(
                    `Now: ${now.format('YYYY-MM-DD HH:mm:ss')} | ` +
                    `UTC Offset: ${now.utcOffset()} minutes | ` +
                    `Is UTC: ${now.isUTC()} | ` +
                    `Format with TZ: ${now.format('YYYY-MM-DD HH:mm:ss Z')} | ` +
                    `Timestamp: ${now.valueOf()}`
                );
                console.log(
                    `Candlestick: ${candlestickTime.format('YYYY-MM-DD HH:mm:ss')} | ` +
                    `UTC Offset: ${candlestickTime.utcOffset()} minutes | ` +
                    `Is UTC: ${candlestickTime.isUTC()} | ` +
                    `Format with TZ: ${candlestickTime.format('YYYY-MM-DD HH:mm:ss Z')} | ` +
                    `Timestamp: ${candlestickTime.valueOf()}`
                );
            }

            const shouldInclude = isValid && (this.currentTimeframe === "All" || isNotCurrentSegment);
            return shouldInclude;
        }).sort((a, b) => a.x - b.x);

        const lastCandlestickTime = candlesticks.length > 0 ? candlesticks[candlesticks.length - 1].x : null;

        const rawTickers = this.marketModel.tickers[this.currentMarket] || [];

        const tickers = rawTickers.filter(t => {
            const tickerTime = moment.utc(t.x);
            const isAfterStart = tickerTime.isAfter(startTime);
            const isBeforeNow = tickerTime.isBefore(now);
            const isPositiveX = t.x > 0;
            const isWithinTimeframe = isAfterStart && isBeforeNow && isPositiveX;
            const isAfterLastCandlestick = lastCandlestickTime ? tickerTime.isAfter(moment.utc(lastCandlestickTime)) : true;
            const isValid = isWithinTimeframe && isAfterLastCandlestick;
            return isValid;
        }).sort((a, b) => a.x - b.x);

        return {
            candlesticks: candlesticks,
            tickers: tickers,
            positions: this.marketModel.positions[this.currentMarket] || []
        };
    }

    getOrderbookData(marketTicker = this.currentMarket) {
        if (!marketTicker) return [];
        return this.marketModel.getOrderbook(marketTicker);
    }

    getMarketList() {
        return this.marketModel.subscribedMarkets || [];
    }

    getIntervalKey(timeframe) {
        return timeframe === "15m" ? "minute" :
            timeframe === "1h" ? "minute" :
                timeframe === "1d" ? "hour" :
                    timeframe === "3d" ? "hour" :
                        timeframe === "1w" ? "day" :
                            timeframe === "1M" ? "day" :
                                timeframe === "All" ? "day" : "hour";
    }

    getTimeUnit(timeframe) {
        return timeframe === "15m" ? "minute" :
            timeframe === "1h" ? "minute" :
                timeframe === "1d" ? "hour" :
                    timeframe === "3d" ? "hour" :
                        timeframe === "1w" ? "day" :
                            timeframe === "1M" ? "day" :
                                timeframe === "All" ? "day" : "hour";
    }

    getTimeframeDuration(timeframe) {
        if (timeframe === "All") {
            // Return a very large duration to effectively include all data
            return moment.duration(100, "years");
        }
        return moment.duration(
            timeframe === "15m" ? 15 : timeframe === "1h" ? 1 : timeframe === "1d" ? 1 :
                timeframe === "3d" ? 3 : timeframe === "1w" ? 7 : timeframe === "1M" ? 30 : 1,
            timeframe === "15m" ? "minutes" : timeframe === "1h" ? "hours" : "days"
        );
    }

    getPositionSize() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).PositionSize : 0; }
    getPositionLabel() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).PositionLabel : "--"; }
    getBuyinPrice() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).BuyinPrice : 0; }
    getPositionUpside() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).PositionUpside : 0; }
    getPositionDownside() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).PositionDownside : 0; }
    getTotalTraded() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).TotalTraded : "--"; }
    getRealizedPnl() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).RealizedPnl : 0; }
    getFeesPaid() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).FeesPaid : 0; }
    getPositionROI() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).PositionROI : 0; }
    getPositionROIAmt() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).PositionROIAmt : 0; }
    getHoldTime() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).HoldTime : "--"; }
    getRestingOrdersCount() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).RestingOrdersCount : 0; }

    getCurrentPrice(marketTicker = this.currentMarket) {
        if (!marketTicker || !this.marketModel.tickers[marketTicker]?.length) {
            console.log(`No tickers available for ${marketTicker}`);
            return { ask: 0, bid: 0, when: "0001-01-01T00:00:00", source: "Unknown" };
        }
        const latestTicker = this.marketModel.tickers[marketTicker][this.marketModel.tickers[marketTicker].length - 1];

        // Find the ticker with the maximum timestamp
        const maxTimestampTicker = this.marketModel.tickers[marketTicker].reduce((latest, current) =>
            current.x > latest.x ? current : latest
        );

        console.log(`Selected ticker for ${marketTicker}:`, {
            selected: {
                timestamp: latestTicker.timestamp,
                x: latestTicker.x,
                yesAsk: latestTicker.yesAsk,
                yesBid: latestTicker.yesBid
            },
            maxTimestamp: {
                timestamp: maxTimestampTicker.timestamp,
                x: maxTimestampTicker.x,
                yesAsk: maxTimestampTicker.yesAsk,
                yesBid: maxTimestampTicker.yesBid
            },
            isLatest: latestTicker.x === maxTimestampTicker.x
        });

        const yesAsk = latestTicker.yesAsk;
        const yesBid = latestTicker.yesBid;
        const timestamp = latestTicker.timestamp;
        const source = latestTicker.source;
        const noAsk = 100 - yesBid;
        const noBid = 100 - yesAsk;
        return this.isYes ? { ask: yesAsk, bid: yesBid, when: timestamp, source: source } :
            { ask: noAsk, bid: noBid, when: timestamp, source: source };
    }

    getWarningCount() {
        return this.marketModel.getWarningCount();
    }

    getErrorCount() {
        return this.marketModel.getErrorCount();
    }

    getOrderbookLastUpdated() {
        if (!this.currentMarket) return null;
        return this.marketModel.orderbookLastUpdated[this.currentMarket] || null;
    }

    getAllTimeHigh() {
        const data = this.marketModel.getMarketData(this.currentMarket);
        return this.isYes ?
            { ask: data.AllTimeHighYes_Ask.ask, askWhen: data.AllTimeHighYes_Ask.when, bid: data.AllTimeHighYes_Bid.bid, bidWhen: data.AllTimeHighYes_Bid.when } :
            { ask: data.AllTimeHighNo_Ask.ask, askWhen: data.AllTimeHighNo_Ask.when, bid: data.AllTimeHighNo_Bid.bid, bidWhen: data.AllTimeHighNo_Bid.when };
    }

    getAllTimeLow() {
        const data = this.marketModel.getMarketData(this.currentMarket);
        return this.isYes ?
            { ask: data.AllTimeLowYes_Ask.ask, askWhen: data.AllTimeLowYes_Ask.when, bid: data.AllTimeLowYes_Bid.bid, bidWhen: data.AllTimeLowYes_Bid.when } :
            { ask: data.AllTimeLowNo_Ask.ask, askWhen: data.AllTimeLowNo_Ask.when, bid: data.AllTimeLowNo_Bid.bid, bidWhen: data.AllTimeLowNo_Bid.when };
    }

    getRecentHigh() {
        const data = this.marketModel.getMarketData(this.currentMarket);
        return this.isYes ?
            { ask: data.RecentHighYes_Ask.ask, askWhen: data.RecentHighYes_Ask.when, bid: data.RecentHighYes_Bid.bid, bidWhen: data.RecentHighYes_Bid.when } :
            { ask: data.RecentHighNo_Ask.ask, askWhen: data.RecentHighNo_Ask.when, bid: data.RecentHighNo_Bid.bid, bidWhen: data.RecentHighNo_Bid.when };
    }

    getRecentLow() {
        const data = this.marketModel.getMarketData(this.currentMarket);
        return this.isYes ?
            { ask: data.RecentLowYes_Ask.ask, askWhen: data.RecentLowYes_Ask.when, bid: data.RecentLowYes_Bid.bid, bidWhen: data.RecentLowYes_Bid.when } :
            { ask: data.RecentLowNo_Ask.ask, askWhen: data.RecentLowNo_Ask.when, bid: data.RecentLowNo_Bid.bid, bidWhen: data.RecentLowNo_Bid.when };
    }

    getHighestVolumeDay(ticker = this.currentMarket) {
        return this.marketModel.getMarketData(ticker).HighestVolume_Day;
    }

    getHighestVolumeHour(ticker = this.currentMarket) {
        return this.marketModel.getMarketData(ticker).HighestVolume_Hour;
    }

    getHighestVolumeMinute(ticker = this.currentMarket) {
        return this.marketModel.getMarketData(ticker).HighestVolume_Minute;
    }

    getGoodBadPriceYes() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).GoodBadPriceYes : "--"; }
    getGoodBadPriceNo() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).GoodBadPriceNo : "--"; }
    getMarketBehaviorYes() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).MarketBehaviorYes : "--"; }
    getMarketBehaviorNo() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).MarketBehaviorNo : "--"; }
    getMarketType() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).MarketType : "--"; }
    getMarketStatus() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).MarketStatus : "--"; }
    getMarketAge() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).MarketAgeSeconds : 0; }
    getTimeLeft() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).TimeLeftSeconds : 0; }
    getBalance() { return this.marketModel.balance; }
    getPositionsValue() { return this.marketModel.positionsValue; }
    isDataLoaded(marketTicker, interval) { return this.marketModel.isDataLoaded(marketTicker, interval); }
    getMarketTitle(ticker = this.currentMarket) {
        return ticker ? this.marketModel.getMarketData(ticker).Title : "--";
    }
    getMarketSubtitle(ticker = this.currentMarket) {
        return this.isYes ? this.marketModel.getMarketData(ticker).YesSubtitle : this.marketModel.getMarketData(ticker).NoSubtitle;
    }
    getTradeVolumePerMinute_Yes(ticker = this.currentMarket) {
        return this.marketModel.getMarketData(ticker).tradeVolumePerMinute_Yes;
    }
    getTradeVolumePerMinute_No(ticker = this.currentMarket) {
        return this.marketModel.getMarketData(ticker).tradeVolumePerMinute_No;
    }
    getTradeRatePerMinute_Yes(ticker = this.currentMarket) {
        return this.marketModel.getMarketData(ticker).tradeRatePerMinute_Yes;
    }
    getTradeRatePerMinute_No(ticker = this.currentMarket) {
        return this.marketModel.getMarketData(ticker).tradeRatePerMinute_No;
    }
    NonTradeRelatedOrderCount_Yes(ticker = this.currentMarket) {
        return this.marketModel.getMarketData(ticker).NonTradeRelatedOrderCount_Yes;
    }
    NonTradeRelatedOrderCount_No(ticker = this.currentMarket) {
        return this.marketModel.getMarketData(ticker).NonTradeRelatedOrderCount_No;
    }
    getDepthAtBestYesBid() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).DepthAtBestYesBid : 0; }
    getDepthAtBestNoBid() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).DepthAtBestNoBid : 0; }
    getDepthAtBestYesAsk() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).DepthAtBestYesAsk : 0; }
    getDepthAtBestNoAsk() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).DepthAtBestNoAsk : 0; }
    getTotalYesBidContracts() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).TotalYesBidContracts : 0; }
    getTotalNoBidContracts() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).TotalNoBidContracts : 0; }
    getTotalYesAskContracts() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).TotalYesAskContracts : 0; }
    getTotalNoAskContracts() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).TotalNoAskContracts : 0; }
    getBidImbalance() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).BidImbalance : 0; }
    getaskBidImbalanceVolume() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).askBidImbalanceVolume : 0; }
    getDepthAtTop4YesBids() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).DepthAtTop4YesBids : 0; }
    getDepthAtTop4NoBids() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).DepthAtTop4NoBids : 0; }
    getDepthAtTop4YesAsks() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).DepthAtTop4YesAsks : 0; }
    getDepthAtTop4NoAsks() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).DepthAtTop4NoAsks : 0; }
    getYesBidCenterOfMass() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).YesBidCenterOfMass : 0; }
    getNoBidCenterOfMass() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).NoBidCenterOfMass : 0; }
    getYesAskCenterOfMass() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).YesAskCenterOfMass : 0; }
    getNoAskCenterOfMass() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).NoAskCenterOfMass : 0; }
    getFullImbalance() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).FullImbalance : 0; }
    getYesContractsRatePerMinute() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).YesContractsRatePerMinute : 0; }
    getNoContractsRatePerMinute() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).NoContractsRatePerMinute : 0; }
    getRSI() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).RSI : 0; }
    getMACD() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).MACD : { MACD: 0, Signal: 0, Histogram: 0 }; }
    getEMA() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).EMA : 0; }
    getBollingerBands() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).BollingerBands : { lower: 0, middle: 0, upper: 0 }; }
    getATR() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).ATR : 0; }
    getVWAP() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).VWAP : 0; }
    getStochasticOscillator() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).StochasticOscillator : { K: 0, D: 0 }; }
    getOBV() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).OBV : 0; }

    getOrderRatePerMinute_YesAsk() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).OrderRatePerMinute_YesAsk : 0; }
    getOrderRatePerMinute_YesBid() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).OrderRatePerMinute_YesBid : 0; }
    getOrderRatePerMinute_NoAsk() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).OrderRatePerMinute_NoAsk : 0; }
    getOrderRatePerMinute_NoBid() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).OrderRatePerMinute_NoBid : 0; }

    getCumulativeDepth() {
        const data = this.marketModel.getMarketData(this.currentMarket);
        return this.isYes ?
            { low: data.CumulativeYesBidDepth, recentLow: data.CumulativeYesBidDepth, current: data.CumulativeYesBidDepth, recentHigh: data.CumulativeYesBidDepth, high: data.CumulativeYesBidDepth } :
            { low: data.CumulativeNoBidDepth, recentLow: data.CumulativeNoBidDepth, current: data.CumulativeNoBidDepth, recentHigh: data.CumulativeNoBidDepth, high: data.CumulativeNoBidDepth };
    }

    getSpread() {
        if (!this.currentMarket || !this.marketModel.tickers[this.currentMarket]?.length) {
            return { low: 0, recentLow: 0, current: 0, recentHigh: 0, high: 0 };
        }
        const latestTicker = this.marketModel.tickers[this.currentMarket][this.marketModel.tickers[this.currentMarket].length - 1];
        const spread = this.isYes ? latestTicker.yesSpread : latestTicker.noSpread;
        return { low: spread, recentLow: spread, current: spread, recentHigh: spread, high: spread };
    }

    getBidRange() {
        const data = this.marketModel.getMarketData(this.currentMarket);
        return this.isYes ?
            { low: data.YesBidRange, recentLow: data.YesBidRange, current: data.YesBidRange, recentHigh: data.YesBidRange, high: data.YesBidRange } :
            { low: data.NoBidRange, recentLow: data.NoBidRange, current: data.NoBidRange, recentHigh: data.NoBidRange, high: data.NoBidRange };
    }

    getAverageTradeSizeYes() {
        const data = this.marketModel.getMarketData(this.currentMarket);
        return data.AverageTradeSize_Yes || 0;
    }
    getAverageTradeSizeNo() {
        const data = this.marketModel.getMarketData(this.currentMarket);
        return data.AverageTradeSize_No || 0;
    }
    getTradeCount() {
        const data = this.marketModel.getMarketData(this.currentMarket);
        return {
            yes: data.TradeCount_Yes || 0,
            no: data.TradeCount_No || 0
        };
    }

    getVelocityPerMinute_Bottom_YesAsk() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).VelocityPerMinute_Bottom_YesAsk : 0; }
    getLevelCount_Bottom_YesAsk() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).LevelCount_Bottom_YesAsk : 0; }
    getVelocityPerMinute_Bottom_YesBid() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).VelocityPerMinute_Bottom_YesBid : 0; }
    getLevelCount_Bottom_YesBid() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).LevelCount_Bottom_YesBid : 0; }
    getVelocityPerMinute_Bottom_NoAsk() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).VelocityPerMinute_Bottom_NoAsk : 0; }
    getLevelCount_Bottom_NoAsk() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).LevelCount_Bottom_NoAsk : 0; }
    getVelocityPerMinute_Bottom_NoBid() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).VelocityPerMinute_Bottom_NoBid : 0; }
    getLevelCount_Bottom_NoBid() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).LevelCount_Bottom_NoBid : 0; }

    getVelocityPerMinute_Top_YesBid() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).VelocityPerMinute_Top_YesBid : 0; }
    getLevelCount_Top_YesBid() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).LevelCount_Top_YesBid : 0; }
    getVelocityPerMinute_Top_NoBid() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).VelocityPerMinute_Top_NoBid : 0; }
    getLevelCount_Top_NoBid() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).LevelCount_Top_NoBid : 0; }
    getVelocityPerMinute_Top_YesAsk() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).VelocityPerMinute_Top_YesAsk : 0; }
    getLevelCount_Top_YesAsk() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).LevelCount_Top_YesAsk : 0; }
    getVelocityPerMinute_Top_NoAsk() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).VelocityPerMinute_Top_NoAsk : 0; }
    getLevelCount_Top_NoAsk() { return this.currentMarket ? this.marketModel.getMarketData(this.currentMarket).LevelCount_Top_NoAsk : 0; }

    getBottomVelocity() {
        const data = this.marketModel.getMarketData(this.currentMarket);
        return this.isYes ? {
            ask: data.VelocityPerMinute_Bottom_YesAsk || 0,
            askLevels: data.LevelCount_Bottom_YesAsk || 0,
            bid: data.VelocityPerMinute_Bottom_YesBid || 0,
            bidLevels: data.LevelCount_Bottom_YesBid || 0
        } : {
            ask: data.VelocityPerMinute_Bottom_NoAsk || 0,
            askLevels: data.LevelCount_Bottom_NoAsk || 0,
            bid: data.VelocityPerMinute_Bottom_NoBid || 0,
            bidLevels: data.LevelCount_Bottom_NoBid || 0
        };
    }

    getTopVelocity() {
        const data = this.marketModel.getMarketData(this.currentMarket);
        return this.isYes ? {
            ask: data.VelocityPerMinute_Top_YesAsk || 0,
            askLevels: data.LevelCount_Top_YesAsk || 0,
            bid: data.VelocityPerMinute_Top_YesBid || 0,
            bidLevels: data.LevelCount_Top_YesBid || 0
        } : {
            ask: data.VelocityPerMinute_Top_NoAsk || 0,
            askLevels: data.LevelCount_Top_NoAsk || 0,
            bid: data.VelocityPerMinute_Top_NoBid || 0,
            bidLevels: data.LevelCount_Top_NoBid || 0
        };
    }

    getAverageTradeSize() {
        const data = this.marketModel.getMarketData(this.currentMarket);
        return this.isYes ?
            {
                ask: data.YesAskAverageTradeSize || 0,
                bid: data.YesBidAverageTradeSize || 0
            } :
            {
                ask: data.NoAskAverageTradeSize || 0,
                bid: data.NoBidAverageTradeSize || 0
            };
    }

    getDepthAtBestAsk() {
        return this.isYes ? this.getDepthAtBestYesAsk() : this.getDepthAtBestNoAsk();
    }

    getTotalAskContracts() {
        return this.isYes ? this.getTotalYesAskContracts() : this.getTotalNoAskContracts();
    }

    getDepthAtTop4Asks() {
        return this.isYes ? this.getDepthAtTop4YesAsks() : this.getDepthAtTop4NoAsks();
    }

    getTopBidVelocityPerMinute() {
        return this.isYes ? this.getVelocityPerMinute_Top_YesBid() : this.getVelocityPerMinute_Top_NoBid();
    }

    getaskVelocityTopPerMinute() {
        return this.isYes ? this.getVelocityPerMinute_Top_YesAsk() : this.getVelocityPerMinute_Top_NoAsk();
    }

}