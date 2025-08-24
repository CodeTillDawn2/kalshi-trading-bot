using System.Collections.ObjectModel;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ReactiveUI;

namespace SimulatorGUI.ViewModels
{
    /// <summary>
    /// Encapsulates all data required by a CartesianChart to render a
    /// market's price history.  The Series collection contains lines and
    /// scatter series for midpoint prices, buy/sell events and other
    /// markers.  XAxes and YAxes define the axis configuration for
    /// formatting timestamps and prices.  Market is simply the ticker
    /// being displayed.
    /// </summary>
    public class ChartViewModel : ReactiveObject
    {
        private string _market = string.Empty;
        public string Market
        {
            get => _market;
            set => this.RaiseAndSetIfChanged(ref _market, value);
        }

        private ObservableCollection<ISeries> _series = new();
        public ObservableCollection<ISeries> Series
        {
            get => _series;
            set => this.RaiseAndSetIfChanged(ref _series, value);
        }

        private Axis[] _xAxes = System.Array.Empty<Axis>();
        public Axis[] XAxes
        {
            get => _xAxes;
            set => this.RaiseAndSetIfChanged(ref _xAxes, value);
        }

        private Axis[] _yAxes = System.Array.Empty<Axis>();
        public Axis[] YAxes
        {
            get => _yAxes;
            set => this.RaiseAndSetIfChanged(ref _yAxes, value);
        }
    }
}