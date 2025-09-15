namespace BacklashDTOs.Helpers
{
    /// <summary>
    /// Provides utility methods for working with Unix timestamps.
    /// </summary>
    public static class UnixHelper
    {

        /// <summary>
        /// Converts a Unix timestamp to a DateTime.
        /// </summary>
        /// <param name="unixTimestamp">The Unix timestamp in seconds.</param>
        /// <returns>The DateTime equivalent in UTC.</returns>
        public static DateTime ConvertFromUnixTimestamp(long unixTimestamp)
        {
            DateTimeOffset utcDateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);

            return utcDateTime.UtcDateTime;
        }

        /// <summary>
        /// Converts a DateTime to a Unix timestamp.
        /// </summary>
        /// <param name="dateTime">The DateTime to convert.</param>
        /// <returns>The Unix timestamp in seconds.</returns>
        public static long ConvertToUnixTimestamp(DateTime dateTime)
        {
            // Convert the UTC DateTime to Unix Timestamp
            long unixTimestamp = ((DateTimeOffset)dateTime).ToUnixTimeSeconds();

            return unixTimestamp;
        }

        /// <summary>
        /// Adds a number of days to a Unix timestamp.
        /// </summary>
        /// <param name="unixStart">The starting Unix timestamp.</param>
        /// <param name="NumberOfDays">The number of days to add.</param>
        /// <returns>The new Unix timestamp.</returns>
        public static long AddUnixDays(long unixStart, int NumberOfDays)
        {
            int additional_ts = 60 * 60 * 24 * NumberOfDays; // 60 seconds per minute * 60 minutes * 24 hours = 1 day
            return unixStart + additional_ts;
        }
        /// <summary>
        /// Adds a number of hours to a Unix timestamp.
        /// </summary>
        /// <param name="unixStart">The starting Unix timestamp.</param>
        /// <param name="numberOfHours">The number of hours to add.</param>
        /// <returns>The new Unix timestamp.</returns>
        public static long AddUnixHours(long unixStart, int numberOfHours)
        {
            int additional_ts = 60 * 60 * numberOfHours; // 60 seconds per minute * 60 minutes = 1 hour
            return unixStart + additional_ts;
        }

        /// <summary>
        /// Adds a number of minutes to a Unix timestamp.
        /// </summary>
        /// <param name="unixStart">The starting Unix timestamp.</param>
        /// <param name="numberOfMinutes">The number of minutes to add.</param>
        /// <returns>The new Unix timestamp.</returns>
        public static long AddUnixMinutes(long unixStart, int numberOfMinutes)
        {
            int additional_ts = 60 * numberOfMinutes; // 60 seconds per minute
            return unixStart + additional_ts;
        }
        /// <summary>
        /// Adds a number of months to a Unix timestamp (approximate).
        /// </summary>
        /// <param name="unixStart">The starting Unix timestamp.</param>
        /// <param name="NumberOfMonths">The number of months to add.</param>
        /// <returns>The new Unix timestamp.</returns>
        public static long AddUnixMonths(long unixStart, int NumberOfMonths)
        {
            int additional_ts = 60 * 60 * 24 * 31 * NumberOfMonths; // 60 seconds per minte * 60 minutes * 24 hours * 31 days = ~1 month
            return unixStart + additional_ts;
        }
    }


}
