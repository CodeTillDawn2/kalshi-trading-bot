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
            positionsGrid = new TableLayoutPanel();
            positionSizeLabel = new CheckBox();
            positionSizeValue = new Label();
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
            simulatedPositionLabel = new CheckBox();
            simulatedPositionValue = new Label();
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
            dashboardGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 75F));
            dashboardGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
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
            chartContainer.Size = new Size(593, 482);
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
            chartLayout.Size = new Size(579, 468);
            chartLayout.TabIndex = 0;
            // 
            // chartControls
            // 
            chartControls.AutoSize = true;
            chartControls.Dock = DockStyle.Fill;
            chartControls.Location = new Point(3, 3);
            chartControls.Name = "chartControls";
            chartControls.Size = new Size(573, 40);
            chartControls.TabIndex = 0;
            // 
            // priceChart
            // 
            priceChart.Dock = DockStyle.Fill;
            priceChart.Location = new Point(4, 49);
            priceChart.Margin = new Padding(4, 3, 4, 3);
            priceChart.Name = "priceChart";
            priceChart.Size = new Size(571, 416);
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
            marketInfoContainer.Size = new Size(316, 482);
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
            infoGrid.Size = new Size(302, 468);
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
            leftColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 44.1558456F));
            leftColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 55.8441544F));
            leftColumn.Size = new Size(160, 462);
            leftColumn.TabIndex = 0;
            leftColumn.Paint += leftColumn_Paint;
            // 
            // pricesGrid
            // 
            pricesGrid.AutoSize = true;
            pricesGrid.ColumnCount = 3;
            pricesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31.818182F));
            pricesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.7662354F));
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
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.67F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 19.1011238F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 13.4831457F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.67F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.67F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.67F));
            pricesGrid.Size = new Size(154, 198);
            pricesGrid.TabIndex = 0;
            // 
            // pricesHeaderEmpty
            // 
            pricesHeaderEmpty.AutoSize = true;
            pricesHeaderEmpty.Dock = DockStyle.Fill;
            pricesHeaderEmpty.Location = new Point(3, 0);
            pricesHeaderEmpty.Name = "pricesHeaderEmpty";
            pricesHeaderEmpty.Size = new Size(43, 33);
            pricesHeaderEmpty.TabIndex = 0;
            // 
            // pricesHeaderNo
            // 
            pricesHeaderNo.AutoSize = true;
            pricesHeaderNo.Dock = DockStyle.Fill;
            pricesHeaderNo.Location = new Point(52, 0);
            pricesHeaderNo.Name = "pricesHeaderNo";
            pricesHeaderNo.Size = new Size(46, 33);
            pricesHeaderNo.TabIndex = 1;
            pricesHeaderNo.Text = "No";
            // 
            // pricesHeaderYes
            // 
            pricesHeaderYes.AutoSize = true;
            pricesHeaderYes.Dock = DockStyle.Fill;
            pricesHeaderYes.Location = new Point(104, 0);
            pricesHeaderYes.Name = "pricesHeaderYes";
            pricesHeaderYes.Size = new Size(47, 33);
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
            allTimeHighLabel.Size = new Size(43, 38);
            allTimeHighLabel.TabIndex = 3;
            allTimeHighLabel.Text = "All Time High";
            // 
            // allTimeHighAsk
            // 
            allTimeHighAsk.AutoSize = true;
            allTimeHighAsk.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            allTimeHighAsk.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            allTimeHighAsk.Controls.Add(allTimeHighAskPrice, 0, 0);
            allTimeHighAsk.Controls.Add(allTimeHighAskTime, 0, 1);
            allTimeHighAsk.Dock = DockStyle.Fill;
            allTimeHighAsk.Font = new Font("Segoe UI", 7F);
            allTimeHighAsk.Location = new Point(52, 36);
            allTimeHighAsk.Name = "allTimeHighAsk";
            allTimeHighAsk.RowCount = 2;
            allTimeHighAsk.RowStyles.Add(new RowStyle(SizeType.Absolute, 13F));
            allTimeHighAsk.RowStyles.Add(new RowStyle(SizeType.Absolute, 27F));
            allTimeHighAsk.Size = new Size(46, 32);
            allTimeHighAsk.TabIndex = 4;
            // 
            // allTimeHighAskPrice
            // 
            allTimeHighAskPrice.AutoSize = true;
            allTimeHighAskPrice.Dock = DockStyle.Fill;
            allTimeHighAskPrice.Font = new Font("Segoe UI", 7F);
            allTimeHighAskPrice.Location = new Point(3, 0);
            allTimeHighAskPrice.Name = "allTimeHighAskPrice";
            allTimeHighAskPrice.Size = new Size(40, 13);
            allTimeHighAskPrice.TabIndex = 0;
            allTimeHighAskPrice.Text = "--";
            // 
            // allTimeHighAskTime
            // 
            allTimeHighAskTime.AutoSize = true;
            allTimeHighAskTime.Dock = DockStyle.Fill;
            allTimeHighAskTime.Font = new Font("Segoe UI", 7F);
            allTimeHighAskTime.Location = new Point(3, 13);
            allTimeHighAskTime.Name = "allTimeHighAskTime";
            allTimeHighAskTime.Size = new Size(40, 27);
            allTimeHighAskTime.TabIndex = 1;
            allTimeHighAskTime.Text = "--";
            // 
            // allTimeHighBid
            // 
            allTimeHighBid.AutoSize = true;
            allTimeHighBid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            allTimeHighBid.Controls.Add(allTimeHighBidPrice, 0, 0);
            allTimeHighBid.Controls.Add(allTimeHighBidTime, 0, 1);
            allTimeHighBid.Dock = DockStyle.Fill;
            allTimeHighBid.Font = new Font("Segoe UI", 7F);
            allTimeHighBid.Location = new Point(104, 36);
            allTimeHighBid.Name = "allTimeHighBid";
            allTimeHighBid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            allTimeHighBid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            allTimeHighBid.Size = new Size(47, 32);
            allTimeHighBid.TabIndex = 5;
            // 
            // allTimeHighBidPrice
            // 
            allTimeHighBidPrice.AutoSize = true;
            allTimeHighBidPrice.Dock = DockStyle.Fill;
            allTimeHighBidPrice.Font = new Font("Segoe UI", 7F);
            allTimeHighBidPrice.Location = new Point(3, 0);
            allTimeHighBidPrice.Name = "allTimeHighBidPrice";
            allTimeHighBidPrice.Size = new Size(13, 20);
            allTimeHighBidPrice.TabIndex = 0;
            allTimeHighBidPrice.Text = "--";
            // 
            // allTimeHighBidTime
            // 
            allTimeHighBidTime.AutoSize = true;
            allTimeHighBidTime.Dock = DockStyle.Fill;
            allTimeHighBidTime.Font = new Font("Segoe UI", 7F);
            allTimeHighBidTime.Location = new Point(31, 20);
            allTimeHighBidTime.Name = "allTimeHighBidTime";
            allTimeHighBidTime.Size = new Size(13, 20);
            allTimeHighBidTime.TabIndex = 1;
            allTimeHighBidTime.Text = "--";
            // 
            // recentHighLabel
            // 
            recentHighLabel.AutoSize = true;
            recentHighLabel.Dock = DockStyle.Fill;
            recentHighLabel.Location = new Point(3, 71);
            recentHighLabel.Font = new Font("Segoe UI", 7F);
            recentHighLabel.Name = "recentHighLabel";
            recentHighLabel.Size = new Size(43, 26);
            recentHighLabel.TabIndex = 6;
            recentHighLabel.Text = "Recent High";
            // 
            // recentHighAsk
            // 
            recentHighAsk.AutoSize = true;
            recentHighAsk.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            recentHighAsk.Controls.Add(recentHighAskPrice, 0, 0);
            recentHighAsk.Controls.Add(recentHighAskTime, 0, 1);
            recentHighAsk.Dock = DockStyle.Fill;
            recentHighAsk.Location = new Point(52, 74);
            recentHighAsk.Name = "recentHighAsk";
            recentHighAsk.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            recentHighAsk.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            recentHighAsk.Size = new Size(46, 20);
            recentHighAsk.TabIndex = 7;
            // 
            // recentHighAskPrice
            // 
            recentHighAskPrice.AutoSize = true;
            recentHighAskPrice.Dock = DockStyle.Fill;
            recentHighAskPrice.Location = new Point(3, 0);
            recentHighAskPrice.Name = "recentHighAskPrice";
            recentHighAskPrice.Font = new Font("Segoe UI", 7F);
            recentHighAskPrice.Size = new Size(17, 20);
            recentHighAskPrice.TabIndex = 0;
            recentHighAskPrice.Text = "--";
            // 
            // recentHighAskTime
            // 
            recentHighAskTime.AutoSize = true;
            recentHighAskTime.Dock = DockStyle.Fill;
            recentHighAskTime.Location = new Point(26, 20);
            recentHighAskTime.Name = "recentHighAskTime";
            recentHighAskTime.Font = new Font("Segoe UI", 7F);
            recentHighAskTime.Size = new Size(17, 20);
            recentHighAskTime.TabIndex = 1;
            recentHighAskTime.Text = "--";
            // 
            // recentHighBid
            // 
            recentHighBid.AutoSize = true;
            recentHighBid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            recentHighBid.Controls.Add(recentHighBidPrice, 0, 0);
            recentHighBid.Controls.Add(recentHighBidTime, 0, 1);
            recentHighBid.Dock = DockStyle.Fill;
            recentHighBid.Location = new Point(104, 74);
            recentHighBid.Name = "recentHighBid";
            recentHighBid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            recentHighBid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            recentHighBid.Size = new Size(47, 20);
            recentHighBid.TabIndex = 8;
            // 
            // recentHighBidPrice
            // 
            recentHighBidPrice.AutoSize = true;
            recentHighBidPrice.Dock = DockStyle.Fill;
            recentHighBidPrice.Location = new Point(3, 0);
            recentHighBidPrice.Name = "recentHighBidPrice";
            recentHighBidPrice.Font = new Font("Segoe UI", 7F);
            recentHighBidPrice.Size = new Size(17, 20);
            recentHighBidPrice.TabIndex = 0;
            recentHighBidPrice.Text = "--";
            // 
            // recentHighBidTime
            // 
            recentHighBidTime.AutoSize = true;
            recentHighBidTime.Dock = DockStyle.Fill;
            recentHighBidTime.Location = new Point(27, 20);
            recentHighBidTime.Name = "recentHighBidTime";
            recentHighBidTime.Font = new Font("Segoe UI", 7F);
            recentHighBidTime.Size = new Size(17, 20);
            recentHighBidTime.TabIndex = 1;
            recentHighBidTime.Text = "--";
            // 
            // currentPriceLabel
            // 
            currentPriceLabel.AutoSize = true;
            currentPriceLabel.Dock = DockStyle.Fill;
            currentPriceLabel.Location = new Point(3, 97);
            currentPriceLabel.Name = "currentPriceLabel";
            currentPriceLabel.Font = new Font("Segoe UI", 7F);
            currentPriceLabel.Size = new Size(43, 33);
            currentPriceLabel.TabIndex = 9;
            currentPriceLabel.Text = "Current";
            // 
            // currentPriceAsk
            // 
            currentPriceAsk.AutoSize = true;
            currentPriceAsk.Dock = DockStyle.Fill;
            currentPriceAsk.Location = new Point(52, 97);
            currentPriceAsk.Name = "currentPriceAsk";
            currentPriceAsk.Font = new Font("Segoe UI", 7F);
            currentPriceAsk.Size = new Size(46, 33);
            currentPriceAsk.TabIndex = 10;
            currentPriceAsk.Text = "--";
            // 
            // currentPriceBid
            // 
            currentPriceBid.AutoSize = true;
            currentPriceBid.Dock = DockStyle.Fill;
            currentPriceBid.Location = new Point(104, 97);
            currentPriceBid.Name = "currentPriceBid";
            currentPriceBid.Font = new Font("Segoe UI", 7F);
            currentPriceBid.Size = new Size(47, 33);
            currentPriceBid.TabIndex = 11;
            currentPriceBid.Text = "--";
            // 
            // recentLowLabel
            // 
            recentLowLabel.AutoSize = true;
            recentLowLabel.Dock = DockStyle.Fill;
            recentLowLabel.Location = new Point(3, 130);
            recentLowLabel.Name = "recentLowLabel";
            recentLowLabel.Font = new Font("Segoe UI", 7F);
            recentLowLabel.Size = new Size(43, 33);
            recentLowLabel.TabIndex = 12;
            recentLowLabel.Text = "Recent Low";
            // 
            // recentLowAsk
            // 
            recentLowAsk.AutoSize = true;
            recentLowAsk.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            recentLowAsk.Controls.Add(recentLowAskPrice, 0, 0);
            recentLowAsk.Controls.Add(recentLowAskTime, 0, 1);
            recentLowAsk.Dock = DockStyle.Fill;
            recentLowAsk.Location = new Point(52, 133);
            recentLowAsk.Name = "recentLowAsk";
            recentLowAsk.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            recentLowAsk.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            recentLowAsk.Size = new Size(46, 27);
            recentLowAsk.TabIndex = 13;
            // 
            // recentLowAskPrice
            // 
            recentLowAskPrice.AutoSize = true;
            recentLowAskPrice.Dock = DockStyle.Fill;
            recentLowAskPrice.Location = new Point(3, 0);
            recentLowAskPrice.Font = new Font("Segoe UI", 7F);
            recentLowAskPrice.Name = "recentLowAskPrice";
            recentLowAskPrice.Size = new Size(17, 20);
            recentLowAskPrice.TabIndex = 0;
            recentLowAskPrice.Text = "--";
            // 
            // recentLowAskTime
            // 
            recentLowAskTime.AutoSize = true;
            recentLowAskTime.Dock = DockStyle.Fill;
            recentLowAskTime.Location = new Point(26, 20);
            recentLowAskTime.Font = new Font("Segoe UI", 7F);
            recentLowAskTime.Name = "recentLowAskTime";
            recentLowAskTime.Size = new Size(17, 20);
            recentLowAskTime.TabIndex = 1;
            recentLowAskTime.Text = "--";
            // 
            // recentLowBid
            // 
            recentLowBid.AutoSize = true;
            recentLowBid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            recentLowBid.Controls.Add(recentLowBidPrice, 0, 0);
            recentLowBid.Controls.Add(recentLowBidTime, 0, 1);
            recentLowBid.Dock = DockStyle.Fill;
            recentLowBid.Location = new Point(104, 133);
            recentLowBid.Name = "recentLowBid";
            recentLowBid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            recentLowBid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            recentLowBid.Size = new Size(47, 27);
            recentLowBid.TabIndex = 14;
            // 
            // recentLowBidPrice
            // 
            recentLowBidPrice.AutoSize = true;
            recentLowBidPrice.Dock = DockStyle.Fill;
            recentLowBidPrice.Location = new Point(3, 0);
            recentLowBidPrice.Font = new Font("Segoe UI", 7F);
            recentLowBidPrice.Name = "recentLowBidPrice";
            recentLowBidPrice.Size = new Size(17, 20);
            recentLowBidPrice.TabIndex = 0;
            recentLowBidPrice.Text = "--";
            // 
            // recentLowBidTime
            // 
            recentLowBidTime.AutoSize = true;
            recentLowBidTime.Dock = DockStyle.Fill;
            recentLowBidTime.Location = new Point(27, 20);
            recentLowBidTime.Name = "recentLowBidTime";
            recentLowBidTime.Size = new Size(17, 20);
            recentLowBidTime.Font = new Font("Segoe UI", 7F);
            recentLowBidTime.TabIndex = 1;
            recentLowBidTime.Text = "--";
            // 
            // allTimeLowLabel
            // 
            allTimeLowLabel.AutoSize = true;
            allTimeLowLabel.Dock = DockStyle.Fill;
            allTimeLowLabel.Location = new Point(3, 163);
            allTimeLowLabel.Name = "allTimeLowLabel";
            allTimeLowLabel.Font = new Font("Segoe UI", 7F);
            allTimeLowLabel.Size = new Size(43, 35);
            allTimeLowLabel.TabIndex = 15;
            allTimeLowLabel.Text = "All Time Low";
            // 
            // allTimeLowAsk
            // 
            allTimeLowAsk.AutoSize = true;
            allTimeLowAsk.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            allTimeLowAsk.Controls.Add(allTimeLowAskPrice, 0, 0);
            allTimeLowAsk.Controls.Add(allTimeLowAskTime, 0, 1);
            allTimeLowAsk.Dock = DockStyle.Fill;
            allTimeLowAsk.Location = new Point(52, 166);
            allTimeLowAsk.Name = "allTimeLowAsk";
            allTimeLowAsk.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            allTimeLowAsk.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            allTimeLowAsk.Size = new Size(46, 29);
            allTimeLowAsk.TabIndex = 16;
            // 
            // allTimeLowAskPrice
            // 
            allTimeLowAskPrice.AutoSize = true;
            allTimeLowAskPrice.Dock = DockStyle.Fill;
            allTimeLowAskPrice.Location = new Point(3, 0);
            allTimeLowAskPrice.Name = "allTimeLowAskPrice";
            allTimeLowAskPrice.Font = new Font("Segoe UI", 7F);
            allTimeLowAskPrice.Size = new Size(17, 20);
            allTimeLowAskPrice.TabIndex = 0;
            allTimeLowAskPrice.Text = "--";
            // 
            // allTimeLowAskTime
            // 
            allTimeLowAskTime.AutoSize = true;
            allTimeLowAskTime.Dock = DockStyle.Fill;
            allTimeLowAskTime.Location = new Point(26, 20);
            allTimeLowAskTime.Font = new Font("Segoe UI", 7F);
            allTimeLowAskTime.Name = "allTimeLowAskTime";
            allTimeLowAskTime.Size = new Size(17, 20);
            allTimeLowAskTime.TabIndex = 1;
            allTimeLowAskTime.Text = "--";
            // 
            // allTimeLowBid
            // 
            allTimeLowBid.AutoSize = true;
            allTimeLowBid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            allTimeLowBid.Controls.Add(allTimeLowBidPrice, 0, 0);
            allTimeLowBid.Controls.Add(allTimeLowBidTime, 0 , 1);
            allTimeLowBid.Dock = DockStyle.Fill;
            allTimeLowBid.Location = new Point(104, 166);
            allTimeLowBid.Name = "allTimeLowBid";
            allTimeLowBid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            allTimeLowBid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            allTimeLowBid.Size = new Size(47, 29);
            allTimeLowBid.TabIndex = 17;
            // 
            // allTimeLowBidPrice
            // 
            allTimeLowBidPrice.AutoSize = true;
            allTimeLowBidPrice.Dock = DockStyle.Left;
            allTimeLowBidPrice.Location = new Point(3, 0);
            allTimeLowBidPrice.Font = new Font("Segoe UI", 7F);
            allTimeLowBidPrice.Name = "allTimeLowBidPrice";
            allTimeLowBidPrice.Size = new Size(17, 20);
            allTimeLowBidPrice.TabIndex = 0;
            allTimeLowBidPrice.Text = "--";
            // 
            // allTimeLowBidTime
            // 
            allTimeLowBidTime.AutoSize = true;
            allTimeLowBidTime.Dock = DockStyle.Right;
            allTimeLowBidTime.Location = new Point(27, 20);
            allTimeLowBidTime.Font = new Font("Segoe UI", 7F);
            allTimeLowBidTime.Name = "allTimeLowBidTime";
            allTimeLowBidTime.Size = new Size(17, 20);
            allTimeLowBidTime.TabIndex = 1;
            allTimeLowBidTime.Text = "--";
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
            tradingMetricsGrid.Location = new Point(3, 207);
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
            tradingMetricsGrid.Size = new Size(154, 252);
            tradingMetricsGrid.TabIndex = 1;
            // 
            // tradingMetricsHeader
            // 
            tradingMetricsHeader.AutoSize = true;
            tradingMetricsGrid.SetColumnSpan(tradingMetricsHeader, 2);
            tradingMetricsHeader.Dock = DockStyle.Fill;
            tradingMetricsHeader.Location = new Point(3, 0);
            tradingMetricsHeader.Name = "tradingMetricsHeader";
            tradingMetricsHeader.Size = new Size(148, 22);
            tradingMetricsHeader.TabIndex = 0;
            tradingMetricsHeader.Text = "Market Header";
            // 
            // rsiLabel
            // 
            rsiLabel.AutoSize = true;
            rsiLabel.Dock = DockStyle.Fill;
            rsiLabel.Location = new Point(3, 25);
            rsiLabel.Name = "rsiLabel";
            rsiLabel.Font = new Font("Segoe UI", 7F);
            rsiLabel.Size = new Size(86, 16);
            rsiLabel.TabIndex = 1;
            rsiLabel.Text = "RSI:";
            // 
            // rsiValue
            // 
            rsiValue.AutoSize = true;
            rsiValue.Dock = DockStyle.Fill;
            rsiValue.Location = new Point(95, 22);
            rsiValue.Font = new Font("Segoe UI", 7F);
            rsiValue.Name = "rsiValue";
            rsiValue.Size = new Size(56, 22);
            rsiValue.TabIndex = 2;
            rsiValue.Text = "--";
            // 
            // macdLabel
            // 
            macdLabel.AutoSize = true;
            macdLabel.Dock = DockStyle.Fill;
            macdLabel.Location = new Point(3, 47);
            macdLabel.Font = new Font("Segoe UI", 7F);
            macdLabel.Name = "macdLabel";
            macdLabel.Size = new Size(86, 16);
            macdLabel.TabIndex = 3;
            macdLabel.Text = "MACD:";
            // 
            // macdValue
            // 
            macdValue.AutoSize = true;
            macdValue.Dock = DockStyle.Fill;
            macdValue.Location = new Point(95, 44);
            macdValue.Font = new Font("Segoe UI", 7F);
            macdValue.Name = "macdValue";
            macdValue.Size = new Size(56, 22);
            macdValue.TabIndex = 4;
            macdValue.Text = "--";
            // 
            // emaLabel
            // 
            emaLabel.AutoSize = true;
            emaLabel.Dock = DockStyle.Fill;
            emaLabel.Location = new Point(3, 69);
            emaLabel.Font = new Font("Segoe UI", 7F);
            emaLabel.Name = "emaLabel";
            emaLabel.Size = new Size(86, 16);
            emaLabel.TabIndex = 5;
            emaLabel.Text = "EMA:";
            // 
            // emaValue
            // 
            emaValue.AutoSize = true;
            emaValue.Dock = DockStyle.Fill;
            emaValue.Location = new Point(95, 66);
            emaValue.Font = new Font("Segoe UI", 7F);
            emaValue.Name = "emaValue";
            emaValue.Size = new Size(56, 22);
            emaValue.TabIndex = 6;
            emaValue.Text = "--";
            // 
            // bollingerLabel
            // 
            bollingerLabel.AutoSize = true;
            bollingerLabel.Dock = DockStyle.Fill;
            bollingerLabel.Location = new Point(3, 91);
            bollingerLabel.Font = new Font("Segoe UI", 7F);
            bollingerLabel.Name = "bollingerLabel";
            bollingerLabel.Size = new Size(86, 16);
            bollingerLabel.TabIndex = 7;
            bollingerLabel.Text = "Bollinger:";
            // 
            // bollingerValue
            // 
            bollingerValue.AutoSize = true;
            bollingerValue.Dock = DockStyle.Fill;
            bollingerValue.Location = new Point(95, 88);
            bollingerValue.Font = new Font("Segoe UI", 7F);
            bollingerValue.Name = "bollingerValue";
            bollingerValue.Size = new Size(56, 22);
            bollingerValue.TabIndex = 8;
            bollingerValue.Text = "--";
            // 
            // atrLabel
            // 
            atrLabel.AutoSize = true;
            atrLabel.Dock = DockStyle.Fill;
            atrLabel.Location = new Point(3, 113);
            atrLabel.Font = new Font("Segoe UI", 7F);
            atrLabel.Name = "atrLabel";
            atrLabel.Size = new Size(86, 16);
            atrLabel.TabIndex = 9;
            atrLabel.Text = "ATR:";
            // 
            // atrValue
            // 
            atrValue.AutoSize = true;
            atrValue.Dock = DockStyle.Fill;
            atrValue.Location = new Point(95, 110);
            atrValue.Font = new Font("Segoe UI", 7F);
            atrValue.Name = "atrValue";
            atrValue.Size = new Size(56, 22);
            atrValue.TabIndex = 10;
            atrValue.Text = "--";
            // 
            // vwapLabel
            // 
            vwapLabel.AutoSize = true;
            vwapLabel.Dock = DockStyle.Fill;
            vwapLabel.Location = new Point(3, 135);
            vwapLabel.Font = new Font("Segoe UI", 7F);
            vwapLabel.Name = "vwapLabel";
            vwapLabel.Size = new Size(86, 16);
            vwapLabel.TabIndex = 11;
            vwapLabel.Text = "VWAP:";
            // 
            // vwapValue
            // 
            vwapValue.AutoSize = true;
            vwapValue.Dock = DockStyle.Fill;
            vwapValue.Location = new Point(95, 132);
            vwapValue.Font = new Font("Segoe UI", 7F);
            vwapValue.Name = "vwapValue";
            vwapValue.Size = new Size(56, 22);
            vwapValue.TabIndex = 12;
            vwapValue.Text = "--";
            // 
            // stochasticLabel
            // 
            stochasticLabel.AutoSize = true;
            stochasticLabel.Dock = DockStyle.Fill;
            stochasticLabel.Location = new Point(3, 157);
            stochasticLabel.Font = new Font("Segoe UI", 7F);
            stochasticLabel.Name = "stochasticLabel";
            stochasticLabel.Size = new Size(86, 16);
            stochasticLabel.TabIndex = 13;
            stochasticLabel.Text = "Stochastic:";
            // 
            // stochasticValue
            // 
            stochasticValue.AutoSize = true;
            stochasticValue.Dock = DockStyle.Fill;
            stochasticValue.Location = new Point(95, 154);
            stochasticValue.Font = new Font("Segoe UI", 7F);
            stochasticValue.Name = "stochasticValue";
            stochasticValue.Size = new Size(56, 22);
            stochasticValue.TabIndex = 14;
            stochasticValue.Text = "--";
            // 
            // obvLabel
            // 
            obvLabel.AutoSize = true;
            obvLabel.Dock = DockStyle.Fill;
            obvLabel.Location = new Point(3, 179);
            obvLabel.Font = new Font("Segoe UI", 7F);
            obvLabel.Name = "obvLabel";
            obvLabel.Size = new Size(86, 16);
            obvLabel.TabIndex = 15;
            obvLabel.Text = "OBV:";
            // 
            // obvValue
            // 
            obvValue.AutoSize = true;
            obvValue.Dock = DockStyle.Fill;
            obvValue.Location = new Point(95, 176);
            obvValue.Font = new Font("Segoe UI", 7F);
            obvValue.Name = "obvValue";
            obvValue.Size = new Size(56, 22);
            obvValue.TabIndex = 16;
            obvValue.Text = "--";
            // 
            // psarLabel
            // 
            psarLabel.AutoSize = true;
            psarLabel.Dock = DockStyle.Fill;
            psarLabel.Location = new Point(3, 201);
            psarLabel.Font = new Font("Segoe UI", 7F);
            psarLabel.Name = "psarLabel";
            psarLabel.Size = new Size(86, 16);
            psarLabel.TabIndex = 17;
            psarLabel.Text = "PSAR:";
            // 
            // psarValue
            // 
            psarValue.AutoSize = true;
            psarValue.Dock = DockStyle.Fill;
            psarValue.Location = new Point(95, 198);
            psarValue.Font = new Font("Segoe UI", 7F);
            psarValue.Name = "psarValue";
            psarValue.Size = new Size(56, 22);
            psarValue.TabIndex = 18;
            psarValue.Text = "--";
            // 
            // adxLabel
            // 
            adxLabel.AutoSize = true;
            adxLabel.Dock = DockStyle.Fill;
            adxLabel.Location = new Point(3, 223);
            adxLabel.Font = new Font("Segoe UI", 7F);
            adxLabel.Name = "adxLabel";
            adxLabel.Size = new Size(86, 26);
            adxLabel.TabIndex = 19;
            adxLabel.Text = "ADX:";
            // 
            // adxValue
            // 
            adxValue.AutoSize = true;
            adxValue.Dock = DockStyle.Fill;
            adxValue.Location = new Point(95, 220);
            adxValue.Font = new Font("Segoe UI", 7F);
            adxValue.Name = "adxValue";
            adxValue.Size = new Size(56, 32);
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
            rightColumn.Size = new Size(130, 462);
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
            otherInfoGrid.Size = new Size(124, 86);
            otherInfoGrid.TabIndex = 0;
            // 
            // CategoryLabel
            // 
            CategoryLabel.AutoSize = true;
            CategoryLabel.Dock = DockStyle.Fill;
            CategoryLabel.Location = new Point(3, 0);
            CategoryLabel.Name = "CategoryLabel";
            CategoryLabel.Font = new Font("Segoe UI", 7F);
            CategoryLabel.Size = new Size(68, 28);
            CategoryLabel.TabIndex = 0;
            CategoryLabel.Text = "Category:";
            // 
            // categoryValue
            // 
            categoryValue.AutoSize = true;
            categoryValue.Dock = DockStyle.Fill;
            categoryValue.Location = new Point(77, 0);
            categoryValue.Font = new Font("Segoe UI", 7F);
            categoryValue.Name = "categoryValue";
            categoryValue.Size = new Size(44, 28);
            categoryValue.TabIndex = 1;
            categoryValue.Text = "--";
            // 
            // timeLeftLabel
            // 
            timeLeftLabel.AutoSize = true;
            timeLeftLabel.Dock = DockStyle.Fill;
            timeLeftLabel.Location = new Point(3, 28);
            timeLeftLabel.Font = new Font("Segoe UI", 7F);
            timeLeftLabel.Name = "timeLeftLabel";
            timeLeftLabel.Size = new Size(68, 28);
            timeLeftLabel.TabIndex = 2;
            timeLeftLabel.Text = "Time Left:";
            // 
            // timeLeftValue
            // 
            timeLeftValue.AutoSize = true;
            timeLeftValue.Dock = DockStyle.Fill;
            timeLeftValue.Location = new Point(77, 28);
            timeLeftValue.Font = new Font("Segoe UI", 7F);
            timeLeftValue.Name = "timeLeftValue";
            timeLeftValue.Size = new Size(44, 28);
            timeLeftValue.TabIndex = 3;
            timeLeftValue.Text = "--";
            // 
            // marketAgeLabel
            // 
            marketAgeLabel.AutoSize = true;
            marketAgeLabel.Dock = DockStyle.Fill;
            marketAgeLabel.Location = new Point(3, 56);
            marketAgeLabel.Font = new Font("Segoe UI", 7F);
            marketAgeLabel.Name = "marketAgeLabel";
            marketAgeLabel.Size = new Size(68, 30);
            marketAgeLabel.TabIndex = 4;
            marketAgeLabel.Text = "Market Age:";
            // 
            // marketAgeValue
            // 
            marketAgeValue.AutoSize = true;
            marketAgeValue.Dock = DockStyle.Fill;
            marketAgeValue.Location = new Point(77, 56);
            marketAgeValue.Font = new Font("Segoe UI", 7F);
            marketAgeValue.Name = "marketAgeValue";
            marketAgeValue.Size = new Size(44, 30);
            marketAgeValue.TabIndex = 5;
            marketAgeValue.Text = "--";
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
            flowMomentumGrid.Location = new Point(3, 95);
            flowMomentumGrid.Name = "flowMomentumGrid";
            flowMomentumGrid.RowCount = 7;
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            flowMomentumGrid.Size = new Size(124, 178);
            flowMomentumGrid.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Dock = DockStyle.Fill;
            label1.Location = new Point(3, 22);
            label1.Name = "label1";
            label1.Size = new Size(56, 22);
            label1.TabIndex = 21;
            // 
            // flowHeader
            // 
            flowHeader.AutoSize = true;
            flowMomentumGrid.SetColumnSpan(flowHeader, 3);
            flowHeader.Dock = DockStyle.Fill;
            flowHeader.Location = new Point(3, 0);
            flowHeader.Font = new Font("Segoe UI", 7F);
            flowHeader.Name = "flowHeader";
            flowHeader.Size = new Size(118, 22);
            flowHeader.TabIndex = 0;
            flowHeader.Text = "Flow/Momentum";
            // 
            // flowHeaderYes
            // 
            flowHeaderYes.AutoSize = true;
            flowHeaderYes.Dock = DockStyle.Fill;
            flowHeaderYes.Location = new Point(65, 22);
            flowHeaderYes.Name = "flowHeaderYes";
            flowHeaderYes.Font = new Font("Segoe UI", 7F);
            flowHeaderYes.Size = new Size(25, 22);
            flowHeaderYes.TabIndex = 1;
            flowHeaderYes.Text = "Yes";
            // 
            // flowHeaderNo
            // 
            flowHeaderNo.AutoSize = true;
            flowHeaderNo.Dock = DockStyle.Fill;
            flowHeaderNo.Location = new Point(96, 22);
            flowHeaderNo.Name = "flowHeaderNo";
            flowHeaderNo.Font = new Font("Segoe UI", 7F);
            flowHeaderNo.Size = new Size(25, 22);
            flowHeaderNo.TabIndex = 2;
            flowHeaderNo.Text = "No";
            // 
            // topVelocityLabel
            // 
            topVelocityLabel.AutoSize = true;
            topVelocityLabel.Dock = DockStyle.Fill;
            topVelocityLabel.Location = new Point(3, 47);
            topVelocityLabel.Font = new Font("Segoe UI", 7F);
            topVelocityLabel.Name = "topVelocityLabel";
            topVelocityLabel.Size = new Size(56, 16);
            topVelocityLabel.TabIndex = 3;
            topVelocityLabel.Text = "Top Velocity:";
            // 
            // topVelocityYesValue
            // 
            topVelocityYesValue.AutoSize = true;
            topVelocityYesValue.Dock = DockStyle.Fill;
            topVelocityYesValue.Location = new Point(65, 44);
            topVelocityYesValue.Name = "topVelocityYesValue";
            topVelocityYesValue.Font = new Font("Segoe UI", 7F);
            topVelocityYesValue.Size = new Size(25, 22);
            topVelocityYesValue.TabIndex = 4;
            topVelocityYesValue.Text = "--";
            // 
            // topVelocityNoValue
            // 
            topVelocityNoValue.AutoSize = true;
            topVelocityNoValue.Dock = DockStyle.Fill;
            topVelocityNoValue.Location = new Point(96, 44);
            topVelocityNoValue.Name = "topVelocityNoValue";
            topVelocityNoValue.Font = new Font("Segoe UI", 7F);
            topVelocityNoValue.Size = new Size(25, 22);
            topVelocityNoValue.TabIndex = 5;
            topVelocityNoValue.Text = "--";
            // 
            // bottomVelocityLabel
            // 
            bottomVelocityLabel.AutoSize = true;
            bottomVelocityLabel.Dock = DockStyle.Fill;
            bottomVelocityLabel.Location = new Point(3, 69);
            bottomVelocityLabel.Name = "bottomVelocityLabel";
            bottomVelocityLabel.Font = new Font("Segoe UI", 7F);
            bottomVelocityLabel.Size = new Size(56, 16);
            bottomVelocityLabel.TabIndex = 6;
            bottomVelocityLabel.Text = "Bottom Velocity:";
            // 
            // bottomVelocityYesValue
            // 
            bottomVelocityYesValue.AutoSize = true;
            bottomVelocityYesValue.Dock = DockStyle.Fill;
            bottomVelocityYesValue.Location = new Point(65, 66);
            bottomVelocityYesValue.Name = "bottomVelocityYesValue";
            bottomVelocityYesValue.Font = new Font("Segoe UI", 7F);
            bottomVelocityYesValue.Size = new Size(25, 22);
            bottomVelocityYesValue.TabIndex = 7;
            bottomVelocityYesValue.Text = "--";
            // 
            // bottomVelocityNoValue
            // 
            bottomVelocityNoValue.AutoSize = true;
            bottomVelocityNoValue.Dock = DockStyle.Fill;
            bottomVelocityNoValue.Location = new Point(96, 66);
            bottomVelocityNoValue.Name = "bottomVelocityNoValue";
            bottomVelocityNoValue.Font = new Font("Segoe UI", 7F);
            bottomVelocityNoValue.Size = new Size(25, 22);
            bottomVelocityNoValue.TabIndex = 8;
            bottomVelocityNoValue.Text = "--";
            // 
            // netOrderRateLabel
            // 
            netOrderRateLabel.AutoSize = true;
            netOrderRateLabel.Dock = DockStyle.Fill;
            netOrderRateLabel.Location = new Point(3, 91);
            netOrderRateLabel.Name = "netOrderRateLabel";
            netOrderRateLabel.Font = new Font("Segoe UI", 7F);
            netOrderRateLabel.Size = new Size(56, 16);
            netOrderRateLabel.TabIndex = 9;
            netOrderRateLabel.Text = "Net Order Rate:";
            // 
            // netOrderRateYesValue
            // 
            netOrderRateYesValue.AutoSize = true;
            netOrderRateYesValue.Dock = DockStyle.Fill;
            netOrderRateYesValue.Location = new Point(65, 88);
            netOrderRateYesValue.Name = "netOrderRateYesValue";
            netOrderRateYesValue.Font = new Font("Segoe UI", 7F);
            netOrderRateYesValue.Size = new Size(25, 22);
            netOrderRateYesValue.TabIndex = 10;
            netOrderRateYesValue.Text = "--";
            // 
            // netOrderRateNoValue
            // 
            netOrderRateNoValue.AutoSize = true;
            netOrderRateNoValue.Dock = DockStyle.Fill;
            netOrderRateNoValue.Location = new Point(96, 88);
            netOrderRateNoValue.Name = "netOrderRateNoValue";
            netOrderRateNoValue.Font = new Font("Segoe UI", 7F);
            netOrderRateNoValue.Size = new Size(25, 22);
            netOrderRateNoValue.TabIndex = 11;
            netOrderRateNoValue.Text = "--";
            // 
            // tradeVolumeLabel
            // 
            tradeVolumeLabel.AutoSize = true;
            tradeVolumeLabel.Dock = DockStyle.Fill;
            tradeVolumeLabel.Location = new Point(3, 113);
            tradeVolumeLabel.Name = "tradeVolumeLabel";
            tradeVolumeLabel.Font = new Font("Segoe UI", 7F);
            tradeVolumeLabel.Size = new Size(56, 16);
            tradeVolumeLabel.TabIndex = 12;
            tradeVolumeLabel.Text = "Trade Volume:";
            // 
            // tradeVolumeYesValue
            // 
            tradeVolumeYesValue.AutoSize = true;
            tradeVolumeYesValue.Dock = DockStyle.Fill;
            tradeVolumeYesValue.Location = new Point(65, 110);
            tradeVolumeYesValue.Name = "tradeVolumeYesValue";
            tradeVolumeYesValue.Size = new Size(25, 22);
            tradeVolumeYesValue.Font = new Font("Segoe UI", 7F);
            tradeVolumeYesValue.TabIndex = 13;
            tradeVolumeYesValue.Text = "--";
            // 
            // tradeVolumeNoValue
            // 
            tradeVolumeNoValue.AutoSize = true;
            tradeVolumeNoValue.Dock = DockStyle.Fill;
            tradeVolumeNoValue.Location = new Point(96, 110);
            tradeVolumeNoValue.Name = "tradeVolumeNoValue";
            tradeVolumeNoValue.Size = new Size(25, 22);
            tradeVolumeNoValue.Font = new Font("Segoe UI", 7F);
            tradeVolumeNoValue.TabIndex = 14;
            tradeVolumeNoValue.Text = "--";
            // 
            // avgTradeSizeLabel
            // 
            avgTradeSizeLabel.AutoSize = true;
            avgTradeSizeLabel.Dock = DockStyle.Fill;
            avgTradeSizeLabel.Location = new Point(3, 135);
            avgTradeSizeLabel.Name = "avgTradeSizeLabel";
            avgTradeSizeLabel.Font = new Font("Segoe UI", 7F);
            avgTradeSizeLabel.Size = new Size(56, 16);
            avgTradeSizeLabel.TabIndex = 15;
            avgTradeSizeLabel.Text = "Avg Trade Size:";
            // 
            // avgTradeSizeYesValue
            // 
            avgTradeSizeYesValue.AutoSize = true;
            avgTradeSizeYesValue.Dock = DockStyle.Fill;
            avgTradeSizeYesValue.Location = new Point(65, 132);
            avgTradeSizeYesValue.Name = "avgTradeSizeYesValue";
            avgTradeSizeYesValue.Font = new Font("Segoe UI", 7F);
            avgTradeSizeYesValue.Size = new Size(25, 22);
            avgTradeSizeYesValue.TabIndex = 16;
            avgTradeSizeYesValue.Text = "--";
            // 
            // avgTradeSizeNoValue
            // 
            avgTradeSizeNoValue.AutoSize = true;
            avgTradeSizeNoValue.Dock = DockStyle.Fill;
            avgTradeSizeNoValue.Location = new Point(96, 132);
            avgTradeSizeNoValue.Name = "avgTradeSizeNoValue";
            avgTradeSizeNoValue.Font = new Font("Segoe UI", 7F);
            avgTradeSizeNoValue.Size = new Size(25, 22);
            avgTradeSizeNoValue.TabIndex = 17;
            avgTradeSizeNoValue.Text = "--";
            // 
            // slopeLabel
            // 
            slopeLabel.AutoSize = true;
            slopeLabel.Dock = DockStyle.Fill;
            slopeLabel.Location = new Point(3, 157);
            slopeLabel.Name = "slopeLabel";
            slopeLabel.Font = new Font("Segoe UI", 7F);
            slopeLabel.Size = new Size(56, 18);
            slopeLabel.TabIndex = 18;
            slopeLabel.Text = "Slope:";
            // 
            // slopeYesValue
            // 
            slopeYesValue.AutoSize = true;
            slopeYesValue.Dock = DockStyle.Fill;
            slopeYesValue.Location = new Point(65, 154);
            slopeYesValue.Name = "slopeYesValue";
            slopeYesValue.Font = new Font("Segoe UI", 7F);
            slopeYesValue.Size = new Size(25, 24);
            slopeYesValue.TabIndex = 19;
            slopeYesValue.Text = "--";
            // 
            // slopeNoValue
            // 
            slopeNoValue.AutoSize = true;
            slopeNoValue.Dock = DockStyle.Fill;
            slopeNoValue.Location = new Point(96, 154);
            slopeNoValue.Name = "slopeNoValue";
            slopeNoValue.Font = new Font("Segoe UI", 7F);
            slopeNoValue.Size = new Size(25, 24);
            slopeNoValue.TabIndex = 20;
            slopeNoValue.Text = "--";
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
            contextGrid.Location = new Point(3, 279);
            contextGrid.Name = "contextGrid";
            contextGrid.RowCount = 6;
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.67F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.67F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.67F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.67F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.67F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.67F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            contextGrid.Size = new Size(124, 180);
            contextGrid.TabIndex = 2;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Dock = DockStyle.Fill;
            label2.Location = new Point(3, 26);
            label2.Name = "label2";
            label2.Size = new Size(56, 26);
            label2.TabIndex = 16;
            // 
            // contextHeader
            // 
            contextHeader.AutoSize = true;
            contextGrid.SetColumnSpan(contextHeader, 3);
            contextHeader.Dock = DockStyle.Fill;
            contextHeader.Location = new Point(3, 0);
            contextHeader.Name = "contextHeader";
            contextHeader.Font = new Font("Segoe UI", 7F);
            contextHeader.Size = new Size(118, 26);
            contextHeader.TabIndex = 0;
            contextHeader.Text = "Context";
            // 
            // contextHeaderYes
            // 
            contextHeaderYes.AutoSize = true;
            contextHeaderYes.Dock = DockStyle.Fill;
            contextHeaderYes.Location = new Point(65, 26);
            contextHeaderYes.Name = "contextHeaderYes";
            contextHeaderYes.Font = new Font("Segoe UI", 7F);
            contextHeaderYes.Size = new Size(25, 26);
            contextHeaderYes.TabIndex = 1;
            contextHeaderYes.Text = "Yes";
            // 
            // contextHeaderNo
            // 
            contextHeaderNo.AutoSize = true;
            contextHeaderNo.Dock = DockStyle.Fill;
            contextHeaderNo.Location = new Point(96, 26);
            contextHeaderNo.Name = "contextHeaderNo";
            contextHeaderNo.Font = new Font("Segoe UI", 7F);
            contextHeaderNo.Size = new Size(25, 26);
            contextHeaderNo.TabIndex = 2;
            contextHeaderNo.Text = "No";
            // 
            // spreadLabel
            // 
            spreadLabel.AutoSize = true;
            spreadLabel.Dock = DockStyle.Fill;
            spreadLabel.Location = new Point(3, 52);
            spreadLabel.Name = "spreadLabel";
            spreadLabel.Font = new Font("Segoe UI", 7F);
            spreadLabel.Size = new Size(56, 26);
            spreadLabel.TabIndex = 3;
            spreadLabel.Text = "Spread:";
            // 
            // spreadValue
            // 
            spreadValue.AutoSize = true;
            contextGrid.SetColumnSpan(spreadValue, 2);
            spreadValue.Dock = DockStyle.Fill;
            spreadValue.Location = new Point(65, 52);
            spreadValue.Font = new Font("Segoe UI", 7F);
            spreadValue.Name = "spreadValue";
            spreadValue.Size = new Size(56, 26);
            spreadValue.TabIndex = 4;
            spreadValue.Text = "--";
            // 
            // imbalLabel
            // 
            imbalLabel.AutoSize = true;
            imbalLabel.Dock = DockStyle.Fill;
            imbalLabel.Location = new Point(3, 81);
            imbalLabel.Name = "imbalLabel";
            imbalLabel.Font = new Font("Segoe UI", 7F);
            imbalLabel.Size = new Size(56, 20);
            imbalLabel.TabIndex = 5;
            imbalLabel.Text = "Imbal:";
            // 
            // imbalValue
            // 
            imbalValue.AutoSize = true;
            contextGrid.SetColumnSpan(imbalValue, 2);
            imbalValue.Dock = DockStyle.Fill;
            imbalValue.Location = new Point(65, 78);
            imbalValue.Font = new Font("Segoe UI", 7F);
            imbalValue.Name = "imbalValue";
            imbalValue.Size = new Size(56, 26);
            imbalValue.TabIndex = 6;
            imbalValue.Text = "--";
            // 
            // depthTop4Label
            // 
            depthTop4Label.AutoSize = true;
            depthTop4Label.Dock = DockStyle.Fill;
            depthTop4Label.Location = new Point(3, 107);
            depthTop4Label.Name = "depthTop4Label";
            depthTop4Label.Font = new Font("Segoe UI", 7F);
            depthTop4Label.Size = new Size(56, 20);
            depthTop4Label.TabIndex = 7;
            depthTop4Label.Text = "Depth Top 4:";
            // 
            // depthTop4YesValue
            // 
            depthTop4YesValue.AutoSize = true;
            depthTop4YesValue.Dock = DockStyle.Fill;
            depthTop4YesValue.Location = new Point(65, 104);
            depthTop4YesValue.Name = "depthTop4YesValue";
            depthTop4YesValue.Font = new Font("Segoe UI", 7F);
            depthTop4YesValue.Size = new Size(25, 26);
            depthTop4YesValue.TabIndex = 8;
            depthTop4YesValue.Text = "--";
            // 
            // depthTop4NoValue
            // 
            depthTop4NoValue.AutoSize = true;
            depthTop4NoValue.Dock = DockStyle.Fill;
            depthTop4NoValue.Location = new Point(96, 104);
            depthTop4NoValue.Font = new Font("Segoe UI", 7F);
            depthTop4NoValue.Name = "depthTop4NoValue";
            depthTop4NoValue.Size = new Size(25, 26);
            depthTop4NoValue.TabIndex = 9;
            depthTop4NoValue.Text = "--";
            // 
            // centerMassLabel
            // 
            centerMassLabel.AutoSize = true;
            centerMassLabel.Dock = DockStyle.Fill;
            centerMassLabel.Location = new Point(3, 133);
            centerMassLabel.Font = new Font("Segoe UI", 7F);
            centerMassLabel.Name = "centerMassLabel";
            centerMassLabel.Size = new Size(56, 20);
            centerMassLabel.TabIndex = 10;
            centerMassLabel.Text = "Center Mass:";
            // 
            // centerMassYesValue
            // 
            centerMassYesValue.AutoSize = true;
            centerMassYesValue.Dock = DockStyle.Fill;
            centerMassYesValue.Location = new Point(65, 130);
            centerMassYesValue.Font = new Font("Segoe UI", 7F);
            centerMassYesValue.Name = "centerMassYesValue";
            centerMassYesValue.Size = new Size(25, 26);
            centerMassYesValue.TabIndex = 11;
            centerMassYesValue.Text = "--";
            // 
            // centerMassNoValue
            // 
            centerMassNoValue.AutoSize = true;
            centerMassNoValue.Dock = DockStyle.Fill;
            centerMassNoValue.Location = new Point(96, 130);
            centerMassNoValue.Font = new Font("Segoe UI", 7F);
            centerMassNoValue.Name = "centerMassNoValue";
            centerMassNoValue.Size = new Size(25, 26);
            centerMassNoValue.TabIndex = 12;
            centerMassNoValue.Text = "--";
            // 
            // totalContractsLabel
            // 
            totalContractsLabel.AutoSize = true;
            totalContractsLabel.Dock = DockStyle.Fill;
            totalContractsLabel.Location = new Point(3, 159);
            totalContractsLabel.Name = "totalContractsLabel";
            totalContractsLabel.Font = new Font("Segoe UI", 7F);
            totalContractsLabel.Size = new Size(56, 18);
            totalContractsLabel.TabIndex = 13;
            totalContractsLabel.Text = "Total Contracts:";
            // 
            // totalContractsYesValue
            // 
            totalContractsYesValue.AutoSize = true;
            totalContractsYesValue.Dock = DockStyle.Fill;
            totalContractsYesValue.Location = new Point(65, 156);
            totalContractsYesValue.Font = new Font("Segoe UI", 7F);
            totalContractsYesValue.Name = "totalContractsYesValue";
            totalContractsYesValue.Size = new Size(25, 24);
            totalContractsYesValue.TabIndex = 14;
            totalContractsYesValue.Text = "--";
            // 
            // totalContractsNoValue
            // 
            totalContractsNoValue.AutoSize = true;
            totalContractsNoValue.Dock = DockStyle.Fill;
            totalContractsNoValue.Location = new Point(96, 156);
            totalContractsNoValue.Font = new Font("Segoe UI", 7F);
            totalContractsNoValue.Name = "totalContractsNoValue";
            totalContractsNoValue.Size = new Size(25, 24);
            totalContractsNoValue.TabIndex = 15;
            totalContractsNoValue.Text = "--";
            // 
            // positionsContainer
            // 
            positionsContainer.BorderStyle = BorderStyle.FixedSingle;
            positionsContainer.Controls.Add(positionsLayout);
            positionsContainer.Dock = DockStyle.Fill;
            positionsContainer.Location = new Point(4, 491);
            positionsContainer.Margin = new Padding(4, 3, 4, 3);
            positionsContainer.Font = new Font("Segoe UI", 7F);
            positionsContainer.Name = "positionsContainer";
            positionsContainer.Padding = new Padding(6);
            positionsContainer.Size = new Size(593, 157);
            positionsContainer.TabIndex = 2;
            // 
            // positionsLayout
            // 
            positionsLayout.AutoSize = true;
            positionsLayout.ColumnCount = 1;
            positionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            positionsLayout.Controls.Add(positionsGrid, 0, 0);
            positionsLayout.Dock = DockStyle.Fill;
            positionsLayout.Location = new Point(6, 6);
            positionsLayout.Margin = new Padding(0);
            positionsLayout.Name = "positionsLayout";
            positionsLayout.RowCount = 1;
            positionsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            positionsLayout.Size = new Size(579, 143);
            positionsLayout.TabIndex = 0;
            // 
            // positionsGrid
            // 
            positionsGrid.Anchor = AnchorStyles.Right;
            positionsGrid.AutoSize = true;
            positionsGrid.ColumnCount = 2;
            positionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            positionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            positionsGrid.Controls.Add(positionSizeLabel, 0, 0);
            positionsGrid.Controls.Add(positionSizeValue, 1, 0);
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
            positionsGrid.Controls.Add(simulatedPositionLabel, 0, 7);
            positionsGrid.Controls.Add(simulatedPositionValue, 1, 7);
            positionsGrid.Location = new Point(353, 3);
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
            positionsGrid.Size = new Size(223, 137);
            positionsGrid.TabIndex = 1;
            // 
            // positionSizeLabel
            // 
            positionSizeLabel.AutoSize = true;
            positionSizeLabel.Dock = DockStyle.Fill;
            positionSizeLabel.Location = new Point(3, 3);
            positionSizeLabel.Name = "positionSizeLabel";
            positionSizeLabel.Font = new Font("Segoe UI", 7F);
            positionSizeLabel.Size = new Size(127, 11);
            positionSizeLabel.TabIndex = 0;
            positionSizeLabel.Text = "Position Size:";
            // 
            // positionSizeValue
            // 
            positionSizeValue.AutoSize = true;
            positionSizeValue.Dock = DockStyle.Fill;
            positionSizeValue.Location = new Point(136, 0);
            positionSizeValue.Name = "positionSizeValue";
            positionSizeValue.Font = new Font("Segoe UI", 7F);
            positionSizeValue.Size = new Size(84, 17);
            positionSizeValue.TabIndex = 1;
            positionSizeValue.Text = "--";
            // 
            // lastTradeLabel
            // 
            lastTradeLabel.AutoSize = true;
            lastTradeLabel.Dock = DockStyle.Fill;
            lastTradeLabel.Location = new Point(3, 17);
            lastTradeLabel.Name = "lastTradeLabel";
            lastTradeLabel.Font = new Font("Segoe UI", 7F);
            lastTradeLabel.Size = new Size(127, 17);
            lastTradeLabel.TabIndex = 2;
            lastTradeLabel.Text = "Last Trade:";
            // 
            // lastTradeValue
            // 
            lastTradeValue.AutoSize = true;
            lastTradeValue.Dock = DockStyle.Fill;
            lastTradeValue.Location = new Point(136, 17);
            lastTradeValue.Name = "lastTradeValue";
            lastTradeValue.Font = new Font("Segoe UI", 7F);
            lastTradeValue.Size = new Size(84, 17);
            lastTradeValue.TabIndex = 3;
            lastTradeValue.Text = "--";
            // 
            // positionRoiLabel
            // 
            positionRoiLabel.AutoSize = true;
            positionRoiLabel.Dock = DockStyle.Fill;
            positionRoiLabel.Location = new Point(3, 37);
            positionRoiLabel.Name = "positionRoiLabel";
            positionRoiLabel.Font = new Font("Segoe UI", 7F);
            positionRoiLabel.Size = new Size(127, 11);
            positionRoiLabel.TabIndex = 4;
            positionRoiLabel.Text = "Position ROI:";
            // 
            // positionRoiValue
            // 
            positionRoiValue.AutoSize = true;
            positionRoiValue.Dock = DockStyle.Fill;
            positionRoiValue.Location = new Point(136, 34);
            positionRoiValue.Name = "positionRoiValue";
            positionRoiValue.Font = new Font("Segoe UI", 7F);
            positionRoiValue.Size = new Size(84, 17);
            positionRoiValue.TabIndex = 5;
            positionRoiValue.Text = "--";
            // 
            // buyinPriceLabel
            // 
            buyinPriceLabel.AutoSize = true;
            buyinPriceLabel.Dock = DockStyle.Fill;
            buyinPriceLabel.Location = new Point(3, 51);
            buyinPriceLabel.Name = "buyinPriceLabel";
            buyinPriceLabel.Font = new Font("Segoe UI", 7F);
            buyinPriceLabel.Size = new Size(127, 17);
            buyinPriceLabel.TabIndex = 6;
            buyinPriceLabel.Text = "Buyin Price:";
            // 
            // buyinPriceValue
            // 
            buyinPriceValue.AutoSize = true;
            buyinPriceValue.Dock = DockStyle.Fill;
            buyinPriceValue.Location = new Point(136, 51);
            buyinPriceValue.Name = "buyinPriceValue";
            buyinPriceValue.Font = new Font("Segoe UI", 7F);
            buyinPriceValue.Size = new Size(84, 17);
            buyinPriceValue.TabIndex = 7;
            buyinPriceValue.Text = "--";
            // 
            // positionUpsideLabel
            // 
            positionUpsideLabel.AutoSize = true;
            positionUpsideLabel.Dock = DockStyle.Fill;
            positionUpsideLabel.Location = new Point(3, 68);
            positionUpsideLabel.Name = "positionUpsideLabel";
            positionUpsideLabel.Size = new Size(127, 17);
            positionUpsideLabel.Font = new Font("Segoe UI", 7F);
            positionUpsideLabel.TabIndex = 8;
            positionUpsideLabel.Text = "Position Upside:";
            // 
            // positionUpsideValue
            // 
            positionUpsideValue.AutoSize = true;
            positionUpsideValue.Dock = DockStyle.Fill;
            positionUpsideValue.Location = new Point(136, 68);
            positionUpsideValue.Name = "positionUpsideValue";
            positionUpsideValue.Font = new Font("Segoe UI", 7F);
            positionUpsideValue.Size = new Size(84, 17);
            positionUpsideValue.TabIndex = 9;
            positionUpsideValue.Text = "--";
            // 
            // positionDownsideLabel
            // 
            positionDownsideLabel.AutoSize = true;
            positionDownsideLabel.Dock = DockStyle.Fill;
            positionDownsideLabel.Location = new Point(3, 85);
            positionDownsideLabel.Font = new Font("Segoe UI", 7F);
            positionDownsideLabel.Name = "positionDownsideLabel";
            positionDownsideLabel.Size = new Size(127, 17);
            positionDownsideLabel.TabIndex = 10;
            positionDownsideLabel.Text = "Position Downside:";
            // 
            // positionDownsideValue
            // 
            positionDownsideValue.AutoSize = true;
            positionDownsideValue.Dock = DockStyle.Fill;
            positionDownsideValue.Location = new Point(136, 85);
            positionDownsideValue.Name = "positionDownsideValue";
            positionDownsideValue.Font = new Font("Segoe UI", 7F);
            positionDownsideValue.Size = new Size(84, 17);
            positionDownsideValue.TabIndex = 11;
            positionDownsideValue.Text = "--";
            // 
            // restingOrdersLabel
            // 
            restingOrdersLabel.AutoSize = true;
            restingOrdersLabel.Dock = DockStyle.Fill;
            restingOrdersLabel.Location = new Point(3, 105);
            restingOrdersLabel.Font = new Font("Segoe UI", 7F);
            restingOrdersLabel.Name = "restingOrdersLabel";
            restingOrdersLabel.Size = new Size(127, 11);
            restingOrdersLabel.TabIndex = 12;
            restingOrdersLabel.Text = "Resting Orders:";
            // 
            // restingOrdersValue
            // 
            restingOrdersValue.AutoSize = true;
            restingOrdersValue.Dock = DockStyle.Fill;
            restingOrdersValue.Location = new Point(136, 102);
            restingOrdersValue.Font = new Font("Segoe UI", 7F);
            restingOrdersValue.Name = "restingOrdersValue";
            restingOrdersValue.Size = new Size(84, 17);
            restingOrdersValue.TabIndex = 13;
            restingOrdersValue.Text = "--";
            // 
            // simulatedPositionLabel
            // 
            simulatedPositionLabel.AutoSize = true;
            simulatedPositionLabel.Dock = DockStyle.Fill;
            simulatedPositionLabel.Location = new Point(3, 122);
            simulatedPositionLabel.Name = "simulatedPositionLabel";
            simulatedPositionLabel.Font = new Font("Segoe UI", 7F);
            simulatedPositionLabel.Size = new Size(127, 12);
            simulatedPositionLabel.TabIndex = 14;
            simulatedPositionLabel.Text = "Simulated Position:";
            // 
            // simulatedPositionValue
            // 
            simulatedPositionValue.AutoSize = true;
            simulatedPositionValue.Dock = DockStyle.Fill;
            simulatedPositionValue.Location = new Point(136, 119);
            simulatedPositionValue.Font = new Font("Segoe UI", 7F);
            simulatedPositionValue.Name = "simulatedPositionValue";
            simulatedPositionValue.Size = new Size(84, 18);
            simulatedPositionValue.TabIndex = 15;
            simulatedPositionValue.Text = "--";
            // 
            // orderbookContainer
            // 
            orderbookContainer.BorderStyle = BorderStyle.FixedSingle;
            orderbookContainer.Controls.Add(orderbookGrid);
            orderbookContainer.Dock = DockStyle.Fill;
            orderbookContainer.Font = new Font("Segoe UI", 7F);
            orderbookContainer.Location = new Point(605, 491);
            orderbookContainer.Margin = new Padding(4, 3, 4, 3);
            orderbookContainer.Name = "orderbookContainer";
            orderbookContainer.Padding = new Padding(6);
            orderbookContainer.Size = new Size(316, 157);
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
            orderbookGrid.Size = new Size(302, 143);
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
            backButton.Location = new Point(4, 660);
            backButton.Margin = new Padding(4, 3, 4, 3);
            backButton.Font = new Font("Segoe UI", 7F);
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
            chartHeader.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            chartHeader.Location = new Point(0, 0);
            chartHeader.Name = "chartHeader";
            chartHeader.Font = new Font("Segoe UI", 7F);
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
        private TableLayoutPanel positionsGrid;
        private CheckBox positionSizeLabel;
        private Label positionSizeValue;
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
        private CheckBox simulatedPositionLabel;
        private Label simulatedPositionValue;
        private Panel orderbookContainer;
        private DataGridView orderbookGrid;
        private DataGridViewTextBoxColumn priceCol;
        private DataGridViewTextBoxColumn sizeCol;
        private DataGridViewTextBoxColumn valueCol;
        private Button backButton;
        private Label label1;
        private Label label2;
        private TableLayoutPanel positionsLayout;
    }
}