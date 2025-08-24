using ReactiveUI;

namespace SimulatorGUI.ViewModels
{
    /// <summary>
    /// Represents a row in the market results table.  Each result holds
    /// summary data about a backtested market: its ticker, the realised
    /// profit/loss, whether it has been selected for a run, and an
    /// optional chart model populated when the user drills into the
    /// detailed price history.  ReactiveObject is used so changes
    /// propagate to the UI automatically.
    /// </summary>
    public class MarketResult : ReactiveObject
    {
        private string _market = string.Empty;
        public string Market
        {
            get => _market;
            set => this.RaiseAndSetIfChanged(ref _market, value);
        }

        private double _pnL;
        public double PnL
        {
            get => _pnL;
            set => this.RaiseAndSetIfChanged(ref _pnL, value);
        }

        private ChartViewModel? _chart;
        public ChartViewModel? Chart
        {
            get => _chart;
            set => this.RaiseAndSetIfChanged(ref _chart, value);
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }
    }
}