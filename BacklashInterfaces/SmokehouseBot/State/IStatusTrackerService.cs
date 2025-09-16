namespace BacklashBot.State.Interfaces
{
/// <summary>IStatusTrackerService</summary>
/// <summary>IStatusTrackerService</summary>
    public interface IStatusTrackerService : IDisposable
/// <summary>CancelAll</summary>
/// <summary>GetCancellationToken</summary>
    {
/// <summary>ResetAll</summary>
        CancellationToken GetCancellationToken();
        void CancelAll();
        void ResetAll();
    }
}
