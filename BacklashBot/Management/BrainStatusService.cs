using BacklashBot.Configuration;
using BacklashBot.Management.Interfaces;
using BacklashBotData.Data.Interfaces;
using BacklashCommon.Configuration;
using BacklashDTOs.Data;
using BacklashInterfaces.PerformanceMetrics;
using Microsoft.Extensions.Options;
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
        private readonly InstanceNameConfig instanceNameConfig;
        private readonly BrainStatusServiceConfig _brainStatusConfig;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly ILogger<BrainStatusService> _logger;
        private TimeSpan _initializationTime = TimeSpan.Zero;
        private int _initializationAttempts = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrainStatusService"/> class.
        /// </summary>
        /// <param name="scopeFactory">Factory for creating service scopes to access database context.</param>
        /// <param name="instanceNameConfig">Configuration options for general execution settings.</param>
        /// <param name="brainStatusConfig">Configuration options for brain status settings.</param>
        /// <param name="performanceMonitor">Monitor for recording performance metrics.</param>
        /// <param name="sessionIdentifier">The pre-generated session identifier.</param>
        /// <param name="logger">Logger for recording service operations and errors.</param>
        public BrainStatusService(IServiceScopeFactory scopeFactory, IOptions<InstanceNameConfig> instanceNameConfig, IOptions<BrainStatusServiceConfig> brainStatusConfig, IPerformanceMonitor performanceMonitor, string sessionIdentifier, ILogger<BrainStatusService> logger)
        {
            ArgumentNullException.ThrowIfNull(scopeFactory);
            ArgumentNullException.ThrowIfNull(instanceNameConfig);
            ArgumentNullException.ThrowIfNull(brainStatusConfig);
            ArgumentNullException.ThrowIfNull(performanceMonitor);
            ArgumentNullException.ThrowIfNull(sessionIdentifier);
            ArgumentNullException.ThrowIfNull(logger);

            _scopeFactory = scopeFactory;
            this.instanceNameConfig = instanceNameConfig.Value ?? throw new ArgumentNullException(nameof(instanceNameConfig.Value));
            _brainStatusConfig = brainStatusConfig.Value ?? throw new ArgumentNullException(nameof(brainStatusConfig.Value));
            _performanceMonitor = performanceMonitor;
            _sessionIdentifier = sessionIdentifier;

            if (string.IsNullOrWhiteSpace(this.instanceNameConfig.Name))
            {
                throw new ArgumentException("BrainInstance must be specified in GeneralExecutionConfig.", nameof(BrainStatusService.instanceNameConfig.Name));
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
                BrainInstanceDTO? brainInstance = await context.GetBrainInstance(instanceNameConfig.Name);
                if (brainInstance == null)
                {
                    _logger.LogError("Brain instance with ID {BrainInstance} not found.", instanceNameConfig.Name);
                    throw new Exception($"Brain instance with ID {instanceNameConfig.Name} not found.");
                }
                _brainLock = brainInstance.BrainLock ?? Guid.NewGuid();
                // _sessionIdentifier is already set in constructor
                _initialized = true;
                _initializationTime = DateTime.UtcNow - startTime;
                if (_brainStatusConfig.EnablePerformanceMetrics)
                {
                    _performanceMonitor.RecordNumericDisplayMetric(
                        className: nameof(BrainStatusService),
                        id: "InitializationTime",
                        name: "Initialization Time",
                        description: "Time taken to initialize the brain status service",
                        value: _initializationTime.TotalMilliseconds,
                        unit: "ms",
                        category: "Performance"
                    );
                }
                _logger.LogInformation("Brain status service initialized successfully for brain instance {BrainInstance}.", instanceNameConfig.Name);
            }
            catch (Exception ex)
            {
                _initializationTime = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "Failed to initialize brain status service for brain instance {BrainInstance}.", instanceNameConfig.Name);
                throw; // Rethrow to surface in task; handle as needed in consumers.
            }
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
