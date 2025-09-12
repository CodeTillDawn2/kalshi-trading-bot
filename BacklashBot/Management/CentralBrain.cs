// CentralBrain.cs
using KalshiBotData.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using BacklashDTOs.Configuration;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashInterfaces.SmokehouseBot.Services;
using BacklashDTOs;
using BacklashDTOs.Data;
using BacklashInterfaces.Constants;
using System.Diagnostics;
using TradingStrategies.Configuration;

namespace BacklashBot.Management
{
    /// <summary>
    /// Central orchestrator for the Kalshi trading bot system. Manages the complete bot lifecycle including
    /// startup, shutdown, market monitoring, snapshot creation, and error handling. Acts as the main
    /// coordination point between various services and components.
    /// </summary>
    /// <remarks>
    /// This class implements IHostedService and coordinates multiple responsibilities:
    /// - Service lifecycle management (start/stop)
    /// - Periodic market data snapshots
    /// - Brain instance management and locking
    /// - Market watchlist monitoring and adjustments
    /// - Error detection and recovery
    /// - Scheduled overnight maintenance tasks
    /// </remarks>
    public class CentralBrain : ICentralBrain
    {
        private readonly ILogger<ICentralBrain> _logger;
        private readonly IServiceFactory _serviceFactory;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMarketManagerService _marketManager;
        private readonly ICentralErrorHandler _errorHandler;
        private readonly ICentralPerformanceMonitor _performanceTracker;
        private readonly IBrainStatusService _brainStatus;
        private readonly SnapshotConfig _snapshotConfig;
        private readonly ExecutionConfig _executionConfig;
        private readonly TradingConfig _tradingConfig;
        private readonly CalculationConfig _calculationConfig;
        private readonly IScopeManagerService _scopeManagerService;
        private IStatusTrackerService _statusTrackerService;
        private IBotReadyStatus _readyStatus;
        private Timer? _snapshotTimer;
        private readonly SemaphoreSlim _snapshotLock = new SemaphoreSlim(1, 1);
        private readonly TimeSpan _decisionInterval;
        private bool SchemaVerified = false;
        private bool _servicesStopped = false;
        private bool _isStartingUp = false;
        private bool _isShuttingDown = false;
        public bool IsServicesStopped => _servicesStopped;
        public bool IsStartingUp => _isStartingUp;
        public bool IsShuttingDown => _isShuttingDown;

        private Timer _shutdownTimer;
        private Timer _startTimer;
        private static int _instanceCounter = 0;
        private readonly string _brainInstance;

        private Timer _errorCheckTimer;
        private bool _isReset = false;
        private Timer _overnightTimer;

        private static readonly TimeSpan OvernightStart = new TimeSpan(3, 0, 0); // 3:00 AM
        private static readonly TimeSpan OvernightTaskDelay = TimeSpan.FromMinutes(15); // 15 minutes after start

        private BrainInstanceDTO? _thisBrain = null;

        private PerformanceMetrics _performanceMetrics = new PerformanceMetrics();


        public CentralBrain(
            ILogger<ICentralBrain> logger,
            IServiceFactory serviceFactory,
            IServiceScopeFactory scopeFactory,
            IOptions<SnapshotConfig> snapshotConfig,
            IOptions<TradingConfig> tradingConfig,
            IOptions<ExecutionConfig> executionConfig,
            ICentralErrorHandler backlashErrorHandler,
            ICentralPerformanceMonitor backlashPerformanceTracker,
            IOptions<CalculationConfig> calculationConfig,
            IMarketManagerService marketManager,
            IHostApplicationLifetime appLifetime,
            IScopeManagerService scopeManagerService,
            IStatusTrackerService statusTrackerService,
            IBotReadyStatus readyStatus,
            IBrainStatusService brainStatusService)
        {
            _logger = logger;
            _scopeManagerService = scopeManagerService;
            _serviceFactory = serviceFactory;
            _scopeFactory = scopeFactory;
            _snapshotConfig = snapshotConfig.Value;
            _marketManager = marketManager;
            _brainStatus = brainStatusService;
            _errorHandler = backlashErrorHandler;
            _performanceTracker = backlashPerformanceTracker;
            _tradingConfig = tradingConfig.Value;
            _executionConfig = executionConfig.Value;
            _calculationConfig = calculationConfig.Value;
            _statusTrackerService = statusTrackerService;
            _readyStatus = readyStatus;
            _brainInstance = _executionConfig.BrainInstance;
            _decisionInterval = TimeSpan.FromSeconds(_tradingConfig.DecisionFrequencySeconds);

        }

        private bool IsWebSocketServiceRunning()
        {
            var webSocketService = _serviceFactory.GetWebSocketHostedService();
            return webSocketService?.IsConnected() ?? false;
        }

        /// <summary>
        /// Starts the CentralBrain service and initializes all dependent components.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method performs the following initialization steps:
        /// 1. Ensures brain status is initialized
        /// 2. Initializes brain instance management
        /// 3. Starts dashboard services if enabled
        /// 4. Sets up error monitoring timer
        /// 5. Begins the complete startup sequence
        /// </remarks>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _brainStatus.EnsureInitializedAsync();
                _logger.LogInformation("BRAIN: Created SmokehouseBrain instance {InstanceName}", _brainInstance);
                _logger.LogInformation("BRAIN: {InstanceName} Waking up, Session {1}", _brainInstance, _brainStatus.SessionIdentifier);
                InitializeBrain();
                if (!_executionConfig.LaunchDataDashboard)
                {
                    _logger.LogInformation("BRAIN: LaunchDataDashboard is false, skipping dashboard startup and daily timers.");
                    return;
                }
                _errorCheckTimer = new Timer(MonitorAndHandleErrors, null, TimeSpan.FromSeconds(.5), TimeSpan.FromSeconds(.5));

                _=Task.Run(() => CompleteStartupSequence(), cancellationToken);

                _logger.LogInformation("BRAIN: SmokehouseBrain running...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BRAIN: Error initializing SmokehouseBrain");
                throw;
            }
        }

        /// <summary>
        /// Stops the CentralBrain service and gracefully shuts down all dependent components.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method performs an orderly shutdown by:
        /// 1. Canceling all active operations
        /// 2. Waiting briefly for operations to complete
        /// 3. Shutting down the dashboard and all services
        /// 4. Logging the shutdown completion
        /// </remarks>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            File.AppendAllText("logs/stopasync_init.log", $"BRAIN: StopAsync called at {DateTime.UtcNow}. Host token cancelled: {cancellationToken.IsCancellationRequested}\n");
            _logger.LogInformation("BRAIN: StopAsync called at {0}", DateTime.UtcNow);
            try
            {
                _logger.LogInformation("BRAIN: Triggering ServiceFactory.CancelAll at {0}", DateTime.UtcNow);
                _statusTrackerService.CancelAll();
                _logger.LogInformation("BRAIN: ServiceFactory.CancelAll completed, CancellationToken.IsCancellationRequested={IsRequested}", _statusTrackerService.GetCancellationToken().IsCancellationRequested);
                await Task.Delay(TimeSpan.FromSeconds(3));
                await ShutdownDashboardAsync();
                _logger.LogInformation("BRAIN: Going to sleep at {0}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BRAIN: Error during StopAsync");
                throw;
            }
        }

        private async Task CompleteStartupSequence()
        {
            await StartDashboard();
            ConfigureScheduledTasks();
        }

        public async Task StartDashboard()
        {
            if (_isStartingUp)
            {
                _logger.LogInformation("BRAIN: StartDashboard aborted: already starting up.");
                return;
            }
            _isStartingUp = true;
            _performanceTracker.IsStartingUp = true;

            bool internetIsUp = await _errorHandler.CheckInternetConnection();
            if (!internetIsUp)
            {
                _isStartingUp = false;
                _performanceTracker.IsStartingUp = false;
                _logger.LogWarning("BRAIN: Brain did not start due to the internet being down.");
                _startTimer?.Dispose();
                _startTimer = new Timer(async _ =>
                {
                    _logger.LogDebug("BRAIN: Retry start timer triggered at {Now}", DateTimeOffset.Now);
                    if (IsServicesStopped && !IsStartingUp)
                    {
                        try
                        {
                            await StartDashboard();
                            _logger.LogInformation("BRAIN: Retry startup completed successfully");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "BRAIN: Error during retry start timer callback");
                        }
                    }
                    ConfigureScheduledTasks();
                }, null, TimeSpan.FromMinutes(5), Timeout.InfiniteTimeSpan);
                return;
            }

            if (!_isReset)
                _serviceFactory.ResetAll();

            _isReset = false;

            _serviceFactory.InitializeServices(_brainStatus.BrainLock);
            _logger.LogInformation("BRAIN: Starting Kalshi Bot Initialization...");
            try
            {
                var dataCache = _serviceFactory.GetDataCache();
                _statusTrackerService.ResetAll();
                _readyStatus.ResetAll();
                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();
                var snapshotService = _serviceFactory.GetTradingSnapshotService();
                var marketDataService = _serviceFactory.GetMarketDataService();
                snapshotService.ResetSnapshotTracking();
                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

                _thisBrain = await context.GetBrainInstanceByName(instanceName: _brainInstance);
                if (_thisBrain == null)
                    throw new Exception($"Brain instance was not found: {_brainInstance}");

                SchemaVerified = await snapshotService.ValidateSnapshotSchema();

                if (_thisBrain.WatchPositions || _thisBrain.WatchOrders)
                {
                    await marketDataService.RetrieveAndUpdatePositionsAsync();
                }
                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();
                // Load existing watched markets for this brain lock, sorted by interest score descending
                var watchedMarketsList = await context.GetMarketWatchesFiltered(
                    brainLocksIncluded: new HashSet<Guid>() { _brainStatus.BrainLock });
                var sortedWatchedMarkets = watchedMarketsList
                    .OrderByDescending(w => w.InterestScore ?? double.MinValue)
                    .ToList();
                HashSet<MarketWatchDTO> MarketsWatched = new HashSet<MarketWatchDTO>(sortedWatchedMarkets);

                HashSet<MarketWatchDTO> watchedList;
                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();
                if (_thisBrain.WatchPositions)
                {
                    List<MarketPositionDTO> positionMarkets = await context.GetMarketPositions(hasPosition: true, hasRestingOrder: false);
                    HashSet<string> positionMarketTickers = positionMarkets.Select(x => x.Ticker).ToHashSet();
                    watchedList = await context.GetMarketWatchesFiltered(marketTickers: positionMarketTickers, brainLockIsNull: true);
                    MarketsWatched = MarketsWatched.Union(watchedList).ToHashSet();
                    foreach (MarketWatchDTO existingWatch in watchedList)
                    {
                        existingWatch.LastWatched = DateTime.Now;
                        existingWatch.BrainLock = _brainStatus.BrainLock;
                    }
                    var newList = positionMarkets.Select(x => x.Ticker).Where(x => !MarketsWatched.Select(x => x.market_ticker)
                    .Contains(x)).ToList();
                    foreach (string position in newList)
                    {
                        MarketWatchDTO mw = new MarketWatchDTO() { market_ticker = position, BrainLock = _brainStatus.BrainLock, LastWatched = DateTime.Now };
                        watchedList.Add(mw);
                    }

                    foreach (MarketWatchDTO watch in watchedList)
                    {
                        await context.AddOrUpdateMarketWatch(watch);
                    }
                }
                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();
                if (_thisBrain.WatchOrders)
                {
                    List<MarketPositionDTO> restingOrdersMarkets = await context.GetMarketPositions(hasPosition: true, hasRestingOrder: false);
                    HashSet<string> restingTickers = restingOrdersMarkets.Select(x => x.Ticker).ToHashSet();

                    watchedList = await context.GetMarketWatchesFiltered(marketTickers: restingTickers, brainLockIsNull: true);
                    MarketsWatched = MarketsWatched.Union(watchedList).ToHashSet();
                    foreach (MarketWatchDTO existingWatch in watchedList)
                    {
                        existingWatch.LastWatched = DateTime.Now;
                        existingWatch.BrainLock = _brainStatus.BrainLock;
                    }
                    var newList = restingOrdersMarkets.Where(x => !MarketsWatched.Select(y => y.market_ticker)
                        .Contains(x.Ticker)).Select(x => x.Ticker).ToHashSet();
                    foreach (string restingOrderTicker in newList)
                    {
                        MarketWatchDTO mw = new MarketWatchDTO() { market_ticker = restingOrderTicker, BrainLock = _brainStatus.BrainLock, LastWatched = DateTime.Now };
                        watchedList.Add(mw);
                    }


                    foreach (MarketWatchDTO watch in watchedList)
                    {
                        await context.AddOrUpdateMarketWatch(watch);
                    }
                }
                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();


                foreach (MarketWatchDTO watch in MarketsWatched)
                {
                    if (watch.InterestScore == null || watch.InterestScoreDate <= DateTime.Now.AddHours(-12))
                    {
                        List<(string Ticker, double Score)> scores = await _serviceFactory.GetInterestScoreService().GetMarketInterestScores(_scopeFactory, new HashSet<string> { watch.market_ticker });
                        if (scores.Count > 0)
                        {
                            watch.InterestScore = scores[0].Score;
                            watch.InterestScoreDate = DateTime.Now;
                        }
                        await context.AddOrUpdateMarketWatch(watch);
                    }

                }

                _logger.LogDebug("BRAIN: Initial cache state: InitializationCompleted={IsCompleted}, BrowserReady={IsBrowserReady}",
                    _readyStatus.InitializationCompleted.Task.IsCompleted, _readyStatus.BrowserReady.Task.IsCompleted);

                if (_servicesStopped || (_readyStatus.InitializationCompleted.Task.IsCompleted && !_readyStatus.InitializationCompleted.Task.Result))
                {
                    _readyStatus.InitializationCompleted = new TaskCompletionSource<bool>();
                    _logger.LogDebug("BRAIN: Reset InitializationCompleted task");
                }
                if (_servicesStopped || !_readyStatus.BrowserReady.Task.IsCompleted || (_readyStatus.BrowserReady.Task.IsCompleted && !_readyStatus.BrowserReady.Task.Result))
                {
                    _readyStatus.BrowserReady = new TaskCompletionSource<bool>();
                    _logger.LogDebug("BRAIN: Reset BrowserReady task");
                }

                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                var orderBookService = _serviceFactory.GetOrderBookService();

                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                var webSocketService = _serviceFactory.GetWebSocketHostedService();
                if (_servicesStopped && IsWebSocketServiceRunning())
                {
                    _logger.LogDebug("BRAIN: Stopping WebSocketHostedService for restart...");
                    await webSocketService.ShutdownAsync(CancellationToken.None);
                    _logger.LogInformation("BRAIN: WebSocketHostedService stopped for restart");
                }

                if (_servicesStopped && IsMarketRefreshServiceRunning())
                {
                    _logger.LogDebug("BRAIN: Stopping MarketRefreshService for restart...");
                    await _serviceFactory.GetMarketRefreshService().StopAsync(CancellationToken.None);
                    _logger.LogInformation("BRAIN: MarketRefreshService stopped for restart");
                }

                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                _logger.LogDebug("BRAIN: Starting OrderBookService...");
                await orderBookService.StartServicesAsync();
                _logger.LogInformation("BRAIN: OrderBookService started");

                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                await webSocketService.TriggerConnectionCheckAsync();

                // Start WebSocket services BEFORE snapshot timer to ensure markets receive data
                _logger.LogDebug("BRAIN: Starting WebSocketHostedService...");
                webSocketService.StartServices(_statusTrackerService.GetCancellationToken());
               _logger.LogInformation("BRAIN: WebSocketHostedService started successfully");

                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                var webSocketClient = _serviceFactory.GetKalshiWebSocketClient();
                webSocketClient.EnableReconnect();
                _logger.LogInformation("Enabled WebSocket reconnection");

                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                _logger.LogInformation("BRAIN: Triggering WebSocket connection check...");
                await webSocketService.TriggerConnectionCheckAsync();
                _logger.LogInformation("BRAIN: Triggered WebSocket connection check");

                // Initialize snapshot timer early to start generating snapshots before market initialization completes
                _logger.LogInformation("BRAIN: Initializing snapshot timer early with 1-minute delay...");
                if (_snapshotTimer == null)
                {
                    _snapshotTimer = new Timer(
                        async state => await ExecuteSnapshotCycle(state),
                        null,
                        TimeSpan.FromMinutes(1),
                        _decisionInterval
                    );
                    _logger.LogInformation("BRAIN: Snapshot timer initialized early with 1-minute delay.");
                }
                else
                {
                    _logger.LogInformation("BRAIN: Snapshot timer already exists, skipping early initialization.");
                }

                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                var broadcastService = _serviceFactory.GetBroadcastService();
                _logger.LogDebug("BRAIN: Starting BroadcastService...");
                await broadcastService.StartServicesAsync();
                _logger.LogInformation("BRAIN: BroadcastService started");

                var overseerClientService = _serviceFactory.GetOverseerClientService();
                _logger.LogDebug("OVERSEER- Starting OverseerClientService...");
                await overseerClientService.StartAsync();
                _logger.LogInformation("OVERSEER- OverseerClientService started");

                var marketInitializer = _serviceFactory.GetMarketDataInitializer();
                _logger.LogDebug("BRAIN: Starting MarketDataInitializer.SetupAsync...");
                await marketInitializer.SetupAsync();
                _logger.LogInformation("BRAIN: Market data initialization completed");

                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                // Now that initialization is complete, connect the WebSocket directly
                _logger.LogInformation("BRAIN: Connecting WebSocket now that initialization is complete...");
                await webSocketClient.ConnectAsync();
                _logger.LogInformation("BRAIN: WebSocket connected successfully");

                _logger.LogDebug("Starting MarketRefreshService...");
                _serviceFactory.GetMarketRefreshService().ExecuteServicesAsync(_statusTrackerService.GetCancellationToken());
                _logger.LogInformation("MarketRefreshService started");


                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                if (!_readyStatus.BrowserReady.Task.IsCompleted)
                {
                    _readyStatus.BrowserReady.SetResult(true);
                    _logger.LogInformation("BRAIN: Set BrowserReady task to true");
                }
                _servicesStopped = false;

                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                _errorHandler.CatastrophicErrorAlreadyDetected = false;

                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                await _performanceTracker.StartTimer();

                _logger.LogInformation("BRAIN: Data Dashboard version {version} started successfully", dataCache.SoftwareVersion);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("StartDashboard was cancelled");
                _readyStatus.InitializationCompleted.TrySetResult(false);
                await ShutdownDashboardAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting dashboard");
                var dataCache = _serviceFactory.GetDataCache();
                if (!_readyStatus.BrowserReady.Task.IsCompleted)
                {
                    _readyStatus.BrowserReady.SetResult(false);
                    _logger.LogWarning("Set BrowserReady task to false due to error");
                }
                throw;
            }
            finally
            {
                _logger.LogInformation("BRAIN: Dashboard startup sequence completed successfully");
                _isStartingUp = false;
                _performanceTracker.IsStartingUp = false;
            }
        }

        public async Task ShutdownDashboardAsync()
        {
            File.AppendAllText("logs/shutodown.log", $"{DateTime.Now.ToString()} - Shutting down\n");
            if (_isShuttingDown)
            {
                _logger.LogInformation("BRAIN: ShutdownServicesAsync aborted: already shutting down.");
                return;
            }
            if (_servicesStopped)
            {
                _logger.LogInformation("BRAIN: ShutdownServicesAsync aborted: services have already been stopped.");
                return;
            }
            _isShuttingDown = true;
            _performanceTracker.IsShuttingDown = true;
            _logger.LogDebug("Shutting down dependent services...");
            try
            {
                _logger.LogDebug("BRAIN: Shutting down Dashboard for {InstanceName}", _brainInstance);
                _statusTrackerService.CancelAll();

                if (_snapshotTimer != null)
                {
                    _snapshotTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    _snapshotTimer.Dispose();
                    _snapshotTimer = null;
                    _logger.LogDebug("BRAIN: Snapshot timer stopped and disposed");
                }

                _errorHandler.LastSuccessfulSnapshot = DateTime.MinValue;

                var webSocketService = _serviceFactory.GetWebSocketHostedService();
                _logger.LogDebug("Stopping WebSocketHostedService...");
                await webSocketService.ShutdownAsync(CancellationToken.None);
                _logger.LogDebug("WebSocketHostedService stopped");

                _logger.LogDebug("Stopping MarketRefreshService...");
                await _serviceFactory.GetMarketRefreshService().StopAsync(CancellationToken.None);
                _logger.LogDebug("MarketRefreshService stopped");

                var marketDataService = _serviceFactory.GetMarketDataService();
                _logger.LogDebug("Stopping MarketDataService...");
                marketDataService.StopServicesAsync();
                _logger.LogDebug("MarketDataService stopped");

                var broadcastService = _serviceFactory.GetBroadcastService();
                _logger.LogDebug("Stopping BroadcastService...");
                await broadcastService.StopServicesAsync();
                _logger.LogDebug("BroadcastService stopped");

                var overseerClientService = _serviceFactory.GetOverseerClientService();
                _logger.LogDebug("OVERSEER- Stopping OverseerClientService...");
                await overseerClientService.StopAsync();
                _logger.LogDebug("OVERSEER- OverseerClientService stopped");

                var orderBookService = _serviceFactory.GetOrderBookService();
                _logger.LogDebug("Stopping OrderBookService...");
                await orderBookService.StopServicesAsync();
                _logger.LogDebug("OrderBookService stopped");

                var dataCache = _serviceFactory.GetDataCache();
                foreach (var marketData in dataCache.Markets.Values)
                {
                    marketData.ChangeTracker?.Stop();
                    marketData.ChangeTracker?.Dispose();
                    _logger.LogDebug("Disposed OrderbookChangeTracker for market {MarketTicker}", marketData.MarketTicker);
                }

                dataCache.Markets.Clear();
                dataCache.WatchedMarkets.Clear();
                _logger.LogDebug("Cleared DataCache");

                if (!_readyStatus.InitializationCompleted.Task.IsCompleted)
                {
                    _readyStatus.InitializationCompleted.SetResult(false);
                    _logger.LogDebug("Set InitializationCompleted task to false");
                }
                else
                {
                    _readyStatus.InitializationCompleted = new TaskCompletionSource<bool>();
                    _readyStatus.InitializationCompleted.SetResult(false);
                    _logger.LogDebug("Reset and set InitializationCompleted task to false");
                }

                if (!_readyStatus.BrowserReady.Task.IsCompleted)
                {
                    _readyStatus.BrowserReady.SetResult(false);
                    _logger.LogDebug("Set BrowserReady task to false");
                }
                else
                {
                    _readyStatus.BrowserReady = new TaskCompletionSource<bool>();
                    _readyStatus.BrowserReady.SetResult(false);
                    _logger.LogDebug("Reset and set BrowserReady task to false");
                }

                var webSocketClient = _serviceFactory.GetKalshiWebSocketClient();
                webSocketClient.DisableReconnect();
                _logger.LogDebug("Disabled WebSocket reconnection");

                _servicesStopped = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error shutting down dependent services");
                throw;
            }
            finally
            {
                _isShuttingDown = false;
                _performanceTracker.IsShuttingDown = false;
                if (!_isReset)
                {
                    _serviceFactory.ResetAll();
                    _isReset = true;
                }
            }
            _logger.LogInformation("BRAIN: Dependent services shut down successfully");
        }

        public void Dispose()
        {
            _snapshotTimer?.Dispose();
            _shutdownTimer?.Dispose();
            _startTimer?.Dispose();
            _overnightTimer?.Dispose();
            _errorCheckTimer?.Dispose();
            _serviceFactory.Dispose();
        }

        private void InitializeBrain()
        {
            InitializeOrUpdateBrainInstance();
            _logger.LogDebug("BRAIN: Checking snapshot schema...");
        }

        private async void InitializeOrUpdateBrainInstance()
        {
            _logger.LogDebug("BRAIN: Checking in...");
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

            _thisBrain = await context.GetBrainInstance(instanceName: _brainInstance);
            UpdateBrainInstanceStatus(_thisBrain);
            CleanupStaleBrainLocks(_thisBrain);
        }

        private async void UpdateBrainInstanceStatus(BrainInstanceDTO? brainInstance)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

            if (brainInstance == null)
            {
                brainInstance = new BrainInstanceDTO() { BrainInstanceName = _brainInstance, BrainLock = Guid.NewGuid() };
            }
            else
            {
                if (brainInstance.BrainLock == null)
                {
                    brainInstance.BrainLock = Guid.NewGuid();
                }
            }
            brainInstance.LastSeen = DateTime.Now;
            await context.AddOrUpdateBrainInstance(brainInstance);
        }

        private async void CleanupStaleBrainLocks(BrainInstanceDTO? brainInstance)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

            List<BrainInstanceDTO> staleBrains = await context.GetStaleBrains(_brainStatus.BrainLock);

            HashSet<Guid> staleBrainLocks = staleBrains.Where(x => x.BrainLock != null).Select(x => x.BrainLock.Value).ToHashSet();

            List<BrainInstanceDTO> allBrainInstancesWithLock = await context.GetBrainInstancesFiltered(hasBrainLock: true);
            HashSet<Guid> AllBrainLocks = allBrainInstancesWithLock.Select(x => x.BrainLock.Value).ToHashSet();

            foreach (BrainInstanceDTO staleBrain in staleBrains)
            {
                try
                {
                    _logger.LogInformation("BRAIN: Resetting stale locks for {staleBrain}", staleBrain.BrainInstanceName);
                    staleBrain.BrainLock = null;
                    await context.AddOrUpdateBrainInstance(staleBrain);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error resetting stale locks for {0}. Exception: {1}, Stack Trace {2}, Inner Exception: {3}",
                        staleBrain.BrainInstanceName, ex.Message, ex.StackTrace, ex.InnerException != null ? ex.InnerException.Message : "");
                }

            }

            foreach (Guid staleBrainLock in staleBrainLocks)
            {
                try
                {
                    var staleLocks = await context.GetMarketWatchesFiltered(brainLocksIncluded: new HashSet<Guid>() { staleBrainLock });
                    foreach (MarketWatchDTO staleLock in staleLocks)
                    {
                        staleLock.BrainLock = null;
                        await context.AddOrUpdateMarketWatch(staleLock);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error clearing stale brain lock {0}. Exception: {1}, Stack Trace {2}, Inner Exception: {3}",
                        staleBrainLock, ex.Message, ex.StackTrace, ex.InnerException != null ? ex.InnerException.Message : "");
                }

            }

            var myWatches = await context.GetMarketWatchesFiltered(brainLocksIncluded: new HashSet<Guid>() { _brainStatus.BrainLock });

            foreach (MarketWatchDTO myWatch in myWatches)
            {
                try
                {
                    myWatch.LastWatched = DateTime.Now;
                    if (!_servicesStopped || !_isShuttingDown && !_isStartingUp)
                    {
                        myWatch.AverageWebsocketEventsPerMinute = _performanceTracker.CalculateAverageWebsocketEventsReceived(myWatch.market_ticker);
                    }
                    else
                    {
                        myWatch.AverageWebsocketEventsPerMinute = 0;
                    }
                    await context.AddOrUpdateMarketWatch(myWatch);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error calculating average events per minute for market {0}. Exception: {1}, Stack Trace {2}, Inner Exception: {3}",
                        myWatch.market_ticker, ex.Message, ex.StackTrace, ex.InnerException != null ? ex.InnerException.Message : "");
                }
            }

            HashSet<MarketWatchDTO> orphanedWatches = await context.GetMarketWatchesFiltered(
                brainLocksExcluded: AllBrainLocks, brainLockIsNull: false);

            foreach (MarketWatchDTO orphanedWatch in orphanedWatches)
            {
                orphanedWatch.BrainLock = null;
            }
        }

        private bool AreConditionsMetForSnapshot()
        {
            var cancellationToken = _statusTrackerService.GetCancellationToken();
            if (IsServicesStopped
                || IsShuttingDown
                || cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Data is not ready because one of the following conditions is met: " +
                    "IsServicesStopped={IsServicesStopped}, IsShuttingDown={IsShuttingDown}, CancellationToken.IsCancellationRequested={IsCancellationRequested}",
                    IsServicesStopped, IsShuttingDown, cancellationToken.IsCancellationRequested);
                return false;
            }
            if (_serviceFactory.GetDataCache().TradingStatus == false) return false;
            if (!_serviceFactory.GetOrderBookService().IsEventQueueUnderLimit(150))
            {
                _logger.LogInformation("BRAIN: Waiting for queues to settle.");
                return false;
            }
            return true;
        }

        private async Task ExecuteSnapshotCycle(object state)
        {
            try
            {
                var cancellationToken = _statusTrackerService.GetCancellationToken();
                if (cancellationToken.IsCancellationRequested && _snapshotTimer != null)
                {
                    _logger.LogDebug("BRAIN: Snapshot timer callback cancelled due to global cancellation token.");
                    _snapshotTimer?.Dispose();
                    _snapshotTimer = null;
                    return;
                }

                // Check if WebSocket is connected before proceeding with snapshots
                if (!IsWebSocketServiceRunning())
                {
                    _logger.LogDebug("BRAIN: Skipping snapshot creation - WebSocket not connected yet.");
                    ResetPerformanceMetrics();
                    return;
                }

                InitializeOrUpdateBrainInstance();
                if (AreConditionsMetForSnapshot())
                {
                    UpdatePerformanceMetrics();
                    await GenerateMarketSnapshots();
                    await _marketManager.HandleMarketResets(); ;
                    await _marketManager.MonitorWatchList(_thisBrain, _performanceMetrics);
                }
                else
                {
                    ResetPerformanceMetrics();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "BRAIN: Error in snapshot timer callback: {Message}", ex.Message);
            }

        }

        private async Task GenerateMarketSnapshots()
        {

            _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

            CacheSnapshot? allSnapshots = null;
            var stopwatch = Stopwatch.StartNew();
            int expectedSnapshotLength = 30;
            bool lockAcquired = await _snapshotLock.WaitAsync(TimeSpan.FromSeconds(expectedSnapshotLength));
            if (!lockAcquired)
            {
                _logger.LogWarning($"BRAIN: Timeout waiting for snapshot lock after {expectedSnapshotLength} seconds.");
                return;
            }

            try
            {
                var cancellationToken = _statusTrackerService.GetCancellationToken();
                cancellationToken.ThrowIfCancellationRequested();
                DateTime snapshotDate = DateTime.Now;

                // Step 1: Filter watched markets explicitly
                var watchedMarketsData = new List<KeyValuePair<string, IMarketData>>();
                lock (_serviceFactory.GetDataCache().Markets)
                {
                    foreach (var kvp in _serviceFactory.GetDataCache().Markets)
                    {
                        if (_serviceFactory.GetDataCache().WatchedMarkets.Contains(kvp.Key))
                        {
                            watchedMarketsData.Add(kvp);
                        }
                    }
                }

                // Step 2: Create snapshots for each watched market in parallel for better performance
                var marketSnapshots = new List<MarketSnapshot>();
                var snapshotTasks = new List<Task<MarketSnapshot?>>();

                foreach (var kvp in watchedMarketsData)
                {
                    if (!kvp.Value.ReceivedFirstSnapshot) continue;

                    // Create snapshot task for parallel processing
                    var snapshotTask = Task.Run(() =>
                    {
                        try
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            kvp.Value.RefreshTickerMetadata();
                            kvp.Value.RecalculateOrderbookChangeMetrics();
                            var marketSnapshot = CreateMarketSnapshot(snapshotDate, kvp);
                            kvp.Value.LastSnapshotTaken = DateTime.UtcNow;
                            return marketSnapshot;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error creating snapshot for market {MarketTicker}", kvp.Key);
                            return null;
                        }
                    }, cancellationToken);

                    snapshotTasks.Add(snapshotTask);
                }

                _logger.LogDebug("BRAIN: Starting parallel snapshot creation for {Count} markets", watchedMarketsData.Count);

                // Wait for all snapshot tasks to complete
                var completedSnapshots = await Task.WhenAll(snapshotTasks);

                // Filter out null results and add to the list
                foreach (var snapshot in completedSnapshots)
                {
                    if (snapshot != null)
                    {
                        marketSnapshots.Add(snapshot);
                    }
                }

                _logger.LogDebug("BRAIN: Completed parallel snapshot creation, processed {Count} markets successfully", marketSnapshots.Count);

                cancellationToken.ThrowIfCancellationRequested();
                allSnapshots = new CacheSnapshot(snapshotDate, _serviceFactory.GetDataCache().SoftwareVersion, _snapshotConfig.SnapshotSchemaVersion,
                    _serviceFactory.GetDataCache().AccountBalance, _serviceFactory.GetDataCache().PortfolioValue, _serviceFactory.GetDataCache().LastWebSocketTimestamp, marketSnapshots);


                if (_thisBrain.CaptureSnapshots)
                {
                    double percentUsage = 0;
                    if (_performanceTracker.LastPerformanceSampleDate != null) percentUsage = _performanceTracker.LastRefreshUsagePercentage;

                    var snapshotService = _serviceFactory.GetTradingSnapshotService();
                    cancellationToken.ThrowIfCancellationRequested();
                    List<string> snapshotsSaved = await snapshotService.SaveSnapshotAsync(_brainInstance, allSnapshots);
                    int savedSnapshotCount = snapshotsSaved.Count();
                    if (savedSnapshotCount > 0) _errorHandler.LastSuccessfulSnapshot = DateTime.Now;

                    _logger.LogInformation("BRAIN: {count} snapshots saved at {Timestamp}, Refresh Usage: {usage}%, Queue Usage {queue}%, EventQueue: {eventQueue}, TickerQueue: {tickerQueue}, NotificationQueue: {notificationQueue}, Orderbook Queue: {orderbookQueue}",
                        savedSnapshotCount, allSnapshots.Timestamp, Math.Round(percentUsage, 1), Math.Round(_performanceTracker.GetQueueHighCountPercentage(), 1), Math.Round(_performanceMetrics.EventQueueAvg, 1), Math.Round(_performanceMetrics.TickerQueueAvg, 1), Math.Round(_performanceMetrics.NotificationQueueAvg, 1), Math.Round(_performanceMetrics.OrderbookQueueAvg, 1));
                }
                else
                {
                    _logger.LogDebug("BRAIN: Snapshot not saved: snapshots disabled in configuration.");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("BRAIN: Snapshot creation cancelled");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BRAIN: Error creating snapshot");
                return;
            }
            finally
            {
                _snapshotLock.Release();
                stopwatch.Stop();
                int maxMillis = 30000;
                if (stopwatch.ElapsedMilliseconds > maxMillis)
                {
                    _logger.LogWarning("BRAIN: Snapshot lock held for {Elapsed} ms, exceeding threshold of {maxMillis} ms.", stopwatch.ElapsedMilliseconds, maxMillis);
                }
            }

            try
            {
                _logger.LogDebug("BRAIN: Triggering analysis for snapshot at {Timestamp}, Markets {0}", allSnapshots.Timestamp, String.Join(",", allSnapshots.Markets));
                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();
                ProcessSnapshotAnalysis(allSnapshots);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("BRAIN: Snapshot analysis cancelled");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BRAIN: Error analyzing snapshot");
                return;
            }
        }

        // Extracted method for creating a single market snapshot (simplifies the main method)
        private MarketSnapshot CreateMarketSnapshot(DateTime snapshotDate, KeyValuePair<string, IMarketData> kvp)
        {
            // Handle OrderbookData with null check
            var orderbookData = kvp.Value.OrderbookData ?? new List<OrderbookData>();
            var orderbookDataDicts = orderbookData.Select(od => new Dictionary<string, object>
                {
                    { "price", od.Price },
                    { "side", od.Side },
                    { "resting_contracts", od.RestingContracts },
                    { "last_modified_date", od.LastModifiedDate }
                }).ToList();

            // Handle RestingOrders with null check (this fixes the null exception)
            var restingOrders = kvp.Value.RestingOrders ?? new List<OrderDTO>();
            var restingOrdersTuples = restingOrders.Select(ord => (
                action: ord.Action,
                side: ord.Side,
                type: ord.Type,
                count: ord.RemainingCount,
                price: ord.Side == "yes" ? ord.YesPrice : ord.NoPrice,
                expiration: ord.ExpirationTimeUTC
            )).ToList();

            // Log for debugging if needed
            _logger.LogDebug("Created snapshot for market {Ticker}: OrderbookData count={OrderbookCount}, RestingOrders count={RestingCount}",
                kvp.Key, orderbookDataDicts.Count, restingOrdersTuples.Count);

            long totalYesDepth = 0;
            long totalNoDepth = 0;

            foreach (var level in kvp.Value.OrderbookData)
            {
                if (level.Side == "yes")
                {
                    totalYesDepth += level.RestingContracts * level.Price;
                }
                else
                {
                    totalNoDepth += level.RestingContracts * level.Price;
                }
            }

            return new MarketSnapshot(
               snapshotDate,
               kvp.Value.MarketTicker,
               kvp.Value.MarketCategory,
               kvp.Value.MarketStatus,
               kvp.Value.MarketType,
               kvp.Value.BestYesBid,
               kvp.Value.BestNoBid,
               orderbookDataDicts,
               kvp.Value.AllTimeHighYes_Bid,
               kvp.Value.AllTimeLowYes_Bid,
               kvp.Value.AllTimeHighNo_Bid,
               kvp.Value.AllTimeLowNo_Bid,
               kvp.Value.RecentHighYes_Bid,
               kvp.Value.RecentLowYes_Bid,
               kvp.Value.RecentHighNo_Bid,
               kvp.Value.RecentLowNo_Bid,
               kvp.Value.AllSupportResistanceLevels,
               kvp.Value.PositionSize,
               kvp.Value.MarketExposure,
               kvp.Value.BuyinPrice,
               kvp.Value.PositionUpside,
               kvp.Value.PositionDownside,
               kvp.Value.TotalPositionTraded,
               restingOrdersTuples,
               kvp.Value.RealizedPnl,
               kvp.Value.FeesPaid,
               kvp.Value.PositionROI,
               kvp.Value.ExpectedFees,
               kvp.Value.PositionROIAmt,
               kvp.Value.TradeRatePerMinute_Yes,
               kvp.Value.TradeRatePerMinute_No,
               kvp.Value.TradeVolumePerMinute_Yes,
               kvp.Value.TradeVolumePerMinute_No,
               kvp.Value.TradeCount_Yes,
               kvp.Value.TradeCount_No,
               kvp.Value.OrderVolumePerMinute_YesBid,
               kvp.Value.OrderVolumePerMinute_NoBid,
               kvp.Value.NonTradeRelatedOrderCount_Yes,
               kvp.Value.NonTradeRelatedOrderCount_No,
               kvp.Value.AverageTradeSize_Yes,
               kvp.Value.AverageTradeSize_No,
               kvp.Value.HighestVolume_Day,
               kvp.Value.HighestVolume_Hour,
               kvp.Value.HighestVolume_Minute,
               kvp.Value.RecentVolume_LastHour,
               kvp.Value.RecentVolume_LastThreeHours,
               kvp.Value.RecentVolume_LastMonth,
               kvp.Value.VelocityPerMinute_Bottom_Yes_Bid,
               kvp.Value.LevelCount_Bottom_Yes_Bid,
               kvp.Value.VelocityPerMinute_Bottom_No_Bid,
               kvp.Value.LevelCount_Bottom_No_Bid,
               kvp.Value.VelocityPerMinute_Top_Yes_Bid,
               kvp.Value.LevelCount_Top_Yes_Bid,
               kvp.Value.VelocityPerMinute_Top_No_Bid,
               kvp.Value.LevelCount_Top_No_Bid,
               kvp.Value.YesSpread,
               kvp.Value.NoSpread,
               kvp.Value.DepthAtBestYesBid,
               kvp.Value.DepthAtBestNoBid,
               kvp.Value.TopTenPercentLevelDepth_Yes,
               kvp.Value.TopTenPercentLevelDepth_No,
               kvp.Value.BidRange_Yes,
               kvp.Value.BidRange_No,
               kvp.Value.TotalBidContracts_Yes,
               kvp.Value.TotalBidContracts_No,
               kvp.Value.BidCountImbalance,
               kvp.Value.BidVolumeImbalance,
               kvp.Value.DepthAtTop4YesBids,
               kvp.Value.DepthAtTop4NoBids,
               kvp.Value.RSI_Short,
               kvp.Value.RSI_Medium,
               kvp.Value.RSI_Long,
               kvp.Value.MACD_Medium,
               kvp.Value.MACD_Long,
               kvp.Value.EMA_Medium,
               kvp.Value.EMA_Long,
               kvp.Value.BollingerBands_Medium,
               kvp.Value.BollingerBands_Long,
               kvp.Value.ATR_Medium,
               kvp.Value.ATR_Long,
               kvp.Value.VWAP_Short,
               kvp.Value.VWAP_Medium,
               kvp.Value.StochasticOscillator_Short,
               kvp.Value.StochasticOscillator_Medium,
               kvp.Value.StochasticOscillator_Long,
               kvp.Value.OBV_Medium,
               kvp.Value.OBV_Long,
               kvp.Value.ChangeMetricsMature,
               kvp.Value.MarketAge,
               kvp.Value.TimeLeft,
               kvp.Value.CanCloseEarly,
               kvp.Value.LastWebSocketMessageReceived,
               kvp.Value.MarketBehaviorYes,
               kvp.Value.MarketBehaviorNo,
               kvp.Value.GoodBadPriceYes,
               kvp.Value.GoodBadPriceNo,
               kvp.Value.HoldTime,
               kvp.Value.YesBidCenterOfMass,
               kvp.Value.NoBidCenterOfMass,
               kvp.Value.TolerancePercentage,
               _snapshotConfig.SnapshotSchemaVersion,
               totalYesDepth,
               totalNoDepth,
               kvp.Value.TotalBidVolume_Yes,
               kvp.Value.TotalBidVolume_No,
               kvp.Value.YesBidSlopePerMinute_Short,
               kvp.Value.NoBidSlopePerMinute_Short,
               kvp.Value.YesBidSlopePerMinute_Medium,
               kvp.Value.NoBidSlopePerMinute_Medium,
               kvp.Value.PSAR,
               kvp.Value.ADX,
               kvp.Value.PlusDI,
               kvp.Value.MinusDI,
               kvp.Value.CurrentTradeRatePerMinute_Yes,
               kvp.Value.CurrentTradeRatePerMinute_No,
               kvp.Value.CurrentTradeVolumePerMinute_Yes,
               kvp.Value.CurrentTradeVolumePerMinute_No,
               kvp.Value.CurrentTradeCount_Yes,
               kvp.Value.CurrentTradeCount_No,
               kvp.Value.CurrentOrderVolumePerMinute_YesBid,
               kvp.Value.CurrentOrderVolumePerMinute_NoBid,
               kvp.Value.CurrentNonTradeRelatedOrderCount_Yes,
               kvp.Value.CurrentNonTradeRelatedOrderCount_No,
               kvp.Value.CurrentAverageTradeSize_Yes,
               kvp.Value.CurrentAverageTradeSize_No,
               kvp.Value.RecentCandlesticks
           );

        }


        private void ProcessSnapshotAnalysis(CacheSnapshot snapshot)
        {
            try
            {
                var cancellationToken = _statusTrackerService.GetCancellationToken();
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("BRAIN: Analyzing markets with snapshot from {Timestamp}", snapshot.Timestamp);
                var marketDataService = _serviceFactory.GetMarketDataService();
                foreach (MarketSnapshot marketSnapshot in snapshot.Markets.Values)
                {
                    _logger.LogDebug("BRAIN: Analyzing market {MarketTicker} with snapshot from {Timestamp}", marketSnapshot.MarketTicker, snapshot.Timestamp);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (marketSnapshot != null)
                    {
                        CheckMarketClosureConditions(marketSnapshot);
                        PerformMarketAnalysis(marketSnapshot);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("AnalyzeMarketsAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze markets with snapshot from {Timestamp}", snapshot.Timestamp);
            }
        }

        private void CheckMarketClosureConditions(MarketSnapshot snapshot)
        {
            // Only consider market closed if we have received WebSocket data AND orderbook data but depth is still 0
            // This prevents false positives when WebSocket subscriptions are delayed or orderbook is empty
            var marketData = _serviceFactory.GetMarketDataService().GetMarketDetails(snapshot.MarketTicker);
            bool hasReceivedWebSocketData = marketData != null &&
                (marketData.LastWebSocketMessageReceived > DateTime.MinValue ||
                 marketData.Tickers.Any());
            bool hasOrderbookData = marketData != null && marketData.OrderbookData.Any();

            // Only trigger reset if we have WebSocket data AND orderbook data but depth is still 0 AND first snapshot has been received
            bool shouldResetForEmptyDepth = snapshot.DepthAtBestNoBid == 0
                && snapshot.DepthAtBestYesBid == 0
                && snapshot.CanCloseEarly == true
                && hasReceivedWebSocketData
                && hasOrderbookData
                && marketData.ReceivedFirstSnapshot;

            if (shouldResetForEmptyDepth
                || snapshot.TimeLeft == null
                || snapshot.TimeLeft.Value.Ticks < 0)
            {
                _logger.LogInformation("BRAIN: Market {0} seems like it is closed. Resetting. DepthNo={1}, DepthYes={2}, CanCloseEarly={3}, TimeLeft={4}, HasWebSocketData={5}, HasOrderbookData={6}, ReceivedFirstSnapshot={7}",
                    snapshot.MarketTicker, snapshot.DepthAtBestNoBid, snapshot.DepthAtBestYesBid, snapshot.CanCloseEarly, snapshot.TimeLeft, hasReceivedWebSocketData, hasOrderbookData, marketData?.ReceivedFirstSnapshot ?? false);
                _marketManager.TriggerMarketReset(snapshot.MarketTicker);
            }
        }

        private void PerformMarketAnalysis(MarketSnapshot marketSnapshot)
        {
            string marketTicker = marketSnapshot.MarketTicker;
            try
            {
                var cancellationToken = _statusTrackerService.GetCancellationToken();
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("BRAIN: Analyzing market {MarketTicker} with snapshot from {Timestamp}", marketTicker, marketSnapshot.Timestamp);

                var marketDataService = _serviceFactory.GetMarketDataService();
                var marketData = marketDataService?.GetMarketDetails(marketTicker);
                if (marketData == null || marketData.MarketInfo == null ||
                    marketData.MarketInfo.status != KalshiConstants.Status_Active)
                {
                    _logger.LogDebug("BRAIN: Skipping analysis for {MarketTicker}: market inactive or not found.", marketTicker);
                    return;
                }

                if (marketSnapshot == null)
                {
                    _logger.LogWarning("BRAIN: No snapshot data found for {MarketTicker} at {Timestamp}.", marketTicker, marketSnapshot?.Timestamp);
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("AnalyzeMarketAsync was cancelled for {MarketTicker}", marketTicker);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze market {MarketTicker} with snapshot from {Timestamp}", marketTicker, marketSnapshot.Timestamp);
            }
        }

        private bool IsMarketRefreshServiceRunning()
        {
            return _serviceFactory.GetMarketRefreshService().IsRunning();
        }

        private void ConfigureScheduledTasks()
        {
            if (IsServicesStopped)
            {
                _logger.LogInformation("BRAIN: Skipping SetupDailyTimers: services are stopped");
                return;
            }

            var now = DateTimeOffset.Now;
            var today = now.Date;
            var overnightTaskTime = new DateTimeOffset(today.Year, today.Month, today.Day, OvernightStart.Hours, OvernightStart.Minutes, OvernightStart.Seconds, now.Offset);

            if (now > overnightTaskTime) overnightTaskTime = overnightTaskTime.AddDays(1);

            var overnightTaskDelay = overnightTaskTime - now;

            _logger.LogInformation("BRAIN: Setting up timers. Now={Now}, OvernightTaskTime={OvernightTaskTime}, OvernightTaskDelay={OvernightTaskDelay}s",
                now, overnightTaskTime, overnightTaskDelay.TotalSeconds);

            _shutdownTimer?.Dispose();
            _startTimer?.Dispose();
            _overnightTimer?.Dispose();

            if (_executionConfig.RunOvernightActivities)
            {
                _overnightTimer = new Timer(async _ =>
                {
                    _logger.LogInformation("BRAIN: Overnight tasks timer triggered at {Now}", DateTimeOffset.Now);
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var overnightService = scope.ServiceProvider.GetRequiredService<IOvernightActivitiesHelper>();
                        await overnightService.RunOvernightTasks(_scopeFactory, new CancellationToken());
                        _logger.LogInformation("BRAIN: Overnight tasks completed successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "BRAIN: Error during overnight tasks");
                    }
                    finally
                    {
                        ConfigureScheduledTasks();
                    }
                }, null, overnightTaskDelay, Timeout.InfiniteTimeSpan);
            }
        }

        private async void MonitorAndHandleErrors(object state)
        {
            if (await _errorHandler.HandleErrors())
            {
                await ShutdownDashboardAsync();
                _logger.LogInformation("BRAIN: Delaying 5 seconds before attempting restart.");
                await Task.Delay(5000);
                _logger.LogInformation("BRAIN: Attempting restart.");
                await StartDashboard();
                _logger.LogInformation("BRAIN: Restarted.");
            }
        }

        private void UpdatePerformanceMetrics()
        {
            var (eventQueueAvg, tickerQueueAvg, notificationQueueAvg, orderBookQueueAvg) = _performanceTracker.GetQueueCountRollingAverages();
            _performanceMetrics.EventQueueAvg = eventQueueAvg;
            _performanceMetrics.TickerQueueAvg = tickerQueueAvg;
            _performanceMetrics.NotificationQueueAvg = notificationQueueAvg;
            _performanceMetrics.OrderbookQueueAvg = orderBookQueueAvg;
            _performanceMetrics.CurrentUsage = _performanceTracker.LastRefreshUsagePercentage;
            _performanceMetrics.CurrentCount = _performanceTracker.LastRefreshMarketCount;
        }

        private void ResetPerformanceMetrics()
        {
            _performanceMetrics.EventQueueAvg = 0;
            _performanceMetrics.TickerQueueAvg = 0;
            _performanceMetrics.NotificationQueueAvg = 0;
            _performanceMetrics.OrderbookQueueAvg = 0;
            _performanceMetrics.CurrentUsage = 0;
            _performanceMetrics.CurrentCount = 0;
        }
    }
}
