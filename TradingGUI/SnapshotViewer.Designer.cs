// Full updated SnapshotViewer.Designer.cs

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
            mainLayout = new TableLayoutPanel();
            dashboardGrid = new TableLayoutPanel();
            chartContainer = new Panel();
            chartLayout = new TableLayoutPanel();
            chartControls = new FlowLayoutPanel();
            priceChart = new FormsPlot();
            marketInfoContainer = new Panel();
            infoGrid = new TableLayoutPanel();
            leftColumn = new TableLayoutPanel();
            pricesGrid = new TableLayoutPanel();
            pricesHeaderEmpty = new Label();
            pricesHeaderNo = new Label();
            pricesHeaderYes = new Label();
            allTimeHighLabel = new Label();
            allTimeHighAsk = new TableLayoutPanel();
            allTimeHighAskPrice = new Label();
            allTimeHighAskTime = new Label();
            allTimeHighBid = new TableLayoutPanel();
            allTimeHighBidPrice = new Label();
            allTimeHighBidTime = new Label();
            recentHighLabel = new Label();
            recentHighAsk = new TableLayoutPanel();
            recentHighAskPrice = new Label();
            recentHighAskTime = new Label();
            recentHighBid = new TableLayoutPanel();
            recentHighBidPrice = new Label();
            recentHighBidTime = new Label();
            currentPriceLabel = new Label();
            currentPriceAsk = new Label();
            currentPriceBid = new Label();
            recentLowLabel = new Label();
            recentLowAsk = new TableLayoutPanel();
            recentLowAskPrice = new Label();
            recentLowAskTime = new Label();
            recentLowBid = new TableLayoutPanel();
            recentLowBidPrice = new Label();
            recentLowBidTime = new Label();
            allTimeLowLabel = new Label();
            allTimeLowAsk = new TableLayoutPanel();
            allTimeLowAskPrice = new Label();
            allTimeLowAskTime = new Label();
            allTimeLowBid = new TableLayoutPanel();
            allTimeLowBidPrice = new Label();
            allTimeLowBidTime = new Label();
            tradingMetricsGrid = new TableLayoutPanel();
            tradingMetricsHeader = new Label();
            rsiLabel = new CheckBox();
            rsiValue = new Label();
            macdLabel = new CheckBox();
            macdValue = new Label();
            emaLabel = new CheckBox();
            emaValue = new Label();
            bollingerLabel = new CheckBox();
            bollingerValue = new Label();
            atrLabel = new CheckBox();
            atrValue = new Label();
            vwapLabel = new CheckBox();
            vwapValue = new Label();
            stochasticLabel = new CheckBox();
            stochasticValue = new Label();
            obvLabel = new CheckBox();
            obvValue = new Label();
            psarLabel = new CheckBox();
            psarValue = new Label();
            adxLabel = new CheckBox();
            adxValue = new Label();
            rightColumn = new TableLayoutPanel();
            otherInfoGrid = new TableLayoutPanel();
            CategoryLabel = new Label();
            categoryValue = new Label();
            timeLeftLabel = new Label();
            timeLeftValue = new Label();
            marketAgeLabel = new Label();
            marketAgeValue = new Label();
            flowMomentumGrid = new TableLayoutPanel();
            label1 = new Label();
            flowHeader = new Label();
            flowHeaderYes = new Label();
            flowHeaderNo = new Label();
            topVelocityLabel = new CheckBox();
            topVelocityYesValue = new Label();
            topVelocityNoValue = new Label();
            bottomVelocityLabel = new CheckBox();
            bottomVelocityYesValue = new Label();
            bottomVelocityNoValue = new Label();
            netOrderRateLabel = new CheckBox();
            netOrderRateYesValue = new Label();
            netOrderRateNoValue = new Label();
            tradeVolumeLabel = new CheckBox();
            tradeVolumeYesValue = new Label();
            tradeVolumeNoValue = new Label();
            avgTradeSizeLabel = new CheckBox();
            avgTradeSizeYesValue = new Label();
            avgTradeSizeNoValue = new Label();
            slopeLabel = new CheckBox();
            slopeYesValue = new Label();
            slopeNoValue = new Label();
            contextGrid = new TableLayoutPanel();
            label2 = new Label();
            contextHeader = new Label();
            contextHeaderYes = new Label();
            contextHeaderNo = new Label();
            spreadLabel = new Label();
            spreadValue = new Label();
            imbalLabel = new CheckBox();
            imbalValue = new Label();
            depthTop4Label = new CheckBox();
            depthTop4YesValue = new Label();
            depthTop4NoValue = new Label();
            centerMassLabel = new CheckBox();
            centerMassYesValue = new Label();
            centerMassNoValue = new Label();
            totalContractsLabel = new CheckBox();
            totalContractsYesValue = new Label();
            totalContractsNoValue = new Label();
            positionsContainer = new Panel();
            positionsLayout = new TableLayoutPanel();
            orderbookContainer = new Panel();
            orderbookGrid = new DataGridView();
            priceCol = new DataGridViewTextBoxColumn();
            sizeCol = new DataGridViewTextBoxColumn();
            valueCol = new DataGridViewTextBoxColumn();
            backButton = new Button();
            chartHeader = new Label();
            positionsGrid = new TableLayoutPanel();
            positionSizeValue = new Label();
            positionSizeLabel = new CheckBox();
            simulatedPositionValue = new Label();
            simulatedPositionLabel = new CheckBox();
            lastTradeLabel = new Label();
            lastTradeValue = new Label();
            positionRoiLabel = new CheckBox();
            positionRoiValue = new Label();
            buyinPriceLabel = new Label();
            buyinPriceValue = new Label();
            positionUpsideLabel = new Label();
            positionUpsideValue = new Label();
            positionDownsideLabel = new Label();
            positionDownsideValue = new Label();
            restingOrdersLabel = new CheckBox();
            restingOrdersValue = new Label();
            strategyOutputTextbox = new TextBox();
            mainLayout.SuspendLayout();
            dashboardGrid.SuspendLayout();
            chartContainer.SuspendLayout();
            chartLayout.SuspendLayout();
            marketInfoContainer.SuspendLayout();
            infoGrid.SuspendLayout();
            leftColumn.SuspendLayout();
            pricesGrid.SuspendLayout();
            allTimeHighAsk.SuspendLayout();
            allTimeHighBid.SuspendLayout();
            recentHighAsk.SuspendLayout();
            recentHighBid.SuspendLayout();
            recentLowAsk.SuspendLayout();
            recentLowBid.SuspendLayout();
            allTimeLowAsk.SuspendLayout();
            allTimeLowBid.SuspendLayout();
            tradingMetricsGrid.SuspendLayout();
            rightColumn.SuspendLayout();
            otherInfoGrid.SuspendLayout();
            flowMomentumGrid.SuspendLayout();
            contextGrid.SuspendLayout();
            positionsContainer.SuspendLayout();
            positionsLayout.SuspendLayout();
            orderbookContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)orderbookGrid).BeginInit();
            positionsGrid.SuspendLayout();
            SuspendLayout();
            // 
            // mainLayout
            // 
            mainLayout.AutoSize = true;
            mainLayout.ColumnCount = 1;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.Controls.Add(dashboardGrid, 0, 0);
            mainLayout.Controls.Add(backButton, 0, 1);
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.Location = new Point(0, 0);
            mainLayout.Margin = new Padding(4, 3, 4, 3);
            mainLayout.Name = "mainLayout";
            mainLayout.RowCount = 2;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 95F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 5F));
            mainLayout.Size = new Size(933, 692);
            mainLayout.TabIndex = 0;
            // 
            // dashboardGrid
            // 
            dashboardGrid.ColumnCount = 2;
            dashboardGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            dashboardGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            dashboardGrid.Controls.Add(chartContainer, 0, 0);
            dashboardGrid.Controls.Add(marketInfoContainer, 1, 0);
            dashboardGrid.Controls.Add(positionsContainer, 0, 1);
            dashboardGrid.Controls.Add(orderbookContainer, 1, 1);
            dashboardGrid.Dock = DockStyle.Fill;
            dashboardGrid.Location = new Point(4, 3);
            dashboardGrid.Margin = new Padding(4, 3, 4, 3);
            dashboardGrid.Name = "dashboardGrid";
            dashboardGrid.RowCount = 2;
            dashboardGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 69.43164F));
            dashboardGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 30.5683556F));
            dashboardGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            dashboardGrid.Size = new Size(925, 651);
            dashboardGrid.TabIndex = 0;
            // 
            // chartContainer
            // 
            chartContainer.BorderStyle = BorderStyle.FixedSingle;
            chartContainer.Controls.Add(chartLayout);
            chartContainer.Dock = DockStyle.Fill;
            chartContainer.Location = new Point(4, 3);
            chartContainer.Margin = new Padding(4, 3, 4, 3);
            chartContainer.Name = "chartContainer";
            chartContainer.Padding = new Padding(6);
            chartContainer.Size = new Size(593, 446);
            chartContainer.TabIndex = 0;
            // 
            // chartLayout
            // 
            chartLayout.AutoSize = true;
            chartLayout.ColumnCount = 1;
            chartLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            chartLayout.Controls.Add(chartControls, 0, 0);
            chartLayout.Controls.Add(priceChart, 0, 1);
            chartLayout.Dock = DockStyle.Fill;
            chartLayout.Location = new Point(6, 6);
            chartLayout.Margin = new Padding(0);
            chartLayout.Name = "chartLayout";
            chartLayout.RowCount = 2;
            chartLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            chartLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 90F));
            chartLayout.Size = new Size(579, 432);
            chartLayout.TabIndex = 0;
            // 
            // chartControls
            // 
            chartControls.AutoSize = true;
            chartControls.Dock = DockStyle.Fill;
            chartControls.Location = new Point(3, 3);
            chartControls.Name = "chartControls";
            chartControls.Size = new Size(573, 37);
            chartControls.TabIndex = 0;
            // 
            // priceChart
            // 
            priceChart.Dock = DockStyle.Fill;
            priceChart.Location = new Point(4, 46);
            priceChart.Margin = new Padding(4, 3, 4, 3);
            priceChart.Name = "priceChart";
            priceChart.Size = new Size(571, 383);
            priceChart.TabIndex = 1;
            // 
            // marketInfoContainer
            // 
            marketInfoContainer.BorderStyle = BorderStyle.FixedSingle;
            marketInfoContainer.Controls.Add(infoGrid);
            marketInfoContainer.Dock = DockStyle.Fill;
            marketInfoContainer.Location = new Point(605, 3);
            marketInfoContainer.Margin = new Padding(4, 3, 4, 3);
            marketInfoContainer.Name = "marketInfoContainer";
            marketInfoContainer.Padding = new Padding(6);
            marketInfoContainer.Size = new Size(316, 446);
            marketInfoContainer.TabIndex = 1;
            // 
            // infoGrid
            // 
            infoGrid.AutoSize = true;
            infoGrid.ColumnCount = 2;
            infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            infoGrid.Controls.Add(leftColumn, 0, 0);
            infoGrid.Controls.Add(rightColumn, 1, 0);
            infoGrid.Dock = DockStyle.Fill;
            infoGrid.Location = new Point(6, 6);
            infoGrid.Margin = new Padding(0);
            infoGrid.Name = "infoGrid";
            infoGrid.RowCount = 1;
            infoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            infoGrid.Size = new Size(302, 432);
            infoGrid.TabIndex = 0;
            // 
            // leftColumn
            // 
            leftColumn.AutoSize = true;
            leftColumn.ColumnCount = 1;
            leftColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftColumn.Controls.Add(pricesGrid, 0, 0);
            leftColumn.Controls.Add(tradingMetricsGrid, 0, 1);
            leftColumn.Dock = DockStyle.Fill;
            leftColumn.Location = new Point(3, 3);
            leftColumn.Name = "leftColumn";
            leftColumn.RowCount = 2;
            leftColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 46.9483566F));
            leftColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 53.0516434F));
            leftColumn.Size = new Size(160, 426);
            leftColumn.TabIndex = 0;
            leftColumn.Paint += leftColumn_Paint;
            // 
            // pricesGrid
            // 
            pricesGrid.AutoSize = true;
            pricesGrid.ColumnCount = 3;
            pricesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29.22078F));
            pricesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35.7142868F));
            pricesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34.4155846F));
            pricesGrid.Controls.Add(pricesHeaderEmpty, 0, 0);
            pricesGrid.Controls.Add(pricesHeaderNo, 1, 0);
            pricesGrid.Controls.Add(pricesHeaderYes, 2, 0);
            pricesGrid.Controls.Add(allTimeHighLabel, 0, 1);
            pricesGrid.Controls.Add(allTimeHighAsk, 1, 1);
            pricesGrid.Controls.Add(allTimeHighBid, 2, 1);
            pricesGrid.Controls.Add(recentHighLabel, 0, 2);
            pricesGrid.Controls.Add(recentHighAsk, 1, 2);
            pricesGrid.Controls.Add(recentHighBid, 2, 2);
            pricesGrid.Controls.Add(currentPriceLabel, 0, 3);
            pricesGrid.Controls.Add(currentPriceAsk, 1, 3);
            pricesGrid.Controls.Add(currentPriceBid, 2, 3);
            pricesGrid.Controls.Add(recentLowLabel, 0, 4);
            pricesGrid.Controls.Add(recentLowAsk, 1, 4);
            pricesGrid.Controls.Add(recentLowBid, 2, 4);
            pricesGrid.Controls.Add(allTimeLowLabel, 0, 5);
            pricesGrid.Controls.Add(allTimeLowAsk, 1, 5);
            pricesGrid.Controls.Add(allTimeLowBid, 2, 5);
            pricesGrid.Dock = DockStyle.Fill;
            pricesGrid.Location = new Point(3, 3);
            pricesGrid.Name = "pricesGrid";
            pricesGrid.RowCount = 6;
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 21F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 23.2432442F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 20.54054F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 15.6756754F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 19.45946F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            pricesGrid.Size = new Size(154, 194);
            pricesGrid.TabIndex = 0;
            // 
            // pricesHeaderEmpty
            // 
            pricesHeaderEmpty.AutoSize = true;
            pricesHeaderEmpty.Dock = DockStyle.Fill;
            pricesHeaderEmpty.Location = new Point(3, 0);
            pricesHeaderEmpty.Name = "pricesHeaderEmpty";
            pricesHeaderEmpty.Size = new Size(39, 33);
            pricesHeaderEmpty.TabIndex = 0;
            // 
            // pricesHeaderNo
            // 
            pricesHeaderNo.AutoSize = true;
            pricesHeaderNo.Dock = DockStyle.Fill;
            pricesHeaderNo.Location = new Point(48, 0);
            pricesHeaderNo.Name = "pricesHeaderNo";
            pricesHeaderNo.Size = new Size(49, 33);
            pricesHeaderNo.TabIndex = 1;
            pricesHeaderNo.Text = "No";
            // 
            // pricesHeaderYes
            // 
            pricesHeaderYes.AutoSize = true;
            pricesHeaderYes.Dock = DockStyle.Fill;
            pricesHeaderYes.Location = new Point(103, 0);
            pricesHeaderYes.Name = "pricesHeaderYes";
            pricesHeaderYes.Size = new Size(48, 33);
            pricesHeaderYes.TabIndex = 2;
            pricesHeaderYes.Text = "Yes";
            // 
            // allTimeHighLabel
            // 
            allTimeHighLabel.AutoSize = true;
            allTimeHighLabel.Dock = DockStyle.Fill;
            allTimeHighLabel.Font = new Font("Segoe UI", 7F);
            allTimeHighLabel.Location = new Point(3, 33);
            allTimeHighLabel.Name = "allTimeHighLabel";
            allTimeHighLabel.Size = new Size(39, 36);
            allTimeHighLabel.TabIndex = 3;
            allTimeHighLabel.Text = "All Time High";
            // 
            // allTimeHighAsk
            // 
            allTimeHighAsk.AutoSize = true;
            allTimeHighAsk.ColumnStyles.Add(new ColumnStyle());
            allTimeHighAsk.ColumnStyles.Add(new ColumnStyle());
            allTimeHighAsk.Controls.Add(allTimeHighAskPrice, 0, 0);
            allTimeHighAsk.Controls.Add(allTimeHighAskTime, 0, 1);
            allTimeHighAsk.Dock = DockStyle.Fill;
            allTimeHighAsk.Font = new Font("Segoe UI", 7F);
            allTimeHighAsk.Location = new Point(48, 36);
            allTimeHighAsk.Name = "allTimeHighAsk";
            allTimeHighAsk.RowCount = 2;
            allTimeHighAsk.RowStyles.Add(new RowStyle());
            allTimeHighAsk.RowStyles.Add(new RowStyle());
            allTimeHighAsk.Size = new Size(49, 30);
            allTimeHighAsk.TabIndex = 4;
            // 
            // allTimeHighAskPrice
            // 
            allTimeHighAskPrice.AutoSize = true;
            allTimeHighAskPrice.Dock = DockStyle.Fill;
            allTimeHighAskPrice.Font = new Font("Segoe UI", 7F);
            allTimeHighAskPrice.Location = new Point(3, 0);
            allTimeHighAskPrice.Name = "allTimeHighAskPrice";
            allTimeHighAskPrice.Size = new Size(43, 12);
            allTimeHighAskPrice.TabIndex = 0;
            allTimeHighAskPrice.Text = "--";
            allTimeHighAskPrice.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // allTimeHighAskTime
            // 
            allTimeHighAskTime.AutoSize = true;
            allTimeHighAskTime.Dock = DockStyle.Fill;
            allTimeHighAskTime.Font = new Font("Segoe UI", 7F);
            allTimeHighAskTime.Location = new Point(3, 12);
            allTimeHighAskTime.Name = "allTimeHighAskTime";
            allTimeHighAskTime.Size = new Size(43, 18);
            allTimeHighAskTime.TabIndex = 1;
            allTimeHighAskTime.Text = "--";
            allTimeHighAskTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // allTimeHighBid
            // 
            allTimeHighBid.AutoSize = true;
            allTimeHighBid.ColumnStyles.Add(new ColumnStyle());
            allTimeHighBid.Controls.Add(allTimeHighBidPrice, 0, 0);
            allTimeHighBid.Controls.Add(allTimeHighBidTime, 0, 1);
            allTimeHighBid.Dock = DockStyle.Fill;
            allTimeHighBid.Font = new Font("Segoe UI", 7F);
            allTimeHighBid.Location = new Point(103, 36);
            allTimeHighBid.Name = "allTimeHighBid";
            allTimeHighBid.RowStyles.Add(new RowStyle());
            allTimeHighBid.RowStyles.Add(new RowStyle());
            allTimeHighBid.Size = new Size(48, 30);
            allTimeHighBid.TabIndex = 5;
            // 
            // allTimeHighBidPrice
            // 
            allTimeHighBidPrice.AutoSize = true;
            allTimeHighBidPrice.Dock = DockStyle.Fill;
            allTimeHighBidPrice.Font = new Font("Segoe UI", 7F);
            allTimeHighBidPrice.Location = new Point(3, 0);
            allTimeHighBidPrice.Name = "allTimeHighBidPrice";
            allTimeHighBidPrice.Size = new Size(42, 12);
            allTimeHighBidPrice.TabIndex = 0;
            allTimeHighBidPrice.Text = "--";
            allTimeHighBidPrice.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // allTimeHighBidTime
            // 
            allTimeHighBidTime.AutoSize = true;
            allTimeHighBidTime.Dock = DockStyle.Fill;
            allTimeHighBidTime.Font = new Font("Segoe UI", 7F);
            allTimeHighBidTime.Location = new Point(3, 12);
            allTimeHighBidTime.Name = "allTimeHighBidTime";
            allTimeHighBidTime.Size = new Size(42, 18);
            allTimeHighBidTime.TabIndex = 1;
            allTimeHighBidTime.Text = "--";
            allTimeHighBidTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // recentHighLabel
            // 
            recentHighLabel.AutoSize = true;
            recentHighLabel.Dock = DockStyle.Fill;
            recentHighLabel.Font = new Font("Segoe UI", 7F);
            recentHighLabel.Location = new Point(3, 69);
            recentHighLabel.Name = "recentHighLabel";
            recentHighLabel.Size = new Size(39, 32);
            recentHighLabel.TabIndex = 6;
            recentHighLabel.Text = "Recent High";
            // 
            // recentHighAsk
            // 
            recentHighAsk.AutoSize = true;
            recentHighAsk.ColumnStyles.Add(new ColumnStyle());
            recentHighAsk.Controls.Add(recentHighAskPrice, 0, 0);
            recentHighAsk.Controls.Add(recentHighAskTime, 0, 1);
            recentHighAsk.Dock = DockStyle.Fill;
            recentHighAsk.Location = new Point(48, 72);
            recentHighAsk.Name = "recentHighAsk";
            recentHighAsk.RowStyles.Add(new RowStyle());
            recentHighAsk.RowStyles.Add(new RowStyle());
            recentHighAsk.Size = new Size(49, 26);
            recentHighAsk.TabIndex = 7;
            // 
            // recentHighAskPrice
            // 
            recentHighAskPrice.AutoSize = true;
            recentHighAskPrice.Dock = DockStyle.Fill;
            recentHighAskPrice.Font = new Font("Segoe UI", 7F);
            recentHighAskPrice.Location = new Point(3, 0);
            recentHighAskPrice.Name = "recentHighAskPrice";
            recentHighAskPrice.Size = new Size(43, 12);
            recentHighAskPrice.TabIndex = 0;
            recentHighAskPrice.Text = "--";
            recentHighAskPrice.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // recentHighAskTime
            // 
            recentHighAskTime.AutoSize = true;
            recentHighAskTime.Dock = DockStyle.Fill;
            recentHighAskTime.Font = new Font("Segoe UI", 7F);
            recentHighAskTime.Location = new Point(3, 12);
            recentHighAskTime.Name = "recentHighAskTime";
            recentHighAskTime.Size = new Size(43, 14);
            recentHighAskTime.TabIndex = 1;
            recentHighAskTime.Text = "--";
            recentHighAskTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // recentHighBid
            // 
            recentHighBid.AutoSize = true;
            recentHighBid.ColumnStyles.Add(new ColumnStyle());
            recentHighBid.Controls.Add(recentHighBidPrice, 0, 0);
            recentHighBid.Controls.Add(recentHighBidTime, 0, 1);
            recentHighBid.Dock = DockStyle.Fill;
            recentHighBid.Location = new Point(103, 72);
            recentHighBid.Name = "recentHighBid";
            recentHighBid.RowStyles.Add(new RowStyle());
            recentHighBid.RowStyles.Add(new RowStyle());
            recentHighBid.Size = new Size(48, 26);
            recentHighBid.TabIndex = 8;
            // 
            // recentHighBidPrice
            // 
            recentHighBidPrice.AutoSize = true;
            recentHighBidPrice.Dock = DockStyle.Fill;
            recentHighBidPrice.Font = new Font("Segoe UI", 7F);
            recentHighBidPrice.Location = new Point(3, 0);
            recentHighBidPrice.Name = "recentHighBidPrice";
            recentHighBidPrice.Size = new Size(42, 12);
            recentHighBidPrice.TabIndex = 0;
            recentHighBidPrice.Text = "--";
            recentHighBidPrice.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // recentHighBidTime
            // 
            recentHighBidTime.AutoSize = true;
            recentHighBidTime.Dock = DockStyle.Fill;
            recentHighBidTime.Font = new Font("Segoe UI", 7F);
            recentHighBidTime.Location = new Point(3, 12);
            recentHighBidTime.Name = "recentHighBidTime";
            recentHighBidTime.Size = new Size(42, 14);
            recentHighBidTime.TabIndex = 1;
            recentHighBidTime.Text = "--";
            recentHighBidTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // currentPriceLabel
            // 
            currentPriceLabel.AutoSize = true;
            currentPriceLabel.Dock = DockStyle.Fill;
            currentPriceLabel.Font = new Font("Segoe UI", 7F);
            currentPriceLabel.Location = new Point(3, 101);
            currentPriceLabel.Name = "currentPriceLabel";
            currentPriceLabel.Size = new Size(39, 24);
            currentPriceLabel.TabIndex = 9;
            currentPriceLabel.Text = "Current";
            // 
            // currentPriceAsk
            // 
            currentPriceAsk.AutoSize = true;
            currentPriceAsk.Dock = DockStyle.Fill;
            currentPriceAsk.Font = new Font("Segoe UI", 7F);
            currentPriceAsk.Location = new Point(48, 101);
            currentPriceAsk.Name = "currentPriceAsk";
            currentPriceAsk.Size = new Size(49, 24);
            currentPriceAsk.TabIndex = 10;
            currentPriceAsk.Text = "--";
            currentPriceAsk.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // currentPriceBid
            // 
            currentPriceBid.AutoSize = true;
            currentPriceBid.Dock = DockStyle.Fill;
            currentPriceBid.Font = new Font("Segoe UI", 7F);
            currentPriceBid.Location = new Point(103, 101);
            currentPriceBid.Name = "currentPriceBid";
            currentPriceBid.Size = new Size(48, 24);
            currentPriceBid.TabIndex = 11;
            currentPriceBid.Text = "--";
            currentPriceBid.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // recentLowLabel
            // 
            recentLowLabel.AutoSize = true;
            recentLowLabel.Dock = DockStyle.Fill;
            recentLowLabel.Font = new Font("Segoe UI", 7F);
            recentLowLabel.Location = new Point(3, 125);
            recentLowLabel.Name = "recentLowLabel";
            recentLowLabel.Size = new Size(39, 30);
            recentLowLabel.TabIndex = 12;
            recentLowLabel.Text = "Recent Low";
            // 
            // recentLowAsk
            // 
            recentLowAsk.AutoSize = true;
            recentLowAsk.ColumnStyles.Add(new ColumnStyle());
            recentLowAsk.Controls.Add(recentLowAskPrice, 0, 0);
            recentLowAsk.Controls.Add(recentLowAskTime, 0, 1);
            recentLowAsk.Dock = DockStyle.Fill;
            recentLowAsk.Location = new Point(48, 128);
            recentLowAsk.Name = "recentLowAsk";
            recentLowAsk.RowStyles.Add(new RowStyle());
            recentLowAsk.RowStyles.Add(new RowStyle());
            recentLowAsk.Size = new Size(49, 24);
            recentLowAsk.TabIndex = 13;
            // 
            // recentLowAskPrice
            // 
            recentLowAskPrice.AutoSize = true;
            recentLowAskPrice.Dock = DockStyle.Fill;
            recentLowAskPrice.Font = new Font("Segoe UI", 7F);
            recentLowAskPrice.Location = new Point(3, 0);
            recentLowAskPrice.Name = "recentLowAskPrice";
            recentLowAskPrice.Size = new Size(43, 12);
            recentLowAskPrice.TabIndex = 0;
            recentLowAskPrice.Text = "--";
            recentLowAskPrice.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // recentLowAskTime
            // 
            recentLowAskTime.AutoSize = true;
            recentLowAskTime.Dock = DockStyle.Fill;
            recentLowAskTime.Font = new Font("Segoe UI", 7F);
            recentLowAskTime.Location = new Point(3, 12);
            recentLowAskTime.Name = "recentLowAskTime";
            recentLowAskTime.Size = new Size(43, 12);
            recentLowAskTime.TabIndex = 1;
            recentLowAskTime.Text = "--";
            recentLowAskTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // recentLowBid
            // 
            recentLowBid.AutoSize = true;
            recentLowBid.ColumnStyles.Add(new ColumnStyle());
            recentLowBid.Controls.Add(recentLowBidPrice, 0, 0);
            recentLowBid.Controls.Add(recentLowBidTime, 0, 1);
            recentLowBid.Dock = DockStyle.Fill;
            recentLowBid.Location = new Point(103, 128);
            recentLowBid.Name = "recentLowBid";
            recentLowBid.RowStyles.Add(new RowStyle());
            recentLowBid.RowStyles.Add(new RowStyle());
            recentLowBid.Size = new Size(48, 24);
            recentLowBid.TabIndex = 14;
            // 
            // recentLowBidPrice
            // 
            recentLowBidPrice.AutoSize = true;
            recentLowBidPrice.Dock = DockStyle.Fill;
            recentLowBidPrice.Font = new Font("Segoe UI", 7F);
            recentLowBidPrice.Location = new Point(3, 0);
            recentLowBidPrice.Name = "recentLowBidPrice";
            recentLowBidPrice.Size = new Size(42, 12);
            recentLowBidPrice.TabIndex = 0;
            recentLowBidPrice.Text = "--";
            recentLowBidPrice.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // recentLowBidTime
            // 
            recentLowBidTime.AutoSize = true;
            recentLowBidTime.Dock = DockStyle.Fill;
            recentLowBidTime.Font = new Font("Segoe UI", 7F);
            recentLowBidTime.Location = new Point(3, 12);
            recentLowBidTime.Name = "recentLowBidTime";
            recentLowBidTime.Size = new Size(42, 12);
            recentLowBidTime.TabIndex = 1;
            recentLowBidTime.Text = "--";
            recentLowBidTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // allTimeLowLabel
            // 
            allTimeLowLabel.AutoSize = true;
            allTimeLowLabel.Dock = DockStyle.Fill;
            allTimeLowLabel.Font = new Font("Segoe UI", 7F);
            allTimeLowLabel.Location = new Point(3, 155);
            allTimeLowLabel.Name = "allTimeLowLabel";
            allTimeLowLabel.Size = new Size(39, 39);
            allTimeLowLabel.TabIndex = 15;
            allTimeLowLabel.Text = "All Time Low";
            // 
            // allTimeLowAsk
            // 
            allTimeLowAsk.AutoSize = true;
            allTimeLowAsk.ColumnStyles.Add(new ColumnStyle());
            allTimeLowAsk.Controls.Add(allTimeLowAskPrice, 0, 0);
            allTimeLowAsk.Controls.Add(allTimeLowAskTime, 0, 1);
            allTimeLowAsk.Dock = DockStyle.Fill;
            allTimeLowAsk.Location = new Point(48, 158);
            allTimeLowAsk.Name = "allTimeLowAsk";
            allTimeLowAsk.RowStyles.Add(new RowStyle());
            allTimeLowAsk.RowStyles.Add(new RowStyle());
            allTimeLowAsk.Size = new Size(49, 33);
            allTimeLowAsk.TabIndex = 16;
            // 
            // allTimeLowAskPrice
            // 
            allTimeLowAskPrice.AutoSize = true;
            allTimeLowAskPrice.Dock = DockStyle.Fill;
            allTimeLowAskPrice.Font = new Font("Segoe UI", 7F);
            allTimeLowAskPrice.Location = new Point(3, 0);
            allTimeLowAskPrice.Name = "allTimeLowAskPrice";
            allTimeLowAskPrice.Size = new Size(43, 12);
            allTimeLowAskPrice.TabIndex = 0;
            allTimeLowAskPrice.Text = "--";
            allTimeLowAskPrice.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // allTimeLowAskTime
            // 
            allTimeLowAskTime.AutoSize = true;
            allTimeLowAskTime.Dock = DockStyle.Fill;
            allTimeLowAskTime.Font = new Font("Segoe UI", 7F);
            allTimeLowAskTime.Location = new Point(3, 12);
            allTimeLowAskTime.Name = "allTimeLowAskTime";
            allTimeLowAskTime.Size = new Size(43, 21);
            allTimeLowAskTime.TabIndex = 1;
            allTimeLowAskTime.Text = "--";
            allTimeLowAskTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // allTimeLowBid
            // 
            allTimeLowBid.AutoSize = true;
            allTimeLowBid.ColumnStyles.Add(new ColumnStyle());
            allTimeLowBid.Controls.Add(allTimeLowBidPrice, 0, 0);
            allTimeLowBid.Controls.Add(allTimeLowBidTime, 0, 1);
            allTimeLowBid.Dock = DockStyle.Fill;
            allTimeLowBid.Location = new Point(103, 158);
            allTimeLowBid.Name = "allTimeLowBid";
            allTimeLowBid.RowStyles.Add(new RowStyle());
            allTimeLowBid.RowStyles.Add(new RowStyle());
            allTimeLowBid.Size = new Size(48, 33);
            allTimeLowBid.TabIndex = 17;
            // 
            // allTimeLowBidPrice
            // 
            allTimeLowBidPrice.AutoSize = true;
            allTimeLowBidPrice.Dock = DockStyle.Fill;
            allTimeLowBidPrice.Font = new Font("Segoe UI", 7F);
            allTimeLowBidPrice.Location = new Point(3, 0);
            allTimeLowBidPrice.Name = "allTimeLowBidPrice";
            allTimeLowBidPrice.Size = new Size(42, 12);
            allTimeLowBidPrice.TabIndex = 0;
            allTimeLowBidPrice.Text = "--";
            allTimeLowBidPrice.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // allTimeLowBidTime
            // 
            allTimeLowBidTime.AutoSize = true;
            allTimeLowBidTime.Dock = DockStyle.Fill;
            allTimeLowBidTime.Font = new Font("Segoe UI", 7F);
            allTimeLowBidTime.Location = new Point(3, 12);
            allTimeLowBidTime.Name = "allTimeLowBidTime";
            allTimeLowBidTime.Size = new Size(42, 21);
            allTimeLowBidTime.TabIndex = 1;
            allTimeLowBidTime.Text = "--";
            allTimeLowBidTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tradingMetricsGrid
            // 
            tradingMetricsGrid.AutoSize = true;
            tradingMetricsGrid.ColumnCount = 2;
            tradingMetricsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tradingMetricsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            tradingMetricsGrid.Controls.Add(tradingMetricsHeader, 0, 0);
            tradingMetricsGrid.Controls.Add(rsiLabel, 0, 1);
            tradingMetricsGrid.Controls.Add(rsiValue, 1, 1);
            tradingMetricsGrid.Controls.Add(macdLabel, 0, 2);
            tradingMetricsGrid.Controls.Add(macdValue, 1, 2);
            tradingMetricsGrid.Controls.Add(emaLabel, 0, 3);
            tradingMetricsGrid.Controls.Add(emaValue, 1, 3);
            tradingMetricsGrid.Controls.Add(bollingerLabel, 0, 4);
            tradingMetricsGrid.Controls.Add(bollingerValue, 1, 4);
            tradingMetricsGrid.Controls.Add(atrLabel, 0, 5);
            tradingMetricsGrid.Controls.Add(atrValue, 1, 5);
            tradingMetricsGrid.Controls.Add(vwapLabel, 0, 6);
            tradingMetricsGrid.Controls.Add(vwapValue, 1, 6);
            tradingMetricsGrid.Controls.Add(stochasticLabel, 0, 7);
            tradingMetricsGrid.Controls.Add(stochasticValue, 1, 7);
            tradingMetricsGrid.Controls.Add(obvLabel, 0, 8);
            tradingMetricsGrid.Controls.Add(obvValue, 1, 8);
            tradingMetricsGrid.Controls.Add(psarLabel, 0, 9);
            tradingMetricsGrid.Controls.Add(psarValue, 1, 9);
            tradingMetricsGrid.Controls.Add(adxLabel, 0, 10);
            tradingMetricsGrid.Controls.Add(adxValue, 1, 10);
            tradingMetricsGrid.Dock = DockStyle.Fill;
            tradingMetricsGrid.Location = new Point(3, 203);
            tradingMetricsGrid.Name = "tradingMetricsGrid";
            tradingMetricsGrid.RowCount = 11;
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 9.09F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 9.09F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 9.09F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 9.09F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 9.09F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 9.09F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 9.09F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 9.09F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 9.09F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 9.09F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 9.09F));
            tradingMetricsGrid.Size = new Size(154, 220);
            tradingMetricsGrid.TabIndex = 1;
            // 
            // tradingMetricsHeader
            // 
            tradingMetricsHeader.AutoSize = true;
            tradingMetricsGrid.SetColumnSpan(tradingMetricsHeader, 2);
            tradingMetricsHeader.Dock = DockStyle.Fill;
            tradingMetricsHeader.Location = new Point(3, 0);
            tradingMetricsHeader.Name = "tradingMetricsHeader";
            tradingMetricsHeader.Size = new Size(148, 20);
            tradingMetricsHeader.TabIndex = 0;
            tradingMetricsHeader.Text = "Market Header";
            // 
            // rsiLabel
            // 
            rsiLabel.AutoSize = true;
            rsiLabel.Dock = DockStyle.Fill;
            rsiLabel.Font = new Font("Segoe UI", 7F);
            rsiLabel.Location = new Point(3, 23);
            rsiLabel.Name = "rsiLabel";
            rsiLabel.Size = new Size(86, 14);
            rsiLabel.TabIndex = 1;
            rsiLabel.Text = "RSI:";
            // 
            // rsiValue
            // 
            rsiValue.AutoSize = true;
            rsiValue.Dock = DockStyle.Fill;
            rsiValue.Font = new Font("Segoe UI", 7F);
            rsiValue.Location = new Point(95, 20);
            rsiValue.Name = "rsiValue";
            rsiValue.Size = new Size(56, 20);
            rsiValue.TabIndex = 2;
            rsiValue.Text = "--";
            // 
            // macdLabel
            // 
            macdLabel.AutoSize = true;
            macdLabel.Dock = DockStyle.Fill;
            macdLabel.Font = new Font("Segoe UI", 7F);
            macdLabel.Location = new Point(3, 43);
            macdLabel.Name = "macdLabel";
            macdLabel.Size = new Size(86, 14);
            macdLabel.TabIndex = 3;
            macdLabel.Text = "MACD:";
            // 
            // macdValue
            // 
            macdValue.AutoSize = true;
            macdValue.Dock = DockStyle.Fill;
            macdValue.Font = new Font("Segoe UI", 7F);
            macdValue.Location = new Point(95, 40);
            macdValue.Name = "macdValue";
            macdValue.Size = new Size(56, 20);
            macdValue.TabIndex = 4;
            macdValue.Text = "--";
            // 
            // emaLabel
            // 
            emaLabel.AutoSize = true;
            emaLabel.Dock = DockStyle.Fill;
            emaLabel.Font = new Font("Segoe UI", 7F);
            emaLabel.Location = new Point(3, 63);
            emaLabel.Name = "emaLabel";
            emaLabel.Size = new Size(86, 14);
            emaLabel.TabIndex = 5;
            emaLabel.Text = "EMA:";
            // 
            // emaValue
            // 
            emaValue.AutoSize = true;
            emaValue.Dock = DockStyle.Fill;
            emaValue.Font = new Font("Segoe UI", 7F);
            emaValue.Location = new Point(95, 60);
            emaValue.Name = "emaValue";
            emaValue.Size = new Size(56, 20);
            emaValue.TabIndex = 6;
            emaValue.Text = "--";
            // 
            // bollingerLabel
            // 
            bollingerLabel.AutoSize = true;
            bollingerLabel.Dock = DockStyle.Fill;
            bollingerLabel.Font = new Font("Segoe UI", 7F);
            bollingerLabel.Location = new Point(3, 83);
            bollingerLabel.Name = "bollingerLabel";
            bollingerLabel.Size = new Size(86, 14);
            bollingerLabel.TabIndex = 7;
            bollingerLabel.Text = "Bollinger:";
            // 
            // bollingerValue
            // 
            bollingerValue.AutoSize = true;
            bollingerValue.Dock = DockStyle.Fill;
            bollingerValue.Font = new Font("Segoe UI", 7F);
            bollingerValue.Location = new Point(95, 80);
            bollingerValue.Name = "bollingerValue";
            bollingerValue.Size = new Size(56, 20);
            bollingerValue.TabIndex = 8;
            bollingerValue.Text = "--";
            // 
            // atrLabel
            // 
            atrLabel.AutoSize = true;
            atrLabel.Dock = DockStyle.Fill;
            atrLabel.Font = new Font("Segoe UI", 7F);
            atrLabel.Location = new Point(3, 103);
            atrLabel.Name = "atrLabel";
            atrLabel.Size = new Size(86, 14);
            atrLabel.TabIndex = 9;
            atrLabel.Text = "ATR:";
            // 
            // atrValue
            // 
            atrValue.AutoSize = true;
            atrValue.Dock = DockStyle.Fill;
            atrValue.Font = new Font("Segoe UI", 7F);
            atrValue.Location = new Point(95, 100);
            atrValue.Name = "atrValue";
            atrValue.Size = new Size(56, 20);
            atrValue.TabIndex = 10;
            atrValue.Text = "--";
            // 
            // vwapLabel
            // 
            vwapLabel.AutoSize = true;
            vwapLabel.Dock = DockStyle.Fill;
            vwapLabel.Font = new Font("Segoe UI", 7F);
            vwapLabel.Location = new Point(3, 123);
            vwapLabel.Name = "vwapLabel";
            vwapLabel.Size = new Size(86, 14);
            vwapLabel.TabIndex = 11;
            vwapLabel.Text = "VWAP:";
            // 
            // vwapValue
            // 
            vwapValue.AutoSize = true;
            vwapValue.Dock = DockStyle.Fill;
            vwapValue.Font = new Font("Segoe UI", 7F);
            vwapValue.Location = new Point(95, 120);
            vwapValue.Name = "vwapValue";
            vwapValue.Size = new Size(56, 20);
            vwapValue.TabIndex = 12;
            vwapValue.Text = "--";
            // 
            // stochasticLabel
            // 
            stochasticLabel.AutoSize = true;
            stochasticLabel.Dock = DockStyle.Fill;
            stochasticLabel.Font = new Font("Segoe UI", 7F);
            stochasticLabel.Location = new Point(3, 143);
            stochasticLabel.Name = "stochasticLabel";
            stochasticLabel.Size = new Size(86, 14);
            stochasticLabel.TabIndex = 13;
            stochasticLabel.Text = "Stochastic:";
            // 
            // stochasticValue
            // 
            stochasticValue.AutoSize = true;
            stochasticValue.Dock = DockStyle.Fill;
            stochasticValue.Font = new Font("Segoe UI", 7F);
            stochasticValue.Location = new Point(95, 140);
            stochasticValue.Name = "stochasticValue";
            stochasticValue.Size = new Size(56, 20);
            stochasticValue.TabIndex = 14;
            stochasticValue.Text = "--";
            // 
            // obvLabel
            // 
            obvLabel.AutoSize = true;
            obvLabel.Dock = DockStyle.Fill;
            obvLabel.Font = new Font("Segoe UI", 7F);
            obvLabel.Location = new Point(3, 163);
            obvLabel.Name = "obvLabel";
            obvLabel.Size = new Size(86, 14);
            obvLabel.TabIndex = 15;
            obvLabel.Text = "OBV:";
            // 
            // obvValue
            // 
            obvValue.AutoSize = true;
            obvValue.Dock = DockStyle.Fill;
            obvValue.Font = new Font("Segoe UI", 7F);
            obvValue.Location = new Point(95, 160);
            obvValue.Name = "obvValue";
            obvValue.Size = new Size(56, 20);
            obvValue.TabIndex = 16;
            obvValue.Text = "--";
            // 
            // psarLabel
            // 
            psarLabel.AutoSize = true;
            psarLabel.Dock = DockStyle.Fill;
            psarLabel.Font = new Font("Segoe UI", 7F);
            psarLabel.Location = new Point(3, 183);
            psarLabel.Name = "psarLabel";
            psarLabel.Size = new Size(86, 14);
            psarLabel.TabIndex = 17;
            psarLabel.Text = "PSAR:";
            // 
            // psarValue
            // 
            psarValue.AutoSize = true;
            psarValue.Dock = DockStyle.Fill;
            psarValue.Font = new Font("Segoe UI", 7F);
            psarValue.Location = new Point(95, 180);
            psarValue.Name = "psarValue";
            psarValue.Size = new Size(56, 20);
            psarValue.TabIndex = 18;
            psarValue.Text = "--";
            // 
            // adxLabel
            // 
            adxLabel.AutoSize = true;
            adxLabel.Dock = DockStyle.Fill;
            adxLabel.Font = new Font("Segoe UI", 7F);
            adxLabel.Location = new Point(3, 203);
            adxLabel.Name = "adxLabel";
            adxLabel.Size = new Size(86, 14);
            adxLabel.TabIndex = 19;
            adxLabel.Text = "ADX:";
            // 
            // adxValue
            // 
            adxValue.AutoSize = true;
            adxValue.Dock = DockStyle.Fill;
            adxValue.Font = new Font("Segoe UI", 7F);
            adxValue.Location = new Point(95, 200);
            adxValue.Name = "adxValue";
            adxValue.Size = new Size(56, 20);
            adxValue.TabIndex = 20;
            adxValue.Text = "--";
            // 
            // rightColumn
            // 
            rightColumn.AutoSize = true;
            rightColumn.ColumnCount = 1;
            rightColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rightColumn.Controls.Add(otherInfoGrid, 0, 0);
            rightColumn.Controls.Add(flowMomentumGrid, 0, 1);
            rightColumn.Controls.Add(contextGrid, 0, 2);
            rightColumn.Dock = DockStyle.Fill;
            rightColumn.Location = new Point(169, 3);
            rightColumn.Name = "rightColumn";
            rightColumn.RowCount = 3;
            rightColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            rightColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            rightColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            rightColumn.Size = new Size(130, 426);
            rightColumn.TabIndex = 1;
            // 
            // otherInfoGrid
            // 
            otherInfoGrid.AutoSize = true;
            otherInfoGrid.ColumnCount = 2;
            otherInfoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            otherInfoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            otherInfoGrid.Controls.Add(CategoryLabel, 0, 0);
            otherInfoGrid.Controls.Add(categoryValue, 1, 0);
            otherInfoGrid.Controls.Add(timeLeftLabel, 0, 1);
            otherInfoGrid.Controls.Add(timeLeftValue, 1, 1);
            otherInfoGrid.Controls.Add(marketAgeLabel, 0, 2);
            otherInfoGrid.Controls.Add(marketAgeValue, 1, 2);
            otherInfoGrid.Dock = DockStyle.Fill;
            otherInfoGrid.Location = new Point(3, 3);
            otherInfoGrid.Name = "otherInfoGrid";
            otherInfoGrid.RowCount = 3;
            otherInfoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            otherInfoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            otherInfoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            otherInfoGrid.Size = new Size(124, 79);
            otherInfoGrid.TabIndex = 0;
            // 
            // CategoryLabel
            // 
            CategoryLabel.AutoSize = true;
            CategoryLabel.Dock = DockStyle.Fill;
            CategoryLabel.Font = new Font("Segoe UI", 7F);
            CategoryLabel.Location = new Point(3, 0);
            CategoryLabel.Name = "CategoryLabel";
            CategoryLabel.Size = new Size(68, 26);
            CategoryLabel.TabIndex = 0;
            CategoryLabel.Text = "Category:";
            // 
            // categoryValue
            // 
            categoryValue.AutoSize = true;
            categoryValue.Dock = DockStyle.Fill;
            categoryValue.Font = new Font("Segoe UI", 7F);
            categoryValue.Location = new Point(77, 0);
            categoryValue.Name = "categoryValue";
            categoryValue.Size = new Size(44, 26);
            categoryValue.TabIndex = 1;
            categoryValue.Text = "--";
            categoryValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // timeLeftLabel
            // 
            timeLeftLabel.AutoSize = true;
            timeLeftLabel.Dock = DockStyle.Fill;
            timeLeftLabel.Font = new Font("Segoe UI", 7F);
            timeLeftLabel.Location = new Point(3, 26);
            timeLeftLabel.Name = "timeLeftLabel";
            timeLeftLabel.Size = new Size(68, 26);
            timeLeftLabel.TabIndex = 2;
            timeLeftLabel.Text = "Time Left:";
            // 
            // timeLeftValue
            // 
            timeLeftValue.AutoSize = true;
            timeLeftValue.Dock = DockStyle.Fill;
            timeLeftValue.Font = new Font("Segoe UI", 7F);
            timeLeftValue.Location = new Point(77, 26);
            timeLeftValue.Name = "timeLeftValue";
            timeLeftValue.Size = new Size(44, 26);
            timeLeftValue.TabIndex = 3;
            timeLeftValue.Text = "--";
            timeLeftValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // marketAgeLabel
            // 
            marketAgeLabel.AutoSize = true;
            marketAgeLabel.Dock = DockStyle.Fill;
            marketAgeLabel.Font = new Font("Segoe UI", 7F);
            marketAgeLabel.Location = new Point(3, 52);
            marketAgeLabel.Name = "marketAgeLabel";
            marketAgeLabel.Size = new Size(68, 27);
            marketAgeLabel.TabIndex = 4;
            marketAgeLabel.Text = "Market Age:";
            // 
            // marketAgeValue
            // 
            marketAgeValue.AutoSize = true;
            marketAgeValue.Dock = DockStyle.Fill;
            marketAgeValue.Font = new Font("Segoe UI", 7F);
            marketAgeValue.Location = new Point(77, 52);
            marketAgeValue.Name = "marketAgeValue";
            marketAgeValue.Size = new Size(44, 27);
            marketAgeValue.TabIndex = 5;
            marketAgeValue.Text = "--";
            marketAgeValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // flowMomentumGrid
            // 
            flowMomentumGrid.AutoSize = true;
            flowMomentumGrid.ColumnCount = 3;
            flowMomentumGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            flowMomentumGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            flowMomentumGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            flowMomentumGrid.Controls.Add(label1, 0, 0);
            flowMomentumGrid.Controls.Add(flowHeader, 0, 0);
            flowMomentumGrid.Controls.Add(flowHeaderYes, 1, 0);
            flowMomentumGrid.Controls.Add(flowHeaderNo, 2, 0);
            flowMomentumGrid.Controls.Add(topVelocityLabel, 0, 1);
            flowMomentumGrid.Controls.Add(topVelocityYesValue, 1, 1);
            flowMomentumGrid.Controls.Add(topVelocityNoValue, 2, 1);
            flowMomentumGrid.Controls.Add(bottomVelocityLabel, 0, 2);
            flowMomentumGrid.Controls.Add(bottomVelocityYesValue, 1, 2);
            flowMomentumGrid.Controls.Add(bottomVelocityNoValue, 2, 2);
            flowMomentumGrid.Controls.Add(netOrderRateLabel, 0, 3);
            flowMomentumGrid.Controls.Add(netOrderRateYesValue, 1, 3);
            flowMomentumGrid.Controls.Add(netOrderRateNoValue, 2, 3);
            flowMomentumGrid.Controls.Add(tradeVolumeLabel, 0, 4);
            flowMomentumGrid.Controls.Add(tradeVolumeYesValue, 1, 4);
            flowMomentumGrid.Controls.Add(tradeVolumeNoValue, 2, 4);
            flowMomentumGrid.Controls.Add(avgTradeSizeLabel, 0, 5);
            flowMomentumGrid.Controls.Add(avgTradeSizeYesValue, 1, 5);
            flowMomentumGrid.Controls.Add(avgTradeSizeNoValue, 2, 5);
            flowMomentumGrid.Controls.Add(slopeLabel, 0, 6);
            flowMomentumGrid.Controls.Add(slopeYesValue, 1, 6);
            flowMomentumGrid.Controls.Add(slopeNoValue, 2, 6);
            flowMomentumGrid.Dock = DockStyle.Fill;
            flowMomentumGrid.Location = new Point(3, 88);
            flowMomentumGrid.Name = "flowMomentumGrid";
            flowMomentumGrid.RowCount = 7;
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            flowMomentumGrid.Size = new Size(124, 164);
            flowMomentumGrid.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Dock = DockStyle.Fill;
            label1.Location = new Point(3, 20);
            label1.Name = "label1";
            label1.Size = new Size(56, 20);
            label1.TabIndex = 21;
            // 
            // flowHeader
            // 
            flowHeader.AutoSize = true;
            flowMomentumGrid.SetColumnSpan(flowHeader, 3);
            flowHeader.Dock = DockStyle.Fill;
            flowHeader.Font = new Font("Segoe UI", 7F);
            flowHeader.Location = new Point(3, 0);
            flowHeader.Name = "flowHeader";
            flowHeader.Size = new Size(118, 20);
            flowHeader.TabIndex = 0;
            flowHeader.Text = "Flow/Momentum";
            // 
            // flowHeaderYes
            // 
            flowHeaderYes.AutoSize = true;
            flowHeaderYes.Dock = DockStyle.Fill;
            flowHeaderYes.Font = new Font("Segoe UI", 7F);
            flowHeaderYes.Location = new Point(65, 20);
            flowHeaderYes.Name = "flowHeaderYes";
            flowHeaderYes.Size = new Size(25, 20);
            flowHeaderYes.TabIndex = 1;
            flowHeaderYes.Text = "Yes";
            // 
            // flowHeaderNo
            // 
            flowHeaderNo.AutoSize = true;
            flowHeaderNo.Dock = DockStyle.Fill;
            flowHeaderNo.Font = new Font("Segoe UI", 7F);
            flowHeaderNo.Location = new Point(96, 20);
            flowHeaderNo.Name = "flowHeaderNo";
            flowHeaderNo.Size = new Size(25, 20);
            flowHeaderNo.TabIndex = 2;
            flowHeaderNo.Text = "No";
            // 
            // topVelocityLabel
            // 
            topVelocityLabel.AutoSize = true;
            topVelocityLabel.Dock = DockStyle.Fill;
            topVelocityLabel.Font = new Font("Segoe UI", 7F);
            topVelocityLabel.Location = new Point(3, 43);
            topVelocityLabel.Name = "topVelocityLabel";
            topVelocityLabel.Size = new Size(56, 14);
            topVelocityLabel.TabIndex = 3;
            topVelocityLabel.Text = "Top Velocity:";
            // 
            // topVelocityYesValue
            // 
            topVelocityYesValue.AutoSize = true;
            topVelocityYesValue.Dock = DockStyle.Fill;
            topVelocityYesValue.Font = new Font("Segoe UI", 7F);
            topVelocityYesValue.Location = new Point(65, 40);
            topVelocityYesValue.Name = "topVelocityYesValue";
            topVelocityYesValue.Size = new Size(25, 20);
            topVelocityYesValue.TabIndex = 4;
            topVelocityYesValue.Text = "--";
            topVelocityYesValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // topVelocityNoValue
            // 
            topVelocityNoValue.AutoSize = true;
            topVelocityNoValue.Dock = DockStyle.Fill;
            topVelocityNoValue.Font = new Font("Segoe UI", 7F);
            topVelocityNoValue.Location = new Point(96, 40);
            topVelocityNoValue.Name = "topVelocityNoValue";
            topVelocityNoValue.Size = new Size(25, 20);
            topVelocityNoValue.TabIndex = 5;
            topVelocityNoValue.Text = "--";
            topVelocityNoValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // bottomVelocityLabel
            // 
            bottomVelocityLabel.AutoSize = true;
            bottomVelocityLabel.Dock = DockStyle.Fill;
            bottomVelocityLabel.Font = new Font("Segoe UI", 7F);
            bottomVelocityLabel.Location = new Point(3, 63);
            bottomVelocityLabel.Name = "bottomVelocityLabel";
            bottomVelocityLabel.Size = new Size(56, 14);
            bottomVelocityLabel.TabIndex = 6;
            bottomVelocityLabel.Text = "Bottom Velocity:";
            // 
            // bottomVelocityYesValue
            // 
            bottomVelocityYesValue.AutoSize = true;
            bottomVelocityYesValue.Dock = DockStyle.Fill;
            bottomVelocityYesValue.Font = new Font("Segoe UI", 7F);
            bottomVelocityYesValue.Location = new Point(65, 60);
            bottomVelocityYesValue.Name = "bottomVelocityYesValue";
            bottomVelocityYesValue.Size = new Size(25, 20);
            bottomVelocityYesValue.TabIndex = 7;
            bottomVelocityYesValue.Text = "--";
            bottomVelocityYesValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // bottomVelocityNoValue
            // 
            bottomVelocityNoValue.AutoSize = true;
            bottomVelocityNoValue.Dock = DockStyle.Fill;
            bottomVelocityNoValue.Font = new Font("Segoe UI", 7F);
            bottomVelocityNoValue.Location = new Point(96, 60);
            bottomVelocityNoValue.Name = "bottomVelocityNoValue";
            bottomVelocityNoValue.Size = new Size(25, 20);
            bottomVelocityNoValue.TabIndex = 8;
            bottomVelocityNoValue.Text = "--";
            bottomVelocityNoValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // netOrderRateLabel
            // 
            netOrderRateLabel.AutoSize = true;
            netOrderRateLabel.Dock = DockStyle.Fill;
            netOrderRateLabel.Font = new Font("Segoe UI", 7F);
            netOrderRateLabel.Location = new Point(3, 83);
            netOrderRateLabel.Name = "netOrderRateLabel";
            netOrderRateLabel.Size = new Size(56, 14);
            netOrderRateLabel.TabIndex = 9;
            netOrderRateLabel.Text = "Net Order Rate:";
            // 
            // netOrderRateYesValue
            // 
            netOrderRateYesValue.AutoSize = true;
            netOrderRateYesValue.Dock = DockStyle.Fill;
            netOrderRateYesValue.Font = new Font("Segoe UI", 7F);
            netOrderRateYesValue.Location = new Point(65, 80);
            netOrderRateYesValue.Name = "netOrderRateYesValue";
            netOrderRateYesValue.Size = new Size(25, 20);
            netOrderRateYesValue.TabIndex = 10;
            netOrderRateYesValue.Text = "--";
            netOrderRateYesValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // netOrderRateNoValue
            // 
            netOrderRateNoValue.AutoSize = true;
            netOrderRateNoValue.Dock = DockStyle.Fill;
            netOrderRateNoValue.Font = new Font("Segoe UI", 7F);
            netOrderRateNoValue.Location = new Point(96, 80);
            netOrderRateNoValue.Name = "netOrderRateNoValue";
            netOrderRateNoValue.Size = new Size(25, 20);
            netOrderRateNoValue.TabIndex = 11;
            netOrderRateNoValue.Text = "--";
            netOrderRateNoValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tradeVolumeLabel
            // 
            tradeVolumeLabel.AutoSize = true;
            tradeVolumeLabel.Dock = DockStyle.Fill;
            tradeVolumeLabel.Font = new Font("Segoe UI", 7F);
            tradeVolumeLabel.Location = new Point(3, 103);
            tradeVolumeLabel.Name = "tradeVolumeLabel";
            tradeVolumeLabel.Size = new Size(56, 14);
            tradeVolumeLabel.TabIndex = 12;
            tradeVolumeLabel.Text = "Trade Volume:";
            // 
            // tradeVolumeYesValue
            // 
            tradeVolumeYesValue.AutoSize = true;
            tradeVolumeYesValue.Dock = DockStyle.Fill;
            tradeVolumeYesValue.Font = new Font("Segoe UI", 7F);
            tradeVolumeYesValue.Location = new Point(65, 100);
            tradeVolumeYesValue.Name = "tradeVolumeYesValue";
            tradeVolumeYesValue.Size = new Size(25, 20);
            tradeVolumeYesValue.TabIndex = 13;
            tradeVolumeYesValue.Text = "--";
            tradeVolumeYesValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tradeVolumeNoValue
            // 
            tradeVolumeNoValue.AutoSize = true;
            tradeVolumeNoValue.Dock = DockStyle.Fill;
            tradeVolumeNoValue.Font = new Font("Segoe UI", 7F);
            tradeVolumeNoValue.Location = new Point(96, 100);
            tradeVolumeNoValue.Name = "tradeVolumeNoValue";
            tradeVolumeNoValue.Size = new Size(25, 20);
            tradeVolumeNoValue.TabIndex = 14;
            tradeVolumeNoValue.Text = "--";
            tradeVolumeNoValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // avgTradeSizeLabel
            // 
            avgTradeSizeLabel.AutoSize = true;
            avgTradeSizeLabel.Dock = DockStyle.Fill;
            avgTradeSizeLabel.Font = new Font("Segoe UI", 7F);
            avgTradeSizeLabel.Location = new Point(3, 123);
            avgTradeSizeLabel.Name = "avgTradeSizeLabel";
            avgTradeSizeLabel.Size = new Size(56, 14);
            avgTradeSizeLabel.TabIndex = 15;
            avgTradeSizeLabel.Text = "Avg Trade Size:";
            // 
            // avgTradeSizeYesValue
            // 
            avgTradeSizeYesValue.AutoSize = true;
            avgTradeSizeYesValue.Dock = DockStyle.Fill;
            avgTradeSizeYesValue.Font = new Font("Segoe UI", 7F);
            avgTradeSizeYesValue.Location = new Point(65, 120);
            avgTradeSizeYesValue.Name = "avgTradeSizeYesValue";
            avgTradeSizeYesValue.Size = new Size(25, 20);
            avgTradeSizeYesValue.TabIndex = 16;
            avgTradeSizeYesValue.Text = "--";
            avgTradeSizeYesValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // avgTradeSizeNoValue
            // 
            avgTradeSizeNoValue.AutoSize = true;
            avgTradeSizeNoValue.Dock = DockStyle.Fill;
            avgTradeSizeNoValue.Font = new Font("Segoe UI", 7F);
            avgTradeSizeNoValue.Location = new Point(96, 120);
            avgTradeSizeNoValue.Name = "avgTradeSizeNoValue";
            avgTradeSizeNoValue.Size = new Size(25, 20);
            avgTradeSizeNoValue.TabIndex = 17;
            avgTradeSizeNoValue.Text = "--";
            avgTradeSizeNoValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // slopeLabel
            // 
            slopeLabel.AutoSize = true;
            slopeLabel.Dock = DockStyle.Fill;
            slopeLabel.Font = new Font("Segoe UI", 7F);
            slopeLabel.Location = new Point(3, 143);
            slopeLabel.Name = "slopeLabel";
            slopeLabel.Size = new Size(56, 18);
            slopeLabel.TabIndex = 18;
            slopeLabel.Text = "Slope:";
            // 
            // slopeYesValue
            // 
            slopeYesValue.AutoSize = true;
            slopeYesValue.Dock = DockStyle.Fill;
            slopeYesValue.Font = new Font("Segoe UI", 7F);
            slopeYesValue.Location = new Point(65, 140);
            slopeYesValue.Name = "slopeYesValue";
            slopeYesValue.Size = new Size(25, 24);
            slopeYesValue.TabIndex = 19;
            slopeYesValue.Text = "--";
            slopeYesValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // slopeNoValue
            // 
            slopeNoValue.AutoSize = true;
            slopeNoValue.Dock = DockStyle.Fill;
            slopeNoValue.Font = new Font("Segoe UI", 7F);
            slopeNoValue.Location = new Point(96, 140);
            slopeNoValue.Name = "slopeNoValue";
            slopeNoValue.Size = new Size(25, 24);
            slopeNoValue.TabIndex = 20;
            slopeNoValue.Text = "--";
            slopeNoValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // contextGrid
            // 
            contextGrid.AutoSize = true;
            contextGrid.ColumnCount = 3;
            contextGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            contextGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            contextGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            contextGrid.Controls.Add(label2, 0, 0);
            contextGrid.Controls.Add(contextHeader, 0, 0);
            contextGrid.Controls.Add(contextHeaderYes, 1, 0);
            contextGrid.Controls.Add(contextHeaderNo, 2, 0);
            contextGrid.Controls.Add(spreadLabel, 0, 1);
            contextGrid.Controls.Add(spreadValue, 1, 1);
            contextGrid.Controls.Add(imbalLabel, 0, 2);
            contextGrid.Controls.Add(imbalValue, 1, 2);
            contextGrid.Controls.Add(depthTop4Label, 0, 3);
            contextGrid.Controls.Add(depthTop4YesValue, 1, 3);
            contextGrid.Controls.Add(depthTop4NoValue, 2, 3);
            contextGrid.Controls.Add(centerMassLabel, 0, 4);
            contextGrid.Controls.Add(centerMassYesValue, 1, 4);
            contextGrid.Controls.Add(centerMassNoValue, 2, 4);
            contextGrid.Controls.Add(totalContractsLabel, 0, 5);
            contextGrid.Controls.Add(totalContractsYesValue, 1, 5);
            contextGrid.Controls.Add(totalContractsNoValue, 2, 5);
            contextGrid.Dock = DockStyle.Fill;
            contextGrid.Location = new Point(3, 258);
            contextGrid.Name = "contextGrid";
            contextGrid.RowCount = 6;
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            contextGrid.Size = new Size(124, 165);
            contextGrid.TabIndex = 2;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Dock = DockStyle.Fill;
            label2.Location = new Point(3, 23);
            label2.Name = "label2";
            label2.Size = new Size(56, 23);
            label2.TabIndex = 16;
            // 
            // contextHeader
            // 
            contextHeader.AutoSize = true;
            contextGrid.SetColumnSpan(contextHeader, 3);
            contextHeader.Dock = DockStyle.Fill;
            contextHeader.Font = new Font("Segoe UI", 7F);
            contextHeader.Location = new Point(3, 0);
            contextHeader.Name = "contextHeader";
            contextHeader.Size = new Size(118, 23);
            contextHeader.TabIndex = 0;
            contextHeader.Text = "Context";
            // 
            // contextHeaderYes
            // 
            contextHeaderYes.AutoSize = true;
            contextHeaderYes.Dock = DockStyle.Fill;
            contextHeaderYes.Font = new Font("Segoe UI", 7F);
            contextHeaderYes.Location = new Point(65, 23);
            contextHeaderYes.Name = "contextHeaderYes";
            contextHeaderYes.Size = new Size(25, 23);
            contextHeaderYes.TabIndex = 1;
            contextHeaderYes.Text = "Yes";
            // 
            // contextHeaderNo
            // 
            contextHeaderNo.AutoSize = true;
            contextHeaderNo.Dock = DockStyle.Fill;
            contextHeaderNo.Font = new Font("Segoe UI", 7F);
            contextHeaderNo.Location = new Point(96, 23);
            contextHeaderNo.Name = "contextHeaderNo";
            contextHeaderNo.Size = new Size(25, 23);
            contextHeaderNo.TabIndex = 2;
            contextHeaderNo.Text = "No";
            // 
            // spreadLabel
            // 
            spreadLabel.AutoSize = true;
            spreadLabel.Dock = DockStyle.Fill;
            spreadLabel.Font = new Font("Segoe UI", 7F);
            spreadLabel.Location = new Point(3, 46);
            spreadLabel.Name = "spreadLabel";
            spreadLabel.Size = new Size(56, 23);
            spreadLabel.TabIndex = 3;
            spreadLabel.Text = "Spread:";
            // 
            // spreadValue
            // 
            spreadValue.AutoSize = true;
            contextGrid.SetColumnSpan(spreadValue, 2);
            spreadValue.Dock = DockStyle.Fill;
            spreadValue.Font = new Font("Segoe UI", 7F);
            spreadValue.Location = new Point(65, 46);
            spreadValue.Name = "spreadValue";
            spreadValue.Size = new Size(56, 23);
            spreadValue.TabIndex = 4;
            spreadValue.Text = "--";
            spreadValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // imbalLabel
            // 
            imbalLabel.AutoSize = true;
            imbalLabel.Dock = DockStyle.Fill;
            imbalLabel.Font = new Font("Segoe UI", 7F);
            imbalLabel.Location = new Point(3, 72);
            imbalLabel.Name = "imbalLabel";
            imbalLabel.Size = new Size(56, 17);
            imbalLabel.TabIndex = 5;
            imbalLabel.Text = "Imbal:";
            // 
            // imbalValue
            // 
            imbalValue.AutoSize = true;
            contextGrid.SetColumnSpan(imbalValue, 2);
            imbalValue.Dock = DockStyle.Fill;
            imbalValue.Font = new Font("Segoe UI", 7F);
            imbalValue.Location = new Point(65, 69);
            imbalValue.Name = "imbalValue";
            imbalValue.Size = new Size(56, 23);
            imbalValue.TabIndex = 6;
            imbalValue.Text = "--";
            imbalValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // depthTop4Label
            // 
            depthTop4Label.AutoSize = true;
            depthTop4Label.Dock = DockStyle.Fill;
            depthTop4Label.Font = new Font("Segoe UI", 7F);
            depthTop4Label.Location = new Point(3, 95);
            depthTop4Label.Name = "depthTop4Label";
            depthTop4Label.Size = new Size(56, 17);
            depthTop4Label.TabIndex = 7;
            depthTop4Label.Text = "Depth Top 4:";
            // 
            // depthTop4YesValue
            // 
            depthTop4YesValue.AutoSize = true;
            depthTop4YesValue.Dock = DockStyle.Fill;
            depthTop4YesValue.Font = new Font("Segoe UI", 7F);
            depthTop4YesValue.Location = new Point(65, 92);
            depthTop4YesValue.Name = "depthTop4YesValue";
            depthTop4YesValue.Size = new Size(25, 23);
            depthTop4YesValue.TabIndex = 8;
            depthTop4YesValue.Text = "--";
            depthTop4YesValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // depthTop4NoValue
            // 
            depthTop4NoValue.AutoSize = true;
            depthTop4NoValue.Dock = DockStyle.Fill;
            depthTop4NoValue.Font = new Font("Segoe UI", 7F);
            depthTop4NoValue.Location = new Point(96, 92);
            depthTop4NoValue.Name = "depthTop4NoValue";
            depthTop4NoValue.Size = new Size(25, 23);
            depthTop4NoValue.TabIndex = 9;
            depthTop4NoValue.Text = "--";
            depthTop4NoValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // centerMassLabel
            // 
            centerMassLabel.AutoSize = true;
            centerMassLabel.Dock = DockStyle.Fill;
            centerMassLabel.Font = new Font("Segoe UI", 7F);
            centerMassLabel.Location = new Point(3, 118);
            centerMassLabel.Name = "centerMassLabel";
            centerMassLabel.Size = new Size(56, 17);
            centerMassLabel.TabIndex = 10;
            centerMassLabel.Text = "Center Mass:";
            // 
            // centerMassYesValue
            // 
            centerMassYesValue.AutoSize = true;
            centerMassYesValue.Dock = DockStyle.Fill;
            centerMassYesValue.Font = new Font("Segoe UI", 7F);
            centerMassYesValue.Location = new Point(65, 115);
            centerMassYesValue.Name = "centerMassYesValue";
            centerMassYesValue.Size = new Size(25, 23);
            centerMassYesValue.TabIndex = 11;
            centerMassYesValue.Text = "--";
            centerMassYesValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // centerMassNoValue
            // 
            centerMassNoValue.AutoSize = true;
            centerMassNoValue.Dock = DockStyle.Fill;
            centerMassNoValue.Font = new Font("Segoe UI", 7F);
            centerMassNoValue.Location = new Point(96, 115);
            centerMassNoValue.Name = "centerMassNoValue";
            centerMassNoValue.Size = new Size(25, 23);
            centerMassNoValue.TabIndex = 12;
            centerMassNoValue.Text = "--";
            centerMassNoValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // totalContractsLabel
            // 
            totalContractsLabel.AutoSize = true;
            totalContractsLabel.Dock = DockStyle.Fill;
            totalContractsLabel.Font = new Font("Segoe UI", 7F);
            totalContractsLabel.Location = new Point(3, 141);
            totalContractsLabel.Name = "totalContractsLabel";
            totalContractsLabel.Size = new Size(56, 21);
            totalContractsLabel.TabIndex = 13;
            totalContractsLabel.Text = "Total Contracts:";
            // 
            // totalContractsYesValue
            // 
            totalContractsYesValue.AutoSize = true;
            totalContractsYesValue.Dock = DockStyle.Fill;
            totalContractsYesValue.Font = new Font("Segoe UI", 7F);
            totalContractsYesValue.Location = new Point(65, 138);
            totalContractsYesValue.Name = "totalContractsYesValue";
            totalContractsYesValue.Size = new Size(25, 27);
            totalContractsYesValue.TabIndex = 14;
            totalContractsYesValue.Text = "--";
            totalContractsYesValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // totalContractsNoValue
            // 
            totalContractsNoValue.AutoSize = true;
            totalContractsNoValue.Dock = DockStyle.Fill;
            totalContractsNoValue.Font = new Font("Segoe UI", 7F);
            totalContractsNoValue.Location = new Point(96, 138);
            totalContractsNoValue.Name = "totalContractsNoValue";
            totalContractsNoValue.Size = new Size(25, 27);
            totalContractsNoValue.TabIndex = 15;
            totalContractsNoValue.Text = "--";
            totalContractsNoValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // positionsContainer
            // 
            positionsContainer.BorderStyle = BorderStyle.FixedSingle;
            positionsContainer.Controls.Add(positionsLayout);
            positionsContainer.Dock = DockStyle.Fill;
            positionsContainer.Font = new Font("Segoe UI", 7F);
            positionsContainer.Location = new Point(4, 455);
            positionsContainer.Margin = new Padding(4, 3, 4, 3);
            positionsContainer.Name = "positionsContainer";
            positionsContainer.Padding = new Padding(6);
            positionsContainer.Size = new Size(593, 193);
            positionsContainer.TabIndex = 2;
            // 
            // positionsLayout
            // 
            positionsLayout.AutoSize = true;
            positionsLayout.ColumnCount = 2;
            positionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70.8117447F));
            positionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29.1882553F));
            positionsLayout.Controls.Add(strategyOutputTextbox, 0, 0);
            positionsLayout.Controls.Add(positionsGrid, 1, 0);
            positionsLayout.Dock = DockStyle.Fill;
            positionsLayout.Location = new Point(6, 6);
            positionsLayout.Margin = new Padding(0);
            positionsLayout.Name = "positionsLayout";
            positionsLayout.RowCount = 1;
            positionsLayout.RowStyles.Add(new RowStyle());
            positionsLayout.Size = new Size(579, 179);
            positionsLayout.TabIndex = 0;
            positionsLayout.Paint += positionsLayout_Paint;
            // 
            // orderbookContainer
            // 
            orderbookContainer.BorderStyle = BorderStyle.FixedSingle;
            orderbookContainer.Controls.Add(orderbookGrid);
            orderbookContainer.Dock = DockStyle.Fill;
            orderbookContainer.Font = new Font("Segoe UI", 7F);
            orderbookContainer.Location = new Point(605, 455);
            orderbookContainer.Margin = new Padding(4, 3, 4, 3);
            orderbookContainer.Name = "orderbookContainer";
            orderbookContainer.Padding = new Padding(6);
            orderbookContainer.Size = new Size(316, 193);
            orderbookContainer.TabIndex = 3;
            // 
            // orderbookGrid
            // 
            orderbookGrid.AllowUserToAddRows = false;
            orderbookGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            orderbookGrid.BorderStyle = BorderStyle.None;
            orderbookGrid.Columns.AddRange(new DataGridViewColumn[] { priceCol, sizeCol, valueCol });
            orderbookGrid.Dock = DockStyle.Fill;
            orderbookGrid.GridColor = Color.LightGray;
            orderbookGrid.Location = new Point(6, 6);
            orderbookGrid.Margin = new Padding(4, 3, 4, 3);
            orderbookGrid.Name = "orderbookGrid";
            orderbookGrid.ReadOnly = true;
            orderbookGrid.RowHeadersVisible = false;
            orderbookGrid.Size = new Size(302, 179);
            orderbookGrid.TabIndex = 0;
            // 
            // priceCol
            // 
            priceCol.HeaderText = "Price";
            priceCol.Name = "priceCol";
            priceCol.ReadOnly = true;
            // 
            // sizeCol
            // 
            sizeCol.HeaderText = "Size";
            sizeCol.Name = "sizeCol";
            sizeCol.ReadOnly = true;
            // 
            // valueCol
            // 
            valueCol.HeaderText = "Value";
            valueCol.Name = "valueCol";
            valueCol.ReadOnly = true;
            // 
            // backButton
            // 
            backButton.Dock = DockStyle.Fill;
            backButton.Font = new Font("Segoe UI", 7F);
            backButton.Location = new Point(4, 660);
            backButton.Margin = new Padding(4, 3, 4, 3);
            backButton.Name = "backButton";
            backButton.Size = new Size(925, 29);
            backButton.TabIndex = 1;
            backButton.Text = "Back to Full Chart";
            backButton.UseVisualStyleBackColor = true;
            // 
            // chartHeader
            // 
            chartHeader.AutoSize = true;
            chartHeader.Dock = DockStyle.Fill;
            chartHeader.Font = new Font("Segoe UI", 7F);
            chartHeader.Location = new Point(0, 0);
            chartHeader.Name = "chartHeader";
            chartHeader.Size = new Size(573, 40);
            chartHeader.TabIndex = 0;
            chartHeader.Text = "Chart Header";
            chartHeader.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // positionsGrid
            // 
            positionsGrid.AutoSize = true;
            positionsGrid.ColumnCount = 2;
            positionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            positionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            positionsGrid.Controls.Add(positionSizeValue, 1, 7);
            positionsGrid.Controls.Add(positionSizeLabel, 0, 7);
            positionsGrid.Controls.Add(simulatedPositionValue, 1, 0);
            positionsGrid.Controls.Add(simulatedPositionLabel, 0, 0);
            positionsGrid.Controls.Add(lastTradeLabel, 0, 1);
            positionsGrid.Controls.Add(lastTradeValue, 1, 1);
            positionsGrid.Controls.Add(positionRoiLabel, 0, 2);
            positionsGrid.Controls.Add(positionRoiValue, 1, 2);
            positionsGrid.Controls.Add(buyinPriceLabel, 0, 3);
            positionsGrid.Controls.Add(buyinPriceValue, 1, 3);
            positionsGrid.Controls.Add(positionUpsideLabel, 0, 4);
            positionsGrid.Controls.Add(positionUpsideValue, 1, 4);
            positionsGrid.Controls.Add(positionDownsideLabel, 0, 5);
            positionsGrid.Controls.Add(positionDownsideValue, 1, 5);
            positionsGrid.Controls.Add(restingOrdersLabel, 0, 6);
            positionsGrid.Controls.Add(restingOrdersValue, 1, 6);
            positionsGrid.Dock = DockStyle.Right;
            positionsGrid.Location = new Point(416, 3);
            positionsGrid.Name = "positionsGrid";
            positionsGrid.RowCount = 8;
            positionsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            positionsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            positionsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            positionsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            positionsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            positionsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            positionsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            positionsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            positionsGrid.Size = new Size(160, 176);
            positionsGrid.TabIndex = 2;
            // 
            // positionSizeValue
            // 
            positionSizeValue.AutoSize = true;
            positionSizeValue.Dock = DockStyle.Fill;
            positionSizeValue.Font = new Font("Segoe UI", 7F);
            positionSizeValue.Location = new Point(99, 154);
            positionSizeValue.Name = "positionSizeValue";
            positionSizeValue.Size = new Size(58, 22);
            positionSizeValue.TabIndex = 19;
            positionSizeValue.Text = "--";
            positionSizeValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // positionSizeLabel
            // 
            positionSizeLabel.AutoSize = true;
            positionSizeLabel.Dock = DockStyle.Fill;
            positionSizeLabel.Font = new Font("Segoe UI", 7F);
            positionSizeLabel.Location = new Point(3, 157);
            positionSizeLabel.Name = "positionSizeLabel";
            positionSizeLabel.Size = new Size(90, 16);
            positionSizeLabel.TabIndex = 18;
            positionSizeLabel.Text = "Position Size:";
            // 
            // simulatedPositionValue
            // 
            simulatedPositionValue.AutoSize = true;
            simulatedPositionValue.Dock = DockStyle.Fill;
            simulatedPositionValue.Font = new Font("Segoe UI", 7F);
            simulatedPositionValue.Location = new Point(99, 0);
            simulatedPositionValue.Name = "simulatedPositionValue";
            simulatedPositionValue.Size = new Size(58, 22);
            simulatedPositionValue.TabIndex = 17;
            simulatedPositionValue.Text = "--";
            simulatedPositionValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // simulatedPositionLabel
            // 
            simulatedPositionLabel.AutoSize = true;
            simulatedPositionLabel.Dock = DockStyle.Fill;
            simulatedPositionLabel.Font = new Font("Segoe UI", 7F);
            simulatedPositionLabel.Location = new Point(3, 3);
            simulatedPositionLabel.Name = "simulatedPositionLabel";
            simulatedPositionLabel.Size = new Size(90, 16);
            simulatedPositionLabel.TabIndex = 16;
            simulatedPositionLabel.Text = "Simulated Pos:";
            // 
            // lastTradeLabel
            // 
            lastTradeLabel.AutoSize = true;
            lastTradeLabel.Dock = DockStyle.Fill;
            lastTradeLabel.Font = new Font("Segoe UI", 7F);
            lastTradeLabel.Location = new Point(3, 22);
            lastTradeLabel.Name = "lastTradeLabel";
            lastTradeLabel.Size = new Size(90, 22);
            lastTradeLabel.TabIndex = 2;
            lastTradeLabel.Text = "Last Trade:";
            // 
            // lastTradeValue
            // 
            lastTradeValue.AutoSize = true;
            lastTradeValue.Dock = DockStyle.Fill;
            lastTradeValue.Font = new Font("Segoe UI", 7F);
            lastTradeValue.Location = new Point(99, 22);
            lastTradeValue.Name = "lastTradeValue";
            lastTradeValue.Size = new Size(58, 22);
            lastTradeValue.TabIndex = 3;
            lastTradeValue.Text = "--";
            lastTradeValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // positionRoiLabel
            // 
            positionRoiLabel.AutoSize = true;
            positionRoiLabel.Dock = DockStyle.Fill;
            positionRoiLabel.Font = new Font("Segoe UI", 7F);
            positionRoiLabel.Location = new Point(3, 47);
            positionRoiLabel.Name = "positionRoiLabel";
            positionRoiLabel.Size = new Size(90, 16);
            positionRoiLabel.TabIndex = 4;
            positionRoiLabel.Text = "Position ROI:";
            // 
            // positionRoiValue
            // 
            positionRoiValue.AutoSize = true;
            positionRoiValue.Dock = DockStyle.Fill;
            positionRoiValue.Font = new Font("Segoe UI", 7F);
            positionRoiValue.Location = new Point(99, 44);
            positionRoiValue.Name = "positionRoiValue";
            positionRoiValue.Size = new Size(58, 22);
            positionRoiValue.TabIndex = 5;
            positionRoiValue.Text = "--";
            positionRoiValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // buyinPriceLabel
            // 
            buyinPriceLabel.AutoSize = true;
            buyinPriceLabel.Dock = DockStyle.Fill;
            buyinPriceLabel.Font = new Font("Segoe UI", 7F);
            buyinPriceLabel.Location = new Point(3, 66);
            buyinPriceLabel.Name = "buyinPriceLabel";
            buyinPriceLabel.Size = new Size(90, 22);
            buyinPriceLabel.TabIndex = 6;
            buyinPriceLabel.Text = "Buyin Price:";
            // 
            // buyinPriceValue
            // 
            buyinPriceValue.AutoSize = true;
            buyinPriceValue.Dock = DockStyle.Fill;
            buyinPriceValue.Font = new Font("Segoe UI", 7F);
            buyinPriceValue.Location = new Point(99, 66);
            buyinPriceValue.Name = "buyinPriceValue";
            buyinPriceValue.Size = new Size(58, 22);
            buyinPriceValue.TabIndex = 7;
            buyinPriceValue.Text = "--";
            buyinPriceValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // positionUpsideLabel
            // 
            positionUpsideLabel.AutoSize = true;
            positionUpsideLabel.Dock = DockStyle.Fill;
            positionUpsideLabel.Font = new Font("Segoe UI", 7F);
            positionUpsideLabel.Location = new Point(3, 88);
            positionUpsideLabel.Name = "positionUpsideLabel";
            positionUpsideLabel.Size = new Size(90, 22);
            positionUpsideLabel.TabIndex = 8;
            positionUpsideLabel.Text = "Position Upside:";
            // 
            // positionUpsideValue
            // 
            positionUpsideValue.AutoSize = true;
            positionUpsideValue.Dock = DockStyle.Fill;
            positionUpsideValue.Font = new Font("Segoe UI", 7F);
            positionUpsideValue.Location = new Point(99, 88);
            positionUpsideValue.Name = "positionUpsideValue";
            positionUpsideValue.Size = new Size(58, 22);
            positionUpsideValue.TabIndex = 9;
            positionUpsideValue.Text = "--";
            positionUpsideValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // positionDownsideLabel
            // 
            positionDownsideLabel.AutoSize = true;
            positionDownsideLabel.Dock = DockStyle.Fill;
            positionDownsideLabel.Font = new Font("Segoe UI", 7F);
            positionDownsideLabel.Location = new Point(3, 110);
            positionDownsideLabel.Name = "positionDownsideLabel";
            positionDownsideLabel.Size = new Size(90, 22);
            positionDownsideLabel.TabIndex = 10;
            positionDownsideLabel.Text = "Position Downside:";
            // 
            // positionDownsideValue
            // 
            positionDownsideValue.AutoSize = true;
            positionDownsideValue.Dock = DockStyle.Fill;
            positionDownsideValue.Font = new Font("Segoe UI", 7F);
            positionDownsideValue.Location = new Point(99, 110);
            positionDownsideValue.Name = "positionDownsideValue";
            positionDownsideValue.Size = new Size(58, 22);
            positionDownsideValue.TabIndex = 11;
            positionDownsideValue.Text = "--";
            positionDownsideValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // restingOrdersLabel
            // 
            restingOrdersLabel.AutoSize = true;
            restingOrdersLabel.Dock = DockStyle.Fill;
            restingOrdersLabel.Font = new Font("Segoe UI", 7F);
            restingOrdersLabel.Location = new Point(3, 135);
            restingOrdersLabel.Name = "restingOrdersLabel";
            restingOrdersLabel.Size = new Size(90, 16);
            restingOrdersLabel.TabIndex = 12;
            restingOrdersLabel.Text = "Resting Orders:";
            // 
            // restingOrdersValue
            // 
            restingOrdersValue.AutoSize = true;
            restingOrdersValue.Dock = DockStyle.Fill;
            restingOrdersValue.Font = new Font("Segoe UI", 7F);
            restingOrdersValue.Location = new Point(99, 132);
            restingOrdersValue.Name = "restingOrdersValue";
            restingOrdersValue.Size = new Size(58, 22);
            restingOrdersValue.TabIndex = 13;
            restingOrdersValue.Text = "--";
            restingOrdersValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // strategyOutputTextbox
            // 
            strategyOutputTextbox.Dock = DockStyle.Fill;
            strategyOutputTextbox.Location = new Point(3, 3);
            strategyOutputTextbox.Multiline = true;
            strategyOutputTextbox.Name = "strategyOutputTextbox";
            strategyOutputTextbox.ReadOnly = true;
            strategyOutputTextbox.Size = new Size(404, 176);
            strategyOutputTextbox.TabIndex = 4;
            // 
            // SnapshotViewer
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(mainLayout);
            Margin = new Padding(4, 3, 4, 3);
            MinimumSize = new Size(800, 600);
            Name = "SnapshotViewer";
            Size = new Size(933, 692);
            mainLayout.ResumeLayout(false);
            dashboardGrid.ResumeLayout(false);
            chartContainer.ResumeLayout(false);
            chartContainer.PerformLayout();
            chartLayout.ResumeLayout(false);
            chartLayout.PerformLayout();
            marketInfoContainer.ResumeLayout(false);
            marketInfoContainer.PerformLayout();
            infoGrid.ResumeLayout(false);
            infoGrid.PerformLayout();
            leftColumn.ResumeLayout(false);
            leftColumn.PerformLayout();
            pricesGrid.ResumeLayout(false);
            pricesGrid.PerformLayout();
            allTimeHighAsk.ResumeLayout(false);
            allTimeHighAsk.PerformLayout();
            allTimeHighBid.ResumeLayout(false);
            allTimeHighBid.PerformLayout();
            recentHighAsk.ResumeLayout(false);
            recentHighAsk.PerformLayout();
            recentHighBid.ResumeLayout(false);
            recentHighBid.PerformLayout();
            recentLowAsk.ResumeLayout(false);
            recentLowAsk.PerformLayout();
            recentLowBid.ResumeLayout(false);
            recentLowBid.PerformLayout();
            allTimeLowAsk.ResumeLayout(false);
            allTimeLowAsk.PerformLayout();
            allTimeLowBid.ResumeLayout(false);
            allTimeLowBid.PerformLayout();
            tradingMetricsGrid.ResumeLayout(false);
            tradingMetricsGrid.PerformLayout();
            rightColumn.ResumeLayout(false);
            rightColumn.PerformLayout();
            otherInfoGrid.ResumeLayout(false);
            otherInfoGrid.PerformLayout();
            flowMomentumGrid.ResumeLayout(false);
            flowMomentumGrid.PerformLayout();
            contextGrid.ResumeLayout(false);
            contextGrid.PerformLayout();
            positionsContainer.ResumeLayout(false);
            positionsContainer.PerformLayout();
            positionsLayout.ResumeLayout(false);
            positionsLayout.PerformLayout();
            orderbookContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)orderbookGrid).EndInit();
            positionsGrid.ResumeLayout(false);
            positionsGrid.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private TableLayoutPanel mainLayout;
        private TableLayoutPanel dashboardGrid;
        private Panel chartContainer;
        private TableLayoutPanel chartLayout;
        private FlowLayoutPanel chartControls;
        private Label chartHeader;
        private FormsPlot priceChart;
        private Panel marketInfoContainer;
        private TableLayoutPanel infoGrid;
        private TableLayoutPanel leftColumn;
        private TableLayoutPanel pricesGrid;
        private Label pricesHeaderEmpty;
        private Label pricesHeaderNo;
        private Label pricesHeaderYes;
        private Label allTimeHighLabel;
        private TableLayoutPanel allTimeHighAsk;
        private Label allTimeHighAskPrice;
        private Label allTimeHighAskTime;
        private TableLayoutPanel allTimeHighBid;
        private Label allTimeHighBidPrice;
        private Label allTimeHighBidTime;
        private Label recentHighLabel;
        private TableLayoutPanel recentHighAsk;
        private Label recentHighAskPrice;
        private Label recentHighAskTime;
        private TableLayoutPanel recentHighBid;
        private Label recentHighBidPrice;
        private Label recentHighBidTime;
        private Label currentPriceLabel;
        private Label currentPriceAsk;
        private Label currentPriceBid;
        private Label recentLowLabel;
        private TableLayoutPanel recentLowAsk;
        private Label recentLowAskPrice;
        private Label recentLowAskTime;
        private TableLayoutPanel recentLowBid;
        private Label recentLowBidPrice;
        private Label recentLowBidTime;
        private Label allTimeLowLabel;
        private TableLayoutPanel allTimeLowAsk;
        private Label allTimeLowAskPrice;
        private Label allTimeLowAskTime;
        private TableLayoutPanel allTimeLowBid;
        private Label allTimeLowBidPrice;
        private Label allTimeLowBidTime;
        private TableLayoutPanel tradingMetricsGrid;
        private Label tradingMetricsHeader;
        private CheckBox rsiLabel;
        private Label rsiValue;
        private CheckBox macdLabel;
        private Label macdValue;
        private CheckBox emaLabel;
        private Label emaValue;
        private CheckBox bollingerLabel;
        private Label bollingerValue;
        private CheckBox atrLabel;
        private Label atrValue;
        private CheckBox vwapLabel;
        private Label vwapValue;
        private CheckBox stochasticLabel;
        private Label stochasticValue;
        private CheckBox obvLabel;
        private Label obvValue;
        private CheckBox psarLabel;
        private Label psarValue;
        private CheckBox adxLabel;
        private Label adxValue;
        private TableLayoutPanel rightColumn;
        private TableLayoutPanel otherInfoGrid;
        private Label CategoryLabel;
        private Label categoryValue;
        private Label timeLeftLabel;
        private Label timeLeftValue;
        private Label marketAgeLabel;
        private Label marketAgeValue;
        private TableLayoutPanel flowMomentumGrid;
        private Label flowHeader;
        private Label flowHeaderYes;
        private Label flowHeaderNo;
        private CheckBox topVelocityLabel;
        private Label topVelocityYesValue;
        private Label topVelocityNoValue;
        private CheckBox bottomVelocityLabel;
        private Label bottomVelocityYesValue;
        private Label bottomVelocityNoValue;
        private CheckBox netOrderRateLabel;
        private Label netOrderRateYesValue;
        private Label netOrderRateNoValue;
        private CheckBox tradeVolumeLabel;
        private Label tradeVolumeYesValue;
        private Label tradeVolumeNoValue;
        private CheckBox avgTradeSizeLabel;
        private Label avgTradeSizeYesValue;
        private Label avgTradeSizeNoValue;
        private CheckBox slopeLabel;
        private Label slopeYesValue;
        private Label slopeNoValue;
        private TableLayoutPanel contextGrid;
        private Label contextHeader;
        private Label contextHeaderYes;
        private Label contextHeaderNo;
        private Label spreadLabel;
        private Label spreadValue;
        private CheckBox imbalLabel;
        private Label imbalValue;
        private CheckBox depthTop4Label;
        private Label depthTop4YesValue;
        private Label depthTop4NoValue;
        private CheckBox centerMassLabel;
        private Label centerMassYesValue;
        private Label centerMassNoValue;
        private CheckBox totalContractsLabel;
        private Label totalContractsYesValue;
        private Label totalContractsNoValue;
        private Panel positionsContainer;
        private Panel orderbookContainer;
        private DataGridView orderbookGrid;
        private DataGridViewTextBoxColumn priceCol;
        private DataGridViewTextBoxColumn sizeCol;
        private DataGridViewTextBoxColumn valueCol;
        private Button backButton;
        private Label label1;
        private Label label2;
        private TableLayoutPanel positionsLayout;
        private TextBox strategyOutputTextbox;
        private TableLayoutPanel positionsGrid;
        private Label positionSizeValue;
        private CheckBox positionSizeLabel;
        private Label simulatedPositionValue;
        private CheckBox simulatedPositionLabel;
        private Label lastTradeLabel;
        private Label lastTradeValue;
        private CheckBox positionRoiLabel;
        private Label positionRoiValue;
        private Label buyinPriceLabel;
        private Label buyinPriceValue;
        private Label positionUpsideLabel;
        private Label positionUpsideValue;
        private Label positionDownsideLabel;
        private Label positionDownsideValue;
        private CheckBox restingOrdersLabel;
        private Label restingOrdersValue;
    }
}