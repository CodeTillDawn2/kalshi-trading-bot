using SmokehouseBot.State.Interfaces;

namespace KalshiBotOverseer.State
{
    public class OverseerStatusTracker : IStatusTrackerService
    {
        private CancellationTokenSource _globalCancellationTokenSource;
        private readonly object _lock = new();

        public OverseerStatusTracker()
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