using KalshiUI.Constants;
using KalshiUI.Constructs;
using KalshiUI.Data;
using System.Timers;
using SmokehousePatterns;

namespace KalshiUI.Services
{
    public static class SchedulerService
    {
        public static bool ExitEarly = false;
        public static string CurrentMode = "";
        private static readonly System.Timers.Timer ModeCheckTimer;

        public static readonly string Mode_DayTime = "DayTime";
        public static readonly string Mode_NightTime = "NightTime";

        // Static constructor to initialize the timer
        static SchedulerService()
        {
            ModeCheckTimer = new System.Timers.Timer(300000); // 5 minutes in milliseconds
            ModeCheckTimer.Elapsed += CheckModeChange;
            ModeCheckTimer.AutoReset = true; // Repeat every 5 minutes
            ModeCheckTimer.Start();
        }

        public static List<SlimMarket> GetScheduledJob()
        {
            DateTime now = DateTime.Now;

            ExitEarly = false;

            // Convert local time to Eastern Time (ET)
            TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            DateTime easternTime = TimeZoneInfo.ConvertTime(now, easternZone);

            // Check if market is open (8:00 AM ET to 3:00 AM ET)
            bool isMarketOpen = IsMarketOpen(easternTime);

            return isMarketOpen ? DayTime() : NightTime();
        }

        private static void CheckModeChange(object sender, ElapsedEventArgs e)
        {
            // Convert current time to Eastern Time
            TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            DateTime easternTime = TimeZoneInfo.ConvertTime(DateTime.Now, easternZone);

            // Store the previous mode
            string previousMode = CurrentMode;

            // Check current market status
            IsMarketOpen(easternTime);

            // If mode changed, set ExitEarly to true
            if (previousMode != CurrentMode)
            {
                ExitEarly = true;
            }
        }

        private static bool IsMarketOpen(DateTime easternTime)
        {
            // Get the time portion
            TimeSpan timeOfDay = easternTime.TimeOfDay;
            TimeSpan marketOpen = new TimeSpan(8, 0, 0);  // 8:00 AM
            TimeSpan marketClose = new TimeSpan(3, 0, 0); // 3:00 AM

            if (timeOfDay >= marketOpen || timeOfDay < marketClose)
            {
                if (CurrentMode != Mode_DayTime) ExitEarly = true;
                CurrentMode = Mode_DayTime;
                return true;
            }

            if (CurrentMode != Mode_NightTime) ExitEarly = true;
            CurrentMode = Mode_NightTime;
            return false;
        }

        private static List<SlimMarket> NightTime()
        {
            using (var kalshiBotContext = new KalshiBotContext())
            {
                var marketsToAnalyze = kalshiBotContext.Markets
                    .Where(m => m.status != KalshiConstants.Status_Active && (m.LastCandlestick == null || m.LastCandlestick <= m.close_time.AddDays(1))
                    && m.open_time >= DateTime.UtcNow.AddYears(-1))
                    .Select(m => new
                    {
                        m.market_ticker,
                        m.event_ticker,
                        m.LastCandlestick,
                        m.status,
                        m.close_time,
                        m.open_time
                    })
                    .Select(x => new SlimMarket(x.market_ticker, x.event_ticker, x.LastCandlestick, x.status, x.close_time, x.open_time))
                    .ToList();

                return marketsToAnalyze.OrderBy(x => x.LastCandlestick).ToList();
            }
        }

        private static List<SlimMarket> DayTime()
        {
            using (var kalshiBotContext = new KalshiBotContext())
            {
                var marketsToAnalyze = kalshiBotContext.Markets
                    .Where(m => m.status == KalshiConstants.Status_Active && (m.LastCandlestick == null || m.LastCandlestick <= DateTime.UtcNow.AddMinutes(-5)))
                    .OrderBy(x => x.LastCandlestick)
                    .Select(m => new
                    {
                        m.market_ticker,
                        m.event_ticker,
                        m.LastCandlestick,
                        m.status,
                        m.close_time,
                        m.open_time
                    })
                    .Select(x => new SlimMarket(x.market_ticker, x.event_ticker, x.LastCandlestick, x.status, x.close_time, x.open_time))
                    .ToList();

                return marketsToAnalyze;
            }
        }
    }
}