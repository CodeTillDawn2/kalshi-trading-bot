// MarketDashboardControl2.Designer.cs
using ScottPlot;

namespace SimulatorWinForms
{
    partial class MarketDashboardControl2
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            this.mainLayout = new TableLayoutPanel();
            this.cashPositionsPanel = new FlowLayoutPanel();
            this.balanceBox = new Panel();
            this.balanceLayout = new TableLayoutPanel();
            this.balanceLabel = new Label();
            this.balanceValue = new Label();
            this.positionBox = new Panel();
            this.positionLayout = new TableLayoutPanel();
            this.positionLabel = new Label();
            this.positionValue = new Label();
            this.lastWebSocketBox = new Panel();
            this.lastWebSocketLayout = new TableLayoutPanel();
            this.lastWebSocketLabel = new Label();
            this.lastWebSocketValue = new Label();
            this.exchangeStatusBox = new Panel();
            this.exchangeStatusLayout = new TableLayoutPanel();
            this.exchangeStatusLabel = new Label();
            this.exchangeStatusValue = new Label();
            this.tradingStatusBox = new Panel();
            this.tradingStatusLayout = new TableLayoutPanel();
            this.tradingStatusLabel = new Label();
            this.tradingStatusValue = new Label();
            this.dashboardGrid = new TableLayoutPanel();
            this.chartContainer = new Panel();
            this.chartLayout = new TableLayoutPanel();
            this.chartControls = new FlowLayoutPanel();
            this.chartHeader = new Label();
            this.timeframeCombo = new ComboBox();
            this.priceChart = new FormsPlot();
            this.marketInfoContainer = new Panel();
            this.infoGrid = new TableLayoutPanel();
            this.leftColumn = new TableLayoutPanel();
            this.pricesGrid = new TableLayoutPanel();
            this.allTimeHighLabel = new Label();
            this.allTimeHighAsk = new Panel();
            this.allTimeHighAskPrice = new Label();
            this.allTimeHighAskTime = new Label();
            this.allTimeHighBid = new Panel();
            this.allTimeHighBidPrice = new Label();
            this.allTimeHighBidTime = new Label();
            this.recentHighLabel = new Label();
            this.recentHighAsk = new Panel();
            this.recentHighAskPrice = new Label();
            this.recentHighAskTime = new Label();
            this.recentHighBid = new Panel();
            this.recentHighBidPrice = new Label();
            this.recentHighBidTime = new Label();
            this.currentPriceLabel = new Label();
            this.currentPriceAsk = new Label();
            this.currentPriceBid = new Label();
            this.recentLowLabel = new Label();
            this.recentLowAsk = new Panel();
            this.recentLowAskPrice = new Label();
            this.recentLowAskTime = new Label();
            this.recentLowBid = new Panel();
            this.recentLowBidPrice = new Label();
            this.recentLowBidTime = new Label();
            this.allTimeLowLabel = new Label();
            this.allTimeLowAsk = new Panel();
            this.allTimeLowAskPrice = new Label();
            this.allTimeLowAskTime = new Label();
            this.allTimeLowBid = new Panel();
            this.allTimeLowBidPrice = new Label();
            this.allTimeLowBidTime = new Label();
            this.tradingMetricsGrid = new TableLayoutPanel();
            this.tradingMetricsHeader = new Label();
            this.rsiLabel = new Label();
            this.rsiValue = new Label();
            this.macdLabel = new Label();
            this.macdValue = new Label();
            this.emaLabel = new Label();
            this.emaValue = new Label();
            this.bollingerLabel = new Label();
            this.bollingerValue = new Label();
            this.atrLabel = new Label();
            this.atrValue = new Label();
            this.vwapLabel = new Label();
            this.vwapValue = new Label();
            this.stochasticLabel = new Label();
            this.stochasticValue = new Label();
            this.obvLabel = new Label();
            this.obvValue = new Label();
            this.rightColumn = new TableLayoutPanel();
            this.otherInfoGrid = new TableLayoutPanel();
            this.titleLabel = new Label();
            this.titleValue = new Label();
            this.subtitleLabel = new Label();
            this.subtitleValue = new Label();
            this.marketTypeLabel = new Label();
            this.marketTypeValue = new Label();
            this.priceGoodBadLabel = new Label();
            this.priceGoodBadValue = new Label();
            this.marketBehaviorLabel = new Label();
            this.marketBehaviorValue = new Label();
            this.timeLeftLabel = new Label();
            this.timeLeftValue = new Label();
            this.marketAgeLabel = new Label();
            this.marketAgeValue = new Label();
            this.flowMomentumGrid = new TableLayoutPanel();
            this.flowHeader = new Label();
            this.topVelocityLabel = new Label();
            this.topVelocityValue = new Label();
            this.bottomVelocityLabel = new Label();
            this.bottomVelocityValue = new Label();
            this.netOrderRateLabel = new Label();
            this.netOrderRateValue = new Label();
            this.tradeVolumeLabel = new Label();
            this.tradeVolumeValue = new Label();
            this.avgTradeSizeLabel = new Label();
            this.avgTradeSizeValue = new Label();
            this.contextGrid = new TableLayoutPanel();
            this.contextHeader = new Label();
            this.spreadLabel = new Label();
            this.spreadValue = new Label();
            this.imbalLabel = new Label();
            this.imbalValue = new Label();
            this.depthTop4Label = new Label();
            this.depthTop4Value = new Label();
            this.centerMassLabel = new Label();
            this.centerMassValue = new Label();
            this.totalContractsLabel = new Label();
            this.totalContractsValue = new Label();
            this.positionsContainer = new Panel();
            this.strategyPanel = new FlowLayoutPanel();
            this.buyStrategyCombo = new ComboBox();
            this.exitStrategyCombo = new ComboBox();
            this.positionsGrid = new TableLayoutPanel();
            this.positionSizeLabel = new Label();
            this.positionSizeValue = new Label();
            this.lastTradeLabel = new Label();
            this.lastTradeValue = new Label();
            this.positionRoiLabel = new Label();
            this.positionRoiValue = new Label();
            this.buyinPriceLabel = new Label();
            this.buyinPriceValue = new Label();
            this.positionUpsideLabel = new Label();
            this.positionUpsideValue = new Label();
            this.positionDownsideLabel = new Label();
            this.positionDownsideValue = new Label();
            this.restingOrdersLabel = new Label();
            this.restingOrdersValue = new Label();
            this.orderbookContainer = new Panel();
            this.orderbookGrid = new DataGridView();
            this.priceCol = new DataGridViewTextBoxColumn();
            this.sizeCol = new DataGridViewTextBoxColumn();
            this.valueCol = new DataGridViewTextBoxColumn();
            this.backButton = new Button();
            this.mainLayout.SuspendLayout();
            this.cashPositionsPanel.SuspendLayout();
            this.balanceBox.SuspendLayout();
            this.balanceLayout.SuspendLayout();
            this.positionBox.SuspendLayout();
            this.positionLayout.SuspendLayout();
            this.lastWebSocketBox.SuspendLayout();
            this.lastWebSocketLayout.SuspendLayout();
            this.exchangeStatusBox.SuspendLayout();
            this.exchangeStatusLayout.SuspendLayout();
            this.tradingStatusBox.SuspendLayout();
            this.tradingStatusLayout.SuspendLayout();
            this.dashboardGrid.SuspendLayout();
            this.chartContainer.SuspendLayout();
            this.chartLayout.SuspendLayout();
            this.chartControls.SuspendLayout();
            this.marketInfoContainer.SuspendLayout();
            this.infoGrid.SuspendLayout();
            this.leftColumn.SuspendLayout();
            this.pricesGrid.SuspendLayout();
            this.allTimeHighAsk.SuspendLayout();
            this.allTimeHighBid.SuspendLayout();
            this.recentHighAsk.SuspendLayout();
            this.recentHighBid.SuspendLayout();
            this.recentLowAsk.SuspendLayout();
            this.recentLowBid.SuspendLayout();
            this.allTimeLowAsk.SuspendLayout();
            this.allTimeLowBid.SuspendLayout();
            this.tradingMetricsGrid.SuspendLayout();
            this.rightColumn.SuspendLayout();
            this.otherInfoGrid.SuspendLayout();
            this.flowMomentumGrid.SuspendLayout();
            this.contextGrid.SuspendLayout();
            this.positionsContainer.SuspendLayout();
            this.strategyPanel.SuspendLayout();
            this.positionsGrid.SuspendLayout();
            this.orderbookContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.orderbookGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // mainLayout
            // 
            this.mainLayout.AutoSize = true;
            this.mainLayout.ColumnCount = 1;
            this.mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.mainLayout.Controls.Add(this.cashPositionsPanel, 0, 0);
            this.mainLayout.Controls.Add(this.dashboardGrid, 0, 1);
            this.mainLayout.Controls.Add(this.backButton, 0, 2);
            this.mainLayout.Dock = DockStyle.Fill;
            this.mainLayout.Location = new Point(0, 0);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.RowCount = 3;
            this.mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.mainLayout.Size = new Size(800, 600);
            this.mainLayout.TabIndex = 0;
            // 
            // cashPositionsPanel
            // 
            this.cashPositionsPanel.AutoSize = true;
            this.cashPositionsPanel.Controls.Add(this.balanceBox);
            this.cashPositionsPanel.Controls.Add(this.positionBox);
            this.cashPositionsPanel.Controls.Add(this.lastWebSocketBox);
            this.cashPositionsPanel.Controls.Add(this.exchangeStatusBox);
            this.cashPositionsPanel.Controls.Add(this.tradingStatusBox);
            this.cashPositionsPanel.Dock = DockStyle.Fill;
            this.cashPositionsPanel.FlowDirection = FlowDirection.LeftToRight;
            this.cashPositionsPanel.Location = new Point(3, 3);
            this.cashPositionsPanel.Name = "cashPositionsPanel";
            this.cashPositionsPanel.Padding = new Padding(5);
            this.cashPositionsPanel.Size = new Size(794, 50);
            this.cashPositionsPanel.TabIndex = 0;
            // 
            // balanceBox
            // 
            this.balanceBox.AutoSize = true;
            this.balanceBox.BackColor = Color.FromArgb(42, 42, 42);
            this.balanceBox.BorderStyle = BorderStyle.FixedSingle;
            this.balanceBox.Controls.Add(this.balanceLayout);
            this.balanceBox.Margin = new Padding(3);
            this.balanceBox.Name = "balanceBox";
            this.balanceBox.Padding = new Padding(3);
            this.balanceBox.Size = new Size(100, 40);
            this.balanceBox.TabIndex = 0;
            // 
            // balanceLayout
            // 
            this.balanceLayout.AutoSize = true;
            this.balanceLayout.ColumnCount = 1;
            this.balanceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.balanceLayout.Controls.Add(this.balanceLabel, 0, 0);
            this.balanceLayout.Controls.Add(this.balanceValue, 0, 1);
            this.balanceLayout.Dock = DockStyle.Fill;
            this.balanceLayout.Location = new Point(3, 3);
            this.balanceLayout.Name = "balanceLayout";
            this.balanceLayout.RowCount = 2;
            this.balanceLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.balanceLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.balanceLayout.Size = new Size(92, 32);
            this.balanceLayout.TabIndex = 0;
            // 
            // balanceLabel
            // 
            this.balanceLabel.AutoSize = true;
            this.balanceLabel.ForeColor = Color.Gray;
            this.balanceLabel.Font = new Font("Segoe UI", 7F);
            this.balanceLabel.Name = "balanceLabel";
            this.balanceLabel.Size = new Size(50, 13);
            this.balanceLabel.TabIndex = 0;
            this.balanceLabel.Text = "Balance";
            // 
            // balanceValue
            // 
            this.balanceValue.AutoSize = true;
            this.balanceValue.ForeColor = this.textColor;
            this.balanceValue.Name = "balanceValue";
            this.balanceValue.Size = new Size(35, 13);
            this.balanceValue.TabIndex = 1;
            this.balanceValue.Text = "$0.00";
            // 
            // positionBox
            // 
            this.positionBox.AutoSize = true;
            this.positionBox.BackColor = Color.FromArgb(42, 42, 42);
            this.positionBox.BorderStyle = BorderStyle.FixedSingle;
            this.positionBox.Controls.Add(this.positionLayout);
            this.positionBox.Margin = new Padding(3);
            this.positionBox.Name = "positionBox";
            this.positionBox.Padding = new Padding(3);
            this.positionBox.Size = new Size(100, 40);
            this.positionBox.TabIndex = 1;
            // 
            // positionLayout
            // 
            this.positionLayout.AutoSize = true;
            this.positionLayout.ColumnCount = 1;
            this.positionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.positionLayout.Controls.Add(this.positionLabel, 0, 0);
            this.positionLayout.Controls.Add(this.positionValue, 0, 1);
            this.positionLayout.Dock = DockStyle.Fill;
            this.positionLayout.Location = new Point(3, 3);
            this.positionLayout.Name = "positionLayout";
            this.positionLayout.RowCount = 2;
            this.positionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.positionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.positionLayout.Size = new Size(92, 32);
            this.positionLayout.TabIndex = 0;
            // 
            // positionLabel
            // 
            this.positionLabel.AutoSize = true;
            this.positionLabel.ForeColor = Color.Gray;
            this.positionLabel.Font = new Font("Segoe UI", 7F);
            this.positionLabel.Name = "positionLabel";
            this.positionLabel.Size = new Size(50, 13);
            this.positionLabel.TabIndex = 0;
            this.positionLabel.Text = "Position";
            // 
            // positionValue
            // 
            this.positionValue.AutoSize = true;
            this.positionValue.ForeColor = this.textColor;
            this.positionValue.Name = "positionValue";
            this.positionValue.Size = new Size(35, 13);
            this.positionValue.TabIndex = 1;
            this.positionValue.Text = "$0.00";
            // 
            // lastWebSocketBox
            // 
            this.lastWebSocketBox.AutoSize = true;
            this.lastWebSocketBox.BackColor = Color.FromArgb(42, 42, 42);
            this.lastWebSocketBox.BorderStyle = BorderStyle.FixedSingle;
            this.lastWebSocketBox.Controls.Add(this.lastWebSocketLayout);
            this.lastWebSocketBox.Margin = new Padding(3);
            this.lastWebSocketBox.Name = "lastWebSocketBox";
            this.lastWebSocketBox.Padding = new Padding(3);
            this.lastWebSocketBox.Size = new Size(150, 40);
            this.lastWebSocketBox.TabIndex = 2;
            // 
            // lastWebSocketLayout
            // 
            this.lastWebSocketLayout.AutoSize = true;
            this.lastWebSocketLayout.ColumnCount = 1;
            this.lastWebSocketLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.lastWebSocketLayout.Controls.Add(this.lastWebSocketLabel, 0, 0);
            this.lastWebSocketLayout.Controls.Add(this.lastWebSocketValue, 0, 1);
            this.lastWebSocketLayout.Dock = DockStyle.Fill;
            this.lastWebSocketLayout.Location = new Point(3, 3);
            this.lastWebSocketLayout.Name = "lastWebSocketLayout";
            this.lastWebSocketLayout.RowCount = 2;
            this.lastWebSocketLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.lastWebSocketLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.lastWebSocketLayout.Size = new Size(142, 32);
            this.lastWebSocketLayout.TabIndex = 0;
            // 
            // lastWebSocketLabel
            // 
            this.lastWebSocketLabel.AutoSize = true;
            this.lastWebSocketLabel.ForeColor = Color.Gray;
            this.lastWebSocketLabel.Font = new Font("Segoe UI", 7F);
            this.lastWebSocketLabel.Name = "lastWebSocketLabel";
            this.lastWebSocketLabel.Size = new Size(128, 13);
            this.lastWebSocketLabel.TabIndex = 0;
            this.lastWebSocketLabel.Text = "Last Web Socket Event";
            // 
            // lastWebSocketValue
            // 
            this.lastWebSocketValue.AutoSize = true;
            this.lastWebSocketValue.ForeColor = this.textColor;
            this.lastWebSocketValue.Name = "lastWebSocketValue";
            this.lastWebSocketValue.Size = new Size(14, 13);
            this.lastWebSocketValue.TabIndex = 1;
            this.lastWebSocketValue.Text = "--";
            // 
            // exchangeStatusBox
            // 
            this.exchangeStatusBox.AutoSize = true;
            this.exchangeStatusBox.BackColor = Color.FromArgb(42, 42, 42);
            this.exchangeStatusBox.BorderStyle = BorderStyle.FixedSingle;
            this.exchangeStatusBox.Controls.Add(this.exchangeStatusLayout);
            this.exchangeStatusBox.Margin = new Padding(3);
            this.exchangeStatusBox.Name = "exchangeStatusBox";
            this.exchangeStatusBox.Padding = new Padding(3);
            this.exchangeStatusBox.Size = new Size(150, 40);
            this.exchangeStatusBox.TabIndex = 3;
            // 
            // exchangeStatusLayout
            // 
            this.exchangeStatusLayout.AutoSize = true;
            this.exchangeStatusLayout.ColumnCount = 1;
            this.exchangeStatusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.exchangeStatusLayout.Controls.Add(this.exchangeStatusLabel, 0, 0);
            this.exchangeStatusLayout.Controls.Add(this.exchangeStatusValue, 0, 1);
            this.exchangeStatusLayout.Dock = DockStyle.Fill;
            this.exchangeStatusLayout.Location = new Point(3, 3);
            this.exchangeStatusLayout.Name = "exchangeStatusLayout";
            this.exchangeStatusLayout.RowCount = 2;
            this.exchangeStatusLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.exchangeStatusLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.exchangeStatusLayout.Size = new Size(142, 32);
            this.exchangeStatusLayout.TabIndex = 0;
            // 
            // exchangeStatusLabel
            // 
            this.exchangeStatusLabel.AutoSize = true;
            this.exchangeStatusLabel.ForeColor = Color.Gray;
            this.exchangeStatusLabel.Font = new Font("Segoe UI", 7F);
            this.exchangeStatusLabel.Name = "exchangeStatusLabel";
            this.exchangeStatusLabel.Size = new Size(96, 13);
            this.exchangeStatusLabel.TabIndex = 0;
            this.exchangeStatusLabel.Text = "Exchange Status";
            // 
            // exchangeStatusValue
            // 
            this.exchangeStatusValue.AutoSize = true;
            this.exchangeStatusValue.ForeColor = this.textColor;
            this.exchangeStatusValue.Name = "exchangeStatusValue";
            this.exchangeStatusValue.Size = new Size(14, 13);
            this.exchangeStatusValue.TabIndex = 1;
            this.exchangeStatusValue.Text = "--";
            // 
            // tradingStatusBox
            // 
            this.tradingStatusBox.AutoSize = true;
            this.tradingStatusBox.BackColor = Color.FromArgb(42, 42, 42);
            this.tradingStatusBox.BorderStyle = BorderStyle.FixedSingle;
            this.tradingStatusBox.Controls.Add(this.tradingStatusLayout);
            this.tradingStatusBox.Margin = new Padding(3);
            this.tradingStatusBox.Name = "tradingStatusBox";
            this.tradingStatusBox.Padding = new Padding(3);
            this.tradingStatusBox.Size = new Size(150, 40);
            this.tradingStatusBox.TabIndex = 4;
            // 
            // tradingStatusLayout
            // 
            this.tradingStatusLayout.AutoSize = true;
            this.tradingStatusLayout.ColumnCount = 1;
            this.tradingStatusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.tradingStatusLayout.Controls.Add(this.tradingStatusLabel, 0, 0);
            this.tradingStatusLayout.Controls.Add(this.tradingStatusValue, 0, 1);
            this.tradingStatusLayout.Dock = DockStyle.Fill;
            this.tradingStatusLayout.Location = new Point(3, 3);
            this.tradingStatusLayout.Name = "tradingStatusLayout";
            this.tradingStatusLayout.RowCount = 2;
            this.tradingStatusLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.tradingStatusLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.tradingStatusLayout.Size = new Size(142, 32);
            this.tradingStatusLayout.TabIndex = 0;
            // 
            // tradingStatusLabel
            // 
            this.tradingStatusLabel.AutoSize = true;
            this.tradingStatusLabel.ForeColor = Color.Gray;
            this.tradingStatusLabel.Font = new Font("Segoe UI", 7F);
            this.tradingStatusLabel.Name = "tradingStatusLabel";
            this.tradingStatusLabel.Size = new Size(86, 13);
            this.tradingStatusLabel.TabIndex = 0;
            this.tradingStatusLabel.Text = "Trading Status";
            // 
            // tradingStatusValue
            // 
            this.tradingStatusValue.AutoSize = true;
            this.tradingStatusValue.ForeColor = this.textColor;
            this.tradingStatusValue.Name = "tradingStatusValue";
            this.tradingStatusValue.Size = new Size(14, 13);
            this.tradingStatusValue.TabIndex = 1;
            this.tradingStatusValue.Text = "--";
            // 
            // dashboardGrid
            // 
            this.dashboardGrid.ColumnCount = 2;
            this.dashboardGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.dashboardGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.dashboardGrid.Controls.Add(this.chartContainer, 0, 0);
            this.dashboardGrid.Controls.Add(this.marketInfoContainer, 1, 0);
            this.dashboardGrid.Controls.Add(this.positionsContainer, 0, 1);
            this.dashboardGrid.Controls.Add(this.orderbookContainer, 1, 1);
            this.dashboardGrid.Dock = DockStyle.Fill;
            this.dashboardGrid.Location = new Point(3, 59);
            this.dashboardGrid.Name = "dashboardGrid";
            this.dashboardGrid.RowCount = 2;
            this.dashboardGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this.dashboardGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this.dashboardGrid.Size = new Size(794, 500);
            this.dashboardGrid.TabIndex = 1;
            // 
            // chartContainer
            // 
            this.chartContainer.BackColor = this.panelBg;
            this.chartContainer.BorderStyle = BorderStyle.FixedSingle;
            this.chartContainer.Controls.Add(this.chartLayout);
            this.chartContainer.Dock = DockStyle.Fill;
            this.chartContainer.Location = new Point(3, 3);
            this.chartContainer.Name = "chartContainer";
            this.chartContainer.Padding = new Padding(5);
            this.chartContainer.Size = new Size(391, 244);
            this.chartContainer.TabIndex = 0;
            // 
            // chartLayout
            // 
            this.chartLayout.AutoSize = true;
            this.chartLayout.ColumnCount = 1;
            this.chartLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.chartLayout.Controls.Add(this.chartControls, 0, 0);
            this.chartLayout.Controls.Add(this.priceChart, 0, 1);
            this.chartLayout.Dock = DockStyle.Fill;
            this.chartLayout.Location = new Point(5, 5);
            this.chartLayout.Name = "chartLayout";
            this.chartLayout.RowCount = 2;
            this.chartLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.chartLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.chartLayout.Size = new Size(379, 232);
            this.chartLayout.TabIndex = 0;
            // 
            // chartControls
            // 
            this.chartControls.AutoSize = true;
            this.chartControls.Controls.Add(this.chartHeader);
            this.chartControls.Controls.Add(this.timeframeCombo);
            this.chartControls.Dock = DockStyle.Fill;
            this.chartControls.Location = new Point(3, 3);
            this.chartControls.Name = "chartControls";
            this.chartControls.Size = new Size(373, 21);
            this.chartControls.TabIndex = 0;
            // 
            // chartHeader
            // 
            this.chartHeader.AutoSize = true;
            this.chartHeader.ForeColor = Color.White;
            this.chartHeader.Location = new Point(0, 0);
            this.chartHeader.Name = "chartHeader";
            this.chartHeader.Size = new Size(70, 13);
            this.chartHeader.TabIndex = 0;
            this.chartHeader.Text = "Price Chart - ";
            // 
            // timeframeCombo
            // 
            this.timeframeCombo.BackColor = Color.FromArgb(42, 42, 42);
            this.timeframeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            this.timeframeCombo.ForeColor = this.textColor;
            this.timeframeCombo.FormattingEnabled = true;
            this.timeframeCombo.Items.AddRange(new object[] {
            "15 Minutes",
            "1 Hour",
            "1 Day",
            "3 Days",
            "1 Week",
            "1 Month",
            "All"});
            this.timeframeCombo.Location = new Point(73, 0);
            this.timeframeCombo.Name = "timeframeCombo";
            this.timeframeCombo.Size = new Size(121, 21);
            this.timeframeCombo.TabIndex = 1;
            this.timeframeCombo.SelectedIndex = 6;
            // 
            // priceChart
            // 
            this.priceChart.Dock = DockStyle.Fill;
            this.priceChart.Location = new Point(3, 30);
            this.priceChart.Name = "priceChart";
            this.priceChart.Size = new Size(373, 199);
            this.priceChart.TabIndex = 1;
            // 
            // marketInfoContainer
            // 
            this.marketInfoContainer.BackColor = this.panelBg;
            this.marketInfoContainer.BorderStyle = BorderStyle.FixedSingle;
            this.marketInfoContainer.Controls.Add(this.infoGrid);
            this.marketInfoContainer.Dock = DockStyle.Fill;
            this.marketInfoContainer.Location = new Point(400, 3);
            this.marketInfoContainer.Name = "marketInfoContainer";
            this.marketInfoContainer.Padding = new Padding(5);
            this.marketInfoContainer.Size = new Size(391, 244);
            this.marketInfoContainer.TabIndex = 1;
            // 
            // infoGrid
            // 
            this.infoGrid.ColumnCount = 2;
            this.infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.infoGrid.Controls.Add(this.leftColumn, 0, 0);
            this.infoGrid.Controls.Add(this.rightColumn, 1, 0);
            this.infoGrid.Dock = DockStyle.Fill;
            this.infoGrid.Location = new Point(5, 5);
            this.infoGrid.Name = "infoGrid";
            this.infoGrid.RowCount = 1;
            this.infoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.infoGrid.Size = new Size(379, 232);
            this.infoGrid.TabIndex = 0;
            // 
            // leftColumn
            // 
            this.leftColumn.AutoSize = true;
            this.leftColumn.ColumnCount = 1;
            this.leftColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.leftColumn.Controls.Add(this.pricesGrid, 0, 0);
            this.leftColumn.Controls.Add(this.tradingMetricsGrid, 0, 1);
            this.leftColumn.Dock = DockStyle.Fill;
            this.leftColumn.Location = new Point(0, 0);
            this.leftColumn.Name = "leftColumn";
            this.leftColumn.RowCount = 2;
            this.leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.leftColumn.Size = new Size(189, 232);
            this.leftColumn.TabIndex = 0;
            // 
            // pricesGrid
            // 
            this.pricesGrid.AutoSize = true;
            this.pricesGrid.ColumnCount = 3;
            this.pricesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.pricesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            this.pricesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            this.pricesGrid.Controls.Add(this.allTimeHighLabel, 0, 0);
            this.pricesGrid.Controls.Add(this.allTimeHighAsk, 1, 0);
            this.pricesGrid.Controls.Add(this.allTimeHighBid, 2, 0);
            this.pricesGrid.Controls.Add(this.recentHighLabel, 0, 1);
            this.pricesGrid.Controls.Add(this.recentHighAsk, 1, 1);
            this.pricesGrid.Controls.Add(this.recentHighBid, 2, 1);
            this.pricesGrid.Controls.Add(this.currentPriceLabel, 0, 2);
            this.pricesGrid.Controls.Add(this.currentPriceAsk, 1, 2);
            this.pricesGrid.Controls.Add(this.currentPriceBid, 2, 2);
            this.pricesGrid.Controls.Add(this.recentLowLabel, 0, 3);
            this.pricesGrid.Controls.Add(this.recentLowAsk, 1, 3);
            this.pricesGrid.Controls.Add(this.recentLowBid, 2, 3);
            this.pricesGrid.Controls.Add(this.allTimeLowLabel, 0, 4);
            this.pricesGrid.Controls.Add(this.allTimeLowAsk, 1, 4);
            this.pricesGrid.Controls.Add(this.allTimeLowBid, 2, 4);
            this.pricesGrid.Dock = DockStyle.Top;
            this.pricesGrid.Location = new Point(0, 0);
            this.pricesGrid.Name = "pricesGrid";
            this.pricesGrid.RowCount = 5;
            this.pricesGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.pricesGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.pricesGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.pricesGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.pricesGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.pricesGrid.Size = new Size(189, 100);
            this.pricesGrid.TabIndex = 0;
            // 
            // allTimeHighLabel
            // 
            this.allTimeHighLabel.AutoSize = true;
            this.allTimeHighLabel.Dock = DockStyle.Fill;
            this.allTimeHighLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.allTimeHighLabel.Name = "allTimeHighLabel";
            this.allTimeHighLabel.Size = new Size(94, 20);
            this.allTimeHighLabel.TabIndex = 0;
            this.allTimeHighLabel.Text = "All Time High";
            // 
            // allTimeHighAsk
            // 
            this.allTimeHighAsk.AutoSize = true;
            this.allTimeHighAsk.Controls.Add(this.allTimeHighAskPrice);
            this.allTimeHighAsk.Controls.Add(this.allTimeHighAskTime);
            this.allTimeHighAsk.Dock = DockStyle.Fill;
            this.allTimeHighAsk.Name = "allTimeHighAsk";
            this.allTimeHighAsk.Size = new Size(47, 20);
            this.allTimeHighAsk.TabIndex = 1;
            // 
            // allTimeHighAskPrice
            // 
            this.allTimeHighAskPrice.AutoSize = true;
            this.allTimeHighAskPrice.ForeColor = this.askColor;
            this.allTimeHighAskPrice.TextAlign = ContentAlignment.MiddleCenter;
            this.allTimeHighAskPrice.Name = "allTimeHighAskPrice";
            this.allTimeHighAskPrice.Size = new Size(14, 13);
            this.allTimeHighAskPrice.TabIndex = 0;
            this.allTimeHighAskPrice.Text = "--";
            // 
            // allTimeHighAskTime
            // 
            this.allTimeHighAskTime.AutoSize = true;
            this.allTimeHighAskTime.ForeColor = Color.Gray;
            this.allTimeHighAskTime.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            this.allTimeHighAskTime.TextAlign = ContentAlignment.MiddleCenter;
            this.allTimeHighAskTime.Name = "allTimeHighAskTime";
            this.allTimeHighAskTime.Size = new Size(14, 13);
            this.allTimeHighAskTime.TabIndex = 1;
            this.allTimeHighAskTime.Text = "--";
            // 
            // allTimeHighBid
            // 
            this.allTimeHighBid.AutoSize = true;
            this.allTimeHighBid.Controls.Add(this.allTimeHighBidPrice);
            this.allTimeHighBid.Controls.Add(this.allTimeHighBidTime);
            this.allTimeHighBid.Dock = DockStyle.Fill;
            this.allTimeHighBid.Name = "allTimeHighBid";
            this.allTimeHighBid.Size = new Size(48, 20);
            this.allTimeHighBid.TabIndex = 2;
            // 
            // allTimeHighBidPrice
            // 
            this.allTimeHighBidPrice.AutoSize = true;
            this.allTimeHighBidPrice.ForeColor = this.bidColor;
            this.allTimeHighBidPrice.TextAlign = ContentAlignment.MiddleCenter;
            this.allTimeHighBidPrice.Name = "allTimeHighBidPrice";
            this.allTimeHighBidPrice.Size = new Size(14, 13);
            this.allTimeHighBidPrice.TabIndex = 0;
            this.allTimeHighBidPrice.Text = "--";
            // 
            // allTimeHighBidTime
            // 
            this.allTimeHighBidTime.AutoSize = true;
            this.allTimeHighBidTime.ForeColor = Color.Gray;
            this.allTimeHighBidTime.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            this.allTimeHighBidTime.TextAlign = ContentAlignment.MiddleCenter;
            this.allTimeHighBidTime.Name = "allTimeHighBidTime";
            this.allTimeHighBidTime.Size = new Size(14, 13);
            this.allTimeHighBidTime.TabIndex = 1;
            this.allTimeHighBidTime.Text = "--";
            // 
            // recentHighLabel
            // 
            this.recentHighLabel.AutoSize = true;
            this.recentHighLabel.Dock = DockStyle.Fill;
            this.recentHighLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.recentHighLabel.Name = "recentHighLabel";
            this.recentHighLabel.Size = new Size(94, 20);
            this.recentHighLabel.TabIndex = 3;
            this.recentHighLabel.Text = "Recent High";
            // 
            // recentHighAsk
            // 
            this.recentHighAsk.AutoSize = true;
            this.recentHighAsk.Controls.Add(this.recentHighAskPrice);
            this.recentHighAsk.Controls.Add(this.recentHighAskTime);
            this.recentHighAsk.Dock = DockStyle.Fill;
            this.recentHighAsk.Name = "recentHighAsk";
            this.recentHighAsk.Size = new Size(47, 20);
            this.recentHighAsk.TabIndex = 4;
            // 
            // recentHighAskPrice
            // 
            this.recentHighAskPrice.AutoSize = true;
            this.recentHighAskPrice.ForeColor = this.askColor;
            this.recentHighAskPrice.TextAlign = ContentAlignment.MiddleCenter;
            this.recentHighAskPrice.Name = "recentHighAskPrice";
            this.recentHighAskPrice.Size = new Size(14, 13);
            this.recentHighAskPrice.TabIndex = 0;
            this.recentHighAskPrice.Text = "--";
            // 
            // recentHighAskTime
            // 
            this.recentHighAskTime.AutoSize = true;
            this.recentHighAskTime.ForeColor = Color.Gray;
            this.recentHighAskTime.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            this.recentHighAskTime.TextAlign = ContentAlignment.MiddleCenter;
            this.recentHighAskTime.Name = "recentHighAskTime";
            this.recentHighAskTime.Size = new Size(14, 13);
            this.recentHighAskTime.TabIndex = 1;
            this.recentHighAskTime.Text = "--";
            // 
            // recentHighBid
            // 
            this.recentHighBid.AutoSize = true;
            this.recentHighBid.Controls.Add(this.recentHighBidPrice);
            this.recentHighBid.Controls.Add(this.recentHighBidTime);
            this.recentHighBid.Dock = DockStyle.Fill;
            this.recentHighBid.Name = "recentHighBid";
            this.recentHighBid.Size = new Size(48, 20);
            this.recentHighBid.TabIndex = 5;
            // 
            // recentHighBidPrice
            // 
            this.recentHighBidPrice.AutoSize = true;
            this.recentHighBidPrice.ForeColor = this.bidColor;
            this.recentHighBidPrice.TextAlign = ContentAlignment.MiddleCenter;
            this.recentHighBidPrice.Name = "recentHighBidPrice";
            this.recentHighBidPrice.Size = new Size(14, 13);
            this.recentHighBidPrice.TabIndex = 0;
            this.recentHighBidPrice.Text = "--";
            // 
            // recentHighBidTime
            // 
            this.recentHighBidTime.AutoSize = true;
            this.recentHighBidTime.ForeColor = Color.Gray;
            this.recentHighBidTime.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            this.recentHighBidTime.TextAlign = ContentAlignment.MiddleCenter;
            this.recentHighBidTime.Name = "recentHighBidTime";
            this.recentHighBidTime.Size = new Size(14, 13);
            this.recentHighBidTime.TabIndex = 1;
            this.recentHighBidTime.Text = "--";
            // 
            // currentPriceLabel
            // 
            this.currentPriceLabel.AutoSize = true;
            this.currentPriceLabel.Dock = DockStyle.Fill;
            this.currentPriceLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.currentPriceLabel.Name = "currentPriceLabel";
            this.currentPriceLabel.Size = new Size(94, 20);
            this.currentPriceLabel.TabIndex = 6;
            this.currentPriceLabel.Text = "Current Price";
            // 
            // currentPriceAsk
            // 
            this.currentPriceAsk.AutoSize = true;
            this.currentPriceAsk.ForeColor = this.askColor;
            this.currentPriceAsk.Dock = DockStyle.Fill;
            this.currentPriceAsk.TextAlign = ContentAlignment.MiddleCenter;
            this.currentPriceAsk.Name = "currentPriceAsk";
            this.currentPriceAsk.Size = new Size(47, 20);
            this.currentPriceAsk.TabIndex = 7;
            this.currentPriceAsk.Text = "--";
            // 
            // currentPriceBid
            // 
            this.currentPriceBid.AutoSize = true;
            this.currentPriceBid.ForeColor = this.bidColor;
            this.currentPriceBid.Dock = DockStyle.Fill;
            this.currentPriceBid.TextAlign = ContentAlignment.MiddleCenter;
            this.currentPriceBid.Name = "currentPriceBid";
            this.currentPriceBid.Size = new Size(48, 20);
            this.currentPriceBid.TabIndex = 8;
            this.currentPriceBid.Text = "--";
            // 
            // recentLowLabel
            // 
            this.recentLowLabel.AutoSize = true;
            this.recentLowLabel.Dock = DockStyle.Fill;
            this.recentLowLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.recentLowLabel.Name = "recentLowLabel";
            this.recentLowLabel.Size = new Size(94, 20);
            this.recentLowLabel.TabIndex = 9;
            this.recentLowLabel.Text = "Recent Low";
            // 
            // recentLowAsk
            // 
            this.recentLowAsk.AutoSize = true;
            this.recentLowAsk.Controls.Add(this.recentLowAskPrice);
            this.recentLowAsk.Controls.Add(this.recentLowAskTime);
            this.recentLowAsk.Dock = DockStyle.Fill;
            this.recentLowAsk.Name = "recentLowAsk";
            this.recentLowAsk.Size = new Size(47, 20);
            this.recentLowAsk.TabIndex = 10;
            // 
            // recentLowAskPrice
            // 
            this.recentLowAskPrice.AutoSize = true;
            this.recentLowAskPrice.ForeColor = this.askColor;
            this.recentLowAskPrice.TextAlign = ContentAlignment.MiddleCenter;
            this.recentLowAskPrice.Name = "recentLowAskPrice";
            this.recentLowAskPrice.Size = new Size(14, 13);
            this.recentLowAskPrice.TabIndex = 0;
            this.recentLowAskPrice.Text = "--";
            // 
            // recentLowAskTime
            // 
            this.recentLowAskTime.AutoSize = true;
            this.recentLowAskTime.ForeColor = Color.Gray;
            this.recentLowAskTime.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            this.recentLowAskTime.TextAlign = ContentAlignment.MiddleCenter;
            this.recentLowAskTime.Name = "recentLowAskTime";
            this.recentLowAskTime.Size = new Size(14, 13);
            this.recentLowAskTime.TabIndex = 1;
            this.recentLowAskTime.Text = "--";
            // 
            // recentLowBid
            // 
            this.recentLowBid.AutoSize = true;
            this.recentLowBid.Controls.Add(this.recentLowBidPrice);
            this.recentLowBid.Controls.Add(this.recentLowBidTime);
            this.recentLowBid.Dock = DockStyle.Fill;
            this.recentLowBid.Name = "recentLowBid";
            this.recentLowBid.Size = new Size(48, 20);
            this.recentLowBid.TabIndex = 11;
            // 
            // recentLowBidPrice
            // 
            this.recentLowBidPrice.AutoSize = true;
            this.recentLowBidPrice.ForeColor = this.bidColor;
            this.recentLowBidPrice.TextAlign = ContentAlignment.MiddleCenter;
            this.recentLowBidPrice.Name = "recentLowBidPrice";
            this.recentLowBidPrice.Size = new Size(14, 13);
            this.recentLowBidPrice.TabIndex = 0;
            this.recentLowBidPrice.Text = "--";
            // 
            // recentLowBidTime
            // 
            this.recentLowBidTime.AutoSize = true;
            this.recentLowBidTime.ForeColor = Color.Gray;
            this.recentLowBidTime.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            this.recentLowBidTime.TextAlign = ContentAlignment.MiddleCenter;
            this.recentLowBidTime.Name = "recentLowBidTime";
            this.recentLowBidTime.Size = new Size(14, 13);
            this.recentLowBidTime.TabIndex = 1;
            this.recentLowBidTime.Text = "--";
            // 
            // allTimeLowLabel
            // 
            this.allTimeLowLabel.AutoSize = true;
            this.allTimeLowLabel.Dock = DockStyle.Fill;
            this.allTimeLowLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.allTimeLowLabel.Name = "allTimeLowLabel";
            this.allTimeLowLabel.Size = new Size(94, 20);
            this.allTimeLowLabel.TabIndex = 12;
            this.allTimeLowLabel.Text = "All Time Low";
            // 
            // allTimeLowAsk
            // 
            this.allTimeLowAsk.AutoSize = true;
            this.allTimeLowAsk.Controls.Add(this.allTimeLowAskPrice);
            this.allTimeLowAsk.Controls.Add(this.allTimeLowAskTime);
            this.allTimeLowAsk.Dock = DockStyle.Fill;
            this.allTimeLowAsk.Name = "allTimeLowAsk";
            this.allTimeLowAsk.Size = new Size(47, 20);
            this.allTimeLowAsk.TabIndex = 13;
            // 
            // allTimeLowAskPrice
            // 
            this.allTimeLowAskPrice.AutoSize = true;
            this.allTimeLowAskPrice.ForeColor = this.askColor;
            this.allTimeLowAskPrice.TextAlign = ContentAlignment.MiddleCenter;
            this.allTimeLowAskPrice.Name = "allTimeLowAskPrice";
            this.allTimeLowAskPrice.Size = new Size(14, 13);
            this.allTimeLowAskPrice.TabIndex = 0;
            this.allTimeLowAskPrice.Text = "--";
            // 
            // allTimeLowAskTime
            // 
            this.allTimeLowAskTime.AutoSize = true;
            this.allTimeLowAskTime.ForeColor = Color.Gray;
            this.allTimeLowAskTime.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            this.allTimeLowAskTime.TextAlign = ContentAlignment.MiddleCenter;
            this.allTimeLowAskTime.Name = "allTimeLowAskTime";
            this.allTimeLowAskTime.Size = new Size(14, 13);
            this.allTimeLowAskTime.TabIndex = 1;
            this.allTimeLowAskTime.Text = "--";
            // 
            // allTimeLowBid
            // 
            this.allTimeLowBid.AutoSize = true;
            this.allTimeLowBid.Controls.Add(this.allTimeLowBidPrice);
            this.allTimeLowBid.Controls.Add(this.allTimeLowBidTime);
            this.allTimeLowBid.Dock = DockStyle.Fill;
            this.allTimeLowBid.Name = "allTimeLowBid";
            this.allTimeLowBid.Size = new Size(48, 20);
            this.allTimeLowBid.TabIndex = 14;
            // 
            // allTimeLowBidPrice
            // 
            this.allTimeLowBidPrice.AutoSize = true;
            this.allTimeLowBidPrice.ForeColor = this.bidColor;
            this.allTimeLowBidPrice.TextAlign = ContentAlignment.MiddleCenter;
            this.allTimeLowBidPrice.Name = "allTimeLowBidPrice";
            this.allTimeLowBidPrice.Size = new Size(14, 13);
            this.allTimeLowBidPrice.TabIndex = 0;
            this.allTimeLowBidPrice.Text = "--";
            // 
            // allTimeLowBidTime
            // 
            this.allTimeLowBidTime.AutoSize = true;
            this.allTimeLowBidTime.ForeColor = Color.Gray;
            this.allTimeLowBidTime.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            this.allTimeLowBidTime.TextAlign = ContentAlignment.MiddleCenter;
            this.allTimeLowBidTime.Name = "allTimeLowBidTime";
            this.allTimeLowBidTime.Size = new Size(14, 13);
            this.allTimeLowBidTime.TabIndex = 1;
            this.allTimeLowBidTime.Text = "--";
            // 
            // tradingMetricsGrid
            // 
            this.tradingMetricsGrid.AutoSize = true;
            this.tradingMetricsGrid.ColumnCount = 2;
            this.tradingMetricsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.tradingMetricsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.tradingMetricsGrid.Controls.Add(this.tradingMetricsHeader, 0, 0);
            this.tradingMetricsGrid.Controls.Add(this.rsiLabel, 0, 1);
            this.tradingMetricsGrid.Controls.Add(this.rsiValue, 1, 1);
            this.tradingMetricsGrid.Controls.Add(this.macdLabel, 0, 2);
            this.tradingMetricsGrid.Controls.Add(this.macdValue, 1, 2);
            this.tradingMetricsGrid.Controls.Add(this.emaLabel, 0, 3);
            this.tradingMetricsGrid.Controls.Add(this.emaValue, 1, 3);
            this.tradingMetricsGrid.Controls.Add(this.bollingerLabel, 0, 4);
            this.tradingMetricsGrid.Controls.Add(this.bollingerValue, 1, 4);
            this.tradingMetricsGrid.Controls.Add(this.atrLabel, 0, 5);
            this.tradingMetricsGrid.Controls.Add(this.atrValue, 1, 5);
            this.tradingMetricsGrid.Controls.Add(this.vwapLabel, 0, 6);
            this.tradingMetricsGrid.Controls.Add(this.vwapValue, 1, 6);
            this.tradingMetricsGrid.Controls.Add(this.stochasticLabel, 0, 7);
            this.tradingMetricsGrid.Controls.Add(this.stochasticValue, 1, 7);
            this.tradingMetricsGrid.Controls.Add(this.obvLabel, 0, 8);
            this.tradingMetricsGrid.Controls.Add(this.obvValue, 1, 8);
            this.tradingMetricsGrid.Dock = DockStyle.Top;
            this.tradingMetricsGrid.Location = new Point(0, 100);
            this.tradingMetricsGrid.Name = "tradingMetricsGrid";
            this.tradingMetricsGrid.RowCount = 9;
            this.tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.tradingMetricsGrid.Size = new Size(189, 180);
            this.tradingMetricsGrid.TabIndex = 1;
            // 
            // tradingMetricsHeader
            // 
            this.tradingMetricsHeader.AutoSize = true;
            this.tradingMetricsHeader.Dock = DockStyle.Fill;
            this.tradingMetricsHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.tradingMetricsHeader.Name = "tradingMetricsHeader";
            this.tradingMetricsHeader.Size = new Size(189, 20);
            this.tradingMetricsHeader.TabIndex = 0;
            this.tradingMetricsHeader.Text = "Trading Metrics";
            this.tradingMetricsGrid.SetColumnSpan(this.tradingMetricsHeader, 2);
            // 
            // rsiLabel
            // 
            this.rsiLabel.AutoSize = true;
            this.rsiLabel.Dock = DockStyle.Fill;
            this.rsiLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.rsiLabel.Name = "rsiLabel";
            this.rsiLabel.Size = new Size(94, 20);
            this.rsiLabel.TabIndex = 0;
            this.rsiLabel.Text = "RSI";
            // 
            // rsiValue
            // 
            this.rsiValue.AutoSize = true;
            this.rsiValue.Dock = DockStyle.Fill;
            this.rsiValue.TextAlign = ContentAlignment.MiddleRight;
            this.rsiValue.Name = "rsiValue";
            this.rsiValue.Size = new Size(95, 20);
            this.rsiValue.TabIndex = 1;
            this.rsiValue.Text = "--";
            // 
            // macdLabel
            // 
            this.macdLabel.AutoSize = true;
            this.macdLabel.Dock = DockStyle.Fill;
            this.macdLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.macdLabel.Name = "macdLabel";
            this.macdLabel.Size = new Size(94, 20);
            this.macdLabel.TabIndex = 2;
            this.macdLabel.Text = "MACD";
            // 
            // macdValue
            // 
            this.macdValue.AutoSize = true;
            this.macdValue.Dock = DockStyle.Fill;
            this.macdValue.TextAlign = ContentAlignment.MiddleRight;
            this.macdValue.Name = "macdValue";
            this.macdValue.Size = new Size(95, 20);
            this.macdValue.TabIndex = 3;
            this.macdValue.Text = "--";
            // 
            // emaLabel
            // 
            this.emaLabel.AutoSize = true;
            this.emaLabel.Dock = DockStyle.Fill;
            this.emaLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.emaLabel.Name = "emaLabel";
            this.emaLabel.Size = new Size(94, 20);
            this.emaLabel.TabIndex = 4;
            this.emaLabel.Text = "EMA";
            // 
            // emaValue
            // 
            this.emaValue.AutoSize = true;
            this.emaValue.Dock = DockStyle.Fill;
            this.emaValue.TextAlign = ContentAlignment.MiddleRight;
            this.emaValue.Name = "emaValue";
            this.emaValue.Size = new Size(95, 20);
            this.emaValue.TabIndex = 5;
            this.emaValue.Text = "--";
            // 
            // bollingerLabel
            // 
            this.bollingerLabel.AutoSize = true;
            this.bollingerLabel.Dock = DockStyle.Fill;
            this.bollingerLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.bollingerLabel.Name = "bollingerLabel";
            this.bollingerLabel.Size = new Size(94, 20);
            this.bollingerLabel.TabIndex = 6;
            this.bollingerLabel.Text = "Bollinger Bands";
            // 
            // bollingerValue
            // 
            this.bollingerValue.AutoSize = true;
            this.bollingerValue.Dock = DockStyle.Fill;
            this.bollingerValue.TextAlign = ContentAlignment.MiddleRight;
            this.bollingerValue.Name = "bollingerValue";
            this.bollingerValue.Size = new Size(95, 20);
            this.bollingerValue.TabIndex = 7;
            this.bollingerValue.Text = "--";
            // 
            // atrLabel
            // 
            this.atrLabel.AutoSize = true;
            this.atrLabel.Dock = DockStyle.Fill;
            this.atrLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.atrLabel.Name = "atrLabel";
            this.atrLabel.Size = new Size(94, 20);
            this.atrLabel.TabIndex = 8;
            this.atrLabel.Text = "ATR";
            // 
            // atrValue
            // 
            this.atrValue.AutoSize = true;
            this.atrValue.Dock = DockStyle.Fill;
            this.atrValue.TextAlign = ContentAlignment.MiddleRight;
            this.atrValue.Name = "atrValue";
            this.atrValue.Size = new Size(95, 20);
            this.atrValue.TabIndex = 9;
            this.atrValue.Text = "--";
            // 
            // vwapLabel
            // 
            this.vwapLabel.AutoSize = true;
            this.vwapLabel.Dock = DockStyle.Fill;
            this.vwapLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.vwapLabel.Name = "vwapLabel";
            this.vwapLabel.Size = new Size(94, 20);
            this.vwapLabel.TabIndex = 10;
            this.vwapLabel.Text = "VWAP";
            // 
            // vwapValue
            // 
            this.vwapValue.AutoSize = true;
            this.vwapValue.Dock = DockStyle.Fill;
            this.vwapValue.TextAlign = ContentAlignment.MiddleRight;
            this.vwapValue.Name = "vwapValue";
            this.vwapValue.Size = new Size(95, 20);
            this.vwapValue.TabIndex = 11;
            this.vwapValue.Text = "--";
            // 
            // stochasticLabel
            // 
            this.stochasticLabel.AutoSize = true;
            this.stochasticLabel.Dock = DockStyle.Fill;
            this.stochasticLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.stochasticLabel.Name = "stochasticLabel";
            this.stochasticLabel.Size = new Size(94, 20);
            this.stochasticLabel.TabIndex = 12;
            this.stochasticLabel.Text = "Stochastic Oscillator";
            // 
            // stochasticValue
            // 
            this.stochasticValue.AutoSize = true;
            this.stochasticValue.Dock = DockStyle.Fill;
            this.stochasticValue.TextAlign = ContentAlignment.MiddleRight;
            this.stochasticValue.Name = "stochasticValue";
            this.stochasticValue.Size = new Size(95, 20);
            this.stochasticValue.TabIndex = 13;
            this.stochasticValue.Text = "--";
            // 
            // obvLabel
            // 
            this.obvLabel.AutoSize = true;
            this.obvLabel.Dock = DockStyle.Fill;
            this.obvLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.obvLabel.Name = "obvLabel";
            this.obvLabel.Size = new Size(94, 20);
            this.obvLabel.TabIndex = 14;
            this.obvLabel.Text = "OBV";
            // 
            // obvValue
            // 
            this.obvValue.AutoSize = true;
            this.obvValue.Dock = DockStyle.Fill;
            this.obvValue.TextAlign = ContentAlignment.MiddleRight;
            this.obvValue.Name = "obvValue";
            this.obvValue.Size = new Size(95, 20);
            this.obvValue.TabIndex = 15;
            this.obvValue.Text = "--";
            // 
            // rightColumn
            // 
            this.rightColumn.AutoSize = true;
            this.rightColumn.ColumnCount = 1;
            this.rightColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.rightColumn.Controls.Add(this.otherInfoGrid, 0, 0);
            this.rightColumn.Controls.Add(this.flowMomentumGrid, 0, 1);
            this.rightColumn.Controls.Add(this.contextGrid, 0, 2);
            this.rightColumn.Dock = DockStyle.Fill;
            this.rightColumn.Location = new Point(192, 0);
            this.rightColumn.Name = "rightColumn";
            this.rightColumn.RowCount = 3;
            this.rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.rightColumn.Size = new Size(187, 232);
            this.rightColumn.TabIndex = 1;
            // 
            // otherInfoGrid
            // 
            this.otherInfoGrid.AutoSize = true;
            this.otherInfoGrid.ColumnCount = 2;
            this.otherInfoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.otherInfoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.otherInfoGrid.Controls.Add(this.titleLabel, 0, 0);
            this.otherInfoGrid.Controls.Add(this.titleValue, 1, 0);
            this.otherInfoGrid.Controls.Add(this.subtitleLabel, 0, 1);
            this.otherInfoGrid.Controls.Add(this.subtitleValue, 1, 1);
            this.otherInfoGrid.Controls.Add(this.marketTypeLabel, 0, 2);
            this.otherInfoGrid.Controls.Add(this.marketTypeValue, 1, 2);
            this.otherInfoGrid.Controls.Add(this.priceGoodBadLabel, 0, 3);
            this.otherInfoGrid.Controls.Add(this.priceGoodBadValue, 1, 3);
            this.otherInfoGrid.Controls.Add(this.marketBehaviorLabel, 0, 4);
            this.otherInfoGrid.Controls.Add(this.marketBehaviorValue, 1, 4);
            this.otherInfoGrid.Controls.Add(this.timeLeftLabel, 0, 5);
            this.otherInfoGrid.Controls.Add(this.timeLeftValue, 1, 5);
            this.otherInfoGrid.Controls.Add(this.marketAgeLabel, 0, 6);
            this.otherInfoGrid.Controls.Add(this.marketAgeValue, 1, 6);
            this.otherInfoGrid.Dock = DockStyle.Top;
            this.otherInfoGrid.Location = new Point(0, 0);
            this.otherInfoGrid.Name = "otherInfoGrid";
            this.otherInfoGrid.RowCount = 7;
            this.otherInfoGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.otherInfoGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.otherInfoGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.otherInfoGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.otherInfoGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.otherInfoGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.otherInfoGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.otherInfoGrid.Size = new Size(187, 140);
            this.otherInfoGrid.TabIndex = 0;
            // 
            // titleLabel
            // 
            this.titleLabel.AutoSize = true;
            this.titleLabel.Dock = DockStyle.Fill;
            this.titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new Size(93, 20);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "Title";
            // 
            // titleValue
            // 
            this.titleValue.AutoSize = true;
            this.titleValue.Dock = DockStyle.Fill;
            this.titleValue.TextAlign = ContentAlignment.MiddleRight;
            this.titleValue.Name = "titleValue";
            this.titleValue.Size = new Size(94, 20);
            this.titleValue.TabIndex = 1;
            this.titleValue.Text = "--";
            // 
            // subtitleLabel
            // 
            this.subtitleLabel.AutoSize = true;
            this.subtitleLabel.Dock = DockStyle.Fill;
            this.subtitleLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.subtitleLabel.Name = "subtitleLabel";
            this.subtitleLabel.Size = new Size(93, 20);
            this.subtitleLabel.TabIndex = 2;
            this.subtitleLabel.Text = "Subtitle";
            // 
            // subtitleValue
            // 
            this.subtitleValue.AutoSize = true;
            this.subtitleValue.Dock = DockStyle.Fill;
            this.subtitleValue.TextAlign = ContentAlignment.MiddleRight;
            this.subtitleValue.Name = "subtitleValue";
            this.subtitleValue.Size = new Size(94, 20);
            this.subtitleValue.TabIndex = 3;
            this.subtitleValue.Text = "--";
            // 
            // marketTypeLabel
            // 
            this.marketTypeLabel.AutoSize = true;
            this.marketTypeLabel.Dock = DockStyle.Fill;
            this.marketTypeLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.marketTypeLabel.Name = "marketTypeLabel";
            this.marketTypeLabel.Size = new Size(93, 20);
            this.marketTypeLabel.TabIndex = 4;
            this.marketTypeLabel.Text = "Market Type";
            // 
            // marketTypeValue
            // 
            this.marketTypeValue.AutoSize = true;
            this.marketTypeValue.Dock = DockStyle.Fill;
            this.marketTypeValue.TextAlign = ContentAlignment.MiddleRight;
            this.marketTypeValue.Name = "marketTypeValue";
            this.marketTypeValue.Size = new Size(94, 20);
            this.marketTypeValue.TabIndex = 5;
            this.marketTypeValue.Text = "--";
            // 
            // priceGoodBadLabel
            // 
            this.priceGoodBadLabel.AutoSize = true;
            this.priceGoodBadLabel.Dock = DockStyle.Fill;
            this.priceGoodBadLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.priceGoodBadLabel.Name = "priceGoodBadLabel";
            this.priceGoodBadLabel.Size = new Size(93, 20);
            this.priceGoodBadLabel.TabIndex = 6;
            this.priceGoodBadLabel.Text = "Price Good/Bad";
            // 
            // priceGoodBadValue
            // 
            this.priceGoodBadValue.AutoSize = true;
            this.priceGoodBadValue.Dock = DockStyle.Fill;
            this.priceGoodBadValue.TextAlign = ContentAlignment.MiddleRight;
            this.priceGoodBadValue.Name = "priceGoodBadValue";
            this.priceGoodBadValue.Size = new Size(94, 20);
            this.priceGoodBadValue.TabIndex = 7;
            this.priceGoodBadValue.Text = "--";
            // 
            // marketBehaviorLabel
            // 
            this.marketBehaviorLabel.AutoSize = true;
            this.marketBehaviorLabel.Dock = DockStyle.Fill;
            this.marketBehaviorLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.marketBehaviorLabel.Name = "marketBehaviorLabel";
            this.marketBehaviorLabel.Size = new Size(93, 20);
            this.marketBehaviorLabel.TabIndex = 8;
            this.marketBehaviorLabel.Text = "Market Behavior";
            // 
            // marketBehaviorValue
            // 
            this.marketBehaviorValue.AutoSize = true;
            this.marketBehaviorValue.Dock = DockStyle.Fill;
            this.marketBehaviorValue.TextAlign = ContentAlignment.MiddleRight;
            this.marketBehaviorValue.Name = "marketBehaviorValue";
            this.marketBehaviorValue.Size = new Size(94, 20);
            this.marketBehaviorValue.TabIndex = 9;
            this.marketBehaviorValue.Text = "--";
            // 
            // timeLeftLabel
            // 
            this.timeLeftLabel.AutoSize = true;
            this.timeLeftLabel.Dock = DockStyle.Fill;
            this.timeLeftLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.timeLeftLabel.Name = "timeLeftLabel";
            this.timeLeftLabel.Size = new Size(93, 20);
            this.timeLeftLabel.TabIndex = 10;
            this.timeLeftLabel.Text = "Time Left";
            // 
            // timeLeftValue
            // 
            this.timeLeftValue.AutoSize = true;
            this.timeLeftValue.Dock = DockStyle.Fill;
            this.timeLeftValue.TextAlign = ContentAlignment.MiddleRight;
            this.timeLeftValue.Name = "timeLeftValue";
            this.timeLeftValue.Size = new Size(94, 20);
            this.timeLeftValue.TabIndex = 11;
            this.timeLeftValue.Text = "--";
            // 
            // marketAgeLabel
            // 
            this.marketAgeLabel.AutoSize = true;
            this.marketAgeLabel.Dock = DockStyle.Fill;
            this.marketAgeLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.marketAgeLabel.Name = "marketAgeLabel";
            this.marketAgeLabel.Size = new Size(93, 20);
            this.marketAgeLabel.TabIndex = 12;
            this.marketAgeLabel.Text = "Market Age";
            // 
            // marketAgeValue
            // 
            this.marketAgeValue.AutoSize = true;
            this.marketAgeValue.Dock = DockStyle.Fill;
            this.marketAgeValue.TextAlign = ContentAlignment.MiddleRight;
            this.marketAgeValue.Name = "marketAgeValue";
            this.marketAgeValue.Size = new Size(94, 20);
            this.marketAgeValue.TabIndex = 13;
            this.marketAgeValue.Text = "--";
            // 
            // flowMomentumGrid
            // 
            this.flowMomentumGrid.AutoSize = true;
            this.flowMomentumGrid.ColumnCount = 2;
            this.flowMomentumGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.flowMomentumGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.flowMomentumGrid.Controls.Add(this.flowHeader, 0, 0);
            this.flowMomentumGrid.Controls.Add(this.topVelocityLabel, 0, 1);
            this.flowMomentumGrid.Controls.Add(this.topVelocityValue, 1, 1);
            this.flowMomentumGrid.Controls.Add(this.bottomVelocityLabel, 0, 2);
            this.flowMomentumGrid.Controls.Add(this.bottomVelocityValue, 1, 2);
            this.flowMomentumGrid.Controls.Add(this.netOrderRateLabel, 0, 3);
            this.flowMomentumGrid.Controls.Add(this.netOrderRateValue, 1, 3);
            this.flowMomentumGrid.Controls.Add(this.tradeVolumeLabel, 0, 4);
            this.flowMomentumGrid.Controls.Add(this.tradeVolumeValue, 1, 4);
            this.flowMomentumGrid.Controls.Add(this.avgTradeSizeLabel, 0, 5);
            this.flowMomentumGrid.Controls.Add(this.avgTradeSizeValue, 1, 5);
            this.flowMomentumGrid.Dock = DockStyle.Top;
            this.flowMomentumGrid.Location = new Point(0, 140);
            this.flowMomentumGrid.Name = "flowMomentumGrid";
            this.flowMomentumGrid.RowCount = 6;
            this.flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.flowMomentumGrid.Size = new Size(187, 120);
            this.flowMomentumGrid.TabIndex = 1;
            // 
            // flowHeader
            // 
            this.flowHeader.AutoSize = true;
            this.flowHeader.Dock = DockStyle.Fill;
            this.flowHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.flowHeader.Name = "flowHeader";
            this.flowHeader.Size = new Size(187, 20);
            this.flowHeader.TabIndex = 0;
            this.flowHeader.Text = "Flow & Momentum";
            this.flowMomentumGrid.SetColumnSpan(this.flowHeader, 2);
            // 
            // topVelocityLabel
            // 
            this.topVelocityLabel.AutoSize = true;
            this.topVelocityLabel.Dock = DockStyle.Fill;
            this.topVelocityLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.topVelocityLabel.Name = "topVelocityLabel";
            this.topVelocityLabel.Size = new Size(93, 20);
            this.topVelocityLabel.TabIndex = 0;
            this.topVelocityLabel.Text = "Top Velocity";
            // 
            // topVelocityValue
            // 
            this.topVelocityValue.AutoSize = true;
            this.topVelocityValue.Dock = DockStyle.Fill;
            this.topVelocityValue.TextAlign = ContentAlignment.MiddleRight;
            this.topVelocityValue.Name = "topVelocityValue";
            this.topVelocityValue.Size = new Size(94, 20);
            this.topVelocityValue.TabIndex = 1;
            this.topVelocityValue.Text = "-- (--)";
            // 
            // bottomVelocityLabel
            // 
            this.bottomVelocityLabel.AutoSize = true;
            this.bottomVelocityLabel.Dock = DockStyle.Fill;
            this.bottomVelocityLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.bottomVelocityLabel.Name = "bottomVelocityLabel";
            this.bottomVelocityLabel.Size = new Size(93, 20);
            this.bottomVelocityLabel.TabIndex = 2;
            this.bottomVelocityLabel.Text = "Bottom Velocity";
            // 
            // bottomVelocityValue
            // 
            this.bottomVelocityValue.AutoSize = true;
            this.bottomVelocityValue.Dock = DockStyle.Fill;
            this.bottomVelocityValue.TextAlign = ContentAlignment.MiddleRight;
            this.bottomVelocityValue.Name = "bottomVelocityValue";
            this.bottomVelocityValue.Size = new Size(94, 20);
            this.bottomVelocityValue.TabIndex = 3;
            this.bottomVelocityValue.Text = "-- (--)";
            // 
            // netOrderRateLabel
            // 
            this.netOrderRateLabel.AutoSize = true;
            this.netOrderRateLabel.Dock = DockStyle.Fill;
            this.netOrderRateLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.netOrderRateLabel.Name = "netOrderRateLabel";
            this.netOrderRateLabel.Size = new Size(93, 20);
            this.netOrderRateLabel.TabIndex = 4;
            this.netOrderRateLabel.Text = "Net Order Rate";
            // 
            // netOrderRateValue
            // 
            this.netOrderRateValue.AutoSize = true;
            this.netOrderRateValue.Dock = DockStyle.Fill;
            this.netOrderRateValue.TextAlign = ContentAlignment.MiddleRight;
            this.netOrderRateValue.Name = "netOrderRateValue";
            this.netOrderRateValue.Size = new Size(94, 20);
            this.netOrderRateValue.TabIndex = 5;
            this.netOrderRateValue.Text = "-- (--)";
            // 
            // tradeVolumeLabel
            // 
            this.tradeVolumeLabel.AutoSize = true;
            this.tradeVolumeLabel.Dock = DockStyle.Fill;
            this.tradeVolumeLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.tradeVolumeLabel.Name = "tradeVolumeLabel";
            this.tradeVolumeLabel.Size = new Size(93, 20);
            this.tradeVolumeLabel.TabIndex = 6;
            this.tradeVolumeLabel.Text = "Trade Volume";
            // 
            // tradeVolumeValue
            // 
            this.tradeVolumeValue.AutoSize = true;
            this.tradeVolumeValue.Dock = DockStyle.Fill;
            this.tradeVolumeValue.TextAlign = ContentAlignment.MiddleRight;
            this.tradeVolumeValue.Name = "tradeVolumeValue";
            this.tradeVolumeValue.Size = new Size(94, 20);
            this.tradeVolumeValue.TabIndex = 7;
            this.tradeVolumeValue.Text = "-- (--)";
            // 
            // avgTradeSizeLabel
            // 
            this.avgTradeSizeLabel.AutoSize = true;
            this.avgTradeSizeLabel.Dock = DockStyle.Fill;
            this.avgTradeSizeLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.avgTradeSizeLabel.Name = "avgTradeSizeLabel";
            this.avgTradeSizeLabel.Size = new Size(93, 20);
            this.avgTradeSizeLabel.TabIndex = 8;
            this.avgTradeSizeLabel.Text = "Average Trade Size";
            // 
            // avgTradeSizeValue
            // 
            this.avgTradeSizeValue.AutoSize = true;
            this.avgTradeSizeValue.Dock = DockStyle.Fill;
            this.avgTradeSizeValue.TextAlign = ContentAlignment.MiddleRight;
            this.avgTradeSizeValue.Name = "avgTradeSizeValue";
            this.avgTradeSizeValue.Size = new Size(94, 20);
            this.avgTradeSizeValue.TabIndex = 9;
            this.avgTradeSizeValue.Text = "-- (--)";
            // 
            // contextGrid
            // 
            this.contextGrid.AutoSize = true;
            this.contextGrid.ColumnCount = 2;
            this.contextGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.contextGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.contextGrid.Controls.Add(this.contextHeader, 0, 0);
            this.contextGrid.Controls.Add(this.spreadLabel, 0, 1);
            this.contextGrid.Controls.Add(this.spreadValue, 1, 1);
            this.contextGrid.Controls.Add(this.imbalLabel, 0, 2);
            this.contextGrid.Controls.Add(this.imbalValue, 1, 2);
            this.contextGrid.Controls.Add(this.depthTop4Label, 0, 3);
            this.contextGrid.Controls.Add(this.depthTop4Value, 1, 3);
            this.contextGrid.Controls.Add(this.centerMassLabel, 0, 4);
            this.contextGrid.Controls.Add(this.centerMassValue, 1, 4);
            this.contextGrid.Controls.Add(this.totalContractsLabel, 0, 5);
            this.contextGrid.Controls.Add(this.totalContractsValue, 1, 5);
            this.contextGrid.Dock = DockStyle.Top;
            this.contextGrid.Location = new Point(0, 260);
            this.contextGrid.Name = "contextGrid";
            this.contextGrid.RowCount = 6;
            this.contextGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.contextGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.contextGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.contextGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.contextGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.contextGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.contextGrid.Size = new Size(187, 120);
            this.contextGrid.TabIndex = 2;
            // 
            // contextHeader
            // 
            this.contextHeader.AutoSize = true;
            this.contextHeader.Dock = DockStyle.Fill;
            this.contextHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.contextHeader.Name = "contextHeader";
            this.contextHeader.Size = new Size(187, 20);
            this.contextHeader.TabIndex = 0;
            this.contextHeader.Text = "Context & Deeper Book";
            this.contextGrid.SetColumnSpan(this.contextHeader, 2);
            // 
            // spreadLabel
            // 
            this.spreadLabel.AutoSize = true;
            this.spreadLabel.Dock = DockStyle.Fill;
            this.spreadLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.spreadLabel.Name = "spreadLabel";
            this.spreadLabel.Size = new Size(93, 20);
            this.spreadLabel.TabIndex = 0;
            this.spreadLabel.Text = "Spread";
            // 
            // spreadValue
            // 
            this.spreadValue.AutoSize = true;
            this.spreadValue.Dock = DockStyle.Fill;
            this.spreadValue.TextAlign = ContentAlignment.MiddleRight;
            this.spreadValue.Name = "spreadValue";
            this.spreadValue.Size = new Size(94, 20);
            this.spreadValue.TabIndex = 1;
            this.spreadValue.Text = "--";
            // 
            // imbalLabel
            // 
            this.imbalLabel.AutoSize = true;
            this.imbalLabel.Dock = DockStyle.Fill;
            this.imbalLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.imbalLabel.Name = "imbalLabel";
            this.imbalLabel.Size = new Size(93, 20);
            this.imbalLabel.TabIndex = 2;
            this.imbalLabel.Text = "Ask/Bid Imbal (Vol)";
            // 
            // imbalValue
            // 
            this.imbalValue.AutoSize = true;
            this.imbalValue.Dock = DockStyle.Fill;
            this.imbalValue.TextAlign = ContentAlignment.MiddleRight;
            this.imbalValue.Name = "imbalValue";
            this.imbalValue.Size = new Size(94, 20);
            this.imbalValue.TabIndex = 3;
            this.imbalValue.Text = "--";
            // 
            // depthTop4Label
            // 
            this.depthTop4Label.AutoSize = true;
            this.depthTop4Label.Dock = DockStyle.Fill;
            this.depthTop4Label.TextAlign = ContentAlignment.MiddleLeft;
            this.depthTop4Label.Name = "depthTop4Label";
            this.depthTop4Label.Size = new Size(93, 20);
            this.depthTop4Label.TabIndex = 4;
            this.depthTop4Label.Text = "Depth Top 4";
            // 
            // depthTop4Value
            // 
            this.depthTop4Value.AutoSize = true;
            this.depthTop4Value.Dock = DockStyle.Fill;
            this.depthTop4Value.TextAlign = ContentAlignment.MiddleRight;
            this.depthTop4Value.Name = "depthTop4Value";
            this.depthTop4Value.Size = new Size(94, 20);
            this.depthTop4Value.TabIndex = 5;
            this.depthTop4Value.Text = "-- (--)";
            // 
            // centerMassLabel
            // 
            this.centerMassLabel.AutoSize = true;
            this.centerMassLabel.Dock = DockStyle.Fill;
            this.centerMassLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.centerMassLabel.Name = "centerMassLabel";
            this.centerMassLabel.Size = new Size(93, 20);
            this.centerMassLabel.TabIndex = 6;
            this.centerMassLabel.Text = "Center Mass";
            // 
            // centerMassValue
            // 
            this.centerMassValue.AutoSize = true;
            this.centerMassValue.Dock = DockStyle.Fill;
            this.centerMassValue.TextAlign = ContentAlignment.MiddleRight;
            this.centerMassValue.Name = "centerMassValue";
            this.centerMassValue.Size = new Size(94, 20);
            this.centerMassValue.TabIndex = 7;
            this.centerMassValue.Text = "-- (--)";
            // 
            // totalContractsLabel
            // 
            this.totalContractsLabel.AutoSize = true;
            this.totalContractsLabel.Dock = DockStyle.Fill;
            this.totalContractsLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.totalContractsLabel.Name = "totalContractsLabel";
            this.totalContractsLabel.Size = new Size(93, 20);
            this.totalContractsLabel.TabIndex = 8;
            this.totalContractsLabel.Text = "Total Contracts";
            // 
            // totalContractsValue
            // 
            this.totalContractsValue.AutoSize = true;
            this.totalContractsValue.Dock = DockStyle.Fill;
            this.totalContractsValue.TextAlign = ContentAlignment.MiddleRight;
            this.totalContractsValue.Name = "totalContractsValue";
            this.totalContractsValue.Size = new Size(94, 20);
            this.totalContractsValue.TabIndex = 9;
            this.totalContractsValue.Text = "-- (--)";
            // 
            // positionsContainer
            // 
            this.positionsContainer.BackColor = this.panelBg;
            this.positionsContainer.BorderStyle = BorderStyle.FixedSingle;
            this.positionsContainer.Controls.Add(this.strategyPanel);
            this.positionsContainer.Controls.Add(this.positionsGrid);
            this.positionsContainer.Dock = DockStyle.Fill;
            this.positionsContainer.Location = new Point(3, 253);
            this.positionsContainer.Name = "positionsContainer";
            this.positionsContainer.Padding = new Padding(5);
            this.positionsContainer.Size = new Size(391, 244);
            this.positionsContainer.TabIndex = 2;
            // 
            // strategyPanel
            // 
            this.strategyPanel.AutoSize = true;
            this.strategyPanel.Controls.Add(this.buyStrategyCombo);
            this.strategyPanel.Controls.Add(this.exitStrategyCombo);
            this.strategyPanel.Dock = DockStyle.Bottom;
            this.strategyPanel.Location = new Point(5, 200);
            this.strategyPanel.Name = "strategyPanel";
            this.strategyPanel.Size = new Size(379, 32);
            this.strategyPanel.TabIndex = 1;
            // 
            // buyStrategyCombo
            // 
            this.buyStrategyCombo.BackColor = Color.FromArgb(42, 42, 42);
            this.buyStrategyCombo.ForeColor = this.textColor;
            this.buyStrategyCombo.FormattingEnabled = true;
            this.buyStrategyCombo.Location = new Point(3, 3);
            this.buyStrategyCombo.Name = "buyStrategyCombo";
            this.buyStrategyCombo.Size = new Size(121, 21);
            this.buyStrategyCombo.TabIndex = 0;
            this.buyStrategyCombo.Text = "Buy Strategy";
            // 
            // exitStrategyCombo
            // 
            this.exitStrategyCombo.BackColor = Color.FromArgb(42, 42, 42);
            this.exitStrategyCombo.ForeColor = this.textColor;
            this.exitStrategyCombo.FormattingEnabled = true;
            this.exitStrategyCombo.Location = new Point(130, 3);
            this.exitStrategyCombo.Name = "exitStrategyCombo";
            this.exitStrategyCombo.Size = new Size(121, 21);
            this.exitStrategyCombo.TabIndex = 1;
            this.exitStrategyCombo.Text = "Exit Strategy";
            // 
            // positionsGrid
            // 
            this.positionsGrid.AutoSize = true;
            this.positionsGrid.ColumnCount = 4;
            this.positionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            this.positionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.positionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            this.positionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.positionsGrid.Controls.Add(this.positionSizeLabel, 0, 0);
            this.positionsGrid.Controls.Add(this.positionSizeValue, 1, 0);
            this.positionsGrid.Controls.Add(this.lastTradeLabel, 2, 0);
            this.positionsGrid.Controls.Add(this.lastTradeValue, 3, 0);
            this.positionsGrid.Controls.Add(this.positionRoiLabel, 0, 1);
            this.positionsGrid.Controls.Add(this.positionRoiValue, 1, 1);
            this.positionsGrid.Controls.Add(this.buyinPriceLabel, 2, 1);
            this.positionsGrid.Controls.Add(this.buyinPriceValue, 3, 1);
            this.positionsGrid.Controls.Add(this.positionUpsideLabel, 0, 2);
            this.positionsGrid.Controls.Add(this.positionUpsideValue, 1, 2);
            this.positionsGrid.Controls.Add(this.positionDownsideLabel, 2, 2);
            this.positionsGrid.Controls.Add(this.positionDownsideValue, 3, 2);
            this.positionsGrid.Controls.Add(this.restingOrdersLabel, 0, 3);
            this.positionsGrid.Controls.Add(this.restingOrdersValue, 1, 3);
            this.positionsGrid.Dock = DockStyle.Fill;
            this.positionsGrid.Location = new Point(5, 5);
            this.positionsGrid.Name = "positionsGrid";
            this.positionsGrid.RowCount = 4;
            this.positionsGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.positionsGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.positionsGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.positionsGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.positionsGrid.Size = new Size(379, 80);
            this.positionsGrid.TabIndex = 0;
            // 
            // positionSizeLabel
            // 
            this.positionSizeLabel.AutoSize = true;
            this.positionSizeLabel.Dock = DockStyle.Fill;
            this.positionSizeLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.positionSizeLabel.Name = "positionSizeLabel";
            this.positionSizeLabel.Size = new Size(80, 20);
            this.positionSizeLabel.TabIndex = 0;
            this.positionSizeLabel.Text = "Position Size:";
            // 
            // positionSizeValue
            // 
            this.positionSizeValue.AutoSize = true;
            this.positionSizeValue.Dock = DockStyle.Fill;
            this.positionSizeValue.TextAlign = ContentAlignment.MiddleRight;
            this.positionSizeValue.Name = "positionSizeValue";
            this.positionSizeValue.Size = new Size(109, 20);
            this.positionSizeValue.TabIndex = 1;
            this.positionSizeValue.Text = "--";
            // 
            // lastTradeLabel
            // 
            this.lastTradeLabel.AutoSize = true;
            this.lastTradeLabel.Dock = DockStyle.Fill;
            this.lastTradeLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.lastTradeLabel.Name = "lastTradeLabel";
            this.lastTradeLabel.Size = new Size(60, 20);
            this.lastTradeLabel.TabIndex = 2;
            this.lastTradeLabel.Text = "Last Trade:";
            // 
            // lastTradeValue
            // 
            this.lastTradeValue.AutoSize = true;
            this.lastTradeValue.Dock = DockStyle.Fill;
            this.lastTradeValue.TextAlign = ContentAlignment.MiddleRight;
            this.lastTradeValue.Name = "lastTradeValue";
            this.lastTradeValue.Size = new Size(130, 20);
            this.lastTradeValue.TabIndex = 3;
            this.lastTradeValue.Text = "--";
            // 
            // positionRoiLabel
            // 
            this.positionRoiLabel.AutoSize = true;
            this.positionRoiLabel.Dock = DockStyle.Fill;
            this.positionRoiLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.positionRoiLabel.Name = "positionRoiLabel";
            this.positionRoiLabel.Size = new Size(80, 20);
            this.positionRoiLabel.TabIndex = 4;
            this.positionRoiLabel.Text = "Position ROI:";
            // 
            // positionRoiValue
            // 
            this.positionRoiValue.AutoSize = true;
            this.positionRoiValue.Dock = DockStyle.Fill;
            this.positionRoiValue.TextAlign = ContentAlignment.MiddleRight;
            this.positionRoiValue.Name = "positionRoiValue";
            this.positionRoiValue.Size = new Size(109, 20);
            this.positionRoiValue.TabIndex = 5;
            this.positionRoiValue.Text = "--";
            // 
            // buyinPriceLabel
            // 
            this.buyinPriceLabel.AutoSize = true;
            this.buyinPriceLabel.Dock = DockStyle.Fill;
            this.buyinPriceLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.buyinPriceLabel.Name = "buyinPriceLabel";
            this.buyinPriceLabel.Size = new Size(60, 20);
            this.buyinPriceLabel.TabIndex = 6;
            this.buyinPriceLabel.Text = "Buyin Price:";
            // 
            // buyinPriceValue
            // 
            this.buyinPriceValue.AutoSize = true;
            this.buyinPriceValue.Dock = DockStyle.Fill;
            this.buyinPriceValue.TextAlign = ContentAlignment.MiddleRight;
            this.buyinPriceValue.Name = "buyinPriceValue";
            this.buyinPriceValue.Size = new Size(130, 20);
            this.buyinPriceValue.TabIndex = 7;
            this.buyinPriceValue.Text = "--";
            // 
            // positionUpsideLabel
            // 
            this.positionUpsideLabel.AutoSize = true;
            this.positionUpsideLabel.Dock = DockStyle.Fill;
            this.positionUpsideLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.positionUpsideLabel.Name = "positionUpsideLabel";
            this.positionUpsideLabel.Size = new Size(80, 20);
            this.positionUpsideLabel.TabIndex = 8;
            this.positionUpsideLabel.Text = "Position Upside:";
            // 
            // positionUpsideValue
            // 
            this.positionUpsideValue.AutoSize = true;
            this.positionUpsideValue.ForeColor = this.profitColor;
            this.positionUpsideValue.Dock = DockStyle.Fill;
            this.positionUpsideValue.TextAlign = ContentAlignment.MiddleRight;
            this.positionUpsideValue.Name = "positionUpsideValue";
            this.positionUpsideValue.Size = new Size(109, 20);
            this.positionUpsideValue.TabIndex = 9;
            this.positionUpsideValue.Text = "--";
            // 
            // positionDownsideLabel
            // 
            this.positionDownsideLabel.AutoSize = true;
            this.positionDownsideLabel.Dock = DockStyle.Fill;
            this.positionDownsideLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.positionDownsideLabel.Name = "positionDownsideLabel";
            this.positionDownsideLabel.Size = new Size(60, 20);
            this.positionDownsideLabel.TabIndex = 10;
            this.positionDownsideLabel.Text = "Position Downside:";
            // 
            // positionDownsideValue
            // 
            this.positionDownsideValue.AutoSize = true;
            this.positionDownsideValue.ForeColor = this.lossColor;
            this.positionDownsideValue.Dock = DockStyle.Fill;
            this.positionDownsideValue.TextAlign = ContentAlignment.MiddleRight;
            this.positionDownsideValue.Name = "positionDownsideValue";
            this.positionDownsideValue.Size = new Size(130, 20);
            this.positionDownsideValue.TabIndex = 11;
            this.positionDownsideValue.Text = "--";
            // 
            // restingOrdersLabel
            // 
            this.restingOrdersLabel.AutoSize = true;
            this.restingOrdersLabel.Dock = DockStyle.Fill;
            this.restingOrdersLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.restingOrdersLabel.Name = "restingOrdersLabel";
            this.restingOrdersLabel.Size = new Size(80, 20);
            this.restingOrdersLabel.TabIndex = 12;
            this.restingOrdersLabel.Text = "Resting Orders:";
            // 
            // restingOrdersValue
            // 
            this.restingOrdersValue.AutoSize = true;
            this.restingOrdersValue.Dock = DockStyle.Fill;
            this.restingOrdersValue.TextAlign = ContentAlignment.MiddleRight;
            this.restingOrdersValue.Name = "restingOrdersValue";
            this.restingOrdersValue.Size = new Size(109, 20);
            this.restingOrdersValue.TabIndex = 13;
            this.restingOrdersValue.Text = "--";
            // 
            // orderbookContainer
            // 
            this.orderbookContainer.BackColor = this.panelBg;
            this.orderbookContainer.BorderStyle = BorderStyle.FixedSingle;
            this.orderbookContainer.Controls.Add(this.orderbookGrid);
            this.orderbookContainer.Dock = DockStyle.Fill;
            this.orderbookContainer.Location = new Point(400, 253);
            this.orderbookContainer.Name = "orderbookContainer";
            this.orderbookContainer.Padding = new Padding(5);
            this.orderbookContainer.Size = new Size(391, 244);
            this.orderbookContainer.TabIndex = 3;
            // 
            // orderbookGrid
            // 
            this.orderbookGrid.AllowUserToAddRows = false;
            this.orderbookGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.orderbookGrid.BackgroundColor = this.panelBg;
            this.orderbookGrid.BorderStyle = BorderStyle.None;
            this.orderbookGrid.Columns.AddRange(new DataGridViewColumn[] {
            this.priceCol,
            this.sizeCol,
            this.valueCol});
            this.orderbookGrid.Dock = DockStyle.Fill;
            this.orderbookGrid.ForeColor = this.textColor;
            this.orderbookGrid.Location = new Point(5, 5);
            this.orderbookGrid.Name = "orderbookGrid";
            this.orderbookGrid.ReadOnly = true;
            this.orderbookGrid.RowHeadersVisible = false;
            this.orderbookGrid.Size = new Size(379, 232);
            this.orderbookGrid.TabIndex = 0;
            this.orderbookGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(42, 42, 42);
            this.orderbookGrid.ColumnHeadersDefaultCellStyle.ForeColor = this.textColor;
            // 
            // priceCol
            // 
            this.priceCol.HeaderText = "Price";
            this.priceCol.Name = "priceCol";
            this.priceCol.ReadOnly = true;
            // 
            // sizeCol
            // 
            this.sizeCol.HeaderText = "Size";
            this.sizeCol.Name = "sizeCol";
            this.sizeCol.ReadOnly = true;
            // 
            // valueCol
            // 
            this.valueCol.HeaderText = "Value";
            this.valueCol.Name = "valueCol";
            this.valueCol.ReadOnly = true;
            // 
            // backButton
            // 
            this.backButton.BackColor = this.askColor;
            this.backButton.Dock = DockStyle.Fill;
            this.backButton.ForeColor = Color.White;
            this.backButton.Location = new Point(3, 562);
            this.backButton.Name = "backButton";
            this.backButton.Size = new Size(794, 35);
            this.backButton.TabIndex = 2;
            this.backButton.Text = "Back to Full Chart";
            this.backButton.UseVisualStyleBackColor = false;
            // 
            // MarketDashboardControl2
            // 
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Controls.Add(this.mainLayout);
            this.Name = "MarketDashboardControl2";
            this.Size = new Size(800, 600);
            this.mainLayout.ResumeLayout(false);
            this.mainLayout.PerformLayout();
            this.cashPositionsPanel.ResumeLayout(false);
            this.cashPositionsPanel.PerformLayout();
            this.balanceBox.ResumeLayout(false);
            this.balanceLayout.ResumeLayout(false);
            this.balanceLayout.PerformLayout();
            this.positionBox.ResumeLayout(false);
            this.positionLayout.ResumeLayout(false);
            this.positionLayout.PerformLayout();
            this.lastWebSocketBox.ResumeLayout(false);
            this.lastWebSocketLayout.ResumeLayout(false);
            this.lastWebSocketLayout.PerformLayout();
            this.exchangeStatusBox.ResumeLayout(false);
            this.exchangeStatusLayout.ResumeLayout(false);
            this.exchangeStatusLayout.PerformLayout();
            this.tradingStatusBox.ResumeLayout(false);
            this.tradingStatusLayout.ResumeLayout(false);
            this.tradingStatusLayout.PerformLayout();
            this.dashboardGrid.ResumeLayout(false);
            this.chartContainer.ResumeLayout(false);
            this.chartLayout.ResumeLayout(false);
            this.chartLayout.PerformLayout();
            this.chartControls.ResumeLayout(false);
            this.chartControls.PerformLayout();
            this.marketInfoContainer.ResumeLayout(false);
            this.infoGrid.ResumeLayout(false);
            this.leftColumn.ResumeLayout(false);
            this.leftColumn.PerformLayout();
            this.pricesGrid.ResumeLayout(false);
            this.pricesGrid.PerformLayout();
            this.allTimeHighAsk.ResumeLayout(false);
            this.allTimeHighAsk.PerformLayout();
            this.allTimeHighBid.ResumeLayout(false);
            this.allTimeHighBid.PerformLayout();
            this.recentHighAsk.ResumeLayout(false);
            this.recentHighAsk.PerformLayout();
            this.recentHighBid.ResumeLayout(false);
            this.recentHighBid.PerformLayout();
            this.recentLowAsk.ResumeLayout(false);
            this.recentLowAsk.PerformLayout();
            this.recentLowBid.ResumeLayout(false);
            this.recentLowBid.PerformLayout();
            this.allTimeLowAsk.ResumeLayout(false);
            this.allTimeLowAsk.PerformLayout();
            this.allTimeLowBid.ResumeLayout(false);
            this.allTimeLowBid.PerformLayout();
            this.tradingMetricsGrid.ResumeLayout(false);
            this.tradingMetricsGrid.PerformLayout();
            this.rightColumn.ResumeLayout(false);
            this.rightColumn.PerformLayout();
            this.otherInfoGrid.ResumeLayout(false);
            this.otherInfoGrid.PerformLayout();
            this.flowMomentumGrid.ResumeLayout(false);
            this.flowMomentumGrid.PerformLayout();
            this.contextGrid.ResumeLayout(false);
            this.contextGrid.PerformLayout();
            this.positionsContainer.ResumeLayout(false);
            this.positionsContainer.PerformLayout();
            this.strategyPanel.ResumeLayout(false);
            this.positionsGrid.ResumeLayout(false);
            this.positionsGrid.PerformLayout();
            this.orderbookContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.orderbookGrid)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private TableLayoutPanel mainLayout;
        private FlowLayoutPanel cashPositionsPanel;
        private Panel balanceBox;
        private TableLayoutPanel balanceLayout;
        private Label balanceLabel;
        private Label balanceValue;
        private Panel positionBox;
        private TableLayoutPanel positionLayout;
        private Label positionLabel;
        private Label positionValue;
        private Panel lastWebSocketBox;
        private TableLayoutPanel lastWebSocketLayout;
        private Label lastWebSocketLabel;
        private Label lastWebSocketValue;
        private Panel exchangeStatusBox;
        private TableLayoutPanel exchangeStatusLayout;
        private Label exchangeStatusLabel;
        private Label exchangeStatusValue;
        private Panel tradingStatusBox;
        private TableLayoutPanel tradingStatusLayout;
        private Label tradingStatusLabel;
        private Label tradingStatusValue;
        private TableLayoutPanel dashboardGrid;
        private Panel chartContainer;
        private TableLayoutPanel chartLayout;
        private FlowLayoutPanel chartControls;
        private Label chartHeader;
        private ComboBox timeframeCombo;
        private FormsPlot priceChart;
        private Panel marketInfoContainer;
        private TableLayoutPanel infoGrid;
        private TableLayoutPanel leftColumn;
        private TableLayoutPanel pricesGrid;
        private Label allTimeHighLabel;
        private Panel allTimeHighAsk;
        private Label allTimeHighAskPrice;
        private Label allTimeHighAskTime;
        private Panel allTimeHighBid;
        private Label allTimeHighBidPrice;
        private Label allTimeHighBidTime;
        private Label recentHighLabel;
        private Panel recentHighAsk;
        private Label recentHighAskPrice;
        private Label recentHighAskTime;
        private Panel recentHighBid;
        private Label recentHighBidPrice;
        private Label recentHighBidTime;
        private Label currentPriceLabel;
        private Label currentPriceAsk;
        private Label currentPriceBid;
        private Label recentLowLabel;
        private Panel recentLowAsk;
        private Label recentLowAskPrice;
        private Label recentLowAskTime;
        private Panel recentLowBid;
        private Label recentLowBidPrice;
        private Label recentLowBidTime;
        private Label allTimeLowLabel;
        private Panel allTimeLowAsk;
        private Label allTimeLowAskPrice;
        private Label allTimeLowAskTime;
        private Panel allTimeLowBid;
        private Label allTimeLowBidPrice;
        private Label allTimeLowBidTime;
        private TableLayoutPanel tradingMetricsGrid;
        private Label tradingMetricsHeader;
        private Label rsiLabel;
        private Label rsiValue;
        private Label macdLabel;
        private Label macdValue;
        private Label emaLabel;
        private Label emaValue;
        private Label bollingerLabel;
        private Label bollingerValue;
        private Label atrLabel;
        private Label atrValue;
        private Label vwapLabel;
        private Label vwapValue;
        private Label stochasticLabel;
        private Label stochasticValue;
        private Label obvLabel;
        private Label obvValue;
        private TableLayoutPanel rightColumn;
        private TableLayoutPanel otherInfoGrid;
        private Label titleLabel;
        private Label titleValue;
        private Label subtitleLabel;
        private Label subtitleValue;
        private Label marketTypeLabel;
        private Label marketTypeValue;
        private Label priceGoodBadLabel;
        private Label priceGoodBadValue;
        private Label marketBehaviorLabel;
        private Label marketBehaviorValue;
        private Label timeLeftLabel;
        private Label timeLeftValue;
        private Label marketAgeLabel;
        private Label marketAgeValue;
        private TableLayoutPanel flowMomentumGrid;
        private Label flowHeader;
        private Label topVelocityLabel;
        private Label topVelocityValue;
        private Label bottomVelocityLabel;
        private Label bottomVelocityValue;
        private Label netOrderRateLabel;
        private Label netOrderRateValue;
        private Label tradeVolumeLabel;
        private Label tradeVolumeValue;
        private Label avgTradeSizeLabel;
        private Label avgTradeSizeValue;
        private TableLayoutPanel contextGrid;
        private Label contextHeader;
        private Label spreadLabel;
        private Label spreadValue;
        private Label imbalLabel;
        private Label imbalValue;
        private Label depthTop4Label;
        private Label depthTop4Value;
        private Label centerMassLabel;
        private Label centerMassValue;
        private Label totalContractsLabel;
        private Label totalContractsValue;
        private Panel positionsContainer;
        private FlowLayoutPanel strategyPanel;
        private ComboBox buyStrategyCombo;
        private ComboBox exitStrategyCombo;
        private TableLayoutPanel positionsGrid;
        private Label positionSizeLabel;
        private Label positionSizeValue;
        private Label lastTradeLabel;
        private Label lastTradeValue;
        private Label positionRoiLabel;
        private Label positionRoiValue;
        private Label buyinPriceLabel;
        private Label buyinPriceValue;
        private Label positionUpsideLabel;
        private Label positionUpsideValue;
        private Label positionDownsideLabel;
        private Label positionDownsideValue;
        private Label restingOrdersLabel;
        private Label restingOrdersValue;
        private Panel orderbookContainer;
        private DataGridView orderbookGrid;
        private DataGridViewTextBoxColumn priceCol;
        private DataGridViewTextBoxColumn sizeCol;
        private DataGridViewTextBoxColumn valueCol;
        private Button backButton;
    }
}