using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SimulatorWinForms
{
    partial class MarketDashboardControl
    {
        private IContainer components = null;

        // top bar
        private FlowLayoutPanel marketControls;
        private FlowLayoutPanel marketActions;
        private Button btnSubscribe;
        private Button btnUnsubscribe;
        private TextBox txtTicker;
        private FlowLayoutPanel marketSelection;
        private Label lblWarnings;
        private Label lblErrors;
        private Panel stalenessDot;
        private ComboBox cboMarketSelector;
        private CheckBox chkYesNo;
        private Label lblToggle;
        private FlowLayoutPanel cashPositions;
        private Panel boxCash;
        private Panel boxPos;
        private Panel boxLastWs;
        private Panel boxExch;
        private Panel boxTrading;

        // main dashboard
        private TableLayoutPanel dashboard;
        private GroupBox grpChart;
        private TableLayoutPanel chartContainer;
        private FlowLayoutPanel chartControls;
        private ComboBox cboTimeframe;
        private ScottPlot.FormsPlot pricePlot;

        private GroupBox grpInfo;
        private TableLayoutPanel infoGrid;
        private TableLayoutPanel leftInfo;
        private TableLayoutPanel priceGrid;
        private TableLayoutPanel tradeMetrics;
        private TableLayoutPanel rightInfo;
        private TableLayoutPanel rightDetails1;
        private TableLayoutPanel rightDetails2;

        private GroupBox grpPositions;
        private TableLayoutPanel positionsPanel;
        private TableLayoutPanel positionsGrid;
        private ComboBox cboBuyStrategy;
        private ComboBox cboExitStrategy;

        private GroupBox grpOrderbook;
        private TableLayoutPanel orderbookLayout;
        private Label lblOrderbookUpdated;
        private DataGridView dgvOrderbook;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private Panel MakeStatBox(string labelText, out Label valueLabel)
        {
            var container = new Panel
            {
                BackColor = Color.FromArgb(0x2a, 0x2a, 0x2a),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(6),
                Width = 120,
                Height = 44,
                Margin = new Padding(6, 2, 6, 2)
            };
            var k = new Label { Text = labelText, AutoSize = true, ForeColor = Color.Silver };
            valueLabel = new Label { Text = "--", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var stack = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown };
            stack.Controls.Add(k);
            stack.Controls.Add(valueLabel);
            container.Controls.Add(stack);
            return container;
        }

        private TableLayoutPanel MakeTwoColGrid((string key, string val)[] rows)
        {
            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoScroll = true };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            foreach (var r in rows)
            {
                var k = new Label { Text = r.key, AutoSize = true, Margin = new Padding(3, 2, 3, 2) };
                var v = new Label { Text = r.val, AutoSize = true, Margin = new Padding(3, 2, 3, 2), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
                tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                tlp.Controls.Add(k);
                tlp.Controls.Add(v);
            }
            return tlp;
        }

        private void InitializeComponent()
        {
            components = new Container();

            // ======= control-level styling =======
            SuspendLayout();
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor  = Color.Gainsboro;
            Dock = DockStyle.Fill;

            // ======= top market controls =======
            marketControls = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(6, 6, 6, 0),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            marketActions = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            btnSubscribe = new Button { Text = "+", Width = 40, Height = 32, BackColor = Color.Teal, FlatStyle = FlatStyle.Flat };
            btnUnsubscribe = new Button { Text = "−", Width = 40, Height = 32, BackColor = Color.Teal, FlatStyle = FlatStyle.Flat };
            txtTicker = new TextBox { Width = 140, Height = 28, BorderStyle = BorderStyle.FixedSingle };
            marketActions.Controls.AddRange(new Control[] { btnSubscribe, btnUnsubscribe, txtTicker });

            marketSelection = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Padding = new Padding(8, 0, 8, 0) };
            lblWarnings = new Label { Text = "0", AutoSize = true, ForeColor = Color.Orange };
            lblErrors = new Label { Text = "0", AutoSize = true, ForeColor = Color.Red };
            stalenessDot = new Panel { Width = 18, Height = 18, BackColor = Color.FromArgb(0x25, 0x25, 0x25), Margin = new Padding(6, 2, 6, 2) };
            cboMarketSelector = new ComboBox { Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
            chkYesNo = new CheckBox { Checked = true, AutoSize = true, Margin = new Padding(8, 8, 2, 2) };
            lblToggle = new Label { Text = "Yes", AutoSize = true, Margin = new Padding(4, 8, 0, 0) };
            marketSelection.Controls.AddRange(new Control[] { lblWarnings, lblErrors, stalenessDot, cboMarketSelector, chkYesNo, lblToggle });

            cashPositions = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            boxCash = MakeStatBox("Balance", out _);
            boxPos = MakeStatBox("Position", out _);
            boxLastWs = MakeStatBox("Last WebSocket", out _);
            boxExch = MakeStatBox("Exchange", out _);
            boxTrading = MakeStatBox("Trading", out _);
            cashPositions.Controls.AddRange(new Control[] { boxCash, boxPos, boxLastWs, boxExch, boxTrading });

            marketControls.Controls.AddRange(new Control[] { marketActions, marketSelection, cashPositions });
            Controls.Add(marketControls);

            // ======= dashboard 2x2 =======
            dashboard = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(6),
                ColumnCount = 2,
                RowCount = 2
            };
            dashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            dashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            dashboard.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            dashboard.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            Controls.Add(dashboard);

            // ======= chart (top-left) =======
            grpChart = new GroupBox { Text = "Price Chart", Dock = DockStyle.Fill, ForeColor = ForeColor };
            chartContainer = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            chartContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            chartContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            chartControls = new FlowLayoutPanel { Dock = DockStyle.Top, FlowDirection = FlowDirection.RightToLeft };
            cboTimeframe = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
            cboTimeframe.Items.AddRange(new object[] { "15m", "1h", "1d", "3d", "1w", "1M", "All" });
            cboTimeframe.SelectedIndex = 2;
            chartControls.Controls.Add(cboTimeframe);
            pricePlot = new ScottPlot.FormsPlot { Dock = DockStyle.Fill };
            chartContainer.Controls.Add(chartControls, 0, 0);
            chartContainer.Controls.Add(pricePlot, 0, 1);
            grpChart.Controls.Add(chartContainer);
            dashboard.Controls.Add(grpChart, 0, 0);

            // ======= info (top-right) =======
            grpInfo = new GroupBox { Text = "Market Info", Dock = DockStyle.Fill, ForeColor = ForeColor };
            infoGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grpInfo.Controls.Add(infoGrid);

            leftInfo = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            leftInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            leftInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            priceGrid = MakeTwoColGrid(new (string, string)[] {
                ("All Time High (Ask)", "-- @ --"),
                ("All Time High (Bid)", "-- @ --"),
                ("Recent High (Ask)", "-- @ --"),
                ("Recent High (Bid)", "-- @ --"),
                ("Current Ask", "--"),
                ("Current Bid", "--"),
                ("Recent Low (Ask)", "-- @ --"),
                ("Recent Low (Bid)", "-- @ --"),
                ("All Time Low (Ask)", "-- @ --"),
                ("All Time Low (Bid)", "-- @ --"),
            });
            tradeMetrics = MakeTwoColGrid(new (string, string)[] {
                ("RSI", "--"), ("MACD", "--"), ("EMA", "--"), ("Bollinger Bands", "--"),
                ("ATR", "--"), ("VWAP", "--"), ("Stochastic", "--"), ("OBV", "--"),
            });
            leftInfo.Controls.Add(priceGrid, 0, 0);
            leftInfo.Controls.Add(tradeMetrics, 0, 1);

            rightInfo = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            rightInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            rightInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            rightDetails1 = MakeTwoColGrid(new (string, string)[] {
                ("Title", "--"), ("Subtitle", "--"), ("Market Type", "--"),
                ("Price Good/Bad", "--"), ("Market Behavior", "--"),
                ("Time Left", "--"), ("Market Age", "--"),
                ("Top Velocity (Ask/No)", "-- / --"),
                ("Bottom Velocity (Ask/No)", "-- / --"),
                ("Net Order Rate (Ask/No)", "-- / --"),
                ("Trade Volume (Ask/No)", "-- / --"),
                ("Avg Trade Size (Ask/No)", "-- / --")
            });
            rightDetails2 = MakeTwoColGrid(new (string, string)[] {
                ("Spread", "--"), ("Ask/Bid Imbal (Vol)", "--"),
                ("Depth Top 4 (Ask/No)", "-- / --"),
                ("Center Mass (Ask/No)", "-- / --"),
                ("Total Contracts (Ask/No)", "-- / --"),
            });
            rightInfo.Controls.Add(rightDetails1, 0, 0);
            rightInfo.Controls.Add(rightDetails2, 0, 1);

            infoGrid.Controls.Add(leftInfo, 0, 0);
            infoGrid.Controls.Add(rightInfo, 1, 0);
            dashboard.Controls.Add(grpInfo, 1, 0);

            // ======= positions (bottom-left) =======
            grpPositions = new GroupBox { Text = "Positions", Dock = DockStyle.Fill, ForeColor = ForeColor };
            positionsPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            positionsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 75));
            positionsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            positionsGrid = MakeTwoColGrid(new (string, string)[] {
                ("Position Size", "--"), ("Position ROI", "--"),
                ("Position Upside", "--"), ("Resting Orders", "--"),
                ("Last Trade", "--"), ("Buyin Price", "--"), ("Position Downside", "--"),
            });
            var stratPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill };
            cboBuyStrategy = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
            cboExitStrategy = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
            cboBuyStrategy.Items.Add("Buy Strategy");
            cboExitStrategy.Items.Add("Exit Strategy");
            cboBuyStrategy.SelectedIndex = 0;
            cboExitStrategy.SelectedIndex = 0;
            stratPanel.Controls.AddRange(new Control[] { cboBuyStrategy, cboExitStrategy });
            positionsPanel.Controls.Add(positionsGrid, 0, 0);
            positionsPanel.Controls.Add(stratPanel, 0, 1);
            grpPositions.Controls.Add(positionsPanel);
            dashboard.Controls.Add(grpPositions, 0, 1);

            // ======= orderbook (bottom-right) =======
            grpOrderbook = new GroupBox { Text = "Orderbook", Dock = DockStyle.Fill, ForeColor = ForeColor };
            orderbookLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            orderbookLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            orderbookLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            lblOrderbookUpdated = new Label { Text = "Updated --", AutoSize = true, ForeColor = Color.Silver, Margin = new Padding(6, 3, 0, 3) };
            dgvOrderbook = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = BackColor,
                BorderStyle = BorderStyle.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };
            dgvOrderbook.Columns.Add("Price", "Price");
            dgvOrderbook.Columns.Add("Size", "Size");
            dgvOrderbook.Columns.Add("Value", "Value");
            orderbookLayout.Controls.Add(lblOrderbookUpdated, 0, 0);
            orderbookLayout.Controls.Add(dgvOrderbook, 0, 1);
            grpOrderbook.Controls.Add(orderbookLayout);
            dashboard.Controls.Add(grpOrderbook, 1, 1);

            ResumeLayout(false);
        }
    }
}
