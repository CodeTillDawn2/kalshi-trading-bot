
Overview

BacklashBot is a sophisticated, multi-service automated trading system for the Kalshi prediction market platform. It connects to Kalshi's REST and WebSocket APIs in real time, ingests live order book and
market data, applies a library of technical analysis strategies and machine learning models to identify trade opportunities, and manages order execution — all while continuously backtesting against
historical snapshot data to validate and improve strategies.

The system is designed for a distributed, multi-machine deployment: a central data server stores candlestick and snapshot data on a shared network drive, one or more bot instances run independently and
check in with an Overseer service, and operators interact with a Windows Forms dashboard.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Architecture

 graph TD
     Overseer["BacklashOverseer\nASP.NET Core · SignalR Hub · Monitoring API"]
     Bot["BacklashBot\nASP.NET Core · CentralBrain"]
     GUI["TradingGUI\nWindows Forms"]
     API["KalshiBotAPI\nREST + WebSocket"]
     Strats["TradingStrategies\n+ ML Engine"]
     Patterns["BacklashPatterns\n60+ Candlestick Patterns"]
     Sim["TradingSimulator\nBacktesting"]
     DB["SQL Server Database"]
     Storage["Network Share\nParquet / Snapshots"]
 
     Overseer -->|SignalR check-ins| Bot
     GUI -->|SignalR| Bot
     Bot --> API
     Bot --> Strats
     Bot --> Patterns
     Bot --> Sim
     Bot --> DB
     Bot --> Storage

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Projects

1. BacklashBot

The core trading engine — an ASP.NET Core service that orchestrates everything at runtime.

Key responsibilities:

 - CentralBrain — top-level orchestration loop, startup sequencing, overnight activities, and error recovery with circuit breaker logic
 - Market Management — discovers and subscribes to Kalshi markets; tracks interest scores to prioritise which markets to watch
 - Order Book — maintains real-time order book state per market via WebSocket events (OrderbookChangeTracker, OrderBookService)
 - Candlestick Service — builds and persists candlestick data (minute/hour/day) to Parquet files on a network share
 - Snapshot Service — periodically snapshots full market state (order book + indicators) to disk for later backtesting
 - Trading — delegates to TradingStrategies for signal evaluation and trade decisions
 - SignalR Hub — broadcasts live market state and performance metrics to the Overseer and TradingGUI

Configuration: BacklashBot/appsettings.json

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

2. BacklashOverseer

A separate ASP.NET Core service providing centralised monitoring and control of one or more BacklashBot instances.

 - OverseerHub (OverseerHub.cs, ~51 KB) — SignalR hub through which bot instances register, broadcast status, and receive commands
 - Overseer (Overseer.cs, ~25 KB) — manages bot lifecycle state, tracks health, and aggregates metrics
 - PerformanceMetricsService — collects and exposes performance data from all connected bots
 - wwwroot — static web dashboard served directly by the Overseer

Bot instances check in at a configurable interval (default: 30 seconds) with circuit-breaker protection on the connection.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

3. KalshiBotAPI

A class library wrapping all Kalshi API interactions.

 - KalshiAPIService — REST client for market data, order management, and account information; times every call for performance metrics
 - WebSocketConnectionManager — production-grade WebSocket client with exponential backoff retry (1s → 2s → 4s → 8s → 16s), 16 KB configurable buffer, RSA signature caching (5-minute TTL), and 
semaphore-based concurrency control
 - SubscriptionManager — manages market data subscriptions with health checks, stale-subscription cleanup, and sequence-gap detection
 - MessageProcessor — batches incoming WebSocket messages (up to 50 per batch, 100ms interval) with optional advanced locking for high-throughput scenarios

Authentication uses RSA private key signing — the key file path is resolved from a secrets directory outside the repository.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

4. TradingStrategies

A class library containing all strategy logic, the simulation engine, and ML integration.

 TradingStrategies/
 ├── Strategies/
 │   ├── Strategy.cs           # Base strategy abstraction
 │   ├── Signal.cs             # Trade signal model
 │   ├── SignalCondition.cs    # Composable signal conditions
 │   ├── Scenario.cs           # Named scenario for simulation
 │   └── Strats/               # Concrete strategy implementations
 ├── Trading/
 │   ├── TradingOverseer.cs    # Coordinates strategy execution
 │   ├── SimulationEngine.cs   # Backtesting engine core
 │   ├── StrategySimulation.cs
 │   ├── MarketTypeService.cs
 │   ├── EquityCalculator.cs
 │   └── PerformanceMonitor.cs
 ├── ML/
 │   ├── ResearchBus.cs        # Data bus for ML feature extraction
 │   └── SpikePrediction/      # Price spike ML model
 └── Extensions/

See Trading Strategies for the full strategy list.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

5. BacklashPatterns

A dedicated class library for candlestick pattern recognition and trend calculation.

 - PatternSearch.cs (~68 KB) — the core pattern-matching engine; searches for all registered patterns against candlestick history
 - TrendCalcs.cs (~28 KB) — computes trend-related technical values (slopes, momentum, regression)
 - PatternUtils.cs (~24 KB) — shared utilities for candle body/shadow analysis
 - HistoricalPatternCache.cs — caches pattern detection results keyed by market and timestamp
 - PatternVisualizer.cs — generates text/ASCII visualizations of detected patterns
 - PatternDefinitions/ — 60+ individually implemented candlestick patterns (see Candlestick Patterns)

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

6. TradingSimulator

The backtesting engine that replays historical snapshot data through the strategy pipeline.

 - TradingSimulatorService (~54 KB) — orchestrates full simulation runs; loads snapshots, sequences them, and drives the simulation loop
 - MarketProcessor (~55 KB) — processes individual markets through strategies during simulation; calculates P&L, trade fill simulation, and fee accounting (7% configurable fee rate)
 - SimulatorReporting (~46 KB) — generates detailed performance reports per strategy and per market
 - DataLoader — loads and deserialises Parquet-based snapshot files
 - StrategyResolver — resolves which strategy implementation to use for a given simulation config

The simulator is designed to run large-scale parallel backtests. Compute constraints were a primary reason development was paused.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

7. TradingGUI

A Windows Forms desktop application for live monitoring and snapshot review.

 - MainForm (~56 KB) — primary operator dashboard; connects to BacklashBot via SignalR and displays live market state, order books, and performance metrics
 - SnapshotViewer (~144 KB) — richly featured viewer for historical snapshot data; allows replay and inspection of any saved market state
 - MarketChartRenderer — renders candlestick/price charts via GDI+
 - TypographyManager (~20 KB) — manages consistent fonts and styling across the UI

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

8. MentionMarketBingo

A lightweight standalone utility application for watching specific Kalshi markets in real time without the full BacklashBot infrastructure.

 - Connects directly to the Kalshi WebSocket
 - MentionMarketBingoOrderBookService — maintains live order book state
 - MentionMarketBingoWebSocketMonitorService — monitors WebSocket health
 - Simple Windows Forms GUI for displaying order book depth and price movement

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

9. BacklashSql

SQL schema scripts for the SQL Server database (kalshibot-prd). Contains .sql files for table definitions including OverseerLogs and various performance and metrics tables. The database runs on a dedicated
host (192.168.1.210).

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

10. Supporting Libraries

 - BacklashInterfaces — shared interface contracts: IPerformanceMonitor, ICentralPerformanceMonitor, trading strategy interfaces, enums, and constants
 - BacklashDTOs — data transfer objects shared across service boundaries
 - BacklashCommon — shared utilities, configuration helpers, and common services
 - BacklashBotLogging — centralised logging infrastructure with configurable SQL and console log levels
 - OverseerBotShared — models and services shared between the Overseer and connected bot instances
 - BacklashBot.Shared.Tests — shared test infrastructure and base classes

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Trading Strategies

Each strategy implements a common interface and is selected/weighted by TradingOverseer at runtime. All strategies can be backtested independently via TradingSimulator.

 - BollingerBreakout — enters trades when price breaks outside Bollinger Bands, expecting mean reversion or continuation
 - Breakout / Breakout2 — resistance/support breakout detection with two variants of entry confirmation
 - FlowMomentumStrat — order-flow momentum using order book imbalance and trade flow direction
 - LowLiquidityExitExec — specialised exit strategy for low-liquidity conditions to minimise slippage
 - MLEntrySeekerShared — ML-based entry seeker; shares feature extraction logic with the spike prediction model
 - MLSpikePredictionStrat — uses the SpikePredictionModel (ML.NET) to predict imminent price spikes
 - MomentumTrading — classic price/volume momentum with RSI and MACD confirmation
 - NothingEverHappensStrat — designed for flat/range-bound markets; fades movement near resistance and support
 - SlopeMomentumStrat (~118 KB) — the most developed strategy; multi-timeframe slope analysis with comprehensive signal conditions
 - TryAgainStrat (~216 KB) — the most complex strategy; extensive signal composition and retry/re-entry logic

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Technical Indicators

All indicators are computed in BacklashPatterns/TrendCalcs.cs and are fully parameterised via appsettings.json.

 - RSI — short, medium, and long period windows (default 14 each)
 - MACD — medium (12/26/9) and long (12/26/9) configurations
 - EMA — medium (14) and long (14) periods
 - Bollinger Bands — medium and long (both 20 periods, 2σ)
 - ATR — medium (14) and long (14) periods
 - VWAP — short (15) and medium (15) period windows
 - Stochastic Oscillator — short, medium, and long (14/%K, 3/%D each)
 - Parabolic SAR — initial AF
  0.02, step 0.02, max 0.20
 - ADX — 14 periods
 - Resistance/Support Levels — exponential clustering, configurable sigma (2.0), up to 6 levels
 - Price Slope — short window (5 min) and medium window (15 min)

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Candlestick Patterns

BacklashPatterns/PatternDefinitions/ implements 60+ Japanese candlestick patterns:

<details>
<summary>Full pattern list (click to expand)</summary>

Abandoned Baby · Belt Hold · Breakaway · Closing Marubozu · Concealing Baby Swallow · Counterattack · Dark Cloud Cover · Doji (standard, Star, Dragonfly, Gravestone, Long-Legged, Rickshaw Man) · Downside
Gap Three Methods · Engulfing (Bullish/Bearish) · Evening Doji Star · Evening Star · Hammer · Inverted Hammer · Hanging Man · Harami · High Wave Candle · Hikkake · Modified Hikkake · Homing Pigeon ·
Identical Three Crows · In-Neck · On-Neck · Kicking · Kicking-by-Length · Ladder Bottom · Long Line Candle · Short Line Candle · Marubozu · Mat Hold · Morning Doji Star · Morning Star · Piercing ·
Rising/Falling Three Methods · Separating Lines · Shooting Star · Spinning Top · Stalled Pattern · Stick Sandwich · Takuri · Tasuki Gap · Three Advancing White Soldiers · Three Black Crows · Three Inside
Up/Down · Three Line Strike · Three Outside Up/Down · Three Stars in the South · Thrusting · Tristar · Two Crows · Unique Three River · Upside/Downside Gap Three Methods · Upside Gap Two Crows

</details>

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Machine Learning

The ML subsystem lives in TradingStrategies/ML/ and TradingSimulator/ML/.

SpikePredictionModel (SpikePrediction/SpikePredictionModel.cs, ~40 KB):

 - Built with ML.NET
 - Predicts whether a price spike will occur in the near term
 - Features are extracted via ResearchBus.cs — a data bus that aggregates candlestick metrics, order book state, and indicator values into a flat feature vector
 - Used by both MLSpikePredictionStrat (live trading) and MLEntrySeekerShared (entry timing)
 - Training data is sourced from historical snapshot files on the network share

The TradingSimulator/ML/ folder provides simulation-side ML utilities for evaluating model performance against backtested data.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Performance Monitoring

A comprehensive, opt-in-per-service performance monitoring system spans 18 classes across the codebase.

 - IPerformanceMonitor defines RecordExecutionTime() and RecordSimulationMetrics() in both basic and enablement-aware overloads
 - ICentralPerformanceMonitor extends IPerformanceMonitor and is implemented by CentralPerformanceMonitor — the aggregation point for all bot-wide metrics
 - Each service has its own EnablePerformanceMetrics flag in appsettings.json, so metrics collection can be toggled per component at runtime
 - Metrics are stored to the SQL database and broadcast to the Overseer via SignalR

Instrumented classes: TradingOverseer, StrategySimulation, SimulationEngine, MarketTypeService, EquityCalculator, StrategySelectionHelper, PatternSearch, CentralPerformanceMonitor, MarketProcessor,
WebSocketConnectionManager, KalshiAPIService, CandlestickService, KalshiBotScopeManagerService, OrderbookChangeTracker, WebSocketMonitorService, TradingSnapshotService, PerformanceMonitor,
PerformanceMetricsService.

See Performance_Monitoring_Flow_Chart.md for the full flow diagram.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Technology Stack

 - Runtime — .NET 8
 - Language — C#
 - Web framework — ASP.NET Core
 - Real-time comms — SignalR
 - Desktop UI — Windows Forms
 - Database — SQL Server
 - Time-series storage — Apache Parquet (via network share)
 - WebSocket — native .NET ClientWebSocket
 - Machine learning — ML.NET
 - Authentication — RSA private key signing
 - Documentation — DocFX

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Infrastructure & Deployment

Multi-machine Setup

The system is designed for two or more machines:

 - Data server — hosts SQL Server, the Parquet/snapshot network share (SmokehouseDataStorage), and the deployment artifact share (SmokehouseCandlestickImport)
 - Bot host(s) — run BacklashBot and BacklashOverseer as ASP.NET Core services
 - Operator workstation — runs TradingGUI as a Windows Forms application

Deployment Scripts

Three .bat scripts handle the full build-publish-zip pipeline for each deployable service:

 - deploy_backlashbot.bat → publishes to C:\Deploy\BacklashBot
 - deploy_overseer.bat → publishes to C:\Deploy\BacklashOverseer
 - deploy_tradinggui.bat → publishes to C:\Deploy\TradingGUI

Each script cleans bin/ and obj/, runs dotnet restore, dotnet build -c Release, and dotnet publish, then zips the output (excluding appsettings.json, secrets, and logs) with a timestamp. The zip is
transferred to the network share if reachable, otherwise left locally.

Documentation

DocFX (docfx_project/) generates API documentation. serve_docs.bat serves it locally. generate_toc.ps1 auto-generates table-of-contents entries and add_xml_comments.ps1 adds XML doc comment stubs to C#
source files.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Configuration

Each deployable service has its own appsettings.json. Sensitive values use placeholder tokens resolved at runtime from a secrets directory:

 "Kalshi": {
   "KeyId":   "{Kalshi:BotKeyId}",
   "KeyFile": "{Kalshi:BotKeyFile}"
 },
 "DBConnection": {
   "DefaultConnection": "Server=...;User Id={Database:Username};Password={Database:Password};..."
 }

Secrets are stored in C:\KalshiData\Secrets and are not in this repository.

Notable BacklashBot/appsettings.json sections:

 - DataStorage — network share paths and simulation cache directory
 - DBConnection — SQL Server connection string, retry count, batch size, and queue depth
 - Kalshi — API environment (prd / demo) and key references
 - Websockets — buffer size, retry delays, message batching config, subscription health timeouts
 - WatchedMarkets — snapshot interval, candlestick parameters, order book settings
 - MarketData.Calculations — all technical indicator periods and parameters
 - Central.CentralBrain — overnight window, max markets per action, dashboard launch flag

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Security

 - No credentials in source — all secrets (API keys, DB passwords) are resolved from an external secrets directory at runtime using placeholder tokens
 - RSA authentication — Kalshi API requests are signed with an RSA private key; the public key ID is registered with Kalshi and the private key file never enters the repository
 - Local config excluded from deploys — deployment scripts explicitly exclude appsettings.local.json from published zips
 - .gitignore covers build artifacts, local secrets, user-specific IDE files, and runtime logs

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Testing

 - BacklashBot.Shared.Tests — shared test helpers and base classes
 - OverseerTests — Overseer unit tests
 - OverseerBotShared.Tests — shared model and service tests
 - BacklashIntegrationTests — end-to-end integration tests
 - TradingGUITests — GUI component tests
 - TradingSimulator (backtesting) — running a strategy against real historical snapshots validates the entire data pipeline and strategy logic end-to-end, functioning as a form of system-level integration 
test

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Database

SQL Server database kalshibot-prd runs on 192.168.1.210. The schema in BacklashSql/sql/ covers overseer logs, performance metrics (execution times, simulation metrics), market data and event logs, and
WebSocket/API event storage (toggleable via StoreWebSocketEvents).

Data access uses parameterised retry logic — 3 retries with a 1-second delay, batch size of 100, and a queue depth limit of 10,000 — to handle transient connectivity issues.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Built by BacklashStudios · Kalshi prediction market trading bot · .NET 8 · C#
