namespace BacklashBot.Management.Interfaces
{
/// <summary>IBrainStatusService</summary>
/// <summary>IBrainStatusService</summary>
    public interface IBrainStatusService : IDisposable
/// <summary>Gets or sets the SessionIdentifier.</summary>
    {
        Guid BrainLock { get; }
        string SessionIdentifier { get; }
        Task EnsureInitializedAsync();
    }
}
