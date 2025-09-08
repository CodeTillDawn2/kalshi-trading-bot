using SmokehouseBot.State.Interfaces;

namespace KalshiBotOverseer.State
{
    public class OverseerReadyStatus : IBotReadyStatus
    {
        public TaskCompletionSource<bool> InitializationCompleted { get; set; } = new TaskCompletionSource<bool>();
        public TaskCompletionSource<bool> BrowserReady { get; set; } = new TaskCompletionSource<bool>();

        public OverseerReadyStatus()
        {
            ResetAll();
        }

        public void ResetAll()
        {

            InitializationCompleted = new TaskCompletionSource<bool>();
            InitializationCompleted.SetResult(false);
        }
    }
}