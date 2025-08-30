namespace SmokehouseBot.Management.Interfaces
{
    public interface IBrainStatusService : IDisposable
    {
        Guid BrainLock { get; }
        string SessionIdentifier { get; }
        Task EnsureInitializedAsync();
    }
}