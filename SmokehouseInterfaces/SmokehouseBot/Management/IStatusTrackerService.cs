
namespace SmokehouseBot.Management.Interfaces
{
    public interface IStatusTrackerService : IDisposable
    {
        TaskCompletionSource<bool> InitializationCompleted { get; set; }
        TaskCompletionSource<bool> BrowserReady { get; set; }
        CancellationToken GetCancellationToken();
        void CancelAll();
        void ResetAll();
    }
}