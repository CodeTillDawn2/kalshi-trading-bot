namespace BacklashBotData.Extensions
{
    /// <summary>
    /// Configuration class for extension methods, providing customizable options
    /// for timestamp handling and other extension behaviors.
    /// </summary>
    public static class ExtensionConfiguration
    {
        /// <summary>
        /// Delegate for timestamp generation. Defaults to UTC now.
        /// Can be customized to use different time zones or sources.
        /// </summary>
        public static Func<DateTime> TimestampProvider { get; set; } = () => DateTime.UtcNow;

        /// <summary>
        /// Resets the timestamp provider to the default UTC implementation.
        /// </summary>
        public static void ResetToUtc() => TimestampProvider = () => DateTime.UtcNow;

        /// <summary>
        /// Sets the timestamp provider to use local time instead of UTC.
        /// </summary>
        public static void UseLocalTime() => TimestampProvider = () => DateTime.Now;

        /// <summary>
        /// Sets a custom timestamp provider.
        /// </summary>
        /// <param name="provider">The custom timestamp provider function.</param>
        public static void SetCustomTimestampProvider(Func<DateTime> provider)
        {
            TimestampProvider = provider ?? (() => DateTime.UtcNow);
        }
    }
}
