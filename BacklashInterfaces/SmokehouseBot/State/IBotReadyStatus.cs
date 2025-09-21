namespace BacklashBot.State.Interfaces
{
    /// <summary>IBotReadyStatus</summary>
    /// <summary>IBotReadyStatus</summary>
    public interface IBotReadyStatus
    /// <summary>Gets or sets the BrowserReady.</summary>
    /// <summary>Gets or sets the InitializationCompleted.</summary>
    {
        /// <summary>ResetAll</summary>
        TaskCompletionSource<bool> InitializationCompleted { get; set; }
        TaskCompletionSource<bool> BrowserReady { get; set; }
        void ResetAll();
    }
}
