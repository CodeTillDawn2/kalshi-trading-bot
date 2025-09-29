using BacklashInterfaces.PerformanceMetrics;
using OverseerBotShared;

namespace BacklashBot.Shared.Tests
{
    public class DTOTests
    {
        [Fact(DisplayName = "CheckInData: initializes with test values, verifies all properties match")]
        public void CheckInData_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var checkInData = new CheckInData
            {
                BrainInstanceName = "TestBrain",
                Markets = new List<string> { "AAPL", "GOOGL" },
                ErrorCount = 5,
                LastSnapshot = DateTime.UtcNow
            };

            // Assert
            Assert.Equal("TestBrain", checkInData.BrainInstanceName);
            Assert.Equal(2, checkInData.Markets?.Count);
            Assert.Equal(5L, checkInData.ErrorCount);
        }

        [Fact(DisplayName = "CheckInResponse: initializes with success/message/tickers/timestamp, verifies values")]
        public void CheckInResponse_ShouldInitializeCorrectly()
        {
            // Arrange
            var targetTickers = new[] { "AAPL", "MSFT", "GOOGL" };

            // Act
            var response = new CheckInResponse
            {
                Success = true,
                Message = "Check-in processed successfully",
                TargetTickers = targetTickers,
                Timestamp = DateTime.UtcNow
            };

            // Assert
            Assert.True(response.Success);
            Assert.Equal("Check-in processed successfully", response.Message);
            Assert.Equal(targetTickers, response.TargetTickers);
            Assert.NotEqual(default, response.Timestamp);
        }

        [Fact(DisplayName = "TargetTickersConfirmationResponse: initializes with success/brain/message/timestamp, verifies values")]
        public void TargetTickersConfirmationResponse_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var response = new TargetTickersConfirmationResponse
            {
                Success = true,
                BrainInstanceName = "TestBrain",
                Message = "Target tickers confirmed",
                Timestamp = DateTime.UtcNow
            };

            // Assert
            Assert.True(response.Success);
            Assert.Equal("TestBrain", response.BrainInstanceName);
            Assert.Equal("Target tickers confirmed", response.Message);
            Assert.NotEqual(default, response.Timestamp);
        }

        [Fact(DisplayName = "PerformanceMetricsData: initializes with brain/timestamp/metrics, verifies values")]
        public void PerformanceMetricsData_ShouldInitializeCorrectly()
        {
            // Arrange
            var allMetrics = new List<PerformanceMetricEntry>
            {
                new PerformanceMetricEntry
                {
                    ClassName = "MessageProcessor",
                    Metric = new GeneralPerformanceMetric
                    {
                        Id = "total-messages",
                        Name = "Total Messages Processed",
                        Description = "Total number of messages processed",
                        Value = 1000,
                        Unit = "count",
                        VisualType = VisualType.Counter,
                        Category = "Message Processing"
                    }
                },
                new PerformanceMetricEntry
                {
                    ClassName = "MessageProcessor",
                    Metric = new GeneralPerformanceMetric
                    {
                        Id = "total-processing-time",
                        Name = "Total Processing Time",
                        Description = "Total processing time in milliseconds",
                        Value = 5000,
                        Unit = "ms",
                        VisualType = VisualType.NumericDisplay,
                        Category = "Message Processing"
                    }
                },
                new PerformanceMetricEntry
                {
                    ClassName = "MessageProcessor",
                    Metric = new GeneralPerformanceMetric
                    {
                        Id = "avg-processing-time",
                        Name = "Average Processing Time",
                        Description = "Average processing time per message",
                        Value = 5.0,
                        Unit = "ms",
                        VisualType = VisualType.SpeedDial,
                        Category = "Message Processing"
                    }
                },
                new PerformanceMetricEntry
                {
                    ClassName = "MessageProcessor",
                    Metric = new GeneralPerformanceMetric
                    {
                        Id = "messages-per-second",
                        Name = "Messages Per Second",
                        Description = "Processing rate",
                        Value = 200.0,
                        Unit = "msg/s",
                        VisualType = VisualType.NumericDisplay,
                        Category = "Message Processing"
                    }
                },
                new PerformanceMetricEntry
                {
                    ClassName = "MessageProcessor",
                    Metric = new GeneralPerformanceMetric
                    {
                        Id = "queue-depth",
                        Name = "Order Book Queue Depth",
                        Description = "Current queue depth",
                        Value = 50,
                        Unit = "count",
                        VisualType = VisualType.Counter,
                        Category = "Message Processing"
                    }
                },
                new PerformanceMetricEntry
                {
                    ClassName = "MessageProcessor",
                    Metric = new GeneralPerformanceMetric
                    {
                        Id = "duplicate-count",
                        Name = "Duplicate Message Count",
                        Description = "Number of duplicate messages detected",
                        Value = 5,
                        Unit = "count",
                        VisualType = VisualType.Counter,
                        Category = "Message Processing"
                    }
                },
                new PerformanceMetricEntry
                {
                    ClassName = "MessageProcessor",
                    Metric = new GeneralPerformanceMetric
                    {
                        Id = "duplicates-in-window",
                        Name = "Duplicates In Window",
                        Description = "Duplicates detected in current window",
                        Value = 2,
                        Unit = "count",
                        VisualType = VisualType.Counter,
                        Category = "Message Processing"
                    }
                }
            };

            // Act
            var metrics = new PerformanceMetricsData
            {
                BrainInstanceName = "TestBrain",
                Timestamp = DateTime.UtcNow,
                AllMetrics = allMetrics
            };

            // Assert
            Assert.Equal("TestBrain", metrics.BrainInstanceName);
            Assert.NotEqual(default, metrics.Timestamp);
            Assert.NotNull(metrics.AllMetrics);
            Assert.Equal(7, metrics.AllMetrics.Count);

            // Verify specific metrics
            var totalMessagesMetric = metrics.AllMetrics.First(m => m.Metric.Id == "total-messages");
            Assert.Equal(1000, totalMessagesMetric.Metric.Value);
            Assert.Equal("count", totalMessagesMetric.Metric.Unit);

            var avgProcessingTimeMetric = metrics.AllMetrics.First(m => m.Metric.Id == "avg-processing-time");
            Assert.Equal(5.0, avgProcessingTimeMetric.Metric.Value);
            Assert.Equal("ms", avgProcessingTimeMetric.Metric.Unit);
        }

        [Fact(DisplayName = "BrainStatusData: initializes with comprehensive status data, verifies all properties")]
        public void BrainStatusData_ShouldInitializeCorrectly()
        {
            // Arrange
            var markets = new List<string> { "AAPL", "GOOGL" };
            var watchedMarkets = new List<MarketWatchData>
            {
                new MarketWatchData
                {
                    MarketTicker = "AAPL",
                    InterestScore = 85.5,
                    InterestScoreDate = DateTime.UtcNow.AddHours(-1),
                    LastWatched = DateTime.UtcNow.AddMinutes(-30),
                    AverageWebsocketEventsPerMinute = 120.5
                }
            };

            // Act
            var brainStatus = new BrainStatusData
            {
                BrainInstanceName = "TestBrain",
                Markets = markets,
                ErrorCount = 3,
                LastSnapshot = DateTime.UtcNow.AddMinutes(-15),
                LastCheckIn = DateTime.UtcNow,
                IsStartingUp = false,
                IsShuttingDown = false,
                WatchPositions = true,
                WatchOrders = true,
                ManagedWatchList = true,
                CaptureSnapshots = true,
                TargetWatches = 150,
                MinimumInterest = 10.0,
                UsageMin = 60.0,
                UsageMax = 85.0,
                CurrentCpuUsage = 72.3,
                EventQueueAvg = 8.5,
                TickerQueueAvg = 4.2,
                NotificationQueueAvg = 1.8,
                OrderbookQueueAvg = 6.9,
                LastRefreshCycleSeconds = 2.1,
                LastRefreshCycleInterval = 45.0,
                LastRefreshMarketCount = 120.0,
                LastRefreshUsagePercentage = 68.0,
                LastRefreshTimeAcceptable = true,
                LastPerformanceSampleDate = DateTime.UtcNow.AddMinutes(-2),
                IsWebSocketConnected = true,
                WatchedMarkets = watchedMarkets
            };

            // Assert
            Assert.Equal("TestBrain", brainStatus.BrainInstanceName);
            Assert.Equal(markets, brainStatus.Markets);
            Assert.Equal(3L, brainStatus.ErrorCount);
            Assert.NotEqual(default, brainStatus.LastSnapshot);
            Assert.NotEqual(default, brainStatus.LastCheckIn);
            Assert.False(brainStatus.IsStartingUp);
            Assert.False(brainStatus.IsShuttingDown);
            Assert.True(brainStatus.WatchPositions);
            Assert.True(brainStatus.WatchOrders);
            Assert.True(brainStatus.ManagedWatchList);
            Assert.True(brainStatus.CaptureSnapshots);
            Assert.Equal(150, brainStatus.TargetWatches);
            Assert.Equal(10.0, brainStatus.MinimumInterest);
            Assert.Equal(60.0, brainStatus.UsageMin);
            Assert.Equal(85.0, brainStatus.UsageMax);
            Assert.Equal(72.3, brainStatus.CurrentCpuUsage);
            Assert.Equal(8.5, brainStatus.EventQueueAvg);
            Assert.Equal(4.2, brainStatus.TickerQueueAvg);
            Assert.Equal(1.8, brainStatus.NotificationQueueAvg);
            Assert.Equal(6.9, brainStatus.OrderbookQueueAvg);
            Assert.Equal(2.1, brainStatus.LastRefreshCycleSeconds);
            Assert.Equal(45.0, brainStatus.LastRefreshCycleInterval);
            Assert.Equal(120.0, brainStatus.LastRefreshMarketCount);
            Assert.Equal(68.0, brainStatus.LastRefreshUsagePercentage);
            Assert.True(brainStatus.LastRefreshTimeAcceptable);
            Assert.True(brainStatus.IsWebSocketConnected);
            Assert.Equal(watchedMarkets, brainStatus.WatchedMarkets);
        }

        [Fact(DisplayName = "MarketWatchData: initializes with market/brain/interest data, verifies values")]
        public void MarketWatchData_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var marketWatch = new MarketWatchData
            {
                MarketTicker = "AAPL",
                Brain = Guid.NewGuid(),
                InterestScore = 92.5,
                InterestScoreDate = DateTime.UtcNow.AddHours(-2),
                LastWatched = DateTime.UtcNow.AddMinutes(-45),
                AverageWebsocketEventsPerMinute = 89.3
            };

            // Assert
            Assert.Equal("AAPL", marketWatch.MarketTicker);
            Assert.NotEqual(Guid.Empty, marketWatch.Brain);
            Assert.Equal(92.5, marketWatch.InterestScore);
            Assert.NotEqual(default, marketWatch.InterestScoreDate);
            Assert.NotEqual(default, marketWatch.LastWatched);
            Assert.Equal(89.3, marketWatch.AverageWebsocketEventsPerMinute);
        }

        [Fact(DisplayName = "HandshakeResponse: initializes with success/token/message, verifies values")]
        public void HandshakeResponse_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var response = new HandshakeResponse
            {
                Success = true,
                AuthToken = "test-token-123",
                Message = "Handshake successful"
            };

            // Assert
            Assert.True(response.Success);
            Assert.Equal("test-token-123", response.AuthToken);
            Assert.Equal("Handshake successful", response.Message);
        }

        [Fact(DisplayName = "PerformanceMetricsResponse: initializes with success/message/timestamp, verifies values")]
        public void PerformanceMetricsResponse_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var response = new PerformanceMetricsResponse
            {
                Success = true,
                Message = "Metrics processed successfully",
                Timestamp = DateTime.UtcNow
            };

            // Assert
            Assert.True(response.Success);
            Assert.Equal("Metrics processed successfully", response.Message);
            Assert.NotEqual(default, response.Timestamp);
        }

        [Fact(DisplayName = "MessageResponse: initializes with success/type/timestamp/message, verifies values")]
        public void MessageResponse_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var response = new MessageResponse
            {
                Success = true,
                MessageType = "refresh_data",
                Timestamp = DateTime.UtcNow,
                Message = "Data refresh completed"
            };

            // Assert
            Assert.True(response.Success);
            Assert.Equal("refresh_data", response.MessageType);
            Assert.NotEqual(default, response.Timestamp);
            Assert.Equal("Data refresh completed", response.Message);
        }
    }
}
