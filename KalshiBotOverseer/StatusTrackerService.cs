

using SmokehouseBot.State.Interfaces;

namespace KalshiBotOverseer
{
    public class StatusTrackerService : IStatusTrackerService
    {
        public TaskCompletionSource<bool> InitializationCompleted { get; set; } = new TaskCompletionSource<bool>();
        public TaskCompletionSource<bool> BrowserReady { get; set; } = new TaskCompletionSource<bool>();
        private CancellationTokenSource _globalCancellationTokenSource;
        private readonly object _lock = new();

        public StatusTrackerService()
        {
            ResetAll();
        }

        public CancellationToken GetCancellationToken()
        {
            lock (_lock)
            {
                return _globalCancellationTokenSource.Token;
            }
        }

        public void CancelAll()
        {
            lock (_lock)
            {
                _globalCancellationTokenSource.Cancel();
            }
        }

        public void ResetAll()
        {

            InitializationCompleted = new TaskCompletionSource<bool>();
            InitializationCompleted.SetResult(false);
            lock (_lock)
            {
                if (_globalCancellationTokenSource != null)
                {
                    _globalCancellationTokenSource.Dispose();
                }
                _globalCancellationTokenSource = new CancellationTokenSource();
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                CancelAll();
                _globalCancellationTokenSource?.Dispose();
            }
        }

    }
}