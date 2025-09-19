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
        public double ZoomInFactor { get; set; } = 0.9;

        /// <summary>
        /// Factor by which to zoom out when scrolling down (values > 1.0 zoom out).
        /// Larger values result in faster zooming out.
        /// </summary>
        public double ZoomOutFactor { get; set; } = 1.1;

        /// <summary>
        /// Minimum pixel movement threshold for panning operations.
        /// Helps prevent accidental panning from small mouse movements.
        /// </summary>
        public double PanThreshold { get; set; } = 0.001;

        /// <summary>
        /// Interval in milliseconds for the navigation timer that defers expensive chart operations.
        /// Allows immediate UI updates while batching complex rendering.
        /// </summary>
        public int NavigationTimerInterval { get; set; } = 300;

        /// <summary>
        /// Delay in milliseconds after which navigation speed resets to normal.
        /// Prevents speed acceleration from persisting too long after user stops navigating.
        /// </summary>
        public int NavigationResetDelay { get; set; } = 500;

        /// <summary>
        /// Number of consecutive navigations required to trigger rapid speed mode.
        /// Higher values make it harder to accidentally enter rapid navigation.
        /// </summary>
        public int RapidNavigationThreshold { get; set; } = 60;

        /// <summary>
        /// Number of consecutive navigations required to trigger fast speed mode.
        /// Medium threshold for progressive speed increase.
        /// </summary>
        public int FastNavigationThreshold { get; set; } = 15;

        /// <summary>
        /// Number of consecutive navigations required to trigger medium speed mode.
        /// Initial threshold for speed acceleration.
        /// </summary>
        public int MediumNavigationThreshold { get; set; } = 5;

        /// <summary>
        /// Number of snapshots to skip per navigation action in rapid mode.
        /// Large step size for efficient browsing of large datasets.
        /// </summary>
        public int RapidStepSize { get; set; } = 60;

        /// <summary>
        /// Number of snapshots to skip per navigation action in fast mode.
        /// Medium step size for balanced performance.
        /// </summary>
        public int FastStepSize { get; set; } = 5;

        /// <summary>
        /// Number of snapshots to skip per navigation action in medium mode.
        /// Small step size for moderate acceleration.
        /// </summary>
        public int MediumStepSize { get; set; } = 2;

        /// <summary>
        /// Number of snapshots to skip per navigation action in normal mode.
        /// Default step size for precise navigation.
        /// </summary>
        public int NormalStepSize { get; set; } = 1;
    }
}