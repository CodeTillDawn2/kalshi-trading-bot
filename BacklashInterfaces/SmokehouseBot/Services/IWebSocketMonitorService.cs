namespace BacklashBot.Services.Interfaces
{
    public interface IWebSocketMonitorService
    {
        void StartServices(CancellationToken cancellationToken);
        Task ShutdownAsync(CancellationToken cancellationToken);
        Task TriggerConnectionCheckAsync();
        bool IsConnected();
    }
}
