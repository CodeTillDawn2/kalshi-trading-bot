using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingGUI.Configuration
{
    /// <summary>
    /// Configuration class for SnapshotViewer interaction parameters loaded from appsettings.json.
    /// Contains settings for chart navigation, zooming, panning, and user interaction behavior.
    /// </summary>
    public class SnapshotViewerConfig
    {
        /// <summary>
        /// Factor by which to zoom in when scrolling up (values < 1.0 zoom in).
        /// Smaller values result in faster zooming.
        /// </summary>
        required
        public double ZoomInFactor { get; set; }

        /// <summary>
        /// Factor by which to zoom out when scrolling down (values > 1.0 zoom out).
        /// Larger values result in faster zooming out.
        /// </summary>
        required
        public double ZoomOutFactor { get; set; }

        /// <summary>
        /// Minimum pixel movement threshold for panning operations.
        /// Helps prevent accidental panning from small mouse movements.
        /// </summary>
        required
        public double PanThreshold { get; set; }

        /// <summary>
        /// Interval in milliseconds for the navigation timer that defers expensive chart operations.
        /// Allows immediate UI updates while batching complex rendering.
        /// </summary>
        required
        public int NavigationTimerInterval { get; set; }

        /// <summary>
        /// Delay in milliseconds after which navigation speed resets to normal.
        /// Prevents speed acceleration from persisting too long after user stops navigating.
        /// </summary>
        required
        public int NavigationResetDelay { get; set; }

        /// <summary>
        /// Number of consecutive navigations required to trigger rapid speed mode.
        /// Higher values make it harder to accidentally enter rapid navigation.
        /// </summary>
        required
        public int RapidNavigationThreshold { get; set; }

        /// <summary>
        /// Number of consecutive navigations required to trigger fast speed mode.
        /// Medium threshold for progressive speed increase.
        /// </summary>
        required
        public int FastNavigationThreshold { get; set; }

        /// <summary>
        /// Number of consecutive navigations required to trigger medium speed mode.
        /// Initial threshold for speed acceleration.
        /// </summary>
        required
        public int MediumNavigationThreshold { get; set; }

        /// <summary>
        /// Number of snapshots to skip per navigation action in rapid mode.
        /// Large step size for efficient browsing of large datasets.
        /// </summary>
        required
        public int RapidStepSize { get; set; }

        /// <summary>
        /// Number of snapshots to skip per navigation action in fast mode.
        /// Medium step size for balanced performance.
        /// </summary>
        required
        public int FastStepSize { get; set; }

        /// <summary>
        /// Number of snapshots to skip per navigation action in medium mode.
        /// Small step size for moderate acceleration.
        /// </summary>
        required
        public int MediumStepSize { get; set; }

        /// <summary>
        /// Number of snapshots to skip per navigation action in normal mode.
        /// Default step size for precise navigation.
        /// </summary>
        required
        public int NormalStepSize { get; set; }
    }
}
