using BacklashDTOs.Data;
using KalshiBotData.Extensions;
using KalshiBotData.Models;

namespace BacklashBotTests
{
    /// <summary>
    /// Unit tests for extension methods used in the Kalshi trading bot.
    /// This test suite validates the accuracy of DTO transformations, null parameter validation,
    /// update functionality, batch operations, deep cloning, and performance metrics collection
    /// for various model extension methods.
    /// </summary>
    public class ExtensionTests
    {
        #region Extension Method Tests

        /// <summary>
        /// Tests AnnouncementExtensions for null parameter validation and transformation accuracy.
        /// </summary>
        [Test]
        public void TestAnnouncementExtensions()
        {
            TestContext.Out.WriteLine("Testing AnnouncementExtensions for null parameter validation and transformation accuracy.");
            // Test null parameter validation
            Assert.Throws<ArgumentNullException>(() => ((Announcement)null).ToAnnouncementDTO());
            Assert.Throws<ArgumentNullException>(() => ((AnnouncementDTO)null).ToAnnouncement());
            Assert.Throws<ArgumentNullException>(() => ((Announcement)null).UpdateAnnouncement(new AnnouncementDTO()));
            Assert.Throws<ArgumentNullException>(() => new Announcement { Message = "test", Status = "test", Type = "test" }.UpdateAnnouncement(null));

            // Test transformation accuracy
            var originalAnnouncement = new Announcement
            {
                AnnouncementID = 123,
                DeliveryTime = DateTime.UtcNow,
                Message = "Test Message",
                Status = "Active",
                Type = "Info",
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                LastModifiedDate = DateTime.UtcNow.AddDays(-1)
            };

            var dto = originalAnnouncement.ToAnnouncementDTO();
            var reconstructed = dto.ToAnnouncement();

            Assert.That(reconstructed.AnnouncementID, Is.EqualTo(originalAnnouncement.AnnouncementID));
            Assert.That(reconstructed.Message, Is.EqualTo(originalAnnouncement.Message));
            Assert.That(reconstructed.Status, Is.EqualTo(originalAnnouncement.Status));
            Assert.That(reconstructed.Type, Is.EqualTo(originalAnnouncement.Type));

            // Test update functionality
            var updateDTO = new AnnouncementDTO
            {
                AnnouncementID = 123,
                Message = "Updated Message",
                Status = "Inactive",
                Type = "Warning",
                DeliveryTime = DateTime.UtcNow,
                CreatedDate = originalAnnouncement.CreatedDate,
                LastModifiedDate = originalAnnouncement.LastModifiedDate
            };

            var updated = originalAnnouncement.UpdateAnnouncement(updateDTO);
            Assert.That(updated.Message, Is.EqualTo("Updated Message"));
            Assert.That(updated.Status, Is.EqualTo("Inactive"));
            Assert.That(updated.Type, Is.EqualTo("Warning"));

            // Test batch transformations
            var announcements = new List<Announcement> { originalAnnouncement, originalAnnouncement };
            var dtos = announcements.ToAnnouncementDTOs();
            var reconstructedList = dtos.ToAnnouncements();

            Assert.That(reconstructedList.Count, Is.EqualTo(2));
            Assert.That(reconstructedList[0].AnnouncementID, Is.EqualTo(originalAnnouncement.AnnouncementID));

            // Test deep clone
            var cloned = originalAnnouncement.DeepClone();
            Assert.That(cloned.AnnouncementID, Is.EqualTo(originalAnnouncement.AnnouncementID));
            Assert.That(cloned, Is.Not.SameAs(originalAnnouncement)); // Different object references

            var clonedDTO = dto.DeepClone();
            Assert.That(clonedDTO.AnnouncementID, Is.EqualTo(dto.AnnouncementID));
            Assert.That(clonedDTO, Is.Not.SameAs(dto)); // Different object references

            // Test performance metrics collection
            var metrics = AnnouncementExtensions.GetPerformanceMetrics();
            Assert.That(metrics, Is.Not.Null);
            Assert.That(metrics.ContainsKey("ToAnnouncementDTO"), Is.True);
            TestContext.Out.WriteLine("Result: All AnnouncementExtensions validated successfully.");
        }

        /// <summary>
        /// Tests BrainInstanceExtensions for null parameter validation and transformation accuracy.
        /// </summary>
        [Test]
        public void TestBrainInstanceExtensions()
        {
            TestContext.Out.WriteLine("Testing BrainInstanceExtensions for null parameter validation and transformation accuracy.");
            // Test null parameter validation
            Assert.Throws<ArgumentNullException>(() => ((BrainInstance)null).ToBrainInstanceDTO());
            Assert.Throws<ArgumentNullException>(() => ((BrainInstanceDTO)null).ToBrainInstance());
            Assert.Throws<ArgumentNullException>(() => ((BrainInstance)null).UpdateBrainInstance(new BrainInstanceDTO()));
            Assert.Throws<ArgumentNullException>(() => new BrainInstance().UpdateBrainInstance(null));

            // Test transformation accuracy
            var testGuid = Guid.NewGuid();
            var originalBrainInstance = new BrainInstance
            {
                BrainInstanceName = "TestBrain",
                WatchPositions = true,
                WatchOrders = false,
                ManagedWatchList = true,
                TargetWatches = 5,
                BrainLock = testGuid,
                UsageMin = 10,
                UsageMax = 100,
                CaptureSnapshots = true,
                MinimumInterest = 0.5,
                LastSeen = DateTime.UtcNow
            };

            var dto = originalBrainInstance.ToBrainInstanceDTO();
            var reconstructed = dto.ToBrainInstance();

            Assert.That(reconstructed.BrainInstanceName, Is.EqualTo(originalBrainInstance.BrainInstanceName));
            Assert.That(reconstructed.WatchPositions, Is.EqualTo(originalBrainInstance.WatchPositions));
            Assert.That(reconstructed.UsageMin, Is.EqualTo(originalBrainInstance.UsageMin));
            Assert.That(reconstructed.MinimumInterest, Is.EqualTo(originalBrainInstance.MinimumInterest));

            // Test update functionality with validation
            var updateDTO = new BrainInstanceDTO
            {
                BrainInstanceName = "TestBrain",
                BrainLock = Guid.NewGuid(),
                LastSeen = DateTime.UtcNow
            };

            var updated = originalBrainInstance.UpdateBrainInstance(updateDTO);
            Assert.That(updated.BrainLock, Is.EqualTo(updateDTO.BrainLock));

            // Test invalid update (name mismatch)
            var invalidUpdateDTO = new BrainInstanceDTO
            {
                BrainInstanceName = "DifferentName",
                BrainLock = Guid.NewGuid()
            };

            Assert.Throws<ArgumentException>(() => originalBrainInstance.UpdateBrainInstance(invalidUpdateDTO));

            // Test batch transformations
            var brainInstances = new List<BrainInstance> { originalBrainInstance };
            var dtos = brainInstances.ToBrainInstanceDTOs();
            var reconstructedList = dtos.ToBrainInstances();

            Assert.That(reconstructedList.Count, Is.EqualTo(1));
            Assert.That(reconstructedList[0].BrainInstanceName, Is.EqualTo(originalBrainInstance.BrainInstanceName));

            // Test deep clone
            var cloned = originalBrainInstance.DeepClone();
            Assert.That(cloned.BrainInstanceName, Is.EqualTo(originalBrainInstance.BrainInstanceName));
            Assert.That(cloned, Is.Not.SameAs(originalBrainInstance));

            var clonedDTO = dto.DeepClone();
            Assert.That(clonedDTO.BrainInstanceName, Is.EqualTo(dto.BrainInstanceName));
            Assert.That(clonedDTO, Is.Not.SameAs(dto));

            // Test performance metrics collection
            var metrics = BrainInstanceExtensions.GetPerformanceMetrics();
            Assert.That(metrics, Is.Not.Null);
            Assert.That(metrics.ContainsKey("ToBrainInstanceDTO"), Is.True);
            TestContext.Out.WriteLine("Result: All BrainInstanceExtensions validated successfully.");
        }

        /// <summary>
        /// Tests CandlestickExtensions for null parameter validation and transformation accuracy.
        /// </summary>
        [Test]
        public void TestCandlestickExtensions()
        {
            TestContext.Out.WriteLine("Testing CandlestickExtensions for null parameter validation and transformation accuracy.");
            // Test null parameter validation
            Assert.Throws<ArgumentNullException>(() => ((Candlestick)null).ToCandlestickDTO());
            Assert.Throws<ArgumentNullException>(() => ((CandlestickDTO)null).ToCandlestick());
            Assert.Throws<ArgumentNullException>(() => ((Candlestick)null).UpdateCandlestick(new CandlestickDTO()));
            Assert.Throws<ArgumentNullException>(() => new Candlestick().UpdateCandlestick(null));

            // Test transformation accuracy
            var originalCandlestick = new Candlestick
            {
                market_ticker = "TEST_MARKET",
                interval_type = 1, // 1 minute interval
                end_period_ts = 1640995200,
                end_period_datetime_utc = DateTime.UtcNow,
                year = 2022,
                month = 1,
                day = 1,
                hour = 12,
                minute = 0,
                open_interest = 1000,
                price_close = 5050, // Price * 100 to avoid decimals
                price_high = 5100,
                price_low = 4950,
                price_mean = 5025,
                price_open = 5000,
                price_previous = 4980,
                volume = 500,
                yes_ask_close = 5060,
                yes_ask_high = 5110,
                yes_ask_low = 4960,
                yes_ask_open = 5010,
                yes_bid_close = 5040,
                yes_bid_high = 5090,
                yes_bid_low = 4940,
                yes_bid_open = 4990
            };

            var dto = originalCandlestick.ToCandlestickDTO();
            var reconstructed = dto.ToCandlestick();

            Assert.That(reconstructed.market_ticker, Is.EqualTo(originalCandlestick.market_ticker));
            Assert.That(reconstructed.interval_type, Is.EqualTo(originalCandlestick.interval_type));
            Assert.That(reconstructed.end_period_ts, Is.EqualTo(originalCandlestick.end_period_ts));
            Assert.That(reconstructed.price_close, Is.EqualTo(originalCandlestick.price_close));
            Assert.That(reconstructed.volume, Is.EqualTo(originalCandlestick.volume));

            // Test update functionality with validation
            var updateDTO = new CandlestickDTO
            {
                market_ticker = "TEST_MARKET",
                interval_type = 1,
                end_period_ts = 1640995200,
                price_close = 5100,
                volume = 600
            };

            var updated = originalCandlestick.UpdateCandlestick(updateDTO);
            Assert.That(updated.price_close, Is.EqualTo(5100));
            Assert.That(updated.volume, Is.EqualTo(600));

            // Test invalid update (market ticker mismatch)
            var invalidUpdateDTO = new CandlestickDTO
            {
                market_ticker = "DIFFERENT_MARKET",
                interval_type = 1,
                end_period_ts = 1640995200
            };

            Assert.Throws<ArgumentException>(() => originalCandlestick.UpdateCandlestick(invalidUpdateDTO));

            // Test batch transformations
            var candlesticks = new List<Candlestick> { originalCandlestick };
            var dtos = candlesticks.ToCandlestickDTOs();
            var reconstructedList = dtos.ToCandlesticks();

            Assert.That(reconstructedList.Count, Is.EqualTo(1));
            Assert.That(reconstructedList[0].market_ticker, Is.EqualTo(originalCandlestick.market_ticker));

            // Test deep clone
            var cloned = originalCandlestick.DeepClone();
            Assert.That(cloned.market_ticker, Is.EqualTo(originalCandlestick.market_ticker));
            Assert.That(cloned, Is.Not.SameAs(originalCandlestick));

            var clonedDTO = dto.DeepClone();
            Assert.That(clonedDTO.market_ticker, Is.EqualTo(dto.market_ticker));
            Assert.That(clonedDTO, Is.Not.SameAs(dto));

            // Test performance metrics collection
            var metrics = CandlestickExtensions.GetPerformanceMetrics();
            Assert.That(metrics, Is.Not.Null);
            Assert.That(metrics.ContainsKey("ToCandlestickDTO"), Is.True);
            TestContext.Out.WriteLine("Result: All CandlestickExtensions validated successfully.");
        }

        /// <summary>
        /// Tests EventExtensions for null parameter validation and transformation accuracy.
        /// </summary>
        [Test]
        public void TestEventExtensions()
        {
            TestContext.Out.WriteLine("Testing EventExtensions for null parameter validation and transformation accuracy.");
            // Test null parameter validation
            Assert.Throws<ArgumentNullException>(() => ((Event)null).ToEventDTO());
            Assert.Throws<ArgumentNullException>(() => ((EventDTO)null).ToEvent());
            Assert.Throws<ArgumentNullException>(() => ((Event)null).UpdateEvent(new EventDTO()));
            Assert.Throws<ArgumentNullException>(() => new Event { event_ticker = "test", series_ticker = "test", title = "test", collateral_return_type = "binary", category = "test" }.UpdateEvent(null));

            // Test transformation accuracy
            var originalEvent = new Event
            {
                event_ticker = "TEST_EVENT",
                series_ticker = "TEST_SERIES",
                title = "Test Event",
                sub_title = "Test Subtitle",
                collateral_return_type = "binary",
                mutually_exclusive = true,
                category = "Test Category",
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                LastModifiedDate = DateTime.UtcNow.AddDays(-1)
            };

            var dto = originalEvent.ToEventDTO();
            var reconstructed = dto.ToEvent();

            Assert.That(reconstructed.event_ticker, Is.EqualTo(originalEvent.event_ticker));
            Assert.That(reconstructed.series_ticker, Is.EqualTo(originalEvent.series_ticker));
            Assert.That(reconstructed.title, Is.EqualTo(originalEvent.title));
            Assert.That(reconstructed.category, Is.EqualTo(originalEvent.category));

            // Test update functionality
            var updateDTO = new EventDTO
            {
                event_ticker = "TEST_EVENT",
                series_ticker = "UPDATED_SERIES",
                title = "Updated Event",
                sub_title = "Updated Subtitle",
                collateral_return_type = "multi",
                mutually_exclusive = false,
                category = "Updated Category",
                CreatedDate = originalEvent.CreatedDate,
                LastModifiedDate = originalEvent.LastModifiedDate
            };

            var updated = originalEvent.UpdateEvent(updateDTO);
            Assert.That(updated.title, Is.EqualTo("Updated Event"));
            Assert.That(updated.category, Is.EqualTo("Updated Category"));
            Assert.That(updated.CreatedDate, Is.EqualTo(originalEvent.CreatedDate)); // Should not be updated

            // Test invalid update (ticker mismatch)
            var invalidUpdateDTO = new EventDTO
            {
                event_ticker = "DIFFERENT_EVENT",
                title = "Different Event"
            };

            Assert.Throws<ArgumentException>(() => originalEvent.UpdateEvent(invalidUpdateDTO));

            // Test batch transformations
            var events = new List<Event> { originalEvent };
            var dtos = events.ToEventDTOs();
            var reconstructedList = dtos.ToEvents();

            Assert.That(reconstructedList.Count, Is.EqualTo(1));
            Assert.That(reconstructedList[0].event_ticker, Is.EqualTo(originalEvent.event_ticker));

            // Test deep clone
            var cloned = originalEvent.DeepClone();
            Assert.That(cloned.event_ticker, Is.EqualTo(originalEvent.event_ticker));
            Assert.That(cloned, Is.Not.SameAs(originalEvent));

            var clonedDTO = dto.DeepClone();
            Assert.That(clonedDTO.event_ticker, Is.EqualTo(dto.event_ticker));
            Assert.That(clonedDTO, Is.Not.SameAs(dto));

            // Test performance metrics collection
            var metrics = EventExtensions.GetPerformanceMetrics();
            Assert.That(metrics, Is.Not.Null);
            Assert.That(metrics.ContainsKey("ToEventDTO"), Is.True);
            TestContext.Out.WriteLine("Result: All EventExtensions validated successfully.");
        }

        #endregion
    }
}