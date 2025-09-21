using System.ComponentModel.DataAnnotations;

namespace TradingGUI.Configuration
{
    /// <summary>
    /// Configuration class for SnapshotViewer interaction parameters loaded from appsettings.json.
    /// Contains settings for chart navigation, zooming, panning, and user interaction behavior.
    /// </summary>
    public class SnapshotViewerConfig
    {
        /// <summary>
        /// The configuration section name for SnapshotViewerConfig.
        /// </summary>
        public const string SectionName = "GUI:SnapshotViewer";

        /// <summary>
        /// Factor by which to zoom in when scrolling up (values < 1.0 zoom in).
        /// Smaller values result in faster zooming.
        /// </summary>
        [Required(ErrorMessage = "The 'ZoomInFactor' is missing in the configuration.")]
        public double ZoomInFactor { get; set; }

        /// <summary>
        /// Factor by which to zoom out when scrolling down (values > 1.0 zoom out).
        /// Larger values result in faster zooming out.
        /// </summary>
        [Required(ErrorMessage = "The 'ZoomOutFactor' is missing in the configuration.")]
        public double ZoomOutFactor { get; set; }

        /// <summary>
        /// Minimum pixel movement threshold for panning operations.
        /// Helps prevent accidental panning from small mouse movements.
        /// </summary>
        [Required(ErrorMessage = "The 'PanThreshold' is missing in the configuration.")]
        public double PanThreshold { get; set; }

        /// <summary>
        /// Interval in milliseconds for the navigation timer that defers expensive chart operations.
        /// Allows immediate UI updates while batching complex rendering.
        /// </summary>
        [Required(ErrorMessage = "The 'NavigationTimerInterval' is missing in the configuration.")]
        public int NavigationTimerInterval { get; set; }

        /// <summary>
        /// Delay in milliseconds after which navigation speed resets to normal.
        /// Prevents speed acceleration from persisting too long after user stops navigating.
        /// </summary>
        [Required(ErrorMessage = "The 'NavigationResetDelay' is missing in the configuration.")]
        public int NavigationResetDelay { get; set; }

        /// <summary>
        /// Number of consecutive navigations required to trigger rapid speed mode.
        /// Higher values make it harder to accidentally enter rapid navigation.
        /// </summary>
        [Required(ErrorMessage = "The 'RapidNavigationThreshold' is missing in the configuration.")]
        public int RapidNavigationThreshold { get; set; }

        /// <summary>
        /// Number of consecutive navigations required to trigger fast speed mode.
        /// Medium threshold for progressive speed increase.
        /// </summary>
        [Required(ErrorMessage = "The 'FastNavigationThreshold' is missing in the configuration.")]
        public int FastNavigationThreshold { get; set; }

        /// <summary>
        /// Number of consecutive navigations required to trigger medium speed mode.
        /// Initial threshold for speed acceleration.
        /// </summary>
        [Required(ErrorMessage = "The 'MediumNavigationThreshold' is missing in the configuration.")]
        public int MediumNavigationThreshold { get; set; }

        /// <summary>
        /// Number of snapshots to skip per navigation action in rapid mode.
        /// Large step size for efficient browsing of large datasets.
        /// </summary>
        [Required(ErrorMessage = "The 'RapidStepSize' is missing in the configuration.")]
        public int RapidStepSize { get; set; }

        /// <summary>
        /// Number of snapshots to skip per navigation action in fast mode.
        /// Medium step size for balanced performance.
        /// </summary>
        [Required(ErrorMessage = "The 'FastStepSize' is missing in the configuration.")]
        public int FastStepSize { get; set; }

        /// <summary>
        /// Number of snapshots to skip per navigation action in medium mode.
        /// Small step size for moderate acceleration.
        /// </summary>
        [Required(ErrorMessage = "The 'MediumStepSize' is missing in the configuration.")]
        public int MediumStepSize { get; set; }

        /// <summary>
        /// Number of snapshots to skip per navigation action in normal mode.
        /// Default step size for precise navigation.
        /// </summary>
        [Required(ErrorMessage = "The 'NormalStepSize' is missing in the configuration.")]
        public int NormalStepSize { get; set; }
    }
}
