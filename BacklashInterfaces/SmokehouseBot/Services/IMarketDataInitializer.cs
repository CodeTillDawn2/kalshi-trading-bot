
namespace BacklashBot.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that initializes market data,
    /// setting up necessary data structures and connections for market data operations.
    /// </summary>
    public interface IMarketDataInitializer
    {
        /// <summary>
        /// Performs asynchronous setup operations to initialize market data services,
        /// including establishing connections, loading initial data, and preparing for market data processing.
        /// </summary>
        /// <returns>A task representing the asynchronous setup operation.</returns>
        Task SetupAsync();
    }
}
