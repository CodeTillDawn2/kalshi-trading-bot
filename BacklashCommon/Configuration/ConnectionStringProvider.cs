namespace BacklashCommon.Configuration
{
    /// <summary>
    /// Provides the database connection string to avoid conflicts with other string singletons.
    /// </summary>
    public class ConnectionStringProvider
    {
        /// <summary>
        /// The database connection string value.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Initializes a new instance of the ConnectionStringProvider.
        /// </summary>
        /// <param name="value">The connection string value.</param>
        public ConnectionStringProvider(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}