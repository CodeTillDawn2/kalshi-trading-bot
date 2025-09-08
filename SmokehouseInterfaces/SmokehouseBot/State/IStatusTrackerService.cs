namespace SmokehouseBot.State.Interfaces
{
    public interface IStatusTrackerService : IDisposable
    {
        CancellationToken GetCancellationToken();
        void CancelAll();
        void ResetAll();
    }
}