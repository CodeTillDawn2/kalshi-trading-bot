BacklashBot — Kalshi Prediction Market Trading Bot

 Status: Development Paused. The system reached a functional, scalable prototype stage. Further scaling (large-scale backtesting) would require significantly more compute, and the Kalshi API underwent
 frequent breaking changes that introduced ongoing maintenance overhead for a solo project. The codebase remains a thorough demonstration of end-to-end system design, data pipeline optimization, and
 distributed workload handling.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Table of Contents

 1. Overview
 2. Architecture
 3. Projects
  - BacklashBot
  - BacklashOverseer
  - KalshiBotAPI
  - TradingStrategies
  - BacklashPatterns
  - TradingSimulator
  - TradingGUI
  - MentionMarketBingo
  - BacklashSql
  - Supporting Libraries
 4. Trading Strategies
 5. Technical Indicators
 6. Candlestick Patterns
 7. Machine Learning
 8. Performance Monitoring
 9. Technology Stack
 10. Infrastructure & Deployment
 11. Configuration
 12. Security
 13. Testing
 14. Database

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Overview

BacklashBot is a sophisticated, multi-service automated trading system for the Kalshi prediction market platform. It connects to Kalshi's REST and WebSocket APIs in real time, ingests live order book and
market data, applies a library of technical analysis strategies and machine learning models to identify trade opportunities, and manages order execution — all while continuously backtesting against
historical snapshot data to validate and improve strategies.

The system is designed for a distributed, multi-machine deployment: a central data server stores candlestick and snapshot data on a shared network drive, one or more bot instances run independently and
check in with an Overseer service, and operators interact with a Windows Forms dashboard.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Architecture

 ┌─────────────────────────────────────────────────────────────────┐
 │                        BacklashOverseer                         │
 │          (ASP.NET Core · SignalR Hub · Monitoring API)          │
 └────────────────────────────┬────────────────────────────────────┘
                              │ SignalR / HTTP check-ins
         ┌────────────────────┴─────────────────────┐
         │                                          │
 ┌───────▼───────────────────────────┐   ┌──────────▼──────────┐
 │           BacklashBot             │   │     TradingGUI       │
 │    (ASP.NET Core · CentralBrain)  │   │  (Windows Forms UI)  │
 │                                   │   │  MainForm            │
 │  ┌────────────┐  ┌─────────────┐  │   │  SnapshotViewer      │
 │  │KalshiBotAPI│  │TradingStrats│  │   │  MarketChartRenderer │
 │  │ REST + WSS │  │+ ML Engine  │  │   └─────────────────────┘
 │  └────────────┘  └─────────────┘  │
 │  ┌────────────┐  ┌─────────────┐  │
 │  │ Patterns   │  │  Simulator  │  │
 │  │ (60+ PA)   │  │ (Backtest)  │  │
 │  └────────────┘  └─────────────┘  │
 └──────────────┬────────────────────┘
                │
 ┌──────────────▼────────────────────┐
 │         SQL Server Database       │
 │  + Network Share (Parquet/Snaps)  │
 └───────────────────────────────────┘

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Projects

1. BacklashBot

The core trading engine — an ASP.NET Core service that orchestrates everything at runtime.

Key responsibilities:

 - CentralBrain — top-level orchestration loop, startup sequencing, overnight activities, error recovery with circuit breaker logic
 - Market Management — discovers and subscribes to Kalshi markets; tracks interest scores to prioritise which markets to watch
 - Order Book — maintains real-time order book state per market via WebSocket events (OrderbookChangeTracker, OrderBookService)
 - Candlestick Service — builds and persists candlestick data (minute/hour/day) to Parquet files on a network share
 - Snapshot Service — periodically snapshots full market state (order book + indicators) to disk for later backtesting
 - Trading — delegates to the TradingStrategies library for signal evaluation and trade decisions
 - SignalR Hub — broadcasts live market state and performance metrics to the BacklashOverseer and TradingGUI

Configuration file: BacklashBot/appsettings.json

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

2. BacklashOverseer

A separate ASP.NET Core service providing centralised monitoring and control of one or more BacklashBot instances.

Key components:

 - OverseerHub (OverseerHub.cs, ~51KB) — SignalR hub through which bot instances register, broadcast status, and receive commands
 - Overseer (Overseer.cs, ~25KB) — manages bot lifecycle state, tracks health, aggregates metrics
 - PerformanceMetricsService — collects and exposes performance data from all connected bots
 - wwwroot — static web dashboard served directly by the Overseer

Bot instances check in at a configurable interval (default: 30 seconds) with circuit-breaker protection on the connection.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

3. KalshiBotAPI

A class library wrapping all Kalshi API interactions.

Key components:

 - KalshiAPIService — REST client for market data, order management, and account information; times every call for performance metrics
 - WebSocketConnectionManager — production-grade WebSocket client with:
  - Exponential backoff retry (1s → 2s → 4s → 8s → 16s)
  - Configurable buffer size (default: 16 KB)
  - RSA signature caching (5-minute TTL)
  - Semaphore-based concurrency control
 - SubscriptionManager — manages market data subscriptions with health checks, stale-subscription cleanup, and sequence-gap detection
 - MessageProcessor — batches incoming WebSocket messages (up to 50 per batch, 100ms interval) with optional advanced locking for high-throughput scenarios

Authentication uses RSA private key signing — the key file path is resolved from a secrets directory outside the repository.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

4. TradingStrategies

A class library containing all strategy logic, the simulation engine, and ML integration.

Structure:

 TradingStrategies/
 ├── Strategies/
 │   ├── Strategy.cs          # Base strategy abstraction
 │   ├── Signal.cs            # Trade signal model
 │   ├── SignalCondition.cs   # Composable signal conditions
 │   ├── Scenario.cs          # Named scenario for simulation
 │   └── Strats/              # Concrete strategy implementations
 ├── Trading/
 │   ├── TradingOverseer.cs   # Coordinates strategy execution
 │   ├── SimulationEngine.cs  # Backtesting engine core
 │   ├── StrategySimulation.cs
 │   ├── MarketTypeService.cs
 │   ├── EquityCalculator.cs
 │   └── PerformanceMonitor.cs
 ├── ML/
 │   ├── ResearchBus.cs       # Data bus for ML feature extraction
 │   └── SpikePrediction/     # Price spike ML model
 └── Extensions/

See Trading Strategies for the full strategy list.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

5. BacklashPatterns

A dedicated class library for candlestick pattern recognition and trend calculation.

Key components:

 - PatternSearch.cs (~68KB) — the core pattern-matching engine; searches for all registered patterns against candlestick history
 - TrendCalcs.cs (~28KB) — computes trend-related technical values (slopes, momentum, regression)
 - PatternUtils.cs (~24KB) — shared utilities for candle body/shadow analysis
 - HistoricalPatternCache.cs — caches pattern detection results keyed by market and timestamp
 - PatternVisualizer.cs — generates text/ASCII visualizations of detected patterns
 - PatternDefinitions/ — 60+ individually implemented candlestick patterns (see Candlestick Patterns)

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

6. TradingSimulator

The backtesting engine that replays historical snapshot data through the strategy pipeline.

Key components:

 - TradingSimulatorService (~54KB) — orchestrates full simulation runs; loads snapshots, sequences them, and drives the simulation loop
 - MarketProcessor (~55KB) — processes individual markets through strategies during simulation; calculates P&L, trade fill simulation, and fee accounting (7% fee rate configurable)
 - SimulatorReporting (~46KB) — generates detailed performance reports per strategy and per market
 - DataLoader — loads and deserialises Parquet-based snapshot files
 - StrategyResolver — resolves which strategy implementation to use for a given simulation config

The simulator is designed to run large-scale parallel backtests. Compute constraints were a primary reason development was paused.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

7. TradingGUI

A Windows Forms desktop application for live monitoring and snapshot review.

Key components:

 - MainForm (~56KB) — primary operator dashboard; connects to BacklashBot via SignalR and displays live market state, order books, and performance metrics
 - SnapshotViewer (~144KB) — richly featured viewer for historical snapshot data; allows replay and inspection of any saved market state
 - MarketChartRenderer — renders candlestick/price charts via GDI+
 - TypographyManager (~20KB) — manages consistent fonts and styling across the UI

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

8. MentionMarketBingo

A lightweight standalone utility application — a secondary Windows Forms tool for watching specific Kalshi markets in real time without the full BacklashBot infrastructure.

Key components:

 - Connects to Kalshi WebSocket directly
 - MentionMarketBingoOrderBookService — maintains live order book state
 - MentionMarketBingoWebSocketMonitorService — monitors WebSocket health
 - Simple GUI (Form1) for displaying order book depth and price movement

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

9. BacklashSql

SQL schema scripts for the SQL Server database (kalshibot-prd).

Contains .sql files for table definitions, including OverseerLogs and various performance/metrics tables. The database runs on a dedicated host (192.168.1.210).

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

10. Supporting Libraries

┌──────────────────────────────┬──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ Project                      │ Purpose                                                                                                                              │
├──────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ BacklashInterfaces           │ Shared interface contracts: IPerformanceMonitor, ICentralPerformanceMonitor, trading strategy interfaces, enums, constants, DTOs     │
├──────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ BacklashDTOs                 │ Data transfer objects shared across service boundaries                                                                               │
├──────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ BacklashCommon               │ Shared utilities, configuration helpers, and common services                                                                         │
├──────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ BacklashBotLogging           │ Centralised logging infrastructure (SQL + console log levels configurable)                                                           │
├──────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ OverseerBotShared            │ Models and services shared between Overseer and bot instances                                                                        │
├──────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ BacklashBot.Shared.Tests     │ Shared test infrastructure                                                                                                           │
├──────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ OverseerBotShared            │ Shared between BacklashOverseer and connected bots                                                                                   │
└──────────────────────────────┴──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Trading Strategies

Each strategy implements a common interface and is selected/weighted by TradingOverseer at runtime. All strategies can be backtested independently via TradingSimulator.

┌─────────────────────────────┬───────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ Strategy                    │ Description                                                                                               │
├─────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ BollingerBreakout           │ Enters trades when price breaks outside Bollinger Bands, expecting mean reversion or continuation         │
├─────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Breakout / Breakout2        │ Resistance/support breakout detection with two variants of entry confirmation                             │
├─────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ FlowMomentumStrat           │ Order-flow momentum — uses order book imbalance and trade flow direction                                  │
├─────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ LowLiquidityExitExec        │ Specialised exit strategy for low-liquidity conditions to minimise slippage                               │
├─────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ MLEntrySeekerShared         │ ML-based entry seeker; shares feature extraction logic with the spike prediction model                    │
├─────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ MLSpikePredictionStrat      │ Uses the SpikePredictionModel (ML.NET) to predict imminent price spikes                                   │
├─────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ MomentumTrading             │ Classic price/volume momentum strategy with RSI and MACD confirmation                                     │
├─────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ NothingEverHappensStrat     │ Designed for flat/range-bound markets; fades movement near resistance and support                         │
├─────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ SlopeMomentumStrat          │ (~118KB) The most developed strategy; multi-timeframe slope analysis with comprehensive signal conditions │
├─────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ TryAgainStrat               │ (~216KB) The most complex strategy; extensive signal composition and retry/re-entry logic                 │
└─────────────────────────────┴───────────────────────────────────────────────────────────────────────────────────────────────────────────┘

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Technical Indicators

All indicators are computed in BacklashPatterns/TrendCalcs.cs and configured per-timeframe in appsettings.json:

┌───────────────────────────┬─────────────────────────────────────────┐
│ Indicator                 │ Timeframes                              │
├───────────────────────────┼─────────────────────────────────────────┤
│ RSI                       │ Short (14), Medium (14), Long (14)      │
├───────────────────────────┼─────────────────────────────────────────┤
│ MACD                      │ Medium (12/26/9), Long (12/26/9)        │
├───────────────────────────┼─────────────────────────────────────────┤
│ EMA                       │ Medium (14), Long (14)                  │
├───────────────────────────┼─────────────────────────────────────────┤
│ Bollinger Bands           │ Medium (20, 2σ), Long (20, 2σ)          │
├───────────────────────────┼─────────────────────────────────────────┤
│ ATR                       │ Medium (14), Long (14)                  │
├───────────────────────────┼─────────────────────────────────────────┤
│ VWAP                      │ Short (15 periods), Medium (15 periods) │
├───────────────────────────┼─────────────────────────────────────────┤
│ Stochastic Oscillator     │ Short/Medium/Long (14/%K, 3/%D)         │
├───────────────────────────┼─────────────────────────────────────────┤
│ Parabolic SAR             │ AF: 0.02 initial, 0.02 step, 0.2 max    │
├───────────────────────────┼─────────────────────────────────────────┤
│ ADX                       │ 14 periods                              │
├───────────────────────────┼─────────────────────────────────────────┤
│ Resistance/Support Levels │ Exponential clustering, up to 6 levels  │
├───────────────────────────┼─────────────────────────────────────────┤
│ Price Slope               │ Short (5 min), Medium (15 min)          │
└───────────────────────────┴─────────────────────────────────────────┘

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Candlestick Patterns

BacklashPatterns/PatternDefinitions/ implements 60+ Japanese candlestick patterns:

<details>
<summary>Full pattern list (click to expand)</summary>


 - Abandoned Baby
 - Belt Hold
 - Breakaway
 - Closing Marubozu
 - Concealing Baby Swallow
 - Counterattack
 - Dark Cloud Cover
 - Doji (standard, Star, Dragonfly, Gravestone, Long-Legged, Rickshaw Man)
 - Downside Gap Three Methods
 - Engulfing (Bullish/Bearish)
 - Evening Doji Star / Evening Star
 - Hammer / Inverted Hammer / Hanging Man
 - Harami
 - High Wave Candle
 - Hikkake / Modified Hikkake
 - Homing Pigeon
 - Identical Three Crows
 - In-Neck / On-Neck
 - Kicking / Kicking-by-Length
 - Ladder Bottom
 - Long Line / Short Line Candle
 - Marubozu
 - Mat Hold
 - Morning Doji Star / Morning Star
 - Piercing
 - Rising/Falling Three Methods
 - Separating Lines
 - Shooting Star
 - Spinning Top
 - Stalled Pattern
 - Stick Sandwich
 - Takuri
 - Tasuki Gap
 - Three Advancing White Soldiers
 - Three Black Crows
 - Three Inside Up/Down
 - Three Line Strike
 - Three Outside Up/Down
 - Three Stars in the South
 - Thrusting
 - Tristar
 - Two Crows
 - Unique Three River
 - Upside/Downside Gap Three Methods
 - Upside Gap Two Crows

</details>

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Machine Learning

The ML subsystem lives in TradingStrategies/ML/ and TradingSimulator/ML/.

SpikePredictionModel (SpikePrediction/SpikePredictionModel.cs, ~40KB):

 - Built with ML.NET
 - Predicts whether a price spike will occur in the near term
 - Features extracted via ResearchBus.cs — a data bus that aggregates candlestick metrics, order book state, and indicator values into a flat feature vector
 - Used by both MLSpikePredictionStrat (live trading) and MLEntrySeekerShared (entry timing)
 - Training data sourced from historical snapshot files

The TradingSimulator/ML/ folder provides simulation-side ML utilities for evaluating model performance against backtested data.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Performance Monitoring

A comprehensive, opt-in-per-service performance monitoring system spans 18 classes across the codebase.

Design:

 - IPerformanceMonitor interface defines RecordExecutionTime() and RecordSimulationMetrics() in both basic and enablement-aware overloads
 - ICentralPerformanceMonitor extends IPerformanceMonitor and is implemented by CentralPerformanceMonitor — the aggregation point for all bot-wide metrics
 - Each service has a dedicated EnablePerformanceMetrics flag in appsettings.json
 - Metrics are stored to the SQL database and broadcast to the Overseer via SignalR

Classes instrumented: TradingOverseer, StrategySimulation, SimulationEngine, MarketTypeService, EquityCalculator, StrategySelectionHelper, PatternSearch, CentralPerformanceMonitor, MarketProcessor,
WebSocketConnectionManager, KalshiAPIService, CandlestickService, KalshiBotScopeManagerService, OrderbookChangeTracker, WebSocketMonitorService, TradingSnapshotService, PerformanceMonitor,
PerformanceMetricsService.

See Performance_Monitoring_Flow_Chart.md for the full flow diagram.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Technology Stack

┌─────────────────────┬────────────────────────────────────┐
│ Layer               │ Technology                         │
├─────────────────────┼────────────────────────────────────┤
│ Runtime             │ .NET 8                             │
├─────────────────────┼────────────────────────────────────┤
│ Language            │ C#                                 │
├─────────────────────┼────────────────────────────────────┤
│ Web framework       │ ASP.NET Core                       │
├─────────────────────┼────────────────────────────────────┤
│ Real-time comms     │ SignalR                            │
├─────────────────────┼────────────────────────────────────┤
│ Desktop UI          │ Windows Forms                      │
├─────────────────────┼────────────────────────────────────┤
│ Database            │ SQL Server                         │
├─────────────────────┼────────────────────────────────────┤
│ Time-series storage │ Apache Parquet (via network share) │
├─────────────────────┼────────────────────────────────────┤
│ WebSocket           │ Native .NET ClientWebSocket        │
├─────────────────────┼────────────────────────────────────┤
│ Machine learning    │ ML.NET                             │
├─────────────────────┼────────────────────────────────────┤
│ Authentication      │ RSA private key signing            │
├─────────────────────┼────────────────────────────────────┤
│ Documentation       │ DocFX                              │
└─────────────────────┴────────────────────────────────────┘

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Infrastructure & Deployment

Multi-machine Setup

┌──────────────────────┬─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ Machine              │ Role                                                                                                                                                │
├──────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ DESKTOP-ITC50UT      │ Data server — hosts SQL Server, Parquet/snapshot network share (SmokehouseDataStorage), deployment artifact share (SmokehouseCandlestickImport)     │
├──────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Bot host(s)          │ Runs BacklashBot and BacklashOverseer                                                                                                               │
├──────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Operator workstation │ Runs TradingGUI                                                                                                                                     │
└──────────────────────┴─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

Deployment Scripts

Three .bat scripts handle build-publish-zip deployment for each deployable service:

┌──────────────────────────┬─────────────────────────────────────────────────┐
│ Script                   │ Deploys                                         │
├──────────────────────────┼─────────────────────────────────────────────────┤
│ deploy_backlashbot.bat   │ BacklashBot → C:\Deploy\BacklashBot             │
├──────────────────────────┼─────────────────────────────────────────────────┤
│ deploy_overseer.bat      │ BacklashOverseer → C:\Deploy\BacklashOverseer   │
├──────────────────────────┼─────────────────────────────────────────────────┤
│ deploy_tradinggui.bat    │ TradingGUI → C:\Deploy\TradingGUI               │
└──────────────────────────┴─────────────────────────────────────────────────┘

Each script:

 1. Cleans bin/ and obj/
 2. Runs dotnet restore, dotnet build -c Release, dotnet publish
 3. Zips the output (excluding appsettings.json, logs, secrets) with a timestamp
 4. Copies the zip to the network share if reachable, otherwise leaves it local

Documentation

DocFX project (docfx_project/) generates API documentation. serve_docs.bat serves it locally. generate_toc.ps1 auto-generates table-of-contents entries. add_xml_comments.ps1 adds XML doc comment stubs to
C# source files.

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

Secrets are stored in C:\KalshiData\Secrets (not in this repository).

Notable configuration sections (BacklashBot/appsettings.json):

┌───────────────────────────┬────────────────────────────────────────────────────────────────┐
│ Section                   │ Key settings                                                   │
├───────────────────────────┼────────────────────────────────────────────────────────────────┤
│ DataStorage               │ Network share paths, simulation cache directory                │
├───────────────────────────┼────────────────────────────────────────────────────────────────┤
│ DBConnection              │ SQL Server connection string, retry/batch settings             │
├───────────────────────────┼────────────────────────────────────────────────────────────────┤
│ Kalshi                    │ API environment (prd / demo), key references                   │
├───────────────────────────┼────────────────────────────────────────────────────────────────┤
│ Websockets                │ Buffer size, retry delays, batching, subscription health       │
├───────────────────────────┼────────────────────────────────────────────────────────────────┤
│ WatchedMarkets            │ Snapshot interval, candlestick parameters, order book settings │
├───────────────────────────┼────────────────────────────────────────────────────────────────┤
│ MarketData.Calculations   │ All technical indicator periods and parameters                 │
├───────────────────────────┼────────────────────────────────────────────────────────────────┤
│ Central.CentralBrain      │ Overnight window, market limits, launch flags                  │
└───────────────────────────┴────────────────────────────────────────────────────────────────┘

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Security

 - No credentials in source. All secrets (API keys, DB passwords) are resolved from an external secrets directory at runtime using placeholder tokens.
 - RSA authentication. Kalshi API requests are signed with an RSA private key. The public key ID is registered with Kalshi; the private key file never enters the repository.
 - appsettings.local.json excluded from deploys. Deployment scripts explicitly exclude local config overrides from published zips.
 - .gitignore covers build artifacts, local secrets, user-specific IDE files, and runtime logs.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Testing

┌──────────────────────────────────────┬─────────────────────────────────────────────────────────────────┐
│ Project                              │ Type                                                            │
├──────────────────────────────────────┼─────────────────────────────────────────────────────────────────┤
│ BacklashBot.Shared.Tests             │ Shared test helpers / base classes                              │
├──────────────────────────────────────┼─────────────────────────────────────────────────────────────────┤
│ BacklashOverseer → OverseerTests     │ Overseer unit tests                                             │
├──────────────────────────────────────┼─────────────────────────────────────────────────────────────────┤
│ OverseerBotShared.Tests              │ Shared model/service tests                                      │
├──────────────────────────────────────┼─────────────────────────────────────────────────────────────────┤
│ TradingStrategies → simulation       │ Strategy backtesting via TradingSimulator (integration-level)   │
├──────────────────────────────────────┼─────────────────────────────────────────────────────────────────┤
│ BacklashIntegrationTests             │ End-to-end integration tests                                    │
├──────────────────────────────────────┼─────────────────────────────────────────────────────────────────┤
│ TradingGUITests                      │ GUI component tests                                             │
└──────────────────────────────────────┴─────────────────────────────────────────────────────────────────┘

The TradingSimulator functions as both a backtesting tool and a form of system-level integration test: running a strategy against real historical snapshots validates the entire data pipeline and strategy
logic end-to-end.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Database

SQL Server database: kalshibot-prd on 192.168.1.210.

Schema defined in BacklashSql/sql/. Includes tables for:

 - Overseer logs (OverseerLogs.sql)
 - Performance metrics (execution times, simulation metrics)
 - Market data and event logs
 - WebSocket and API event storage (configurable via StoreWebSocketEvents)

Data access uses parameterised retry logic (3 retries, 1-second delay, batch size 100, queue depth up to 10,000) to handle transient SQL connectivity issues.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Built by BacklashStudios · Kalshi prediction market trading bot · .NET 8 · C#
