using BacklashBot.State.Interfaces;

namespace BacklashBot.State
{
    public class KalshiBotReadyStatus : IBotReadyStatus
    {
        public TaskCompletionSource<bool> InitializationCompleted { get; set; } = new TaskCompletionSource<bool>();
        public TaskCompletionSource<bool> BrowserReady { get; set; } = new TaskCompletionSource<bool>();

        public KalshiBotReadyStatus()
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
