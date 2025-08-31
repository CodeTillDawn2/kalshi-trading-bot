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
            rsiLabel = new Label();
            rsiValue = new Label();
            macdLabel = new Label();
            macdValue = new Label();
            emaLabel = new Label();
            emaValue = new Label();
            bollingerLabel = new Label();
            bollingerValue = new Label();
            atrLabel = new Label();
            atrValue = new Label();
            vwapLabel = new Label();
            vwapValue = new Label();
            stochasticLabel = new Label();
            stochasticValue = new Label();
            obvLabel = new Label();
            obvValue = new Label();
            rightColumn = new TableLayoutPanel();
            otherInfoGrid = new TableLayoutPanel();
            titleLabel = new Label();
            titleValue = new Label();
            subtitleLabel = new Label();
            subtitleValue = new Label();
            marketTypeLabel = new Label();
            marketTypeValue = new Label();
            priceGoodBadLabel = new Label();
            priceGoodBadValue = new Label();
            marketBehaviorLabel = new Label();
            marketBehaviorValue = new Label();
            timeLeftLabel = new Label();
            timeLeftValue = new Label();
            marketAgeLabel = new Label();
            marketAgeValue = new Label();
            flowMomentumGrid = new TableLayoutPanel();
            flowHeader = new Label();
            flowHeaderEmpty = new Label();
            flowHeaderYes = new Label();
            flowHeaderNo = new Label();
            topVelocityLabel = new Label();
            topVelocityYesValue = new Label();
            topVelocityNoValue = new Label();
            bottomVelocityLabel = new Label();
            bottomVelocityYesValue = new Label();
            bottomVelocityNoValue = new Label();
            netOrderRateLabel = new Label();
            netOrderRateYesValue = new Label();
            netOrderRateNoValue = new Label();
            tradeVolumeLabel = new Label();
            tradeVolumeYesValue = new Label();
            tradeVolumeNoValue = new Label();
            avgTradeSizeLabel = new Label();
            avgTradeSizeYesValue = new Label();
            avgTradeSizeNoValue = new Label();
            contextGrid = new TableLayoutPanel();
            contextHeader = new Label();
            contextHeaderEmpty = new Label();
            contextHeaderYes = new Label();
            contextHeaderNo = new Label();
            spreadLabel = new Label();
            spreadValue = new Label();
            imbalLabel = new Label();
            imbalValue = new Label();
            depthTop4Label = new Label();
            depthTop4YesValue = new Label();
            depthTop4NoValue = new Label();
            centerMassLabel = new Label();
            centerMassYesValue = new Label();
            centerMassNoValue = new Label();
            totalContractsLabel = new Label();
            totalContractsYesValue = new Label();
            totalContractsNoValue = new Label();
            positionsContainer = new Panel();
            positionsGrid = new TableLayoutPanel();
            positionSizeLabel = new Label();
            positionSizeValue = new Label();
            lastTradeLabel = new Label();
            lastTradeValue = new Label();
            positionRoiLabel = new Label();
            positionRoiValue = new Label();
            buyinPriceLabel = new Label();
            buyinPriceValue = new Label();
            positionUpsideLabel = new Label();
            positionUpsideValue = new Label();
            positionDownsideLabel = new Label();
            positionDownsideValue = new Label();
            restingOrdersLabel = new Label();
            restingOrdersValue = new Label();
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
            dashboardGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 87F));
            dashboardGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 13F));
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
            chartContainer.Size = new Size(593, 556);
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
            chartLayout.Size = new Size(579, 542);
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
            priceChart.Size = new Size(571, 516);
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
            marketInfoContainer.Size = new Size(316, 556);
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
            infoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            infoGrid.Size = new Size(302, 542);
            infoGrid.TabIndex = 0;
            // 
            // leftColumn
            // 
            leftColumn.ColumnCount = 1;
            leftColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftColumn.Controls.Add(pricesGrid, 0, 0);
            leftColumn.Controls.Add(tradingMetricsGrid, 0, 1);
            leftColumn.Dock = DockStyle.Fill;
            leftColumn.Location = new Point(4, 3);
            leftColumn.Margin = new Padding(4, 3, 4, 3);
            leftColumn.Name = "leftColumn";
            leftColumn.RowCount = 2;
            leftColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            leftColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            leftColumn.Size = new Size(143, 536);
            leftColumn.TabIndex = 0;
            // 
            // pricesGrid
            // 
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
            pricesGrid.Location = new Point(4, 3);
            pricesGrid.Margin = new Padding(4, 3, 4, 3);
            pricesGrid.Name = "pricesGrid";
            pricesGrid.RowCount = 6;
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.67F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.67F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.67F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.67F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.67F));
            pricesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.67F));
            pricesGrid.Size = new Size(135, 208);
            pricesGrid.TabIndex = 0;
            // 
            // pricesHeaderEmpty
            // 
            pricesHeaderEmpty.AutoSize = true;
            pricesHeaderEmpty.Dock = DockStyle.Fill;
            pricesHeaderEmpty.Location = new Point(4, 3);
            pricesHeaderEmpty.Margin = new Padding(4, 3, 4, 3);
            pricesHeaderEmpty.Name = "pricesHeaderEmpty";
            pricesHeaderEmpty.Size = new Size(46, 28);
            pricesHeaderEmpty.TabIndex = 15;
            // 
            // pricesHeaderNo
            // 
            pricesHeaderNo.AutoSize = true;
            pricesHeaderNo.Dock = DockStyle.Fill;
            pricesHeaderNo.Location = new Point(58, 3);
            pricesHeaderNo.Margin = new Padding(4, 3, 4, 3);
            pricesHeaderNo.Name = "pricesHeaderNo";
            pricesHeaderNo.Size = new Size(32, 28);
            pricesHeaderNo.TabIndex = 16;
            pricesHeaderNo.Text = "No";
            pricesHeaderNo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pricesHeaderYes
            // 
            pricesHeaderYes.AutoSize = true;
            pricesHeaderYes.Dock = DockStyle.Fill;
            pricesHeaderYes.Location = new Point(98, 3);
            pricesHeaderYes.Margin = new Padding(4, 3, 4, 3);
            pricesHeaderYes.Name = "pricesHeaderYes";
            pricesHeaderYes.Size = new Size(33, 28);
            pricesHeaderYes.TabIndex = 17;
            pricesHeaderYes.Text = "Yes";
            pricesHeaderYes.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // allTimeHighLabel
            // 
            allTimeHighLabel.AutoSize = true;
            allTimeHighLabel.Dock = DockStyle.Fill;
            allTimeHighLabel.Location = new Point(4, 37);
            allTimeHighLabel.Margin = new Padding(4, 3, 4, 3);
            allTimeHighLabel.Name = "allTimeHighLabel";
            allTimeHighLabel.Size = new Size(46, 28);
            allTimeHighLabel.TabIndex = 0;
            allTimeHighLabel.Text = "All Time High";
            allTimeHighLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // allTimeHighAsk
            // 
            allTimeHighAsk.Controls.Add(allTimeHighAskPrice);
            allTimeHighAsk.Controls.Add(allTimeHighAskTime);
            allTimeHighAsk.Dock = DockStyle.Fill;
            allTimeHighAsk.Location = new Point(58, 37);
            allTimeHighAsk.Margin = new Padding(4, 3, 4, 3);
            allTimeHighAsk.Name = "allTimeHighAsk";
            allTimeHighAsk.Size = new Size(32, 28);
            allTimeHighAsk.TabIndex = 1;
            // 
            // allTimeHighAskPrice
            // 
            allTimeHighAskPrice.AutoSize = true;
            allTimeHighAskPrice.Dock = DockStyle.Top;
            allTimeHighAskPrice.Location = new Point(0, 0);
            allTimeHighAskPrice.Margin = new Padding(0);
            allTimeHighAskPrice.Name = "allTimeHighAskPrice";
            allTimeHighAskPrice.Size = new Size(17, 15);
            allTimeHighAskPrice.TabIndex = 0;
            allTimeHighAskPrice.Text = "--";
            allTimeHighAskPrice.TextAlign = ContentAlignment.MiddleRight;
            // 
            // allTimeHighAskTime
            // 
            allTimeHighAskTime.AutoSize = true;
            allTimeHighAskTime.Dock = DockStyle.Bottom;
            allTimeHighAskTime.Location = new Point(0, 13);
            allTimeHighAskTime.Margin = new Padding(0);
            allTimeHighAskTime.Name = "allTimeHighAskTime";
            allTimeHighAskTime.Size = new Size(17, 15);
            allTimeHighAskTime.TabIndex = 1;
            allTimeHighAskTime.Text = "--";
            allTimeHighAskTime.TextAlign = ContentAlignment.MiddleRight;
            // 
            // allTimeHighBid
            // 
            allTimeHighBid.Controls.Add(allTimeHighBidPrice);
            allTimeHighBid.Controls.Add(allTimeHighBidTime);
            allTimeHighBid.Dock = DockStyle.Fill;
            allTimeHighBid.Location = new Point(98, 37);
            allTimeHighBid.Margin = new Padding(4, 3, 4, 3);
            allTimeHighBid.Name = "allTimeHighBid";
            allTimeHighBid.Size = new Size(33, 28);
            allTimeHighBid.TabIndex = 2;
            // 
            // allTimeHighBidPrice
            // 
            allTimeHighBidPrice.AutoSize = true;
            allTimeHighBidPrice.Dock = DockStyle.Top;
            allTimeHighBidPrice.Location = new Point(0, 0);
            allTimeHighBidPrice.Margin = new Padding(0);
            allTimeHighBidPrice.Name = "allTimeHighBidPrice";
            allTimeHighBidPrice.Size = new Size(17, 15);
            allTimeHighBidPrice.TabIndex = 0;
            allTimeHighBidPrice.Text = "--";
            allTimeHighBidPrice.TextAlign = ContentAlignment.MiddleRight;
            // 
            // allTimeHighBidTime
            // 
            allTimeHighBidTime.AutoSize = true;
            allTimeHighBidTime.Dock = DockStyle.Bottom;
            allTimeHighBidTime.Location = new Point(0, 13);
            allTimeHighBidTime.Margin = new Padding(0);
            allTimeHighBidTime.Name = "allTimeHighBidTime";
            allTimeHighBidTime.Size = new Size(17, 15);
            allTimeHighBidTime.TabIndex = 1;
            allTimeHighBidTime.Text = "--";
            allTimeHighBidTime.TextAlign = ContentAlignment.MiddleRight;
            // 
            // recentHighLabel
            // 
            recentHighLabel.AutoSize = true;
            recentHighLabel.Dock = DockStyle.Fill;
            recentHighLabel.Location = new Point(4, 71);
            recentHighLabel.Margin = new Padding(4, 3, 4, 3);
            recentHighLabel.Name = "recentHighLabel";
            recentHighLabel.Size = new Size(46, 28);
            recentHighLabel.TabIndex = 3;
            recentHighLabel.Text = "Recent High";
            recentHighLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // recentHighAsk
            // 
            recentHighAsk.Controls.Add(recentHighAskPrice);
            recentHighAsk.Controls.Add(recentHighAskTime);
            recentHighAsk.Dock = DockStyle.Fill;
            recentHighAsk.Location = new Point(58, 71);
            recentHighAsk.Margin = new Padding(4, 3, 4, 3);
            recentHighAsk.Name = "recentHighAsk";
            recentHighAsk.Size = new Size(32, 28);
            recentHighAsk.TabIndex = 4;
            // 
            // recentHighAskPrice
            // 
            recentHighAskPrice.AutoSize = true;
            recentHighAskPrice.Dock = DockStyle.Top;
            recentHighAskPrice.Location = new Point(0, 0);
            recentHighAskPrice.Margin = new Padding(0);
            recentHighAskPrice.Name = "recentHighAskPrice";
            recentHighAskPrice.Size = new Size(17, 15);
            recentHighAskPrice.TabIndex = 0;
            recentHighAskPrice.Text = "--";
            recentHighAskPrice.TextAlign = ContentAlignment.MiddleRight;
            // 
            // recentHighAskTime
            // 
            recentHighAskTime.AutoSize = true;
            recentHighAskTime.Dock = DockStyle.Bottom;
            recentHighAskTime.Location = new Point(0, 13);
            recentHighAskTime.Margin = new Padding(0);
            recentHighAskTime.Name = "recentHighAskTime";
            recentHighAskTime.Size = new Size(17, 15);
            recentHighAskTime.TabIndex = 1;
            recentHighAskTime.Text = "--";
            recentHighAskTime.TextAlign = ContentAlignment.MiddleRight;
            // 
            // recentHighBid
            // 
            recentHighBid.Controls.Add(recentHighBidPrice);
            recentHighBid.Controls.Add(recentHighBidTime);
            recentHighBid.Dock = DockStyle.Fill;
            recentHighBid.Location = new Point(98, 71);
            recentHighBid.Margin = new Padding(4, 3, 4, 3);
            recentHighBid.Name = "recentHighBid";
            recentHighBid.Size = new Size(33, 28);
            recentHighBid.TabIndex = 5;
            // 
            // recentHighBidPrice
            // 
            recentHighBidPrice.AutoSize = true;
            recentHighBidPrice.Dock = DockStyle.Top;
            recentHighBidPrice.Location = new Point(0, 0);
            recentHighBidPrice.Margin = new Padding(0);
            recentHighBidPrice.Name = "recentHighBidPrice";
            recentHighBidPrice.Size = new Size(17, 15);
            recentHighBidPrice.TabIndex = 0;
            recentHighBidPrice.Text = "--";
            recentHighBidPrice.TextAlign = ContentAlignment.MiddleRight;
            // 
            // recentHighBidTime
            // 
            recentHighBidTime.AutoSize = true;
            recentHighBidTime.Dock = DockStyle.Bottom;
            recentHighBidTime.Location = new Point(0, 13);
            recentHighBidTime.Margin = new Padding(0);
            recentHighBidTime.Name = "recentHighBidTime";
            recentHighBidTime.Size = new Size(17, 15);
            recentHighBidTime.TabIndex = 1;
            recentHighBidTime.Text = "--";
            recentHighBidTime.TextAlign = ContentAlignment.MiddleRight;
            // 
            // currentPriceLabel
            // 
            currentPriceLabel.AutoSize = true;
            currentPriceLabel.Dock = DockStyle.Fill;
            currentPriceLabel.Location = new Point(4, 105);
            currentPriceLabel.Margin = new Padding(4, 3, 4, 3);
            currentPriceLabel.Name = "currentPriceLabel";
            currentPriceLabel.Size = new Size(46, 28);
            currentPriceLabel.TabIndex = 6;
            currentPriceLabel.Text = "Current Price";
            currentPriceLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // currentPriceAsk
            // 
            currentPriceAsk.AutoSize = true;
            currentPriceAsk.Dock = DockStyle.Fill;
            currentPriceAsk.Location = new Point(58, 105);
            currentPriceAsk.Margin = new Padding(4, 3, 4, 3);
            currentPriceAsk.Name = "currentPriceAsk";
            currentPriceAsk.Size = new Size(32, 28);
            currentPriceAsk.TabIndex = 7;
            currentPriceAsk.Text = "--";
            currentPriceAsk.TextAlign = ContentAlignment.MiddleRight;
            // 
            // currentPriceBid
            // 
            currentPriceBid.AutoSize = true;
            currentPriceBid.Dock = DockStyle.Fill;
            currentPriceBid.Location = new Point(98, 105);
            currentPriceBid.Margin = new Padding(4, 3, 4, 3);
            currentPriceBid.Name = "currentPriceBid";
            currentPriceBid.Size = new Size(33, 28);
            currentPriceBid.TabIndex = 8;
            currentPriceBid.Text = "--";
            currentPriceBid.TextAlign = ContentAlignment.MiddleRight;
            // 
            // recentLowLabel
            // 
            recentLowLabel.AutoSize = true;
            recentLowLabel.Dock = DockStyle.Fill;
            recentLowLabel.Location = new Point(4, 139);
            recentLowLabel.Margin = new Padding(4, 3, 4, 3);
            recentLowLabel.Name = "recentLowLabel";
            recentLowLabel.Size = new Size(46, 28);
            recentLowLabel.TabIndex = 9;
            recentLowLabel.Text = "Recent Low";
            recentLowLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // recentLowAsk
            // 
            recentLowAsk.Controls.Add(recentLowAskPrice);
            recentLowAsk.Controls.Add(recentLowAskTime);
            recentLowAsk.Dock = DockStyle.Fill;
            recentLowAsk.Location = new Point(58, 139);
            recentLowAsk.Margin = new Padding(4, 3, 4, 3);
            recentLowAsk.Name = "recentLowAsk";
            recentLowAsk.Size = new Size(32, 28);
            recentLowAsk.TabIndex = 10;
            // 
            // recentLowAskPrice
            // 
            recentLowAskPrice.AutoSize = true;
            recentLowAskPrice.Dock = DockStyle.Top;
            recentLowAskPrice.Location = new Point(0, 0);
            recentLowAskPrice.Margin = new Padding(0);
            recentLowAskPrice.Name = "recentLowAskPrice";
            recentLowAskPrice.Size = new Size(17, 15);
            recentLowAskPrice.TabIndex = 0;
            recentLowAskPrice.Text = "--";
            recentLowAskPrice.TextAlign = ContentAlignment.MiddleRight;
            // 
            // recentLowAskTime
            // 
            recentLowAskTime.AutoSize = true;
            recentLowAskTime.Dock = DockStyle.Bottom;
            recentLowAskTime.Location = new Point(0, 13);
            recentLowAskTime.Margin = new Padding(0);
            recentLowAskTime.Name = "recentLowAskTime";
            recentLowAskTime.Size = new Size(17, 15);
            recentLowAskTime.TabIndex = 1;
            recentLowAskTime.Text = "--";
            recentLowAskTime.TextAlign = ContentAlignment.MiddleRight;
            // 
            // recentLowBid
            // 
            recentLowBid.Controls.Add(recentLowBidPrice);
            recentLowBid.Controls.Add(recentLowBidTime);
            recentLowBid.Dock = DockStyle.Fill;
            recentLowBid.Location = new Point(98, 139);
            recentLowBid.Margin = new Padding(4, 3, 4, 3);
            recentLowBid.Name = "recentLowBid";
            recentLowBid.Size = new Size(33, 28);
            recentLowBid.TabIndex = 11;
            // 
            // recentLowBidPrice
            // 
            recentLowBidPrice.AutoSize = true;
            recentLowBidPrice.Dock = DockStyle.Top;
            recentLowBidPrice.Location = new Point(0, 0);
            recentLowBidPrice.Margin = new Padding(0);
            recentLowBidPrice.Name = "recentLowBidPrice";
            recentLowBidPrice.Size = new Size(17, 15);
            recentLowBidPrice.TabIndex = 0;
            recentLowBidPrice.Text = "--";
            recentLowBidPrice.TextAlign = ContentAlignment.MiddleRight;
            // 
            // recentLowBidTime
            // 
            recentLowBidTime.AutoSize = true;
            recentLowBidTime.Dock = DockStyle.Bottom;
            recentLowBidTime.Location = new Point(0, 13);
            recentLowBidTime.Margin = new Padding(0);
            recentLowBidTime.Name = "recentLowBidTime";
            recentLowBidTime.Size = new Size(17, 15);
            recentLowBidTime.TabIndex = 1;
            recentLowBidTime.Text = "--";
            recentLowBidTime.TextAlign = ContentAlignment.MiddleRight;
            // 
            // allTimeLowLabel
            // 
            allTimeLowLabel.AutoSize = true;
            allTimeLowLabel.Dock = DockStyle.Fill;
            allTimeLowLabel.Location = new Point(4, 173);
            allTimeLowLabel.Margin = new Padding(4, 3, 4, 3);
            allTimeLowLabel.Name = "allTimeLowLabel";
            allTimeLowLabel.Size = new Size(46, 32);
            allTimeLowLabel.TabIndex = 12;
            allTimeLowLabel.Text = "All Time Low";
            allTimeLowLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // allTimeLowAsk
            // 
            allTimeLowAsk.Controls.Add(allTimeLowAskPrice);
            allTimeLowAsk.Controls.Add(allTimeLowAskTime);
            allTimeLowAsk.Dock = DockStyle.Fill;
            allTimeLowAsk.Location = new Point(58, 173);
            allTimeLowAsk.Margin = new Padding(4, 3, 4, 3);
            allTimeLowAsk.Name = "allTimeLowAsk";
            allTimeLowAsk.Size = new Size(32, 32);
            allTimeLowAsk.TabIndex = 13;
            // 
            // allTimeLowAskPrice
            // 
            allTimeLowAskPrice.AutoSize = true;
            allTimeLowAskPrice.Dock = DockStyle.Top;
            allTimeLowAskPrice.Location = new Point(0, 0);
            allTimeLowAskPrice.Margin = new Padding(0);
            allTimeLowAskPrice.Name = "allTimeLowAskPrice";
            allTimeLowAskPrice.Size = new Size(17, 15);
            allTimeLowAskPrice.TabIndex = 0;
            allTimeLowAskPrice.Text = "--";
            allTimeLowAskPrice.TextAlign = ContentAlignment.MiddleRight;
            // 
            // allTimeLowAskTime
            // 
            allTimeLowAskTime.AutoSize = true;
            allTimeLowAskTime.Dock = DockStyle.Bottom;
            allTimeLowAskTime.Location = new Point(0, 17);
            allTimeLowAskTime.Margin = new Padding(0);
            allTimeLowAskTime.Name = "allTimeLowAskTime";
            allTimeLowAskTime.Size = new Size(17, 15);
            allTimeLowAskTime.TabIndex = 1;
            allTimeLowAskTime.Text = "--";
            allTimeLowAskTime.TextAlign = ContentAlignment.MiddleRight;
            // 
            // allTimeLowBid
            // 
            allTimeLowBid.Controls.Add(allTimeLowBidPrice);
            allTimeLowBid.Controls.Add(allTimeLowBidTime);
            allTimeLowBid.Dock = DockStyle.Fill;
            allTimeLowBid.Location = new Point(98, 173);
            allTimeLowBid.Margin = new Padding(4, 3, 4, 3);
            allTimeLowBid.Name = "allTimeLowBid";
            allTimeLowBid.Size = new Size(33, 32);
            allTimeLowBid.TabIndex = 14;
            // 
            // allTimeLowBidPrice
            // 
            allTimeLowBidPrice.AutoSize = true;
            allTimeLowBidPrice.Dock = DockStyle.Top;
            allTimeLowBidPrice.Location = new Point(0, 0);
            allTimeLowBidPrice.Margin = new Padding(0);
            allTimeLowBidPrice.Name = "allTimeLowBidPrice";
            allTimeLowBidPrice.Size = new Size(17, 15);
            allTimeLowBidPrice.TabIndex = 0;
            allTimeLowBidPrice.Text = "--";
            allTimeLowBidPrice.TextAlign = ContentAlignment.MiddleRight;
            // 
            // allTimeLowBidTime
            // 
            allTimeLowBidTime.AutoSize = true;
            allTimeLowBidTime.Dock = DockStyle.Bottom;
            allTimeLowBidTime.Location = new Point(0, 17);
            allTimeLowBidTime.Margin = new Padding(0);
            allTimeLowBidTime.Name = "allTimeLowBidTime";
            allTimeLowBidTime.Size = new Size(17, 15);
            allTimeLowBidTime.TabIndex = 1;
            allTimeLowBidTime.Text = "--";
            allTimeLowBidTime.TextAlign = ContentAlignment.MiddleRight;
            // 
            // tradingMetricsGrid
            // 
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
            tradingMetricsGrid.Dock = DockStyle.Fill;
            tradingMetricsGrid.Location = new Point(4, 217);
            tradingMetricsGrid.Margin = new Padding(4, 3, 4, 3);
            tradingMetricsGrid.Name = "tradingMetricsGrid";
            tradingMetricsGrid.RowCount = 9;
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 11.11F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 11.11F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 11.11F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 11.11F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 11.11F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 11.11F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 11.11F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 11.11F));
            tradingMetricsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 11.11F));
            tradingMetricsGrid.Size = new Size(135, 316);
            tradingMetricsGrid.TabIndex = 1;
            // 
            // tradingMetricsHeader
            // 
            tradingMetricsHeader.AutoSize = true;
            tradingMetricsGrid.SetColumnSpan(tradingMetricsHeader, 2);
            tradingMetricsHeader.Dock = DockStyle.Fill;
            tradingMetricsHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            tradingMetricsHeader.Location = new Point(4, 3);
            tradingMetricsHeader.Margin = new Padding(4, 3, 4, 3);
            tradingMetricsHeader.Name = "tradingMetricsHeader";
            tradingMetricsHeader.Size = new Size(127, 29);
            tradingMetricsHeader.TabIndex = 0;
            tradingMetricsHeader.Text = "Trading Metrics";
            tradingMetricsHeader.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // rsiLabel
            // 
            rsiLabel.AutoSize = true;
            rsiLabel.Dock = DockStyle.Fill;
            rsiLabel.Location = new Point(4, 35);
            rsiLabel.Margin = new Padding(4, 0, 4, 0);
            rsiLabel.Name = "rsiLabel";
            rsiLabel.Size = new Size(59, 35);
            rsiLabel.TabIndex = 1;
            rsiLabel.Text = "RSI";
            rsiLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // rsiValue
            // 
            rsiValue.AutoSize = true;
            rsiValue.Dock = DockStyle.Fill;
            rsiValue.Location = new Point(71, 35);
            rsiValue.Margin = new Padding(4, 0, 4, 0);
            rsiValue.Name = "rsiValue";
            rsiValue.Size = new Size(60, 35);
            rsiValue.TabIndex = 2;
            rsiValue.Text = "--";
            rsiValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // macdLabel
            // 
            macdLabel.AutoSize = true;
            macdLabel.Dock = DockStyle.Fill;
            macdLabel.Location = new Point(4, 70);
            macdLabel.Margin = new Padding(4, 0, 4, 0);
            macdLabel.Name = "macdLabel";
            macdLabel.Size = new Size(59, 35);
            macdLabel.TabIndex = 3;
            macdLabel.Text = "MACD";
            macdLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // macdValue
            // 
            macdValue.AutoSize = true;
            macdValue.Dock = DockStyle.Fill;
            macdValue.Location = new Point(71, 70);
            macdValue.Margin = new Padding(4, 0, 4, 0);
            macdValue.Name = "macdValue";
            macdValue.Size = new Size(60, 35);
            macdValue.TabIndex = 4;
            macdValue.Text = "--";
            macdValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // emaLabel
            // 
            emaLabel.AutoSize = true;
            emaLabel.Dock = DockStyle.Fill;
            emaLabel.Location = new Point(4, 105);
            emaLabel.Margin = new Padding(4, 0, 4, 0);
            emaLabel.Name = "emaLabel";
            emaLabel.Size = new Size(59, 35);
            emaLabel.TabIndex = 5;
            emaLabel.Text = "EMA";
            emaLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // emaValue
            // 
            emaValue.AutoSize = true;
            emaValue.Dock = DockStyle.Fill;
            emaValue.Location = new Point(71, 105);
            emaValue.Margin = new Padding(4, 0, 4, 0);
            emaValue.Name = "emaValue";
            emaValue.Size = new Size(60, 35);
            emaValue.TabIndex = 6;
            emaValue.Text = "--";
            emaValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // bollingerLabel
            // 
            bollingerLabel.AutoSize = true;
            bollingerLabel.Dock = DockStyle.Fill;
            bollingerLabel.Location = new Point(4, 140);
            bollingerLabel.Margin = new Padding(4, 0, 4, 0);
            bollingerLabel.Name = "bollingerLabel";
            bollingerLabel.Size = new Size(59, 35);
            bollingerLabel.TabIndex = 7;
            bollingerLabel.Text = "Bollinger Bands";
            bollingerLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // bollingerValue
            // 
            bollingerValue.AutoSize = true;
            bollingerValue.Dock = DockStyle.Fill;
            bollingerValue.Location = new Point(71, 140);
            bollingerValue.Margin = new Padding(4, 0, 4, 0);
            bollingerValue.Name = "bollingerValue";
            bollingerValue.Size = new Size(60, 35);
            bollingerValue.TabIndex = 8;
            bollingerValue.Text = "--";
            bollingerValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // atrLabel
            // 
            atrLabel.AutoSize = true;
            atrLabel.Dock = DockStyle.Fill;
            atrLabel.Location = new Point(4, 175);
            atrLabel.Margin = new Padding(4, 0, 4, 0);
            atrLabel.Name = "atrLabel";
            atrLabel.Size = new Size(59, 35);
            atrLabel.TabIndex = 9;
            atrLabel.Text = "ATR";
            atrLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // atrValue
            // 
            atrValue.AutoSize = true;
            atrValue.Dock = DockStyle.Fill;
            atrValue.Location = new Point(71, 175);
            atrValue.Margin = new Padding(4, 0, 4, 0);
            atrValue.Name = "atrValue";
            atrValue.Size = new Size(60, 35);
            atrValue.TabIndex = 10;
            atrValue.Text = "--";
            atrValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // vwapLabel
            // 
            vwapLabel.AutoSize = true;
            vwapLabel.Dock = DockStyle.Fill;
            vwapLabel.Location = new Point(4, 210);
            vwapLabel.Margin = new Padding(4, 0, 4, 0);
            vwapLabel.Name = "vwapLabel";
            vwapLabel.Size = new Size(59, 35);
            vwapLabel.TabIndex = 11;
            vwapLabel.Text = "VWAP";
            vwapLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // vwapValue
            // 
            vwapValue.AutoSize = true;
            vwapValue.Dock = DockStyle.Fill;
            vwapValue.Location = new Point(71, 210);
            vwapValue.Margin = new Padding(4, 0, 4, 0);
            vwapValue.Name = "vwapValue";
            vwapValue.Size = new Size(60, 35);
            vwapValue.TabIndex = 12;
            vwapValue.Text = "--";
            vwapValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // stochasticLabel
            // 
            stochasticLabel.AutoSize = true;
            stochasticLabel.Dock = DockStyle.Fill;
            stochasticLabel.Location = new Point(4, 245);
            stochasticLabel.Margin = new Padding(4, 0, 4, 0);
            stochasticLabel.Name = "stochasticLabel";
            stochasticLabel.Size = new Size(59, 35);
            stochasticLabel.TabIndex = 13;
            stochasticLabel.Text = "Stochastic Oscillator";
            stochasticLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // stochasticValue
            // 
            stochasticValue.AutoSize = true;
            stochasticValue.Dock = DockStyle.Fill;
            stochasticValue.Location = new Point(71, 245);
            stochasticValue.Margin = new Padding(4, 0, 4, 0);
            stochasticValue.Name = "stochasticValue";
            stochasticValue.Size = new Size(60, 35);
            stochasticValue.TabIndex = 14;
            stochasticValue.Text = "--";
            stochasticValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // obvLabel
            // 
            obvLabel.AutoSize = true;
            obvLabel.Dock = DockStyle.Fill;
            obvLabel.Location = new Point(4, 280);
            obvLabel.Margin = new Padding(4, 0, 4, 0);
            obvLabel.Name = "obvLabel";
            obvLabel.Size = new Size(59, 36);
            obvLabel.TabIndex = 15;
            obvLabel.Text = "OBV";
            obvLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // obvValue
            // 
            obvValue.AutoSize = true;
            obvValue.Dock = DockStyle.Fill;
            obvValue.Location = new Point(71, 280);
            obvValue.Margin = new Padding(4, 0, 4, 0);
            obvValue.Name = "obvValue";
            obvValue.Size = new Size(60, 36);
            obvValue.TabIndex = 16;
            obvValue.Text = "--";
            obvValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // rightColumn
            // 
            rightColumn.ColumnCount = 1;
            rightColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rightColumn.Controls.Add(otherInfoGrid, 0, 0);
            rightColumn.Controls.Add(flowMomentumGrid, 0, 1);
            rightColumn.Controls.Add(contextGrid, 0, 2);
            rightColumn.Dock = DockStyle.Fill;
            rightColumn.Location = new Point(155, 3);
            rightColumn.Margin = new Padding(4, 3, 4, 3);
            rightColumn.Name = "rightColumn";
            rightColumn.RowCount = 3;
            rightColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            rightColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
            rightColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
            rightColumn.Size = new Size(143, 536);
            rightColumn.TabIndex = 1;
            // 
            // otherInfoGrid
            // 
            otherInfoGrid.ColumnCount = 2;
            otherInfoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            otherInfoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            otherInfoGrid.Controls.Add(titleLabel, 0, 0);
            otherInfoGrid.Controls.Add(titleValue, 1, 0);
            otherInfoGrid.Controls.Add(subtitleLabel, 0, 1);
            otherInfoGrid.Controls.Add(subtitleValue, 1, 1);
            otherInfoGrid.Controls.Add(marketTypeLabel, 0, 2);
            otherInfoGrid.Controls.Add(marketTypeValue, 1, 2);
            otherInfoGrid.Controls.Add(priceGoodBadLabel, 0, 3);
            otherInfoGrid.Controls.Add(priceGoodBadValue, 1, 3);
            otherInfoGrid.Controls.Add(marketBehaviorLabel, 0, 4);
            otherInfoGrid.Controls.Add(marketBehaviorValue, 1, 4);
            otherInfoGrid.Controls.Add(timeLeftLabel, 0, 5);
            otherInfoGrid.Controls.Add(timeLeftValue, 1, 5);
            otherInfoGrid.Controls.Add(marketAgeLabel, 0, 6);
            otherInfoGrid.Controls.Add(marketAgeValue, 1, 6);
            otherInfoGrid.Dock = DockStyle.Fill;
            otherInfoGrid.Location = new Point(4, 3);
            otherInfoGrid.Margin = new Padding(4, 3, 4, 3);
            otherInfoGrid.Name = "otherInfoGrid";
            otherInfoGrid.RowCount = 7;
            otherInfoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            otherInfoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            otherInfoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            otherInfoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            otherInfoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            otherInfoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            otherInfoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            otherInfoGrid.Size = new Size(135, 154);
            otherInfoGrid.TabIndex = 0;
            // 
            // titleLabel
            // 
            titleLabel.AutoSize = true;
            titleLabel.Dock = DockStyle.Fill;
            titleLabel.Location = new Point(4, 3);
            titleLabel.Margin = new Padding(4, 3, 4, 3);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new Size(59, 16);
            titleLabel.TabIndex = 0;
            titleLabel.Text = "Title";
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // titleValue
            // 
            titleValue.AutoSize = true;
            titleValue.Dock = DockStyle.Fill;
            titleValue.Location = new Point(71, 3);
            titleValue.Margin = new Padding(4, 3, 4, 3);
            titleValue.Name = "titleValue";
            titleValue.Size = new Size(60, 16);
            titleValue.TabIndex = 1;
            titleValue.Text = "--";
            titleValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // subtitleLabel
            // 
            subtitleLabel.AutoSize = true;
            subtitleLabel.Dock = DockStyle.Fill;
            subtitleLabel.Location = new Point(4, 22);
            subtitleLabel.Margin = new Padding(4, 0, 4, 0);
            subtitleLabel.Name = "subtitleLabel";
            subtitleLabel.Size = new Size(59, 22);
            subtitleLabel.TabIndex = 2;
            subtitleLabel.Text = "Subtitle";
            subtitleLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // subtitleValue
            // 
            subtitleValue.AutoSize = true;
            subtitleValue.Dock = DockStyle.Fill;
            subtitleValue.Location = new Point(71, 22);
            subtitleValue.Margin = new Padding(4, 0, 4, 0);
            subtitleValue.Name = "subtitleValue";
            subtitleValue.Size = new Size(60, 22);
            subtitleValue.TabIndex = 3;
            subtitleValue.Text = "--";
            subtitleValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // marketTypeLabel
            // 
            marketTypeLabel.AutoSize = true;
            marketTypeLabel.Dock = DockStyle.Fill;
            marketTypeLabel.Location = new Point(4, 44);
            marketTypeLabel.Margin = new Padding(4, 0, 4, 0);
            marketTypeLabel.Name = "marketTypeLabel";
            marketTypeLabel.Size = new Size(59, 22);
            marketTypeLabel.TabIndex = 4;
            marketTypeLabel.Text = "Market Type";
            marketTypeLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // marketTypeValue
            // 
            marketTypeValue.AutoSize = true;
            marketTypeValue.Dock = DockStyle.Fill;
            marketTypeValue.Location = new Point(71, 44);
            marketTypeValue.Margin = new Padding(4, 0, 4, 0);
            marketTypeValue.Name = "marketTypeValue";
            marketTypeValue.Size = new Size(60, 22);
            marketTypeValue.TabIndex = 5;
            marketTypeValue.Text = "--";
            marketTypeValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // priceGoodBadLabel
            // 
            priceGoodBadLabel.AutoSize = true;
            priceGoodBadLabel.Dock = DockStyle.Fill;
            priceGoodBadLabel.Location = new Point(4, 66);
            priceGoodBadLabel.Margin = new Padding(4, 0, 4, 0);
            priceGoodBadLabel.Name = "priceGoodBadLabel";
            priceGoodBadLabel.Size = new Size(59, 22);
            priceGoodBadLabel.TabIndex = 6;
            priceGoodBadLabel.Text = "Price Good/Bad";
            priceGoodBadLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // priceGoodBadValue
            // 
            priceGoodBadValue.AutoSize = true;
            priceGoodBadValue.Dock = DockStyle.Fill;
            priceGoodBadValue.Location = new Point(71, 66);
            priceGoodBadValue.Margin = new Padding(4, 0, 4, 0);
            priceGoodBadValue.Name = "priceGoodBadValue";
            priceGoodBadValue.Size = new Size(60, 22);
            priceGoodBadValue.TabIndex = 7;
            priceGoodBadValue.Text = "--";
            priceGoodBadValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // marketBehaviorLabel
            // 
            marketBehaviorLabel.AutoSize = true;
            marketBehaviorLabel.Dock = DockStyle.Fill;
            marketBehaviorLabel.Location = new Point(4, 88);
            marketBehaviorLabel.Margin = new Padding(4, 0, 4, 0);
            marketBehaviorLabel.Name = "marketBehaviorLabel";
            marketBehaviorLabel.Size = new Size(59, 22);
            marketBehaviorLabel.TabIndex = 8;
            marketBehaviorLabel.Text = "Market Behavior";
            marketBehaviorLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // marketBehaviorValue
            // 
            marketBehaviorValue.AutoSize = true;
            marketBehaviorValue.Dock = DockStyle.Fill;
            marketBehaviorValue.Location = new Point(71, 88);
            marketBehaviorValue.Margin = new Padding(4, 0, 4, 0);
            marketBehaviorValue.Name = "marketBehaviorValue";
            marketBehaviorValue.Size = new Size(60, 22);
            marketBehaviorValue.TabIndex = 9;
            marketBehaviorValue.Text = "--";
            marketBehaviorValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // timeLeftLabel
            // 
            timeLeftLabel.AutoSize = true;
            timeLeftLabel.Dock = DockStyle.Fill;
            timeLeftLabel.Location = new Point(4, 110);
            timeLeftLabel.Margin = new Padding(4, 0, 4, 0);
            timeLeftLabel.Name = "timeLeftLabel";
            timeLeftLabel.Size = new Size(59, 22);
            timeLeftLabel.TabIndex = 10;
            timeLeftLabel.Text = "Time Left";
            timeLeftLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // timeLeftValue
            // 
            timeLeftValue.AutoSize = true;
            timeLeftValue.Dock = DockStyle.Fill;
            timeLeftValue.Location = new Point(71, 110);
            timeLeftValue.Margin = new Padding(4, 0, 4, 0);
            timeLeftValue.Name = "timeLeftValue";
            timeLeftValue.Size = new Size(60, 22);
            timeLeftValue.TabIndex = 11;
            timeLeftValue.Text = "--";
            timeLeftValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // marketAgeLabel
            // 
            marketAgeLabel.AutoSize = true;
            marketAgeLabel.Dock = DockStyle.Fill;
            marketAgeLabel.Location = new Point(4, 132);
            marketAgeLabel.Margin = new Padding(4, 0, 4, 0);
            marketAgeLabel.Name = "marketAgeLabel";
            marketAgeLabel.Size = new Size(59, 22);
            marketAgeLabel.TabIndex = 12;
            marketAgeLabel.Text = "Market Age";
            marketAgeLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // marketAgeValue
            // 
            marketAgeValue.AutoSize = true;
            marketAgeValue.Dock = DockStyle.Fill;
            marketAgeValue.Location = new Point(71, 132);
            marketAgeValue.Margin = new Padding(4, 0, 4, 0);
            marketAgeValue.Name = "marketAgeValue";
            marketAgeValue.Size = new Size(60, 22);
            marketAgeValue.TabIndex = 13;
            marketAgeValue.Text = "--";
            marketAgeValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // flowMomentumGrid
            // 
            flowMomentumGrid.ColumnCount = 3;
            flowMomentumGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            flowMomentumGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            flowMomentumGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            flowMomentumGrid.Controls.Add(flowHeader, 0, 0);
            flowMomentumGrid.Controls.Add(flowHeaderEmpty, 0, 1);
            flowMomentumGrid.Controls.Add(flowHeaderYes, 1, 1);
            flowMomentumGrid.Controls.Add(flowHeaderNo, 2, 1);
            flowMomentumGrid.Controls.Add(topVelocityLabel, 0, 2);
            flowMomentumGrid.Controls.Add(topVelocityYesValue, 1, 2);
            flowMomentumGrid.Controls.Add(topVelocityNoValue, 2, 2);
            flowMomentumGrid.Controls.Add(bottomVelocityLabel, 0, 3);
            flowMomentumGrid.Controls.Add(bottomVelocityYesValue, 1, 3);
            flowMomentumGrid.Controls.Add(bottomVelocityNoValue, 2, 3);
            flowMomentumGrid.Controls.Add(netOrderRateLabel, 0, 4);
            flowMomentumGrid.Controls.Add(netOrderRateYesValue, 1, 4);
            flowMomentumGrid.Controls.Add(netOrderRateNoValue, 2, 4);
            flowMomentumGrid.Controls.Add(tradeVolumeLabel, 0, 5);
            flowMomentumGrid.Controls.Add(tradeVolumeYesValue, 1, 5);
            flowMomentumGrid.Controls.Add(tradeVolumeNoValue, 2, 5);
            flowMomentumGrid.Controls.Add(avgTradeSizeLabel, 0, 6);
            flowMomentumGrid.Controls.Add(avgTradeSizeYesValue, 1, 6);
            flowMomentumGrid.Controls.Add(avgTradeSizeNoValue, 2, 6);
            flowMomentumGrid.Dock = DockStyle.Fill;
            flowMomentumGrid.Location = new Point(4, 163);
            flowMomentumGrid.Margin = new Padding(4, 3, 4, 3);
            flowMomentumGrid.Name = "flowMomentumGrid";
            flowMomentumGrid.RowCount = 7;
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            flowMomentumGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            flowMomentumGrid.Size = new Size(135, 181);
            flowMomentumGrid.TabIndex = 1;
            // 
            // flowHeader
            // 
            flowHeader.AutoSize = true;
            flowMomentumGrid.SetColumnSpan(flowHeader, 3);
            flowHeader.Dock = DockStyle.Fill;
            flowHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            flowHeader.Location = new Point(4, 3);
            flowHeader.Margin = new Padding(4, 3, 4, 3);
            flowHeader.Name = "flowHeader";
            flowHeader.Size = new Size(127, 19);
            flowHeader.TabIndex = 0;
            flowHeader.Text = "Flow & Momentum";
            flowHeader.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // flowHeaderEmpty
            // 
            flowHeaderEmpty.AutoSize = true;
            flowHeaderEmpty.Dock = DockStyle.Fill;
            flowHeaderEmpty.Location = new Point(4, 25);
            flowHeaderEmpty.Margin = new Padding(4, 0, 4, 0);
            flowHeaderEmpty.Name = "flowHeaderEmpty";
            flowHeaderEmpty.Size = new Size(46, 25);
            flowHeaderEmpty.TabIndex = 1;
            // 
            // flowHeaderYes
            // 
            flowHeaderYes.AutoSize = true;
            flowHeaderYes.Dock = DockStyle.Fill;
            flowHeaderYes.Location = new Point(58, 25);
            flowHeaderYes.Margin = new Padding(4, 0, 4, 0);
            flowHeaderYes.Name = "flowHeaderYes";
            flowHeaderYes.Size = new Size(32, 25);
            flowHeaderYes.TabIndex = 2;
            flowHeaderYes.Text = "Yes";
            flowHeaderYes.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // flowHeaderNo
            // 
            flowHeaderNo.AutoSize = true;
            flowHeaderNo.Dock = DockStyle.Fill;
            flowHeaderNo.Location = new Point(98, 25);
            flowHeaderNo.Margin = new Padding(4, 0, 4, 0);
            flowHeaderNo.Name = "flowHeaderNo";
            flowHeaderNo.Size = new Size(33, 25);
            flowHeaderNo.TabIndex = 3;
            flowHeaderNo.Text = "No";
            flowHeaderNo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // topVelocityLabel
            // 
            topVelocityLabel.AutoSize = true;
            topVelocityLabel.Dock = DockStyle.Fill;
            topVelocityLabel.Location = new Point(4, 50);
            topVelocityLabel.Margin = new Padding(4, 0, 4, 0);
            topVelocityLabel.Name = "topVelocityLabel";
            topVelocityLabel.Size = new Size(46, 25);
            topVelocityLabel.TabIndex = 4;
            topVelocityLabel.Text = "Top Velocity";
            topVelocityLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // topVelocityYesValue
            // 
            topVelocityYesValue.AutoSize = true;
            topVelocityYesValue.Dock = DockStyle.Fill;
            topVelocityYesValue.Location = new Point(58, 50);
            topVelocityYesValue.Margin = new Padding(4, 0, 4, 0);
            topVelocityYesValue.Name = "topVelocityYesValue";
            topVelocityYesValue.Size = new Size(32, 25);
            topVelocityYesValue.TabIndex = 5;
            topVelocityYesValue.Text = "--";
            topVelocityYesValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // topVelocityNoValue
            // 
            topVelocityNoValue.AutoSize = true;
            topVelocityNoValue.Dock = DockStyle.Fill;
            topVelocityNoValue.Location = new Point(98, 50);
            topVelocityNoValue.Margin = new Padding(4, 0, 4, 0);
            topVelocityNoValue.Name = "topVelocityNoValue";
            topVelocityNoValue.Size = new Size(33, 25);
            topVelocityNoValue.TabIndex = 6;
            topVelocityNoValue.Text = "--";
            topVelocityNoValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // bottomVelocityLabel
            // 
            bottomVelocityLabel.AutoSize = true;
            bottomVelocityLabel.Dock = DockStyle.Fill;
            bottomVelocityLabel.Location = new Point(4, 75);
            bottomVelocityLabel.Margin = new Padding(4, 0, 4, 0);
            bottomVelocityLabel.Name = "bottomVelocityLabel";
            bottomVelocityLabel.Size = new Size(46, 25);
            bottomVelocityLabel.TabIndex = 7;
            bottomVelocityLabel.Text = "Bottom Velocity";
            bottomVelocityLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // bottomVelocityYesValue
            // 
            bottomVelocityYesValue.AutoSize = true;
            bottomVelocityYesValue.Dock = DockStyle.Fill;
            bottomVelocityYesValue.Location = new Point(58, 75);
            bottomVelocityYesValue.Margin = new Padding(4, 0, 4, 0);
            bottomVelocityYesValue.Name = "bottomVelocityYesValue";
            bottomVelocityYesValue.Size = new Size(32, 25);
            bottomVelocityYesValue.TabIndex = 8;
            bottomVelocityYesValue.Text = "--";
            bottomVelocityYesValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // bottomVelocityNoValue
            // 
            bottomVelocityNoValue.AutoSize = true;
            bottomVelocityNoValue.Dock = DockStyle.Fill;
            bottomVelocityNoValue.Location = new Point(98, 75);
            bottomVelocityNoValue.Margin = new Padding(4, 0, 4, 0);
            bottomVelocityNoValue.Name = "bottomVelocityNoValue";
            bottomVelocityNoValue.Size = new Size(33, 25);
            bottomVelocityNoValue.TabIndex = 9;
            bottomVelocityNoValue.Text = "--";
            bottomVelocityNoValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // netOrderRateLabel
            // 
            netOrderRateLabel.AutoSize = true;
            netOrderRateLabel.Dock = DockStyle.Fill;
            netOrderRateLabel.Location = new Point(4, 100);
            netOrderRateLabel.Margin = new Padding(4, 0, 4, 0);
            netOrderRateLabel.Name = "netOrderRateLabel";
            netOrderRateLabel.Size = new Size(46, 25);
            netOrderRateLabel.TabIndex = 10;
            netOrderRateLabel.Text = "Net Order Rate";
            netOrderRateLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // netOrderRateYesValue
            // 
            netOrderRateYesValue.AutoSize = true;
            netOrderRateYesValue.Dock = DockStyle.Fill;
            netOrderRateYesValue.Location = new Point(58, 100);
            netOrderRateYesValue.Margin = new Padding(4, 0, 4, 0);
            netOrderRateYesValue.Name = "netOrderRateYesValue";
            netOrderRateYesValue.Size = new Size(32, 25);
            netOrderRateYesValue.TabIndex = 11;
            netOrderRateYesValue.Text = "--";
            netOrderRateYesValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // netOrderRateNoValue
            // 
            netOrderRateNoValue.AutoSize = true;
            netOrderRateNoValue.Dock = DockStyle.Fill;
            netOrderRateNoValue.Location = new Point(98, 100);
            netOrderRateNoValue.Margin = new Padding(4, 0, 4, 0);
            netOrderRateNoValue.Name = "netOrderRateNoValue";
            netOrderRateNoValue.Size = new Size(33, 25);
            netOrderRateNoValue.TabIndex = 12;
            netOrderRateNoValue.Text = "--";
            netOrderRateNoValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // tradeVolumeLabel
            // 
            tradeVolumeLabel.AutoSize = true;
            tradeVolumeLabel.Dock = DockStyle.Fill;
            tradeVolumeLabel.Location = new Point(4, 125);
            tradeVolumeLabel.Margin = new Padding(4, 0, 4, 0);
            tradeVolumeLabel.Name = "tradeVolumeLabel";
            tradeVolumeLabel.Size = new Size(46, 25);
            tradeVolumeLabel.TabIndex = 13;
            tradeVolumeLabel.Text = "Trade Volume";
            tradeVolumeLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // tradeVolumeYesValue
            // 
            tradeVolumeYesValue.AutoSize = true;
            tradeVolumeYesValue.Dock = DockStyle.Fill;
            tradeVolumeYesValue.Location = new Point(58, 125);
            tradeVolumeYesValue.Margin = new Padding(4, 0, 4, 0);
            tradeVolumeYesValue.Name = "tradeVolumeYesValue";
            tradeVolumeYesValue.Size = new Size(32, 25);
            tradeVolumeYesValue.TabIndex = 14;
            tradeVolumeYesValue.Text = "--";
            tradeVolumeYesValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // tradeVolumeNoValue
            // 
            tradeVolumeNoValue.AutoSize = true;
            tradeVolumeNoValue.Dock = DockStyle.Fill;
            tradeVolumeNoValue.Location = new Point(98, 125);
            tradeVolumeNoValue.Margin = new Padding(4, 0, 4, 0);
            tradeVolumeNoValue.Name = "tradeVolumeNoValue";
            tradeVolumeNoValue.Size = new Size(33, 25);
            tradeVolumeNoValue.TabIndex = 15;
            tradeVolumeNoValue.Text = "--";
            tradeVolumeNoValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // avgTradeSizeLabel
            // 
            avgTradeSizeLabel.AutoSize = true;
            avgTradeSizeLabel.Dock = DockStyle.Fill;
            avgTradeSizeLabel.Location = new Point(4, 150);
            avgTradeSizeLabel.Margin = new Padding(4, 0, 4, 0);
            avgTradeSizeLabel.Name = "avgTradeSizeLabel";
            avgTradeSizeLabel.Size = new Size(46, 31);
            avgTradeSizeLabel.TabIndex = 16;
            avgTradeSizeLabel.Text = "Average Trade Size";
            avgTradeSizeLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // avgTradeSizeYesValue
            // 
            avgTradeSizeYesValue.AutoSize = true;
            avgTradeSizeYesValue.Dock = DockStyle.Fill;
            avgTradeSizeYesValue.Location = new Point(58, 150);
            avgTradeSizeYesValue.Margin = new Padding(4, 0, 4, 0);
            avgTradeSizeYesValue.Name = "avgTradeSizeYesValue";
            avgTradeSizeYesValue.Size = new Size(32, 31);
            avgTradeSizeYesValue.TabIndex = 17;
            avgTradeSizeYesValue.Text = "--";
            avgTradeSizeYesValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // avgTradeSizeNoValue
            // 
            avgTradeSizeNoValue.AutoSize = true;
            avgTradeSizeNoValue.Dock = DockStyle.Fill;
            avgTradeSizeNoValue.Location = new Point(98, 150);
            avgTradeSizeNoValue.Margin = new Padding(4, 0, 4, 0);
            avgTradeSizeNoValue.Name = "avgTradeSizeNoValue";
            avgTradeSizeNoValue.Size = new Size(33, 31);
            avgTradeSizeNoValue.TabIndex = 18;
            avgTradeSizeNoValue.Text = "--";
            avgTradeSizeNoValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // contextGrid
            // 
            contextGrid.ColumnCount = 3;
            contextGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            contextGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            contextGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            contextGrid.Controls.Add(contextHeader, 0, 0);
            contextGrid.Controls.Add(contextHeaderEmpty, 0, 1);
            contextGrid.Controls.Add(contextHeaderYes, 1, 1);
            contextGrid.Controls.Add(contextHeaderNo, 2, 1);
            contextGrid.Controls.Add(spreadLabel, 0, 2);
            contextGrid.Controls.Add(spreadValue, 1, 2);
            contextGrid.Controls.Add(imbalLabel, 0, 3);
            contextGrid.Controls.Add(imbalValue, 1, 3);
            contextGrid.Controls.Add(depthTop4Label, 0, 4);
            contextGrid.Controls.Add(depthTop4YesValue, 1, 4);
            contextGrid.Controls.Add(depthTop4NoValue, 2, 4);
            contextGrid.Controls.Add(centerMassLabel, 0, 5);
            contextGrid.Controls.Add(centerMassYesValue, 1, 5);
            contextGrid.Controls.Add(centerMassNoValue, 2, 5);
            contextGrid.Controls.Add(totalContractsLabel, 0, 6);
            contextGrid.Controls.Add(totalContractsYesValue, 1, 6);
            contextGrid.Controls.Add(totalContractsNoValue, 2, 6);
            contextGrid.Dock = DockStyle.Fill;
            contextGrid.Location = new Point(4, 350);
            contextGrid.Margin = new Padding(4, 3, 4, 3);
            contextGrid.Name = "contextGrid";
            contextGrid.RowCount = 7;
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 14.29F));
            contextGrid.Size = new Size(135, 183);
            contextGrid.TabIndex = 2;
            // 
            // contextHeader
            // 
            contextHeader.AutoSize = true;
            contextGrid.SetColumnSpan(contextHeader, 3);
            contextHeader.Dock = DockStyle.Fill;
            contextHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            contextHeader.Location = new Point(4, 3);
            contextHeader.Margin = new Padding(4, 3, 4, 3);
            contextHeader.Name = "contextHeader";
            contextHeader.Size = new Size(127, 20);
            contextHeader.TabIndex = 0;
            contextHeader.Text = "Context & Deeper Book";
            contextHeader.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // contextHeaderEmpty
            // 
            contextHeaderEmpty.AutoSize = true;
            contextHeaderEmpty.Dock = DockStyle.Fill;
            contextHeaderEmpty.Location = new Point(4, 26);
            contextHeaderEmpty.Margin = new Padding(4, 0, 4, 0);
            contextHeaderEmpty.Name = "contextHeaderEmpty";
            contextHeaderEmpty.Size = new Size(46, 26);
            contextHeaderEmpty.TabIndex = 1;
            // 
            // contextHeaderYes
            // 
            contextHeaderYes.AutoSize = true;
            contextHeaderYes.Dock = DockStyle.Fill;
            contextHeaderYes.Location = new Point(58, 26);
            contextHeaderYes.Margin = new Padding(4, 0, 4, 0);
            contextHeaderYes.Name = "contextHeaderYes";
            contextHeaderYes.Size = new Size(32, 26);
            contextHeaderYes.TabIndex = 2;
            contextHeaderYes.Text = "Yes";
            contextHeaderYes.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // contextHeaderNo
            // 
            contextHeaderNo.AutoSize = true;
            contextHeaderNo.Dock = DockStyle.Fill;
            contextHeaderNo.Location = new Point(98, 26);
            contextHeaderNo.Margin = new Padding(4, 0, 4, 0);
            contextHeaderNo.Name = "contextHeaderNo";
            contextHeaderNo.Size = new Size(33, 26);
            contextHeaderNo.TabIndex = 3;
            contextHeaderNo.Text = "No";
            contextHeaderNo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // spreadLabel
            // 
            spreadLabel.AutoSize = true;
            spreadLabel.Dock = DockStyle.Fill;
            spreadLabel.Location = new Point(4, 52);
            spreadLabel.Margin = new Padding(4, 0, 4, 0);
            spreadLabel.Name = "spreadLabel";
            spreadLabel.Size = new Size(46, 26);
            spreadLabel.TabIndex = 4;
            spreadLabel.Text = "Spread";
            spreadLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // spreadValue
            // 
            spreadValue.AutoSize = true;
            contextGrid.SetColumnSpan(spreadValue, 2);
            spreadValue.Dock = DockStyle.Fill;
            spreadValue.Location = new Point(58, 52);
            spreadValue.Margin = new Padding(4, 0, 4, 0);
            spreadValue.Name = "spreadValue";
            spreadValue.Size = new Size(73, 26);
            spreadValue.TabIndex = 5;
            spreadValue.Text = "--";
            spreadValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // imbalLabel
            // 
            imbalLabel.AutoSize = true;
            imbalLabel.Dock = DockStyle.Fill;
            imbalLabel.Location = new Point(4, 78);
            imbalLabel.Margin = new Padding(4, 0, 4, 0);
            imbalLabel.Name = "imbalLabel";
            imbalLabel.Size = new Size(46, 26);
            imbalLabel.TabIndex = 6;
            imbalLabel.Text = "Ask/Bid Imbal (Vol)";
            imbalLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // imbalValue
            // 
            imbalValue.AutoSize = true;
            contextGrid.SetColumnSpan(imbalValue, 2);
            imbalValue.Dock = DockStyle.Fill;
            imbalValue.Location = new Point(58, 78);
            imbalValue.Margin = new Padding(4, 0, 4, 0);
            imbalValue.Name = "imbalValue";
            imbalValue.Size = new Size(73, 26);
            imbalValue.TabIndex = 7;
            imbalValue.Text = "--";
            imbalValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // depthTop4Label
            // 
            depthTop4Label.AutoSize = true;
            depthTop4Label.Dock = DockStyle.Fill;
            depthTop4Label.Location = new Point(4, 104);
            depthTop4Label.Margin = new Padding(4, 0, 4, 0);
            depthTop4Label.Name = "depthTop4Label";
            depthTop4Label.Size = new Size(46, 26);
            depthTop4Label.TabIndex = 8;
            depthTop4Label.Text = "Depth Top 4";
            depthTop4Label.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // depthTop4YesValue
            // 
            depthTop4YesValue.AutoSize = true;
            depthTop4YesValue.Dock = DockStyle.Fill;
            depthTop4YesValue.Location = new Point(58, 104);
            depthTop4YesValue.Margin = new Padding(4, 0, 4, 0);
            depthTop4YesValue.Name = "depthTop4YesValue";
            depthTop4YesValue.Size = new Size(32, 26);
            depthTop4YesValue.TabIndex = 9;
            depthTop4YesValue.Text = "--";
            depthTop4YesValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // depthTop4NoValue
            // 
            depthTop4NoValue.AutoSize = true;
            depthTop4NoValue.Dock = DockStyle.Fill;
            depthTop4NoValue.Location = new Point(98, 104);
            depthTop4NoValue.Margin = new Padding(4, 0, 4, 0);
            depthTop4NoValue.Name = "depthTop4NoValue";
            depthTop4NoValue.Size = new Size(33, 26);
            depthTop4NoValue.TabIndex = 10;
            depthTop4NoValue.Text = "--";
            depthTop4NoValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // centerMassLabel
            // 
            centerMassLabel.AutoSize = true;
            centerMassLabel.Dock = DockStyle.Fill;
            centerMassLabel.Location = new Point(4, 130);
            centerMassLabel.Margin = new Padding(4, 0, 4, 0);
            centerMassLabel.Name = "centerMassLabel";
            centerMassLabel.Size = new Size(46, 26);
            centerMassLabel.TabIndex = 11;
            centerMassLabel.Text = "Center Mass";
            centerMassLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // centerMassYesValue
            // 
            centerMassYesValue.AutoSize = true;
            centerMassYesValue.Dock = DockStyle.Fill;
            centerMassYesValue.Location = new Point(58, 130);
            centerMassYesValue.Margin = new Padding(4, 0, 4, 0);
            centerMassYesValue.Name = "centerMassYesValue";
            centerMassYesValue.Size = new Size(32, 26);
            centerMassYesValue.TabIndex = 12;
            centerMassYesValue.Text = "--";
            centerMassYesValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // centerMassNoValue
            // 
            centerMassNoValue.AutoSize = true;
            centerMassNoValue.Dock = DockStyle.Fill;
            centerMassNoValue.Location = new Point(98, 130);
            centerMassNoValue.Margin = new Padding(4, 0, 4, 0);
            centerMassNoValue.Name = "centerMassNoValue";
            centerMassNoValue.Size = new Size(33, 26);
            centerMassNoValue.TabIndex = 13;
            centerMassNoValue.Text = "--";
            centerMassNoValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // totalContractsLabel
            // 
            totalContractsLabel.AutoSize = true;
            totalContractsLabel.Dock = DockStyle.Fill;
            totalContractsLabel.Location = new Point(4, 156);
            totalContractsLabel.Margin = new Padding(4, 0, 4, 0);
            totalContractsLabel.Name = "totalContractsLabel";
            totalContractsLabel.Size = new Size(46, 27);
            totalContractsLabel.TabIndex = 14;
            totalContractsLabel.Text = "Total Contracts";
            totalContractsLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // totalContractsYesValue
            // 
            totalContractsYesValue.AutoSize = true;
            totalContractsYesValue.Dock = DockStyle.Fill;
            totalContractsYesValue.Location = new Point(58, 156);
            totalContractsYesValue.Margin = new Padding(4, 0, 4, 0);
            totalContractsYesValue.Name = "totalContractsYesValue";
            totalContractsYesValue.Size = new Size(32, 27);
            totalContractsYesValue.TabIndex = 15;
            totalContractsYesValue.Text = "--";
            totalContractsYesValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // totalContractsNoValue
            // 
            totalContractsNoValue.AutoSize = true;
            totalContractsNoValue.Dock = DockStyle.Fill;
            totalContractsNoValue.Location = new Point(98, 156);
            totalContractsNoValue.Margin = new Padding(4, 0, 4, 0);
            totalContractsNoValue.Name = "totalContractsNoValue";
            totalContractsNoValue.Size = new Size(33, 27);
            totalContractsNoValue.TabIndex = 16;
            totalContractsNoValue.Text = "--";
            totalContractsNoValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // positionsContainer
            // 
            positionsContainer.BorderStyle = BorderStyle.FixedSingle;
            positionsContainer.Controls.Add(positionsGrid);
            positionsContainer.Dock = DockStyle.Fill;
            positionsContainer.Location = new Point(4, 565);
            positionsContainer.Margin = new Padding(4, 3, 4, 3);
            positionsContainer.Name = "positionsContainer";
            positionsContainer.Padding = new Padding(6);
            positionsContainer.Size = new Size(593, 78);
            positionsContainer.TabIndex = 2;
            // 
            // positionsGrid
            // 
            positionsGrid.ColumnCount = 4;
            positionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            positionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            positionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            positionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            positionsGrid.Controls.Add(positionSizeLabel, 0, 0);
            positionsGrid.Controls.Add(positionSizeValue, 1, 0);
            positionsGrid.Controls.Add(lastTradeLabel, 2, 0);
            positionsGrid.Controls.Add(lastTradeValue, 3, 0);
            positionsGrid.Controls.Add(positionRoiLabel, 0, 1);
            positionsGrid.Controls.Add(positionRoiValue, 1, 1);
            positionsGrid.Controls.Add(buyinPriceLabel, 2, 1);
            positionsGrid.Controls.Add(buyinPriceValue, 3, 1);
            positionsGrid.Controls.Add(positionUpsideLabel, 0, 2);
            positionsGrid.Controls.Add(positionUpsideValue, 1, 2);
            positionsGrid.Controls.Add(positionDownsideLabel, 2, 2);
            positionsGrid.Controls.Add(positionDownsideValue, 3, 2);
            positionsGrid.Controls.Add(restingOrdersLabel, 0, 3);
            positionsGrid.Controls.Add(restingOrdersValue, 1, 3);
            positionsGrid.Dock = DockStyle.Fill;
            positionsGrid.Location = new Point(6, 6);
            positionsGrid.Margin = new Padding(4, 3, 4, 3);
            positionsGrid.Name = "positionsGrid";
            positionsGrid.RowCount = 4;
            positionsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            positionsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            positionsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            positionsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            positionsGrid.Size = new Size(579, 64);
            positionsGrid.TabIndex = 0;
            // 
            // positionSizeLabel
            // 
            positionSizeLabel.AutoSize = true;
            positionSizeLabel.Dock = DockStyle.Fill;
            positionSizeLabel.Location = new Point(4, 0);
            positionSizeLabel.Margin = new Padding(4, 0, 4, 0);
            positionSizeLabel.Name = "positionSizeLabel";
            positionSizeLabel.Size = new Size(136, 16);
            positionSizeLabel.TabIndex = 0;
            positionSizeLabel.Text = "Position Size:";
            positionSizeLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // positionSizeValue
            // 
            positionSizeValue.AutoSize = true;
            positionSizeValue.Dock = DockStyle.Fill;
            positionSizeValue.Location = new Point(148, 0);
            positionSizeValue.Margin = new Padding(4, 0, 4, 0);
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
            lastTradeLabel.Location = new Point(292, 0);
            lastTradeLabel.Margin = new Padding(4, 0, 4, 0);
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
            lastTradeValue.Location = new Point(436, 0);
            lastTradeValue.Margin = new Padding(4, 0, 4, 0);
            lastTradeValue.Name = "lastTradeValue";
            lastTradeValue.Size = new Size(139, 16);
            lastTradeValue.TabIndex = 3;
            lastTradeValue.Text = "--";
            lastTradeValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // positionRoiLabel
            // 
            positionRoiLabel.AutoSize = true;
            positionRoiLabel.Dock = DockStyle.Fill;
            positionRoiLabel.Location = new Point(4, 16);
            positionRoiLabel.Margin = new Padding(4, 0, 4, 0);
            positionRoiLabel.Name = "positionRoiLabel";
            positionRoiLabel.Size = new Size(136, 16);
            positionRoiLabel.TabIndex = 4;
            positionRoiLabel.Text = "Position ROI:";
            positionRoiLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // positionRoiValue
            // 
            positionRoiValue.AutoSize = true;
            positionRoiValue.Dock = DockStyle.Fill;
            positionRoiValue.Location = new Point(148, 16);
            positionRoiValue.Margin = new Padding(4, 0, 4, 0);
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
            buyinPriceLabel.Location = new Point(292, 16);
            buyinPriceLabel.Margin = new Padding(4, 0, 4, 0);
            buyinPriceLabel.Name = "buyinPriceLabel";
            buyinPriceLabel.Size = new Size(136, 16);
            buyinPriceLabel.TabIndex = 6;
            buyinPriceLabel.Text = "Buyin Price:";
            buyinPriceLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // buyinPriceValue
            // 
            buyinPriceValue.AutoSize = true;
            buyinPriceValue.Dock = DockStyle.Fill;
            buyinPriceValue.Location = new Point(436, 16);
            buyinPriceValue.Margin = new Padding(4, 0, 4, 0);
            buyinPriceValue.Name = "buyinPriceValue";
            buyinPriceValue.Size = new Size(139, 16);
            buyinPriceValue.TabIndex = 7;
            buyinPriceValue.Text = "--";
            buyinPriceValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // positionUpsideLabel
            // 
            positionUpsideLabel.AutoSize = true;
            positionUpsideLabel.Dock = DockStyle.Fill;
            positionUpsideLabel.Location = new Point(4, 32);
            positionUpsideLabel.Margin = new Padding(4, 0, 4, 0);
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
            positionUpsideValue.Location = new Point(148, 32);
            positionUpsideValue.Margin = new Padding(4, 0, 4, 0);
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
            positionDownsideLabel.Location = new Point(292, 32);
            positionDownsideLabel.Margin = new Padding(4, 0, 4, 0);
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
            positionDownsideValue.Location = new Point(436, 32);
            positionDownsideValue.Margin = new Padding(4, 0, 4, 0);
            positionDownsideValue.Name = "positionDownsideValue";
            positionDownsideValue.Size = new Size(139, 16);
            positionDownsideValue.TabIndex = 11;
            positionDownsideValue.Text = "--";
            positionDownsideValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // restingOrdersLabel
            // 
            restingOrdersLabel.AutoSize = true;
            restingOrdersLabel.Dock = DockStyle.Fill;
            restingOrdersLabel.Location = new Point(4, 48);
            restingOrdersLabel.Margin = new Padding(4, 0, 4, 0);
            restingOrdersLabel.Name = "restingOrdersLabel";
            restingOrdersLabel.Size = new Size(136, 16);
            restingOrdersLabel.TabIndex = 12;
            restingOrdersLabel.Text = "Resting Orders:";
            restingOrdersLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // restingOrdersValue
            // 
            restingOrdersValue.AutoSize = true;
            restingOrdersValue.Dock = DockStyle.Fill;
            restingOrdersValue.Location = new Point(148, 48);
            restingOrdersValue.Margin = new Padding(4, 0, 4, 0);
            restingOrdersValue.Name = "restingOrdersValue";
            restingOrdersValue.Size = new Size(136, 16);
            restingOrdersValue.TabIndex = 13;
            restingOrdersValue.Text = "--";
            restingOrdersValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // orderbookContainer
            // 
            orderbookContainer.BorderStyle = BorderStyle.FixedSingle;
            orderbookContainer.Controls.Add(orderbookGrid);
            orderbookContainer.Dock = DockStyle.Fill;
            orderbookContainer.Location = new Point(605, 565);
            orderbookContainer.Margin = new Padding(4, 3, 4, 3);
            orderbookContainer.Name = "orderbookContainer";
            orderbookContainer.Padding = new Padding(6);
            orderbookContainer.Size = new Size(316, 78);
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
            orderbookGrid.Size = new Size(302, 64);
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
            leftColumn.ResumeLayout(false);
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
            otherInfoGrid.ResumeLayout(false);
            otherInfoGrid.PerformLayout();
            flowMomentumGrid.ResumeLayout(false);
            flowMomentumGrid.PerformLayout();
            contextGrid.ResumeLayout(false);
            contextGrid.PerformLayout();
            positionsContainer.ResumeLayout(false);
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
        private Label pricesHeaderEmpty;
        private Label pricesHeaderNo;
        private Label pricesHeaderYes;
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
        private Label flowHeaderEmpty;
        private Label flowHeaderYes;
        private Label flowHeaderNo;
        private Label topVelocityLabel;
        private Label topVelocityYesValue;
        private Label topVelocityNoValue;
        private Label bottomVelocityLabel;
        private Label bottomVelocityYesValue;
        private Label bottomVelocityNoValue;
        private Label netOrderRateLabel;
        private Label netOrderRateYesValue;
        private Label netOrderRateNoValue;
        private Label tradeVolumeLabel;
        private Label tradeVolumeYesValue;
        private Label tradeVolumeNoValue;
        private Label avgTradeSizeLabel;
        private Label avgTradeSizeYesValue;
        private Label avgTradeSizeNoValue;
        private TableLayoutPanel contextGrid;
        private Label contextHeader;
        private Label contextHeaderEmpty;
        private Label contextHeaderYes;
        private Label contextHeaderNo;
        private Label spreadLabel;
        private Label spreadValue;
        private Label imbalLabel;
        private Label imbalValue;
        private Label depthTop4Label;
        private Label depthTop4YesValue;
        private Label depthTop4NoValue;
        private Label centerMassLabel;
        private Label centerMassYesValue;
        private Label centerMassNoValue;
        private Label totalContractsLabel;
        private Label totalContractsYesValue;
        private Label totalContractsNoValue;
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