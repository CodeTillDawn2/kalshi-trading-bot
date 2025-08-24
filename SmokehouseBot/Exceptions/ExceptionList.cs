

namespace SmokehouseBot.Exceptions
{

    public interface ISmokehouseBotException
    {
        //CancellationToken LogCancellationToken { get; }
    }
    public class OrderbookTransientFailureException : Exception, ISmokehouseBotException
    {
        public string MarketId { get; }

        public OrderbookTransientFailureException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    public class CandlestickTransientFailureException : Exception, ISmokehouseBotException
    {
        public string MarketId { get; }

        public CandlestickTransientFailureException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    public class MarketTransientFailureException : Exception, ISmokehouseBotException
    {
        public string MarketId { get; }

        public MarketTransientFailureException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    public class MarketInvalidException : Exception, ISmokehouseBotException
    {
        public string MarketId { get; }

        public MarketInvalidException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    public class SnapshotInvalidException : Exception, ISmokehouseBotException
    {
        public string MarketId { get; }

        public SnapshotInvalidException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    public class ConnectionDisruptionException : Exception, ISmokehouseBotException
    {

        public ConnectionDisruptionException(string message, Exception? innerException = null)
            : base(message, innerException)
        {

        }
    }


    public class NotInCacheException : Exception, ISmokehouseBotException
    {
        public string MarketId { get; }

        public NotInCacheException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }


    public class KalshiKeyFileNotFoundException : Exception, ISmokehouseBotException
    {
        public KalshiKeyFileNotFoundException(string message, Exception? innerException = null)
            : base(message, innerException)
        {

        }
    }

    public class CandlestickFetchException : Exception, ISmokehouseBotException
    {
        public string MarketId { get; }

        public CandlestickFetchException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    public class TradeMissedException : Exception, ISmokehouseBotException
    {
        public string MarketId { get; }

        public TradeMissedException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    public class DeadLockException : Exception, ISmokehouseBotException
    {
        public string MarketId { get; }

        public DeadLockException(string marketId, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            MarketId = marketId;
        }
    }

    public class ProcessingThresholdExceededException : Exception, ISmokehouseBotException
    {
        public ProcessingThresholdExceededException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }

    public class KnownDuplicateInsertException : Exception, ISmokehouseBotException
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

    public class MarketInterestScoreDeadlockException : Exception, ISmokehouseBotException
    {
        public MarketInterestScoreDeadlockException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }

    public class WebSocketRetryFailedException : Exception, ISmokehouseBotException
    {
        public WebSocketRetryFailedException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }

    // This exception can be used by SmokehouseErrorHandler if it needs to log 
    // an error that doesn't fit other categories or when an original exception is unexpectedly null.
    public class UnhandledSmokehouseException : Exception, ISmokehouseBotException
    {
        public UnhandledSmokehouseException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}