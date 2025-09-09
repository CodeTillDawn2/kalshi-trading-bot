namespace BacklashBot.State.Interfaces
{
    public interface IBotReadyStatus
    {
        TaskCompletionSource<bool> InitializationCompleted { get; set; }
        TaskCompletionSource<bool> BrowserReady { get; set; }
        void ResetAll();
    }
}
