using Microsoft.Extensions.DependencyInjection;

namespace BacklashBot.Services.Interfaces
{
/// <summary>IScopeManagerService</summary>
/// <summary>IScopeManagerService</summary>
    public interface IScopeManagerService : IDisposable
/// <summary>ResetAll</summary>
/// <summary>InitializeScope</summary>
    {
/// <summary>Gets or sets the Scope.</summary>
        void InitializeScope();
        void ResetAll();
        IServiceScope Scope { get; }
        IServiceScope CreateScope();

    }
}
