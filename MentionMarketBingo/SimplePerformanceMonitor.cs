using BacklashInterfaces.PerformanceMetrics;
using Microsoft.Extensions.Logging;

namespace MentionMarketBingo
{
    /// <summary>
    /// Simple implementation of IPerformanceMonitor for the MentionMarketBingo project.
    /// Inherits from BasePerformanceMonitor to provide basic performance monitoring functionality.
    /// </summary>
    public class SimplePerformanceMonitor : BasePerformanceMonitor
    {
        /// <summary>
        /// Initializes a new instance of the SimplePerformanceMonitor class.
        /// </summary>
        /// <param name="logger">Optional logger for recording operations.</param>
        public SimplePerformanceMonitor(ILogger<SimplePerformanceMonitor>? logger = null)
            : base(logger)
        {
        }
    }
}