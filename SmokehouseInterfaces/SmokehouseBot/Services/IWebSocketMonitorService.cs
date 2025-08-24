namespace SmokehouseBot.Services.Interfaces
{
    public interface IWebSocketMonitorService
    {
        void StartServices(CancellationToken cancellationToken);
        Task StopServicesAsync(CancellationToken cancellationToken);
        Task TriggerConnectionCheckAsync();
        bool IsConnected();
    }
}
