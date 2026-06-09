
BacklashBot

BacklashBot is a multi-service automated trading system for the Kalshi prediction market platform. It connects to Kalshi's REST and WebSocket APIs in real time, ingests live order book and market data,
applies a library of configurable technical analysis strategies to identify trade opportunities, and manages order execution — all while supporting backtesting against historical snapshot data to evaluate
and iterate on strategies.

The system is designed for a distributed, multi-machine deployment: a central data server stores candlestick and snapshot data on a shared network drive, one or more bot instances run independently and
check in with an Overseer service, and operators interact with a Windows Forms dashboard.

 Status: Paused. Development stalled on two compounding constraints: Kalshi's 7% fee structure sets a high bar, and the compute available for backtesting limited sample sizes to the point where no strategy
 could be validated with confidence — not disproven, just unverifiable given the throughput. Running production infrastructure continuously without a dedicated server compounded this. The backtesting
 pipeline remains usable and the strategies remain unproven either way.

 Compatibility note: Since this bot was built, Kalshi transitioned from an integer 0–100 price representation to a decimal-based system that supports prices below one cent. The entire codebase assumes
 integer tick prices. Adapting it to the current Kalshi API would require refactoring price handling throughout.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Table of Contents

 - At a Glance
 - Getting Started
 - Architecture
 - Market Snapshots
 - Projects
 - Trading Strategies
 - Technical Indicators
 - Candlestick Patterns
 - Machine Learning
 - Performance Monitoring
 - Technology Stack
 - Infrastructure and Deployment
 - Configuration
 - Security
 - Testing
 - Database

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

At a Glance

 - 22 C# projects in a single solution — 16 source/library projects and 6 test projects
 - 3 separately deployable services: trading bot, Overseer management API, and Windows Forms dashboard
 - Real-time WebSocket consumption from Kalshi with exponential backoff retry (1s → 16s), RSA-signed authentication with 5-minute signature caching, and sequence-gap detection
 - SignalR for live streaming between bot instances, Overseer, and the dashboard; bots check in every 30 seconds via a circuit-breaker-protected connection
 - Signal/SignalCondition/Scenario composition model for declaring strategy rules in terms of named market conditions, technical indicators, and order-flow metrics
 - 60+ Japanese candlestick patterns implemented individually in BacklashPatterns/PatternDefinitions/
 - 10 trading strategy implementations ranging from momentum and breakout to ML-backed spike prediction
 - Full backtesting engine (TradingSimulator) that replays historical Parquet snapshots through the live strategy pipeline with fill simulation and 7% fee accounting
 - 18 instrumented performance monitoring classes with per-component enable/disable flags and SQL + SignalR reporting
 - ML scaffolding in TradingStrategies/ML/ for spike prediction and entry research — early-stage, not validated
 - DocFX API documentation with auto-generated table-of-contents tooling

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Getting Started

Prerequisites

 - .NET 8 SDK
 - SQL Server (local or remote) — schema scripts are in BacklashSql/sql/
 - A Kalshi account with API access (production or demo environment) and an RSA key pair registered with Kalshi
 - A secrets directory at C:\KalshiData\Secrets\ containing:
    - The RSA private key file for Kalshi API authentication
    - A secrets file providing {Kalshi:BotKeyId}, {Kalshi:BotKeyFile}, {Database:Username}, and {Database:Password} tokens
 - (Optional) A Windows network share for Parquet candlestick and snapshot storage — configure the UNC path in DataStorage in appsettings.json

Building and Deploying

Three deployment scripts handle the full clean → restore → build → publish → zip pipeline:

    deploy_backlashbot.bat     → publishes BacklashBot to C:\Deploy\BacklashBot
    deploy_overseer.bat        → publishes BacklashOverseer to C:\Deploy\BacklashOverseer
    deploy_tradinggui.bat      → publishes TradingGUI to C:\Deploy\TradingGUI

Each script excludes appsettings.json, secrets, and log files from the output zip, timestamps the zip, and attempts to copy it to the configured network share for remote deployment. To build without
deploying, run dotnet restore && dotnet build -c Release from the solution root.

Database Setup

Run the scripts in BacklashSql/sql/ against a SQL Server instance named kalshibot-prd, or use SchemaDeployment (BacklashBotTasks/Deployment/) to apply them programmatically at startup.

Configuration

Edit the appsettings.json for each service you intend to run. Sensitive values are placeholder tokens (e.g. {Database:Password}) that the runtime resolves from the secrets directory — the files
checked into source contain no actual credentials. See Configuration for the full token and section reference.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Architecture

BacklashBot is a distributed system made up of several independently deployable services that communicate over SignalR and HTTP.

Data Flow

Market data enters the system through WebSocketConnectionManager, which maintains a persistent authenticated connection to Kalshi and feeds raw frames into MessageProcessor. MessageProcessor batches
incoming messages and hands them off to SubscriptionManager, which routes each message to the appropriate channel handler. OrderbookChangeTracker and
OrderBookService consume these events to keep a per-market order book current in DataCache. MarketDataService coordinates the live watchlist: it processes WebSocket events, applies ticker updates,
manages subscriptions, and notifies connected SignalR clients of state changes.

CandlestickService reads order book state at each tick to build and persist OHLCV candles (minute, hour, day) to Parquet files on the network share. TradingSnapshotService periodically snapshots
the complete market state — order book, all computed indicators, pattern detections — to disk at a configurable interval (default 60 seconds) for later backtesting.

On each decision cycle, CentralBrain invokes TradingOverseer to evaluate trading signals against the current snapshot and sends any resulting orders through KalshiAPIService.

Orchestration and Error Recovery

CentralBrain is the top-level IHostedService. On startup, it waits for all dependent services to signal readiness through KalshiBotReadyStatus (a set of TaskCompletionSource objects, one per
service), then creates a unique SessionIdentifier for the run. A 500ms error-monitoring timer feeds into CentralErrorHandler, which evaluates each error individually by exception type. Some types
are immediately catastrophic — exhausted WebSocket retries, a missing API key file, a confirmed internet outage, or any unhandled exception type — and trigger a restart without waiting for a count.
Recoverable types (transient market failures, snapshot validation errors, order book processing errors) are handled with a targeted per-market reset and added to a rolling window. If 10 or more
recoverable errors accumulate within a 5-minute window while snapshots are also lagging (last successful snapshot older than 3 minutes), that combination is also treated as catastrophic. A separate
check independently triggers a restart if no snapshot has been written in the last 10 minutes, regardless of error counts. Warnings that carry an attached exception are promoted to errors and
processed through the same pipeline; message-only warnings are counted but do not trigger recovery. Internet connectivity is verified with exponential backoff (1s initial, up to 60s, max 100
attempts) before services are restarted. An overnight window begins at 3:00 AM, during which BacklashBotTasks performs market refresh, data cleanup, and ML retraining.

Market Management

Two market management strategies are available behind a shared BaseMarketManagerService base class. ManagedMarketManagerService dynamically adjusts how many markets to watch based on real-time
system metrics — queue depths, CPU usage, and performance data — automatically scaling back when the system is under load and expanding when headroom allows. The target count is computed by
TargetCalculationService, which derives a separate limit from each queue type (notification, orderbook, event, ticker) and from the configured CPU usage target, then takes the minimum of all valid
results. UnmanagedMarketManagerService uses a fixed target watch count instead, maintaining stable subscriptions without dynamic scaling. Both share the same interest score logic (computed by
InterestScoreService) to prioritise which markets to subscribe to and which to drop when the watchlist must shrink.

Monitoring Layer

BacklashOverseer runs as a separate service. Bot instances connect to its OverseerHub and send health check-ins every 30 seconds, protected by a circuit breaker that trips after 5 failures within
5 minutes. OverseerClientService on the bot side handles discovery and automatic failover to better Overseer servers when they become available. TradingGUI provides the same live view as a Windows
Forms application, connecting directly to the bot's BacklashBotHub SignalR hub.

Persistence

All structured data flows through BacklashBotData, an Entity Framework Core + ADO.NET data layer backed by SQL Server. Time-series candlestick data and market snapshots are stored as Parquet files
on a network share (SmokehouseDataStorage), keeping large binary blobs off the relational database.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Market Snapshots

A MarketSnapshot is the central data structure of BacklashBot — a versioned, point-in-time serialization of a market's complete state. Everything the bot knows about a market at the moment of evaluation is encoded in a single snapshot object, and that same object is what TradingSimulator replays during backtesting. Because live strategy evaluation and historical simulation consume identical data structures, backtests have exact fidelity to production behavior by construction.

Each snapshot captures:

  Order book state — the full order book, best bids and asks for both sides, depth at the best level and at the top four levels, cumulative depth within a configurable price tolerance, bid range, total contract volume on each side, count and volume imbalance across sides, and center-of-mass pricing for YES and NO bids.

  Trade and order flow — rolling-window trade rate, volume, count, and average size broken out by YES and NO, alongside non-trade-related order counts and order volume per minute. A separate set of instantaneous current-window metrics captures the most recent activity independently of the historical rolling window.

  Order book velocity — the rate of change in order book composition (measured in dollars per minute) split into a top-10% price tier and a bottom tier, for both YES and NO sides. This distinguishes aggressive near-market activity from passive depth changes.

  Price history and momentum — all-time and 3-month high and low YES/NO bid prices with timestamps, short- and medium-term bid slopes for each side, bid range, and spread.

  Support and resistance levels — a list of SupportResistanceLevel objects computed via exponential clustering.

  Technical indicators — RSI (short, medium, long), MACD (medium, long), EMA (medium, long), Bollinger Bands (medium, long), ATR (medium, long), VWAP (short, medium), Stochastic Oscillator (short, medium, long), OBV (medium, long), ADX, +DI/-DI, and PSAR.

  Volume history — peak single-day, single-hour, and single-minute volumes, plus aggregate volumes over the last hour, three hours, and month.

  Recent candlesticks — a list of PseudoCandlestick objects embedded directly in the snapshot.

  Position and P&L — current position size, market exposure, buy-in price, upside and downside exposure, realized PnL, fees paid, expected fees, resting order count, and ROI (both percentage and dollar).

  Market metadata — age, time remaining, whether the market can close early, a flag indicating whether change metrics have stabilized (ChangeMetricsMature), the timestamp of the last WebSocket message received, and behavioral classifications for each price side.

  Derived metrics — a CalculateLiquidityScore() method computes a composite 0–100 score from spread, depth at best, cumulative depth within tolerance, volume, imbalance, and estimated slippage. This score is embedded in the snapshot at capture time.

Serialization

Because snapshots accumulate into large Parquet corpora over time, their JSON is compacted through a layered set of custom JsonConverter implementations in BacklashDTOs.Converters. MarketSnapshotSlimConverter is the outermost layer — it recursively rewrites every property name in the entire JSON tree using a table of roughly 100 short mnemonic codes, so verbose names like VelocityPerMinute_Top_Yes_Bid become vty and StochasticOscillator_Medium becomes stoM. It handles nested sub-keys inside compound types the same way and reads both forms, so files written before the converter existed remain deserializable. OrderbookSlimConverter compacts the order book rows further using single-character keys and abbreviated side values (y/n); because the order book can span dozens of levels and is repeated across every snapshot, this recurs with high frequency. Separate converters handle DateTime fields (compact ISO-8601 without fractional seconds), C# value tuples for multi-component indicators like MACD and Bollinger Bands (which System.Text.Json cannot serialize natively), and fields that embed pre-serialized JSON strings.

MarketSnapshotDelta is a companion class that wraps two consecutive snapshots and computes the diff between them using MarketSnapshot.Diff(). It exposes a typed accessor per field that returns the new value only if that field actually changed — null otherwise — making it efficient to propagate only the parts of a snapshot that moved between evaluation cycles.

Snapshots are written to disk as Parquet files on the network share by TradingSnapshotService, at a configurable interval defaulting to 60 seconds. They carry an integer SnapshotSchemaVersion field (currently version 43). An UpgradeSnapshot() method handles backward compatibility by stepping older snapshots forward through each schema version in sequence, so historical data files remain usable after the schema evolves.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Projects

1. BacklashBot

The core trading engine — an ASP.NET Core service that orchestrates everything at runtime.

State Layer

MarketData is the central per-market data model. It aggregates everything needed to make a trading decision: real-time order book state, historical candlesticks, ticker updates, open positions,
all computed technical indicators, and the metric values (velocities, slopes, depth ratios, trade rates) calculated by OrderbookChangeTracker. Every strategy evaluation reads from a MarketData
instance. DataCache is a thread-safe in-memory store (backed by concurrent dictionaries) holding the live collection of MarketData objects alongside account balance and current trading status.
KalshiBotReadyStatus coordinates startup sequencing using per-service TaskCompletionSource objects so CentralBrain knows when all services have finished initialising before opening WebSocket
connections. KalshiBotStatusTracker holds the global CancellationTokenSource used to signal graceful shutdown to all components. BrainStatusService resolves the runtime bot identity at startup:
it reads the brain instance record from the database to retrieve its registered BrainLock GUID and session identifier, using double-checked locking to ensure the initialization runs exactly once.

Services

MarketDataInitializer runs at startup: it fetches the initial watchlist from the database, subscribes to the appropriate WebSocket channels, loads existing positions and account balance, and
populates DataCache before the bot enters its main trading loop. MarketDataService is the runtime hub for market data operations — it handles incoming WebSocket events, updates the watchlist,
keeps order books and tickers current, and broadcasts state changes to connected SignalR clients.

OrderBookService maintains per-market order book state by processing WebSocket snapshot and delta events asynchronously with thread-safe access. OrderbookChangeTracker sits alongside it and
analyses each order book change in detail: it records change events, matches them against trade flow, and maintains rolling windows of velocity, volume, and rate metrics that strategies consume.
TradingCalculator provides the indicator arithmetic adapted specifically for Kalshi's binary outcome markets — RSI, MACD, Bollinger Bands, Stochastic, ATR, VWAP, OBV, and others — with input
validation tuned to the 0–100 price range. TradingCalculations is a complementary static utility for thread-safe calculations of EMA, liquidation prices, fees, and ROI.

CandlestickService loads, processes, and persists candlestick data from both Parquet files and the SQL database, applying forward-filling and deduplication when combining sources.

MarketRefreshService runs periodic background refreshes of watched-market metadata at a configurable interval. WebSocketMonitorService monitors exchange operational status (open/closed/maintenance)
and manages the WebSocket connection lifecycle — connecting when the exchange opens, disconnecting during scheduled maintenance, and monitoring health throughout.

BroadcastService sends periodic system status snapshots to all connected SignalR clients (interval: 30s, 3 retries with 3s retry delay). OverseerClientService manages the bot's outbound connection
to the Overseer server: it handles registration, periodic check-ins with current health and metrics, and automatic discovery of available Overseer servers on the local network.

KalshiBotScopeManagerService manages dependency injection scope creation and disposal, with optional performance metrics for tracking scope overhead. ServiceFactory provides centralised access
to services through DI scopes with caching and thread-safe access management. HealthCheckService verifies the operational status of all dependent services on startup and on demand.

Hubs and Middleware

BacklashBotHub is the SignalR hub through which TradingGUI and BacklashOverseer receive live market data, metrics, and status updates. SignalRAuthenticationMiddleware is an IHubFilter that
intercepts every hub method invocation and validates the caller's credentials against the database before allowing access.

Configuration: BacklashBot/appsettings.json

2. BacklashOverseer

A separate ASP.NET Core service providing centralised monitoring and control of one or more BacklashBot instances.

OverseerHub.cs is the SignalR hub through which bot instances register and broadcast status. Overseer.cs maintains a lifecycle state model for each connected bot — tracking last check-in time,
reported metrics, and error counts. Bot instances that have not checked in for six hours (with a grace period during the 3–8 AM maintenance window) are flagged as stale by a dedicated staleness
check. BrainPersistenceService provides thread-safe storage and retrieval of brain instance configurations, market watch lists, and performance metrics history, with configurable retention limits
and batch database operations. SnapshotService queries the database for snapshot group data, aggregates it by market ticker, and calculates coverage statistics (recorded hours vs. expected market
hours). OverseerWebSocketMonitorService handles WebSocket connection and exchange status monitoring for the Overseer's own connection to Kalshi, operating independently of BacklashBot's monitoring.
PerformanceMetricsService collects the metrics stream from all connected bots and makes it available to the static web dashboard served from wwwroot/.

MarketWatchController is a REST API controller that backs the web dashboard with cacheable data endpoints: active market watch assignments and interest scores, brain instance lock groupings with per-instance market counts, current and historical positions, open orders, account balance from the Kalshi API, and snapshot coverage by market. Responses are served from a memory cache with configurable expiry to reduce database pressure, and cache hit/miss rates are tracked through the performance monitoring system.

3. KalshiBotAPI

A class library wrapping all Kalshi API interactions.

KalshiAPIService is a typed REST client covering market discovery, order placement, order management, account balance, and historical data retrieval. Every API call is individually timed for the
performance monitoring system.

KalshiWebSocketClient is the main orchestration class for the WebSocket layer. It manages the complete connection lifecycle — initial connect, channel subscriptions, and reconnection — coordinating
between WebSocketConnectionManager, SubscriptionManager, and MessageProcessor so that callers interact with a single client rather than the three subsystems directly.

WebSocketConnectionManager maintains the live TCP connection to Kalshi's WebSocket endpoint. It authenticates using RSA private key signing and caches generated signatures for 5 minutes. When a
connection drops, it retries with exponential backoff (1s, 2s, 4s, 8s, 16s delays) and waits 5 seconds before resetting the underlying socket. A semaphore with a 60-second timeout prevents
concurrent reconnection attempts. The manager tracks connection latency, message throughput, bandwidth, buffer utilisation, and error rate, and posts a full metrics snapshot every minute.

BaseSubscriptionManager provides the shared subscription lifecycle logic — subscribe/update/unsubscribe, state tracking, confirmation processing, and request queuing — as a base class.
SubscriptionManager extends it for the trading bot, subscribing to all relevant market channels (orderbook, ticker, fills, trades) and applying health checks, stale-subscription detection
(60-minute silence threshold), and sequence-gap logging. OverseerSubscriptionManager extends the same base for the Overseer, which subscribes only to fill and lifecycle channels rather than
the full market data feed.

MessageProcessor receives raw frames from WebSocketConnectionManager and batches them before dispatch (up to 50 messages per batch, flushed every 100ms). It tracks per-channel sequence numbers
in a sliding window of 10,000 entries and logs a warning whenever a gap is detected, signalling that messages may have been missed.

Authentication uses RSA private key signing — the private key file path is resolved from the secrets directory at runtime and never appears in source or configuration.

4. TradingStrategies

A class library containing all strategy logic, the simulation engine, and ML integration.

Strategies are built on a typed Signal / SignalCondition / Scenario composition model. The Signal enumeration defines over 40 named market conditions spanning technical indicator states
(MACD crossovers, Bollinger Band breaks, RSI extremes, VWAP position), order-flow conditions (bid imbalance, buy/sell pressure, velocity ratios, trade rate share), and structural conditions
(near support or resistance, wide spread, boundary proximity). A SignalCondition pairs a Signal with a comparison operator and threshold to form a single evaluatable rule, and a Scenario composes
multiple conditions into a complete strategy configuration. This model keeps strategy logic declarative and testable independently of the execution engine.

StrategyConfiguration is the central registry for all strategy definitions. It holds the canonical list of available implementations and provides factory methods for instantiating them with the
correct configuration, so simulation runs and live trading both resolve strategies through the same path.

MarketTypeService classifies each market snapshot into a market condition type (trend, range-bound, volatile, etc.) using a priority-ordered rule set. This classification is used by TradingOverseer
to select which strategy configuration to apply per snapshot and is cached with a configurable expiration. ActionDecision encapsulates the output of a single strategy evaluation: the decided
action, order parameters, and explanatory metadata passed to the execution engine.

Simulation output is represented by several cooperating types: SimulationPath holds the complete state of one trading path through a snapshot sequence — position, cash balance, risk metrics,
order book state, and per-market-condition strategy assignments. SimulationEventLog captures the full environment state at each step: market condition, action taken, position, average cost,
and prices. PathPerformance aggregates the final P&L, equity change, trade count, and market state transitions for a completed path. EventGroup and PathInfo are supporting summary types
used by ReportGenerator to produce CSV-formatted performance reports from event log sequences.

MarketSnapshotExtensions and PseudoCandlestickExtensions provide extension methods for computing order book metrics from simulated snapshot data and converting internal snapshot formats into
standard candlestick structures for pattern detection and indicator calculation.

The ML/ folder contains ResearchBus, SpikePredictionModel, and supporting data classes. See Machine Learning for details.

5. BacklashPatterns

A dedicated class library for candlestick pattern recognition and trend calculation.

PatternSearch.cs is the core pattern-matching engine. It searches through candlestick history for all registered patterns in priority order, suppressing lower-priority matches when a
higher-precedence multi-candle formation has been confirmed for the same bars — so a Three White Soldiers detection, for example, suppresses the individual Marubozu detections on those candles.
Pattern detection timings and per-type counts are tracked via PatternDetectionMetrics and summarised into PatternDetectionMetricsSummary when performance monitoring is enabled.
PatternDetectionConfig controls the detection thresholds — body significance ratios, volume multipliers, and parallel processing settings — that all pattern implementations read.

CandleMetrics is a record struct produced once per candle and shared across all pattern checks. It pre-calculates body size, upper and lower wick lengths, total range, and bullish/bearish
direction so that individual pattern implementations read computed values rather than repeating the arithmetic.

TrendCalcs.cs computes all technical indicator values from the candlestick series. PatternUtils.cs provides shared candle body/shadow measurement utilities.
HistoricalPatternCache caches detection results keyed by market ticker and timestamp, avoiding redundant recomputation when the same candle series is evaluated by multiple strategies in the
same cycle. PatternVisualizer generates text-based visualisations of detected patterns for debugging and dashboard display.

6. TradingSimulator

The backtesting engine that replays historical snapshot data through the strategy pipeline.

TradingSimulatorService orchestrates simulation runs. It loads historical snapshots from the Parquet network share via DataLoader, sequences them chronologically per market, and drives
the simulation loop. Each market's snapshot sequence is processed by MarketProcessor.

MarketProcessor handles the per-market simulation lifecycle. It creates a scenario via StrategyResolver, invokes TradingOverseer, and records the resulting path performance. Fills are
simulated at the best available bid price (BestYesBid or BestNoBid at each snapshot timestamp), the configurable fee rate (default 7%) is applied, and average cost and position are tracked
across fills. TradingStrategy provides an additional layer within the simulator for executing strategies against historical snapshot sequences with full position tracking and risk management.
TradingDecision is the output type from a single strategy evaluation within the simulator, capturing the action, metadata, and a finality flag.

SimulatorReporting produces the final output: per-strategy and per-market P&L breakdowns, trade counts, win/loss statistics, drawdown metrics, and path visualisations. The ML/
subfolder contains FlattenPriceTrainer, which flattens backtested price series into feature vectors for evaluating model performance against historical data.

7. TradingGUI

A Windows Forms desktop application for live monitoring and snapshot review.

MainForm connects to BacklashBot's SignalR hub on startup and renders a live dashboard: active markets and their interest scores, real-time order book depth, current positions, recent
trade events, and performance metrics. Controls allow operators to start/stop trading, adjust market subscriptions, and view the bot's current ready status.

SnapshotViewer is a fully featured historical replay tool. It loads snapshot files from the network share and renders the order book state, computed indicator values, and detected
candlestick patterns at each point in time, allowing stepping forward and backward through the sequence to audit strategy behaviour on any historical session.

MarketChartRenderer draws OHLCV candlestick charts using GDI+, overlaying Bollinger Bands, EMAs, support/resistance levels, and pattern annotations. TypographyManager centralises
font loading, colour palettes, and layout constants so the entire UI remains visually consistent.

8. BacklashBotData

The data-access layer shared by BacklashBot and TradingSimulator.

BacklashBotContext is an Entity Framework Core DbContext covering all persistence entities: markets, series, events, candlesticks, order book snapshots, fills, trades, orders, market positions,
strategy weight sets, bot brain instances and their serialised persistence state, SignalR client registrations, Overseer log entries, and backtesting logs. Queries that are read-only use
AsNoTracking() for reduced memory overhead. All SaveChanges calls are wrapped in a retry loop (3 retries, 1-second delay) that absorbs duplicate-key errors (SQL error 2627), allowing idempotent
upserts without explicit conflict checks in calling code. SqlDataService provides ADO.NET-backed bulk insert paths for high-frequency data such as candlestick ingestion and WebSocket feed events
where EF Core overhead is not acceptable.

9. BacklashBotTasks

Background task implementations that CentralBrain schedules and invokes at runtime.

OvernightTasks runs after the 3:00 AM overnight window opens: it refreshes market and series metadata from the Kalshi API, recomputes interest scores, imports new candlestick data, removes watches
for closed markets, clears orphaned brain locks from prior sessions, and generates SnapshotGroup objects for ML training and backtesting. MLTasks drives periodic ML model retraining — it exports
feature vectors from ResearchBus, triggers SpikePredictionModel to retrain on updated snapshot data, and writes the new model file to the network share. SnapshotProcessingTasks aggregates raw
snapshots into Parquet files on the network share, managing file rotation and retention. ScheduleUpdateTasks syncs the local exchange schedule cache against Kalshi's schedule API. SchemaDeployment
applies SQL schema migrations programmatically at startup, reading script files from BacklashSql/sql/ so schema changes can be deployed without running scripts manually.

10. MentionMarketBingo

A lightweight standalone utility for watching specific Kalshi markets in real time without the full BacklashBot infrastructure.

Form1 is the main application window. It connects to the Kalshi WebSocket directly, loads market data for the configured tickers, and displays live order book depth, price movement, and recent
events in a minimal Windows Forms UI. MentionMarketBingoOrderBookService maintains live order book state per market. MentionMarketBingoWebSocketMonitorService monitors connection health and
triggers reconnection when the WebSocket goes silent. A CustomProgressBar control provides a compact visual indicator of order book depth balance. SimplePerformanceMonitor is a minimal
IPerformanceMonitor implementation wired in for the app's lightweight monitoring needs.

11. BacklashSql

SQL schema scripts for the SQL Server database (kalshibot-prd). See Database for the full data domain breakdown.

Supporting Libraries

BacklashInterfaces defines the shared interface contracts consumed across the solution: IPerformanceMonitor (RecordExecutionTime, RecordSimulationMetrics) and its extension ICentralPerformanceMonitor,
the trading strategy interface hierarchy, market data and snapshot interfaces, and system-wide enums and constants. Keeping contracts here rather than in any one project prevents circular references
between the service projects that all depend on them.

BacklashDTOs defines the data transfer objects crossing service boundaries — snapshot payloads, strategy result objects, SignalR message models, and feed event DTOs. DTOs are intentionally separate
from both the entity models in BacklashBotData and the interface contracts in BacklashInterfaces.

BacklashCommon is the shared utilities library. SnapshotDiscrepancyValidator detects integrity problems in snapshot data — missing order books, overlapping bid/ask prices, and rate inconsistencies —
before snapshots are used in backtesting. SnapshotGroupHelper packages consecutive snapshots into analysis groups, filtering out gaps and invalid periods. SnapshotPeriodHelper groups raw snapshots
into valid analysis periods based on time-gap thresholds and price stability, with async file I/O and parallel processing for large datasets. TradingSnapshotService manages snapshot save and load
operations including schema versioning and validation. InterestScoreService calculates the composite interest score for each Kalshi market — a weighted combination of spread characteristics, trade
volume, and liquidity depth — with a configurable cache duration (6 hours) and performance metrics. MarketTypeHelper classifies market condition types from snapshot data using a priority-ordered
rule set, shared between live trading and simulation. SessionIdentifierGenerator produces unique session IDs from cryptographic randomness and timestamp entropy. BaseWebSocketMonitorService provides
the core exchange-status monitoring loop that both BacklashBot's WebSocketMonitorService and BacklashOverseer's OverseerWebSocketMonitorService extend.

BacklashBotLogging centralises all logging infrastructure. DatabaseLogger is a custom ILogger implementation that writes to both the console and an asynchronous database queue simultaneously.
DatabaseLoggingQueue is the IHostedService that drains that queue in the background, batching log entries before persisting them to SQL to avoid blocking the caller. DatabaseLoggerProvider creates
DatabaseLogger instances and configures log levels per component from appsettings.json, allowing each service to have independent SQL and console log level thresholds.

OverseerBotShared contains the contracts and models shared between BacklashOverseer and BacklashBot for their SignalR communication. Interfaces.cs defines IBacklashBotHub and IOverseerHub as
strongly typed hub contracts, providing compile-time validation of method names across the client/server boundary. SignalRClient is the strongly typed client implementation used by the bot to
communicate with the Overseer hub. DTOs.cs defines CheckInData, CheckInResponse, and related payloads exchanged during health check-ins. SignalRErrorHandler adds error classification and retry
logic on top of the base SignalR error handling, with structured logging of connection failures.

Test Projects

 - BacklashBot.Shared.Tests — shared test helpers and base classes
 - BacklashBotTests — unit tests covering: technical indicator calculation accuracy (RSI, MACD, EMA, Bollinger Bands, ATR, VWAP, Stochastic), Kalshi REST API request/response handling,
   WebSocket frame parsing and sequence behaviour, snapshot data integrity validation, configuration secrets interpolation, extension method correctness, logger behaviour, and trading metric
   scenario coverage. TestHelper provides static factory methods for building test configuration objects shared across suites.
 - OverseerTests — Overseer service unit tests
 - OverseerBotShared.Tests — tests for shared models and service contracts
 - BacklashIntegrationTests — end-to-end tests covering the full data pipeline from snapshot ingestion through strategy evaluation and database persistence
 - TradingGUITests — GUI component tests

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Trading Strategies

Strategies are built on a typed Signal / SignalCondition / Scenario composition model. A strategy declares a set of named signal conditions — each pairing a Signal (a named market indicator or
flow state) with a comparison operator and threshold — and the evaluation engine tests those conditions against the live or simulated snapshot to produce an ActionDecision. This makes strategy
logic declarative and independently testable: conditions can be unit-tested in isolation, composed in different combinations, and varied per market-condition type through the Scenario model.

The Signal enumeration covers technical indicator states (MACD crossovers, Bollinger Band breaks, RSI extremes, VWAP position), order-flow conditions (bid imbalance, buy/sell pressure, velocity
ratios, trade rate share), structural conditions (near support or resistance, wide spread, boundary proximity), and volume signals (OBV direction, high-volume trade events). Any combination of
these can be composed into an entry or exit rule.

The 10 included implementations explore a range of approaches: trend-following and breakout strategies that respond to price clearing identified levels, momentum strategies that track order-flow
imbalance and slope divergence across multiple timeframes, mean-reversion strategies for range-bound markets, a specialised low-liquidity exit manager, and two ML-backed strategies that use the
SpikePredictionModel score as an entry gate or confirmation signal. These were developed iteratively and tested against backtested data; none were validated as consistently profitable against the
7% fee structure, and they are best understood as a starting framework for further research.

StrategyConfiguration is the central registry. It defines which strategy implementations are available and provides factory methods for instantiation, so both live trading and simulation runs
resolve strategies through the same path.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Technical Indicators

All indicators are computed in BacklashPatterns/TrendCalcs.cs and parameterised via appsettings.json under WatchedMarkets.MarketData.Calculations.

 - RSI — short, medium, and long period windows (default 14 each)
 - MACD — medium (12/26/9) and long (12/26/9) configurations
 - EMA — medium (14) and long (14) periods
 - Bollinger Bands — medium and long (both 20 periods, 2σ)
 - ATR — medium (14) and long (14) periods
 - VWAP — short (15) and medium (15) period windows
 - Stochastic Oscillator — short, medium, and long (14 %K, 3 %D)
 - Parabolic SAR — initial AF 0.02, step 0.02, max 0.20
 - ADX — 14 periods
 - Resistance/Support Levels — exponential clustering with configurable sigma (2.0), up to 6 levels per side
 - Price Slope — short window (5 min) and medium window (15 min)
 - On-Balance Volume

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Candlestick Patterns

BacklashPatterns/PatternDefinitions/ implements 60+ Japanese candlestick patterns. Each pattern is an individual class that tests the relevant candle body/shadow geometry using shared measurements
from CandleMetrics. PatternSearch evaluates all registered patterns on each cycle in priority order, suppressing lower-priority matches when a higher-precedence multi-candle formation has been
confirmed for the same bars.

<details>
<summary>Full pattern list (click to expand)</summary>

Abandoned Baby · Belt Hold · Breakaway · Closing Marubozu · Concealing Baby Swallow · Counterattack · Dark Cloud Cover · Doji (standard, Star, Dragonfly, Gravestone, Long-Legged, Rickshaw Man) · Downside
Gap Three Methods · Engulfing (Bullish/Bearish) · Evening Doji Star · Evening Star · Hammer · Inverted Hammer · Hanging Man · Harami · High Wave Candle · Hikkake · Modified Hikkake · Homing Pigeon ·
Identical Three Crows · In-Neck · On-Neck · Kicking · Kicking-by-Length · Ladder Bottom · Long Line Candle · Short Line Candle · Marubozu · Mat Hold · Morning Doji Star · Morning Star · Piercing ·
Rising/Falling Three Methods · Separating Lines · Shooting Star · Spinning Top · Stalled Pattern · Stick Sandwich · Takuri · Tasuki Gap · Three Advancing White Soldiers · Three Black Crows · Three Inside
Up/Down · Three Line Strike · Three Outside Up/Down · Three Stars in the South · Thrusting · Tristar · Two Crows · Unique Three River · Upside/Downside Gap Side-by-Side White Lines ·
Upside/Downside Gap Three Methods · Upside Gap Two Crows

</details>

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Machine Learning

The ML subsystem in TradingStrategies/ML/ and TradingSimulator/ML/ represents early-stage scaffolding that was never fully developed or validated. It is included as a foundation for future research
rather than a working component.

SpikePredictionModel is an ML.NET binary classifier intended to predict near-term price spikes on the YES side of a market. SpikePredictionData.cs and SpikePredictionExtensions.cs define the input
schema and feature extraction helpers. FlattenPriceTrainer (TradingSimulator/ML/) is a simulation-side utility for generating feature vectors from backtested price series.

ResearchBus is a thread-safe static data collection hub that strategies can populate during live trading and simulation. It logs per-entry outcome metrics — excursion, drawdown, hit rate — to a
ConcurrentBag and exports them as CSV for offline analysis. The intent was to build a labelled dataset for model training and parameter optimisation; the data pipeline exists but the downstream
analysis was not completed.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Performance Monitoring

A comprehensive, opt-in-per-service performance monitoring system spans 18 classes across the codebase. Every instrumented class reads an EnablePerformanceMetrics flag from its own configuration
section in appsettings.json, so individual components can be toggled without redeployment.

IPerformanceMonitor (BacklashInterfaces) defines RecordExecutionTime() and RecordSimulationMetrics(). ICentralPerformanceMonitor extends it and is implemented by CentralPerformanceMonitor, the
single aggregation point for all bot-wide metrics. Component monitors post data to CentralPerformanceMonitor, which batches records for SQL storage and forwards them to the Overseer via SignalR.

What each component measures: WebSocketConnectionManager tracks connection latency (min/max/average), message throughput (messages per second, peak), bandwidth, buffer utilisation, RSA signature
cache hit rate, and error rate. KalshiAPIService times every individual REST call. PatternSearch tracks detection pass duration and per-pattern-type counts via PatternDetectionMetrics.
TradingOverseer and SimulationEngine track snapshot processing throughput and GC memory usage. Each strategy records per-evaluation timing. OrderbookChangeTracker records rolling window metrics
for velocity and trade rate calculations.

Instrumented classes: TradingOverseer, StrategySimulation, SimulationEngine, MarketTypeService, EquityCalculator, StrategySelectionHelper, PatternSearch, CentralPerformanceMonitor, MarketProcessor,
WebSocketConnectionManager, KalshiAPIService, CandlestickService, KalshiBotScopeManagerService, OrderbookChangeTracker, WebSocketMonitorService, TradingSnapshotService, PerformanceMonitor,
PerformanceMetricsService.

See Performance_Monitoring_Flow_Chart.md for the full data-flow diagram.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Technology Stack

 - Runtime — .NET 8
 - Language — C#
 - Web framework — ASP.NET Core
 - Real-time comms — SignalR
 - Desktop UI — Windows Forms
 - Database — SQL Server
 - ORM / data access — Entity Framework Core + ADO.NET (BacklashBotData)
 - Time-series storage — Apache Parquet (via network share)
 - WebSocket — native .NET ClientWebSocket
 - Machine learning — ML.NET (Random Forest classifier)
 - Authentication — RSA private key signing (Kalshi API requirement)
 - Documentation — DocFX

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Infrastructure and Deployment

Multi-machine Setup

The system is designed for two or more machines:

 - Data server — hosts SQL Server (kalshibot-prd), the Parquet/snapshot network share (SmokehouseDataStorage), and the deployment artifact share (SmokehouseCandlestickImport)
 - Bot host(s) — run BacklashBot and BacklashOverseer as ASP.NET Core services
 - Operator workstation — runs TradingGUI as a Windows Forms application and optionally MentionMarketBingo for lightweight market observation

Deployment Scripts

Four scripts are included at the solution root:

 - deploy_backlashbot.bat — full clean/restore/build/publish pipeline for BacklashBot; outputs to C:\Deploy\BacklashBot
 - deploy_overseer.bat — same pipeline for BacklashOverseer; outputs to C:\Deploy\BacklashOverseer
 - deploy_tradinggui.bat — same pipeline for TradingGUI; outputs to C:\Deploy\TradingGUI
 - serve_docs.bat — serves the DocFX documentation site locally

Each deploy script excludes appsettings.json, secrets, and logs from the output zip, timestamps the zip, and attempts to copy it to the network share if reachable.

Documentation

DocFX (docfx_project/) generates API documentation from XML doc comments. generate_toc.ps1 auto-generates table-of-contents entries from the project structure and add_xml_comments.ps1 adds XML
doc comment stubs to C# source files that are missing them.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Configuration

Each deployable service has its own appsettings.json. Sensitive values are placeholder tokens resolved from the secrets directory at C:\KalshiData\Secrets at runtime:

 "Kalshi": {
   "KeyId":   "{Kalshi:BotKeyId}",
   "KeyFile": "{Kalshi:BotKeyFile}"
 },
 "DBConnection": {
   "DefaultConnection": "Server=...;User Id={Database:Username};Password={Database:Password};..."
 }

Notable BacklashBot/appsettings.json sections and their key values:

 - DataStorage — UNC paths to SmokehouseDataStorage (snapshots) and SmokehouseCandlestickImport (deployment); simulation cache at C:\Temp\SimulationCache
 - DBConnection — SQL Server connection string with retry count (3), retry delay (1s), batch size (100), and queue depth limit (10,000)
 - Kalshi — API environment (prd / demo) and key token references
 - Communications — BroadcastService (30s interval, 3 retries, 3s retry delay), OverseerClientService (30s check-in interval, 30s connection timeout, circuit breaker: 5 failures trip after
   5 minutes), BacklashBotHub endpoint config
 - API — KalshiAPIService per-endpoint timeout and rate-limit settings
 - Websockets — buffer size (16,384 bytes), retry delays ([1s, 2s, 4s, 8s, 16s]), RSA signature cache (5 min), semaphore timeout (60s), message batch size (50), batch flush interval (100ms),
   sequence tracking window (10,000), subscription health check interval (1 hour), stale subscription threshold (60 min)
 - Management — MarketManagerService discovery polling interval and market filter config
 - WatchedMarkets — snapshot interval (60s), candlestick parameters, order book depth settings, InterestScore weights, TargetCalculationService config
 - MarketData.Calculations — all indicator periods (RSI: 14, MACD: 12/26/9, Bollinger: 20/2σ, ATR: 14, VWAP: 15, Stochastic: 14/3, slope windows: 5 min / 15 min)
 - Central.CentralBrain — overnight start (3:00 AM), error check interval (500ms), error window (5 min), error threshold (10), internet recovery (max 100 attempts, 1s → 60s backoff),
   max markets per subscription action (10), dashboard launch flag
 - SnapshotGroupHelper — grouping strategy, retention window, and batch size settings

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Security

 - No credentials in source — all secrets (API keys, DB passwords) are resolved from an external secrets directory at runtime using placeholder tokens; the tokens in checked-in config files
   are inert without the accompanying secrets files
 - RSA authentication — every Kalshi API request is signed with an RSA private key; the public key ID is registered with Kalshi and the private key file is stored outside the repository and
   never included in published deployment zips
 - SignalR authentication — SignalRAuthenticationMiddleware validates caller credentials against the database on every hub method invocation
 - Local config excluded from deploys — deployment scripts explicitly exclude appsettings.local.json from published zips
 - .gitignore covers build artifacts, local secrets, user-specific IDE files, and runtime logs

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Testing

 - BacklashBot.Shared.Tests — shared test helpers and base classes
 - BacklashBotTests — unit tests covering technical indicator arithmetic accuracy, Kalshi REST and WebSocket API behaviour, snapshot data integrity validation, configuration secrets
   interpolation, extension method correctness, logger behaviour, and trading metric scenarios. TestHelper provides static factory methods for test configuration objects shared across suites.
 - OverseerTests — Overseer service unit tests
 - OverseerBotShared.Tests — tests for shared models and service contracts
 - BacklashIntegrationTests — end-to-end tests covering the full data pipeline from snapshot ingestion through strategy evaluation and database persistence
 - TradingGUITests — GUI component tests
 - TradingSimulator (backtesting) — running any strategy against real historical snapshots exercises the entire data pipeline — snapshot loading, indicator computation, pattern detection,
   signal evaluation, order fill simulation, and P&L accounting — functioning as a comprehensive system-level integration test with real-world data

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Database

SQL Server database kalshibot-prd runs on a dedicated host on the local network. The schema (BacklashSql/sql/) covers every data domain in the system:

 - Market and series metadata — markets, series, settlement sources, tags, exchange schedules, announcements, and milestones
 - Order book and price data — real-time orderbook snapshots, candlesticks, tickers
 - Trade feed events — fills, lifecycle events (market open/close/settlement), trade executions; stored with a toggleable StoreWebSocketEvents flag to control write volume
 - Snapshot data for backtesting — raw snapshots, snapshot schemas (versioned), and snapshot groups that package consecutive snapshots for ML training runs
 - Order and position tracking — open/closed orders and per-market positions
 - Strategy state — weight sets and per-market weight set assignments, strategy performance data
 - Bot operational state — brain instances, brain persistence (serialised state across restarts), SignalR client registrations, Overseer health logs, and backtesting run logs
 - Performance data — execution timing and simulation metrics written by the 18 instrumented classes

Data access uses parameterised retry logic — 3 retries with a 1-second delay, batch size of 100, and a queue depth limit of 10,000 — to handle transient connectivity on the LAN. Stored procedures
handle all feed inserts and market/series upserts. Report queries (rpt_ prefix) cover performance metrics, semaphore contention, timer statuses, WebSocket event counts, and broadcast throughput.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

MIT License — Copyright (c) 2025 Peter Foley. Free to use, modify, and distribute with attribution.

Built by Peter Foley · Kalshi prediction market trading bot · .NET 8 · C#
