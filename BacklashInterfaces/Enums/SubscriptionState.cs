namespace BacklashInterfaces.Enums
{
    /// <summary>
    /// Represents the current state of a subscription to a specific market data channel via WebSocket.
    /// This enum is used by the SubscriptionManager to track and manage the lifecycle of subscriptions,
    /// ensuring proper handling of connection states and preventing duplicate or invalid operations.
    /// </summary>
    public enum SubscriptionState
    {
        /// <summary>
        /// The subscription is not active. This is the initial state before any subscription attempt,
        /// or after a successful unsubscription. In this state, no data is being received for the channel.
        /// </summary>
        Unsubscribed,

        /// <summary>
        /// A subscription request has been sent to the server, but confirmation has not yet been received.
        /// This transitional state prevents multiple concurrent subscription attempts for the same channel.
        /// </summary>
        Subscribing,

        /// <summary>
        /// The subscription is active and confirmed. Data for the channel is being received and processed.
        /// This state indicates a successful connection and ongoing data flow.
        /// </summary>
        Subscribed,

        /// <summary>
        /// An unsubscription request has been sent to the server, but confirmation has not yet been received.
        /// This transitional state ensures clean disconnection and prevents new subscription attempts during teardown.
        /// </summary>
        Unsubscribing
    }
}
