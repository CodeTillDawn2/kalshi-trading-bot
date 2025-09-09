

namespace BacklashDTOs.Exceptions
{

    public interface IBacklashBotException
    {
        //CancellationToken LogCancellationToken { get; }
    }
    public class OrderbookTransientFailureException : Exception, IBacklashBotException
    {
        public string MarketId { get; }

        public OrderbookTransientFailureException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    public class CandlestickTransientFailureException : Exception, IBacklashBotException
    {
        public string MarketId { get; }

        public CandlestickTransientFailureException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    public class MarketTransientFailureException : Exception, IBacklashBotException
    {
        public string MarketId { get; }

        public MarketTransientFailureException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    public class MarketInvalidException : Exception, IBacklashBotException
    {
        public string MarketId { get; }

        public MarketInvalidException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    public class SnapshotInvalidException : Exception, IBacklashBotException
    {
        public string MarketId { get; }

        public SnapshotInvalidException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    public class ConnectionDisruptionException : Exception, IBacklashBotException
    {

        public ConnectionDisruptionException(string message, Exception? innerException = null)
            : base(message, innerException)
        {

        }
    }


    public class NotInCacheException : Exception, IBacklashBotException
    {
        public string MarketId { get; }

        public NotInCacheException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }


    public class KalshiKeyFileNotFoundException : Exception, IBacklashBotException
    {
        public KalshiKeyFileNotFoundException(string message, Exception? innerException = null)
            : base(message, innerException)
        {

        }
    }

    public class CandlestickFetchException : Exception, IBacklashBotException
    {
        public string MarketId { get; }

        public CandlestickFetchException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    public class TradeMissedException : Exception, IBacklashBotException
    {
        public string MarketId { get; }

        public TradeMissedException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    public class DeadLockException : Exception, IBacklashBotException
    {
        public string MarketId { get; }

        public DeadLockException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    public class ProcessingThresholdExceededException : Exception, IBacklashBotException
    {
        public ProcessingThresholdExceededException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }

    public class KnownDuplicateInsertException : Exception, IBacklashBotException
    {
        public string EntityType { get; }
        public string DuplicateKeyInfo { get; }

        public KnownDuplicateInsertException(string entityType, string duplicateKeyInfo, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            EntityType = entityType;
            DuplicateKeyInfo = duplicateKeyInfo;
        }
    }

    public class MarketInterestScoreDeadlockException : Exception, IBacklashBotException
    {
        public MarketInterestScoreDeadlockException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }

    public class WebSocketRetryFailedException : Exception, IBacklashBotException
    {
        public WebSocketRetryFailedException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }

    // This exception can be used by SmokehouseErrorHandler if it needs to log 
    // an error that doesn't fit other categories or when an original exception is unexpectedly null.
    public class UnhandledSmokehouseException : Exception, IBacklashBotException
    {
        public UnhandledSmokehouseException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
