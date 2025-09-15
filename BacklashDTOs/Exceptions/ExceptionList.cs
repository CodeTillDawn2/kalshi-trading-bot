

namespace BacklashDTOs.Exceptions
{

    /// <summary>
    /// Interface for BacklashBot exceptions.
    /// </summary>
    public interface IBacklashBotException
    {
        //CancellationToken LogCancellationToken { get; }
    }
    /// <summary>
    /// Exception thrown when there is a transient failure in the orderbook.
    /// </summary>
    public class OrderbookTransientFailureException : Exception, IBacklashBotException
    {
        /// <summary>
        /// Gets the market ID associated with the exception.
        /// </summary>
        public string MarketId { get; }

        /// <summary>
        /// Initializes a new instance of the OrderbookTransientFailureException class.
        /// </summary>
        /// <param name="marketId">The market ID.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public OrderbookTransientFailureException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    /// <summary>
    /// Exception thrown when there is a transient failure in candlestick data.
    /// </summary>
    public class CandlestickTransientFailureException : Exception, IBacklashBotException
    {
        /// <summary>
        /// Gets the market ID associated with the exception.
        /// </summary>
        public string MarketId { get; }

        /// <summary>
        /// Initializes a new instance of the CandlestickTransientFailureException class.
        /// </summary>
        /// <param name="marketId">The market ID.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public CandlestickTransientFailureException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    /// <summary>
    /// Exception thrown when there is a transient failure in market data.
    /// </summary>
    public class MarketTransientFailureException : Exception, IBacklashBotException
    {
        /// <summary>
        /// Gets the market ID associated with the exception.
        /// </summary>
        public string MarketId { get; }

        /// <summary>
        /// Initializes a new instance of the MarketTransientFailureException class.
        /// </summary>
        /// <param name="marketId">The market ID.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public MarketTransientFailureException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    /// <summary>
    /// Exception thrown when market data is invalid.
    /// </summary>
    public class MarketInvalidException : Exception, IBacklashBotException
    {
        /// <summary>
        /// Gets the market ID associated with the exception.
        /// </summary>
        public string MarketId { get; }

        /// <summary>
        /// Initializes a new instance of the MarketInvalidException class.
        /// </summary>
        /// <param name="marketId">The market ID.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public MarketInvalidException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    /// <summary>
    /// Exception thrown when snapshot data is invalid.
    /// </summary>
    public class SnapshotInvalidException : Exception, IBacklashBotException
    {
        /// <summary>
        /// Gets the market ID associated with the exception.
        /// </summary>
        public string MarketId { get; }

        /// <summary>
        /// Initializes a new instance of the SnapshotInvalidException class.
        /// </summary>
        /// <param name="marketId">The market ID.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public SnapshotInvalidException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    /// <summary>
    /// Exception thrown when there is a connection disruption.
    /// </summary>
    public class ConnectionDisruptionException : Exception, IBacklashBotException
    {

        /// <summary>
        /// Initializes a new instance of the ConnectionDisruptionException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ConnectionDisruptionException(string message, Exception? innerException = null)
            : base(message, innerException)
        {

        }
    }


    /// <summary>
    /// Exception thrown when data is not found in cache.
    /// </summary>
    public class NotInCacheException : Exception, IBacklashBotException
    {
        /// <summary>
        /// Gets the market ID associated with the exception.
        /// </summary>
        public string MarketId { get; }

        /// <summary>
        /// Initializes a new instance of the NotInCacheException class.
        /// </summary>
        /// <param name="marketId">The market ID.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public NotInCacheException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }


    /// <summary>
    /// Exception thrown when the Kalshi key file is not found.
    /// </summary>
    public class KalshiKeyFileNotFoundException : Exception, IBacklashBotException
    {
        /// <summary>
        /// Initializes a new instance of the KalshiKeyFileNotFoundException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public KalshiKeyFileNotFoundException(string message, Exception? innerException = null)
            : base(message, innerException)
        {

        }
    }

    /// <summary>
    /// Exception thrown when there is an error fetching candlestick data.
    /// </summary>
    public class CandlestickFetchException : Exception, IBacklashBotException
    {
        /// <summary>
        /// Gets the market ID associated with the exception.
        /// </summary>
        public string MarketId { get; }

        /// <summary>
        /// Initializes a new instance of the CandlestickFetchException class.
        /// </summary>
        /// <param name="marketId">The market ID.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public CandlestickFetchException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    /// <summary>
    /// Exception thrown when a trade is missed.
    /// </summary>
    public class TradeMissedException : Exception, IBacklashBotException
    {
        /// <summary>
        /// Gets the market ID associated with the exception.
        /// </summary>
        public string MarketId { get; }

        /// <summary>
        /// Initializes a new instance of the TradeMissedException class.
        /// </summary>
        /// <param name="marketId">The market ID.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public TradeMissedException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    /// <summary>
    /// Exception thrown when a deadlock occurs.
    /// </summary>
    public class DeadLockException : Exception, IBacklashBotException
    {
        /// <summary>
        /// Gets the market ID associated with the exception.
        /// </summary>
        public string MarketId { get; }

        /// <summary>
        /// Initializes a new instance of the DeadLockException class.
        /// </summary>
        /// <param name="marketId">The market ID.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public DeadLockException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    /// <summary>
    /// Exception thrown when processing threshold is exceeded.
    /// </summary>
    public class ProcessingThresholdExceededException : Exception, IBacklashBotException
    {
        /// <summary>
        /// Initializes a new instance of the ProcessingThresholdExceededException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ProcessingThresholdExceededException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a known duplicate insert occurs.
    /// </summary>
    public class KnownDuplicateInsertException : Exception, IBacklashBotException
    {
        /// <summary>
        /// Gets the entity type.
        /// </summary>
        public string EntityType { get; }
        /// <summary>
        /// Gets the duplicate key information.
        /// </summary>
        public string DuplicateKeyInfo { get; }

        /// <summary>
        /// Initializes a new instance of the KnownDuplicateInsertException class.
        /// </summary>
        /// <param name="entityType">The entity type.</param>
        /// <param name="duplicateKeyInfo">The duplicate key information.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public KnownDuplicateInsertException(string entityType, string duplicateKeyInfo, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            EntityType = entityType;
            DuplicateKeyInfo = duplicateKeyInfo;
        }
    }

    /// <summary>
    /// Exception thrown when a deadlock occurs in market interest score.
    /// </summary>
    public class MarketInterestScoreDeadlockException : Exception, IBacklashBotException
    {
        /// <summary>
        /// Initializes a new instance of the MarketInterestScoreDeadlockException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public MarketInterestScoreDeadlockException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when WebSocket retry fails.
    /// </summary>
    public class WebSocketRetryFailedException : Exception, IBacklashBotException
    {
        /// <summary>
        /// Initializes a new instance of the WebSocketRetryFailedException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public WebSocketRetryFailedException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }

    // This exception can be used by SmokehouseErrorHandler if it needs to log
    // an error that doesn't fit other categories or when an original exception is unexpectedly null.
    /// <summary>
    /// Exception for unhandled Smokehouse errors.
    /// </summary>
    public class UnhandledSmokehouseException : Exception, IBacklashBotException
    {
        /// <summary>
        /// Initializes a new instance of the UnhandledSmokehouseException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public UnhandledSmokehouseException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
