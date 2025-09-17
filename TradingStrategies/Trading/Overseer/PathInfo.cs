using System.Collections.Generic;

namespace TradingStrategies.Trading.Overseer
{
    /// <summary>
    /// Contains information about the strategies used in a specific trading path.
    /// This class provides a simple container for listing the strategy names that were
    /// active during a particular market condition or trading scenario.
    /// </summary>
    public class PathInfo
    {
        /// <summary>
        /// List of strategy names that were applied in this path.
        /// </summary>
        public List<string> Strats { get; set; } = new List<string>();
    }
}