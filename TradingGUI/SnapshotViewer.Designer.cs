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
            allTimeHighAsk = new Panel();
            allTimeHighAskPrice = new Label();
            allTimeHighAskTime = new Label();
            allTimeHighBid = new Panel();
            allTimeHighBidPrice = new Label();
            allTimeHighBidTime = new Label();
            recentHighLabel = new Label();
            recentHighAsk = new Panel();
            recentHighAskPrice = new Label();
            recentHighAskTime = new Label();
            recentHighBid = new Panel();
            recentHighBidPrice = new Label();
            recentHighBidTime = new Label();
            currentPriceLabel = new Label();
            currentPriceAsk = new Label();
            currentPriceBid = new Label();
            recentLowLabel = new Label();
            recentLowAsk = new Panel();
            recentLowAskPrice = new Label();
            recentLowAskTime = new Label();
            recentLowBid = new Panel();
            recentLowBidPrice = new Label();
            recentLowBidTime = new Label();
            allTimeLowLabel = new Label();
            allTimeLowAsk = new Panel();
            allTimeLowAskPrice = new Label();
            allTimeLowAskTime = new Label();
            allTimeLowBid = new Panel();
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
            positionTextBox = new TextBox();
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
            priceChart.Location = new Point(3, 49);
            priceChart.Name = "priceChart";
            priceChart.Size = new Size(573, 416);
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
            leftColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            leftColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            leftColumn.Size = new Size(160, 462);
            leftColumn.TabIndex = 0;
            // 
            // pricesGrid
            // 
            pricesGrid.AutoSize = true;
            pricesGrid.ColumnCount = 3;
            pricesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pricesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pricesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
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
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.67F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.67F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.67F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.67F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.67F));
            pricesGrid.Size = new Size(154, 178);
            pricesGrid.TabIndex = 0;
            // 
            // pricesHeaderEmpty
            // 
            pricesHeaderEmpty.AutoSize = true;
            pricesHeaderEmpty.Dock = DockStyle.Fill;
            pricesHeaderEmpty.Location = new Point(3, 0);
            pricesHeaderEmpty.Name = "pricesHeaderEmpty";
            pricesHeaderEmpty.Size = new Size(71, 29);
            pricesHeaderEmpty.TabIndex = 0;
            // 
            // pricesHeaderNo
            // 
            pricesHeaderNo.AutoSize = true;
            pricesHeaderNo.Dock = DockStyle.Fill;
            pricesHeaderNo.Location = new Point(80, 0);
            pricesHeaderNo.Name = "pricesHeaderNo";
            pricesHeaderNo.Size = new Size(32, 29);
            pricesHeaderNo.TabIndex = 1;
            pricesHeaderNo.Text = "No";
            // 
            // pricesHeaderYes
            // 
            pricesHeaderYes.AutoSize = true;
            pricesHeaderYes.Dock = DockStyle.Fill;
            pricesHeaderYes.Location = new Point(118, 0);
            pricesHeaderYes.Name = "pricesHeaderYes";
            pricesHeaderYes.Size = new Size(33, 29);
            pricesHeaderYes.TabIndex = 2;
            pricesHeaderYes.Text = "Yes";
            // 
            // allTimeHighLabel
            // 
            allTimeHighLabel.AutoSize = true;
            allTimeHighLabel.Dock = DockStyle.Fill;
            allTimeHighLabel.Location = new Point(3, 29);
            allTimeHighLabel.Name = "allTimeHighLabel";
            allTimeHighLabel.Size = new Size(71, 29);
            allTimeHighLabel.TabIndex = 3;
            allTimeHighLabel.Text = "All Time High";
            // 
            // allTimeHighAsk
            // 
            allTimeHighAsk.AutoSize = true;
            allTimeHighAsk.Controls.Add(allTimeHighAskPrice);
            allTimeHighAsk.Controls.Add(allTimeHighAskTime);
            allTimeHighAsk.Dock = DockStyle.Fill;
            allTimeHighAsk.Location = new Point(80, 32);
            allTimeHighAsk.Name = "allTimeHighAsk";
            allTimeHighAsk.Size = new Size(32, 23);
            allTimeHighAsk.TabIndex = 4;
            // 
            // allTimeHighAskPrice
            // 
            allTimeHighAskPrice.AutoSize = true;
            allTimeHighAskPrice.Dock = DockStyle.Left;
            allTimeHighAskPrice.Location = new Point(0, 0);
            allTimeHighAskPrice.Name = "allTimeHighAskPrice";
            allTimeHighAskPrice.Size = new Size(17, 15);
            allTimeHighAskPrice.TabIndex = 0;
            allTimeHighAskPrice.Text = "--";
            // 
            // allTimeHighAskTime
            // 
            allTimeHighAskTime.AutoSize = true;
            allTimeHighAskTime.Dock = DockStyle.Right;
            allTimeHighAskTime.Location = new Point(15, 0);
            allTimeHighAskTime.Name = "allTimeHighAskTime";
            allTimeHighAskTime.Size = new Size(17, 15);
            allTimeHighAskTime.TabIndex = 1;
            allTimeHighAskTime.Text = "--";
            // 
            // allTimeHighBid
            // 
            allTimeHighBid.AutoSize = true;
            allTimeHighBid.Controls.Add(allTimeHighBidPrice);
            allTimeHighBid.Controls.Add(allTimeHighBidTime);
            allTimeHighBid.Dock = DockStyle.Fill;
            allTimeHighBid.Location = new Point(118, 32);
            allTimeHighBid.Name = "allTimeHighBid";
            allTimeHighBid.Size = new Size(33, 23);
            allTimeHighBid.TabIndex = 5;
            // 
            // allTimeHighBidPrice
            // 
            allTimeHighBidPrice.AutoSize = true;
            allTimeHighBidPrice.Dock = DockStyle.Left;
            allTimeHighBidPrice.Location = new Point(0, 0);
            allTimeHighBidPrice.Name = "allTimeHighBidPrice";
            allTimeHighBidPrice.Size = new Size(17, 15);
            allTimeHighBidPrice.TabIndex = 0;
            allTimeHighBidPrice.Text = "--";
            // 
            // allTimeHighBidTime
            // 
            allTimeHighBidTime.AutoSize = true;
            allTimeHighBidTime.Dock = DockStyle.Right;
            allTimeHighBidTime.Location = new Point(16, 0);
            allTimeHighBidTime.Name = "allTimeHighBidTime";
            allTimeHighBidTime.Size = new Size(17, 15);
            allTimeHighBidTime.TabIndex = 1;
            allTimeHighBidTime.Text = "--";
            // 
            // recentHighLabel
            // 
            recentHighLabel.AutoSize = true;
            recentHighLabel.Dock = DockStyle.Fill;
            recentHighLabel.Location = new Point(3, 58);
            recentHighLabel.Name = "recentHighLabel";
            recentHighLabel.Size = new Size(71, 29);
            recentHighLabel.TabIndex = 6;
            recentHighLabel.Text = "Recent High";
            // 
            // recentHighAsk
            // 
            recentHighAsk.AutoSize = true;
            recentHighAsk.Controls.Add(recentHighAskPrice);
            recentHighAsk.Controls.Add(recentHighAskTime);
            recentHighAsk.Dock = DockStyle.Fill;
            recentHighAsk.Location = new Point(80, 61);
            recentHighAsk.Name = "recentHighAsk";
            recentHighAsk.Size = new Size(32, 23);
            recentHighAsk.TabIndex = 7;
            // 
            // recentHighAskPrice
            // 
            recentHighAskPrice.AutoSize = true;
            recentHighAskPrice.Dock = DockStyle.Left;
            recentHighAskPrice.Location = new Point(0, 0);
            recentHighAskPrice.Name = "recentHighAskPrice";
            recentHighAskPrice.Size = new Size(17, 15);
            recentHighAskPrice.TabIndex = 0;
            recentHighAskPrice.Text = "--";
            // 
            // recentHighAskTime
            // 
            recentHighAskTime.AutoSize = true;
            recentHighAskTime.Dock = DockStyle.Right;
            recentHighAskTime.Location = new Point(15, 0);
            recentHighAskTime.Name = "recentHighAskTime";
            recentHighAskTime.Size = new Size(17, 15);
            recentHighAskTime.TabIndex = 1;
            recentHighAskTime.Text = "--";
            // 
            // recentHighBid
            // 
            recentHighBid.AutoSize = true;
            recentHighBid.Controls.Add(recentHighBidPrice);
            recentHighBid.Controls.Add(recentHighBidTime);
            recentHighBid.Dock = DockStyle.Fill;
            recentHighBid.Location = new Point(118, 61);
            recentHighBid.Name = "recentHighBid";
            recentHighBid.Size = new Size(33, 23);
            recentHighBid.TabIndex = 8;
            // 
            // recentHighBidPrice
            // 
            recentHighBidPrice.AutoSize = true;
            recentHighBidPrice.Dock = DockStyle.Left;
            recentHighBidPrice.Location = new Point(0, 0);
            recentHighBidPrice.Name = "recentHighBidPrice";
            recentHighBidPrice.Size = new Size(17, 15);
            recentHighBidPrice.TabIndex = 0;
            recentHighBidPrice.Text = "--";
            // 
            // recentHighBidTime
            // 
            recentHighBidTime.AutoSize = true;
            recentHighBidTime.Dock = DockStyle.Right;
            recentHighBidTime.Location = new Point(16, 0);
            recentHighBidTime.Name = "recentHighBidTime";
            recentHighBidTime.Size = new Size(17, 15);
            recentHighBidTime.TabIndex = 1;
            recentHighBidTime.Text = "--";
            // 
            // currentPriceLabel
            // 
            currentPriceLabel.AutoSize = true;
            currentPriceLabel.Dock = DockStyle.Fill;
            currentPriceLabel.Location = new Point(3, 87);
            currentPriceLabel.Name = "currentPriceLabel";
            currentPriceLabel.Size = new Size(71, 29);
            currentPriceLabel.TabIndex = 9;
            currentPriceLabel.Text = "Current";
            // 
            // currentPriceAsk
            // 
            currentPriceAsk.AutoSize = true;
            currentPriceAsk.Dock = DockStyle.Fill;
            currentPriceAsk.Location = new Point(80, 87);
            currentPriceAsk.Name = "currentPriceAsk";
            currentPriceAsk.Size = new Size(32, 29);
            currentPriceAsk.TabIndex = 10;
            currentPriceAsk.Text = "--";
            // 
            // currentPriceBid
            // 
            currentPriceBid.AutoSize = true;
            currentPriceBid.Dock = DockStyle.Fill;
            currentPriceBid.Location = new Point(118, 87);
            currentPriceBid.Name = "currentPriceBid";
            currentPriceBid.Size = new Size(33, 29);
            currentPriceBid.TabIndex = 11;
            currentPriceBid.Text = "--";
            // 
            // recentLowLabel
            // 
            recentLowLabel.AutoSize = true;
            recentLowLabel.Dock = DockStyle.Fill;
            recentLowLabel.Location = new Point(3, 116);
            recentLowLabel.Name = "recentLowLabel";
            recentLowLabel.Size = new Size(71, 29);
            recentLowLabel.TabIndex = 12;
            recentLowLabel.Text = "Recent Low";
            // 
            // recentLowAsk
            // 
            recentLowAsk.AutoSize = true;
            recentLowAsk.Controls.Add(recentLowAskPrice);
            recentLowAsk.Controls.Add(recentLowAskTime);
            recentLowAsk.Dock = DockStyle.Fill;
            recentLowAsk.Location = new Point(80, 119);
            recentLowAsk.Name = "recentLowAsk";
            recentLowAsk.Size = new Size(32, 23);
            recentLowAsk.TabIndex = 13;
            // 
            // recentLowAskPrice
            // 
            recentLowAskPrice.AutoSize = true;
            recentLowAskPrice.Dock = DockStyle.Left;
            recentLowAskPrice.Location = new Point(0, 0);
            recentLowAskPrice.Name = "recentLowAskPrice";
            recentLowAskPrice.Size = new Size(17, 15);
            recentLowAskPrice.TabIndex = 0;
            recentLowAskPrice.Text = "--";
            // 
            // recentLowAskTime
            // 
            recentLowAskTime.AutoSize = true;
            recentLowAskTime.Dock = DockStyle.Right;
            recentLowAskTime.Location = new Point(15, 0);
            recentLowAskTime.Name = "recentLowAskTime";
            recentLowAskTime.Size = new Size(17, 15);
            recentLowAskTime.TabIndex = 1;
            recentLowAskTime.Text = "--";
            // 
            // recentLowBid
            // 
            recentLowBid.AutoSize = true;
            recentLowBid.Controls.Add(recentLowBidPrice);
            recentLowBid.Controls.Add(recentLowBidTime);
            recentLowBid.Dock = DockStyle.Fill;
            recentLowBid.Location = new Point(118, 119);
            recentLowBid.Name = "recentLowBid";
            recentLowBid.Size = new Size(33, 23);
            recentLowBid.TabIndex = 14;
            // 
            // recentLowBidPrice
            // 
            recentLowBidPrice.AutoSize = true;
            recentLowBidPrice.Dock = DockStyle.Left;
            recentLowBidPrice.Location = new Point(0, 0);
            recentLowBidPrice.Name = "recentLowBidPrice";
            recentLowBidPrice.Size = new Size(17, 15);
            recentLowBidPrice.TabIndex = 0;
            recentLowBidPrice.Text = "--";
            // 
            // recentLowBidTime
            // 
            recentLowBidTime.AutoSize = true;
            recentLowBidTime.Dock = DockStyle.Right;
            recentLowBidTime.Location = new Point(16, 0);
            recentLowBidTime.Name = "recentLowBidTime";
            recentLowBidTime.Size = new Size(17, 15);
            recentLowBidTime.TabIndex = 1;
            recentLowBidTime.Text = "--";
            // 
            // allTimeLowLabel
            // 
            allTimeLowLabel.AutoSize = true;
            allTimeLowLabel.Dock = DockStyle.Fill;
            allTimeLowLabel.Location = new Point(3, 145);
            allTimeLowLabel.Name = "allTimeLowLabel";
            allTimeLowLabel.Size = new Size(71, 33);
            allTimeLowLabel.TabIndex = 15;
            allTimeLowLabel.Text = "All Time Low";
            // 
            // allTimeLowAsk
            // 
            allTimeLowAsk.AutoSize = true;
            allTimeLowAsk.Controls.Add(allTimeLowAskPrice);
            allTimeLowAsk.Controls.Add(allTimeLowAskTime);
            allTimeLowAsk.Dock = DockStyle.Fill;
            allTimeLowAsk.Location = new Point(80, 148);
            allTimeLowAsk.Name = "allTimeLowAsk";
            allTimeLowAsk.Size = new Size(32, 27);
            allTimeLowAsk.TabIndex = 16;
            // 
            // allTimeLowAskPrice
            // 
            allTimeLowAskPrice.AutoSize = true;
            allTimeLowAskPrice.Dock = DockStyle.Left;
            allTimeLowAskPrice.Location = new Point(0, 0);
            allTimeLowAskPrice.Name = "allTimeLowAskPrice";
            allTimeLowAskPrice.Size = new Size(17, 15);
            allTimeLowAskPrice.TabIndex = 0;
            allTimeLowAskPrice.Text = "--";
            // 
            // allTimeLowAskTime
            // 
            allTimeLowAskTime.AutoSize = true;
            allTimeLowAskTime.Dock = DockStyle.Right;
            allTimeLowAskTime.Location = new Point(15, 0);
            allTimeLowAskTime.Name = "allTimeLowAskTime";
            allTimeLowAskTime.Size = new Size(17, 15);
            allTimeLowAskTime.TabIndex = 1;
            allTimeLowAskTime.Text = "--";
            // 
            // allTimeLowBid
            // 
            allTimeLowBid.AutoSize = true;
            allTimeLowBid.Controls.Add(allTimeLowBidPrice);
            allTimeLowBid.Controls.Add(allTimeLowBidTime);
            allTimeLowBid.Dock = DockStyle.Fill;
            allTimeLowBid.Location = new Point(118, 148);
            allTimeLowBid.Name = "allTimeLowBid";
            allTimeLowBid.Size = new Size(33, 27);
            allTimeLowBid.TabIndex = 17;
            // 
            // allTimeLowBidPrice
            // 
            allTimeLowBidPrice.AutoSize = true;
            allTimeLowBidPrice.Dock = DockStyle.Left;
            allTimeLowBidPrice.Location = new Point(0, 0);
            allTimeLowBidPrice.Name = "allTimeLowBidPrice";
            allTimeLowBidPrice.Size = new Size(17, 15);
            allTimeLowBidPrice.TabIndex = 0;
            allTimeLowBidPrice.Text = "--";
            // 
            // allTimeLowBidTime
            // 
            allTimeLowBidTime.AutoSize = true;
            allTimeLowBidTime.Dock = DockStyle.Right;
            allTimeLowBidTime.Location = new Point(16, 0);
            allTimeLowBidTime.Name = "allTimeLowBidTime";
            allTimeLowBidTime.Size = new Size(17, 15);
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
            tradingMetricsGrid.Location = new Point(3, 187);
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
            tradingMetricsGrid.Size = new Size(154, 272);
            tradingMetricsGrid.TabIndex = 1;
            // 
            // tradingMetricsHeader
            // 
            tradingMetricsHeader.AutoSize = true;
            tradingMetricsGrid.SetColumnSpan(tradingMetricsHeader, 2);
            tradingMetricsHeader.Dock = DockStyle.Fill;
            tradingMetricsHeader.Location = new Point(3, 0);
            tradingMetricsHeader.Name = "tradingMetricsHeader";
            tradingMetricsHeader.Size = new Size(148, 24);
            tradingMetricsHeader.TabIndex = 0;
            tradingMetricsHeader.Text = "Trading Metrics";
            // 
            // rsiLabel
            // 
            rsiLabel.AutoSize = true;
            rsiLabel.Dock = DockStyle.Fill;
            rsiLabel.Location = new Point(3, 27);
            rsiLabel.Name = "rsiLabel";
            rsiLabel.Size = new Size(86, 18);
            rsiLabel.TabIndex = 1;
            rsiLabel.Text = "RSI:";
            // 
            // rsiValue
            // 
            rsiValue.AutoSize = true;
            rsiValue.Dock = DockStyle.Fill;
            rsiValue.Location = new Point(95, 24);
            rsiValue.Name = "rsiValue";
            rsiValue.Size = new Size(56, 24);
            rsiValue.TabIndex = 2;
            rsiValue.Text = "--";
            // 
            // macdLabel
            // 
            macdLabel.AutoSize = true;
            macdLabel.Dock = DockStyle.Fill;
            macdLabel.Location = new Point(3, 51);
            macdLabel.Name = "macdLabel";
            macdLabel.Size = new Size(86, 18);
            macdLabel.TabIndex = 3;
            macdLabel.Text = "MACD:";
            // 
            // macdValue
            // 
            macdValue.AutoSize = true;
            macdValue.Dock = DockStyle.Fill;
            macdValue.Location = new Point(95, 48);
            macdValue.Name = "macdValue";
            macdValue.Size = new Size(56, 24);
            macdValue.TabIndex = 4;
            macdValue.Text = "--";
            // 
            // emaLabel
            // 
            emaLabel.AutoSize = true;
            emaLabel.Dock = DockStyle.Fill;
            emaLabel.Location = new Point(3, 75);
            emaLabel.Name = "emaLabel";
            emaLabel.Size = new Size(86, 18);
            emaLabel.TabIndex = 5;
            emaLabel.Text = "EMA:";
            // 
            // emaValue
            // 
            emaValue.AutoSize = true;
            emaValue.Dock = DockStyle.Fill;
            emaValue.Location = new Point(95, 72);
            emaValue.Name = "emaValue";
            emaValue.Size = new Size(56, 24);
            emaValue.TabIndex = 6;
            emaValue.Text = "--";
            // 
            // bollingerLabel
            // 
            bollingerLabel.AutoSize = true;
            bollingerLabel.Dock = DockStyle.Fill;
            bollingerLabel.Location = new Point(3, 99);
            bollingerLabel.Name = "bollingerLabel";
            bollingerLabel.Size = new Size(86, 18);
            bollingerLabel.TabIndex = 7;
            bollingerLabel.Text = "Bollinger:";
            // 
            // bollingerValue
            // 
            bollingerValue.AutoSize = true;
            bollingerValue.Dock = DockStyle.Fill;
            bollingerValue.Location = new Point(95, 96);
            bollingerValue.Name = "bollingerValue";
            bollingerValue.Size = new Size(56, 24);
            bollingerValue.TabIndex = 8;
            bollingerValue.Text = "--";
            // 
            // atrLabel
            // 
            atrLabel.AutoSize = true;
            atrLabel.Dock = DockStyle.Fill;
            atrLabel.Location = new Point(3, 123);
            atrLabel.Name = "atrLabel";
            atrLabel.Size = new Size(86, 18);
            atrLabel.TabIndex = 9;
            atrLabel.Text = "ATR:";
            // 
            // atrValue
            // 
            atrValue.AutoSize = true;
            atrValue.Dock = DockStyle.Fill;
            atrValue.Location = new Point(95, 120);
            atrValue.Name = "atrValue";
            atrValue.Size = new Size(56, 24);
            atrValue.TabIndex = 10;
            atrValue.Text = "--";
            // 
            // vwapLabel
            // 
            vwapLabel.AutoSize = true;
            vwapLabel.Dock = DockStyle.Fill;
            vwapLabel.Location = new Point(3, 147);
            vwapLabel.Name = "vwapLabel";
            vwapLabel.Size = new Size(86, 18);
            vwapLabel.TabIndex = 11;
            vwapLabel.Text = "VWAP:";
            // 
            // vwapValue
            // 
            vwapValue.AutoSize = true;
            vwapValue.Dock = DockStyle.Fill;
            vwapValue.Location = new Point(95, 144);
            vwapValue.Name = "vwapValue";
            vwapValue.Size = new Size(56, 24);
            vwapValue.TabIndex = 12;
            vwapValue.Text = "--";
            // 
            // stochasticLabel
            // 
            stochasticLabel.AutoSize = true;
            stochasticLabel.Dock = DockStyle.Fill;
            stochasticLabel.Location = new Point(3, 171);
            stochasticLabel.Name = "stochasticLabel";
            stochasticLabel.Size = new Size(86, 18);
            stochasticLabel.TabIndex = 13;
            stochasticLabel.Text = "Stochastic:";
            // 
            // stochasticValue
            // 
            stochasticValue.AutoSize = true;
            stochasticValue.Dock = DockStyle.Fill;
            stochasticValue.Location = new Point(95, 168);
            stochasticValue.Name = "stochasticValue";
            stochasticValue.Size = new Size(56, 24);
            stochasticValue.TabIndex = 14;
            stochasticValue.Text = "--";
            // 
            // obvLabel
            // 
            obvLabel.AutoSize = true;
            obvLabel.Dock = DockStyle.Fill;
            obvLabel.Location = new Point(3, 195);
            obvLabel.Name = "obvLabel";
            obvLabel.Size = new Size(86, 18);
            obvLabel.TabIndex = 15;
            obvLabel.Text = "OBV:";
            // 
            // obvValue
            // 
            obvValue.AutoSize = true;
            obvValue.Dock = DockStyle.Fill;
            obvValue.Location = new Point(95, 192);
            obvValue.Name = "obvValue";
            obvValue.Size = new Size(56, 24);
            obvValue.TabIndex = 16;
            obvValue.Text = "--";
            // 
            // psarLabel
            // 
            psarLabel.AutoSize = true;
            psarLabel.Dock = DockStyle.Fill;
            psarLabel.Location = new Point(3, 219);
            psarLabel.Name = "psarLabel";
            psarLabel.Size = new Size(86, 18);
            psarLabel.TabIndex = 17;
            psarLabel.Text = "PSAR:";
            // 
            // psarValue
            // 
            psarValue.AutoSize = true;
            psarValue.Dock = DockStyle.Fill;
            psarValue.Location = new Point(95, 216);
            psarValue.Name = "psarValue";
            psarValue.Size = new Size(56, 24);
            psarValue.TabIndex = 18;
            psarValue.Text = "--";
            // 
            // adxLabel
            // 
            adxLabel.AutoSize = true;
            adxLabel.Dock = DockStyle.Fill;
            adxLabel.Location = new Point(3, 243);
            adxLabel.Name = "adxLabel";
            adxLabel.Size = new Size(86, 26);
            adxLabel.TabIndex = 19;
            adxLabel.Text = "ADX:";
            // 
            // adxValue
            // 
            adxValue.AutoSize = true;
            adxValue.Dock = DockStyle.Fill;
            adxValue.Location = new Point(95, 240);
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
            CategoryLabel.Size = new Size(68, 28);
            CategoryLabel.TabIndex = 0;
            CategoryLabel.Text = "Category:";
            // 
            // categoryValue
            // 
            categoryValue.AutoSize = true;
            categoryValue.Dock = DockStyle.Fill;
            categoryValue.Location = new Point(77, 0);
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
            // flowHeader
            // 
            flowHeader.AutoSize = true;
            flowMomentumGrid.SetColumnSpan(flowHeader, 3);
            flowHeader.Dock = DockStyle.Fill;
            flowHeader.Location = new Point(3, 0);
            flowHeader.Name = "flowHeader";
            flowHeader.Size = new Size(118, 22);
            flowHeader.TabIndex = 0;
            flowHeader.Text = "Flow/Momentum";
            // 
            // flowHeaderYes
            // 
            flowHeaderYes.AutoSize = true;
            flowHeaderYes.Dock = DockStyle.Fill;
            flowHeaderYes.Location = new Point(3, 22);
            flowHeaderYes.Name = "flowHeaderYes";
            flowHeaderYes.Size = new Size(56, 22);
            flowHeaderYes.TabIndex = 1;
            flowHeaderYes.Text = "Yes";
            // 
            // flowHeaderNo
            // 
            flowHeaderNo.AutoSize = true;
            flowHeaderNo.Dock = DockStyle.Fill;
            flowHeaderNo.Location = new Point(65, 22);
            flowHeaderNo.Name = "flowHeaderNo";
            flowHeaderNo.Size = new Size(25, 22);
            flowHeaderNo.TabIndex = 2;
            flowHeaderNo.Text = "No";
            // 
            // topVelocityLabel
            // 
            topVelocityLabel.AutoSize = true;
            topVelocityLabel.Dock = DockStyle.Fill;
            topVelocityLabel.Location = new Point(96, 25);
            topVelocityLabel.Name = "topVelocityLabel";
            topVelocityLabel.Size = new Size(25, 16);
            topVelocityLabel.TabIndex = 3;
            topVelocityLabel.Text = "Top Velocity:";
            // 
            // topVelocityYesValue
            // 
            topVelocityYesValue.AutoSize = true;
            topVelocityYesValue.Dock = DockStyle.Fill;
            topVelocityYesValue.Location = new Point(3, 44);
            topVelocityYesValue.Name = "topVelocityYesValue";
            topVelocityYesValue.Size = new Size(56, 22);
            topVelocityYesValue.TabIndex = 4;
            topVelocityYesValue.Text = "--";
            // 
            // topVelocityNoValue
            // 
            topVelocityNoValue.AutoSize = true;
            topVelocityNoValue.Dock = DockStyle.Fill;
            topVelocityNoValue.Location = new Point(65, 44);
            topVelocityNoValue.Name = "topVelocityNoValue";
            topVelocityNoValue.Size = new Size(25, 22);
            topVelocityNoValue.TabIndex = 5;
            topVelocityNoValue.Text = "--";
            // 
            // bottomVelocityLabel
            // 
            bottomVelocityLabel.AutoSize = true;
            bottomVelocityLabel.Dock = DockStyle.Fill;
            bottomVelocityLabel.Location = new Point(96, 47);
            bottomVelocityLabel.Name = "bottomVelocityLabel";
            bottomVelocityLabel.Size = new Size(25, 16);
            bottomVelocityLabel.TabIndex = 6;
            bottomVelocityLabel.Text = "Bottom Velocity:";
            // 
            // bottomVelocityYesValue
            // 
            bottomVelocityYesValue.AutoSize = true;
            bottomVelocityYesValue.Dock = DockStyle.Fill;
            bottomVelocityYesValue.Location = new Point(3, 66);
            bottomVelocityYesValue.Name = "bottomVelocityYesValue";
            bottomVelocityYesValue.Size = new Size(56, 22);
            bottomVelocityYesValue.TabIndex = 7;
            bottomVelocityYesValue.Text = "--";
            // 
            // bottomVelocityNoValue
            // 
            bottomVelocityNoValue.AutoSize = true;
            bottomVelocityNoValue.Dock = DockStyle.Fill;
            bottomVelocityNoValue.Location = new Point(65, 66);
            bottomVelocityNoValue.Name = "bottomVelocityNoValue";
            bottomVelocityNoValue.Size = new Size(25, 22);
            bottomVelocityNoValue.TabIndex = 8;
            bottomVelocityNoValue.Text = "--";
            // 
            // netOrderRateLabel
            // 
            netOrderRateLabel.AutoSize = true;
            netOrderRateLabel.Dock = DockStyle.Fill;
            netOrderRateLabel.Location = new Point(96, 69);
            netOrderRateLabel.Name = "netOrderRateLabel";
            netOrderRateLabel.Size = new Size(25, 16);
            netOrderRateLabel.TabIndex = 9;
            netOrderRateLabel.Text = "Net Order Rate:";
            // 
            // netOrderRateYesValue
            // 
            netOrderRateYesValue.AutoSize = true;
            netOrderRateYesValue.Dock = DockStyle.Fill;
            netOrderRateYesValue.Location = new Point(3, 88);
            netOrderRateYesValue.Name = "netOrderRateYesValue";
            netOrderRateYesValue.Size = new Size(56, 22);
            netOrderRateYesValue.TabIndex = 10;
            netOrderRateYesValue.Text = "--";
            // 
            // netOrderRateNoValue
            // 
            netOrderRateNoValue.AutoSize = true;
            netOrderRateNoValue.Dock = DockStyle.Fill;
            netOrderRateNoValue.Location = new Point(65, 88);
            netOrderRateNoValue.Name = "netOrderRateNoValue";
            netOrderRateNoValue.Size = new Size(25, 22);
            netOrderRateNoValue.TabIndex = 11;
            netOrderRateNoValue.Text = "--";
            // 
            // tradeVolumeLabel
            // 
            tradeVolumeLabel.AutoSize = true;
            tradeVolumeLabel.Dock = DockStyle.Fill;
            tradeVolumeLabel.Location = new Point(96, 91);
            tradeVolumeLabel.Name = "tradeVolumeLabel";
            tradeVolumeLabel.Size = new Size(25, 16);
            tradeVolumeLabel.TabIndex = 12;
            tradeVolumeLabel.Text = "Trade Volume:";
            // 
            // tradeVolumeYesValue
            // 
            tradeVolumeYesValue.AutoSize = true;
            tradeVolumeYesValue.Dock = DockStyle.Fill;
            tradeVolumeYesValue.Location = new Point(3, 110);
            tradeVolumeYesValue.Name = "tradeVolumeYesValue";
            tradeVolumeYesValue.Size = new Size(56, 22);
            tradeVolumeYesValue.TabIndex = 13;
            tradeVolumeYesValue.Text = "--";
            // 
            // tradeVolumeNoValue
            // 
            tradeVolumeNoValue.AutoSize = true;
            tradeVolumeNoValue.Dock = DockStyle.Fill;
            tradeVolumeNoValue.Location = new Point(65, 110);
            tradeVolumeNoValue.Name = "tradeVolumeNoValue";
            tradeVolumeNoValue.Size = new Size(25, 22);
            tradeVolumeNoValue.TabIndex = 14;
            tradeVolumeNoValue.Text = "--";
            // 
            // avgTradeSizeLabel
            // 
            avgTradeSizeLabel.AutoSize = true;
            avgTradeSizeLabel.Dock = DockStyle.Fill;
            avgTradeSizeLabel.Location = new Point(96, 113);
            avgTradeSizeLabel.Name = "avgTradeSizeLabel";
            avgTradeSizeLabel.Size = new Size(25, 16);
            avgTradeSizeLabel.TabIndex = 15;
            avgTradeSizeLabel.Text = "Avg Trade Size:";
            // 
            // avgTradeSizeYesValue
            // 
            avgTradeSizeYesValue.AutoSize = true;
            avgTradeSizeYesValue.Dock = DockStyle.Fill;
            avgTradeSizeYesValue.Location = new Point(3, 132);
            avgTradeSizeYesValue.Name = "avgTradeSizeYesValue";
            avgTradeSizeYesValue.Size = new Size(56, 22);
            avgTradeSizeYesValue.TabIndex = 16;
            avgTradeSizeYesValue.Text = "--";
            // 
            // avgTradeSizeNoValue
            // 
            avgTradeSizeNoValue.AutoSize = true;
            avgTradeSizeNoValue.Dock = DockStyle.Fill;
            avgTradeSizeNoValue.Location = new Point(65, 132);
            avgTradeSizeNoValue.Name = "avgTradeSizeNoValue";
            avgTradeSizeNoValue.Size = new Size(25, 22);
            avgTradeSizeNoValue.TabIndex = 17;
            avgTradeSizeNoValue.Text = "--";
            // 
            // slopeLabel
            // 
            slopeLabel.AutoSize = true;
            slopeLabel.Dock = DockStyle.Fill;
            slopeLabel.Location = new Point(96, 135);
            slopeLabel.Name = "slopeLabel";
            slopeLabel.Size = new Size(25, 16);
            slopeLabel.TabIndex = 18;
            slopeLabel.Text = "Slope:";
            // 
            // slopeYesValue
            // 
            slopeYesValue.AutoSize = true;
            slopeYesValue.Dock = DockStyle.Fill;
            slopeYesValue.Location = new Point(3, 154);
            slopeYesValue.Name = "slopeYesValue";
            slopeYesValue.Size = new Size(56, 24);
            slopeYesValue.TabIndex = 19;
            slopeYesValue.Text = "--";
            // 
            // slopeNoValue
            // 
            slopeNoValue.AutoSize = true;
            slopeNoValue.Dock = DockStyle.Fill;
            slopeNoValue.Location = new Point(65, 154);
            slopeNoValue.Name = "slopeNoValue";
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
            // contextHeader
            // 
            contextHeader.AutoSize = true;
            contextGrid.SetColumnSpan(contextHeader, 3);
            contextHeader.Dock = DockStyle.Fill;
            contextHeader.Location = new Point(3, 0);
            contextHeader.Name = "contextHeader";
            contextHeader.Size = new Size(118, 26);
            contextHeader.TabIndex = 0;
            contextHeader.Text = "Context";
            // 
            // contextHeaderYes
            // 
            contextHeaderYes.AutoSize = true;
            contextHeaderYes.Dock = DockStyle.Fill;
            contextHeaderYes.Location = new Point(3, 26);
            contextHeaderYes.Name = "contextHeaderYes";
            contextHeaderYes.Size = new Size(56, 26);
            contextHeaderYes.TabIndex = 1;
            contextHeaderYes.Text = "Yes";
            // 
            // contextHeaderNo
            // 
            contextHeaderNo.AutoSize = true;
            contextHeaderNo.Dock = DockStyle.Fill;
            contextHeaderNo.Location = new Point(65, 26);
            contextHeaderNo.Name = "contextHeaderNo";
            contextHeaderNo.Size = new Size(25, 26);
            contextHeaderNo.TabIndex = 2;
            contextHeaderNo.Text = "No";
            // 
            // spreadLabel
            // 
            spreadLabel.AutoSize = true;
            spreadLabel.Dock = DockStyle.Fill;
            spreadLabel.Location = new Point(96, 26);
            spreadLabel.Name = "spreadLabel";
            spreadLabel.Size = new Size(25, 26);
            spreadLabel.TabIndex = 3;
            spreadLabel.Text = "Spread:";
            // 
            // spreadValue
            // 
            spreadValue.AutoSize = true;
            contextGrid.SetColumnSpan(spreadValue, 2);
            spreadValue.Dock = DockStyle.Fill;
            spreadValue.Location = new Point(3, 52);
            spreadValue.Name = "spreadValue";
            spreadValue.Size = new Size(87, 26);
            spreadValue.TabIndex = 4;
            spreadValue.Text = "--";
            // 
            // imbalLabel
            // 
            imbalLabel.AutoSize = true;
            imbalLabel.Dock = DockStyle.Fill;
            imbalLabel.Location = new Point(96, 55);
            imbalLabel.Name = "imbalLabel";
            imbalLabel.Size = new Size(25, 20);
            imbalLabel.TabIndex = 5;
            imbalLabel.Text = "Imbal:";
            // 
            // imbalValue
            // 
            imbalValue.AutoSize = true;
            contextGrid.SetColumnSpan(imbalValue, 2);
            imbalValue.Dock = DockStyle.Fill;
            imbalValue.Location = new Point(3, 78);
            imbalValue.Name = "imbalValue";
            imbalValue.Size = new Size(87, 26);
            imbalValue.TabIndex = 6;
            imbalValue.Text = "--";
            // 
            // depthTop4Label
            // 
            depthTop4Label.AutoSize = true;
            depthTop4Label.Dock = DockStyle.Fill;
            depthTop4Label.Location = new Point(96, 81);
            depthTop4Label.Name = "depthTop4Label";
            depthTop4Label.Size = new Size(25, 20);
            depthTop4Label.TabIndex = 7;
            depthTop4Label.Text = "Depth Top 4:";
            // 
            // depthTop4YesValue
            // 
            depthTop4YesValue.AutoSize = true;
            depthTop4YesValue.Dock = DockStyle.Fill;
            depthTop4YesValue.Location = new Point(3, 104);
            depthTop4YesValue.Name = "depthTop4YesValue";
            depthTop4YesValue.Size = new Size(56, 26);
            depthTop4YesValue.TabIndex = 8;
            depthTop4YesValue.Text = "--";
            // 
            // depthTop4NoValue
            // 
            depthTop4NoValue.AutoSize = true;
            depthTop4NoValue.Dock = DockStyle.Fill;
            depthTop4NoValue.Location = new Point(65, 104);
            depthTop4NoValue.Name = "depthTop4NoValue";
            depthTop4NoValue.Size = new Size(25, 26);
            depthTop4NoValue.TabIndex = 9;
            depthTop4NoValue.Text = "--";
            // 
            // centerMassLabel
            // 
            centerMassLabel.AutoSize = true;
            centerMassLabel.Dock = DockStyle.Fill;
            centerMassLabel.Location = new Point(96, 107);
            centerMassLabel.Name = "centerMassLabel";
            centerMassLabel.Size = new Size(25, 20);
            centerMassLabel.TabIndex = 10;
            centerMassLabel.Text = "Center Mass:";
            // 
            // centerMassYesValue
            // 
            centerMassYesValue.AutoSize = true;
            centerMassYesValue.Dock = DockStyle.Fill;
            centerMassYesValue.Location = new Point(3, 130);
            centerMassYesValue.Name = "centerMassYesValue";
            centerMassYesValue.Size = new Size(56, 26);
            centerMassYesValue.TabIndex = 11;
            centerMassYesValue.Text = "--";
            // 
            // centerMassNoValue
            // 
            centerMassNoValue.AutoSize = true;
            centerMassNoValue.Dock = DockStyle.Fill;
            centerMassNoValue.Location = new Point(65, 130);
            centerMassNoValue.Name = "centerMassNoValue";
            centerMassNoValue.Size = new Size(25, 26);
            centerMassNoValue.TabIndex = 12;
            centerMassNoValue.Text = "--";
            // 
            // totalContractsLabel
            // 
            totalContractsLabel.AutoSize = true;
            totalContractsLabel.Dock = DockStyle.Fill;
            totalContractsLabel.Location = new Point(96, 133);
            totalContractsLabel.Name = "totalContractsLabel";
            totalContractsLabel.Size = new Size(25, 20);
            totalContractsLabel.TabIndex = 13;
            totalContractsLabel.Text = "Total Contracts:";
            // 
            // totalContractsYesValue
            // 
            totalContractsYesValue.AutoSize = true;
            totalContractsYesValue.Dock = DockStyle.Fill;
            totalContractsYesValue.Location = new Point(3, 156);
            totalContractsYesValue.Name = "totalContractsYesValue";
            totalContractsYesValue.Size = new Size(56, 24);
            totalContractsYesValue.TabIndex = 14;
            totalContractsYesValue.Text = "--";
            // 
            // totalContractsNoValue
            // 
            totalContractsNoValue.AutoSize = true;
            totalContractsNoValue.Dock = DockStyle.Fill;
            totalContractsNoValue.Location = new Point(65, 156);
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
            positionsLayout.Controls.Add(positionTextBox, 0, 0);
            positionsLayout.Controls.Add(positionsGrid, 0, 1);
            positionsLayout.Dock = DockStyle.Fill;
            positionsLayout.Location = new Point(6, 6);
            positionsLayout.Margin = new Padding(0);
            positionsLayout.Name = "positionsLayout";
            positionsLayout.RowCount = 2;
            positionsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            positionsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 70F));
            positionsLayout.Size = new Size(579, 143);
            positionsLayout.TabIndex = 0;
            // 
            // positionTextBox
            // 
            positionTextBox.Dock = DockStyle.Fill;
            positionTextBox.Location = new Point(3, 3);
            positionTextBox.Multiline = true;
            positionTextBox.Name = "positionTextBox";
            positionTextBox.ReadOnly = true;
            positionTextBox.Size = new Size(573, 36);
            positionTextBox.TabIndex = 0;
            // 
            // positionsGrid
            // 
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
            positionsGrid.Dock = DockStyle.Fill;
            positionsGrid.Location = new Point(3, 45);
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
            positionsGrid.Size = new Size(573, 95);
            positionsGrid.TabIndex = 1;
            // 
            // positionSizeLabel
            // 
            positionSizeLabel.AutoSize = true;
            positionSizeLabel.Dock = DockStyle.Fill;
            positionSizeLabel.Location = new Point(3, 3);
            positionSizeLabel.Name = "positionSizeLabel";
            positionSizeLabel.Size = new Size(337, 5);
            positionSizeLabel.TabIndex = 0;
            positionSizeLabel.Text = "Position Size:";
            // 
            // positionSizeValue
            // 
            positionSizeValue.AutoSize = true;
            positionSizeValue.Dock = DockStyle.Fill;
            positionSizeValue.Location = new Point(346, 0);
            positionSizeValue.Name = "positionSizeValue";
            positionSizeValue.Size = new Size(224, 11);
            positionSizeValue.TabIndex = 1;
            positionSizeValue.Text = "--";
            // 
            // lastTradeLabel
            // 
            lastTradeLabel.AutoSize = true;
            lastTradeLabel.Dock = DockStyle.Fill;
            lastTradeLabel.Location = new Point(3, 11);
            lastTradeLabel.Name = "lastTradeLabel";
            lastTradeLabel.Size = new Size(337, 11);
            lastTradeLabel.TabIndex = 2;
            lastTradeLabel.Text = "Last Trade:";
            // 
            // lastTradeValue
            // 
            lastTradeValue.AutoSize = true;
            lastTradeValue.Dock = DockStyle.Fill;
            lastTradeValue.Location = new Point(346, 11);
            lastTradeValue.Name = "lastTradeValue";
            lastTradeValue.Size = new Size(224, 11);
            lastTradeValue.TabIndex = 3;
            lastTradeValue.Text = "--";
            // 
            // positionRoiLabel
            // 
            positionRoiLabel.AutoSize = true;
            positionRoiLabel.Dock = DockStyle.Fill;
            positionRoiLabel.Location = new Point(3, 25);
            positionRoiLabel.Name = "positionRoiLabel";
            positionRoiLabel.Size = new Size(337, 5);
            positionRoiLabel.TabIndex = 4;
            positionRoiLabel.Text = "Position ROI:";
            // 
            // positionRoiValue
            // 
            positionRoiValue.AutoSize = true;
            positionRoiValue.Dock = DockStyle.Fill;
            positionRoiValue.Location = new Point(346, 22);
            positionRoiValue.Name = "positionRoiValue";
            positionRoiValue.Size = new Size(224, 11);
            positionRoiValue.TabIndex = 5;
            positionRoiValue.Text = "--";
            // 
            // buyinPriceLabel
            // 
            buyinPriceLabel.AutoSize = true;
            buyinPriceLabel.Dock = DockStyle.Fill;
            buyinPriceLabel.Location = new Point(3, 33);
            buyinPriceLabel.Name = "buyinPriceLabel";
            buyinPriceLabel.Size = new Size(337, 11);
            buyinPriceLabel.TabIndex = 6;
            buyinPriceLabel.Text = "Buyin Price:";
            // 
            // buyinPriceValue
            // 
            buyinPriceValue.AutoSize = true;
            buyinPriceValue.Dock = DockStyle.Fill;
            buyinPriceValue.Location = new Point(346, 33);
            buyinPriceValue.Name = "buyinPriceValue";
            buyinPriceValue.Size = new Size(224, 11);
            buyinPriceValue.TabIndex = 7;
            buyinPriceValue.Text = "--";
            // 
            // positionUpsideLabel
            // 
            positionUpsideLabel.AutoSize = true;
            positionUpsideLabel.Dock = DockStyle.Fill;
            positionUpsideLabel.Location = new Point(3, 44);
            positionUpsideLabel.Name = "positionUpsideLabel";
            positionUpsideLabel.Size = new Size(337, 11);
            positionUpsideLabel.TabIndex = 8;
            positionUpsideLabel.Text = "Position Upside:";
            // 
            // positionUpsideValue
            // 
            positionUpsideValue.AutoSize = true;
            positionUpsideValue.Dock = DockStyle.Fill;
            positionUpsideValue.Location = new Point(346, 44);
            positionUpsideValue.Name = "positionUpsideValue";
            positionUpsideValue.Size = new Size(224, 11);
            positionUpsideValue.TabIndex = 9;
            positionUpsideValue.Text = "--";
            // 
            // positionDownsideLabel
            // 
            positionDownsideLabel.AutoSize = true;
            positionDownsideLabel.Dock = DockStyle.Fill;
            positionDownsideLabel.Location = new Point(3, 55);
            positionDownsideLabel.Name = "positionDownsideLabel";
            positionDownsideLabel.Size = new Size(337, 11);
            positionDownsideLabel.TabIndex = 10;
            positionDownsideLabel.Text = "Position Downside:";
            // 
            // positionDownsideValue
            // 
            positionDownsideValue.AutoSize = true;
            positionDownsideValue.Dock = DockStyle.Fill;
            positionDownsideValue.Location = new Point(346, 55);
            positionDownsideValue.Name = "positionDownsideValue";
            positionDownsideValue.Size = new Size(224, 11);
            positionDownsideValue.TabIndex = 11;
            positionDownsideValue.Text = "--";
            // 
            // restingOrdersLabel
            // 
            restingOrdersLabel.AutoSize = true;
            restingOrdersLabel.Dock = DockStyle.Fill;
            restingOrdersLabel.Location = new Point(3, 69);
            restingOrdersLabel.Name = "restingOrdersLabel";
            restingOrdersLabel.Size = new Size(337, 5);
            restingOrdersLabel.TabIndex = 12;
            restingOrdersLabel.Text = "Resting Orders:";
            // 
            // restingOrdersValue
            // 
            restingOrdersValue.AutoSize = true;
            restingOrdersValue.Dock = DockStyle.Fill;
            restingOrdersValue.Location = new Point(346, 66);
            restingOrdersValue.Name = "restingOrdersValue";
            restingOrdersValue.Size = new Size(224, 11);
            restingOrdersValue.TabIndex = 13;
            restingOrdersValue.Text = "--";
            // 
            // simulatedPositionLabel
            // 
            simulatedPositionLabel.AutoSize = true;
            simulatedPositionLabel.Dock = DockStyle.Fill;
            simulatedPositionLabel.Location = new Point(3, 80);
            simulatedPositionLabel.Name = "simulatedPositionLabel";
            simulatedPositionLabel.Size = new Size(337, 12);
            simulatedPositionLabel.TabIndex = 14;
            simulatedPositionLabel.Text = "Simulated Position:";
            // 
            // simulatedPositionValue
            // 
            simulatedPositionValue.AutoSize = true;
            simulatedPositionValue.Dock = DockStyle.Fill;
            simulatedPositionValue.Location = new Point(346, 77);
            simulatedPositionValue.Name = "simulatedPositionValue";
            simulatedPositionValue.Size = new Size(224, 18);
            simulatedPositionValue.TabIndex = 15;
            simulatedPositionValue.Text = "--";
            // 
            // orderbookContainer
            // 
            orderbookContainer.BorderStyle = BorderStyle.FixedSingle;
            orderbookContainer.Controls.Add(orderbookGrid);
            orderbookContainer.Dock = DockStyle.Fill;
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
        private TableLayoutPanel positionsLayout;
        private TextBox positionTextBox;
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
    }
}