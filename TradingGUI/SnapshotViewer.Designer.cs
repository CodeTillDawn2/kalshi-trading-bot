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
            flowHeaderYes = new Label();
            flowHeaderNo = new Label();
            topVelocityCB = new CheckBox();
            topVelocityYesValue = new Label();
            topVelocityNoValue = new Label();
            bottomVelocityCB = new CheckBox();
            bottomVelocityYesValue = new Label();
            bottomVelocityNoValue = new Label();
            netOrderRateCB = new CheckBox();
            netOrderRateYesValue = new Label();
            netOrderRateNoValue = new Label();
            tradeVolumeCB = new CheckBox();
            tradeVolumeYesValue = new Label();
            tradeVolumeNoValue = new Label();
            avgTradeSizeCB = new CheckBox();
            avgTradeSizeYesValue = new Label();
            avgTradeSizeNoValue = new Label();
            slopeCB = new CheckBox();
            slopeYesValue = new Label();
            slopeNoValue = new Label();
            contextGrid = new TableLayoutPanel();
            totalDepthNoValue = new Label();
            totalDepthYesValue = new Label();
            totalDepthCB = new CheckBox();
            contextHeaderYes = new Label();
            contextHeaderNo = new Label();
            spreadLabel = new Label();
            spreadValue = new Label();
            imbalCB = new CheckBox();
            imbalValue = new Label();
            depthTop4CB = new CheckBox();
            depthTop4YesValue = new Label();
            depthTop4NoValue = new Label();
            centerMassCB = new CheckBox();
            centerMassYesValue = new Label();
            centerMassNoValue = new Label();
            totalContractsCB = new CheckBox();
            totalContractsYesValue = new Label();
            totalContractsNoValue = new Label();
            positionsContainer = new Panel();
            positionsLayout = new TableLayoutPanel();
            strategyOutputTextbox = new TextBox();
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
            orderbookContainer = new Panel();
            orderbookGrid = new DataGridView();
            priceCol = new DataGridViewTextBoxColumn();
            sizeCol = new DataGridViewTextBoxColumn();
            valueCol = new DataGridViewTextBoxColumn();
            backButton = new Button();
            chartHeader = new Label();
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
            positionsGrid.SuspendLayout();
            orderbookContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)orderbookGrid).BeginInit();
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
            dashboardGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 63.0270271F));
            dashboardGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36.9729729F));
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
            chartContainer.Size = new Size(575, 446);
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
            chartLayout.Size = new Size(561, 432);
            chartLayout.TabIndex = 0;
            // 
            // chartControls
            // 
            chartControls.AutoSize = true;
            chartControls.Dock = DockStyle.Fill;
            chartControls.Location = new Point(3, 3);
            chartControls.Name = "chartControls";
            chartControls.Size = new Size(555, 37);
            chartControls.TabIndex = 0;
            // 
            // priceChart
            // 
            priceChart.Dock = DockStyle.Fill;
            priceChart.Location = new Point(4, 46);
            priceChart.Margin = new Padding(4, 3, 4, 3);
            priceChart.Name = "priceChart";
            priceChart.Size = new Size(553, 383);
            priceChart.TabIndex = 1;
            // 
            // marketInfoContainer
            // 
            marketInfoContainer.BorderStyle = BorderStyle.FixedSingle;
            marketInfoContainer.Controls.Add(infoGrid);
            marketInfoContainer.Dock = DockStyle.Fill;
            marketInfoContainer.Location = new Point(587, 3);
            marketInfoContainer.Margin = new Padding(4, 3, 4, 3);
            marketInfoContainer.Name = "marketInfoContainer";
            marketInfoContainer.Padding = new Padding(6);
            marketInfoContainer.Size = new Size(334, 446);
            marketInfoContainer.TabIndex = 1;
            // 
            // infoGrid
            // 
            infoGrid.AutoSize = true;
            infoGrid.ColumnCount = 2;
            infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48.75F));
            infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 51.25F));
            infoGrid.Controls.Add(leftColumn, 0, 0);
            infoGrid.Controls.Add(rightColumn, 1, 0);
            infoGrid.Dock = DockStyle.Fill;
            infoGrid.Location = new Point(6, 6);
            infoGrid.Margin = new Padding(0);
            infoGrid.Name = "infoGrid";
            infoGrid.RowCount = 1;
            infoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            infoGrid.Size = new Size(320, 432);
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
            leftColumn.Size = new Size(150, 426);
            leftColumn.TabIndex = 0;
            leftColumn.Paint += leftColumn_Paint;
            // 
            // pricesGrid
            // 
            pricesGrid.AutoSize = true;
            pricesGrid.ColumnCount = 3;
            pricesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30.5555553F));
            pricesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34.02778F));
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
            pricesGrid.Size = new Size(144, 194);
            pricesGrid.TabIndex = 0;
            // 
            // pricesHeaderEmpty
            // 
            pricesHeaderEmpty.AutoSize = true;
            pricesHeaderEmpty.Dock = DockStyle.Fill;
            pricesHeaderEmpty.Location = new Point(3, 0);
            pricesHeaderEmpty.Name = "pricesHeaderEmpty";
            pricesHeaderEmpty.Size = new Size(38, 33);
            pricesHeaderEmpty.TabIndex = 0;
            // 
            // pricesHeaderNo
            // 
            pricesHeaderNo.AutoSize = true;
            pricesHeaderNo.Dock = DockStyle.Fill;
            pricesHeaderNo.Location = new Point(47, 0);
            pricesHeaderNo.Name = "pricesHeaderNo";
            pricesHeaderNo.Size = new Size(43, 33);
            pricesHeaderNo.TabIndex = 1;
            pricesHeaderNo.Text = "No";
            // 
            // pricesHeaderYes
            // 
            pricesHeaderYes.AutoSize = true;
            pricesHeaderYes.Dock = DockStyle.Fill;
            pricesHeaderYes.Location = new Point(96, 0);
            pricesHeaderYes.Name = "pricesHeaderYes";
            pricesHeaderYes.Size = new Size(45, 33);
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
            allTimeHighLabel.Size = new Size(38, 36);
            allTimeHighLabel.TabIndex = 3;
            allTimeHighLabel.Text = "All Time High";
            allTimeHighLabel.TextAlign = ContentAlignment.MiddleCenter;
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
            allTimeHighAsk.Location = new Point(47, 36);
            allTimeHighAsk.Name = "allTimeHighAsk";
            allTimeHighAsk.RowCount = 2;
            allTimeHighAsk.RowStyles.Add(new RowStyle());
            allTimeHighAsk.RowStyles.Add(new RowStyle());
            allTimeHighAsk.Size = new Size(43, 30);
            allTimeHighAsk.TabIndex = 4;
            // 
            // allTimeHighAskPrice
            // 
            allTimeHighAskPrice.AutoSize = true;
            allTimeHighAskPrice.Dock = DockStyle.Fill;
            allTimeHighAskPrice.Font = new Font("Segoe UI", 7F);
            allTimeHighAskPrice.Location = new Point(3, 0);
            allTimeHighAskPrice.Name = "allTimeHighAskPrice";
            allTimeHighAskPrice.Size = new Size(37, 12);
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
            allTimeHighAskTime.Size = new Size(37, 18);
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
            allTimeHighBid.Location = new Point(96, 36);
            allTimeHighBid.Name = "allTimeHighBid";
            allTimeHighBid.RowStyles.Add(new RowStyle());
            allTimeHighBid.RowStyles.Add(new RowStyle());
            allTimeHighBid.Size = new Size(45, 30);
            allTimeHighBid.TabIndex = 5;
            // 
            // allTimeHighBidPrice
            // 
            allTimeHighBidPrice.AutoSize = true;
            allTimeHighBidPrice.Dock = DockStyle.Fill;
            allTimeHighBidPrice.Font = new Font("Segoe UI", 7F);
            allTimeHighBidPrice.Location = new Point(3, 0);
            allTimeHighBidPrice.Name = "allTimeHighBidPrice";
            allTimeHighBidPrice.Size = new Size(39, 12);
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
            allTimeHighBidTime.Size = new Size(39, 18);
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
            recentHighLabel.Size = new Size(38, 32);
            recentHighLabel.TabIndex = 6;
            recentHighLabel.Text = "Recent High";
            recentHighLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // recentHighAsk
            // 
            recentHighAsk.AutoSize = true;
            recentHighAsk.ColumnStyles.Add(new ColumnStyle());
            recentHighAsk.Controls.Add(recentHighAskPrice, 0, 0);
            recentHighAsk.Controls.Add(recentHighAskTime, 0, 1);
            recentHighAsk.Dock = DockStyle.Fill;
            recentHighAsk.Location = new Point(47, 72);
            recentHighAsk.Name = "recentHighAsk";
            recentHighAsk.RowStyles.Add(new RowStyle());
            recentHighAsk.RowStyles.Add(new RowStyle());
            recentHighAsk.Size = new Size(43, 26);
            recentHighAsk.TabIndex = 7;
            // 
            // recentHighAskPrice
            // 
            recentHighAskPrice.AutoSize = true;
            recentHighAskPrice.Dock = DockStyle.Fill;
            recentHighAskPrice.Font = new Font("Segoe UI", 7F);
            recentHighAskPrice.Location = new Point(3, 0);
            recentHighAskPrice.Name = "recentHighAskPrice";
            recentHighAskPrice.Size = new Size(37, 12);
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
            recentHighAskTime.Size = new Size(37, 14);
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
            recentHighBid.Location = new Point(96, 72);
            recentHighBid.Name = "recentHighBid";
            recentHighBid.RowStyles.Add(new RowStyle());
            recentHighBid.RowStyles.Add(new RowStyle());
            recentHighBid.Size = new Size(45, 26);
            recentHighBid.TabIndex = 8;
            // 
            // recentHighBidPrice
            // 
            recentHighBidPrice.AutoSize = true;
            recentHighBidPrice.Dock = DockStyle.Fill;
            recentHighBidPrice.Font = new Font("Segoe UI", 7F);
            recentHighBidPrice.Location = new Point(3, 0);
            recentHighBidPrice.Name = "recentHighBidPrice";
            recentHighBidPrice.Size = new Size(39, 12);
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
            recentHighBidTime.Size = new Size(39, 14);
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
            currentPriceLabel.Size = new Size(38, 24);
            currentPriceLabel.TabIndex = 9;
            currentPriceLabel.Text = "Current";
            currentPriceLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // currentPriceAsk
            // 
            currentPriceAsk.AutoSize = true;
            currentPriceAsk.Dock = DockStyle.Fill;
            currentPriceAsk.Font = new Font("Segoe UI", 7F);
            currentPriceAsk.Location = new Point(47, 101);
            currentPriceAsk.Name = "currentPriceAsk";
            currentPriceAsk.Size = new Size(43, 24);
            currentPriceAsk.TabIndex = 10;
            currentPriceAsk.Text = "--";
            currentPriceAsk.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // currentPriceBid
            // 
            currentPriceBid.AutoSize = true;
            currentPriceBid.Dock = DockStyle.Fill;
            currentPriceBid.Font = new Font("Segoe UI", 7F);
            currentPriceBid.Location = new Point(96, 101);
            currentPriceBid.Name = "currentPriceBid";
            currentPriceBid.Size = new Size(45, 24);
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
            recentLowLabel.Size = new Size(38, 30);
            recentLowLabel.TabIndex = 12;
            recentLowLabel.Text = "Recent Low";
            recentLowLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // recentLowAsk
            // 
            recentLowAsk.AutoSize = true;
            recentLowAsk.ColumnStyles.Add(new ColumnStyle());
            recentLowAsk.Controls.Add(recentLowAskPrice, 0, 0);
            recentLowAsk.Controls.Add(recentLowAskTime, 0, 1);
            recentLowAsk.Dock = DockStyle.Fill;
            recentLowAsk.Location = new Point(47, 128);
            recentLowAsk.Name = "recentLowAsk";
            recentLowAsk.RowStyles.Add(new RowStyle());
            recentLowAsk.RowStyles.Add(new RowStyle());
            recentLowAsk.Size = new Size(43, 24);
            recentLowAsk.TabIndex = 13;
            // 
            // recentLowAskPrice
            // 
            recentLowAskPrice.AutoSize = true;
            recentLowAskPrice.Dock = DockStyle.Fill;
            recentLowAskPrice.Font = new Font("Segoe UI", 7F);
            recentLowAskPrice.Location = new Point(3, 0);
            recentLowAskPrice.Name = "recentLowAskPrice";
            recentLowAskPrice.Size = new Size(37, 12);
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
            recentLowAskTime.Size = new Size(37, 12);
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
            recentLowBid.Location = new Point(96, 128);
            recentLowBid.Name = "recentLowBid";
            recentLowBid.RowStyles.Add(new RowStyle());
            recentLowBid.RowStyles.Add(new RowStyle());
            recentLowBid.Size = new Size(45, 24);
            recentLowBid.TabIndex = 14;
            // 
            // recentLowBidPrice
            // 
            recentLowBidPrice.AutoSize = true;
            recentLowBidPrice.Dock = DockStyle.Fill;
            recentLowBidPrice.Font = new Font("Segoe UI", 7F);
            recentLowBidPrice.Location = new Point(3, 0);
            recentLowBidPrice.Name = "recentLowBidPrice";
            recentLowBidPrice.Size = new Size(39, 12);
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
            recentLowBidTime.Size = new Size(39, 12);
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
            allTimeLowLabel.Size = new Size(38, 39);
            allTimeLowLabel.TabIndex = 15;
            allTimeLowLabel.Text = "All Time Low";
            allTimeLowLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // allTimeLowAsk
            // 
            allTimeLowAsk.AutoSize = true;
            allTimeLowAsk.ColumnStyles.Add(new ColumnStyle());
            allTimeLowAsk.Controls.Add(allTimeLowAskPrice, 0, 0);
            allTimeLowAsk.Controls.Add(allTimeLowAskTime, 0, 1);
            allTimeLowAsk.Dock = DockStyle.Fill;
            allTimeLowAsk.Location = new Point(47, 158);
            allTimeLowAsk.Name = "allTimeLowAsk";
            allTimeLowAsk.RowStyles.Add(new RowStyle());
            allTimeLowAsk.RowStyles.Add(new RowStyle());
            allTimeLowAsk.Size = new Size(43, 33);
            allTimeLowAsk.TabIndex = 16;
            // 
            // allTimeLowAskPrice
            // 
            allTimeLowAskPrice.AutoSize = true;
            allTimeLowAskPrice.Dock = DockStyle.Fill;
            allTimeLowAskPrice.Font = new Font("Segoe UI", 7F);
            allTimeLowAskPrice.Location = new Point(3, 0);
            allTimeLowAskPrice.Name = "allTimeLowAskPrice";
            allTimeLowAskPrice.Size = new Size(37, 12);
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
            allTimeLowAskTime.Size = new Size(37, 21);
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
            allTimeLowBid.Location = new Point(96, 158);
            allTimeLowBid.Name = "allTimeLowBid";
            allTimeLowBid.RowStyles.Add(new RowStyle());
            allTimeLowBid.RowStyles.Add(new RowStyle());
            allTimeLowBid.Size = new Size(45, 33);
            allTimeLowBid.TabIndex = 17;
            // 
            // allTimeLowBidPrice
            // 
            allTimeLowBidPrice.AutoSize = true;
            allTimeLowBidPrice.Dock = DockStyle.Fill;
            allTimeLowBidPrice.Font = new Font("Segoe UI", 7F);
            allTimeLowBidPrice.Location = new Point(3, 0);
            allTimeLowBidPrice.Name = "allTimeLowBidPrice";
            allTimeLowBidPrice.Size = new Size(39, 12);
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
            allTimeLowBidTime.Size = new Size(39, 21);
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
            tradingMetricsGrid.Size = new Size(144, 220);
            tradingMetricsGrid.TabIndex = 1;
            // 
            // tradingMetricsHeader
            // 
            tradingMetricsHeader.AutoSize = true;
            tradingMetricsGrid.SetColumnSpan(tradingMetricsHeader, 2);
            tradingMetricsHeader.Dock = DockStyle.Fill;
            tradingMetricsHeader.Location = new Point(3, 0);
            tradingMetricsHeader.Name = "tradingMetricsHeader";
            tradingMetricsHeader.Size = new Size(138, 20);
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
            rsiLabel.Size = new Size(80, 14);
            rsiLabel.TabIndex = 1;
            rsiLabel.Text = "RSI:";
            // 
            // rsiValue
            // 
            rsiValue.AutoSize = true;
            rsiValue.Dock = DockStyle.Fill;
            rsiValue.Font = new Font("Segoe UI", 7F);
            rsiValue.Location = new Point(89, 20);
            rsiValue.Name = "rsiValue";
            rsiValue.Size = new Size(52, 20);
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
            macdLabel.Size = new Size(80, 14);
            macdLabel.TabIndex = 3;
            macdLabel.Text = "MACD:";
            // 
            // macdValue
            // 
            macdValue.AutoSize = true;
            macdValue.Dock = DockStyle.Fill;
            macdValue.Font = new Font("Segoe UI", 7F);
            macdValue.Location = new Point(89, 40);
            macdValue.Name = "macdValue";
            macdValue.Size = new Size(52, 20);
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
            emaLabel.Size = new Size(80, 14);
            emaLabel.TabIndex = 5;
            emaLabel.Text = "EMA:";
            // 
            // emaValue
            // 
            emaValue.AutoSize = true;
            emaValue.Dock = DockStyle.Fill;
            emaValue.Font = new Font("Segoe UI", 7F);
            emaValue.Location = new Point(89, 60);
            emaValue.Name = "emaValue";
            emaValue.Size = new Size(52, 20);
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
            bollingerLabel.Size = new Size(80, 14);
            bollingerLabel.TabIndex = 7;
            bollingerLabel.Text = "Bollinger:";
            // 
            // bollingerValue
            // 
            bollingerValue.AutoSize = true;
            bollingerValue.Dock = DockStyle.Fill;
            bollingerValue.Font = new Font("Segoe UI", 7F);
            bollingerValue.Location = new Point(89, 80);
            bollingerValue.Name = "bollingerValue";
            bollingerValue.Size = new Size(52, 20);
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
            atrLabel.Size = new Size(80, 14);
            atrLabel.TabIndex = 9;
            atrLabel.Text = "ATR:";
            // 
            // atrValue
            // 
            atrValue.AutoSize = true;
            atrValue.Dock = DockStyle.Fill;
            atrValue.Font = new Font("Segoe UI", 7F);
            atrValue.Location = new Point(89, 100);
            atrValue.Name = "atrValue";
            atrValue.Size = new Size(52, 20);
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
            vwapLabel.Size = new Size(80, 14);
            vwapLabel.TabIndex = 11;
            vwapLabel.Text = "VWAP:";
            // 
            // vwapValue
            // 
            vwapValue.AutoSize = true;
            vwapValue.Dock = DockStyle.Fill;
            vwapValue.Font = new Font("Segoe UI", 7F);
            vwapValue.Location = new Point(89, 120);
            vwapValue.Name = "vwapValue";
            vwapValue.Size = new Size(52, 20);
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
            stochasticLabel.Size = new Size(80, 14);
            stochasticLabel.TabIndex = 13;
            stochasticLabel.Text = "Stochastic:";
            // 
            // stochasticValue
            // 
            stochasticValue.AutoSize = true;
            stochasticValue.Dock = DockStyle.Fill;
            stochasticValue.Font = new Font("Segoe UI", 7F);
            stochasticValue.Location = new Point(89, 140);
            stochasticValue.Name = "stochasticValue";
            stochasticValue.Size = new Size(52, 20);
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
            obvLabel.Size = new Size(80, 14);
            obvLabel.TabIndex = 15;
            obvLabel.Text = "OBV:";
            // 
            // obvValue
            // 
            obvValue.AutoSize = true;
            obvValue.Dock = DockStyle.Fill;
            obvValue.Font = new Font("Segoe UI", 7F);
            obvValue.Location = new Point(89, 160);
            obvValue.Name = "obvValue";
            obvValue.Size = new Size(52, 20);
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
            psarLabel.Size = new Size(80, 14);
            psarLabel.TabIndex = 17;
            psarLabel.Text = "PSAR:";
            // 
            // psarValue
            // 
            psarValue.AutoSize = true;
            psarValue.Dock = DockStyle.Fill;
            psarValue.Font = new Font("Segoe UI", 7F);
            psarValue.Location = new Point(89, 180);
            psarValue.Name = "psarValue";
            psarValue.Size = new Size(52, 20);
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
            adxLabel.Size = new Size(80, 14);
            adxLabel.TabIndex = 19;
            adxLabel.Text = "ADX:";
            // 
            // adxValue
            // 
            adxValue.AutoSize = true;
            adxValue.Dock = DockStyle.Fill;
            adxValue.Font = new Font("Segoe UI", 7F);
            adxValue.Location = new Point(89, 200);
            adxValue.Name = "adxValue";
            adxValue.Size = new Size(52, 20);
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
            rightColumn.Location = new Point(159, 3);
            rightColumn.Name = "rightColumn";
            rightColumn.RowCount = 3;
            rightColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 18.7793427F));
            rightColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 41.07981F));
            rightColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            rightColumn.Size = new Size(158, 426);
            rightColumn.TabIndex = 1;
            // 
            // otherInfoGrid
            // 
            otherInfoGrid.AutoSize = true;
            otherInfoGrid.ColumnCount = 2;
            otherInfoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45.16129F));
            otherInfoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54.83871F));
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
            otherInfoGrid.Size = new Size(152, 74);
            otherInfoGrid.TabIndex = 0;
            // 
            // CategoryLabel
            // 
            CategoryLabel.AutoSize = true;
            CategoryLabel.Dock = DockStyle.Fill;
            CategoryLabel.Font = new Font("Segoe UI", 7F);
            CategoryLabel.Location = new Point(3, 0);
            CategoryLabel.Name = "CategoryLabel";
            CategoryLabel.Size = new Size(62, 24);
            CategoryLabel.TabIndex = 0;
            CategoryLabel.Text = "Category:";
            CategoryLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // categoryValue
            // 
            categoryValue.AutoSize = true;
            categoryValue.Dock = DockStyle.Fill;
            categoryValue.Font = new Font("Segoe UI", 7F);
            categoryValue.Location = new Point(71, 0);
            categoryValue.Name = "categoryValue";
            categoryValue.Size = new Size(78, 24);
            categoryValue.TabIndex = 1;
            categoryValue.Text = "--";
            categoryValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // timeLeftLabel
            // 
            timeLeftLabel.AutoSize = true;
            timeLeftLabel.Dock = DockStyle.Fill;
            timeLeftLabel.Font = new Font("Segoe UI", 7F);
            timeLeftLabel.Location = new Point(3, 24);
            timeLeftLabel.Name = "timeLeftLabel";
            timeLeftLabel.Size = new Size(62, 24);
            timeLeftLabel.TabIndex = 2;
            timeLeftLabel.Text = "Time Left:";
            timeLeftLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // timeLeftValue
            // 
            timeLeftValue.AutoSize = true;
            timeLeftValue.Dock = DockStyle.Fill;
            timeLeftValue.Font = new Font("Segoe UI", 7F);
            timeLeftValue.Location = new Point(71, 24);
            timeLeftValue.Name = "timeLeftValue";
            timeLeftValue.Size = new Size(78, 24);
            timeLeftValue.TabIndex = 3;
            timeLeftValue.Text = "--";
            timeLeftValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // marketAgeLabel
            // 
            marketAgeLabel.AutoSize = true;
            marketAgeLabel.Dock = DockStyle.Fill;
            marketAgeLabel.Font = new Font("Segoe UI", 7F);
            marketAgeLabel.Location = new Point(3, 48);
            marketAgeLabel.Name = "marketAgeLabel";
            marketAgeLabel.Size = new Size(62, 26);
            marketAgeLabel.TabIndex = 4;
            marketAgeLabel.Text = "Market Age:";
            marketAgeLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // marketAgeValue
            // 
            marketAgeValue.AutoSize = true;
            marketAgeValue.Dock = DockStyle.Fill;
            marketAgeValue.Font = new Font("Segoe UI", 7F);
            marketAgeValue.Location = new Point(71, 48);
            marketAgeValue.Name = "marketAgeValue";
            marketAgeValue.Size = new Size(78, 26);
            marketAgeValue.TabIndex = 5;
            marketAgeValue.Text = "--";
            marketAgeValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // flowMomentumGrid
            // 
            flowMomentumGrid.AutoSize = true;
            flowMomentumGrid.ColumnCount = 3;
            flowMomentumGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 59.86842F));
            flowMomentumGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19.0789471F));
            flowMomentumGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21.0526314F));
            flowMomentumGrid.Controls.Add(flowHeaderYes, 1, 0);
            flowMomentumGrid.Controls.Add(flowHeaderNo, 2, 0);
            flowMomentumGrid.Controls.Add(topVelocityCB, 0, 1);
            flowMomentumGrid.Controls.Add(topVelocityYesValue, 1, 1);
            flowMomentumGrid.Controls.Add(topVelocityNoValue, 2, 1);
            flowMomentumGrid.Controls.Add(bottomVelocityCB, 0, 2);
            flowMomentumGrid.Controls.Add(bottomVelocityYesValue, 1, 2);
            flowMomentumGrid.Controls.Add(bottomVelocityNoValue, 2, 2);
            flowMomentumGrid.Controls.Add(netOrderRateCB, 0, 3);
            flowMomentumGrid.Controls.Add(netOrderRateYesValue, 1, 3);
            flowMomentumGrid.Controls.Add(netOrderRateNoValue, 2, 3);
            flowMomentumGrid.Controls.Add(tradeVolumeCB, 0, 4);
            flowMomentumGrid.Controls.Add(tradeVolumeYesValue, 1, 4);
            flowMomentumGrid.Controls.Add(tradeVolumeNoValue, 2, 4);
            flowMomentumGrid.Controls.Add(avgTradeSizeCB, 0, 5);
            flowMomentumGrid.Controls.Add(avgTradeSizeYesValue, 1, 5);
            flowMomentumGrid.Controls.Add(avgTradeSizeNoValue, 2, 5);
            flowMomentumGrid.Controls.Add(slopeCB, 0, 6);
            flowMomentumGrid.Controls.Add(slopeYesValue, 1, 6);
            flowMomentumGrid.Controls.Add(slopeNoValue, 2, 6);
            flowMomentumGrid.Dock = DockStyle.Fill;
            flowMomentumGrid.Location = new Point(3, 83);
            flowMomentumGrid.Name = "flowMomentumGrid";
            flowMomentumGrid.RowCount = 7;
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 8F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            flowMomentumGrid.Size = new Size(152, 169);
            flowMomentumGrid.TabIndex = 1;
            // 
            // flowHeaderYes
            // 
            flowHeaderYes.AutoSize = true;
            flowHeaderYes.Dock = DockStyle.Fill;
            flowHeaderYes.Font = new Font("Segoe UI", 7F);
            flowHeaderYes.Location = new Point(94, 0);
            flowHeaderYes.Name = "flowHeaderYes";
            flowHeaderYes.Size = new Size(23, 16);
            flowHeaderYes.TabIndex = 1;
            flowHeaderYes.Text = "Yes";
            // 
            // flowHeaderNo
            // 
            flowHeaderNo.AutoSize = true;
            flowHeaderNo.Dock = DockStyle.Fill;
            flowHeaderNo.Font = new Font("Segoe UI", 7F);
            flowHeaderNo.Location = new Point(123, 0);
            flowHeaderNo.Name = "flowHeaderNo";
            flowHeaderNo.Size = new Size(26, 16);
            flowHeaderNo.TabIndex = 2;
            flowHeaderNo.Text = "No";
            // 
            // topVelocityCB
            // 
            topVelocityCB.AutoSize = true;
            topVelocityCB.Dock = DockStyle.Fill;
            topVelocityCB.Font = new Font("Segoe UI", 6F, FontStyle.Regular, GraphicsUnit.Point, 0);
            topVelocityCB.Location = new Point(3, 19);
            topVelocityCB.Name = "topVelocityCB";
            topVelocityCB.Size = new Size(85, 19);
            topVelocityCB.TabIndex = 3;
            topVelocityCB.Text = "Top Velocity:";
            // 
            // topVelocityYesValue
            // 
            topVelocityYesValue.AutoSize = true;
            topVelocityYesValue.Dock = DockStyle.Fill;
            topVelocityYesValue.Font = new Font("Segoe UI", 7F);
            topVelocityYesValue.Location = new Point(94, 16);
            topVelocityYesValue.Name = "topVelocityYesValue";
            topVelocityYesValue.Size = new Size(23, 25);
            topVelocityYesValue.TabIndex = 4;
            topVelocityYesValue.Text = "--";
            topVelocityYesValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // topVelocityNoValue
            // 
            topVelocityNoValue.AutoSize = true;
            topVelocityNoValue.Dock = DockStyle.Fill;
            topVelocityNoValue.Font = new Font("Segoe UI", 7F);
            topVelocityNoValue.Location = new Point(123, 16);
            topVelocityNoValue.Name = "topVelocityNoValue";
            topVelocityNoValue.Size = new Size(26, 25);
            topVelocityNoValue.TabIndex = 5;
            topVelocityNoValue.Text = "--";
            topVelocityNoValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // bottomVelocityCB
            // 
            bottomVelocityCB.AutoSize = true;
            bottomVelocityCB.Dock = DockStyle.Fill;
            bottomVelocityCB.Font = new Font("Segoe UI", 6F, FontStyle.Regular, GraphicsUnit.Point, 0);
            bottomVelocityCB.Location = new Point(3, 44);
            bottomVelocityCB.Name = "bottomVelocityCB";
            bottomVelocityCB.Size = new Size(85, 19);
            bottomVelocityCB.TabIndex = 6;
            bottomVelocityCB.Text = "Bottom Velocity:";
            // 
            // bottomVelocityYesValue
            // 
            bottomVelocityYesValue.AutoSize = true;
            bottomVelocityYesValue.Dock = DockStyle.Fill;
            bottomVelocityYesValue.Font = new Font("Segoe UI", 7F);
            bottomVelocityYesValue.Location = new Point(94, 41);
            bottomVelocityYesValue.Name = "bottomVelocityYesValue";
            bottomVelocityYesValue.Size = new Size(23, 25);
            bottomVelocityYesValue.TabIndex = 7;
            bottomVelocityYesValue.Text = "--";
            bottomVelocityYesValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // bottomVelocityNoValue
            // 
            bottomVelocityNoValue.AutoSize = true;
            bottomVelocityNoValue.Dock = DockStyle.Fill;
            bottomVelocityNoValue.Font = new Font("Segoe UI", 7F);
            bottomVelocityNoValue.Location = new Point(123, 41);
            bottomVelocityNoValue.Name = "bottomVelocityNoValue";
            bottomVelocityNoValue.Size = new Size(26, 25);
            bottomVelocityNoValue.TabIndex = 8;
            bottomVelocityNoValue.Text = "--";
            bottomVelocityNoValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // netOrderRateCB
            // 
            netOrderRateCB.AutoSize = true;
            netOrderRateCB.Dock = DockStyle.Fill;
            netOrderRateCB.Font = new Font("Segoe UI", 6F, FontStyle.Regular, GraphicsUnit.Point, 0);
            netOrderRateCB.Location = new Point(3, 69);
            netOrderRateCB.Name = "netOrderRateCB";
            netOrderRateCB.Size = new Size(85, 19);
            netOrderRateCB.TabIndex = 9;
            netOrderRateCB.Text = "Net Order Rate:";
            // 
            // netOrderRateYesValue
            // 
            netOrderRateYesValue.AutoSize = true;
            netOrderRateYesValue.Dock = DockStyle.Fill;
            netOrderRateYesValue.Font = new Font("Segoe UI", 7F);
            netOrderRateYesValue.Location = new Point(94, 66);
            netOrderRateYesValue.Name = "netOrderRateYesValue";
            netOrderRateYesValue.Size = new Size(23, 25);
            netOrderRateYesValue.TabIndex = 10;
            netOrderRateYesValue.Text = "--";
            netOrderRateYesValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // netOrderRateNoValue
            // 
            netOrderRateNoValue.AutoSize = true;
            netOrderRateNoValue.Dock = DockStyle.Fill;
            netOrderRateNoValue.Font = new Font("Segoe UI", 7F);
            netOrderRateNoValue.Location = new Point(123, 66);
            netOrderRateNoValue.Name = "netOrderRateNoValue";
            netOrderRateNoValue.Size = new Size(26, 25);
            netOrderRateNoValue.TabIndex = 11;
            netOrderRateNoValue.Text = "--";
            netOrderRateNoValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tradeVolumeCB
            // 
            tradeVolumeCB.AutoSize = true;
            tradeVolumeCB.Dock = DockStyle.Fill;
            tradeVolumeCB.Font = new Font("Segoe UI", 6F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tradeVolumeCB.Location = new Point(3, 94);
            tradeVolumeCB.Name = "tradeVolumeCB";
            tradeVolumeCB.Size = new Size(85, 19);
            tradeVolumeCB.TabIndex = 12;
            tradeVolumeCB.Text = "Trade Volume:";
            // 
            // tradeVolumeYesValue
            // 
            tradeVolumeYesValue.AutoSize = true;
            tradeVolumeYesValue.Dock = DockStyle.Fill;
            tradeVolumeYesValue.Font = new Font("Segoe UI", 7F);
            tradeVolumeYesValue.Location = new Point(94, 91);
            tradeVolumeYesValue.Name = "tradeVolumeYesValue";
            tradeVolumeYesValue.Size = new Size(23, 25);
            tradeVolumeYesValue.TabIndex = 13;
            tradeVolumeYesValue.Text = "--";
            tradeVolumeYesValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tradeVolumeNoValue
            // 
            tradeVolumeNoValue.AutoSize = true;
            tradeVolumeNoValue.Dock = DockStyle.Fill;
            tradeVolumeNoValue.Font = new Font("Segoe UI", 7F);
            tradeVolumeNoValue.Location = new Point(123, 91);
            tradeVolumeNoValue.Name = "tradeVolumeNoValue";
            tradeVolumeNoValue.Size = new Size(26, 25);
            tradeVolumeNoValue.TabIndex = 14;
            tradeVolumeNoValue.Text = "--";
            tradeVolumeNoValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // avgTradeSizeCB
            // 
            avgTradeSizeCB.AutoSize = true;
            avgTradeSizeCB.Dock = DockStyle.Fill;
            avgTradeSizeCB.Font = new Font("Segoe UI", 6F, FontStyle.Regular, GraphicsUnit.Point, 0);
            avgTradeSizeCB.Location = new Point(3, 119);
            avgTradeSizeCB.Name = "avgTradeSizeCB";
            avgTradeSizeCB.Size = new Size(85, 19);
            avgTradeSizeCB.TabIndex = 15;
            avgTradeSizeCB.Text = "Avg Trade Size:";
            // 
            // avgTradeSizeYesValue
            // 
            avgTradeSizeYesValue.AutoSize = true;
            avgTradeSizeYesValue.Dock = DockStyle.Fill;
            avgTradeSizeYesValue.Font = new Font("Segoe UI", 7F);
            avgTradeSizeYesValue.Location = new Point(94, 116);
            avgTradeSizeYesValue.Name = "avgTradeSizeYesValue";
            avgTradeSizeYesValue.Size = new Size(23, 25);
            avgTradeSizeYesValue.TabIndex = 16;
            avgTradeSizeYesValue.Text = "--";
            avgTradeSizeYesValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // avgTradeSizeNoValue
            // 
            avgTradeSizeNoValue.AutoSize = true;
            avgTradeSizeNoValue.Dock = DockStyle.Fill;
            avgTradeSizeNoValue.Font = new Font("Segoe UI", 7F);
            avgTradeSizeNoValue.Location = new Point(123, 116);
            avgTradeSizeNoValue.Name = "avgTradeSizeNoValue";
            avgTradeSizeNoValue.Size = new Size(26, 25);
            avgTradeSizeNoValue.TabIndex = 17;
            avgTradeSizeNoValue.Text = "--";
            avgTradeSizeNoValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // slopeCB
            // 
            slopeCB.AutoSize = true;
            slopeCB.Dock = DockStyle.Fill;
            slopeCB.Font = new Font("Segoe UI", 6F, FontStyle.Regular, GraphicsUnit.Point, 0);
            slopeCB.Location = new Point(3, 144);
            slopeCB.Name = "slopeCB";
            slopeCB.Size = new Size(85, 22);
            slopeCB.TabIndex = 18;
            slopeCB.Text = "5 Min Slope:";
            // 
            // slopeYesValue
            // 
            slopeYesValue.AutoSize = true;
            slopeYesValue.Dock = DockStyle.Fill;
            slopeYesValue.Font = new Font("Segoe UI", 7F);
            slopeYesValue.Location = new Point(94, 141);
            slopeYesValue.Name = "slopeYesValue";
            slopeYesValue.Size = new Size(23, 28);
            slopeYesValue.TabIndex = 19;
            slopeYesValue.Text = "--";
            slopeYesValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // slopeNoValue
            // 
            slopeNoValue.AutoSize = true;
            slopeNoValue.Dock = DockStyle.Fill;
            slopeNoValue.Font = new Font("Segoe UI", 7F);
            slopeNoValue.Location = new Point(123, 141);
            slopeNoValue.Name = "slopeNoValue";
            slopeNoValue.Size = new Size(26, 28);
            slopeNoValue.TabIndex = 20;
            slopeNoValue.Text = "--";
            slopeNoValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // contextGrid
            // 
            contextGrid.AutoSize = true;
            contextGrid.ColumnCount = 3;
            contextGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57.2368431F));
            contextGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21.0526314F));
            contextGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21.7105255F));
            contextGrid.Controls.Add(totalDepthNoValue, 2, 4);
            contextGrid.Controls.Add(totalDepthYesValue, 1, 4);
            contextGrid.Controls.Add(totalDepthCB, 0, 4);
            contextGrid.Controls.Add(contextHeaderYes, 1, 0);
            contextGrid.Controls.Add(contextHeaderNo, 2, 0);
            contextGrid.Controls.Add(spreadLabel, 0, 1);
            contextGrid.Controls.Add(spreadValue, 1, 1);
            contextGrid.Controls.Add(imbalCB, 0, 2);
            contextGrid.Controls.Add(imbalValue, 1, 2);
            contextGrid.Controls.Add(depthTop4CB, 0, 3);
            contextGrid.Controls.Add(depthTop4YesValue, 1, 3);
            contextGrid.Controls.Add(depthTop4NoValue, 2, 3);
            contextGrid.Controls.Add(centerMassCB, 0, 5);
            contextGrid.Controls.Add(centerMassYesValue, 1, 5);
            contextGrid.Controls.Add(centerMassNoValue, 2, 5);
            contextGrid.Controls.Add(totalContractsCB, 0, 6);
            contextGrid.Controls.Add(totalContractsYesValue, 1, 6);
            contextGrid.Controls.Add(totalContractsNoValue, 2, 6);
            contextGrid.Dock = DockStyle.Fill;
            contextGrid.Location = new Point(3, 258);
            contextGrid.Name = "contextGrid";
            contextGrid.RowCount = 7;
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 8F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            contextGrid.Size = new Size(152, 165);
            contextGrid.TabIndex = 2;
            // 
            // totalDepthNoValue
            // 
            totalDepthNoValue.AutoSize = true;
            totalDepthNoValue.Dock = DockStyle.Fill;
            totalDepthNoValue.Font = new Font("Segoe UI", 7F);
            totalDepthNoValue.Location = new Point(122, 87);
            totalDepthNoValue.Name = "totalDepthNoValue";
            totalDepthNoValue.Size = new Size(27, 24);
            totalDepthNoValue.TabIndex = 18;
            totalDepthNoValue.Text = "--";
            totalDepthNoValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // totalDepthYesValue
            // 
            totalDepthYesValue.AutoSize = true;
            totalDepthYesValue.Dock = DockStyle.Fill;
            totalDepthYesValue.Font = new Font("Segoe UI", 7F);
            totalDepthYesValue.Location = new Point(90, 87);
            totalDepthYesValue.Name = "totalDepthYesValue";
            totalDepthYesValue.Size = new Size(26, 24);
            totalDepthYesValue.TabIndex = 17;
            totalDepthYesValue.Text = "--";
            totalDepthYesValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // totalDepthCB
            // 
            totalDepthCB.AutoSize = true;
            totalDepthCB.Dock = DockStyle.Fill;
            totalDepthCB.Font = new Font("Segoe UI", 6F, FontStyle.Regular, GraphicsUnit.Point, 0);
            totalDepthCB.Location = new Point(3, 90);
            totalDepthCB.Name = "totalDepthCB";
            totalDepthCB.Size = new Size(81, 18);
            totalDepthCB.TabIndex = 16;
            totalDepthCB.Text = "Total Depth";
            // 
            // contextHeaderYes
            // 
            contextHeaderYes.AutoSize = true;
            contextHeaderYes.Dock = DockStyle.Fill;
            contextHeaderYes.Font = new Font("Segoe UI", 7F);
            contextHeaderYes.Location = new Point(90, 0);
            contextHeaderYes.Name = "contextHeaderYes";
            contextHeaderYes.Size = new Size(26, 15);
            contextHeaderYes.TabIndex = 1;
            contextHeaderYes.Text = "Yes";
            // 
            // contextHeaderNo
            // 
            contextHeaderNo.AutoSize = true;
            contextHeaderNo.Dock = DockStyle.Fill;
            contextHeaderNo.Font = new Font("Segoe UI", 7F);
            contextHeaderNo.Location = new Point(122, 0);
            contextHeaderNo.Name = "contextHeaderNo";
            contextHeaderNo.Size = new Size(27, 15);
            contextHeaderNo.TabIndex = 2;
            contextHeaderNo.Text = "No";
            // 
            // spreadLabel
            // 
            spreadLabel.AutoSize = true;
            spreadLabel.Dock = DockStyle.Fill;
            spreadLabel.Font = new Font("Segoe UI", 7F);
            spreadLabel.Location = new Point(3, 15);
            spreadLabel.Name = "spreadLabel";
            spreadLabel.Size = new Size(81, 24);
            spreadLabel.TabIndex = 3;
            spreadLabel.Text = "Spread:";
            // 
            // spreadValue
            // 
            spreadValue.AutoSize = true;
            contextGrid.SetColumnSpan(spreadValue, 2);
            spreadValue.Dock = DockStyle.Fill;
            spreadValue.Font = new Font("Segoe UI", 7F);
            spreadValue.Location = new Point(90, 15);
            spreadValue.Name = "spreadValue";
            spreadValue.Size = new Size(59, 24);
            spreadValue.TabIndex = 4;
            spreadValue.Text = "--";
            spreadValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // imbalCB
            // 
            imbalCB.AutoSize = true;
            imbalCB.Dock = DockStyle.Fill;
            imbalCB.Font = new Font("Segoe UI", 6F, FontStyle.Regular, GraphicsUnit.Point, 0);
            imbalCB.Location = new Point(3, 42);
            imbalCB.Name = "imbalCB";
            imbalCB.Size = new Size(81, 18);
            imbalCB.TabIndex = 5;
            imbalCB.Text = "Imbal:";
            // 
            // imbalValue
            // 
            imbalValue.AutoSize = true;
            contextGrid.SetColumnSpan(imbalValue, 2);
            imbalValue.Dock = DockStyle.Fill;
            imbalValue.Font = new Font("Segoe UI", 7F);
            imbalValue.Location = new Point(90, 39);
            imbalValue.Name = "imbalValue";
            imbalValue.Size = new Size(59, 24);
            imbalValue.TabIndex = 6;
            imbalValue.Text = "--";
            imbalValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // depthTop4CB
            // 
            depthTop4CB.AutoSize = true;
            depthTop4CB.Dock = DockStyle.Fill;
            depthTop4CB.Font = new Font("Segoe UI", 6F, FontStyle.Regular, GraphicsUnit.Point, 0);
            depthTop4CB.Location = new Point(3, 66);
            depthTop4CB.Name = "depthTop4CB";
            depthTop4CB.Size = new Size(81, 18);
            depthTop4CB.TabIndex = 7;
            depthTop4CB.Text = "Depth Top 4:";
            // 
            // depthTop4YesValue
            // 
            depthTop4YesValue.AutoSize = true;
            depthTop4YesValue.Dock = DockStyle.Fill;
            depthTop4YesValue.Font = new Font("Segoe UI", 7F);
            depthTop4YesValue.Location = new Point(90, 63);
            depthTop4YesValue.Name = "depthTop4YesValue";
            depthTop4YesValue.Size = new Size(26, 24);
            depthTop4YesValue.TabIndex = 8;
            depthTop4YesValue.Text = "--";
            depthTop4YesValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // depthTop4NoValue
            // 
            depthTop4NoValue.AutoSize = true;
            depthTop4NoValue.Dock = DockStyle.Fill;
            depthTop4NoValue.Font = new Font("Segoe UI", 7F);
            depthTop4NoValue.Location = new Point(122, 63);
            depthTop4NoValue.Name = "depthTop4NoValue";
            depthTop4NoValue.Size = new Size(27, 24);
            depthTop4NoValue.TabIndex = 9;
            depthTop4NoValue.Text = "--";
            depthTop4NoValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // centerMassCB
            // 
            centerMassCB.AutoSize = true;
            centerMassCB.Dock = DockStyle.Fill;
            centerMassCB.Font = new Font("Segoe UI", 6F, FontStyle.Regular, GraphicsUnit.Point, 0);
            centerMassCB.Location = new Point(3, 114);
            centerMassCB.Name = "centerMassCB";
            centerMassCB.Size = new Size(81, 18);
            centerMassCB.TabIndex = 10;
            centerMassCB.Text = "Center Mass:";
            // 
            // centerMassYesValue
            // 
            centerMassYesValue.AutoSize = true;
            centerMassYesValue.Dock = DockStyle.Fill;
            centerMassYesValue.Font = new Font("Segoe UI", 7F);
            centerMassYesValue.Location = new Point(90, 111);
            centerMassYesValue.Name = "centerMassYesValue";
            centerMassYesValue.Size = new Size(26, 24);
            centerMassYesValue.TabIndex = 11;
            centerMassYesValue.Text = "--";
            centerMassYesValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // centerMassNoValue
            // 
            centerMassNoValue.AutoSize = true;
            centerMassNoValue.Dock = DockStyle.Fill;
            centerMassNoValue.Font = new Font("Segoe UI", 7F);
            centerMassNoValue.Location = new Point(122, 111);
            centerMassNoValue.Name = "centerMassNoValue";
            centerMassNoValue.Size = new Size(27, 24);
            centerMassNoValue.TabIndex = 12;
            centerMassNoValue.Text = "--";
            centerMassNoValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // totalContractsCB
            // 
            totalContractsCB.AutoSize = true;
            totalContractsCB.Dock = DockStyle.Fill;
            totalContractsCB.Font = new Font("Segoe UI", 6F, FontStyle.Regular, GraphicsUnit.Point, 0);
            totalContractsCB.Location = new Point(3, 138);
            totalContractsCB.Name = "totalContractsCB";
            totalContractsCB.Size = new Size(81, 24);
            totalContractsCB.TabIndex = 13;
            totalContractsCB.Text = "Total Contracts:";
            // 
            // totalContractsYesValue
            // 
            totalContractsYesValue.AutoSize = true;
            totalContractsYesValue.Dock = DockStyle.Fill;
            totalContractsYesValue.Font = new Font("Segoe UI", 7F);
            totalContractsYesValue.Location = new Point(90, 135);
            totalContractsYesValue.Name = "totalContractsYesValue";
            totalContractsYesValue.Size = new Size(26, 30);
            totalContractsYesValue.TabIndex = 14;
            totalContractsYesValue.Text = "--";
            totalContractsYesValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // totalContractsNoValue
            // 
            totalContractsNoValue.AutoSize = true;
            totalContractsNoValue.Dock = DockStyle.Fill;
            totalContractsNoValue.Font = new Font("Segoe UI", 7F);
            totalContractsNoValue.Location = new Point(122, 135);
            totalContractsNoValue.Name = "totalContractsNoValue";
            totalContractsNoValue.Size = new Size(27, 30);
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
            positionsContainer.Size = new Size(575, 193);
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
            positionsLayout.Size = new Size(561, 179);
            positionsLayout.TabIndex = 0;
            positionsLayout.Paint += positionsLayout_Paint;
            // 
            // strategyOutputTextbox
            // 
            strategyOutputTextbox.Dock = DockStyle.Fill;
            strategyOutputTextbox.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            strategyOutputTextbox.Location = new Point(3, 3);
            strategyOutputTextbox.Multiline = true;
            strategyOutputTextbox.Name = "strategyOutputTextbox";
            strategyOutputTextbox.ReadOnly = true;
            strategyOutputTextbox.Size = new Size(391, 192);
            strategyOutputTextbox.TabIndex = 4;
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
            positionsGrid.Location = new Point(400, 3);
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
            positionsGrid.Size = new Size(158, 192);
            positionsGrid.TabIndex = 2;
            // 
            // positionSizeValue
            // 
            positionSizeValue.AutoSize = true;
            positionSizeValue.Dock = DockStyle.Fill;
            positionSizeValue.Font = new Font("Segoe UI", 7F);
            positionSizeValue.Location = new Point(97, 168);
            positionSizeValue.Name = "positionSizeValue";
            positionSizeValue.Size = new Size(58, 24);
            positionSizeValue.TabIndex = 19;
            positionSizeValue.Text = "--";
            positionSizeValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // positionSizeLabel
            // 
            positionSizeLabel.AutoSize = true;
            positionSizeLabel.Dock = DockStyle.Fill;
            positionSizeLabel.Font = new Font("Segoe UI", 7F);
            positionSizeLabel.Location = new Point(3, 171);
            positionSizeLabel.Name = "positionSizeLabel";
            positionSizeLabel.Size = new Size(88, 18);
            positionSizeLabel.TabIndex = 18;
            positionSizeLabel.Text = "Position Size:";
            // 
            // simulatedPositionValue
            // 
            simulatedPositionValue.AutoSize = true;
            simulatedPositionValue.Dock = DockStyle.Fill;
            simulatedPositionValue.Font = new Font("Segoe UI", 7F);
            simulatedPositionValue.Location = new Point(97, 0);
            simulatedPositionValue.Name = "simulatedPositionValue";
            simulatedPositionValue.Size = new Size(58, 24);
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
            simulatedPositionLabel.Size = new Size(88, 18);
            simulatedPositionLabel.TabIndex = 16;
            simulatedPositionLabel.Text = "Simulated Pos:";
            // 
            // lastTradeLabel
            // 
            lastTradeLabel.AutoSize = true;
            lastTradeLabel.Dock = DockStyle.Fill;
            lastTradeLabel.Font = new Font("Segoe UI", 7F);
            lastTradeLabel.Location = new Point(3, 24);
            lastTradeLabel.Name = "lastTradeLabel";
            lastTradeLabel.Size = new Size(88, 24);
            lastTradeLabel.TabIndex = 2;
            lastTradeLabel.Text = "Last Trade:";
            // 
            // lastTradeValue
            // 
            lastTradeValue.AutoSize = true;
            lastTradeValue.Dock = DockStyle.Fill;
            lastTradeValue.Font = new Font("Segoe UI", 7F);
            lastTradeValue.Location = new Point(97, 24);
            lastTradeValue.Name = "lastTradeValue";
            lastTradeValue.Size = new Size(58, 24);
            lastTradeValue.TabIndex = 3;
            lastTradeValue.Text = "--";
            lastTradeValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // positionRoiLabel
            // 
            positionRoiLabel.AutoSize = true;
            positionRoiLabel.Dock = DockStyle.Fill;
            positionRoiLabel.Font = new Font("Segoe UI", 7F);
            positionRoiLabel.Location = new Point(3, 51);
            positionRoiLabel.Name = "positionRoiLabel";
            positionRoiLabel.Size = new Size(88, 18);
            positionRoiLabel.TabIndex = 4;
            positionRoiLabel.Text = "Position ROI:";
            // 
            // positionRoiValue
            // 
            positionRoiValue.AutoSize = true;
            positionRoiValue.Dock = DockStyle.Fill;
            positionRoiValue.Font = new Font("Segoe UI", 7F);
            positionRoiValue.Location = new Point(97, 48);
            positionRoiValue.Name = "positionRoiValue";
            positionRoiValue.Size = new Size(58, 24);
            positionRoiValue.TabIndex = 5;
            positionRoiValue.Text = "--";
            positionRoiValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // buyinPriceLabel
            // 
            buyinPriceLabel.AutoSize = true;
            buyinPriceLabel.Dock = DockStyle.Fill;
            buyinPriceLabel.Font = new Font("Segoe UI", 7F);
            buyinPriceLabel.Location = new Point(3, 72);
            buyinPriceLabel.Name = "buyinPriceLabel";
            buyinPriceLabel.Size = new Size(88, 24);
            buyinPriceLabel.TabIndex = 6;
            buyinPriceLabel.Text = "Buyin Price:";
            // 
            // buyinPriceValue
            // 
            buyinPriceValue.AutoSize = true;
            buyinPriceValue.Dock = DockStyle.Fill;
            buyinPriceValue.Font = new Font("Segoe UI", 7F);
            buyinPriceValue.Location = new Point(97, 72);
            buyinPriceValue.Name = "buyinPriceValue";
            buyinPriceValue.Size = new Size(58, 24);
            buyinPriceValue.TabIndex = 7;
            buyinPriceValue.Text = "--";
            buyinPriceValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // positionUpsideLabel
            // 
            positionUpsideLabel.AutoSize = true;
            positionUpsideLabel.Dock = DockStyle.Fill;
            positionUpsideLabel.Font = new Font("Segoe UI", 7F);
            positionUpsideLabel.Location = new Point(3, 96);
            positionUpsideLabel.Name = "positionUpsideLabel";
            positionUpsideLabel.Size = new Size(88, 24);
            positionUpsideLabel.TabIndex = 8;
            positionUpsideLabel.Text = "Position Upside:";
            // 
            // positionUpsideValue
            // 
            positionUpsideValue.AutoSize = true;
            positionUpsideValue.Dock = DockStyle.Fill;
            positionUpsideValue.Font = new Font("Segoe UI", 7F);
            positionUpsideValue.Location = new Point(97, 96);
            positionUpsideValue.Name = "positionUpsideValue";
            positionUpsideValue.Size = new Size(58, 24);
            positionUpsideValue.TabIndex = 9;
            positionUpsideValue.Text = "--";
            positionUpsideValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // positionDownsideLabel
            // 
            positionDownsideLabel.AutoSize = true;
            positionDownsideLabel.Dock = DockStyle.Fill;
            positionDownsideLabel.Font = new Font("Segoe UI", 7F);
            positionDownsideLabel.Location = new Point(3, 120);
            positionDownsideLabel.Name = "positionDownsideLabel";
            positionDownsideLabel.Size = new Size(88, 24);
            positionDownsideLabel.TabIndex = 10;
            positionDownsideLabel.Text = "Position Downside:";
            // 
            // positionDownsideValue
            // 
            positionDownsideValue.AutoSize = true;
            positionDownsideValue.Dock = DockStyle.Fill;
            positionDownsideValue.Font = new Font("Segoe UI", 7F);
            positionDownsideValue.Location = new Point(97, 120);
            positionDownsideValue.Name = "positionDownsideValue";
            positionDownsideValue.Size = new Size(58, 24);
            positionDownsideValue.TabIndex = 11;
            positionDownsideValue.Text = "--";
            positionDownsideValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // restingOrdersLabel
            // 
            restingOrdersLabel.AutoSize = true;
            restingOrdersLabel.Dock = DockStyle.Fill;
            restingOrdersLabel.Font = new Font("Segoe UI", 7F);
            restingOrdersLabel.Location = new Point(3, 147);
            restingOrdersLabel.Name = "restingOrdersLabel";
            restingOrdersLabel.Size = new Size(88, 18);
            restingOrdersLabel.TabIndex = 12;
            restingOrdersLabel.Text = "Resting Orders:";
            // 
            // restingOrdersValue
            // 
            restingOrdersValue.AutoSize = true;
            restingOrdersValue.Dock = DockStyle.Fill;
            restingOrdersValue.Font = new Font("Segoe UI", 7F);
            restingOrdersValue.Location = new Point(97, 144);
            restingOrdersValue.Name = "restingOrdersValue";
            restingOrdersValue.Size = new Size(58, 24);
            restingOrdersValue.TabIndex = 13;
            restingOrdersValue.Text = "--";
            restingOrdersValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // orderbookContainer
            // 
            orderbookContainer.BorderStyle = BorderStyle.FixedSingle;
            orderbookContainer.Controls.Add(orderbookGrid);
            orderbookContainer.Dock = DockStyle.Fill;
            orderbookContainer.Font = new Font("Segoe UI", 7F);
            orderbookContainer.Location = new Point(587, 455);
            orderbookContainer.Margin = new Padding(4, 3, 4, 3);
            orderbookContainer.Name = "orderbookContainer";
            orderbookContainer.Padding = new Padding(6);
            orderbookContainer.Size = new Size(334, 193);
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
            orderbookGrid.Size = new Size(320, 179);
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
            positionsGrid.ResumeLayout(false);
            positionsGrid.PerformLayout();
            orderbookContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)orderbookGrid).EndInit();
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
        private Label flowHeaderYes;
        private Label flowHeaderNo;
        private CheckBox topVelocityCB;
        private Label topVelocityYesValue;
        private Label topVelocityNoValue;
        private CheckBox bottomVelocityCB;
        private Label bottomVelocityYesValue;
        private Label bottomVelocityNoValue;
        private CheckBox netOrderRateCB;
        private Label netOrderRateYesValue;
        private Label netOrderRateNoValue;
        private CheckBox tradeVolumeCB;
        private Label tradeVolumeYesValue;
        private Label tradeVolumeNoValue;
        private CheckBox avgTradeSizeCB;
        private Label avgTradeSizeYesValue;
        private Label avgTradeSizeNoValue;
        private CheckBox slopeCB;
        private Label slopeYesValue;
        private Label slopeNoValue;
        private TableLayoutPanel contextGrid;
        private Label contextHeaderYes;
        private Label contextHeaderNo;
        private Label spreadLabel;
        private Label spreadValue;
        private CheckBox imbalCB;
        private Label imbalValue;
        private CheckBox depthTop4CB;
        private Label depthTop4YesValue;
        private Label depthTop4NoValue;
        private CheckBox centerMassCB;
        private Label centerMassYesValue;
        private Label centerMassNoValue;
        private CheckBox totalContractsCB;
        private Label totalContractsYesValue;
        private Label totalContractsNoValue;
        private Panel positionsContainer;
        private Panel orderbookContainer;
        private DataGridView orderbookGrid;
        private DataGridViewTextBoxColumn priceCol;
        private DataGridViewTextBoxColumn sizeCol;
        private DataGridViewTextBoxColumn valueCol;
        private Button backButton;
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
        private Label totalDepthNoValue;
        private Label totalDepthYesValue;
        private CheckBox totalDepthCB;
    }
}