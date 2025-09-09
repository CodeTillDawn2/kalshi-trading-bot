using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.Options;
using BacklashBot.Configuration;
using BacklashBot.Management.Interfaces;
using BacklashDTOs.Data;

namespace BacklashBot.Management
{
    public class BrainStatusService : IBrainStatusService
    {
        private Guid _brainLock;
        private string _sessionIdentifier;
        private bool _initialized;
        private Task _initTask;
        private readonly object _lock = new();
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ExecutionConfig _executionConfig;

        public BrainStatusService(IServiceScopeFactory scopeFactory, IOptions<ExecutionConfig> executionConfig)
        {
            _scopeFactory = scopeFactory;
            _executionConfig = executionConfig.Value;
        }

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

        public string SessionIdentifier
        {
            get
            {
                if (!_initialized)
                {
                    throw new InvalidOperationException("Call EnsureInitializedAsync() before accessing properties.");
                }
                return _sessionIdentifier;
            }
        }

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

        private async Task PopulateFieldsAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
                BrainInstanceDTO? brainInstance = await context.GetBrainInstance(_executionConfig.BrainInstance);
                if (brainInstance == null)
                {
                    throw new Exception($"Brain instance with ID {_executionConfig.BrainInstance} not found.");
                }
                _brainLock = brainInstance.BrainLock ?? Guid.NewGuid();
                _sessionIdentifier = GenerateRandomString(5);
                _initialized = true;
            }
            catch (Exception ex)
            {
                throw; // Rethrow to surface in task; handle as needed in consumers.
            }
        }

        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public void Dispose()
        {

        }
    }
}
