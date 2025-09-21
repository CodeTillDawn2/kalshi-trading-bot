namespace BacklashBot.Services.Interfaces
{
    /// <summary>IBroadcastService</summary>
    /// <summary>IBroadcastService</summary>
    public interface IBroadcastService : IDisposable
    /// <summary>StopServicesAsync</summary>
    /// <summary>StartServicesAsync</summary>
    {
        Task StartServicesAsync();
        Task StopServicesAsync();
    }
}
