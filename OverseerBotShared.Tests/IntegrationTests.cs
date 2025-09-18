using System;
using System.Collections.Generic;
using System.Text.Json;
using OverseerBotShared;
using Xunit;

namespace OverseerBotShared.Tests
{
    /// <summary>
    /// Integration tests that verify the communication contract between
    /// BacklashBot (client) and KalshiBotOverseer (server) to guarantee
    /// the same structure being sent is the one expected to be received.
    /// These tests simulate the actual SignalR communication flow.
    /// </summary>
    public class IntegrationTests
    {
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        [Fact]
        public void EndToEnd_CheckInDataFlow_ShouldMaintainExactStructure()
        {
            // Arrange - Simulate what BacklashBot would send
            var originalData = new CheckInData
            {
                BrainInstanceName = "ProductionBrain",
                Markets = new List<string> { "AAPL", "GOOGL", "MSFT", "TSLA", "NVDA" },
                ErrorCount = 3,
                LastSnapshot = new DateTime(2024, 1, 15, 16, 45, 0, DateTimeKind.Utc),
                IsStartingUp = false,
                IsShuttingDown = false,
                WatchPositions = true,
                WatchOrders = true,
                ManagedWatchList = true,
                CaptureSnapshots = true,
                TargetWatches = 150,
                MinimumInterest = 4.5,
                UsageMin = 75.0,
                UsageMax = 95.0,
                CurrentCpuUsage = 72.3,
                EventQueueAvg = 15.2,
                TickerQueueAvg = 22.8,
                NotificationQueueAvg = 7.1,
                OrderbookQueueAvg = 28.4,
                LastRefreshCycleSeconds = 3.2,
                LastRefreshCycleInterval = 60,
                LastRefreshMarketCount = 45,
                LastRefreshUsagePercentage = 85.7,
                LastRefreshTimeAcceptable = true,
                LastPerformanceSampleDate = new DateTime(2024, 1, 15, 16, 40, 0, DateTimeKind.Utc),
                IsWebSocketConnected = true
            };

            // Act - Simulate SignalR serialization (what actually happens during transmission)
            var jsonPayload = JsonSerializer.Serialize(originalData, _jsonOptions);

            // Simulate OverseerHub receiving and deserializing the data
            var receivedData = JsonSerializer.Deserialize<CheckInData>(jsonPayload, _jsonOptions);

            // Assert - Verify the Overseer received the exact same structure
            Assert.NotNull(receivedData);
            Assert.Equal(originalData.BrainInstanceName, receivedData.BrainInstanceName);
            Assert.Equal(originalData.Markets, receivedData.Markets);
            Assert.Equal(originalData.ErrorCount, receivedData.ErrorCount);
            Assert.Equal(originalData.LastSnapshot, receivedData.LastSnapshot);
            Assert.Equal(originalData.IsStartingUp, receivedData.IsStartingUp);
            Assert.Equal(originalData.IsShuttingDown, receivedData.IsShuttingDown);
            Assert.Equal(originalData.WatchPositions, receivedData.WatchPositions);
            Assert.Equal(originalData.WatchOrders, receivedData.WatchOrders);
            Assert.Equal(originalData.ManagedWatchList, receivedData.ManagedWatchList);
            Assert.Equal(originalData.CaptureSnapshots, receivedData.CaptureSnapshots);
            Assert.Equal(originalData.TargetWatches, receivedData.TargetWatches);
            Assert.Equal(originalData.MinimumInterest, receivedData.MinimumInterest);
            Assert.Equal(originalData.UsageMin, receivedData.UsageMin);
            Assert.Equal(originalData.UsageMax, receivedData.UsageMax);
            Assert.Equal(originalData.CurrentCpuUsage, receivedData.CurrentCpuUsage);
            Assert.Equal(originalData.EventQueueAvg, receivedData.EventQueueAvg);
            Assert.Equal(originalData.TickerQueueAvg, receivedData.TickerQueueAvg);
            Assert.Equal(originalData.NotificationQueueAvg, receivedData.NotificationQueueAvg);
            Assert.Equal(originalData.OrderbookQueueAvg, receivedData.OrderbookQueueAvg);
            Assert.Equal(originalData.LastRefreshCycleSeconds, receivedData.LastRefreshCycleSeconds);
            Assert.Equal(originalData.LastRefreshCycleInterval, receivedData.LastRefreshCycleInterval);
            Assert.Equal(originalData.LastRefreshMarketCount, receivedData.LastRefreshMarketCount);
            Assert.Equal(originalData.LastRefreshUsagePercentage, receivedData.LastRefreshUsagePercentage);
            Assert.Equal(originalData.LastRefreshTimeAcceptable, receivedData.LastRefreshTimeAcceptable);
            Assert.Equal(originalData.LastPerformanceSampleDate, receivedData.LastPerformanceSampleDate);
            Assert.Equal(originalData.IsWebSocketConnected, receivedData.IsWebSocketConnected);
        }

        [Fact]
        public void EndToEnd_CheckInResponseFlow_ShouldMaintainExactStructure()
        {
            // Arrange - Simulate what Overseer would send back
            var originalResponse = new CheckInResponse
            {
                Success = true,
                Message = "Check-in processed successfully with target assignments",
                TargetTickers = new[] { "AMD", "CRM", "NOW", "SHOP" },
                Timestamp = new DateTime(2024, 1, 15, 16, 45, 30, DateTimeKind.Utc)
            };

            // Act - Simulate SignalR serialization (what actually happens during transmission)
            var jsonPayload = JsonSerializer.Serialize(originalResponse, _jsonOptions);

            // Simulate BacklashBot receiving and deserializing the response
            var receivedResponse = JsonSerializer.Deserialize<CheckInResponse>(jsonPayload, _jsonOptions);

            // Assert - Verify the BacklashBot received the exact same structure
            Assert.NotNull(receivedResponse);
            Assert.Equal(originalResponse.Success, receivedResponse.Success);
            Assert.Equal(originalResponse.Message, receivedResponse.Message);
            Assert.Equal(originalResponse.TargetTickers, receivedResponse.TargetTickers);
            Assert.Equal(originalResponse.Timestamp, receivedResponse.Timestamp);
        }

        [Fact]
        public void EndToEnd_PerformanceMetricsFlow_ShouldMaintainExactStructure()
        {
            // Arrange - Simulate performance metrics transmission
            var originalMetrics = new PerformanceMetricsData
            {
                BrainInstanceName = "MetricsTestBrain",
                Timestamp = new DateTime(2024, 1, 15, 17, 0, 0, DateTimeKind.Utc),
                MessageProcessorTotalMessagesProcessed = 5000,
                MessageProcessorTotalProcessingTimeMs = 8500,
                MessageProcessorAverageProcessingTimeMs = 1.7,
                MessageProcessorMessagesPerSecond = 42.8,
                MessageProcessorOrderBookQueueDepth = 15,
                MessageProcessorDuplicateMessageCount = 5,
                MessageProcessorDuplicatesInWindow = 2,
                MessageProcessorLastDuplicateWarningTime = new DateTime(2024, 1, 15, 16, 50, 0, DateTimeKind.Utc),
                MessageProcessorMessageTypeCounts = new Dictionary<string, long>
                {
                    ["MarketUpdate"] = 2500,
                    ["OrderBook"] = 1800,
                    ["TradeExecution"] = 700
                },
                ConfigurableMetrics = new Dictionary<string, object>
                {
                    ["AverageResponseTime"] = 125.5,
                    ["MemoryUsageMB"] = 756.2,
                    ["ThreadCount"] = 12,
                    ["ConnectionPoolSize"] = 25
                }
            };

            // Act - Simulate SignalR serialization
            var jsonPayload = JsonSerializer.Serialize(originalMetrics, _jsonOptions);

            // Simulate OverseerHub receiving and deserializing the metrics
            var receivedMetrics = JsonSerializer.Deserialize<PerformanceMetricsData>(jsonPayload, _jsonOptions);

            // Assert - Verify the Overseer received the exact same structure
            Assert.NotNull(receivedMetrics);
            Assert.Equal(originalMetrics.BrainInstanceName, receivedMetrics.BrainInstanceName);
            Assert.Equal(originalMetrics.Timestamp, receivedMetrics.Timestamp);
            Assert.Equal(originalMetrics.MessageProcessorTotalMessagesProcessed, receivedMetrics.MessageProcessorTotalMessagesProcessed);
            Assert.Equal(originalMetrics.MessageProcessorTotalProcessingTimeMs, receivedMetrics.MessageProcessorTotalProcessingTimeMs);
            Assert.Equal(originalMetrics.MessageProcessorAverageProcessingTimeMs, receivedMetrics.MessageProcessorAverageProcessingTimeMs);
            Assert.Equal(originalMetrics.MessageProcessorMessagesPerSecond, receivedMetrics.MessageProcessorMessagesPerSecond);
            Assert.Equal(originalMetrics.MessageProcessorOrderBookQueueDepth, receivedMetrics.MessageProcessorOrderBookQueueDepth);
            Assert.Equal(originalMetrics.MessageProcessorDuplicateMessageCount, receivedMetrics.MessageProcessorDuplicateMessageCount);
            Assert.Equal(originalMetrics.MessageProcessorDuplicatesInWindow, receivedMetrics.MessageProcessorDuplicatesInWindow);
            Assert.Equal(originalMetrics.MessageProcessorLastDuplicateWarningTime, receivedMetrics.MessageProcessorLastDuplicateWarningTime);
            Assert.Equal(originalMetrics.MessageProcessorMessageTypeCounts, receivedMetrics.MessageProcessorMessageTypeCounts);
            Assert.Equal(originalMetrics.ConfigurableMetrics, receivedMetrics.ConfigurableMetrics);
        }

        [Fact]
        public void Transmission_WithNullValues_ShouldHandleCorrectly()
        {
            // Arrange - Test null value handling in transmission
            var originalData = new CheckInData
            {
                BrainInstanceName = null,
                Markets = null,
                LastSnapshot = null,
                LastPerformanceSampleDate = null,
                ErrorCount = 0,
                IsStartingUp = false,
                IsShuttingDown = false,
                WatchPositions = false,
                WatchOrders = false,
                ManagedWatchList = false,
                CaptureSnapshots = false,
                TargetWatches = 0,
                MinimumInterest = 0.0,
                UsageMin = 0.0,
                UsageMax = 0.0,
                CurrentCpuUsage = 0.0,
                EventQueueAvg = 0.0,
                TickerQueueAvg = 0.0,
                NotificationQueueAvg = 0.0,
                OrderbookQueueAvg = 0.0,
                LastRefreshCycleSeconds = 0.0,
                LastRefreshCycleInterval = 0,
                LastRefreshMarketCount = 0,
                LastRefreshUsagePercentage = 0.0,
                LastRefreshTimeAcceptable = false,
                IsWebSocketConnected = false
            };

            // Act - Simulate transmission with nulls
            var jsonPayload = JsonSerializer.Serialize(originalData, _jsonOptions);
            var receivedData = JsonSerializer.Deserialize<CheckInData>(jsonPayload, _jsonOptions);

            // Assert - Verify null handling
            Assert.NotNull(receivedData);
            Assert.Null(receivedData.BrainInstanceName);
            Assert.Null(receivedData.Markets);
            Assert.Null(receivedData.LastSnapshot);
            Assert.Null(receivedData.LastPerformanceSampleDate);
            Assert.Equal(0, receivedData.ErrorCount);
            Assert.False(receivedData.IsStartingUp);
            Assert.False(receivedData.IsShuttingDown);
            Assert.Equal(0.0, receivedData.CurrentCpuUsage);
            Assert.Equal(0, receivedData.TargetWatches);
        }

        [Fact]
        public void MultipleSequentialTransmissions_ShouldMaintainConsistency()
        {
            // Arrange - Test multiple transmissions
            var testDataSets = new List<CheckInData>
            {
                new CheckInData
                {
                    BrainInstanceName = "Brain1",
                    Markets = new List<string> { "MKT1", "MKT2" },
                    ErrorCount = 1,
                    TargetWatches = 100,
                    CurrentCpuUsage = 60.0,
                    IsWebSocketConnected = true
                },
                new CheckInData
                {
                    BrainInstanceName = "Brain2",
                    Markets = new List<string> { "MKT3", "MKT4", "MKT5" },
                    ErrorCount = 2,
                    TargetWatches = 200,
                    CurrentCpuUsage = 70.0,
                    IsWebSocketConnected = false
                },
                new CheckInData
                {
                    BrainInstanceName = "Brain3",
                    Markets = new List<string> { "MKT6" },
                    ErrorCount = 0,
                    TargetWatches = 50,
                    CurrentCpuUsage = 55.0,
                    IsWebSocketConnected = true
                }
            };

            // Act - Simulate multiple transmissions
            var transmittedData = new List<CheckInData>();
            foreach (var data in testDataSets)
            {
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var received = JsonSerializer.Deserialize<CheckInData>(json, _jsonOptions);
                transmittedData.Add(received);
            }

            // Assert - Verify all transmissions maintained structure
            Assert.Equal(3, transmittedData.Count);

            for (int i = 0; i < testDataSets.Count; i++)
            {
                var original = testDataSets[i];
                var transmitted = transmittedData[i];

                Assert.Equal(original.BrainInstanceName, transmitted.BrainInstanceName);
                Assert.Equal(original.Markets, transmitted.Markets);
                Assert.Equal(original.ErrorCount, transmitted.ErrorCount);
                Assert.Equal(original.TargetWatches, transmitted.TargetWatches);
                Assert.Equal(original.CurrentCpuUsage, transmitted.CurrentCpuUsage);
                Assert.Equal(original.IsWebSocketConnected, transmitted.IsWebSocketConnected);
            }
        }

        [Fact]
        public void ErrorResponseTransmission_ShouldMaintainExactStructure()
        {
            // Arrange - Test error response transmission
            var errorResponse = new CheckInResponse
            {
                Success = false,
                Message = "Check-in failed: Invalid brain instance name",
                TargetTickers = Array.Empty<string>(),
                Timestamp = new DateTime(2024, 1, 15, 16, 45, 0, DateTimeKind.Utc)
            };

            // Act - Simulate error transmission
            var jsonPayload = JsonSerializer.Serialize(errorResponse, _jsonOptions);
            var receivedResponse = JsonSerializer.Deserialize<CheckInResponse>(jsonPayload, _jsonOptions);

            // Assert - Verify error structure is maintained
            Assert.NotNull(receivedResponse);
            Assert.False(receivedResponse.Success);
            Assert.Equal("Check-in failed: Invalid brain instance name", receivedResponse.Message);
            Assert.Empty(receivedResponse.TargetTickers);
            Assert.Equal(errorResponse.Timestamp, receivedResponse.Timestamp);
        }

        [Fact]
        public void DTO_PropertyValidation_ShouldEnsureDataIntegrity()
        {
            // Test CheckInData validation
            var checkInData = new CheckInData
            {
                BrainInstanceName = "ValidBrain",
                Markets = new List<string> { "VALID1", "VALID2" },
                ErrorCount = 10,
                TargetWatches = 100,
                CurrentCpuUsage = 85.5,
                IsWebSocketConnected = true
            };

            // Verify all properties are accessible and have expected values
            Assert.Equal("ValidBrain", checkInData.BrainInstanceName);
            Assert.Equal(2, checkInData.Markets.Count);
            Assert.Contains("VALID1", checkInData.Markets);
            Assert.Contains("VALID2", checkInData.Markets);
            Assert.Equal(10L, checkInData.ErrorCount);
            Assert.Equal(100, checkInData.TargetWatches);
            Assert.Equal(85.5, checkInData.CurrentCpuUsage);
            Assert.True(checkInData.IsWebSocketConnected);

            // Test CheckInResponse validation
            var checkInResponse = new CheckInResponse
            {
                Success = true,
                Message = "Success message",
                TargetTickers = new[] { "TICKER1", "TICKER2" },
                Timestamp = DateTime.UtcNow
            };

            Assert.True(checkInResponse.Success);
            Assert.Equal("Success message", checkInResponse.Message);
            Assert.Equal(2, checkInResponse.TargetTickers.Length);
            Assert.Contains("TICKER1", checkInResponse.TargetTickers);
            Assert.Contains("TICKER2", checkInResponse.TargetTickers);
            Assert.True(checkInResponse.Timestamp > DateTime.MinValue);
        }

        [Fact]
        public void ComplexNestedDataStructures_ShouldMaintainIntegrity()
        {
            // Arrange - Test complex nested structures
            var complexData = new CheckInData
            {
                BrainInstanceName = "ComplexBrain",
                Markets = new List<string> { "MARKET_A", "MARKET_B", "MARKET_C", "MARKET_D" },
                ErrorCount = 25,
                LastSnapshot = DateTime.UtcNow,
                IsStartingUp = false,
                IsShuttingDown = false,
                WatchPositions = true,
                WatchOrders = true,
                ManagedWatchList = true,
                CaptureSnapshots = true,
                TargetWatches = 300,
                MinimumInterest = 7.5,
                UsageMin = 80.0,
                UsageMax = 95.0,
                CurrentCpuUsage = 88.5,
                EventQueueAvg = 45.2,
                TickerQueueAvg = 32.8,
                NotificationQueueAvg = 12.1,
                OrderbookQueueAvg = 67.4,
                LastRefreshCycleSeconds = 8.5,
                LastRefreshCycleInterval = 120,
                LastRefreshMarketCount = 75,
                LastRefreshUsagePercentage = 92.3,
                LastRefreshTimeAcceptable = true,
                LastPerformanceSampleDate = DateTime.UtcNow.AddMinutes(-15),
                IsWebSocketConnected = true
            };

            // Act - Multiple round trips to ensure stability
            var json1 = JsonSerializer.Serialize(complexData, _jsonOptions);
            var intermediate = JsonSerializer.Deserialize<CheckInData>(json1, _jsonOptions);
            var json2 = JsonSerializer.Serialize(intermediate, _jsonOptions);
            var final = JsonSerializer.Deserialize<CheckInData>(json2, _jsonOptions);

            // Assert - Verify multiple serialization cycles maintain integrity
            Assert.NotNull(final);
            Assert.Equal(complexData.BrainInstanceName, final.BrainInstanceName);
            Assert.Equal(complexData.Markets.Count, final.Markets.Count);
            Assert.Equal(complexData.ErrorCount, final.ErrorCount);
            Assert.Equal(complexData.TargetWatches, final.TargetWatches);
            Assert.Equal(complexData.CurrentCpuUsage, final.CurrentCpuUsage);
            Assert.Equal(complexData.LastRefreshCycleInterval, final.LastRefreshCycleInterval);

            // Verify JSON strings are identical (ensuring consistent serialization)
            Assert.Equal(json1, json2);
        }
    }
}