// signalrService.js
// Handles SignalR connection and communication for chart and performance data.
class SignalRService {
    constructor(marketModel, viewModel) {
        this.marketModel = marketModel; // Can be null if not relevant for the current page
        this.viewModel = viewModel;     // The primary ViewModel for the current page

        // Initialize ViewModel properties if they are undefined (robustness for different ViewModels)
        if (this.viewModel) {
            this.viewModel.performanceMetrics = this.viewModel.performanceMetrics || [];
            this.viewModel.semaphoreStatus = this.viewModel.semaphoreStatus || [];
            this.viewModel.timerStatus = this.viewModel.timerStatus || [];
            this.viewModel.webSocketEventCounts = this.viewModel.webSocketEventCounts || [];
            this.viewModel.broadcastCounts = this.viewModel.broadcastCounts || [];
            this.viewModel.queueMetrics = this.viewModel.queueMetrics || {};
        }

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/chartHub") // URL for the SignalR hub
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.startConnection();
        this.registerHandlers();
    }

    async startConnection() {
        try {
            await this.connection.start();
            console.log("SignalR Connected.");
            // Call refreshData on ViewModel if it exists (e.g., for initial data load)
            if (this.viewModel && typeof this.viewModel.refreshData === 'function') {
                this.viewModel.refreshData();
            }
        } catch (err) {
            console.error("SignalR Connection Error: ", err);
            setTimeout(() => this.startConnection(), 5000); // Retry connection after 5 seconds
        }
    }

    // Registers handlers for messages received from the SignalR hub.
    registerHandlers() {
        this.connection.on("StartBatchUpdate", () => {
            if (this.marketModel) this.marketModel.startBatchUpdate();
        });

        this.connection.on("UpdateExchangeStatus", data => {
            if (this.marketModel) {
                this.marketModel.updateExchangeStatus(data.exchangeStatus);
                this.marketModel.updateTradingStatus(data.tradingStatus);
            }
            if (this.viewModel) this.viewModel.onModelChanged();
        });

        this.connection.on("EndBatchUpdate", () => {
            if (this.marketModel) this.marketModel.endBatchUpdate();
            if (this.viewModel) this.viewModel.onModelChanged();
        });

        this.connection.on("UpdateMarketList", markets => {
            if (this.marketModel) this.marketModel.setSubscribedMarkets(markets);
            if (this.viewModel) this.viewModel.onModelChanged();
        });

        this.connection.on("ReceiveMarketDataBatch", data => {
            if (this.marketModel && data && data.tickers) {
                console.log("Received ReceiveMarketDataBatch:", data); // Restored console.log
                const marketTicker = data.marketTicker;
                data.tickers.forEach(ticker => {
                    this.marketModel.addTicker(marketTicker, {
                        x: moment.utc(ticker.timestamp).valueOf(),
                        yesAsk: ticker.yesAsk,
                        yesBid: ticker.yesBid,
                        timestamp: ticker.timestamp,
                        source: ticker.source,
                        yesSpread: ticker.yesSpread,
                        noSpread: ticker.noSpread
                    }, true);
                });
            }
        });

        this.connection.on("HistoricalData", data => {
            if (this.marketModel) {
                if (!data || !data.marketTicker) {
                    console.warn("Invalid historical data received:", data);
                    if (typeof this.marketModel.setLoading === 'function') {
                        this.marketModel.setLoading(false);
                    }
                    if (this.viewModel && typeof this.viewModel.isUpdatingMarket !== 'undefined') {
                        this.viewModel.isUpdatingMarket = false;
                    }
                    return;
                }

                if (data.priceData && data.priceData.lastUpdated) {
                    this.marketModel.lastWebSocketUpdate = moment.utc(data.priceData.lastUpdated).toDate();
                }

                const candlesticks = data.candlesticks || {};
                const timeframes = ["minute", "hour", "day"];
                timeframes.forEach(interval => {
                    if (candlesticks[interval] && candlesticks[interval].data) {
                        this.marketModel.storeHistoricalData(data.marketTicker, interval, {
                            candlesticks: candlesticks[interval],
                            marketInfo: data.marketInfo,
                            priceData: data.priceData
                        });
                        // Log the last 20 candlesticks for this interval
                        const storedCandlesticks = this.marketModel.candlesticks[interval][data.marketTicker] || [];
                        const last20Candlesticks = storedCandlesticks.slice(-20).map(c => ({
                            timestamp: moment.utc(c.x).format('YYYY-MM-DD HH:mm:ss'),
                            x: c.x,
                            open: c.o,
                            high: c.h,
                            low: c.l,
                            close: c.c,
                            volume: c.v
                        }));
                        console.log(`Last 20 ${interval} candlesticks for ${data.marketTicker}:`, last20Candlesticks);
                    }
                });

                const currentInterval = (this.viewModel && typeof this.viewModel.getIntervalKey === 'function' ? this.viewModel.getIntervalKey(this.viewModel.currentTimeframe) : null) || "hour";

                if (typeof this.marketModel.isDataLoaded === 'function' && this.marketModel.isDataLoaded(data.marketTicker, currentInterval)) {
                    if (typeof this.marketModel.setLoading === 'function') {
                        this.marketModel.setLoading(false);
                    }
                    if (this.viewModel && typeof this.viewModel.isUpdatingMarket !== 'undefined') {
                        this.viewModel.isUpdatingMarket = false;
                    }
                } else {
                    console.warn(`Data not loaded for ${data.marketTicker} (${currentInterval}) after processing HistoricalData.`);
                }
            }
            if (this.viewModel) this.viewModel.onModelChanged();
        });

        // Restored marketModel-specific handlers
        this.connection.on("UpdatePositionPriceMetadata", data => {
            if (this.marketModel) {
                this.marketModel.updatePositionPriceMetadata(data.marketTicker, data.positions, true);
            }
            if (this.viewModel) this.viewModel.onModelChanged();
        });

        this.connection.on("OrderbookData", data => {
            if (this.marketModel) {
                this.marketModel.updateOrderbook(data, true);
            }
            if (this.viewModel) this.viewModel.onModelChanged();
        });

        this.connection.on("RealTimeData", data => {
            if (this.marketModel) {
                this.marketModel.updateRealTimeMetrics(data, true);
            }
            if (this.viewModel) this.viewModel.onModelChanged();
        });

        this.connection.on("UpdatePositions", data => {
            if (this.marketModel) {
                this.marketModel.updatePositions(data.marketTicker, data.positions, true);
            }
            if (this.viewModel) this.viewModel.onModelChanged();
        });

        this.connection.on("UpdateBalance", balance => {
            if (this.marketModel) this.marketModel.updateBalance(balance);
            if (this.viewModel) this.viewModel.onModelChanged();
        });

        this.connection.on("UpdatePositionsValue", value => {
            if (this.marketModel) this.marketModel.updatePositionsValue(value);
            if (this.viewModel) this.viewModel.onModelChanged();
        });

        this.connection.on("UpdateLastWebSocketUpdate", timestamp => {
            if (this.marketModel) this.marketModel.lastWebSocketUpdate = moment.utc(timestamp).toDate();
            if (this.viewModel) this.viewModel.onModelChanged();
        });

        // Handler for performance metrics, primarily used by PerformanceMonitorViewModel.
        this.connection.on("PerformanceMetrics", data => {
            if (this.viewModel && data) {
                console.log("Received PerformanceMetrics:", data); // Restored console.log
                this.viewModel.performanceMetrics = (data.metrics || []).map(metric => ({
                    ServiceName: metric.serviceName,
                    LastExecutionTime: metric.lastExecutionTime,
                    MarketCount: metric.marketCount,
                    UsagePercentage: metric.usagePercentage,
                    IsUsageAcceptable: metric.isUsageAcceptable,
                    Timestamp: metric.timestamp,
                    BrainInstance: metric.brainInstance || "Unknown" // Default if null/undefined
                }));
                this.viewModel.semaphoreStatus = data.semaphoreStatus || [];
                this.viewModel.timerStatus = data.timerStatus || [];
                this.viewModel.webSocketEventCounts = data.webSocketEventCounts || [];
                this.viewModel.broadcastCounts = data.broadcastCounts || [];
                this.viewModel.queueMetrics = data.queueMetrics || {};

                if (typeof this.viewModel.setLoading === 'function') {
                    this.viewModel.setLoading(false);
                } else {
                    this.viewModel.onModelChanged(); // Fallback if setLoading isn't on this viewModel type
                }
            }
        });
    }

    // Invokes a method on the SignalR hub.
    invoke(methodName, ...args) {
        if (this.connection.state === signalR.HubConnectionState.Connected) {
            return this.connection.invoke(methodName, ...args).catch(err => {
                console.error(`Error invoking hub method '${methodName}':`, err);
            });
        } else {
            console.warn(`Cannot invoke hub method '${methodName}': SignalR connection not established.`);
            return Promise.reject(new Error("SignalR connection not established."));
        }
    }
}