namespace BacklashInterfaces.SmokehouseBot.Services
{
    public interface IOverseerClientService
    {
        Task StartAsync();
        Task StopAsync();
    }
}