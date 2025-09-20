namespace BacklashBot.Management.Interfaces
{
    /// <summary>
    /// Interface for brain status service that manages brain instance initialization and status.
    /// </summary>
    public interface IBrainStatusService : IDisposable
    {
        /// <summary>
        /// Gets the unique brain lock GUID for this brain instance.
        /// </summary>
        Guid BrainLock { get; }

        /// <summary>
        /// Gets the session identifier string for this brain instance.
        /// </summary>
        string SessionIdentifier { get; }

        /// <summary>
        /// Ensures the brain status service is initialized asynchronously.
        /// </summary>
        /// <returns>A task representing the initialization operation.</returns>
        Task EnsureInitializedAsync();
    }
}
