using Microsoft.Extensions.DependencyInjection;

namespace BacklashBot.Services.Interfaces
{
    public interface IScopeManagerService : IDisposable
    {
        void InitializeScope();
        void ResetAll();
        IServiceScope Scope { get; }
        IServiceScope CreateScope();

    }
}
