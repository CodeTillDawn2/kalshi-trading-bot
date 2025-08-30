using System.Windows.Forms;

namespace SimulatorWinForms
{
    public partial class MarketDashboardControl : UserControl
    {
        public MarketDashboardControl()
        {
            InitializeComponent();
        }

        // expose the chart so MainForm can seed or manipulate it
        public ScottPlot.FormsPlot Plot => pricePlot;
    }
}
