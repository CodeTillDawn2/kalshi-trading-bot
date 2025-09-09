namespace BacklashBot.Services.Interfaces
{
    public interface IBroadcastService : IDisposable
    {
        Task StartServicesAsync();
        Task StopServicesAsync();
        Task BroadcastAllDataToClientAsync(string connectionId);
        void UnsubscribeFromEvents();
    }
}
