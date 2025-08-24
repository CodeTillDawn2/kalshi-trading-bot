using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using SmokehouseDTOs;

namespace TradingSimulatorGUI
{
    /// <summary>
    /// Interaction logic for MarketSnapshotWindow.xaml
    /// </summary>
    public partial class MarketSnapshotWindow : Window, INotifyPropertyChanged
    {
        private MarketSnapshot _snapshot;

        public MarketSnapshotWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Gets or sets the current market snapshot bound to the view.  Changing this property
        /// raises the PropertyChanged notification so bindings update accordingly.
        /// </summary>
        public MarketSnapshot Snapshot
        {
            get => _snapshot;
            set
            {
                _snapshot = value;
                OnPropertyChanged(nameof(Snapshot));
            }
        }

        /// <summary>
        /// A collection of resting order view models used by the DataGrid.  The window maintains
        /// this list and repopulates it when a new snapshot is provided.
        /// </summary>
        public ObservableCollection<RestingOrderViewModel> RestingOrdersView { get; } = new ObservableCollection<RestingOrderViewModel>();

        /// <summary>
        /// Populate this view from a MarketSnapshot instance.  Assigns the Snapshot property and
        /// converts the resting order tuples into a list of view models.
        /// </summary>
        /// <param name="snapshot">The snapshot to display.</param>
        public void PopulateFromSnapshot(MarketSnapshot snapshot)
        {
            Snapshot = snapshot;
            RestingOrdersView.Clear();
            if (snapshot?.RestingOrders != null)
            {
                foreach (var (action, side, type, count, price, expiration) in snapshot.RestingOrders)
                {
                    RestingOrdersView.Add(new RestingOrderViewModel
                    {
                        Action = action,
                        Side = side,
                        Type = type,
                        Count = count,
                        Price = price,
                        Expiration = expiration?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty
                    });
                }
            }
        }
    }

    /// <summary>
    /// Simple view model for a single resting order.  The DataGrid binds to properties of
    /// this type so each tuple in the MarketSnapshot is presented as a row.
    /// </summary>
    public class RestingOrderViewModel
    {
        public string Action { get; set; }
        public string Side { get; set; }
        public string Type { get; set; }
        public int Count { get; set; }
        public int Price { get; set; }
        public string Expiration { get; set; }
    }

    /// <summary>
    /// Converts numeric values into a brush.  Positive values produce green, negative values red,
    /// and zero or non‑numeric values use the default foreground colour.  Used for ROI and PnL.
    /// </summary>
    public class PositiveNegativeBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return SystemColors.ControlTextBrush;
            }
            if (double.TryParse(value.ToString(), out double d))
            {
                if (d > 0)
                {
                    return System.Windows.Media.Brushes.Green;
                }
                if (d < 0)
                {
                    return System.Windows.Media.Brushes.Red;
                }
            }
            return SystemColors.ControlTextBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}