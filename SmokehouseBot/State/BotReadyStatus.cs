using SmokehouseBot.State.Interfaces;

namespace SmokehouseBot.State
{
    public class BotReadyStatus : IBotReadyStatus
    {
        public TaskCompletionSource<bool> InitializationCompleted { get; set; } = new TaskCompletionSource<bool>();
        public TaskCompletionSource<bool> BrowserReady { get; set; } = new TaskCompletionSource<bool>();

        public BotReadyStatus()
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