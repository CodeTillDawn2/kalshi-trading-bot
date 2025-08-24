class MarketView {
    constructor(marketViewModel) {
        this.marketViewModel = marketViewModel;
        this.elements = {};
        this.initializeElements();
        this.chart = null;
        this.touchStartX = 0;
        this.touchEndX = 0;
        this.supportResistanceDatasets = [];
        this.lastSupportResistanceLevels = [];
        this.initializeChart();
        this.setupEventListeners();
        this.update();
        setInterval(() => this.update(), 60000);
        this.startTimeSinceUpdates();
        window.addEventListener('resize', () => {
            if (this.chart) this.chart.resize();
        });
        this.setupNavigationListeners();
    }

    static dataBindings = {
        marketTickerInput: { selector: '#marketTicker', type: 'input' },
        marketSelector: { selector: '#marketSelector', type: 'select' },
        cash: {
            selector: '.data-field[data-key="cash"]',
            format: value => value === 0 || value === undefined || value === null ? '$0.00' : `$${Number(value).toFixed(2)}`,
            getValue: vm => vm.getBalance()
        },
        exchangeStatus: {
            selector: '.data-field[data-key="exchangeStatus"]',
            format: value => value === undefined || value === null ? '--' : (value ? 'Open' : 'Closed'),
            getValue: vm => vm.marketModel.exchangeStatus
        },
        tradingStatus: {
            selector: '.data-field[data-key="tradingStatus"]',
            format: value => value === undefined || value === null ? '--' : (value ? 'Open' : 'Closed'),
            getValue: vm => vm.marketModel.tradingStatus
        },
        positionsCount: {
            selector: '.data-field[data-key="positionsCount"]',
            format: value => value === 0 || value === undefined || value === null ? '$0.00' : `$${Number(value).toFixed(2)}`,
            getValue: vm => vm.getPositionsValue()
        },
        lastWebSocketEvent: {
            selector: '.data-field[data-key="lastWebSocketEvent"]',
            format: value => value ? moment.utc(value).local().fromNow() : '--',
            getValue: vm => vm.marketModel.getLastWebSocketUpdate(),
            attributes: { title: (vm, marketView) => marketView.formatWhen(vm.marketModel.getLastWebSocketUpdate()) }
        },
        response: { selector: '#response', type: 'div' },
        currentMarketChart: {
            selector: '.data-field[data-key="currentMarketChart"]',
            format: value => value || '',
            getValue: vm => vm.currentMarket
        },
        timeframeSelector: { selector: '#timeframeSelector', type: 'select' },
        loadingOverlay: { selector: '#loadingOverlay', type: 'div' },
        priceAsk: {
            selector: '.price-field[data-key="priceAsk"]',
            format: value => value !== undefined && value !== null ? `${Number(value).toFixed(0)}¢` : '--',
            getValue: vm => vm.getCurrentPrice().ask,
            attributes: { title: (vm, marketView) => marketView.formatWhen(vm.getCurrentPrice().when) }
        },
        priceBid: {
            selector: '.price-field[data-key="priceBid"]',
            format: value => value !== undefined && value !== null ? `${Number(value).toFixed(0)}¢` : '--',
            getValue: vm => vm.getCurrentPrice().bid,
            attributes: { title: (vm, marketView) => marketView.formatWhen(vm.getCurrentPrice().when) }
        },
        allTimeHighAsk: {
            selector: '.price-field[data-key="allTimeHighAsk"]',
            format: value => value !== undefined && value !== null ? `${Number(value).toFixed(0)}¢` : '--',
            getValue: vm => vm.getAllTimeHigh().ask,
            attributes: { title: (vm, marketView) => marketView.formatWhen(vm.getAllTimeHigh().askWhen) }
        },
        allTimeHighAskTime: {
            selector: '.price-field[data-key="allTimeHighAskTime"]',
            format: value => value ? moment.utc(value).local().fromNow() : '',
            getValue: vm => vm.getAllTimeHigh().askWhen,
            attributes: { title: (vm, marketView) => marketView.formatWhen(vm.getAllTimeHigh().askWhen) }
        },
        allTimeHighBid: {
            selector: '.price-field[data-key="allTimeHighBid"]',
            format: value => value !== undefined && value !== null ? `${Number(value).toFixed(0)}¢` : '--',
            getValue: vm => vm.getAllTimeHigh().bid,
            attributes: { title: (vm, marketView) => marketView.formatWhen(vm.getAllTimeHigh().bidWhen) }
        },
        allTimeHighBidTime: {
            selector: '.price-field[data-key="allTimeHighBidTime"]',
            format: value => value ? moment.utc(value).local().fromNow() : '',
            getValue: vm => vm.getAllTimeHigh().bidWhen,
            attributes: { title: (vm, marketView) => marketView.formatWhen(vm.getAllTimeHigh().bidWhen) }
        },
        allTimeLowAsk: {
            selector: '.price-field[data-key="allTimeLowAsk"]',
            format: value => value !== undefined && value !== null ? `${Number(value).toFixed(0)}¢` : '--',
            getValue: vm => vm.getAllTimeLow().ask,
            attributes: { title: (vm, marketView) => marketView.formatWhen(vm.getAllTimeLow().askWhen) }
        },
        allTimeLowAskTime: {
            selector: '.price-field[data-key="allTimeLowAskTime"]',
            format: value => value ? moment.utc(value).local().fromNow() : '',
            getValue: vm => vm.getAllTimeLow().askWhen,
            attributes: { title: (vm, marketView) => marketView.formatWhen(vm.getAllTimeLow().askWhen) }
        },
        allTimeLowBid: {
            selector: '.price-field[data-key="allTimeLowBid"]',
            format: value => value !== undefined && value !== null ? `${Number(value).toFixed(0)}¢` : '--',
            getValue: vm => vm.getAllTimeLow().bid,
            attributes: { title: (vm, marketView) => marketView.formatWhen(vm.getAllTimeLow().bidWhen) }
        },
        allTimeLowBidTime: {
            selector: '.price-field[data-key="allTimeLowBidTime"]',
            format: value => value ? moment.utc(value).local().fromNow() : '',
            getValue: vm => vm.getAllTimeLow().bidWhen,
            attributes: { title: (vm, marketView) => marketView.formatWhen(vm.getAllTimeLow().bidWhen) }
        },
        recentHighAsk: {
            selector: '.price-field[data-key="recentHighAsk"]',
            format: value => value !== undefined && value !== null ? `${Number(value).toFixed(0)}¢` : '--',
            getValue: vm => vm.getRecentHigh().ask,
            attributes: { title: (vm, marketView) => marketView.formatWhen(vm.getRecentHigh().askWhen) }
        },
        recentHighAskTime: {
            selector: '.price-field[data-key="recentHighAskTime"]',
            format: value => value ? moment.utc(value).local().fromNow() : '',
            getValue: vm => vm.getRecentHigh().askWhen,
            attributes: { title: (vm, marketView) => marketView.formatWhen(vm.getRecentHigh().askWhen) }
        },
        recentHighBid: {
            selector: '.price-field[data-key="recentHighBid"]',
            format: value => value !== undefined && value !== null ? `${Number(value).toFixed(0)}¢` : '--',
            getValue: vm => vm.getRecentHigh().bid,
            attributes: { title: (vm, marketView) => marketView.formatWhen(vm.getRecentHigh().bidWhen) }
        },
        recentHighBidTime: {
            selector: '.price-field[data-key="recentHighBidTime"]',
            format: value => value ? moment.utc(value).local().fromNow() : '',
            getValue: vm => vm.getRecentHigh().bidWhen,
            attributes: { title: (vm, marketView) => marketView.formatWhen(vm.getRecentHigh().bidWhen) }
        },
        recentLowAsk: {
            selector: '.price-field[data-key="recentLowAsk"]',
            format: value => value !== undefined && value !== null ? `${Number(value).toFixed(0)}¢` : '--',
            getValue: vm => vm.getRecentLow().ask,
            attributes: { title: (vm, marketView) => marketView.formatWhen(vm.getRecentLow().askWhen) }
        },
        recentLowAskTime: {
            selector: '.price-field[data-key="recentLowAskTime"]',
            format: value => value ? moment.utc(value).local().fromNow() : '',
            getValue: vm => vm.getRecentLow().askWhen,
            attributes: { title: (vm, marketView) => marketView.formatWhen(vm.getRecentHigh().askWhen) }
        },
        recentLowBid: {
            selector: '.price-field[data-key="recentLowBid"]',
            format: value => value !== undefined && value !== null ? `${Number(value).toFixed(0)}¢` : '--',
            getValue: vm => vm.getRecentLow().bid,
            attributes: { title: (vm, marketView) => marketView.formatWhen(vm.getRecentLow().bidWhen) }
        },
        recentLowBidTime: {
            selector: '.price-field[data-key="recentLowBidTime"]',
            format: value => value ? moment.utc(value).local().fromNow() : '',
            getValue: vm => vm.getRecentLow().bidWhen,
            attributes: { title: (vm, marketView) => marketView.formatWhen(vm.getRecentLow().bidWhen) }
        },
        marketTitle: {
            selector: '.data-field[data-key="marketTitle"]',
            format: value => value || '--',
            getValue: vm => vm.getMarketTitle(),
            attributes: { title: (vm) => vm.getMarketTitle() || '--' }
        },
        marketSubtitle: {
            selector: '.data-field[data-key="marketSubtitle"]',
            format: value => value || '--',
            getValue: vm => vm.getMarketSubtitle(),
            attributes: { title: (vm) => vm.getMarketSubtitle() || '--' }
        },
        marketAge: {
            selector: '.data-field[data-key="marketAge"]',
            format: value => value <= 0 || value === undefined || value === null ? '--' : `${Math.floor(value / (24 * 3600))}d ${Math.floor((value % (24 * 3600)) / 3600)}h`,
            getValue: vm => vm.getMarketAge(),
            attributes: {
                title: (vm, marketView) => {
                    const marketData = vm.marketModel.getMarketData(vm.currentMarket);
                    const openTime = marketData.MarketInfo?.open_time;
                    if (!openTime || openTime === "0001-01-01T00:00:00") return '--';
                    return moment.utc(openTime).local().format('MMM D, h:mm a');
                }
            }
        },
        // In marketView.js, within the static dataBindings object
        marketTimeLeft: {
            selector: '.data-field[data-key="marketTimeLeft"]',
            format: (value, vm) => {
                const marketData = vm.marketModel.getMarketData(vm.currentMarket);
                const closeTime = marketData.MarketInfo?.close_time;
                const marketStatus = marketData.MarketStatus || "--";
                if (closeTime && moment.utc(closeTime).isBefore(moment.utc())) {
                    // Market has ended; display status capitalized
                    return marketStatus.charAt(0).toUpperCase() + marketStatus.slice(1);
                }
                // Market is active; display time left
                return value <= 0 || value === undefined || value === null
                    ? "--"
                    : `${Math.floor(value / (24 * 3600))}d ${Math.floor((value % (24 * 3600)) / 3600)}h`;
            },
            getValue: vm => vm.getTimeLeft(),
            attributes: {
                title: (vm, marketView) => {
                    const marketData = vm.marketModel.getMarketData(vm.currentMarket);
                    const closeTime = marketData.MarketInfo?.close_time;
                    const marketStatus = marketData.MarketStatus || "--";
                    if (!closeTime || closeTime === "0001-01-01T00:00:00" || marketData.MarketStatus === "closed") {
                        return marketStatus.charAt(0).toUpperCase() + marketStatus.slice(1);
                    }
                    return moment.utc(closeTime).local().format("MMM D, h:mm a");
                }
            }
        },
        priceGoodBad: {
            selector: '.data-field[data-key="priceGoodBad"]',
            format: value => value || '--',
            getValue: vm => vm.isYes ? vm.getGoodBadPriceYes() : vm.getGoodBadPriceNo()
        },
        marketBehavior: {
            selector: '.data-field[data-key="marketBehavior"]',
            format: value => value || '--',
            getValue: vm => vm.isYes ? vm.getMarketBehaviorYes() : vm.getMarketBehaviorNo()
        },
        orderbookLastUpdated: {
            selector: '#orderbookLastUpdated',
            format: value => value ? `Updated ${moment.utc(value).local().fromNow()}` : 'Updated --',
            getValue: vm => vm.getOrderbookLastUpdated(),
            attributes: { title: (vm, marketView) => marketView.formatWhen(vm.getOrderbookLastUpdated()) }
        },
        currentMarketPositions: {
            selector: '.data-field[data-key="currentMarketPositions"]',
            format: value => value || '',
            getValue: vm => vm.currentMarket
        },
        positionSize: {
            selector: '.data-field[data-key="positionSize"]',
            format: (value, vm) => value !== undefined && value !== null ? `${Math.abs(Number(value))} <span style="color: ${vm.getPositionLabel() === 'Yes' ? getComputedStyle(document.documentElement).getPropertyValue('--ask-color').trim() : getComputedStyle(document.documentElement).getPropertyValue('--bid-color').trim()}">${vm.getPositionLabel()}</span>` : '--',
            getValue: vm => vm.getPositionSize()
        },
        positionROI: {
            selector: '.data-field[data-key="positionROI"]',
            format: (value, vm) => value !== undefined && value !== null ? `<span class="${value >= 0 ? 'position-roi-profit' : 'position-roi-loss'}" title="${vm.getPositionROIAmt().toLocaleString('en-US', { style: 'currency', currency: 'USD' })}">${value >= 0 ? `${Math.abs(Number(value)).toFixed(2)}% profit` : `${Math.abs(Number(value)).toFixed(2)}% loss`}</span>` : '--',
            getValue: vm => vm.getPositionROI()
        },
        positionUpside: {
            selector: '.data-field[data-key="positionUpside"]',
            format: value => value !== undefined && value !== null ? `$${Number(value) < 0 ? `-${Math.abs(Number(value)).toFixed(2)}` : Number(value).toFixed(2)}` : '--',
            getValue: vm => vm.getPositionUpside()
        },
        holdTime: {
            selector: '.data-field[data-key="holdTime"]',
            format: value => value !== '--' && value !== undefined && value !== null ? value : '--',
            getValue: vm => vm.getHoldTime(),
            attributes: {
                title: (vm, marketView) => {
                    const marketData = vm.marketModel.getMarketData(vm.currentMarket);
                    const lastUpdatedTs = marketData.Positions?.[0]?.lastUpdatedTs;
                    if (!lastUpdatedTs || lastUpdatedTs === "0001-01-01T00:00:00" || !marketData.Positions?.[0]?.position) return '--';
                    return moment.utc(lastUpdatedTs).local().format('MMM D, h:mm a');
                }
            }
        },
        buyinPrice: {
            selector: '.data-field[data-key="buyinPrice"]',
            format: value => value !== 0 && value !== undefined && value !== null ? `${Math.abs(Number(value)).toFixed(2)}¢` : '--',
            getValue: vm => vm.getBuyinPrice()
        },
        positionDownside: {
            selector: '.data-field[data-key="positionDownside"]',
            format: value => value !== undefined && value !== null ? `$${Number(value) < 0 ? `-${Math.abs(Number(value)).toFixed(2)}` : `+${Number(value).toFixed(2)}`}` : '--',
            getValue: vm => vm.getPositionDownside()
        },
        restingOrdersCount: {
            selector: '.data-field[data-key="restingOrdersCount"]',
            format: value => value !== undefined && value !== null ? Number(value).toString() : '--',
            getValue: vm => vm.getRestingOrdersCount()
        },
        yesNoToggle: { selector: '#yesNoToggle', type: 'input' },
        toggleLabel: {
            selector: '.data-field[data-key="toggleLabel"]',
            format: value => value ? 'Yes' : 'No',
            getValue: vm => vm.isYes
        },
        orderbookBody: { selector: '#orderbookBody', type: 'tbody' },
        spreadCurrent: {
            selector: '.data-field[data-key="spreadCurrent"]',
            format: value => value === 0 || value === undefined || value === null ? '--' : Number(value).toFixed(0),
            getValue: vm => vm.getSpread().current
        },
        askBidImbalanceVolume: {
            selector: '.data-field[data-key="askBidImbalanceVolume"]',
            format: (value, vm) => {
                if (value === 0 || value === undefined || value === null) return '--';
                const totalAsk = vm.isYes ? vm.getTotalYesAskContracts() : vm.getTotalNoAskContracts();
                const totalBid = vm.isYes ? vm.getTotalYesBidContracts() : vm.getTotalNoBidContracts();
                if (totalAsk === 0 && totalBid === 0) return '--';
                const ratio = totalAsk > totalBid ? (totalAsk / (totalBid || 1)) : (totalBid / (totalAsk || 1));
                return Number(ratio).toFixed(2);
            },
            getValue: vm => vm.getaskBidImbalanceVolume()
        },
        depthAtTop4Bids: {
            selector: '.data-field[data-key="depthAtTop4Bids"]',
            format: value => value === 0 || value === undefined || value === null ? '--' : Number(value).toFixed(0),
            getValue: vm => vm.isYes ? vm.getDepthAtTop4YesBids() : vm.getDepthAtTop4NoBids()
        },
        depthAtTop4Asks: {
            selector: '.data-field[data-key="depthAtTop4Asks"]',
            format: value => value === 0 || value === undefined || value === null ? '--' : Number(value).toFixed(0),
            getValue: vm => vm.getDepthAtTop4Asks()
        },
        bidCenterOfMass: {
            selector: '.data-field[data-key="bidCenterOfMass"]',
            format: value => value === 0 || value === undefined || value === null ? '--' : Number(value).toFixed(0),
            getValue: vm => vm.isYes ? vm.getYesBidCenterOfMass() : vm.getNoBidCenterOfMass()
        },
        askCenterOfMass: {
            selector: '.data-field[data-key="askCenterOfMass"]',
            format: value => value === 0 || value === undefined || value === null ? '--' : Number(value).toFixed(0),
            getValue: vm => vm.isYes ? vm.getYesAskCenterOfMass() : vm.getNoAskCenterOfMass()
        },
        totalBidContracts: {
            selector: '.data-field[data-key="totalBidContracts"]',
            format: value => value === 0 || value === undefined || value === null ? '--' : (value >= 1000000 ? `~${(Number(value) / 1000000).toFixed(1)}M` : value >= 1000 ? `~${(Number(value) / 1000).toFixed(0)}k` : Number(value).toFixed(0)),
            getValue: vm => vm.isYes ? vm.getTotalYesBidContracts() : vm.getTotalNoBidContracts()
        },
        totalAskContracts: {
            selector: '.data-field[data-key="totalAskContracts"]',
            format: value => value === 0 || value === undefined || value === null ? '--' : (value >= 1000000 ? `~${(Number(value) / 1000000).toFixed(1)}M` : value >= 1000 ? `~${(Number(value) / 1000).toFixed(0)}k` : Number(value).toFixed(0)),
            getValue: vm => vm.getTotalAskContracts()
        },
        rsi: {
            selector: '.data-field[data-key="rsi"]',
            format: value => value === 0 || value === undefined || value === null ? '--' : Number(value).toFixed(2),
            getValue: vm => vm.getRSI()
        },
        macd: {
            selector: '.data-field[data-key="macd"]',
            format: value => {
                const macd = Number(value?.macd ?? value?.MACD ?? 0);
                const signal = Number(value?.signal ?? value?.Signal ?? 0);
                const histogram = Number(value?.histogram ?? value?.Histogram ?? 0);
                return (macd === 0 && signal === 0 && histogram === 0) || value === undefined || value === null ? '--' : `${macd.toFixed(2)}, ${signal.toFixed(2)}, ${histogram.toFixed(2)}`;
            },
            getValue: vm => vm.getMACD()
        },
        ema: {
            selector: '.data-field[data-key="ema"]',
            format: value => (value ?? 0) === 0 || value === undefined || value === null ? '--' : Number(value).toFixed(2),
            getValue: vm => vm.getEMA()
        },
        bollingerBands: {
            selector: '.data-field[data-key="bollingerBands"]',
            format: value => {
                const lower = Number(value?.lower ?? 0);
                const middle = Number(value?.middle ?? 0);
                const upper = Number(value?.upper ?? 0);
                return (lower === 0 && middle === 0 && upper === 0) || value === undefined || value === null ? '--' : `${lower.toFixed(2)}, ${middle.toFixed(2)}, ${upper.toFixed(2)}`;
            },
            getValue: vm => vm.getBollingerBands()
        },
        atr: {
            selector: '.data-field[data-key="atr"]',
            format: value => value === 0 || value === undefined || value === null ? '--' : Number(value).toFixed(2),
            getValue: vm => vm.getATR()
        },
        vwap: {
            selector: '.data-field[data-key="vwap"]',
            format: value => value === 0 || value === undefined || value === null ? '--' : Number(value).toFixed(2),
            getValue: vm => vm.getVWAP()
        },
        stochasticOscillator: {
            selector: '.data-field[data-key="stochasticOscillator"]',
            format: value => {
                const k = Number(value?.k ?? 0);
                const d = Number(value?.d ?? 0);
                return (k === 0 && d === 0) || value === undefined || value === null ? '--' : `${k.toFixed(2)}, ${d.toFixed(2)}`;
            },
            getValue: vm => vm.getStochasticOscillator()
        },
        obv: {
            selector: '.data-field[data-key="obv"]',
            format: value => value === 0 || value === undefined || value === null ? '--' : Number(value).toLocaleString(),
            getValue: vm => vm.getOBV()
        },
        marketTypeCanCloseEarly: {
            selector: '.data-field[data-key="marketTypeCanCloseEarly"]',
            format: (value, vm) => {
                const marketType = vm.getMarketType() || '--';
                const canCloseEarly = vm.marketModel.getMarketData(vm.currentMarket)?.MarketInfo?.canCloseEarly ? "Can close early" : "Cannot close early";
                return `${marketType}/${canCloseEarly}`;
            },
            getValue: vm => ({ marketType: vm.getMarketType(), canCloseEarly: vm.marketModel.getMarketData(vm.currentMarket)?.MarketInfo?.canCloseEarly })
        },
        askVelocityTop: {
            selector: '.data-field[data-key="askVelocityTop"]',
            format: (value, vm) => {
                const velocity = vm.getTopVelocity();
                return `${velocity.ask < 0 ? '-' : ''}${Number(velocity.ask).toFixed(2).replace('-', '')}/min (${velocity.askLevels || 0} levels)`;
            },
            getValue: vm => vm.getTopVelocity().ask
        },
        topBidVelocity: {
            selector: '.data-field[data-key="topBidVelocity"]',
            format: (value, vm) => {
                const velocity = vm.getTopVelocity();
                return `${velocity.bid < 0 ? '-' : ''}${Number(velocity.bid).toFixed(2).replace('-', '')}/min (${velocity.bidLevels || 0} levels)`;
            },
            getValue: vm => vm.getTopVelocity().bid
        },
        bottomVelocityAsk: {
            selector: '.data-field[data-key="bottomVelocityAsk"]',
            format: (value, vm) => {
                const velocity = vm.getBottomVelocity();
                return `${velocity.ask < 0 ? '-' : ''}${Number(velocity.ask).toFixed(2).replace('-', '')}/min (${velocity.askLevels || 0} levels)`;
            },
            getValue: vm => vm.getBottomVelocity().ask
        },
        bottomVelocityBid: {
            selector: '.data-field[data-key="bottomVelocityBid"]',
            format: (value, vm) => {
                const velocity = vm.getBottomVelocity();
                return `${velocity.ask < 0 ? '-' : ''}${Number(velocity.bid).toFixed(2).replace('-', '')}/min (${velocity.bidLevels || 0} levels)`;
            },
            getValue: vm => vm.getBottomVelocity().bid
        },
        netOrderRateAsk: {
            selector: '.data-field[data-key="netOrderRateAsk"]',
            format: (value, vm) => {
                if (value === undefined || value === null) return '--';
                const tradeCount = vm.isYes ? vm.NonTradeRelatedOrderCount_No() : vm.NonTradeRelatedOrderCount_Yes();
                if (tradeCount === undefined || tradeCount === null || isNaN(tradeCount)) return '--';
                const topVelocity = vm.getTopVelocity();
                const bottomVelocity = vm.getBottomVelocity();
                const totalVelocity = topVelocity.ask + bottomVelocity.ask;
                const percentage = totalVelocity === 0 ? 0 : (value / totalVelocity) * 100;
                return `${percentage.toFixed(0)}% (${tradeCount} orders)`;
            },
            getValue: vm => vm.isYes ? vm.getOrderRatePerMinute_YesAsk() : vm.getOrderRatePerMinute_NoAsk()
        },
        netOrderRateBid: {
            selector: '.data-field[data-key="netOrderRateBid"]',
            format: (value, vm) => {
                if (value === undefined || value === null) return '--';
                const tradeCount = vm.isYes ? vm.NonTradeRelatedOrderCount_Yes() : vm.NonTradeRelatedOrderCount_No();
                if (tradeCount === undefined || tradeCount === null || isNaN(tradeCount)) return '--';
                const topVelocity = vm.getTopVelocity();
                const bottomVelocity = vm.getBottomVelocity();
                const totalVelocity = topVelocity.bid + bottomVelocity.bid;
                const percentage = totalVelocity === 0 ? 0 : (value / totalVelocity) * 100;
                return `${percentage.toFixed(0)}% (${tradeCount} orders)`;
            },
            getValue: vm => vm.isYes ? vm.getOrderRatePerMinute_YesBid() : vm.getOrderRatePerMinute_NoBid()
        },
        averageTradeSizeAsk: {
            selector: '.data-field[data-key="averageTradeSizeAsk"]',
            format: (value, vm) => {
                const size = vm.isYes ? (Number(vm.getAverageTradeSizeNo()) || 0) : (Number(vm.getAverageTradeSizeYes()) || 0);
                const tradeRate = vm.isYes ? (Number(vm.getTradeRatePerMinute_No()) || 0) : (Number(vm.getTradeRatePerMinute_Yes()) || 0);
                return size === undefined || size === null || isNaN(size)
                    ? '--'
                    : `${size.toFixed(2)} (${tradeRate.toFixed(2)}/min)`;
            },
            getValue: vm => vm.isYes ? vm.getAverageTradeSizeNo() : vm.getAverageTradeSizeYes()
        },
        averageTradeSizeBid: {
            selector: '.data-field[data-key="averageTradeSizeBid"]',
            format: (value, vm) => {
                const size = vm.isYes ? (Number(vm.getAverageTradeSizeYes()) || 0) : (Number(vm.getAverageTradeSizeNo()) || 0);
                const tradeRate = vm.isYes ? (Number(vm.getTradeRatePerMinute_Yes()) || 0) : (Number(vm.getTradeRatePerMinute_No()) || 0);
                return size === undefined || size === null || isNaN(size)
                    ? '--'
                    : `${size.toFixed(2)} (${tradeRate.toFixed(2)}/min)`;
            },
            getValue: vm => vm.isYes ? vm.getAverageTradeSizeYes() : vm.getAverageTradeSizeNo()
        },
        tradeVolumeAsk: {
            selector: '.data-field[data-key="tradeVolumeAsk"]',
            format: (value, vm) => {
                if (value === undefined || value === null) return '--';
                const tradeCount = vm.isYes ? vm.getTradeCount().no : vm.getTradeCount().yes;
                if (tradeCount === undefined || tradeCount === null || isNaN(tradeCount)) return '--';
                const topVelocity = vm.getTopVelocity();
                const bottomVelocity = vm.getBottomVelocity();
                const totalVelocity = topVelocity.ask + bottomVelocity.ask;
                const percentage = totalVelocity === 0 ? 0 : (value / totalVelocity) * 100;
                return `${percentage.toFixed(0)}% (${tradeCount} trades)`;
            },
            getValue: vm => vm.isYes ? vm.getTradeVolumePerMinute_No() * -1 : vm.getTradeVolumePerMinute_Yes() * -1
        },
        tradeVolumeBid: {
            selector: '.data-field[data-key="tradeVolumeBid"]',
            format: (value, vm) => {
                if (value === undefined || value === null) return '--';
                const tradeCount = vm.isYes ? vm.getTradeCount().yes : vm.getTradeCount().no;
                if (tradeCount === undefined || tradeCount === null || isNaN(tradeCount)) return '--';
                const topVelocity = vm.getTopVelocity();
                const bottomVelocity = vm.getBottomVelocity();
                const totalVelocity = topVelocity.bid + bottomVelocity.bid;
                const percentage = totalVelocity === 0 ? 0 : (value / totalVelocity) * 100;
                return `${percentage.toFixed(0)}% (${tradeCount} trades)`;
            },
            getValue: vm => vm.isYes ? vm.getTradeVolumePerMinute_Yes() : vm.getTradeVolumePerMinute_No()
        },
        warningCount: {
            selector: '.data-field[data-key="warningCount"]',
            format: value => value === 0 || value === undefined || value === null ? '0' : Number(value).toLocaleString(),
            getValue: vm => vm.getWarningCount()
        },
        errorCount: {
            selector: '.data-field[data-key="errorCount"]',
            format: value => value === 0 || value === undefined || value === null ? '0' : Number(value).toLocaleString(),
            getValue: vm => vm.getErrorCount()
        }
    };

    initializeElements() {
        Object.entries(MarketView.dataBindings).forEach(([key, { selector }]) => {
            this.elements[key] = document.querySelector(selector);
            if (!this.elements[key]) console.warn(`Element '${key}' not found with selector '${selector}'`);
        });
        this.elements.priceChart = document.getElementById("priceChart");
        this.elements.stalenessIndicator = document.querySelector('.staleness-indicator');
        if (!this.elements.stalenessIndicator) console.warn("Staleness indicator not found with selector '.staleness-indicator'");
    }

    setupEventListeners() {
        if (this.elements.marketSelector) {
            this.elements.marketSelector.onchange = (event) => {
                this.marketViewModel.changeMarket(event.target.value);
            };
        }
        if (this.elements.timeframeSelector) {
            this.elements.timeframeSelector.onchange = (event) => {
                this.marketViewModel.changeTimeframe(event.target.value);
            };
        }
        if (this.elements.yesNoToggle) {
            this.elements.yesNoToggle.onchange = (event) => {
                this.marketViewModel.toggleYesNo(event.target.checked);
            };
        }
    }

    setupNavigationListeners() {
        document.addEventListener('keydown', (event) => {
            if (!this.elements.marketSelector || !this.marketViewModel.currentMarket) return;
            const markets = this.marketViewModel.getMarketList();
            if (markets.length === 0) return;
            const currentIndex = markets.indexOf(this.marketViewModel.currentMarket);
            let newIndex;
            if (event.key === 'ArrowLeft') {
                event.preventDefault();
                if (markets.length === 1) return;
                newIndex = (currentIndex - 1 + markets.length) % markets.length;
            } else if (event.key === 'ArrowRight') {
                if (markets.length === 1) return;
                event.preventDefault();
                newIndex = (currentIndex + 1) % markets.length;
            } else {
                return;
            }
            const newMarket = markets[newIndex];
            this.marketViewModel.changeMarket(newMarket);
            this.elements.marketSelector.value = newMarket;
        });

        document.addEventListener('touchstart', (event) => {
            this.touchStartX = event.changedTouches[0].screenX;
        }, false);

        document.addEventListener('touchend', (event) => {
            this.touchEndX = event.changedTouches[0].screenX;
            this.handleSwipe();
        }, false);
    }

    handleSwipe() {
        if (!this.elements.marketSelector || !this.marketViewModel.currentMarket) return;
        const markets = this.marketViewModel.getMarketList();
        if (markets.length === 0) return;
        const currentIndex = markets.indexOf(this.marketViewModel.currentMarket);
        const swipeThreshold = 50;
        const swipeDistance = this.touchEndX - this.touchStartX;
        if (Math.abs(swipeDistance) < swipeThreshold) return;
        let newIndex;
        if (swipeDistance > 0) {
            newIndex = (currentIndex - 1 + markets.length) % markets.length;
        } else {
            newIndex = (currentIndex + 1) % markets.length;
        }
        const newMarket = markets[newIndex];
        this.marketViewModel.changeMarket(newMarket);
        this.elements.marketSelector.value = newMarket;
    }

    initializeChart() {
        if (!this.elements.priceChart) {
            console.error("Canvas element with ID 'priceChart' not found.");
            return;
        }
        this.chart = new Chart(this.elements.priceChart.getContext("2d"), {
            data: {
                datasets: [
                    {
                        type: "candlestick",
                        label: "Candlestick (Mid Price)",
                        data: [],
                        color: {
                            up: getComputedStyle(document.documentElement).getPropertyValue('--ask-color').trim() || "#00FF00", // Fallback to green
                            down: getComputedStyle(document.documentElement).getPropertyValue('--bid-color').trim() || "#FF0000", // Fallback to red
                            unchanged: "#bbbbbb"
                        },
                        borderColor: getComputedStyle(document.documentElement).getPropertyValue('--ask-color').trim() || "#00FF00" // Fallback to green
                    },
                    {
                        type: "line",
                        label: "Ask (Live)",
                        data: [],
                        borderColor: getComputedStyle(document.documentElement).getPropertyValue('--ask-color').trim() || "#00FF00",
                        fill: false,
                        tension: 0.1
                    },
                    {
                        type: "line",
                        label: "Bid (Live)",
                        data: [],
                        borderColor: getComputedStyle(document.documentElement).getPropertyValue('--bid-color').trim() || "#FF0000",
                        fill: false,
                        tension: 0.1
                    },
                    {
                        type: "line",
                        label: "Buyin Price",
                        data: [],
                        borderColor: "rgba(255, 255, 255, 0.4)", 
                        borderDash: [5, 5],
                        borderWidth: 1,
                        pointRadius: 0,
                        fill: false,
                        tension: 0,
                        spanGaps: true
                    },
                    {
                        type: "line",
                        label: "Bollinger Bands",
                        group: "bollinger",
                        subLabel: "Upper",
                        data: [],
                        borderColor: "#FF0000",
                        borderWidth: 1,
                        pointRadius: 0,
                        fill: false,
                        tension: 0,
                        spanGaps: true,
                        yAxisID: "y"
                    },
                    {
                        type: "line",
                        label: "Bollinger Bands",
                        group: "bollinger",
                        subLabel: "Lower",
                        data: [],
                        borderColor: "#FF0000",
                        borderWidth: 1,
                        pointRadius: 0,
                        fill: false,
                        tension: 0,
                        spanGaps: true,
                        yAxisID: "y"
                    },
                    {
                        type: "bar",
                        label: "Volume",
                        data: [],
                        backgroundColor: "rgba(255, 255, 255, 0.8)",
                        borderColor: "rgba(255, 255, 255, 1)",
                        borderWidth: 1,
                        yAxisID: "yVolume",
                        barPercentage: 0.4 // Adjust to align with candlesticks
                    }
                ]
            },
            options: {
                scales: {
                    x: {
                        type: "time",
                        time: {
                            unit: "hour",
                            displayFormats: { minute: "h:mm a", hour: "MMM D, h a", day: "MMM D" },
                            tooltipFormat: "MMM D, YYYY, h:mm a",
                            parser: (value) => moment.utc(value).valueOf(),
                            source: "data"
                        },
                        adapters: {
                            date: {
                                zone: undefined
                            }
                        },
                        title: { display: true, text: "Time" },
                        ticks: { color: "#bbbbbb", maxTicksLimit: 10 }
                    },
                    y: {
                        title: { display: true, text: "Price (cents)" },
                        ticks: { color: "#bbbbbb", stepSize: 5 },
                        position: "left"
                    },
                    yVolume: {
                        title: { display: true, text: "Volume (Number of Contracts Traded)" },
                        ticks: {
                            color: "#bbbbbb",
                            stepSize: 100,
                            callback: function (value, index, values) {
                                const max = this.chart.options.scales.yVolume.max || 1000;
                                if (max < 1000) {
                                    return Number(value).toFixed(2); // 2 decimal places
                                } else if (max < 10000) {
                                    return Math.round(Number(value)); // 0 decimal places
                                } else {
                                    const kValue = Number(value) / 1000;
                                    return `${kValue.toFixed(1)}k`; // e.g., 50.6k
                                }
                            }
                        },
                        position: "right",
                        grid: { display: false },
                        min: 0
                    }
                },
                plugins: {
                    legend: {
                        labels: {
                            color: "#e0e0e0",
                            font: { size: 10 },
                            padding: 5,
                            filter: (legendItem, chartData) => {
                                const dataset = chartData.datasets[legendItem.datasetIndex];
                                if (!dataset || !dataset.data) return false;
                                // Always include these labels if they have data, even if hidden
                                if (["Candlestick (Mid Price)", "Ask (Live)", "Bid (Live)", "Buyin Price", "Bollinger Bands", "Volume", "Support/Resistance"].includes(dataset.label)) {
                                    return dataset.data.length > 0;
                                }
                                return false;
                            },
                            generateLabels: (chart) => {
                                const datasets = chart.data.datasets;
                                const seenLabels = new Set();
                                const labels = [];

                                datasets.forEach((dataset, i) => {
                                    if (!dataset.label) return;

                                    // Use group for Bollinger Bands or isSupportResistance for Support/Resistance
                                    const key = dataset.group || (dataset.isSupportResistance ? "supportResistance" : dataset.label);
                                    if (!seenLabels.has(key)) {
                                        seenLabels.add(key);

                                        // For Bollinger Bands
                                        if (dataset.group === "bollinger") {
                                            const bollingerDatasets = datasets.filter(ds => ds.group === "bollinger");
                                            const isHidden = bollingerDatasets.every(ds => ds.hidden);
                                            labels.push({
                                                text: dataset.label,
                                                fillStyle: dataset.borderColor || dataset.backgroundColor,
                                                strokeStyle: dataset.borderColor,
                                                lineWidth: dataset.borderWidth,
                                                hidden: isHidden,
                                                datasetIndex: i,
                                                fontColor: isHidden ? "rgba(224, 224, 224, 0.5)" : "#e0e0e0",
                                                bollingerIndices: bollingerDatasets.map((_, idx) => datasets.indexOf(bollingerDatasets[idx]))
                                            });
                                        }
                                        // For Support/Resistance
                                        else if (dataset.isSupportResistance) {
                                            const srDatasets = datasets.filter(ds => ds.isSupportResistance);
                                            const isHidden = srDatasets.every(ds => ds.hidden);
                                            labels.push({
                                                text: dataset.label,
                                                fillStyle: dataset.borderColor || dataset.backgroundColor,
                                                strokeStyle: dataset.borderColor,
                                                lineWidth: dataset.borderWidth,
                                                hidden: isHidden,
                                                datasetIndex: i,
                                                fontColor: isHidden ? "rgba(224, 224, 224, 0.5)" : "#e0e0e0",
                                                supportResistanceIndices: srDatasets.map((_, idx) => datasets.indexOf(srDatasets[idx]))
                                            });
                                        }
                                        // For other datasets
                                        else {
                                            labels.push({
                                                text: dataset.label,
                                                fillStyle: dataset.borderColor || dataset.backgroundColor,
                                                strokeStyle: dataset.borderColor,
                                                lineWidth: dataset.borderWidth,
                                                hidden: dataset.hidden,
                                                datasetIndex: i,
                                                fontColor: dataset.hidden ? "rgba(224, 224, 224, 0.5)" : "#e0e0e0"
                                            });
                                        }
                                    }
                                });

                                return labels;
                            }
                        },
                        position: "top",
                        align: "center",
                        onClick: (e, legendItem, legend) => {
                            const chart = legend.chart;
                            if (legendItem.bollingerIndices || legendItem.supportResistanceIndices) {
                                // Toggle all datasets for grouped items (Bollinger or Support/Resistance)
                                const indices = legendItem.bollingerIndices || legendItem.supportResistanceIndices || [];
                                const isHidden = !chart.data.datasets[indices[0]].hidden;
                                indices.forEach(index => {
                                    chart.data.datasets[index].hidden = isHidden;
                                    chart.getDatasetMeta(index).hidden = isHidden;
                                });
                            } else {
                                // Toggle individual dataset
                                const index = legendItem.datasetIndex;
                                const isHidden = !chart.data.datasets[index].hidden;
                                chart.data.datasets[index].hidden = isHidden;
                                chart.getDatasetMeta(index).hidden = isHidden;
                            }
                            chart.update();
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: context => {
                                const dataset = context.dataset;
                                const data = context.raw;
                                if (dataset.type === "candlestick") return `${dataset.label}: O:${Number(data.o).toFixed(0)} H:${Number(data.h).toFixed(0)} L:${Number(data.l).toFixed(0)} C:${Number(data.c).toFixed(0)}`;
                                if (dataset.label === "Buyin Price" || dataset.label.includes("Bollinger")) return `${dataset.label}: ${Number(data.y).toFixed(0)}¢`;
                                return `${dataset.label}: ${Number(data.y).toFixed(0)}`;
                            },
                            title: tooltipItems => moment.utc(tooltipItems[0].parsed.x).local().format("MMM D, YYYY, h:mm a") // Display local time
                        }
                    },
                    maintainAspectRatio: false,
                    annotation: {
                        annotations: {
                            atrLabel: {
                                type: "label",
                                xValue: 0,
                                yValue: 0,
                                content: "ATR: --",
                                color: "#e0e0e0",
                                backgroundColor: "rgba(0, 0, 0, 0.7)",
                                font: { size: 10 },
                                position: "start",
                                xAdjust: 10,
                                yAdjust: 10,
                                display: false
                            }
                        }
                    }
                },
                layout: {
                    padding: {
                        bottom: 50
                    }
                }
            }
        });
    }

    startTimeSinceUpdates() {
        setInterval(() => this.update(), 10000);
    }

    formatWhen(when) {
        if (!when || when === "0001-01-01T00:00:00") return "--";
        return moment.utc(when).local().format("MMM D, h:mm a");
    }

    update() {
        if (!this.chart) return;

        // Update body background based on trading status and data staleness
        const body = document.body;
        const tradingStatus = this.marketViewModel.marketModel.tradingStatus;
        const now = moment.utc().toDate();
        const staleThresholdMs = 300000; // 5 minutes

        // Check staleness for all subscribed markets
        let isCurrentMarketStale = false;
        let isAnyMarketStale = false;
        const subscribedMarkets = this.marketViewModel.marketModel.subscribedMarkets || [];

        for (const marketTicker of subscribedMarkets) {
            const orderbookLastUpdated = this.marketViewModel.marketModel.orderbookLastUpdated[marketTicker] || null;
            const lastWebSocketUpdate = this.marketViewModel.marketModel.getLastWebSocketUpdate()?.toISOString() || null;
            const orderbook = this.marketViewModel.getOrderbookData(marketTicker);

            // Check time-based staleness
            const timeSinceLastUpdateMs = lastWebSocketUpdate ? (now - new Date(lastWebSocketUpdate)) : Infinity;
            const isStaleTime = timeSinceLastUpdateMs > staleThresholdMs;

            // Check price mismatch
            let isPriceMismatch = false;
            if (orderbook?.length) {
                // Get best Yes Bid and No Bid from orderbook
                const yesBids = orderbook.filter(order => order.side.toLowerCase() === "yes").sort((a, b) => Number(b.price) - Number(a.price));
                const noBids = orderbook.filter(order => order.side.toLowerCase() === "no").sort((a, b) => Number(b.price) - Number(a.price));
                const bestYesBid = yesBids.length > 0 ? Number(yesBids[0].price) : null;
                const bestNoBid = noBids.length > 0 ? Number(noBids[0].price) : null;

                // Calculate expected Ask and Bid prices based on isYes toggle
                const currentPrice = this.marketViewModel.getCurrentPrice(marketTicker);
                const expectedAsk = this.marketViewModel.isYes ? (bestNoBid !== null ? 100 - bestNoBid : null) : (bestYesBid !== null ? 100 - bestYesBid : null);
                const expectedBid = this.marketViewModel.isYes ? bestYesBid : bestNoBid;

                // Compare with currentPrice ask and bid
                isPriceMismatch = (expectedAsk !== null && currentPrice.ask !== null && Math.abs(currentPrice.ask - expectedAsk) > 0.01) ||
                    (expectedBid !== null && currentPrice.bid !== null && Math.abs(currentPrice.bid - expectedBid) > 0.01);
            }

            // Determine if this market is stale
            const isMarketStale = isStaleTime || isPriceMismatch;

            // Update flags
            if (isMarketStale) {
                isAnyMarketStale = true;
            }
            if (marketTicker === this.marketViewModel.currentMarket && isMarketStale) {
                isCurrentMarketStale = true;
            }

            // No need to continue checking if both conditions are met
            if (isCurrentMarketStale && isAnyMarketStale) {
                break;
            }
        }

        // Remove existing classes
        body.classList.remove('stale-background', 'exchange-closed');

        // Apply background class based on current market staleness
        if (!tradingStatus) {
            body.classList.add('exchange-closed');
        } else if (isCurrentMarketStale) {
            body.classList.add('stale-background');
        }

        // Update staleness indicator based on any market being stale
        if (this.elements.stalenessIndicator) {
            this.elements.stalenessIndicator.classList.toggle('stale', isAnyMarketStale);
        }

        if (this.elements.loadingOverlay) {
            this.elements.loadingOverlay.style.display =
                this.marketViewModel.isUpdatingMarket || this.marketViewModel.marketModel.isLoading ? "block" : "none";
        }

        Object.entries(MarketView.dataBindings).forEach(([key, binding]) => {
            const element = this.elements[key];
            if (element && binding.format && binding.getValue) {
                const value = binding.getValue(this.marketViewModel);
                element.innerHTML = binding.format(value, this.marketViewModel);
                if (binding.attributes) {
                    Object.entries(binding.attributes).forEach(([attr, getAttrValue]) => {
                        element.setAttribute(attr, getAttrValue(this.marketViewModel, this));
                    });
                }
            }
        });

        // Update the labels for bid-ask-values dynamically
        this.updateBidAskLabels();

        if (this.elements.askBidImbalanceVolume && this.marketViewModel.currentMarket) {
            const totalAsk = this.marketViewModel.isYes ? this.marketViewModel.getTotalYesAskContracts() : this.marketViewModel.getTotalNoAskContracts();
            const totalBid = this.marketViewModel.isYes ? this.marketViewModel.getTotalYesBidContracts() : this.marketViewModel.getTotalNoBidContracts();
            this.elements.askBidImbalanceVolume.style.color =
                totalAsk > totalBid ? 'var(--ask-color)' :
                    totalBid > totalAsk ? 'var(--bid-color)' : '#e0e0e0';
        }

        if (this.marketViewModel.currentMarket) {
            const { candlesticks, tickers } = this.marketViewModel.getChartData();

            const recentChartTickers = tickers.slice(-3).map((t, i) => ({
                index: tickers.length - 3 + i,
                timestamp: t.timestamp,
                x: t.x,
                yesAsk: t.yesAsk,
                yesBid: t.yesBid
            }));
            console.log(`Chart tickers for ${this.marketViewModel.currentMarket} (last 3):`, recentChartTickers);

            this.chart.data.datasets[0].data = candlesticks.map(c => ({
                x: moment.utc(c.x).valueOf(),
                h: Number(this.marketViewModel.isYes ? c.h : 100 - c.h),
                l: Number(this.marketViewModel.isYes ? c.l : 100 - c.l),
                c: Number(this.marketViewModel.isYes ? c.c : 100 - c.c),
                o: Number(this.marketViewModel.isYes ? c.o : 100 - c.o)
            }));


            this.chart.data.datasets[0].data = candlesticks.map(c => ({
                x: moment.utc(c.x).valueOf(),
                h: Number(this.marketViewModel.isYes ? c.h : 100 - c.h),
                l: Number(this.marketViewModel.isYes ? c.l : 100 - c.l),
                c: Number(this.marketViewModel.isYes ? c.c : 100 - c.c),
                o: Number(this.marketViewModel.isYes ? c.o : 100 - c.o)
            }));

            if (candlesticks.length > 0) {
                this.chart.data.datasets[6].data = candlesticks.map(c => ({
                    x: moment.utc(c.x).valueOf(),
                    y: Number(c.v) || 0 // Use 0 if volume is undefined or null
                }));
            } else {
                // Provide placeholder data with zero volume for the current x-range
                const xRange = this.calculateXRange(candlesticks, tickers);
                this.chart.data.datasets[6].data = [
                    { x: xRange.min, y: 0 },
                    { x: xRange.max, y: 0 }
                ];
            }

            this.chart.data.datasets[1].data = tickers.map(t => ({
                x: moment.utc(t.x).valueOf(),
                y: Number(this.marketViewModel.isYes ? t.yesAsk : 100 - t.yesBid)
            }));

            this.chart.data.datasets[2].data = tickers.map(t => ({
                x: moment.utc(t.x).valueOf(),
                y: Number(this.marketViewModel.isYes ? t.yesBid : 100 - t.yesAsk)
            }));

            const positionSize = this.marketViewModel.getPositionSize();
            let xRange = this.calculateXRange(candlesticks, tickers);
            if (positionSize !== 0) {
                const buyinPrice = this.marketViewModel.getBuyinPrice();
                this.chart.data.datasets[3].data = [
                    { x: xRange.min, y: buyinPrice },
                    { x: xRange.max, y: buyinPrice }
                ];
            } else {
                this.chart.data.datasets[3].data = [];
            }

            const bollingerBands = this.marketViewModel.getBollingerBands();
            const isBollingerUseful = bollingerBands.lower !== null && bollingerBands.upper !== null &&
                bollingerBands.lower !== bollingerBands.upper;
            if (isBollingerUseful) {
                const upper = Number(this.marketViewModel.isYes ? bollingerBands.upper : 100 - bollingerBands.upper);
                const lower = Number(this.marketViewModel.isYes ? bollingerBands.lower : 100 - bollingerBands.lower);
                this.chart.data.datasets[4].data = [
                    { x: xRange.min, y: upper },
                    { x: xRange.max, y: upper }
                ];
                this.chart.data.datasets[5].data = [
                    { x: xRange.min, y: lower },
                    { x: xRange.max, y: lower }
                ];
            } else {
                this.chart.data.datasets[4].data = [];
                this.chart.data.datasets[5].data = [];
            }

            xRange = this.calculateXRange(candlesticks, tickers);
            this.chart.options.scales.x.min = xRange.min;
            this.chart.options.scales.x.max = xRange.max;
            this.adjustChartScales(candlesticks, tickers);
            const yRange = this.calculateYRange(candlesticks, tickers);
            this.chart.options.scales.y.min = yRange.min;
            this.chart.options.scales.y.max = yRange.max;
            this.chart.options.scales.x.time.unit = this.calculateTimeUnit(candlesticks, tickers);

            this.chart.data.datasets = this.chart.data.datasets.filter(ds => !ds.isSupportResistance);
            const marketData = this.marketViewModel.marketModel.getMarketData(this.marketViewModel.currentMarket);
            const rawLevels = marketData.supportResistanceLevels || [];

            const supportResistanceLevels = this.marketViewModel.isYes
                ? rawLevels
                : rawLevels.map(level => ({
                    ...level,
                    price: 100 - level.price
                }));

            const shouldUpdateSR =
                this.lastSupportResistanceLevels.length === 0 ||
                !this.areSupportResistanceLevelsEqual(supportResistanceLevels, this.lastSupportResistanceLevels);

            if (shouldUpdateSR) {
                this.lastSupportResistanceLevels = supportResistanceLevels.map(level => ({
                    price: level.price ?? null,
                    totalVolume: level.totalVolume ?? null,
                    testCount: level.testCount ?? null
                }));
                this.supportResistanceDatasets = [];

                const volumePerTest = supportResistanceLevels.map(level =>
                    level.testCount > 0 ? (Number(level.totalVolume) || 0) / Number(level.testCount) : 0
                );
                const minVolumePerTest = Math.min(...volumePerTest);
                const maxVolumePerTest = Math.max(...volumePerTest);
                const volumeRange = maxVolumePerTest - minVolumePerTest || 1;

                supportResistanceLevels.forEach((level, index) => {
                    if (level.price == null || level.totalVolume == null || level.testCount == null) return;

                    const adjustedPrice = Number(level.price);
                    if (isNaN(adjustedPrice) || adjustedPrice < 0 || adjustedPrice > 100) return;

                    const vpt = level.testCount > 0 ? (Number(level.totalVolume) || 0) / Number(level.testCount) : 0;
                    let normalizedThickness = volumeRange === 0 ? 2 : 1 + ((vpt - minVolumePerTest) / volumeRange) * 2;
                    let thickness = Math.max(2, Math.min(4, normalizedThickness));

                    this.supportResistanceDatasets.push({
                        type: "line",
                        label: "Support/Resistance",
                        data: [
                            { x: 0, y: adjustedPrice },
                            { x: Date.now() + 1000 * 60 * 60 * 24 * 365, y: adjustedPrice }
                        ],
                        borderColor: "rgba(0, 255, 0, 0.4)",
                        backgroundColor: "rgba(0, 255, 0, 0.1)",
                        borderWidth: thickness,
                        borderDash: [5, 5],
                        pointRadius: 0,
                        fill: false,
                        tension: 0,
                        spanGaps: true,
                        isSupportResistance: true,
                        hidden: false
                    });
                });
            }

            this.chart.data.datasets = [...this.chart.data.datasets, ...this.supportResistanceDatasets];
            this.chart.update();
        }

        const marketList = this.marketViewModel.getMarketList();
        if (this.elements.marketSelector) {
            this.elements.marketSelector.innerHTML = marketList.length === 0 ?
                "<option value=''>No markets subscribed</option>" :
                marketList.map(m => {
                    const title = this.marketViewModel.isUpdatingMarket && m === this.marketViewModel.currentMarket ? "Loading..." : (this.marketViewModel.getMarketTitle(m) || m);
                    return `<option value="${m}" ${m === this.marketViewModel.currentMarket ? "selected" : ""}>${title}</option>`;
                }).join("");
            if (this.marketViewModel.currentMarket) {
                this.elements.marketSelector.value = this.marketViewModel.currentMarket;
            }
        }

        const orderbookData = this.marketViewModel.getOrderbookData();
        if (this.elements.orderbookBody) {
            if (orderbookData && Array.isArray(orderbookData) && orderbookData.length > 0) {
                let asks = this.marketViewModel.isYes ?
                    orderbookData.filter(order => order.side.toLowerCase() === "yes").sort((a, b) => Number(b.price) - Number(a.price)).slice(0, 4) :
                    orderbookData.filter(order => order.side.toLowerCase() === "no").sort((a, b) => Number(b.price) - Number(a.price)).slice(0, 4);
                let bids = this.marketViewModel.isYes ?
                    orderbookData.filter(order => order.side.toLowerCase() === "no")
                        .map(order => ({ price: 100 - Number(order.price), size: Number(order.size), value: (100 - Number(order.price)) * Number(order.size) / 100.0 }))
                        .sort((a, b) => Number(a.price) - Number(b.price)).slice(0, 4) :
                    orderbookData.filter(order => order.side.toLowerCase() === "yes")
                        .map(order => ({ price: 100 - Number(order.price), size: Number(order.size), value: (100 - Number(order.price)) * Number(order.size) / 100.0 }))
                        .sort((a, b) => Number(a.price) - Number(b.price)).slice(0, 4);

                const bestAsk = asks.length > 0 ? Number(asks[0].price) : null;
                const bestBid = bids.length > 0 ? Number(bids[0].price) : null;
                const lastPrice = bestAsk && bestBid ? Math.round((bestAsk + bestBid) / 2) : null;

                let rows = [];
                if (bids.length > 0) {
                    bids.reverse().forEach(order => {
                        rows.push(`<tr class="bid-row"><td>${this.formatCents(order.price)}</td><td>${order.size}</td><td>${this.formatDollars(order.value)}</td></tr>`);
                    });
                } else {
                    rows.push(`<tr class="bid-row"><td>--</td><td>--</td><td>--</td></tr>`);
                }
                rows.push('<tr><td colspan="3" style="height: 10px;"></td></tr>');
                if (asks.length > 0) {
                    asks.forEach(order => {
                        const isLast = lastPrice && Number(order.price) === lastPrice;
                        rows.push(
                            `<tr class="ask-row"><td>${this.formatCents(order.price)}</td><td>${order.size}</td><td>${this.formatDollars(order.value)}</td></tr>`);
                    });
                } else {
                    rows.push(`<tr class="ask-row"><td>--</td><td>--</td><td>--</td></tr>`);
                }
                this.elements.orderbookBody.innerHTML = rows.join("");
            } else {
                this.elements.orderbookBody.innerHTML = "<tr><td colspan='3'>No orderbook data available</td></tr>";
            }
        }

        if (this.elements.yesNoToggle) {
            this.elements.yesNoToggle.checked = this.marketViewModel.isYes;
        }
    }

    updateBidAskLabels() {
        const bidAskContainers = document.querySelectorAll('.bid-ask-values');
        bidAskContainers.forEach(container => {
            // Remove existing labels if they exist
            const existingLabels = container.querySelectorAll('.bid-ask-label');
            existingLabels.forEach(label => label.remove());

            // Create new labels
            const leftLabel = document.createElement('span');
            leftLabel.className = 'bid-ask-label';
            leftLabel.style.position = 'absolute';
            leftLabel.style.top = '-1.2em';
            leftLabel.style.left = '50%';
            leftLabel.style.transform = 'translateX(-50%)';
            leftLabel.style.fontSize = '0.7rem';
            leftLabel.style.fontWeight = 'bold';
            leftLabel.style.color = 'var(--ask-color)';
            leftLabel.textContent = this.marketViewModel.isYes ? 'No Bid' : 'Yes Bid';

            const rightLabel = document.createElement('span');
            rightLabel.className = 'bid-ask-label';
            rightLabel.style.position = 'absolute';
            rightLabel.style.top = '-1.2em';
            rightLabel.style.right = '0';
            rightLabel.style.fontSize = '0.7rem';
            rightLabel.style.fontWeight = 'bold';
            rightLabel.style.color = 'var(--bid-color)';
            rightLabel.textContent = this.marketViewModel.isYes ? 'Yes Bid' : 'No Bid';

            container.appendChild(leftLabel);
            container.appendChild(rightLabel);
        });
    }

    calculateYRange(candlesticks, tickers) {
        let prices = [];

        for (let c of candlesticks) {
            prices.push(Number(this.marketViewModel.isYes ? c.h : 100 - c.h));
            prices.push(Number(this.marketViewModel.isYes ? c.l : 100 - c.l));
        }

        for (let t of tickers) {
            if (t.yesAsk !== null) prices.push(Number(this.marketViewModel.isYes ? t.yesAsk : 100 - t.yesBid));
            if (t.yesBid !== null) prices.push(Number(this.marketViewModel.isYes ? t.yesBid : 100 - t.yesAsk));
        }

        if (prices.length === 0) return { min: 0, max: 100 };

        let min = Math.min(...prices);
        let max = Math.max(...prices);

        // Add some margin
        var margin = (max - min) * .1 || 10;
        if (margin < 10) {
            margin = 10;
        }
        return {
            min: Math.max(0, min - margin),
            max: Math.min(100, max + margin)
        };
    }


    formatCents(value) { return `${Number(value).toFixed(0)}¢`; }
    formatDollars(value) { return value === 0 ? "--" : (Number(value) < 0 ? `-${Math.abs(Number(value)).toFixed(2)}` : `${Number(value).toFixed(2)}`); }

    calculateXRange(candlesticks, tickers) {
        const allXValues = [
            ...candlesticks.map(c => moment.utc(c.x).valueOf()),
            ...tickers.map(t => moment.utc(t.x).valueOf())
        ].filter(x => x > 0);
        const now = moment.utc().valueOf(); // Use UTC time
        const timeframeDurationMs = this.marketViewModel.getTimeframeDuration(this.marketViewModel.currentTimeframe).asMilliseconds();
        if (allXValues.length === 0) {
            return { min: now - timeframeDurationMs, max: now + (timeframeDurationMs * 0.05) };
        }
        if (this.marketViewModel.currentTimeframe === "All") {
            const minX = Math.min(...allXValues);
            const maxX = Math.max(...allXValues, now);
            const dataRange = maxX - minX;
            const rightPadding = dataRange * 0.05;
            return { min: minX, max: maxX + rightPadding };
        }
        const minX = now - timeframeDurationMs;
        const maxX = now;
        const rightPadding = timeframeDurationMs * 0.05;
        return { min: minX, max: maxX + rightPadding };
    }

    areSupportResistanceLevelsEqual(newLevels, oldLevels) {
        if (newLevels.length !== oldLevels.length) return false;
        return newLevels.every((newLevel, index) => {
            const oldLevel = oldLevels[index];
            return (
                Number(newLevel.price) === Number(oldLevel.price)
            );
        });
    }

    calculateTimeUnit(candlesticks, tickers) {
        const timeframe = this.marketViewModel.currentTimeframe;

        // For 15m and 1h, use minute-based ticks
        if (timeframe === "15m" || timeframe === "1h") {
            return "minute";
        }

        // For "All", dynamically determine the best unit based on data range
        if (timeframe === "All") {
            const allXValues = [
                ...candlesticks.map(c => c.x),
                ...tickers.map(t => t.x)
            ].filter(x => x > 0).sort((a, b) => a - b);

            if (allXValues.length < 2) return "day";

            const rangeMs = allXValues[allXValues.length - 1] - allXValues[0];
            const rangeDays = rangeMs / (1000 * 60 * 60 * 24); // Convert range to days

            if (rangeDays > 30) return "month"; // Use month ticks for ranges over 30 days
            if (rangeDays > 7) return "week";   // Use week ticks for ranges over 7 days
            return "day";                       // Default to day ticks for shorter ranges
        }

        // For other timeframes, calculate dynamically
        const allXValues = [
            ...candlesticks.map(c => c.x),
            ...tickers.map(t => t.x)
        ].filter(x => x > 0).sort((a, b) => a - b);

        if (allXValues.length < 2) return this.marketViewModel.getTimeUnit(this.marketViewModel.currentTimeframe);

        const rangeMs = allXValues[allXValues.length - 1] - allXValues[0];
        const density = allXValues.length / (rangeMs / (1000 * 60 * 60));

        if (density > 10) return "day";
        if (density > 2) return "hour";
        return "minute";
    }

    adjustChartScales(candlesticks, tickers) {
        const allPriceValues = [
            ...candlesticks.flatMap(c => [
                Number(this.marketViewModel.isYes ? c.o : 100 - c.o),
                Number(this.marketViewModel.isYes ? c.h : 100 - c.h),
                Number(this.marketViewModel.isYes ? c.l : 100 - c.l),
                Number(this.marketViewModel.isYes ? c.c : 100 - c.c)
            ]),
            ...tickers.flatMap(t => [
                Number(this.marketViewModel.isYes ? t.yesBid : 100 - t.yesAsk),
                Number(this.marketViewModel.isYes ? t.yesAsk : 100 - t.yesBid)
            ])
        ].filter(v => v !== undefined && !isNaN(v));

        const positionSize = this.marketViewModel.getPositionSize();
        if (positionSize !== 0) {
            const buyinPrice = this.marketViewModel.getBuyinPrice();
            const adjustedBuyinPrice = Number(this.marketViewModel.isYes ? buyinPrice : 100 - buyinPrice);
            allPriceValues.push(adjustedBuyinPrice);
        }

        // Calculate price y-axis
        if (allPriceValues.length > 0) {
            const minValue = Math.min(...allPriceValues);
            const maxValue = Math.max(...allPriceValues);
            this.chart.options.scales.y.min = Math.max(0, Math.floor((minValue - 10) / 5) * 5);
            this.chart.options.scales.y.max = Math.ceil((maxValue + 10) / 5) * 5;
            this.chart.options.scales.y.ticks.stepSize = Math.max(5, Math.round((this.chart.options.scales.y.max - this.chart.options.scales.y.min) / 10));
        } else {
            this.chart.options.scales.y.min = 0;
            this.chart.options.scales.y.max = 100;
            this.chart.options.scales.y.ticks.stepSize = 10;
        }

        // Calculate volume y-axis
        const timeUnit = this.calculateTimeUnit(candlesticks, tickers);
        let maxVolume;

        // Map time unit to the appropriate highestVolume field
        if (timeUnit === 'minute') {
            maxVolume = Number(this.marketViewModel.getHighestVolumeMinute());
        } else if (timeUnit === 'hour') {
            maxVolume = Number(this.marketViewModel.getHighestVolumeHour());
        } else {
            maxVolume = Number(this.marketViewModel.getHighestVolumeDay());
        }

        // Fallback to candlestick volume if highestVolume is invalid or zero
        const allVolumeValues = candlesticks.map(c => Number(c.v) || 0).filter(v => v !== undefined && !isNaN(v));
        if (allVolumeValues.length > 0 && (maxVolume === 0 || isNaN(maxVolume))) {
            maxVolume = Math.max(...allVolumeValues);
        }

        // Set volume y-axis scales
        if (maxVolume > 0) {
            this.chart.options.scales.yVolume.min = 0; // Volume is non-negative
            this.chart.options.scales.yVolume.max = Math.ceil(maxVolume * 1.1); // Add 10% margin
            this.chart.options.scales.yVolume.ticks.stepSize = Math.max(100, Math.round(this.chart.options.scales.yVolume.max / 10));
        } else {
            this.chart.options.scales.yVolume.min = 0;
            this.chart.options.scales.yVolume.max = 1000; // Default max for zero volume
            this.chart.options.scales.yVolume.ticks.stepSize = 100;
        }
    }
}