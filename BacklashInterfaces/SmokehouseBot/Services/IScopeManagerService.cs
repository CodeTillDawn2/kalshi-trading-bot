using Microsoft.Extensions.DependencyInjection;

namespace BacklashBot.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that manages dependency injection scopes
    /// and provides access to scoped services within the application.
    /// </summary>
    public interface IScopeManagerService : IDisposable
    {
        /// <summary>
        /// Initializes the service scope for dependency injection.
        /// </summary>
        void InitializeScope();

        /// <summary>
        /// Resets all managed scopes and services.
        /// </summary>
        void ResetAll();

        /// <summary>
        /// Gets the current service scope.
        /// </summary>
        IServiceScope Scope { get; }

        /// <summary>
        /// Creates a new service scope for dependency injection.
        /// </summary>
        /// <returns>The newly created service scope.</returns>
        IServiceScope CreateScope();
    }
}
