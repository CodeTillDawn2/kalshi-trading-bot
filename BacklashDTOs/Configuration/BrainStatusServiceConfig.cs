namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration class for BrainStatusService-specific settings.
    /// </summary>
    public class BrainStatusServiceConfig
    {
        /// <summary>
        /// Gets or sets the length of the session identifier string.
        /// </summary>
        public int SessionIdLength { get; set; } = 5;
    }
}