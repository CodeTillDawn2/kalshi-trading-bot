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
            chartHeader = new Label();
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
            mainLayout.SuspendLayout();
            dashboardGrid.SuspendLayout();
            chartContainer.SuspendLayout();
            chartLayout.SuspendLayout();
            chartControls.SuspendLayout();
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
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
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
            dashboardGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 74.9226F));
            dashboardGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25.0774F));
            dashboardGrid.Size = new Size(925, 646);
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
            chartContainer.Size = new Size(593, 478);
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
            chartLayout.Margin = new Padding(4, 3, 4, 3);
            chartLayout.Name = "chartLayout";
            chartLayout.RowCount = 2;
            chartLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            chartLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            chartLayout.Size = new Size(579, 464);
            chartLayout.TabIndex = 0;
            // 
            // chartControls
            // 
            chartControls.AutoSize = true;
            chartControls.Controls.Add(chartHeader);
            chartControls.Dock = DockStyle.Fill;
            chartControls.Location = new Point(4, 3);
            chartControls.Margin = new Padding(4, 3, 4, 3);
            chartControls.Name = "chartControls";
            chartControls.Size = new Size(571, 14);
            chartControls.TabIndex = 0;
            // 
            // chartHeader
            // 
            chartHeader.AutoSize = true;
            chartHeader.Location = new Point(0, 0);
            chartHeader.Margin = new Padding(0);
            chartHeader.Name = "chartHeader";
            chartHeader.Size = new Size(76, 15);
            chartHeader.TabIndex = 0;
            chartHeader.Text = "Price Chart - ";
            // 
            // priceChart
            // 
            priceChart.Dock = DockStyle.Fill;
            priceChart.Location = new Point(4, 23);
            priceChart.Margin = new Padding(4, 3, 4, 3);
            priceChart.Name = "priceChart";
            priceChart.Size = new Size(571, 438);
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
            marketInfoContainer.Size = new Size(316, 478);
            marketInfoContainer.TabIndex = 1;
            // 
            // infoGrid
            // 
            infoGrid.ColumnCount = 2;
            infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            infoGrid.Controls.Add(leftColumn, 0, 0);
            infoGrid.Controls.Add(rightColumn, 1, 0);
            infoGrid.Dock = DockStyle.Fill;
            infoGrid.Location = new Point(6, 6);
            infoGrid.Margin = new Padding(4, 3, 4, 3);
            infoGrid.Name = "infoGrid";
            infoGrid.RowCount = 1;
            infoGrid.RowStyles.Add(new RowStyle());
            infoGrid.Size = new Size(302, 464);
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
            leftColumn.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
            leftColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            leftColumn.Size = new Size(145, 458);
            leftColumn.TabIndex = 0;
            // 
            // pricesGrid
            // 
            pricesGrid.AutoSize = true;
            pricesGrid.ColumnCount = 3;
            pricesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            pricesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            pricesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
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
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            pricesGrid.Size = new Size(139, 144);
            pricesGrid.TabIndex = 0;
            // 
            // pricesHeaderEmpty
            // 
            pricesHeaderEmpty.AutoSize = true;
            pricesHeaderEmpty.Dock = DockStyle.Fill;
            pricesHeaderEmpty.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            pricesHeaderEmpty.Location = new Point(3, 0);
            pricesHeaderEmpty.Name = "pricesHeaderEmpty";
            pricesHeaderEmpty.Size = new Size(49, 20);
            pricesHeaderEmpty.TabIndex = 0;
            pricesHeaderEmpty.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pricesHeaderNo
            // 
            pricesHeaderNo.AutoSize = true;
            pricesHeaderNo.Dock = DockStyle.Fill;
            pricesHeaderNo.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            pricesHeaderNo.Location = new Point(58, 0);
            pricesHeaderNo.Name = "pricesHeaderNo";
            pricesHeaderNo.Size = new Size(35, 20);
            pricesHeaderNo.TabIndex = 1;
            pricesHeaderNo.Text = "No";
            pricesHeaderNo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pricesHeaderYes
            // 
            pricesHeaderYes.AutoSize = true;
            pricesHeaderYes.Dock = DockStyle.Fill;
            pricesHeaderYes.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            pricesHeaderYes.Location = new Point(99, 0);
            pricesHeaderYes.Name = "pricesHeaderYes";
            pricesHeaderYes.Size = new Size(37, 20);
            pricesHeaderYes.TabIndex = 2;
            pricesHeaderYes.Text = "Yes";
            pricesHeaderYes.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // allTimeHighLabel
            // 
            allTimeHighLabel.AutoSize = true;
            allTimeHighLabel.Dock = DockStyle.Fill;
            allTimeHighLabel.Location = new Point(3, 20);
            allTimeHighLabel.Name = "allTimeHighLabel";
            allTimeHighLabel.Size = new Size(49, 30);
            allTimeHighLabel.TabIndex = 3;
            allTimeHighLabel.Text = "All Time High:";
            allTimeHighLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // allTimeHighAsk
            // 
            allTimeHighAsk.Controls.Add(allTimeHighAskPrice);
            allTimeHighAsk.Controls.Add(allTimeHighAskTime);
            allTimeHighAsk.Dock = DockStyle.Fill;
            allTimeHighAsk.Location = new Point(58, 23);
            allTimeHighAsk.Name = "allTimeHighAsk";
            allTimeHighAsk.Size = new Size(35, 24);
            allTimeHighAsk.TabIndex = 4;
            // 
            // allTimeHighAskPrice
            // 
            allTimeHighAskPrice.AutoSize = true;
            allTimeHighAskPrice.Dock = DockStyle.Top;
            allTimeHighAskPrice.Location = new Point(0, 0);
            allTimeHighAskPrice.Name = "allTimeHighAskPrice";
            allTimeHighAskPrice.Size = new Size(17, 15);
            allTimeHighAskPrice.TabIndex = 0;
            allTimeHighAskPrice.Text = "--";
            // 
            // allTimeHighAskTime
            // 
            allTimeHighAskTime.AutoSize = true;
            allTimeHighAskTime.Dock = DockStyle.Bottom;
            allTimeHighAskTime.Font = new Font("Segoe UI", 7F);
            allTimeHighAskTime.Location = new Point(0, 12);
            allTimeHighAskTime.Name = "allTimeHighAskTime";
            allTimeHighAskTime.Size = new Size(13, 12);
            allTimeHighAskTime.TabIndex = 1;
            allTimeHighAskTime.Text = "--";
            // 
            // allTimeHighBid
            // 
            allTimeHighBid.Controls.Add(allTimeHighBidPrice);
            allTimeHighBid.Controls.Add(allTimeHighBidTime);
            allTimeHighBid.Dock = DockStyle.Fill;
            allTimeHighBid.Location = new Point(99, 23);
            allTimeHighBid.Name = "allTimeHighBid";
            allTimeHighBid.Size = new Size(37, 24);
            allTimeHighBid.TabIndex = 5;
            // 
            // allTimeHighBidPrice
            // 
            allTimeHighBidPrice.AutoSize = true;
            allTimeHighBidPrice.Dock = DockStyle.Top;
            allTimeHighBidPrice.Location = new Point(0, 0);
            allTimeHighBidPrice.Name = "allTimeHighBidPrice";
            allTimeHighBidPrice.Size = new Size(17, 15);
            allTimeHighBidPrice.TabIndex = 0;
            allTimeHighBidPrice.Text = "--";
            // 
            // allTimeHighBidTime
            // 
            allTimeHighBidTime.AutoSize = true;
            allTimeHighBidTime.Dock = DockStyle.Bottom;
            allTimeHighBidTime.Font = new Font("Segoe UI", 7F);
            allTimeHighBidTime.Location = new Point(0, 12);
            allTimeHighBidTime.Name = "allTimeHighBidTime";
            allTimeHighBidTime.Size = new Size(13, 12);
            allTimeHighBidTime.TabIndex = 1;
            allTimeHighBidTime.Text = "--";
            // 
            // recentHighLabel
            // 
            recentHighLabel.AutoSize = true;
            recentHighLabel.Dock = DockStyle.Fill;
            recentHighLabel.Location = new Point(3, 50);
            recentHighLabel.Name = "recentHighLabel";
            recentHighLabel.Size = new Size(49, 30);
            recentHighLabel.TabIndex = 6;
            recentHighLabel.Text = "Recent High:";
            recentHighLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // recentHighAsk
            // 
            recentHighAsk.Controls.Add(recentHighAskPrice);
            recentHighAsk.Controls.Add(recentHighAskTime);
            recentHighAsk.Dock = DockStyle.Fill;
            recentHighAsk.Location = new Point(58, 53);
            recentHighAsk.Name = "recentHighAsk";
            recentHighAsk.Size = new Size(35, 24);
            recentHighAsk.TabIndex = 7;
            // 
            // recentHighAskPrice
            // 
            recentHighAskPrice.AutoSize = true;
            recentHighAskPrice.Dock = DockStyle.Top;
            recentHighAskPrice.Location = new Point(0, 0);
            recentHighAskPrice.Name = "recentHighAskPrice";
            recentHighAskPrice.Size = new Size(17, 15);
            recentHighAskPrice.TabIndex = 0;
            recentHighAskPrice.Text = "--";
            // 
            // recentHighAskTime
            // 
            recentHighAskTime.AutoSize = true;
            recentHighAskTime.Dock = DockStyle.Bottom;
            recentHighAskTime.Font = new Font("Segoe UI", 7F);
            recentHighAskTime.Location = new Point(0, 12);
            recentHighAskTime.Name = "recentHighAskTime";
            recentHighAskTime.Size = new Size(13, 12);
            recentHighAskTime.TabIndex = 1;
            recentHighAskTime.Text = "--";
            // 
            // recentHighBid
            // 
            recentHighBid.Controls.Add(recentHighBidPrice);
            recentHighBid.Controls.Add(recentHighBidTime);
            recentHighBid.Dock = DockStyle.Fill;
            recentHighBid.Location = new Point(99, 53);
            recentHighBid.Name = "recentHighBid";
            recentHighBid.Size = new Size(37, 24);
            recentHighBid.TabIndex = 8;
            // 
            // recentHighBidPrice
            // 
            recentHighBidPrice.AutoSize = true;
            recentHighBidPrice.Dock = DockStyle.Top;
            recentHighBidPrice.Location = new Point(0, 0);
            recentHighBidPrice.Name = "recentHighBidPrice";
            recentHighBidPrice.Size = new Size(17, 15);
            recentHighBidPrice.TabIndex = 0;
            recentHighBidPrice.Text = "--";
            // 
            // recentHighBidTime
            // 
            recentHighBidTime.AutoSize = true;
            recentHighBidTime.Dock = DockStyle.Bottom;
            recentHighBidTime.Font = new Font("Segoe UI", 7F);
            recentHighBidTime.Location = new Point(0, 12);
            recentHighBidTime.Name = "recentHighBidTime";
            recentHighBidTime.Size = new Size(13, 12);
            recentHighBidTime.TabIndex = 1;
            recentHighBidTime.Text = "--";
            // 
            // currentPriceLabel
            // 
            currentPriceLabel.AutoSize = true;
            currentPriceLabel.Dock = DockStyle.Fill;
            currentPriceLabel.Location = new Point(3, 80);
            currentPriceLabel.Name = "currentPriceLabel";
            currentPriceLabel.Size = new Size(49, 20);
            currentPriceLabel.TabIndex = 9;
            currentPriceLabel.Text = "Current:";
            currentPriceLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // currentPriceAsk
            // 
            currentPriceAsk.AutoSize = true;
            currentPriceAsk.Dock = DockStyle.Fill;
            currentPriceAsk.Location = new Point(58, 80);
            currentPriceAsk.Name = "currentPriceAsk";
            currentPriceAsk.Size = new Size(35, 20);
            currentPriceAsk.TabIndex = 10;
            currentPriceAsk.Text = "--";
            currentPriceAsk.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // currentPriceBid
            // 
            currentPriceBid.AutoSize = true;
            currentPriceBid.Dock = DockStyle.Fill;
            currentPriceBid.Location = new Point(99, 80);
            currentPriceBid.Name = "currentPriceBid";
            currentPriceBid.Size = new Size(37, 20);
            currentPriceBid.TabIndex = 11;
            currentPriceBid.Text = "--";
            currentPriceBid.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // recentLowLabel
            // 
            recentLowLabel.AutoSize = true;
            recentLowLabel.Dock = DockStyle.Fill;
            recentLowLabel.Location = new Point(3, 100);
            recentLowLabel.Name = "recentLowLabel";
            recentLowLabel.Size = new Size(49, 30);
            recentLowLabel.TabIndex = 12;
            recentLowLabel.Text = "Recent Low:";
            recentLowLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // recentLowAsk
            // 
            recentLowAsk.Controls.Add(recentLowAskPrice);
            recentLowAsk.Controls.Add(recentLowAskTime);
            recentLowAsk.Dock = DockStyle.Fill;
            recentLowAsk.Location = new Point(58, 103);
            recentLowAsk.Name = "recentLowAsk";
            recentLowAsk.Size = new Size(35, 24);
            recentLowAsk.TabIndex = 13;
            // 
            // recentLowAskPrice
            // 
            recentLowAskPrice.AutoSize = true;
            recentLowAskPrice.Dock = DockStyle.Top;
            recentLowAskPrice.Location = new Point(0, 0);
            recentLowAskPrice.Name = "recentLowAskPrice";
            recentLowAskPrice.Size = new Size(17, 15);
            recentLowAskPrice.TabIndex = 0;
            recentLowAskPrice.Text = "--";
            // 
            // recentLowAskTime
            // 
            recentLowAskTime.AutoSize = true;
            recentLowAskTime.Dock = DockStyle.Bottom;
            recentLowAskTime.Font = new Font("Segoe UI", 7F);
            recentLowAskTime.Location = new Point(0, 12);
            recentLowAskTime.Name = "recentLowAskTime";
            recentLowAskTime.Size = new Size(13, 12);
            recentLowAskTime.TabIndex = 1;
            recentLowAskTime.Text = "--";
            // 
            // recentLowBid
            // 
            recentLowBid.Controls.Add(recentLowBidPrice);
            recentLowBid.Controls.Add(recentLowBidTime);
            recentLowBid.Dock = DockStyle.Fill;
            recentLowBid.Location = new Point(99, 103);
            recentLowBid.Name = "recentLowBid";
            recentLowBid.Size = new Size(37, 24);
            recentLowBid.TabIndex = 14;
            // 
            // recentLowBidPrice
            // 
            recentLowBidPrice.AutoSize = true;
            recentLowBidPrice.Dock = DockStyle.Top;
            recentLowBidPrice.Location = new Point(0, 0);
            recentLowBidPrice.Name = "recentLowBidPrice";
            recentLowBidPrice.Size = new Size(17, 15);
            recentLowBidPrice.TabIndex = 0;
            recentLowBidPrice.Text = "--";
            // 
            // recentLowBidTime
            // 
            recentLowBidTime.AutoSize = true;
            recentLowBidTime.Dock = DockStyle.Bottom;
            recentLowBidTime.Font = new Font("Segoe UI", 7F);
            recentLowBidTime.Location = new Point(0, 12);
            recentLowBidTime.Name = "recentLowBidTime";
            recentLowBidTime.Size = new Size(13, 12);
            recentLowBidTime.TabIndex = 1;
            recentLowBidTime.Text = "--";
            // 
            // allTimeLowLabel
            // 
            allTimeLowLabel.AutoSize = true;
            allTimeLowLabel.Dock = DockStyle.Fill;
            allTimeLowLabel.Location = new Point(3, 130);
            allTimeLowLabel.Name = "allTimeLowLabel";
            allTimeLowLabel.Size = new Size(49, 30);
            allTimeLowLabel.TabIndex = 15;
            allTimeLowLabel.Text = "All Time Low:";
            allTimeLowLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // allTimeLowAsk
            // 
            allTimeLowAsk.Controls.Add(allTimeLowAskPrice);
            allTimeLowAsk.Controls.Add(allTimeLowAskTime);
            allTimeLowAsk.Dock = DockStyle.Fill;
            allTimeLowAsk.Location = new Point(58, 133);
            allTimeLowAsk.Name = "allTimeLowAsk";
            allTimeLowAsk.Size = new Size(35, 24);
            allTimeLowAsk.TabIndex = 16;
            // 
            // allTimeLowAskPrice
            // 
            allTimeLowAskPrice.AutoSize = true;
            allTimeLowAskPrice.Dock = DockStyle.Top;
            allTimeLowAskPrice.Location = new Point(0, 0);
            allTimeLowAskPrice.Name = "allTimeLowAskPrice";
            allTimeLowAskPrice.Size = new Size(17, 15);
            allTimeLowAskPrice.TabIndex = 0;
            allTimeLowAskPrice.Text = "--";
            // 
            // allTimeLowAskTime
            // 
            allTimeLowAskTime.AutoSize = true;
            allTimeLowAskTime.Dock = DockStyle.Bottom;
            allTimeLowAskTime.Font = new Font("Segoe UI", 7F);
            allTimeLowAskTime.Location = new Point(0, 12);
            allTimeLowAskTime.Name = "allTimeLowAskTime";
            allTimeLowAskTime.Size = new Size(13, 12);
            allTimeLowAskTime.TabIndex = 1;
            allTimeLowAskTime.Text = "--";
            // 
            // allTimeLowBid
            // 
            allTimeLowBid.Controls.Add(allTimeLowBidPrice);
            allTimeLowBid.Controls.Add(allTimeLowBidTime);
            allTimeLowBid.Dock = DockStyle.Fill;
            allTimeLowBid.Location = new Point(99, 133);
            allTimeLowBid.Name = "allTimeLowBid";
            allTimeLowBid.Size = new Size(37, 24);
            allTimeLowBid.TabIndex = 17;
            // 
            // allTimeLowBidPrice
            // 
            allTimeLowBidPrice.AutoSize = true;
            allTimeLowBidPrice.Dock = DockStyle.Top;
            allTimeLowBidPrice.Location = new Point(0, 0);
            allTimeLowBidPrice.Name = "allTimeLowBidPrice";
            allTimeLowBidPrice.Size = new Size(17, 15);
            allTimeLowBidPrice.TabIndex = 0;
            allTimeLowBidPrice.Text = "--";
            // 
            // allTimeLowBidTime
            // 
            allTimeLowBidTime.AutoSize = true;
            allTimeLowBidTime.Dock = DockStyle.Bottom;
            allTimeLowBidTime.Font = new Font("Segoe UI", 7F);
            allTimeLowBidTime.Location = new Point(0, 12);
            allTimeLowBidTime.Name = "allTimeLowBidTime";
            allTimeLowBidTime.Size = new Size(13, 12);
            allTimeLowBidTime.TabIndex = 1;
            allTimeLowBidTime.Text = "--";
            // 
            // tradingMetricsGrid
            // 
            tradingMetricsGrid.AutoSize = true;
            tradingMetricsGrid.ColumnCount = 2;
            tradingMetricsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tradingMetricsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
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
            tradingMetricsGrid.Dock = DockStyle.Bottom;
            tradingMetricsGrid.Location = new Point(3, 275);
            tradingMetricsGrid.Name = "tradingMetricsGrid";
            tradingMetricsGrid.RowCount = 11;
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            tradingMetricsGrid.Size = new Size(139, 180);
            tradingMetricsGrid.TabIndex = 1;
            // 
            // tradingMetricsHeader
            // 
            tradingMetricsHeader.AutoSize = true;
            tradingMetricsHeader.Dock = DockStyle.Fill;
            tradingMetricsHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            tradingMetricsHeader.Location = new Point(3, 0);
            tradingMetricsHeader.Name = "tradingMetricsHeader";
            tradingMetricsHeader.Size = new Size(63, 20);
            tradingMetricsHeader.TabIndex = 0;
            tradingMetricsHeader.Text = "Trading Metrics";
            tradingMetricsHeader.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // rsiLabel
            // 
            rsiLabel.AutoSize = true;
            rsiLabel.Dock = DockStyle.Fill;
            rsiLabel.Enabled = false;
            rsiLabel.Location = new Point(3, 20);
            rsiLabel.Margin = new Padding(3, 0, 3, 0);
            rsiLabel.Name = "rsiLabel";
            rsiLabel.Size = new Size(63, 16);
            rsiLabel.TabIndex = 1;
            rsiLabel.Text = "RSI:";
            // 
            // rsiValue
            // 
            rsiValue.AutoSize = true;
            rsiValue.Dock = DockStyle.Fill;
            rsiValue.Location = new Point(72, 20);
            rsiValue.Name = "rsiValue";
            rsiValue.Size = new Size(64, 16);
            rsiValue.TabIndex = 2;
            rsiValue.Text = "--";
            rsiValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // macdLabel
            // 
            macdLabel.AutoSize = true;
            macdLabel.Dock = DockStyle.Fill;
            macdLabel.Enabled = false;
            macdLabel.Location = new Point(3, 36);
            macdLabel.Margin = new Padding(3, 0, 3, 0);
            macdLabel.Name = "macdLabel";
            macdLabel.Size = new Size(63, 16);
            macdLabel.TabIndex = 3;
            macdLabel.Text = "MACD:";
            // 
            // macdValue
            // 
            macdValue.AutoSize = true;
            macdValue.Dock = DockStyle.Fill;
            macdValue.Location = new Point(72, 36);
            macdValue.Name = "macdValue";
            macdValue.Size = new Size(64, 16);
            macdValue.TabIndex = 4;
            macdValue.Text = "--";
            macdValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // emaLabel
            // 
            emaLabel.AutoSize = true;
            emaLabel.Dock = DockStyle.Fill;
            emaLabel.Enabled = false;
            emaLabel.Location = new Point(3, 52);
            emaLabel.Margin = new Padding(3, 0, 3, 0);
            emaLabel.Name = "emaLabel";
            emaLabel.Size = new Size(63, 16);
            emaLabel.TabIndex = 5;
            emaLabel.Text = "EMA:";
            // 
            // emaValue
            // 
            emaValue.AutoSize = true;
            emaValue.Dock = DockStyle.Fill;
            emaValue.Location = new Point(72, 52);
            emaValue.Name = "emaValue";
            emaValue.Size = new Size(64, 16);
            emaValue.TabIndex = 6;
            emaValue.Text = "--";
            emaValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // bollingerLabel
            // 
            bollingerLabel.AutoSize = true;
            bollingerLabel.Dock = DockStyle.Fill;
            bollingerLabel.Enabled = false;
            bollingerLabel.Location = new Point(3, 68);
            bollingerLabel.Margin = new Padding(3, 0, 3, 0);
            bollingerLabel.Name = "bollingerLabel";
            bollingerLabel.Size = new Size(63, 16);
            bollingerLabel.TabIndex = 7;
            bollingerLabel.Text = "Bollinger:";
            // 
            // bollingerValue
            // 
            bollingerValue.AutoSize = true;
            bollingerValue.Dock = DockStyle.Fill;
            bollingerValue.Location = new Point(72, 68);
            bollingerValue.Name = "bollingerValue";
            bollingerValue.Size = new Size(64, 16);
            bollingerValue.TabIndex = 8;
            bollingerValue.Text = "--";
            bollingerValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // atrLabel
            // 
            atrLabel.AutoSize = true;
            atrLabel.Dock = DockStyle.Fill;
            atrLabel.Enabled = false;
            atrLabel.Location = new Point(3, 84);
            atrLabel.Margin = new Padding(3, 0, 3, 0);
            atrLabel.Name = "atrLabel";
            atrLabel.Size = new Size(63, 16);
            atrLabel.TabIndex = 9;
            atrLabel.Text = "ATR:";
            // 
            // atrValue
            // 
            atrValue.AutoSize = true;
            atrValue.Dock = DockStyle.Fill;
            atrValue.Location = new Point(72, 84);
            atrValue.Name = "atrValue";
            atrValue.Size = new Size(64, 16);
            atrValue.TabIndex = 10;
            atrValue.Text = "--";
            atrValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // vwapLabel
            // 
            vwapLabel.AutoSize = true;
            vwapLabel.Dock = DockStyle.Fill;
            vwapLabel.Enabled = false;
            vwapLabel.Location = new Point(3, 100);
            vwapLabel.Margin = new Padding(3, 0, 3, 0);
            vwapLabel.Name = "vwapLabel";
            vwapLabel.Size = new Size(63, 16);
            vwapLabel.TabIndex = 11;
            vwapLabel.Text = "VWAP:";
            // 
            // vwapValue
            // 
            vwapValue.AutoSize = true;
            vwapValue.Dock = DockStyle.Fill;
            vwapValue.Location = new Point(72, 100);
            vwapValue.Name = "vwapValue";
            vwapValue.Size = new Size(64, 16);
            vwapValue.TabIndex = 12;
            vwapValue.Text = "--";
            vwapValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // stochasticLabel
            // 
            stochasticLabel.AutoSize = true;
            stochasticLabel.Dock = DockStyle.Fill;
            stochasticLabel.Enabled = false;
            stochasticLabel.Location = new Point(3, 116);
            stochasticLabel.Margin = new Padding(3, 0, 3, 0);
            stochasticLabel.Name = "stochasticLabel";
            stochasticLabel.Size = new Size(63, 16);
            stochasticLabel.TabIndex = 13;
            stochasticLabel.Text = "Stochastic:";
            // 
            // stochasticValue
            // 
            stochasticValue.AutoSize = true;
            stochasticValue.Dock = DockStyle.Fill;
            stochasticValue.Location = new Point(72, 116);
            stochasticValue.Name = "stochasticValue";
            stochasticValue.Size = new Size(64, 16);
            stochasticValue.TabIndex = 14;
            stochasticValue.Text = "--";
            stochasticValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // obvLabel
            // 
            obvLabel.AutoSize = true;
            obvLabel.Dock = DockStyle.Fill;
            obvLabel.Enabled = false;
            obvLabel.Location = new Point(3, 132);
            obvLabel.Margin = new Padding(3, 0, 3, 0);
            obvLabel.Name = "obvLabel";
            obvLabel.Size = new Size(63, 16);
            obvLabel.TabIndex = 15;
            obvLabel.Text = "OBV:";
            // 
            // obvValue
            // 
            obvValue.AutoSize = true;
            obvValue.Dock = DockStyle.Fill;
            obvValue.Location = new Point(72, 132);
            obvValue.Name = "obvValue";
            obvValue.Size = new Size(64, 16);
            obvValue.TabIndex = 16;
            obvValue.Text = "--";
            obvValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // psarLabel
            // 
            psarLabel.AutoSize = true;
            psarLabel.Dock = DockStyle.Fill;
            psarLabel.Enabled = false;
            psarLabel.Location = new Point(3, 148);
            psarLabel.Margin = new Padding(3, 0, 3, 0);
            psarLabel.Name = "psarLabel";
            psarLabel.Size = new Size(63, 16);
            psarLabel.TabIndex = 17;
            psarLabel.Text = "PSAR:";
            // 
            // psarValue
            // 
            psarValue.AutoSize = true;
            psarValue.Dock = DockStyle.Fill;
            psarValue.Location = new Point(72, 148);
            psarValue.Name = "psarValue";
            psarValue.Size = new Size(64, 16);
            psarValue.TabIndex = 18;
            psarValue.Text = "--";
            psarValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // adxLabel
            // 
            adxLabel.AutoSize = true;
            adxLabel.Dock = DockStyle.Top;
            adxLabel.Enabled = false;
            adxLabel.Location = new Point(3, 164);
            adxLabel.Margin = new Padding(3, 0, 3, 0);
            adxLabel.Name = "adxLabel";
            adxLabel.Size = new Size(63, 16);
            adxLabel.TabIndex = 19;
            adxLabel.Text = "ADX:";
            // 
            // adxValue
            // 
            adxValue.AutoSize = true;
            adxValue.Dock = DockStyle.Top;
            adxValue.Location = new Point(72, 164);
            adxValue.Name = "adxValue";
            adxValue.Size = new Size(64, 15);
            adxValue.TabIndex = 20;
            adxValue.Text = "--";
            adxValue.TextAlign = ContentAlignment.MiddleRight;
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
            rightColumn.Location = new Point(154, 3);
            rightColumn.Name = "rightColumn";
            rightColumn.RowCount = 3;
            rightColumn.RowStyles.Add(new RowStyle(SizeType.Absolute, 74F));
            rightColumn.RowStyles.Add(new RowStyle(SizeType.Absolute, 275F));
            rightColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rightColumn.Size = new Size(145, 458);
            rightColumn.TabIndex = 1;
            // 
            // otherInfoGrid
            // 
            otherInfoGrid.AutoSize = true;
            otherInfoGrid.ColumnCount = 2;
            otherInfoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            otherInfoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            otherInfoGrid.Controls.Add(CategoryLabel, 0, 0);
            otherInfoGrid.Controls.Add(categoryValue, 1, 0);
            otherInfoGrid.Controls.Add(timeLeftLabel, 0, 1);
            otherInfoGrid.Controls.Add(timeLeftValue, 1, 1);
            otherInfoGrid.Controls.Add(marketAgeLabel, 0, 2);
            otherInfoGrid.Controls.Add(marketAgeValue, 1, 2);
            otherInfoGrid.Dock = DockStyle.Fill;
            otherInfoGrid.Location = new Point(2, 2);
            otherInfoGrid.Margin = new Padding(2);
            otherInfoGrid.Name = "otherInfoGrid";
            otherInfoGrid.RowCount = 3;
            otherInfoGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            otherInfoGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            otherInfoGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            otherInfoGrid.Size = new Size(141, 70);
            otherInfoGrid.TabIndex = 0;
            // 
            // CategoryLabel
            // 
            CategoryLabel.AutoSize = true;
            CategoryLabel.Dock = DockStyle.Fill;
            CategoryLabel.Location = new Point(2, 0);
            CategoryLabel.Margin = new Padding(2, 0, 2, 0);
            CategoryLabel.Name = "CategoryLabel";
            CategoryLabel.Size = new Size(66, 16);
            CategoryLabel.TabIndex = 0;
            CategoryLabel.Text = "Category:";
            CategoryLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // categoryValue
            // 
            categoryValue.AutoSize = true;
            categoryValue.Dock = DockStyle.Fill;
            categoryValue.Location = new Point(72, 0);
            categoryValue.Margin = new Padding(2, 0, 2, 0);
            categoryValue.Name = "categoryValue";
            categoryValue.Size = new Size(67, 16);
            categoryValue.TabIndex = 1;
            categoryValue.Text = "--";
            categoryValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // timeLeftLabel
            // 
            timeLeftLabel.AutoSize = true;
            timeLeftLabel.Dock = DockStyle.Fill;
            timeLeftLabel.Location = new Point(2, 16);
            timeLeftLabel.Margin = new Padding(2, 0, 2, 0);
            timeLeftLabel.Name = "timeLeftLabel";
            timeLeftLabel.Size = new Size(66, 16);
            timeLeftLabel.TabIndex = 2;
            timeLeftLabel.Text = "Time Left:";
            timeLeftLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // timeLeftValue
            // 
            timeLeftValue.AutoSize = true;
            timeLeftValue.Dock = DockStyle.Fill;
            timeLeftValue.Location = new Point(72, 16);
            timeLeftValue.Margin = new Padding(2, 0, 2, 0);
            timeLeftValue.Name = "timeLeftValue";
            timeLeftValue.Size = new Size(67, 16);
            timeLeftValue.TabIndex = 3;
            timeLeftValue.Text = "--";
            timeLeftValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // marketAgeLabel
            // 
            marketAgeLabel.AutoSize = true;
            marketAgeLabel.Dock = DockStyle.Top;
            marketAgeLabel.Location = new Point(2, 32);
            marketAgeLabel.Margin = new Padding(2, 0, 2, 0);
            marketAgeLabel.Name = "marketAgeLabel";
            marketAgeLabel.Size = new Size(66, 30);
            marketAgeLabel.TabIndex = 4;
            marketAgeLabel.Text = "Market Age:";
            marketAgeLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // marketAgeValue
            // 
            marketAgeValue.AutoSize = true;
            marketAgeValue.Dock = DockStyle.Top;
            marketAgeValue.Location = new Point(72, 32);
            marketAgeValue.Margin = new Padding(2, 0, 2, 0);
            marketAgeValue.Name = "marketAgeValue";
            marketAgeValue.Size = new Size(67, 15);
            marketAgeValue.TabIndex = 5;
            marketAgeValue.Text = "--";
            marketAgeValue.TextAlign = ContentAlignment.MiddleRight;
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
            flowMomentumGrid.Location = new Point(3, 77);
            flowMomentumGrid.Name = "flowMomentumGrid";
            flowMomentumGrid.RowCount = 7;
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            flowMomentumGrid.Size = new Size(139, 269);
            flowMomentumGrid.TabIndex = 1;
            // 
            // flowHeader
            // 
            flowHeader.AutoSize = true;
            flowHeader.Dock = DockStyle.Fill;
            flowHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            flowHeader.Location = new Point(3, 0);
            flowHeader.Name = "flowHeader";
            flowHeader.Size = new Size(63, 20);
            flowHeader.TabIndex = 0;
            flowHeader.Text = "Flow/Momentum";
            flowHeader.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // flowHeaderYes
            // 
            flowHeaderYes.AutoSize = true;
            flowHeaderYes.Dock = DockStyle.Fill;
            flowHeaderYes.Location = new Point(72, 0);
            flowHeaderYes.Name = "flowHeaderYes";
            flowHeaderYes.Size = new Size(28, 20);
            flowHeaderYes.TabIndex = 2;
            flowHeaderYes.Text = "Yes";
            flowHeaderYes.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // flowHeaderNo
            // 
            flowHeaderNo.AutoSize = true;
            flowHeaderNo.Dock = DockStyle.Fill;
            flowHeaderNo.Location = new Point(106, 0);
            flowHeaderNo.Name = "flowHeaderNo";
            flowHeaderNo.Size = new Size(30, 20);
            flowHeaderNo.TabIndex = 3;
            flowHeaderNo.Text = "No";
            flowHeaderNo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // topVelocityLabel
            // 
            topVelocityLabel.AutoSize = true;
            topVelocityLabel.Dock = DockStyle.Fill;
            topVelocityLabel.Enabled = false;
            topVelocityLabel.Location = new Point(3, 20);
            topVelocityLabel.Margin = new Padding(3, 0, 3, 0);
            topVelocityLabel.Name = "topVelocityLabel";
            topVelocityLabel.Size = new Size(63, 16);
            topVelocityLabel.TabIndex = 4;
            topVelocityLabel.Text = "Top Velocity:";
            // 
            // topVelocityYesValue
            // 
            topVelocityYesValue.AutoSize = true;
            topVelocityYesValue.Dock = DockStyle.Fill;
            topVelocityYesValue.Location = new Point(72, 20);
            topVelocityYesValue.Name = "topVelocityYesValue";
            topVelocityYesValue.Size = new Size(28, 16);
            topVelocityYesValue.TabIndex = 5;
            topVelocityYesValue.Text = "--";
            topVelocityYesValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // topVelocityNoValue
            // 
            topVelocityNoValue.AutoSize = true;
            topVelocityNoValue.Dock = DockStyle.Fill;
            topVelocityNoValue.Location = new Point(106, 20);
            topVelocityNoValue.Name = "topVelocityNoValue";
            topVelocityNoValue.Size = new Size(30, 16);
            topVelocityNoValue.TabIndex = 6;
            topVelocityNoValue.Text = "--";
            topVelocityNoValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // bottomVelocityLabel
            // 
            bottomVelocityLabel.AutoSize = true;
            bottomVelocityLabel.Dock = DockStyle.Fill;
            bottomVelocityLabel.Enabled = false;
            bottomVelocityLabel.Location = new Point(3, 36);
            bottomVelocityLabel.Margin = new Padding(3, 0, 3, 0);
            bottomVelocityLabel.Name = "bottomVelocityLabel";
            bottomVelocityLabel.Size = new Size(63, 16);
            bottomVelocityLabel.TabIndex = 7;
            bottomVelocityLabel.Text = "Bottom Velocity:";
            // 
            // bottomVelocityYesValue
            // 
            bottomVelocityYesValue.AutoSize = true;
            bottomVelocityYesValue.Dock = DockStyle.Fill;
            bottomVelocityYesValue.Location = new Point(72, 36);
            bottomVelocityYesValue.Name = "bottomVelocityYesValue";
            bottomVelocityYesValue.Size = new Size(28, 16);
            bottomVelocityYesValue.TabIndex = 8;
            bottomVelocityYesValue.Text = "--";
            bottomVelocityYesValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // bottomVelocityNoValue
            // 
            bottomVelocityNoValue.AutoSize = true;
            bottomVelocityNoValue.Dock = DockStyle.Fill;
            bottomVelocityNoValue.Location = new Point(106, 36);
            bottomVelocityNoValue.Name = "bottomVelocityNoValue";
            bottomVelocityNoValue.Size = new Size(30, 16);
            bottomVelocityNoValue.TabIndex = 9;
            bottomVelocityNoValue.Text = "--";
            bottomVelocityNoValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // netOrderRateLabel
            // 
            netOrderRateLabel.AutoSize = true;
            netOrderRateLabel.Dock = DockStyle.Fill;
            netOrderRateLabel.Enabled = false;
            netOrderRateLabel.Location = new Point(3, 52);
            netOrderRateLabel.Margin = new Padding(3, 0, 3, 0);
            netOrderRateLabel.Name = "netOrderRateLabel";
            netOrderRateLabel.Size = new Size(63, 16);
            netOrderRateLabel.TabIndex = 10;
            netOrderRateLabel.Text = "Net Order Rate:";
            // 
            // netOrderRateYesValue
            // 
            netOrderRateYesValue.AutoSize = true;
            netOrderRateYesValue.Dock = DockStyle.Fill;
            netOrderRateYesValue.Location = new Point(72, 52);
            netOrderRateYesValue.Name = "netOrderRateYesValue";
            netOrderRateYesValue.Size = new Size(28, 16);
            netOrderRateYesValue.TabIndex = 11;
            netOrderRateYesValue.Text = "--";
            netOrderRateYesValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // netOrderRateNoValue
            // 
            netOrderRateNoValue.AutoSize = true;
            netOrderRateNoValue.Dock = DockStyle.Fill;
            netOrderRateNoValue.Location = new Point(106, 52);
            netOrderRateNoValue.Name = "netOrderRateNoValue";
            netOrderRateNoValue.Size = new Size(30, 16);
            netOrderRateNoValue.TabIndex = 12;
            netOrderRateNoValue.Text = "--";
            netOrderRateNoValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // tradeVolumeLabel
            // 
            tradeVolumeLabel.AutoSize = true;
            tradeVolumeLabel.Dock = DockStyle.Fill;
            tradeVolumeLabel.Enabled = false;
            tradeVolumeLabel.Location = new Point(3, 68);
            tradeVolumeLabel.Margin = new Padding(3, 0, 3, 0);
            tradeVolumeLabel.Name = "tradeVolumeLabel";
            tradeVolumeLabel.Size = new Size(63, 16);
            tradeVolumeLabel.TabIndex = 13;
            tradeVolumeLabel.Text = "Trade Volume:";
            // 
            // tradeVolumeYesValue
            // 
            tradeVolumeYesValue.AutoSize = true;
            tradeVolumeYesValue.Dock = DockStyle.Fill;
            tradeVolumeYesValue.Location = new Point(72, 68);
            tradeVolumeYesValue.Name = "tradeVolumeYesValue";
            tradeVolumeYesValue.Size = new Size(28, 16);
            tradeVolumeYesValue.TabIndex = 14;
            tradeVolumeYesValue.Text = "--";
            tradeVolumeYesValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // tradeVolumeNoValue
            // 
            tradeVolumeNoValue.AutoSize = true;
            tradeVolumeNoValue.Dock = DockStyle.Fill;
            tradeVolumeNoValue.Location = new Point(106, 68);
            tradeVolumeNoValue.Name = "tradeVolumeNoValue";
            tradeVolumeNoValue.Size = new Size(30, 16);
            tradeVolumeNoValue.TabIndex = 15;
            tradeVolumeNoValue.Text = "--";
            tradeVolumeNoValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // avgTradeSizeLabel
            // 
            avgTradeSizeLabel.AutoSize = true;
            avgTradeSizeLabel.Dock = DockStyle.Fill;
            avgTradeSizeLabel.Enabled = false;
            avgTradeSizeLabel.Location = new Point(3, 84);
            avgTradeSizeLabel.Margin = new Padding(3, 0, 3, 0);
            avgTradeSizeLabel.Name = "avgTradeSizeLabel";
            avgTradeSizeLabel.Size = new Size(63, 16);
            avgTradeSizeLabel.TabIndex = 16;
            avgTradeSizeLabel.Text = "Avg Trade Size:";
            // 
            // avgTradeSizeYesValue
            // 
            avgTradeSizeYesValue.AutoSize = true;
            avgTradeSizeYesValue.Dock = DockStyle.Fill;
            avgTradeSizeYesValue.Location = new Point(72, 84);
            avgTradeSizeYesValue.Name = "avgTradeSizeYesValue";
            avgTradeSizeYesValue.Size = new Size(28, 16);
            avgTradeSizeYesValue.TabIndex = 17;
            avgTradeSizeYesValue.Text = "--";
            avgTradeSizeYesValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // avgTradeSizeNoValue
            // 
            avgTradeSizeNoValue.AutoSize = true;
            avgTradeSizeNoValue.Dock = DockStyle.Fill;
            avgTradeSizeNoValue.Location = new Point(106, 84);
            avgTradeSizeNoValue.Name = "avgTradeSizeNoValue";
            avgTradeSizeNoValue.Size = new Size(30, 16);
            avgTradeSizeNoValue.TabIndex = 18;
            avgTradeSizeNoValue.Text = "--";
            avgTradeSizeNoValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // slopeLabel
            // 
            slopeLabel.AutoSize = true;
            slopeLabel.Dock = DockStyle.Top;
            slopeLabel.Enabled = false;
            slopeLabel.Location = new Point(3, 100);
            slopeLabel.Margin = new Padding(3, 0, 3, 0);
            slopeLabel.Name = "slopeLabel";
            slopeLabel.Size = new Size(63, 19);
            slopeLabel.TabIndex = 19;
            slopeLabel.Text = "Slope:";
            // 
            // slopeYesValue
            // 
            slopeYesValue.AutoSize = true;
            slopeYesValue.Dock = DockStyle.Top;
            slopeYesValue.Location = new Point(72, 100);
            slopeYesValue.Name = "slopeYesValue";
            slopeYesValue.Size = new Size(28, 15);
            slopeYesValue.TabIndex = 20;
            slopeYesValue.Text = "--";
            slopeYesValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // slopeNoValue
            // 
            slopeNoValue.AutoSize = true;
            slopeNoValue.Dock = DockStyle.Top;
            slopeNoValue.Location = new Point(106, 100);
            slopeNoValue.Name = "slopeNoValue";
            slopeNoValue.Size = new Size(30, 15);
            slopeNoValue.TabIndex = 21;
            slopeNoValue.Text = "--";
            slopeNoValue.TextAlign = ContentAlignment.MiddleRight;
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
            contextGrid.Dock = DockStyle.Bottom;
            contextGrid.Location = new Point(3, 355);
            contextGrid.Name = "contextGrid";
            contextGrid.RowCount = 6;
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            contextGrid.Size = new Size(139, 100);
            contextGrid.TabIndex = 2;
            // 
            // contextHeader
            // 
            contextHeader.AutoSize = true;
            contextHeader.Dock = DockStyle.Fill;
            contextHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            contextHeader.Location = new Point(3, 0);
            contextHeader.Name = "contextHeader";
            contextHeader.Size = new Size(63, 20);
            contextHeader.TabIndex = 0;
            contextHeader.Text = "Context";
            contextHeader.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // contextHeaderYes
            // 
            contextHeaderYes.AutoSize = true;
            contextHeaderYes.Dock = DockStyle.Fill;
            contextHeaderYes.Location = new Point(72, 0);
            contextHeaderYes.Name = "contextHeaderYes";
            contextHeaderYes.Size = new Size(28, 20);
            contextHeaderYes.TabIndex = 2;
            contextHeaderYes.Text = "Yes";
            contextHeaderYes.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // contextHeaderNo
            // 
            contextHeaderNo.AutoSize = true;
            contextHeaderNo.Dock = DockStyle.Fill;
            contextHeaderNo.Location = new Point(106, 0);
            contextHeaderNo.Name = "contextHeaderNo";
            contextHeaderNo.Size = new Size(30, 20);
            contextHeaderNo.TabIndex = 3;
            contextHeaderNo.Text = "No";
            contextHeaderNo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // spreadLabel
            // 
            spreadLabel.AutoSize = true;
            spreadLabel.Dock = DockStyle.Fill;
            spreadLabel.Location = new Point(3, 20);
            spreadLabel.Name = "spreadLabel";
            spreadLabel.Size = new Size(63, 16);
            spreadLabel.TabIndex = 4;
            spreadLabel.Text = "Spread:";
            spreadLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // spreadValue
            // 
            spreadValue.AutoSize = true;
            contextGrid.SetColumnSpan(spreadValue, 2);
            spreadValue.Dock = DockStyle.Fill;
            spreadValue.Location = new Point(72, 20);
            spreadValue.Name = "spreadValue";
            spreadValue.Size = new Size(64, 16);
            spreadValue.TabIndex = 5;
            spreadValue.Text = "--";
            spreadValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // imbalLabel
            // 
            imbalLabel.AutoSize = true;
            imbalLabel.Dock = DockStyle.Fill;
            imbalLabel.Enabled = false;
            imbalLabel.Location = new Point(3, 36);
            imbalLabel.Margin = new Padding(3, 0, 3, 0);
            imbalLabel.Name = "imbalLabel";
            imbalLabel.Size = new Size(63, 16);
            imbalLabel.TabIndex = 6;
            imbalLabel.Text = "Imbalance:";
            // 
            // imbalValue
            // 
            imbalValue.AutoSize = true;
            contextGrid.SetColumnSpan(imbalValue, 2);
            imbalValue.Dock = DockStyle.Fill;
            imbalValue.Location = new Point(72, 36);
            imbalValue.Name = "imbalValue";
            imbalValue.Size = new Size(64, 16);
            imbalValue.TabIndex = 7;
            imbalValue.Text = "--";
            imbalValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // depthTop4Label
            // 
            depthTop4Label.AutoSize = true;
            depthTop4Label.Dock = DockStyle.Fill;
            depthTop4Label.Enabled = false;
            depthTop4Label.Location = new Point(3, 52);
            depthTop4Label.Margin = new Padding(3, 0, 3, 0);
            depthTop4Label.Name = "depthTop4Label";
            depthTop4Label.Size = new Size(63, 16);
            depthTop4Label.TabIndex = 8;
            depthTop4Label.Text = "Depth Top 4:";
            // 
            // depthTop4YesValue
            // 
            depthTop4YesValue.AutoSize = true;
            depthTop4YesValue.Dock = DockStyle.Fill;
            depthTop4YesValue.Location = new Point(72, 52);
            depthTop4YesValue.Name = "depthTop4YesValue";
            depthTop4YesValue.Size = new Size(28, 16);
            depthTop4YesValue.TabIndex = 9;
            depthTop4YesValue.Text = "--";
            depthTop4YesValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // depthTop4NoValue
            // 
            depthTop4NoValue.AutoSize = true;
            depthTop4NoValue.Dock = DockStyle.Fill;
            depthTop4NoValue.Location = new Point(106, 52);
            depthTop4NoValue.Name = "depthTop4NoValue";
            depthTop4NoValue.Size = new Size(30, 16);
            depthTop4NoValue.TabIndex = 10;
            depthTop4NoValue.Text = "--";
            depthTop4NoValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // centerMassLabel
            // 
            centerMassLabel.AutoSize = true;
            centerMassLabel.Dock = DockStyle.Fill;
            centerMassLabel.Enabled = false;
            centerMassLabel.Location = new Point(3, 68);
            centerMassLabel.Margin = new Padding(3, 0, 3, 0);
            centerMassLabel.Name = "centerMassLabel";
            centerMassLabel.Size = new Size(63, 16);
            centerMassLabel.TabIndex = 11;
            centerMassLabel.Text = "Center Mass:";
            // 
            // centerMassYesValue
            // 
            centerMassYesValue.AutoSize = true;
            centerMassYesValue.Dock = DockStyle.Fill;
            centerMassYesValue.Location = new Point(72, 68);
            centerMassYesValue.Name = "centerMassYesValue";
            centerMassYesValue.Size = new Size(28, 16);
            centerMassYesValue.TabIndex = 12;
            centerMassYesValue.Text = "--";
            centerMassYesValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // centerMassNoValue
            // 
            centerMassNoValue.AutoSize = true;
            centerMassNoValue.Dock = DockStyle.Fill;
            centerMassNoValue.Location = new Point(106, 68);
            centerMassNoValue.Name = "centerMassNoValue";
            centerMassNoValue.Size = new Size(30, 16);
            centerMassNoValue.TabIndex = 13;
            centerMassNoValue.Text = "--";
            centerMassNoValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // totalContractsLabel
            // 
            totalContractsLabel.AutoSize = true;
            totalContractsLabel.Dock = DockStyle.Top;
            totalContractsLabel.Enabled = false;
            totalContractsLabel.Location = new Point(3, 84);
            totalContractsLabel.Margin = new Padding(3, 0, 3, 0);
            totalContractsLabel.Name = "totalContractsLabel";
            totalContractsLabel.Size = new Size(63, 16);
            totalContractsLabel.TabIndex = 14;
            totalContractsLabel.Text = "Total Contracts:";
            // 
            // totalContractsYesValue
            // 
            totalContractsYesValue.AutoSize = true;
            totalContractsYesValue.Dock = DockStyle.Top;
            totalContractsYesValue.Location = new Point(72, 84);
            totalContractsYesValue.Name = "totalContractsYesValue";
            totalContractsYesValue.Size = new Size(28, 15);
            totalContractsYesValue.TabIndex = 15;
            totalContractsYesValue.Text = "--";
            totalContractsYesValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // totalContractsNoValue
            // 
            totalContractsNoValue.AutoSize = true;
            totalContractsNoValue.Dock = DockStyle.Top;
            totalContractsNoValue.Location = new Point(106, 84);
            totalContractsNoValue.Name = "totalContractsNoValue";
            totalContractsNoValue.Size = new Size(30, 15);
            totalContractsNoValue.TabIndex = 16;
            totalContractsNoValue.Text = "--";
            totalContractsNoValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // positionsContainer
            // 
            positionsContainer.BorderStyle = BorderStyle.FixedSingle;
            positionsContainer.Controls.Add(positionsLayout);
            positionsContainer.Dock = DockStyle.Fill;
            positionsContainer.Location = new Point(4, 487);
            positionsContainer.Margin = new Padding(4, 3, 4, 3);
            positionsContainer.Name = "positionsContainer";
            positionsContainer.Padding = new Padding(6);
            positionsContainer.Size = new Size(593, 156);
            positionsContainer.TabIndex = 2;
            // 
            // positionsLayout
            // 
            positionsLayout.ColumnCount = 2;
            positionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            positionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            positionsLayout.Controls.Add(positionTextBox, 0, 0);
            positionsLayout.Controls.Add(positionsGrid, 1, 0);
            positionsLayout.Dock = DockStyle.Fill;
            positionsLayout.Location = new Point(6, 6);
            positionsLayout.Name = "positionsLayout";
            positionsLayout.RowCount = 1;
            positionsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            positionsLayout.Size = new Size(579, 142);
            positionsLayout.TabIndex = 0;
            // 
            // positionTextBox
            // 
            positionTextBox.Dock = DockStyle.Fill;
            positionTextBox.Location = new Point(3, 3);
            positionTextBox.Multiline = true;
            positionTextBox.Name = "positionTextBox";
            positionTextBox.ReadOnly = true;
            positionTextBox.ScrollBars = ScrollBars.Vertical;
            positionTextBox.Size = new Size(283, 136);
            positionTextBox.TabIndex = 0;
            // 
            // positionsGrid
            // 
            positionsGrid.AutoSize = true;
            positionsGrid.ColumnCount = 2;
            positionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            positionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
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
            positionsGrid.Location = new Point(292, 3);
            positionsGrid.Name = "positionsGrid";
            positionsGrid.RowCount = 8;
            positionsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            positionsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            positionsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            positionsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            positionsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            positionsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            positionsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            positionsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            positionsGrid.Size = new Size(284, 136);
            positionsGrid.TabIndex = 1;
            // 
            // positionSizeLabel
            // 
            positionSizeLabel.AutoSize = true;
            positionSizeLabel.Dock = DockStyle.Fill;
            positionSizeLabel.Enabled = false;
            positionSizeLabel.Location = new Point(3, 0);
            positionSizeLabel.Margin = new Padding(3, 0, 3, 0);
            positionSizeLabel.Name = "positionSizeLabel";
            positionSizeLabel.Size = new Size(136, 16);
            positionSizeLabel.TabIndex = 0;
            positionSizeLabel.Text = "Position Size:";
            // 
            // positionSizeValue
            // 
            positionSizeValue.AutoSize = true;
            positionSizeValue.Dock = DockStyle.Fill;
            positionSizeValue.Location = new Point(145, 0);
            positionSizeValue.Name = "positionSizeValue";
            positionSizeValue.Size = new Size(136, 16);
            positionSizeValue.TabIndex = 1;
            positionSizeValue.Text = "--";
            positionSizeValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lastTradeLabel
            // 
            lastTradeLabel.AutoSize = true;
            lastTradeLabel.Dock = DockStyle.Fill;
            lastTradeLabel.Location = new Point(3, 16);
            lastTradeLabel.Name = "lastTradeLabel";
            lastTradeLabel.Size = new Size(136, 16);
            lastTradeLabel.TabIndex = 2;
            lastTradeLabel.Text = "Last Trade:";
            lastTradeLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lastTradeValue
            // 
            lastTradeValue.AutoSize = true;
            lastTradeValue.Dock = DockStyle.Fill;
            lastTradeValue.Location = new Point(145, 16);
            lastTradeValue.Name = "lastTradeValue";
            lastTradeValue.Size = new Size(136, 16);
            lastTradeValue.TabIndex = 3;
            lastTradeValue.Text = "--";
            lastTradeValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // positionRoiLabel
            // 
            positionRoiLabel.AutoSize = true;
            positionRoiLabel.Dock = DockStyle.Fill;
            positionRoiLabel.Enabled = false;
            positionRoiLabel.Location = new Point(3, 32);
            positionRoiLabel.Margin = new Padding(3, 0, 3, 0);
            positionRoiLabel.Name = "positionRoiLabel";
            positionRoiLabel.Size = new Size(136, 16);
            positionRoiLabel.TabIndex = 4;
            positionRoiLabel.Text = "Position ROI:";
            // 
            // positionRoiValue
            // 
            positionRoiValue.AutoSize = true;
            positionRoiValue.Dock = DockStyle.Fill;
            positionRoiValue.Location = new Point(145, 32);
            positionRoiValue.Name = "positionRoiValue";
            positionRoiValue.Size = new Size(136, 16);
            positionRoiValue.TabIndex = 5;
            positionRoiValue.Text = "--";
            positionRoiValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // buyinPriceLabel
            // 
            buyinPriceLabel.AutoSize = true;
            buyinPriceLabel.Dock = DockStyle.Fill;
            buyinPriceLabel.Location = new Point(3, 48);
            buyinPriceLabel.Name = "buyinPriceLabel";
            buyinPriceLabel.Size = new Size(136, 16);
            buyinPriceLabel.TabIndex = 6;
            buyinPriceLabel.Text = "Buy-in Price:";
            buyinPriceLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // buyinPriceValue
            // 
            buyinPriceValue.AutoSize = true;
            buyinPriceValue.Dock = DockStyle.Fill;
            buyinPriceValue.Location = new Point(145, 48);
            buyinPriceValue.Name = "buyinPriceValue";
            buyinPriceValue.Size = new Size(136, 16);
            buyinPriceValue.TabIndex = 7;
            buyinPriceValue.Text = "--";
            buyinPriceValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // positionUpsideLabel
            // 
            positionUpsideLabel.AutoSize = true;
            positionUpsideLabel.Dock = DockStyle.Fill;
            positionUpsideLabel.Location = new Point(3, 64);
            positionUpsideLabel.Name = "positionUpsideLabel";
            positionUpsideLabel.Size = new Size(136, 16);
            positionUpsideLabel.TabIndex = 8;
            positionUpsideLabel.Text = "Position Upside:";
            positionUpsideLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // positionUpsideValue
            // 
            positionUpsideValue.AutoSize = true;
            positionUpsideValue.Dock = DockStyle.Fill;
            positionUpsideValue.Location = new Point(145, 64);
            positionUpsideValue.Name = "positionUpsideValue";
            positionUpsideValue.Size = new Size(136, 16);
            positionUpsideValue.TabIndex = 9;
            positionUpsideValue.Text = "--";
            positionUpsideValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // positionDownsideLabel
            // 
            positionDownsideLabel.AutoSize = true;
            positionDownsideLabel.Dock = DockStyle.Fill;
            positionDownsideLabel.Location = new Point(3, 80);
            positionDownsideLabel.Name = "positionDownsideLabel";
            positionDownsideLabel.Size = new Size(136, 16);
            positionDownsideLabel.TabIndex = 10;
            positionDownsideLabel.Text = "Position Downside:";
            positionDownsideLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // positionDownsideValue
            // 
            positionDownsideValue.AutoSize = true;
            positionDownsideValue.Dock = DockStyle.Fill;
            positionDownsideValue.Location = new Point(145, 80);
            positionDownsideValue.Name = "positionDownsideValue";
            positionDownsideValue.Size = new Size(136, 16);
            positionDownsideValue.TabIndex = 11;
            positionDownsideValue.Text = "--";
            positionDownsideValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // restingOrdersLabel
            // 
            restingOrdersLabel.AutoSize = true;
            restingOrdersLabel.Dock = DockStyle.Fill;
            restingOrdersLabel.Enabled = false;
            restingOrdersLabel.Location = new Point(3, 96);
            restingOrdersLabel.Margin = new Padding(3, 0, 3, 0);
            restingOrdersLabel.Name = "restingOrdersLabel";
            restingOrdersLabel.Size = new Size(136, 16);
            restingOrdersLabel.TabIndex = 12;
            restingOrdersLabel.Text = "Resting Orders:";
            // 
            // restingOrdersValue
            // 
            restingOrdersValue.AutoSize = true;
            restingOrdersValue.Dock = DockStyle.Fill;
            restingOrdersValue.Location = new Point(145, 96);
            restingOrdersValue.Name = "restingOrdersValue";
            restingOrdersValue.Size = new Size(136, 16);
            restingOrdersValue.TabIndex = 13;
            restingOrdersValue.Text = "--";
            restingOrdersValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // simulatedPositionLabel
            // 
            simulatedPositionLabel.AutoSize = true;
            simulatedPositionLabel.Dock = DockStyle.Top;
            simulatedPositionLabel.Enabled = false;
            simulatedPositionLabel.Location = new Point(3, 112);
            simulatedPositionLabel.Margin = new Padding(3, 0, 3, 0);
            simulatedPositionLabel.Name = "simulatedPositionLabel";
            simulatedPositionLabel.Size = new Size(136, 19);
            simulatedPositionLabel.TabIndex = 14;
            simulatedPositionLabel.Text = "Simulated Position:";
            // 
            // simulatedPositionValue
            // 
            simulatedPositionValue.AutoSize = true;
            simulatedPositionValue.Dock = DockStyle.Top;
            simulatedPositionValue.Location = new Point(145, 112);
            simulatedPositionValue.Name = "simulatedPositionValue";
            simulatedPositionValue.Size = new Size(136, 15);
            simulatedPositionValue.TabIndex = 15;
            simulatedPositionValue.Text = "--";
            simulatedPositionValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // orderbookContainer
            // 
            orderbookContainer.BorderStyle = BorderStyle.FixedSingle;
            orderbookContainer.Controls.Add(orderbookGrid);
            orderbookContainer.Dock = DockStyle.Fill;
            orderbookContainer.Location = new Point(605, 487);
            orderbookContainer.Margin = new Padding(4, 3, 4, 3);
            orderbookContainer.Name = "orderbookContainer";
            orderbookContainer.Padding = new Padding(6);
            orderbookContainer.Size = new Size(316, 156);
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
            orderbookGrid.Size = new Size(302, 142);
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
            backButton.Location = new Point(4, 655);
            backButton.Margin = new Padding(4, 3, 4, 3);
            backButton.Name = "backButton";
            backButton.Size = new Size(925, 34);
            backButton.TabIndex = 1;
            backButton.Text = "Back to Full Chart";
            backButton.UseVisualStyleBackColor = true;
            // 
            // SnapshotViewer
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(mainLayout);
            Margin = new Padding(4, 3, 4, 3);
            Name = "SnapshotViewer";
            Size = new Size(933, 692);
            mainLayout.ResumeLayout(false);
            dashboardGrid.ResumeLayout(false);
            chartContainer.ResumeLayout(false);
            chartContainer.PerformLayout();
            chartLayout.ResumeLayout(false);
            chartLayout.PerformLayout();
            chartControls.ResumeLayout(false);
            chartControls.PerformLayout();
            marketInfoContainer.ResumeLayout(false);
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