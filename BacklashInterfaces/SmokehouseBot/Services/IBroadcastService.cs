namespace BacklashBot.Services.Interfaces
{
    public interface IBroadcastService : IDisposable
    {
        Task StartServicesAsync();
        Task StopServicesAsync();
    }
}
