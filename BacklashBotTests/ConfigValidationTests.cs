using System;
using System.ComponentModel.DataAnnotations;
using NUnit.Framework;

using BacklashBot.Configuration;
using BacklashBot.State;
using BacklashBot.Services;
using BacklashBotData.Configuration;
using BacklashCommon.Configuration;
using BacklashDTOs.Configuration;
using KalshiBotAPI.Configuration;
using TradingStrategies.Configuration;

namespace BacklashBotTests
{
    [TestFixture]
    public class ConfigValidationTests
    {
        [Test]
        public void ValidateAllConfigs_Populated_Valid()
        {
            // SecretsConfig
            var secretsConfig = new SecretsConfig
            {
                SecretsPath = "test/path"
            };
            ValidateConfig(secretsConfig);

            // LoggingConfig
            var loggingConfig = new LoggingConfig
            {
                Environment = "test",
                StoreWebSocketEvents = true,
                SqlDatabaseLogLevel = "Information",
                ConsoleLogLevel = "Debug"
            };
            ValidateConfig(loggingConfig);

            // KalshiConfig
            var kalshiConfig = new KalshiConfig
            {
                Environment = "test",
                BotKeyId = "test-key-id",
                BotKeyFile = "test/path/to/key"
            };
            ValidateConfig(kalshiConfig);

            // KalshiAPIServiceConfig
            var kalshiAPIServiceConfig = new KalshiAPIServiceConfig
            {
                EnablePerformanceMetrics = true,
                CandlestickMandatoryOverlapDaysMinute = 1,
                CandlestickMandatoryOverlapDaysHour = 1,
                CandlestickMandatoryOverlapDaysDay = 1
            };
            ValidateConfig(kalshiAPIServiceConfig);

            // WebSocketConnectionManagerConfig
            var webSocketConnectionManagerConfig = new WebSocketConnectionManagerConfig
            {
                BufferSize = 16384,
                MaxRetryAttempts = 5,
                RetryDelays = new int[] { 1000, 2000, 4000, 8000, 16000 },
                SignatureCacheDurationMinutes = 5,
                ResetDelayMs = 5000,
                SemaphoreTimeoutMs = 60000,
                EnablePerformanceMetrics = true
            };
            ValidateConfig(webSocketConnectionManagerConfig);

            // MessageProcessorConfig
            var messageProcessorConfig = new MessageProcessorConfig
            {
                EnableMessageBatching = true,
                MaxBatchSize = 100,
                BatchProcessingIntervalMs = 100,
                MaxSequenceNumbersToKeep = 10000,
                EnablePerformanceMetrics = true,
                PerformanceMetricsLogIntervalMs = 5000,
                DuplicateMessageWarningThreshold = 10,
                DuplicateMessageTimeWindowMs = 60000,
                UseAdvancedLocking = true
            };
            ValidateConfig(messageProcessorConfig);

            // SubscriptionManagerConfig
            var subscriptionManagerConfig = new SubscriptionManagerConfig
            {
                EnableSubscriptionManagerMetrics = true,
                SubscriptionTimeoutMs = 30000,
                ConfirmationTimeoutSeconds = 30,
                RetryDelayMs = 1000,
                MaxQueueSize = 1000,
                BatchSize = 50,
                HealthCheckIntervalMs = 5000
            };
            ValidateConfig(subscriptionManagerConfig);

            // WebSocketMonitorConfig
            var webSocketMonitorConfig = new WebSocketMonitorConfig
            {
                MonitoringIntervalMinutes = 5,
                RetryDelayMinutes = 1,
                EnableWebSocketMonitorMetrics = true
            };
            ValidateConfig(webSocketMonitorConfig);

            // KalshiWebSocketClientConfig
            var kalshiWebSocketClientConfig = new KalshiWebSocketClientConfig
            {
                EnablePerformanceMetrics = true
            };
            ValidateConfig(kalshiWebSocketClientConfig);

            // TradingSnapshotServiceConfig
            var tradingSnapshotServiceConfig = new TradingSnapshotServiceConfig
            {
                SnapshotToleranceSeconds = 60,
                StorageDirectory = "test/storage",
                MaxParallelism = 4,
                EnablePerformanceMetrics = true
            };
            ValidateConfig(tradingSnapshotServiceConfig);

            // SnapshotPeriodHelperConfig
            var snapshotPeriodHelperConfig = new SnapshotPeriodHelperConfig
            {
                SmallGapMinutes = 5.0,
                MaxActiveGapHours = 1.0,
                PriceChangeThreshold = 10
            };
            ValidateConfig(snapshotPeriodHelperConfig);

            // OrderbookChangeTrackerConfig
            var orderbookChangeTrackerConfig = new OrderbookChangeTrackerConfig
            {
                CleanupThresholdMinutes = 60,
                EnablePerformanceMetrics = true
            };
            ValidateConfig(orderbookChangeTrackerConfig);

            // MarketRefreshServiceConfig
            var marketRefreshServiceConfig = new MarketRefreshServiceConfig
            {
                RefreshIntervalMinutes = 5,
                RefreshThresholdRatio = 0.5,
                TimeBudgetRatio = 0.5,
                EnablePerformanceMetrics = true
            };
            ValidateConfig(marketRefreshServiceConfig);

            // PseudoCandlestickExtensionsConfig
            var pseudoCandlestickExtensionsConfig = new PseudoCandlestickExtensionsConfig
            {
                VolumePrecisionDigits = 2
            };
            ValidateConfig(pseudoCandlestickExtensionsConfig);

            // GeneralExecutionConfig
            var generalExecutionConfig = new GeneralExecutionConfig
            {
                BrainInstance = "test-instance",
                QueuesTargetCount = 10,
                RetryDelayMs = 1000,
                AuthTokenValidityHours = 24,
                HardDataStorageLocation = "test/storage",
                DecisionFrequencySeconds = 60,
                RefreshIntervalMinutes = 5,
                SnapshotSchemaVersion = 1
            };
            ValidateConfig(generalExecutionConfig);

            // OverseerClientServiceConfig
            var overseerClientServiceConfig = new OverseerClientServiceConfig
            {
                OverseerConnectionTimeoutSeconds = 30,
                OverseerSemaphoreTimeoutSeconds = 10,
                OverseerDiscoveryIntervalMinutes = 3,
                OverseerCheckInIntervalSeconds = 30,
                OverseerCircuitBreakerFailureThreshold = 5,
                OverseerCircuitBreakerTimeoutMinutes = 5,
                EnablePerformanceMetrics = true
            };
            ValidateConfig(overseerClientServiceConfig);

            // CandlestickServiceConfig
            var candlestickServiceConfig = new CandlestickServiceConfig
            {
                MaxParallelCandlestickTasks = 4,
                EnablePerformanceMetrics = true,
                CandlestickMandatoryOverlapDaysMinute = 1,
                CandlestickMandatoryOverlapDaysHour = 1,
                CandlestickMandatoryOverlapDaysDay = 1,
                HardDataStorageLocation = "test/storage"
            };
            ValidateConfig(candlestickServiceConfig);

            // BroadcastServiceConfig
            var broadcastServiceConfig = new BroadcastServiceConfig
            {
                BroadcastIntervalSeconds = 30,
                BroadcastMaxRetryAttempts = 3,
                BroadcastRetryDelaySeconds = 1,
                EnablePerformanceMetrics = true
            };
            ValidateConfig(broadcastServiceConfig);

            // MarketDataInitializerConfig
            var marketDataInitializerConfig = new MarketDataInitializerConfig
            {
                EnablePerformanceMetrics = true
            };
            ValidateConfig(marketDataInitializerConfig);

            // CentralPerformanceMonitorConfig
            var centralPerformanceMonitorConfig = new CentralPerformanceMonitorConfig
            {
                QueueHighCountAlertThreshold = 80.0,
                RefreshUsageAlertThreshold = 90.0,
                QueueCountAlertThreshold = 100,
                CentralPerformanceMonitor_EnableDatabaseMetrics = true
            };
            ValidateConfig(centralPerformanceMonitorConfig);

            // KalshiBotScopeManagerServiceConfig
            var kalshiBotScopeManagerServiceConfig = new KalshiBotScopeManagerServiceConfig
            {
                EnablePerformanceMetrics = true
            };
            ValidateConfig(kalshiBotScopeManagerServiceConfig);

            // MarketDataConfig with nested CalculationConfig
            var calculationConfig = new CalculationConfig
            {
                TolerancePercentage = 0.01,
                RecentCandlestickDays = 7,
                SlopeShortMinutes = 5,
                SlopeMediumMinutes = 15,
                RSI_Short_Periods = 14,
                RSI_Medium_Periods = 21,
                RSI_Long_Periods = 50,
                MACD_Medium_FastPeriod = 12,
                MACD_Medium_SlowPeriod = 26,
                MACD_Medium_SignalPeriod = 9,
                MACD_Long_FastPeriod = 26,
                MACD_Long_SlowPeriod = 52,
                MACD_Long_SignalPeriod = 18,
                EMA_Medium_Periods = 21,
                EMA_Long_Periods = 50,
                BollingerBands_Medium_Periods = 20,
                BollingerBands_Medium_StdDev = 2.0,
                BollingerBands_Long_Periods = 50,
                BollingerBands_Long_StdDev = 2.0,
                ATR_Medium_Periods = 14,
                ATR_Long_Periods = 21,
                VWAP_Short_Periods = 5,
                VWAP_Medium_Periods = 20,
                Stochastic_Short_Periods = 5,
                Stochastic_Short_DPeriods = 3,
                Stochastic_Medium_Periods = 14,
                Stochastic_Medium_DPeriods = 3,
                Stochastic_Long_Periods = 21,
                Stochastic_Long_DPeriods = 3,
                TradingFeeRate = 0.001,
                PseudoCandlestickLookbackPeriods = 10,
                RecentCandlesticksCount = 100,
                PSAR_InitialAF = 0.02,
                PSAR_MaxAF = 0.2,
                PSAR_AFStep = 0.02,
                ADX_Periods = 14,
                ResistanceLevels_ExponentialMultiplier = 1.5,
                ResistanceLevels_MinCandlestickPercentage = 0.1,
                ResistanceLevels_MaxLevels = 5,
                ResistanceLevels_Sigma = 2.0,
                ResistanceLevels_MinDistance = 10
            };
            ValidateConfig(calculationConfig);

            var marketDataConfig = new MarketDataConfig
            {
                SemaphoreTimeoutMs = 5000,
                TickerBatchSize = 100,
                ApiRetryTimeoutMs = 30000,
                EnablePerformanceMetrics = true,
                PseudoCandlestickLookbackPeriods = 10,
                Calculations = calculationConfig
            };
            ValidateConfig(marketDataConfig);

            // CentralBrainConfig
            var centralBrainConfig = new CentralBrainConfig
            {
                ErrorCheckInterval = TimeSpan.FromMinutes(1),
                StartupRetryInterval = TimeSpan.FromSeconds(30),
                SnapshotInitialDelay = TimeSpan.FromSeconds(10),
                OvernightStart = TimeSpan.FromHours(22),
                OvernightTaskDelay = TimeSpan.FromMinutes(5),
                LaunchDataDashboard = true,
                RunOvernightActivities = true,
                MaxMarketsPerSubscriptionAction = 50,
                HardDataStorageLocation = "test/storage"
            };
            ValidateConfig(centralBrainConfig);

            // TargetCalculationServiceConfig
            var targetCalculationServiceConfig = new TargetCalculationServiceConfig
            {
                NotificationQueueLimit = 100,
                OrderbookQueueLimit = 100,
                EventQueueLimit = 100,
                TickerQueueLimit = 100
            };
            ValidateConfig(targetCalculationServiceConfig);

            // BrainStatusServiceConfig
            var brainStatusServiceConfig = new BrainStatusServiceConfig
            {
                SessionIdLength = 32
            };
            ValidateConfig(brainStatusServiceConfig);

            // SnapshotGroupHelperConfig
            var snapshotGroupHelperConfig = new SnapshotGroupHelperConfig
            {
                EnablePerformanceMetrics = true
            };
            ValidateConfig(snapshotGroupHelperConfig);

            // QueueMonitoringConfig
            var queueMonitoringConfig = new QueueMonitoringConfig
            {
                QueueHighCountAlertThreshold = 80.0,
                RefreshUsageAlertThreshold = 90.0,
                QueueCountAlertThreshold = 100,
                CentralPerformanceMonitor_EnableDatabaseMetrics = true
            };
            ValidateConfig(queueMonitoringConfig);

            // InterestScoreConfig
            var interestScoreConfig = new InterestScoreConfig
            {
                CacheDurationHours = 6,
                EnablePerformanceMetrics = true,
                MaxPerformanceMetricsHistory = 1000
            };
            ValidateConfig(interestScoreConfig);

            // ErrorHandlerConfig
            var errorHandlerConfig = new ErrorHandlerConfig
            {
                ErrorWindowMinutes = 5,
                ErrorThreshold = 10,
                InternetCheckMaxAttempts = 100,
                InternetCheckInitialDelayMs = 1000,
                InternetCheckMaxDelayMs = 60000
            };
            ValidateConfig(errorHandlerConfig);

            // OrderBookServiceConfig
            var orderBookServiceConfig = new OrderBookServiceConfig
            {
                SemaphoreTimeoutMs = 5000,
                QueueLimit = 1000,
                EventQueueSemaphoreTimeoutMs = 5000,
                EnablePerformanceMetrics = true
            };
            ValidateConfig(orderBookServiceConfig);

            // BacklashBotDataConfig
            var backlashBotDataConfig = new BacklashBotDataConfig
            {
                MaxRetryCount = 3,
                RetryDelaySeconds = 1.0,
                BatchSize = 100,
                MaxQueueSize = 1000,
                WorkersPerQueue = 4,
                EnablePerformanceMetrics = true
            };
            ValidateConfig(backlashBotDataConfig);
        }

        private void ValidateConfig(object config)
        {
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(config, new ValidationContext(config), validationResults, true);
            Assert.IsTrue(isValid, $"Validation failed for {config.GetType().Name}: {string.Join(", ", validationResults.Select(r => r.ErrorMessage))}");
        }
    }
}