namespace BacklashBot.Services.Interfaces
{
    public interface IBroadcastService : IDisposable
    {
        Task StartServicesAsync();
        Task StopServicesAsync();
        Task BroadcastAllDataToClientAsync(string connectionId);
        Task BroadcastAllMarketDataOnDemandAsync();
        void UnsubscribeFromEvents();
    }
}
