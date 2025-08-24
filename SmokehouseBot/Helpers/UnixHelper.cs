namespace SmokehouseBot.Helpers
{
    public static class UnixHelper
    {

        public static DateTime ConvertFromUnixTimestamp(long unixTimestamp)
        {
            DateTimeOffset utcDateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);

            return utcDateTime.UtcDateTime;
        }

        public static long ConvertToUnixTimestamp(DateTime dateTime)
        {
            // Convert the UTC DateTime to Unix Timestamp
            long unixTimestamp = ((DateTimeOffset)dateTime).ToUnixTimeSeconds();

            return unixTimestamp;
        }

        public static long AddUnixDays(long unixStart, int NumberOfDays)
        {
            int additional_ts = 60 * 60 * 24 * NumberOfDays; // 60 seconds per minute * 60 minutes * 24 hours = 1 day
            return unixStart + additional_ts;
        }
        public static long AddUnixHours(long unixStart, int numberOfHours)
        {
            int additional_ts = 60 * 60 * numberOfHours; // 60 seconds per minute * 60 minutes = 1 hour
            return unixStart + additional_ts;
        }

        public static long AddUnixMinutes(long unixStart, int numberOfMinutes)
        {
            int additional_ts = 60 * numberOfMinutes; // 60 seconds per minute
            return unixStart + additional_ts;
        }
        public static long AddUnixMonths(long unixStart, int NumberOfMonths)
        {
            int additional_ts = 60 * 60 * 24 * 31 * NumberOfMonths; // 60 seconds per minte * 60 minutes * 24 hours * 31 days = ~1 month
            return unixStart + additional_ts;
        }
    }


}
