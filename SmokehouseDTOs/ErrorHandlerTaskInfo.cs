using Microsoft.Extensions.Logging;

namespace SmokehouseDTOs
{
    public class ErrorHandlerTaskInfo
    {
        public string FormattedMessage { get; set; }
        public string LogSourceCategory { get; set; }
        public LogLevel Severity { get; set; }
        public Exception OriginalException { get; set; }
        public DateTime Timestamp { get; set; }
    }
}