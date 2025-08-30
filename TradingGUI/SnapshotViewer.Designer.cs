// MarketDashboardControl2.Designer.cs
using ScottPlot;

namespace SimulatorWinForms
{
    partial class SnapshotViewer
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.dashboardGrid = new System.Windows.Forms.TableLayoutPanel();
            this.chartContainer = new System.Windows.Forms.Panel();
            this.chartLayout = new System.Windows.Forms.TableLayoutPanel();
            this.chartControls = new System.Windows.Forms.FlowLayoutPanel();
            this.chartHeader = new System.Windows.Forms.Label();
            this.timeframeCombo = new System.Windows.Forms.ComboBox();
            this.priceChart = new ScottPlot.FormsPlot();
            this.marketInfoContainer = new System.Windows.Forms.Panel();
            this.infoGrid = new System.Windows.Forms.TableLayoutPanel();
            this.leftColumn = new System.Windows.Forms.TableLayoutPanel();
            this.pricesGrid = new System.Windows.Forms.TableLayoutPanel();
            this.allTimeHighLabel = new System.Windows.Forms.Label();
            this.allTimeHighAsk = new System.Windows.Forms.Panel();
            this.allTimeHighAskPrice = new System.Windows.Forms.Label();
            this.allTimeHighAskTime = new System.Windows.Forms.Label();
            this.allTimeHighBid = new System.Windows.Forms.Panel();
            this.allTimeHighBidPrice = new System.Windows.Forms.Label();
            this.allTimeHighBidTime = new System.Windows.Forms.Label();
            this.recentHighLabel = new System.Windows.Forms.Label();
            this.recentHighAsk = new System.Windows.Forms.Panel();
            this.recentHighAskPrice = new System.Windows.Forms.Label();
            this.recentHighAskTime = new System.Windows.Forms.Label();
            this.recentHighBid = new System.Windows.Forms.Panel();
            this.recentHighBidPrice = new System.Windows.Forms.Label();
            this.recentHighBidTime = new System.Windows.Forms.Label();
            this.currentPriceLabel = new System.Windows.Forms.Label();
            this.currentPriceAsk = new System.Windows.Forms.Label();
            this.currentPriceBid = new System.Windows.Forms.Label();
            this.recentLowLabel = new System.Windows.Forms.Label();
            this.recentLowAsk = new System.Windows.Forms.Panel();
            this.recentLowAskPrice = new System.Windows.Forms.Label();
            this.recentLowAskTime = new System.Windows.Forms.Label();
            this.recentLowBid = new System.Windows.Forms.Panel();
            this.recentLowBidPrice = new System.Windows.Forms.Label();
            this.recentLowBidTime = new System.Windows.Forms.Label();
            this.allTimeLowLabel = new System.Windows.Forms.Label();
            this.allTimeLowAsk = new System.Windows.Forms.Panel();
            this.allTimeLowAskPrice = new System.Windows.Forms.Label();
            this.allTimeLowAskTime = new System.Windows.Forms.Label();
            this.allTimeLowBid = new System.Windows.Forms.Panel();
            this.allTimeLowBidPrice = new System.Windows.Forms.Label();
            this.allTimeLowBidTime = new System.Windows.Forms.Label();
            this.tradingMetricsGrid = new System.Windows.Forms.TableLayoutPanel();
            this.tradingMetricsHeader = new System.Windows.Forms.Label();
            this.rsiLabel = new System.Windows.Forms.Label();
            this.rsiValue = new System.Windows.Forms.Label();
            this.macdLabel = new System.Windows.Forms.Label();
            this.macdValue = new System.Windows.Forms.Label();
            this.emaLabel = new System.Windows.Forms.Label();
            this.emaValue = new System.Windows.Forms.Label();
            this.bollingerLabel = new System.Windows.Forms.Label();
            this.bollingerValue = new System.Windows.Forms.Label();
            this.atrLabel = new System.Windows.Forms.Label();
            this.atrValue = new System.Windows.Forms.Label();
            this.vwapLabel = new System.Windows.Forms.Label();
            this.vwapValue = new System.Windows.Forms.Label();
            this.stochasticLabel = new System.Windows.Forms.Label();
            this.stochasticValue = new System.Windows.Forms.Label();
            this.obvLabel = new System.Windows.Forms.Label();
            this.obvValue = new System.Windows.Forms.Label();
            this.rightColumn = new System.Windows.Forms.TableLayoutPanel();
            this.otherInfoGrid = new System.Windows.Forms.TableLayoutPanel();
            this.titleLabel = new System.Windows.Forms.Label();
            this.titleValue = new System.Windows.Forms.Label();
            this.subtitleLabel = new System.Windows.Forms.Label();
            this.subtitleValue = new System.Windows.Forms.Label();
            this.marketTypeLabel = new System.Windows.Forms.Label();
            this.marketTypeValue = new System.Windows.Forms.Label();
            this.priceGoodBadLabel = new System.Windows.Forms.Label();
            this.priceGoodBadValue = new System.Windows.Forms.Label();
            this.marketBehaviorLabel = new System.Windows.Forms.Label();
            this.marketBehaviorValue = new System.Windows.Forms.Label();
            this.timeLeftLabel = new System.Windows.Forms.Label();
            this.timeLeftValue = new System.Windows.Forms.Label();
            this.marketAgeLabel = new System.Windows.Forms.Label();
            this.marketAgeValue = new System.Windows.Forms.Label();
            this.flowMomentumGrid = new System.Windows.Forms.TableLayoutPanel();
            this.flowHeader = new System.Windows.Forms.Label();
            this.topVelocityLabel = new System.Windows.Forms.Label();
            this.topVelocityValue = new System.Windows.Forms.Label();
            this.bottomVelocityLabel = new System.Windows.Forms.Label();
            this.bottomVelocityValue = new System.Windows.Forms.Label();
            this.netOrderRateLabel = new System.Windows.Forms.Label();
            this.netOrderRateValue = new System.Windows.Forms.Label();
            this.tradeVolumeLabel = new System.Windows.Forms.Label();
            this.tradeVolumeValue = new System.Windows.Forms.Label();
            this.avgTradeSizeLabel = new System.Windows.Forms.Label();
            this.avgTradeSizeValue = new System.Windows.Forms.Label();
            this.contextGrid = new System.Windows.Forms.TableLayoutPanel();
            this.contextHeader = new System.Windows.Forms.Label();
            this.spreadLabel = new System.Windows.Forms.Label();
            this.spreadValue = new System.Windows.Forms.Label();
            this.imbalLabel = new System.Windows.Forms.Label();
            this.imbalValue = new System.Windows.Forms.Label();
            this.depthTop4Label = new System.Windows.Forms.Label();
            this.depthTop4Value = new System.Windows.Forms.Label();
            this.centerMassLabel = new System.Windows.Forms.Label();
            this.centerMassValue = new System.Windows.Forms.Label();
            this.totalContractsLabel = new System.Windows.Forms.Label();
            this.totalContractsValue = new System.Windows.Forms.Label();
            this.positionsContainer = new System.Windows.Forms.Panel();
            this.positionsGrid = new System.Windows.Forms.TableLayoutPanel();
            this.positionSizeLabel = new System.Windows.Forms.Label();
            this.positionSizeValue = new System.Windows.Forms.Label();
            this.lastTradeLabel = new System.Windows.Forms.Label();
            this.lastTradeValue = new System.Windows.Forms.Label();
            this.positionRoiLabel = new System.Windows.Forms.Label();
            this.positionRoiValue = new System.Windows.Forms.Label();
            this.buyinPriceLabel = new System.Windows.Forms.Label();
            this.buyinPriceValue = new System.Windows.Forms.Label();
            this.positionUpsideLabel = new System.Windows.Forms.Label();
            this.positionUpsideValue = new System.Windows.Forms.Label();
            this.positionDownsideLabel = new System.Windows.Forms.Label();
            this.positionDownsideValue = new System.Windows.Forms.Label();
            this.restingOrdersLabel = new System.Windows.Forms.Label();
            this.restingOrdersValue = new System.Windows.Forms.Label();
            this.orderbookContainer = new System.Windows.Forms.Panel();
            this.orderbookGrid = new System.Windows.Forms.DataGridView();
            this.priceCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.sizeCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.valueCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.backButton = new System.Windows.Forms.Button();
            this.mainLayout.SuspendLayout();
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
            this.positionsGrid.SuspendLayout();
            this.orderbookContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.orderbookGrid)).BeginInit();
            this.SuspendLayout();
            // mainLayout
            this.mainLayout.AutoSize = true;
            this.mainLayout.ColumnCount = 1;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Controls.Add(this.dashboardGrid, 0, 0);
            this.mainLayout.Controls.Add(this.backButton, 0, 1);
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Location = new System.Drawing.Point(0, 0);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.RowCount = 2;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.mainLayout.Size = new System.Drawing.Size(800, 600);
            this.mainLayout.TabIndex = 0;
            // dashboardGrid
            this.dashboardGrid.ColumnCount = 2;
            this.dashboardGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.dashboardGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.dashboardGrid.Controls.Add(this.chartContainer, 0, 0);
            this.dashboardGrid.Controls.Add(this.marketInfoContainer, 1, 0);
            this.dashboardGrid.Controls.Add(this.positionsContainer, 0, 1);
            this.dashboardGrid.Controls.Add(this.orderbookContainer, 1, 1);
            this.dashboardGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dashboardGrid.Location = new System.Drawing.Point(3, 3);
            this.dashboardGrid.Name = "dashboardGrid";
            this.dashboardGrid.RowCount = 2;
            this.dashboardGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.dashboardGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.dashboardGrid.Size = new System.Drawing.Size(794, 550);
            this.dashboardGrid.TabIndex = 0;
            // chartContainer
            this.chartContainer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.chartContainer.Controls.Add(this.chartLayout);
            this.chartContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartContainer.Location = new System.Drawing.Point(3, 3);
            this.chartContainer.Name = "chartContainer";
            this.chartContainer.Padding = new System.Windows.Forms.Padding(5);
            this.chartContainer.Size = new System.Drawing.Size(391, 269);
            this.chartContainer.TabIndex = 0;
            // chartLayout
            this.chartLayout.AutoSize = true;
            this.chartLayout.ColumnCount = 1;
            this.chartLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.chartLayout.Controls.Add(this.chartControls, 0, 0);
            this.chartLayout.Controls.Add(this.priceChart, 0, 1);
            this.chartLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartLayout.Location = new System.Drawing.Point(5, 5);
            this.chartLayout.Name = "chartLayout";
            this.chartLayout.RowCount = 2;
            this.chartLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.chartLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.chartLayout.Size = new System.Drawing.Size(379, 257);
            this.chartLayout.TabIndex = 0;
            // chartControls
            this.chartControls.AutoSize = true;
            this.chartControls.Controls.Add(this.chartHeader);
            this.chartControls.Controls.Add(this.timeframeCombo);
            this.chartControls.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartControls.Location = new System.Drawing.Point(3, 3);
            this.chartControls.Name = "chartControls";
            this.chartControls.Size = new System.Drawing.Size(373, 21);
            this.chartControls.TabIndex = 0;
            // chartHeader
            this.chartHeader.AutoSize = true;
            this.chartHeader.Location = new System.Drawing.Point(0, 0);
            this.chartHeader.Name = "chartHeader";
            this.chartHeader.Size = new System.Drawing.Size(70, 13);
            this.chartHeader.TabIndex = 0;
            this.chartHeader.Text = "Price Chart - ";
            // timeframeCombo
            this.timeframeCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.timeframeCombo.FormattingEnabled = true;
            this.timeframeCombo.Items.AddRange(new object[] {
            "15 Minutes",
            "1 Hour",
            "1 Day",
            "3 Days",
            "1 Week",
            "1 Month",
            "All"});
            this.timeframeCombo.Location = new System.Drawing.Point(73, 0);
            this.timeframeCombo.Name = "timeframeCombo";
            this.timeframeCombo.Size = new System.Drawing.Size(121, 21);
            this.timeframeCombo.TabIndex = 1;
            this.timeframeCombo.SelectedIndex = 6;
            // priceChart
            this.priceChart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.priceChart.Location = new System.Drawing.Point(3, 30);
            this.priceChart.Name = "priceChart";
            this.priceChart.Size = new System.Drawing.Size(373, 224);
            this.priceChart.TabIndex = 1;
            // marketInfoContainer
            this.marketInfoContainer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.marketInfoContainer.Controls.Add(this.infoGrid);
            this.marketInfoContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.marketInfoContainer.Location = new System.Drawing.Point(400, 3);
            this.marketInfoContainer.Name = "marketInfoContainer";
            this.marketInfoContainer.Padding = new System.Windows.Forms.Padding(5);
            this.marketInfoContainer.Size = new System.Drawing.Size(391, 269);
            this.marketInfoContainer.TabIndex = 1;
            // infoGrid
            this.infoGrid.ColumnCount = 2;
            this.infoGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.infoGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.infoGrid.Controls.Add(this.leftColumn, 0, 0);
            this.infoGrid.Controls.Add(this.rightColumn, 1, 0);
            this.infoGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.infoGrid.Location = new System.Drawing.Point(5, 5);
            this.infoGrid.Name = "infoGrid";
            this.infoGrid.RowCount = 1;
            this.infoGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.infoGrid.Size = new System.Drawing.Size(379, 257);
            this.infoGrid.TabIndex = 0;
            // leftColumn
            this.leftColumn.AutoSize = true;
            this.leftColumn.ColumnCount = 1;
            this.leftColumn.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftColumn.Controls.Add(this.pricesGrid, 0, 0);
            this.leftColumn.Controls.Add(this.tradingMetricsGrid, 0, 1);
            this.leftColumn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftColumn.Location = new System.Drawing.Point(0, 0);
            this.leftColumn.Name = "leftColumn";
            this.leftColumn.RowCount = 2;
            this.leftColumn.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.leftColumn.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.leftColumn.Size = new System.Drawing.Size(189, 257);
            this.leftColumn.TabIndex = 0;
            // pricesGrid
            this.pricesGrid.AutoSize = true;
            this.pricesGrid.ColumnCount = 3;
            this.pricesGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.pricesGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.pricesGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
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
            this.pricesGrid.Dock = System.Windows.Forms.DockStyle.Top;
            this.pricesGrid.Location = new System.Drawing.Point(0, 0);
            this.pricesGrid.Name = "pricesGrid";
            this.pricesGrid.RowCount = 5;
            this.pricesGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.pricesGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.pricesGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.pricesGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.pricesGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.pricesGrid.Size = new System.Drawing.Size(189, 100);
            this.pricesGrid.TabIndex = 0;
            // allTimeHighLabel
            this.allTimeHighLabel.AutoSize = true;
            this.allTimeHighLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.allTimeHighLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.allTimeHighLabel.Name = "allTimeHighLabel";
            this.allTimeHighLabel.Size = new System.Drawing.Size(94, 20);
            this.allTimeHighLabel.TabIndex = 0;
            this.allTimeHighLabel.Text = "All Time High";
            // allTimeHighAsk
            this.allTimeHighAsk.AutoSize = true;
            this.allTimeHighAsk.Controls.Add(this.allTimeHighAskPrice);
            this.allTimeHighAsk.Controls.Add(this.allTimeHighAskTime);
            this.allTimeHighAsk.Dock = System.Windows.Forms.DockStyle.Fill;
            this.allTimeHighAsk.Name = "allTimeHighAsk";
            this.allTimeHighAsk.Size = new System.Drawing.Size(47, 20);
            this.allTimeHighAsk.TabIndex = 1;
            // allTimeHighAskPrice
            this.allTimeHighAskPrice.AutoSize = true;
            this.allTimeHighAskPrice.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.allTimeHighAskPrice.Name = "allTimeHighAskPrice";
            this.allTimeHighAskPrice.Size = new System.Drawing.Size(14, 13);
            this.allTimeHighAskPrice.TabIndex = 0;
            this.allTimeHighAskPrice.Text = "--";
            // allTimeHighAskTime
            this.allTimeHighAskTime.AutoSize = true;
            this.allTimeHighAskTime.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.allTimeHighAskTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.allTimeHighAskTime.Name = "allTimeHighAskTime";
            this.allTimeHighAskTime.Size = new System.Drawing.Size(14, 13);
            this.allTimeHighAskTime.TabIndex = 1;
            this.allTimeHighAskTime.Text = "--";
            // allTimeHighBid
            this.allTimeHighBid.AutoSize = true;
            this.allTimeHighBid.Controls.Add(this.allTimeHighBidPrice);
            this.allTimeHighBid.Controls.Add(this.allTimeHighBidTime);
            this.allTimeHighBid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.allTimeHighBid.Name = "allTimeHighBid";
            this.allTimeHighBid.Size = new System.Drawing.Size(48, 20);
            this.allTimeHighBid.TabIndex = 2;
            // allTimeHighBidPrice
            this.allTimeHighBidPrice.AutoSize = true;
            this.allTimeHighBidPrice.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.allTimeHighBidPrice.Name = "allTimeHighBidPrice";
            this.allTimeHighBidPrice.Size = new System.Drawing.Size(14, 13);
            this.allTimeHighBidPrice.TabIndex = 0;
            this.allTimeHighBidPrice.Text = "--";
            // allTimeHighBidTime
            this.allTimeHighBidTime.AutoSize = true;
            this.allTimeHighBidTime.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.allTimeHighBidTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.allTimeHighBidTime.Name = "allTimeHighBidTime";
            this.allTimeHighBidTime.Size = new System.Drawing.Size(14, 13);
            this.allTimeHighBidTime.TabIndex = 1;
            this.allTimeHighBidTime.Text = "--";
            // recentHighLabel
            this.recentHighLabel.AutoSize = true;
            this.recentHighLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.recentHighLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.recentHighLabel.Name = "recentHighLabel";
            this.recentHighLabel.Size = new System.Drawing.Size(94, 20);
            this.recentHighLabel.TabIndex = 3;
            this.recentHighLabel.Text = "Recent High";
            // recentHighAsk
            this.recentHighAsk.AutoSize = true;
            this.recentHighAsk.Controls.Add(this.recentHighAskPrice);
            this.recentHighAsk.Controls.Add(this.recentHighAskTime);
            this.recentHighAsk.Dock = System.Windows.Forms.DockStyle.Fill;
            this.recentHighAsk.Name = "recentHighAsk";
            this.recentHighAsk.Size = new System.Drawing.Size(47, 20);
            this.recentHighAsk.TabIndex = 4;
            // recentHighAskPrice
            this.recentHighAskPrice.AutoSize = true;
            this.recentHighAskPrice.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.recentHighAskPrice.Name = "recentHighAskPrice";
            this.recentHighAskPrice.Size = new System.Drawing.Size(14, 13);
            this.recentHighAskPrice.TabIndex = 0;
            this.recentHighAskPrice.Text = "--";
            // recentHighAskTime
            this.recentHighAskTime.AutoSize = true;
            this.recentHighAskTime.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.recentHighAskTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.recentHighAskTime.Name = "recentHighAskTime";
            this.recentHighAskTime.Size = new System.Drawing.Size(14, 13);
            this.recentHighAskTime.TabIndex = 1;
            this.recentHighAskTime.Text = "--";
            // recentHighBid
            this.recentHighBid.AutoSize = true;
            this.recentHighBid.Controls.Add(this.recentHighBidPrice);
            this.recentHighBid.Controls.Add(this.recentHighBidTime);
            this.recentHighBid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.recentHighBid.Name = "recentHighBid";
            this.recentHighBid.Size = new System.Drawing.Size(48, 20);
            this.recentHighBid.TabIndex = 5;
            // recentHighBidPrice
            this.recentHighBidPrice.AutoSize = true;
            this.recentHighBidPrice.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.recentHighBidPrice.Name = "recentHighBidPrice";
            this.recentHighBidPrice.Size = new System.Drawing.Size(14, 13);
            this.recentHighBidPrice.TabIndex = 0;
            this.recentHighBidPrice.Text = "--";
            // recentHighBidTime
            this.recentHighBidTime.AutoSize = true;
            this.recentHighBidTime.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.recentHighBidTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.recentHighBidTime.Name = "recentHighBidTime";
            this.recentHighBidTime.Size = new System.Drawing.Size(14, 13);
            this.recentHighBidTime.TabIndex = 1;
            this.recentHighBidTime.Text = "--";
            // currentPriceLabel
            this.currentPriceLabel.AutoSize = true;
            this.currentPriceLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.currentPriceLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.currentPriceLabel.Name = "currentPriceLabel";
            this.currentPriceLabel.Size = new System.Drawing.Size(94, 20);
            this.currentPriceLabel.TabIndex = 6;
            this.currentPriceLabel.Text = "Current Price";
            // currentPriceAsk
            this.currentPriceAsk.AutoSize = true;
            this.currentPriceAsk.Dock = System.Windows.Forms.DockStyle.Fill;
            this.currentPriceAsk.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.currentPriceAsk.Name = "currentPriceAsk";
            this.currentPriceAsk.Size = new System.Drawing.Size(47, 20);
            this.currentPriceAsk.TabIndex = 7;
            this.currentPriceAsk.Text = "--";
            // currentPriceBid
            this.currentPriceBid.AutoSize = true;
            this.currentPriceBid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.currentPriceBid.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.currentPriceBid.Name = "currentPriceBid";
            this.currentPriceBid.Size = new System.Drawing.Size(48, 20);
            this.currentPriceBid.TabIndex = 8;
            this.currentPriceBid.Text = "--";
            // recentLowLabel
            this.recentLowLabel.AutoSize = true;
            this.recentLowLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.recentLowLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.recentLowLabel.Name = "recentLowLabel";
            this.recentLowLabel.Size = new System.Drawing.Size(94, 20);
            this.recentLowLabel.TabIndex = 9;
            this.recentLowLabel.Text = "Recent Low";
            // recentLowAsk
            this.recentLowAsk.AutoSize = true;
            this.recentLowAsk.Controls.Add(this.recentLowAskPrice);
            this.recentLowAsk.Controls.Add(this.recentLowAskTime);
            this.recentLowAsk.Dock = System.Windows.Forms.DockStyle.Fill;
            this.recentLowAsk.Name = "recentLowAsk";
            this.recentLowAsk.Size = new System.Drawing.Size(47, 20);
            this.recentLowAsk.TabIndex = 10;
            // recentLowAskPrice
            this.recentLowAskPrice.AutoSize = true;
            this.recentLowAskPrice.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.recentLowAskPrice.Name = "recentLowAskPrice";
            this.recentLowAskPrice.Size = new System.Drawing.Size(14, 13);
            this.recentLowAskPrice.TabIndex = 0;
            this.recentLowAskPrice.Text = "--";
            // recentLowAskTime
            this.recentLowAskTime.AutoSize = true;
            this.recentLowAskTime.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.recentLowAskTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.recentLowAskTime.Name = "recentLowAskTime";
            this.recentLowAskTime.Size = new System.Drawing.Size(14, 13);
            this.recentLowAskTime.TabIndex = 1;
            this.recentLowAskTime.Text = "--";
            // recentLowBid
            this.recentLowBid.AutoSize = true;
            this.recentLowBid.Controls.Add(this.recentLowBidPrice);
            this.recentLowBid.Controls.Add(this.recentLowBidTime);
            this.recentLowBid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.recentLowBid.Name = "recentLowBid";
            this.recentLowBid.Size = new System.Drawing.Size(48, 20);
            this.recentLowBid.TabIndex = 11;
            // recentLowBidPrice
            this.recentLowBidPrice.AutoSize = true;
            this.recentLowBidPrice.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.recentLowBidPrice.Name = "recentLowBidPrice";
            this.recentLowBidPrice.Size = new System.Drawing.Size(14, 13);
            this.recentLowBidPrice.TabIndex = 0;
            this.recentLowBidPrice.Text = "--";
            // recentLowBidTime
            this.recentLowBidTime.AutoSize = true;
            this.recentLowBidTime.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.recentLowBidTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.recentLowBidTime.Name = "recentLowBidTime";
            this.recentLowBidTime.Size = new System.Drawing.Size(14, 13);
            this.recentLowBidTime.TabIndex = 1;
            this.recentLowBidTime.Text = "--";
            // allTimeLowLabel
            this.allTimeLowLabel.AutoSize = true;
            this.allTimeLowLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.allTimeLowLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.allTimeLowLabel.Name = "allTimeLowLabel";
            this.allTimeLowLabel.Size = new System.Drawing.Size(94, 20);
            this.allTimeLowLabel.TabIndex = 12;
            this.allTimeLowLabel.Text = "All Time Low";
            // allTimeLowAsk
            this.allTimeLowAsk.AutoSize = true;
            this.allTimeLowAsk.Controls.Add(this.allTimeLowAskPrice);
            this.allTimeLowAsk.Controls.Add(this.allTimeLowAskTime);
            this.allTimeLowAsk.Dock = System.Windows.Forms.DockStyle.Fill;
            this.allTimeLowAsk.Name = "allTimeLowAsk";
            this.allTimeLowAsk.Size = new System.Drawing.Size(47, 20);
            this.allTimeLowAsk.TabIndex = 13;
            // allTimeLowAskPrice
            this.allTimeLowAskPrice.AutoSize = true;
            this.allTimeLowAskPrice.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.allTimeLowAskPrice.Name = "allTimeLowAskPrice";
            this.allTimeLowAskPrice.Size = new System.Drawing.Size(14, 13);
            this.allTimeLowAskPrice.TabIndex = 0;
            this.allTimeLowAskPrice.Text = "--";
            // allTimeLowAskTime
            this.allTimeLowAskTime.AutoSize = true;
            this.allTimeLowAskTime.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.allTimeLowAskTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.allTimeLowAskTime.Name = "allTimeLowAskTime";
            this.allTimeLowAskTime.Size = new System.Drawing.Size(14, 13);
            this.allTimeLowAskTime.TabIndex = 1;
            this.allTimeLowAskTime.Text = "--";
            // allTimeLowBid
            this.allTimeLowBid.AutoSize = true;
            this.allTimeLowBid.Controls.Add(this.allTimeLowBidPrice);
            this.allTimeLowBid.Controls.Add(this.allTimeLowBidTime);
            this.allTimeLowBid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.allTimeLowBid.Name = "allTimeLowBid";
            this.allTimeLowBid.Size = new System.Drawing.Size(48, 20);
            this.allTimeLowBid.TabIndex = 14;
            // allTimeLowBidPrice
            this.allTimeLowBidPrice.AutoSize = true;
            this.allTimeLowBidPrice.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.allTimeLowBidPrice.Name = "allTimeLowBidPrice";
            this.allTimeLowBidPrice.Size = new System.Drawing.Size(14, 13);
            this.allTimeLowBidPrice.TabIndex = 0;
            this.allTimeLowBidPrice.Text = "--";
            // allTimeLowBidTime
            this.allTimeLowBidTime.AutoSize = true;
            this.allTimeLowBidTime.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.allTimeLowBidTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.allTimeLowBidTime.Name = "allTimeLowBidTime";
            this.allTimeLowBidTime.Size = new System.Drawing.Size(14, 13);
            this.allTimeLowBidTime.TabIndex = 1;
            this.allTimeLowBidTime.Text = "--";
            // tradingMetricsGrid
            this.tradingMetricsGrid.AutoSize = true;
            this.tradingMetricsGrid.ColumnCount = 2;
            this.tradingMetricsGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tradingMetricsGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
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
            this.tradingMetricsGrid.Dock = System.Windows.Forms.DockStyle.Top;
            this.tradingMetricsGrid.Location = new System.Drawing.Point(0, 100);
            this.tradingMetricsGrid.Name = "tradingMetricsGrid";
            this.tradingMetricsGrid.RowCount = 9;
            this.tradingMetricsGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tradingMetricsGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tradingMetricsGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tradingMetricsGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tradingMetricsGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tradingMetricsGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tradingMetricsGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tradingMetricsGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tradingMetricsGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tradingMetricsGrid.Size = new System.Drawing.Size(189, 180);
            this.tradingMetricsGrid.TabIndex = 1;
            // tradingMetricsHeader
            this.tradingMetricsHeader.AutoSize = true;
            this.tradingMetricsHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tradingMetricsHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tradingMetricsHeader.Name = "tradingMetricsHeader";
            this.tradingMetricsHeader.Size = new System.Drawing.Size(189, 20);
            this.tradingMetricsHeader.TabIndex = 0;
            this.tradingMetricsHeader.Text = "Trading Metrics";
            this.tradingMetricsGrid.SetColumnSpan(this.tradingMetricsHeader, 2);
            // rsiLabel
            this.rsiLabel.AutoSize = true;
            this.rsiLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rsiLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.rsiLabel.Name = "rsiLabel";
            this.rsiLabel.Size = new System.Drawing.Size(94, 20);
            this.rsiLabel.TabIndex = 0;
            this.rsiLabel.Text = "RSI";
            // rsiValue
            this.rsiValue.AutoSize = true;
            this.rsiValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rsiValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.rsiValue.Name = "rsiValue";
            this.rsiValue.Size = new System.Drawing.Size(95, 20);
            this.rsiValue.TabIndex = 1;
            this.rsiValue.Text = "--";
            // macdLabel
            this.macdLabel.AutoSize = true;
            this.macdLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.macdLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.macdLabel.Name = "macdLabel";
            this.macdLabel.Size = new System.Drawing.Size(94, 20);
            this.macdLabel.TabIndex = 2;
            this.macdLabel.Text = "MACD";
            // macdValue
            this.macdValue.AutoSize = true;
            this.macdValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.macdValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.macdValue.Name = "macdValue";
            this.macdValue.Size = new System.Drawing.Size(95, 20);
            this.macdValue.TabIndex = 3;
            this.macdValue.Text = "--";
            // emaLabel
            this.emaLabel.AutoSize = true;
            this.emaLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.emaLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.emaLabel.Name = "emaLabel";
            this.emaLabel.Size = new System.Drawing.Size(94, 20);
            this.emaLabel.TabIndex = 4;
            this.emaLabel.Text = "EMA";
            // emaValue
            this.emaValue.AutoSize = true;
            this.emaValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.emaValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.emaValue.Name = "emaValue";
            this.emaValue.Size = new System.Drawing.Size(95, 20);
            this.emaValue.TabIndex = 5;
            this.emaValue.Text = "--";
            // bollingerLabel
            this.bollingerLabel.AutoSize = true;
            this.bollingerLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bollingerLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.bollingerLabel.Name = "bollingerLabel";
            this.bollingerLabel.Size = new System.Drawing.Size(94, 20);
            this.bollingerLabel.TabIndex = 6;
            this.bollingerLabel.Text = "Bollinger Bands";
            // bollingerValue
            this.bollingerValue.AutoSize = true;
            this.bollingerValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bollingerValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.bollingerValue.Name = "bollingerValue";
            this.bollingerValue.Size = new System.Drawing.Size(95, 20);
            this.bollingerValue.TabIndex = 7;
            this.bollingerValue.Text = "--";
            // atrLabel
            this.atrLabel.AutoSize = true;
            this.atrLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.atrLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.atrLabel.Name = "atrLabel";
            this.atrLabel.Size = new System.Drawing.Size(94, 20);
            this.atrLabel.TabIndex = 8;
            this.atrLabel.Text = "ATR";
            // atrValue
            this.atrValue.AutoSize = true;
            this.atrValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.atrValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.atrValue.Name = "atrValue";
            this.atrValue.Size = new System.Drawing.Size(95, 20);
            this.atrValue.TabIndex = 9;
            this.atrValue.Text = "--";
            // vwapLabel
            this.vwapLabel.AutoSize = true;
            this.vwapLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.vwapLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.vwapLabel.Name = "vwapLabel";
            this.vwapLabel.Size = new System.Drawing.Size(94, 20);
            this.vwapLabel.TabIndex = 10;
            this.vwapLabel.Text = "VWAP";
            // vwapValue
            this.vwapValue.AutoSize = true;
            this.vwapValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.vwapValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.vwapValue.Name = "vwapValue";
            this.vwapValue.Size = new System.Drawing.Size(95, 20);
            this.vwapValue.TabIndex = 11;
            this.vwapValue.Text = "--";
            // stochasticLabel
            this.stochasticLabel.AutoSize = true;
            this.stochasticLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.stochasticLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.stochasticLabel.Name = "stochasticLabel";
            this.stochasticLabel.Size = new System.Drawing.Size(94, 20);
            this.stochasticLabel.TabIndex = 12;
            this.stochasticLabel.Text = "Stochastic Oscillator";
            // stochasticValue
            this.stochasticValue.AutoSize = true;
            this.stochasticValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.stochasticValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.stochasticValue.Name = "stochasticValue";
            this.stochasticValue.Size = new System.Drawing.Size(95, 20);
            this.stochasticValue.TabIndex = 13;
            this.stochasticValue.Text = "--";
            // obvLabel
            this.obvLabel.AutoSize = true;
            this.obvLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.obvLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.obvLabel.Name = "obvLabel";
            this.obvLabel.Size = new System.Drawing.Size(94, 20);
            this.obvLabel.TabIndex = 14;
            this.obvLabel.Text = "OBV";
            // obvValue
            this.obvValue.AutoSize = true;
            this.obvValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.obvValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.obvValue.Name = "obvValue";
            this.obvValue.Size = new System.Drawing.Size(95, 20);
            this.obvValue.TabIndex = 15;
            this.obvValue.Text = "--";
            // rightColumn
            this.rightColumn.AutoSize = true;
            this.rightColumn.ColumnCount = 1;
            this.rightColumn.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightColumn.Controls.Add(this.otherInfoGrid, 0, 0);
            this.rightColumn.Controls.Add(this.flowMomentumGrid, 0, 1);
            this.rightColumn.Controls.Add(this.contextGrid, 0, 2);
            this.rightColumn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightColumn.Location = new System.Drawing.Point(192, 0);
            this.rightColumn.Name = "rightColumn";
            this.rightColumn.RowCount = 3;
            this.rightColumn.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.rightColumn.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.rightColumn.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.rightColumn.Size = new System.Drawing.Size(187, 257);
            this.rightColumn.TabIndex = 1;
            // otherInfoGrid
            this.otherInfoGrid.AutoSize = true;
            this.otherInfoGrid.ColumnCount = 2;
            this.otherInfoGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.otherInfoGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
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
            this.otherInfoGrid.Dock = System.Windows.Forms.DockStyle.Top;
            this.otherInfoGrid.Location = new System.Drawing.Point(0, 0);
            this.otherInfoGrid.Name = "otherInfoGrid";
            this.otherInfoGrid.RowCount = 7;
            this.otherInfoGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.otherInfoGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.otherInfoGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.otherInfoGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.otherInfoGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.otherInfoGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.otherInfoGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.otherInfoGrid.Size = new System.Drawing.Size(187, 140);
            this.otherInfoGrid.TabIndex = 0;
            // titleLabel
            this.titleLabel.AutoSize = true;
            this.titleLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(93, 20);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "Title";
            // titleValue
            this.titleValue.AutoSize = true;
            this.titleValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.titleValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.titleValue.Name = "titleValue";
            this.titleValue.Size = new System.Drawing.Size(94, 20);
            this.titleValue.TabIndex = 1;
            this.titleValue.Text = "--";
            // subtitleLabel
            this.subtitleLabel.AutoSize = true;
            this.subtitleLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.subtitleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.subtitleLabel.Name = "subtitleLabel";
            this.subtitleLabel.Size = new System.Drawing.Size(93, 20);
            this.subtitleLabel.TabIndex = 2;
            this.subtitleLabel.Text = "Subtitle";
            // subtitleValue
            this.subtitleValue.AutoSize = true;
            this.subtitleValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.subtitleValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.subtitleValue.Name = "subtitleValue";
            this.subtitleValue.Size = new System.Drawing.Size(94, 20);
            this.subtitleValue.TabIndex = 3;
            this.subtitleValue.Text = "--";
            // marketTypeLabel
            this.marketTypeLabel.AutoSize = true;
            this.marketTypeLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.marketTypeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.marketTypeLabel.Name = "marketTypeLabel";
            this.marketTypeLabel.Size = new System.Drawing.Size(93, 20);
            this.marketTypeLabel.TabIndex = 4;
            this.marketTypeLabel.Text = "Market Type";
            // marketTypeValue
            this.marketTypeValue.AutoSize = true;
            this.marketTypeValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.marketTypeValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.marketTypeValue.Name = "marketTypeValue";
            this.marketTypeValue.Size = new System.Drawing.Size(94, 20);
            this.marketTypeValue.TabIndex = 5;
            this.marketTypeValue.Text = "--";
            // priceGoodBadLabel
            this.priceGoodBadLabel.AutoSize = true;
            this.priceGoodBadLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.priceGoodBadLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.priceGoodBadLabel.Name = "priceGoodBadLabel";
            this.priceGoodBadLabel.Size = new System.Drawing.Size(93, 20);
            this.priceGoodBadLabel.TabIndex = 6;
            this.priceGoodBadLabel.Text = "Price Good/Bad";
            // priceGoodBadValue
            this.priceGoodBadValue.AutoSize = true;
            this.priceGoodBadValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.priceGoodBadValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.priceGoodBadValue.Name = "priceGoodBadValue";
            this.priceGoodBadValue.Size = new System.Drawing.Size(94, 20);
            this.priceGoodBadValue.TabIndex = 7;
            this.priceGoodBadValue.Text = "--";
            // marketBehaviorLabel
            this.marketBehaviorLabel.AutoSize = true;
            this.marketBehaviorLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.marketBehaviorLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.marketBehaviorLabel.Name = "marketBehaviorLabel";
            this.marketBehaviorLabel.Size = new System.Drawing.Size(93, 20);
            this.marketBehaviorLabel.TabIndex = 8;
            this.marketBehaviorLabel.Text = "Market Behavior";
            // marketBehaviorValue
            this.marketBehaviorValue.AutoSize = true;
            this.marketBehaviorValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.marketBehaviorValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.marketBehaviorValue.Name = "marketBehaviorValue";
            this.marketBehaviorValue.Size = new System.Drawing.Size(94, 20);
            this.marketBehaviorValue.TabIndex = 9;
            this.marketBehaviorValue.Text = "--";
            // timeLeftLabel
            this.timeLeftLabel.AutoSize = true;
            this.timeLeftLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.timeLeftLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.timeLeftLabel.Name = "timeLeftLabel";
            this.timeLeftLabel.Size = new System.Drawing.Size(93, 20);
            this.timeLeftLabel.TabIndex = 10;
            this.timeLeftLabel.Text = "Time Left";
            // timeLeftValue
            this.timeLeftValue.AutoSize = true;
            this.timeLeftValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.timeLeftValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.timeLeftValue.Name = "timeLeftValue";
            this.timeLeftValue.Size = new System.Drawing.Size(94, 20);
            this.timeLeftValue.TabIndex = 11;
            this.timeLeftValue.Text = "--";
            // marketAgeLabel
            this.marketAgeLabel.AutoSize = true;
            this.marketAgeLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.marketAgeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.marketAgeLabel.Name = "marketAgeLabel";
            this.marketAgeLabel.Size = new System.Drawing.Size(93, 20);
            this.marketAgeLabel.TabIndex = 12;
            this.marketAgeLabel.Text = "Market Age";
            // marketAgeValue
            this.marketAgeValue.AutoSize = true;
            this.marketAgeValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.marketAgeValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.marketAgeValue.Name = "marketAgeValue";
            this.marketAgeValue.Size = new System.Drawing.Size(94, 20);
            this.marketAgeValue.TabIndex = 13;
            this.marketAgeValue.Text = "--";
            // flowMomentumGrid
            this.flowMomentumGrid.AutoSize = true;
            this.flowMomentumGrid.ColumnCount = 2;
            this.flowMomentumGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.flowMomentumGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
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
            this.flowMomentumGrid.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowMomentumGrid.Location = new System.Drawing.Point(0, 140);
            this.flowMomentumGrid.Name = "flowMomentumGrid";
            this.flowMomentumGrid.RowCount = 6;
            this.flowMomentumGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.flowMomentumGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.flowMomentumGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.flowMomentumGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.flowMomentumGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.flowMomentumGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.flowMomentumGrid.Size = new System.Drawing.Size(187, 120);
            this.flowMomentumGrid.TabIndex = 1;
            // flowHeader
            this.flowHeader.AutoSize = true;
            this.flowHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flowHeader.Name = "flowHeader";
            this.flowHeader.Size = new System.Drawing.Size(187, 20);
            this.flowHeader.TabIndex = 0;
            this.flowHeader.Text = "Flow & Momentum";
            this.flowMomentumGrid.SetColumnSpan(this.flowHeader, 2);
            // topVelocityLabel
            this.topVelocityLabel.AutoSize = true;
            this.topVelocityLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topVelocityLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.topVelocityLabel.Name = "topVelocityLabel";
            this.topVelocityLabel.Size = new System.Drawing.Size(93, 20);
            this.topVelocityLabel.TabIndex = 0;
            this.topVelocityLabel.Text = "Top Velocity";
            // topVelocityValue
            this.topVelocityValue.AutoSize = true;
            this.topVelocityValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topVelocityValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.topVelocityValue.Name = "topVelocityValue";
            this.topVelocityValue.Size = new System.Drawing.Size(94, 20);
            this.topVelocityValue.TabIndex = 1;
            this.topVelocityValue.Text = "-- (--)";
            // bottomVelocityLabel
            this.bottomVelocityLabel.AutoSize = true;
            this.bottomVelocityLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bottomVelocityLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.bottomVelocityLabel.Name = "bottomVelocityLabel";
            this.bottomVelocityLabel.Size = new System.Drawing.Size(93, 20);
            this.bottomVelocityLabel.TabIndex = 2;
            this.bottomVelocityLabel.Text = "Bottom Velocity";
            // bottomVelocityValue
            this.bottomVelocityValue.AutoSize = true;
            this.bottomVelocityValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bottomVelocityValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.bottomVelocityValue.Name = "bottomVelocityValue";
            this.bottomVelocityValue.Size = new System.Drawing.Size(94, 20);
            this.bottomVelocityValue.TabIndex = 3;
            this.bottomVelocityValue.Text = "-- (--)";
            // netOrderRateLabel
            this.netOrderRateLabel.AutoSize = true;
            this.netOrderRateLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.netOrderRateLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.netOrderRateLabel.Name = "netOrderRateLabel";
            this.netOrderRateLabel.Size = new System.Drawing.Size(93, 20);
            this.netOrderRateLabel.TabIndex = 4;
            this.netOrderRateLabel.Text = "Net Order Rate";
            // netOrderRateValue
            this.netOrderRateValue.AutoSize = true;
            this.netOrderRateValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.netOrderRateValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.netOrderRateValue.Name = "netOrderRateValue";
            this.netOrderRateValue.Size = new System.Drawing.Size(94, 20);
            this.netOrderRateValue.TabIndex = 5;
            this.netOrderRateValue.Text = "-- (--)";
            // tradeVolumeLabel
            this.tradeVolumeLabel.AutoSize = true;
            this.tradeVolumeLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tradeVolumeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.tradeVolumeLabel.Name = "tradeVolumeLabel";
            this.tradeVolumeLabel.Size = new System.Drawing.Size(93, 20);
            this.tradeVolumeLabel.TabIndex = 6;
            this.tradeVolumeLabel.Text = "Trade Volume";
            // tradeVolumeValue
            this.tradeVolumeValue.AutoSize = true;
            this.tradeVolumeValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tradeVolumeValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.tradeVolumeValue.Name = "tradeVolumeValue";
            this.tradeVolumeValue.Size = new System.Drawing.Size(94, 20);
            this.tradeVolumeValue.TabIndex = 7;
            this.tradeVolumeValue.Text = "-- (--)";
            // avgTradeSizeLabel
            this.avgTradeSizeLabel.AutoSize = true;
            this.avgTradeSizeLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.avgTradeSizeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.avgTradeSizeLabel.Name = "avgTradeSizeLabel";
            this.avgTradeSizeLabel.Size = new System.Drawing.Size(93, 20);
            this.avgTradeSizeLabel.TabIndex = 8;
            this.avgTradeSizeLabel.Text = "Average Trade Size";
            // avgTradeSizeValue
            this.avgTradeSizeValue.AutoSize = true;
            this.avgTradeSizeValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.avgTradeSizeValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.avgTradeSizeValue.Name = "avgTradeSizeValue";
            this.avgTradeSizeValue.Size = new System.Drawing.Size(94, 20);
            this.avgTradeSizeValue.TabIndex = 9;
            this.avgTradeSizeValue.Text = "-- (--)";
            // contextGrid
            this.contextGrid.AutoSize = true;
            this.contextGrid.ColumnCount = 2;
            this.contextGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contextGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
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
            this.contextGrid.Dock = System.Windows.Forms.DockStyle.Top;
            this.contextGrid.Location = new System.Drawing.Point(0, 260);
            this.contextGrid.Name = "contextGrid";
            this.contextGrid.RowCount = 6;
            this.contextGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.contextGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.contextGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.contextGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.contextGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.contextGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.contextGrid.Size = new System.Drawing.Size(187, 120);
            this.contextGrid.TabIndex = 2;
            // contextHeader
            this.contextHeader.AutoSize = true;
            this.contextHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contextHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.contextHeader.Name = "contextHeader";
            this.contextHeader.Size = new System.Drawing.Size(187, 20);
            this.contextHeader.TabIndex = 0;
            this.contextHeader.Text = "Context & Deeper Book";
            this.contextGrid.SetColumnSpan(this.contextHeader, 2);
            // spreadLabel
            this.spreadLabel.AutoSize = true;
            this.spreadLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.spreadLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.spreadLabel.Name = "spreadLabel";
            this.spreadLabel.Size = new System.Drawing.Size(93, 20);
            this.spreadLabel.TabIndex = 0;
            this.spreadLabel.Text = "Spread";
            // spreadValue
            this.spreadValue.AutoSize = true;
            this.spreadValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.spreadValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.spreadValue.Name = "spreadValue";
            this.spreadValue.Size = new System.Drawing.Size(94, 20);
            this.spreadValue.TabIndex = 1;
            this.spreadValue.Text = "--";
            // imbalLabel
            this.imbalLabel.AutoSize = true;
            this.imbalLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imbalLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.imbalLabel.Name = "imbalLabel";
            this.imbalLabel.Size = new System.Drawing.Size(93, 20);
            this.imbalLabel.TabIndex = 2;
            this.imbalLabel.Text = "Ask/Bid Imbal (Vol)";
            // imbalValue
            this.imbalValue.AutoSize = true;
            this.imbalValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imbalValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.imbalValue.Name = "imbalValue";
            this.imbalValue.Size = new System.Drawing.Size(94, 20);
            this.imbalValue.TabIndex = 3;
            this.imbalValue.Text = "--";
            // depthTop4Label
            this.depthTop4Label.AutoSize = true;
            this.depthTop4Label.Dock = System.Windows.Forms.DockStyle.Fill;
            this.depthTop4Label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.depthTop4Label.Name = "depthTop4Label";
            this.depthTop4Label.Size = new System.Drawing.Size(93, 20);
            this.depthTop4Label.TabIndex = 4;
            this.depthTop4Label.Text = "Depth Top 4";
            // depthTop4Value
            this.depthTop4Value.AutoSize = true;
            this.depthTop4Value.Dock = System.Windows.Forms.DockStyle.Fill;
            this.depthTop4Value.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.depthTop4Value.Name = "depthTop4Value";
            this.depthTop4Value.Size = new System.Drawing.Size(94, 20);
            this.depthTop4Value.TabIndex = 5;
            this.depthTop4Value.Text = "-- (--)";
            // centerMassLabel
            this.centerMassLabel.AutoSize = true;
            this.centerMassLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.centerMassLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.centerMassLabel.Name = "centerMassLabel";
            this.centerMassLabel.Size = new System.Drawing.Size(93, 20);
            this.centerMassLabel.TabIndex = 6;
            this.centerMassLabel.Text = "Center Mass";
            // centerMassValue
            this.centerMassValue.AutoSize = true;
            this.centerMassValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.centerMassValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.centerMassValue.Name = "centerMassValue";
            this.centerMassValue.Size = new System.Drawing.Size(94, 20);
            this.centerMassValue.TabIndex = 7;
            this.centerMassValue.Text = "-- (--)";
            // totalContractsLabel
            this.totalContractsLabel.AutoSize = true;
            this.totalContractsLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.totalContractsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.totalContractsLabel.Name = "totalContractsLabel";
            this.totalContractsLabel.Size = new System.Drawing.Size(93, 20);
            this.totalContractsLabel.TabIndex = 8;
            this.totalContractsLabel.Text = "Total Contracts";
            // totalContractsValue
            this.totalContractsValue.AutoSize = true;
            this.totalContractsValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.totalContractsValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.totalContractsValue.Name = "totalContractsValue";
            this.totalContractsValue.Size = new System.Drawing.Size(94, 20);
            this.totalContractsValue.TabIndex = 9;
            this.totalContractsValue.Text = "-- (--)";
            // positionsContainer
            this.positionsContainer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.positionsContainer.Controls.Add(this.positionsGrid);
            this.positionsContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.positionsContainer.Location = new System.Drawing.Point(3, 278);
            this.positionsContainer.Name = "positionsContainer";
            this.positionsContainer.Padding = new System.Windows.Forms.Padding(5);
            this.positionsContainer.Size = new System.Drawing.Size(391, 269);
            this.positionsContainer.TabIndex = 2;
            // positionsGrid
            this.positionsGrid.AutoSize = true;
            this.positionsGrid.ColumnCount = 4;
            this.positionsGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.positionsGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.positionsGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.positionsGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
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
            this.positionsGrid.Dock = System.Windows.Forms.DockStyle.Top;
            this.positionsGrid.Location = new System.Drawing.Point(5, 5);
            this.positionsGrid.Name = "positionsGrid";
            this.positionsGrid.RowCount = 4;
            this.positionsGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.positionsGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.positionsGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.positionsGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.positionsGrid.Size = new System.Drawing.Size(379, 80);
            this.positionsGrid.TabIndex = 0;
            // positionSizeLabel
            this.positionSizeLabel.AutoSize = true;
            this.positionSizeLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.positionSizeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.positionSizeLabel.Name = "positionSizeLabel";
            this.positionSizeLabel.Size = new System.Drawing.Size(80, 20);
            this.positionSizeLabel.TabIndex = 0;
            this.positionSizeLabel.Text = "Position Size:";
            // positionSizeValue
            this.positionSizeValue.AutoSize = true;
            this.positionSizeValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.positionSizeValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.positionSizeValue.Name = "positionSizeValue";
            this.positionSizeValue.Size = new System.Drawing.Size(109, 20);
            this.positionSizeValue.TabIndex = 1;
            this.positionSizeValue.Text = "--";
            // lastTradeLabel
            this.lastTradeLabel.AutoSize = true;
            this.lastTradeLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lastTradeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lastTradeLabel.Name = "lastTradeLabel";
            this.lastTradeLabel.Size = new System.Drawing.Size(60, 20);
            this.lastTradeLabel.TabIndex = 2;
            this.lastTradeLabel.Text = "Last Trade:";
            // lastTradeValue
            this.lastTradeValue.AutoSize = true;
            this.lastTradeValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lastTradeValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lastTradeValue.Name = "lastTradeValue";
            this.lastTradeValue.Size = new System.Drawing.Size(130, 20);
            this.lastTradeValue.TabIndex = 3;
            this.lastTradeValue.Text = "--";
            // positionRoiLabel
            this.positionRoiLabel.AutoSize = true;
            this.positionRoiLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.positionRoiLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.positionRoiLabel.Name = "positionRoiLabel";
            this.positionRoiLabel.Size = new System.Drawing.Size(80, 20);
            this.positionRoiLabel.TabIndex = 4;
            this.positionRoiLabel.Text = "Position ROI:";
            // positionRoiValue
            this.positionRoiValue.AutoSize = true;
            this.positionRoiValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.positionRoiValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.positionRoiValue.Name = "positionRoiValue";
            this.positionRoiValue.Size = new System.Drawing.Size(109, 20);
            this.positionRoiValue.TabIndex = 5;
            this.positionRoiValue.Text = "--";
            // buyinPriceLabel
            this.buyinPriceLabel.AutoSize = true;
            this.buyinPriceLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buyinPriceLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buyinPriceLabel.Name = "buyinPriceLabel";
            this.buyinPriceLabel.Size = new System.Drawing.Size(60, 20);
            this.buyinPriceLabel.TabIndex = 6;
            this.buyinPriceLabel.Text = "Buyin Price:";
            // buyinPriceValue
            this.buyinPriceValue.AutoSize = true;
            this.buyinPriceValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buyinPriceValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.buyinPriceValue.Name = "buyinPriceValue";
            this.buyinPriceValue.Size = new System.Drawing.Size(130, 20);
            this.buyinPriceValue.TabIndex = 7;
            this.buyinPriceValue.Text = "--";
            // positionUpsideLabel
            this.positionUpsideLabel.AutoSize = true;
            this.positionUpsideLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.positionUpsideLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.positionUpsideLabel.Name = "positionUpsideLabel";
            this.positionUpsideLabel.Size = new System.Drawing.Size(80, 20);
            this.positionUpsideLabel.TabIndex = 8;
            this.positionUpsideLabel.Text = "Position Upside:";
            // positionUpsideValue
            this.positionUpsideValue.AutoSize = true;
            this.positionUpsideValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.positionUpsideValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.positionUpsideValue.Name = "positionUpsideValue";
            this.positionUpsideValue.Size = new System.Drawing.Size(109, 20);
            this.positionUpsideValue.TabIndex = 9;
            this.positionUpsideValue.Text = "--";
            // positionDownsideLabel
            this.positionDownsideLabel.AutoSize = true;
            this.positionDownsideLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.positionDownsideLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.positionDownsideLabel.Name = "positionDownsideLabel";
            this.positionDownsideLabel.Size = new System.Drawing.Size(60, 20);
            this.positionDownsideLabel.TabIndex = 10;
            this.positionDownsideLabel.Text = "Position Downside:";
            // positionDownsideValue
            this.positionDownsideValue.AutoSize = true;
            this.positionDownsideValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.positionDownsideValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.positionDownsideValue.Name = "positionDownsideValue";
            this.positionDownsideValue.Size = new System.Drawing.Size(130, 20);
            this.positionDownsideValue.TabIndex = 11;
            this.positionDownsideValue.Text = "--";
            // restingOrdersLabel
            this.restingOrdersLabel.AutoSize = true;
            this.restingOrdersLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.restingOrdersLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.restingOrdersLabel.Name = "restingOrdersLabel";
            this.restingOrdersLabel.Size = new System.Drawing.Size(80, 20);
            this.restingOrdersLabel.TabIndex = 12;
            this.restingOrdersLabel.Text = "Resting Orders:";
            // restingOrdersValue
            this.restingOrdersValue.AutoSize = true;
            this.restingOrdersValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.restingOrdersValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.restingOrdersValue.Name = "restingOrdersValue";
            this.restingOrdersValue.Size = new System.Drawing.Size(109, 20);
            this.restingOrdersValue.TabIndex = 13;
            this.restingOrdersValue.Text = "--";
            // orderbookContainer
            this.orderbookContainer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.orderbookContainer.Controls.Add(this.orderbookGrid);
            this.orderbookContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.orderbookContainer.Location = new System.Drawing.Point(400, 278);
            this.orderbookContainer.Name = "orderbookContainer";
            this.orderbookContainer.Padding = new System.Windows.Forms.Padding(5);
            this.orderbookContainer.Size = new System.Drawing.Size(391, 269);
            this.orderbookContainer.TabIndex = 3;
            // orderbookGrid
            this.orderbookGrid.AllowUserToAddRows = false;
            this.orderbookGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.orderbookGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.orderbookGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.priceCol,
            this.sizeCol,
            this.valueCol});
            this.orderbookGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.orderbookGrid.Location = new System.Drawing.Point(5, 5);
            this.orderbookGrid.Name = "orderbookGrid";
            this.orderbookGrid.ReadOnly = true;
            this.orderbookGrid.RowHeadersVisible = false;
            this.orderbookGrid.Size = new System.Drawing.Size(379, 257);
            this.orderbookGrid.TabIndex = 0;
            // priceCol
            this.priceCol.HeaderText = "Price";
            this.priceCol.Name = "priceCol";
            this.priceCol.ReadOnly = true;
            // sizeCol
            this.sizeCol.HeaderText = "Size";
            this.sizeCol.Name = "sizeCol";
            this.sizeCol.ReadOnly = true;
            // valueCol
            this.valueCol.HeaderText = "Value";
            this.valueCol.Name = "valueCol";
            this.valueCol.ReadOnly = true;
            // backButton
            this.backButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.backButton.Location = new System.Drawing.Point(3, 562);
            this.backButton.Name = "backButton";
            this.backButton.Size = new System.Drawing.Size(794, 35);
            this.backButton.TabIndex = 1;
            this.backButton.Text = "Back to Full Chart";
            this.backButton.UseVisualStyleBackColor = true;
            // MarketDashboardControl2
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainLayout);
            this.Name = "MarketDashboardControl2";
            this.Size = new System.Drawing.Size(800, 600);
            this.mainLayout.ResumeLayout(false);
            this.mainLayout.PerformLayout();
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
            this.positionsGrid.ResumeLayout(false);
            this.positionsGrid.PerformLayout();
            this.orderbookContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.orderbookGrid)).EndInit();
            this.ResumeLayout(false);

        }


        private TableLayoutPanel mainLayout;
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