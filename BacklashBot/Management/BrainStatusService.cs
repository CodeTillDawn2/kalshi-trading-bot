using BacklashBotData.Data.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using BacklashDTOs.Configuration;
using BacklashBot.Management.Interfaces;
using BacklashDTOs.Data;
using System.Security.Cryptography;

namespace BacklashBot.Management
{
    /// <summary>
    /// Service responsible for managing the initialization and status of a brain instance,
    /// providing access to the brain lock and session identifier in a thread-safe manner.
    /// </summary>
    public class BrainStatusService : IBrainStatusService
    {
        private Guid _brainLock;
        private string? _sessionIdentifier;
        private bool _initialized;
        private Task? _initTask;
        private readonly object _lock = new();
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ExecutionConfig _executionConfig;
        private readonly ILogger<BrainStatusService> _logger;
        private TimeSpan _initializationTime = TimeSpan.Zero;
        private int _initializationAttempts = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrainStatusService"/> class.
        /// </summary>
        /// <param name="scopeFactory">Factory for creating service scopes to access database context.</param>
        /// <param name="executionConfig">Configuration options for execution settings.</param>
        /// <param name="logger">Logger for recording service operations and errors.</param>
        public BrainStatusService(IServiceScopeFactory scopeFactory, IOptions<ExecutionConfig> executionConfig, ILogger<BrainStatusService> logger)
        {
            ArgumentNullException.ThrowIfNull(scopeFactory);
            ArgumentNullException.ThrowIfNull(executionConfig);
            ArgumentNullException.ThrowIfNull(logger);

            _scopeFactory = scopeFactory;
            _executionConfig = executionConfig.Value ?? throw new ArgumentNullException(nameof(executionConfig.Value));

            if (string.IsNullOrWhiteSpace(_executionConfig.BrainInstance))
            {
                throw new ArgumentException("BrainInstance must be specified in ExecutionConfig.", nameof(_executionConfig.BrainInstance));
            }

            _logger = logger;
        }

        /// <summary>
        /// Gets the unique brain lock GUID for this brain instance.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the service has not been initialized.</exception>
        public Guid BrainLock
        {
            get
            {
                if (!_initialized)
                {
                    throw new InvalidOperationException("Call EnsureInitializedAsync() before accessing properties.");
                }
                return _brainLock;
            }
        }

        /// <summary>
        /// Gets the session identifier string for this brain instance.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the service has not been initialized.</exception>
        public string SessionIdentifier
        {
            get
            {
                if (!_initialized)
                {
                    throw new InvalidOperationException("Call EnsureInitializedAsync() before accessing properties.");
                }
                return _sessionIdentifier!;
            }
        }

        /// <summary>
        /// Ensures the brain status service is initialized asynchronously.
        /// Uses double-checked locking to prevent multiple initialization attempts.
        /// </summary>
        /// <returns>A task representing the initialization operation.</returns>
        public Task EnsureInitializedAsync()
        {
            if (_initTask == null)
            {
                lock (_lock)
                {
                    if (_initTask == null)
                    {
                        _initTask = PopulateFieldsAsync();
                    }
                }
            }
            return _initTask;
        }

        /// <summary>
        /// Populates the brain lock and session identifier fields by retrieving the brain instance from the database.
        /// </summary>
        /// <returns>A task representing the field population operation.</returns>
        /// <exception cref="Exception">Thrown when the brain instance is not found in the database.</exception>
        private async Task PopulateFieldsAsync()
        {
            var startTime = DateTime.UtcNow;
            Interlocked.Increment(ref _initializationAttempts);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
                BrainInstanceDTO? brainInstance = await context.GetBrainInstance(_executionConfig.BrainInstance);
                if (brainInstance == null)
                {
                    _logger.LogError("Brain instance with ID {BrainInstance} not found.", _executionConfig.BrainInstance);
                    throw new Exception($"Brain instance with ID {_executionConfig.BrainInstance} not found.");
                }
                _brainLock = brainInstance.BrainLock ?? Guid.NewGuid();
                _sessionIdentifier = GenerateRandomString(_executionConfig.SessionIdLength);
                _initialized = true;
                _initializationTime = DateTime.UtcNow - startTime;
                _logger.LogInformation("Brain status service initialized successfully for brain instance {BrainInstance}.", _executionConfig.BrainInstance);
            }
            catch (Exception ex)
            {
                _initializationTime = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "Failed to initialize brain status service for brain instance {BrainInstance}.", _executionConfig.BrainInstance);
                throw; // Rethrow to surface in task; handle as needed in consumers.
            }
        }

        /// <summary>
        /// Generates a random alphanumeric string of the specified length with additional entropy.
        /// </summary>
        /// <param name="length">The length of the string to generate.</param>
        /// <returns>A random alphanumeric string.</returns>
        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            // Use more bytes for better entropy
            var data = new byte[length + 8]; // Extra 8 bytes for timestamp
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(data);
            }
            // Incorporate timestamp for additional entropy
            var timestamp = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
            for (int i = 0; i < Math.Min(8, data.Length); i++)
            {
                data[i] ^= timestamp[i];
            }
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[data[i] % chars.Length];
            }
            return new string(result);
        }

        /// <summary>
        /// Gets current performance metrics for the brain status service.
        /// Returns initialization timing and attempt information.
        /// </summary>
        /// <returns>A tuple containing initialization time in milliseconds and number of initialization attempts.</returns>
        public (double InitializationTimeMs, int InitializationAttempts) GetPerformanceMetrics()
        {
            return (_initializationTime.TotalMilliseconds, _initializationAttempts);
        }

        /// <summary>
        /// Disposes of the brain status service resources.
        /// Currently no unmanaged resources are held, so this is a no-op.
        /// </summary>
        public void Dispose()
        {
            // No unmanaged resources to dispose
        }
    }
}
