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
            topVelocityLabel = new Label();
            topVelocityValue = new Label();
            bottomVelocityLabel = new Label();
            bottomVelocityValue = new Label();
            netOrderRateLabel = new Label();
            netOrderRateValue = new Label();
            tradeVolumeLabel = new Label();
            tradeVolumeValue = new Label();
            avgTradeSizeLabel = new Label();
            avgTradeSizeValue = new Label();
            contextGrid = new TableLayoutPanel();
            contextHeader = new Label();
            spreadLabel = new Label();
            spreadValue = new Label();
            imbalLabel = new Label();
            imbalValue = new Label();
            depthTop4Label = new Label();
            depthTop4Value = new Label();
            centerMassLabel = new Label();
            centerMassValue = new Label();
            totalContractsLabel = new Label();
            totalContractsValue = new Label();
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
            chartHeader = new Label();
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
            mainLayout.RowStyles.Add(new RowStyle());
            mainLayout.Size = new Size(933, 692);
            mainLayout.TabIndex = 0;
            // 
            // dashboardGrid
            // 
            dashboardGrid.ColumnCount = 2;
            dashboardGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64.97298F));
            dashboardGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35.0270271F));
            dashboardGrid.Controls.Add(chartContainer, 0, 0);
            dashboardGrid.Controls.Add(marketInfoContainer, 1, 0);
            dashboardGrid.Controls.Add(positionsContainer, 0, 1);
            dashboardGrid.Controls.Add(orderbookContainer, 1, 1);
            dashboardGrid.Dock = DockStyle.Fill;
            dashboardGrid.Location = new Point(4, 3);
            dashboardGrid.Margin = new Padding(4, 3, 4, 3);
            dashboardGrid.Name = "dashboardGrid";
            dashboardGrid.RowCount = 2;
            dashboardGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 87.34375F));
            dashboardGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.65625F));
            dashboardGrid.Size = new Size(925, 640);
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
            chartContainer.Padding = new Padding(6, 6, 6, 6);
            chartContainer.Size = new Size(593, 553);
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
            chartLayout.RowStyles.Add(new RowStyle());
            chartLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            chartLayout.Size = new Size(579, 539);
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
            chartControls.Size = new Size(571, 15);
            chartControls.TabIndex = 0;
            // 
            // priceChart
            // 
            priceChart.Dock = DockStyle.Fill;
            priceChart.Location = new Point(5, 24);
            priceChart.Margin = new Padding(5, 3, 5, 3);
            priceChart.Name = "priceChart";
            priceChart.Size = new Size(569, 512);
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
            marketInfoContainer.Padding = new Padding(6, 6, 6, 6);
            marketInfoContainer.Size = new Size(316, 553);
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
            infoGrid.Size = new Size(302, 539);
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
            leftColumn.Location = new Point(4, 3);
            leftColumn.Margin = new Padding(4, 3, 4, 3);
            leftColumn.Name = "leftColumn";
            leftColumn.RowCount = 2;
            leftColumn.RowStyles.Add(new RowStyle());
            leftColumn.RowStyles.Add(new RowStyle());
            leftColumn.Size = new Size(143, 533);
            leftColumn.TabIndex = 0;
            // 
            // pricesGrid
            // 
            pricesGrid.AutoSize = true;
            pricesGrid.ColumnCount = 3;
            pricesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pricesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pricesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pricesGrid.Controls.Add(allTimeHighLabel, 0, 0);
            pricesGrid.Controls.Add(allTimeHighAsk, 1, 0);
            pricesGrid.Controls.Add(allTimeHighBid, 2, 0);
            pricesGrid.Controls.Add(recentHighLabel, 0, 1);
            pricesGrid.Controls.Add(recentHighAsk, 1, 1);
            pricesGrid.Controls.Add(recentHighBid, 2, 1);
            pricesGrid.Controls.Add(currentPriceLabel, 0, 2);
            pricesGrid.Controls.Add(currentPriceAsk, 1, 2);
            pricesGrid.Controls.Add(currentPriceBid, 2, 2);
            pricesGrid.Controls.Add(recentLowLabel, 0, 3);
            pricesGrid.Controls.Add(recentLowAsk, 1, 3);
            pricesGrid.Controls.Add(recentLowBid, 2, 3);
            pricesGrid.Controls.Add(allTimeLowLabel, 0, 4);
            pricesGrid.Controls.Add(allTimeLowAsk, 1, 4);
            pricesGrid.Controls.Add(allTimeLowBid, 2, 4);
            pricesGrid.Dock = DockStyle.Top;
            pricesGrid.Location = new Point(4, 3);
            pricesGrid.Margin = new Padding(4, 3, 4, 3);
            pricesGrid.Name = "pricesGrid";
            pricesGrid.RowCount = 5;
            pricesGrid.RowStyles.Add(new RowStyle());
            pricesGrid.RowStyles.Add(new RowStyle());
            pricesGrid.RowStyles.Add(new RowStyle());
            pricesGrid.RowStyles.Add(new RowStyle());
            pricesGrid.RowStyles.Add(new RowStyle());
            pricesGrid.Size = new Size(135, 123);
            pricesGrid.TabIndex = 0;
            // 
            // allTimeHighLabel
            // 
            allTimeHighLabel.AutoSize = true;
            allTimeHighLabel.Dock = DockStyle.Fill;
            allTimeHighLabel.Font = new Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            allTimeHighLabel.Location = new Point(4, 0);
            allTimeHighLabel.Margin = new Padding(4, 0, 4, 0);
            allTimeHighLabel.Name = "allTimeHighLabel";
            allTimeHighLabel.Size = new Size(59, 21);
            allTimeHighLabel.TabIndex = 0;
            allTimeHighLabel.Text = "All Time High";
            allTimeHighLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // allTimeHighAsk
            // 
            allTimeHighAsk.AutoSize = true;
            allTimeHighAsk.Controls.Add(allTimeHighAskPrice);
            allTimeHighAsk.Controls.Add(allTimeHighAskTime);
            allTimeHighAsk.Dock = DockStyle.Fill;
            allTimeHighAsk.Location = new Point(71, 3);
            allTimeHighAsk.Margin = new Padding(4, 3, 4, 3);
            allTimeHighAsk.Name = "allTimeHighAsk";
            allTimeHighAsk.Size = new Size(25, 15);
            allTimeHighAsk.TabIndex = 1;
            // 
            // allTimeHighAskPrice
            // 
            allTimeHighAskPrice.AutoSize = true;
            allTimeHighAskPrice.Location = new Point(0, 0);
            allTimeHighAskPrice.Margin = new Padding(4, 0, 4, 0);
            allTimeHighAskPrice.Name = "allTimeHighAskPrice";
            allTimeHighAskPrice.Size = new Size(17, 15);
            allTimeHighAskPrice.TabIndex = 0;
            allTimeHighAskPrice.Text = "--";
            allTimeHighAskPrice.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // allTimeHighAskTime
            // 
            allTimeHighAskTime.AutoSize = true;
            allTimeHighAskTime.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            allTimeHighAskTime.Location = new Point(0, 0);
            allTimeHighAskTime.Margin = new Padding(4, 0, 4, 0);
            allTimeHighAskTime.Name = "allTimeHighAskTime";
            allTimeHighAskTime.Size = new Size(17, 15);
            allTimeHighAskTime.TabIndex = 1;
            allTimeHighAskTime.Text = "--";
            allTimeHighAskTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // allTimeHighBid
            // 
            allTimeHighBid.AutoSize = true;
            allTimeHighBid.Controls.Add(allTimeHighBidPrice);
            allTimeHighBid.Controls.Add(allTimeHighBidTime);
            allTimeHighBid.Dock = DockStyle.Fill;
            allTimeHighBid.Location = new Point(104, 3);
            allTimeHighBid.Margin = new Padding(4, 3, 4, 3);
            allTimeHighBid.Name = "allTimeHighBid";
            allTimeHighBid.Size = new Size(27, 15);
            allTimeHighBid.TabIndex = 2;
            // 
            // allTimeHighBidPrice
            // 
            allTimeHighBidPrice.AutoSize = true;
            allTimeHighBidPrice.Location = new Point(0, 0);
            allTimeHighBidPrice.Margin = new Padding(4, 0, 4, 0);
            allTimeHighBidPrice.Name = "allTimeHighBidPrice";
            allTimeHighBidPrice.Size = new Size(17, 15);
            allTimeHighBidPrice.TabIndex = 0;
            allTimeHighBidPrice.Text = "--";
            allTimeHighBidPrice.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // allTimeHighBidTime
            // 
            allTimeHighBidTime.AutoSize = true;
            allTimeHighBidTime.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            allTimeHighBidTime.Location = new Point(0, 0);
            allTimeHighBidTime.Margin = new Padding(4, 0, 4, 0);
            allTimeHighBidTime.Name = "allTimeHighBidTime";
            allTimeHighBidTime.Size = new Size(17, 15);
            allTimeHighBidTime.TabIndex = 1;
            allTimeHighBidTime.Text = "--";
            allTimeHighBidTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // recentHighLabel
            // 
            recentHighLabel.AutoSize = true;
            recentHighLabel.Dock = DockStyle.Fill;
            recentHighLabel.Font = new Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            recentHighLabel.Location = new Point(4, 21);
            recentHighLabel.Margin = new Padding(4, 0, 4, 0);
            recentHighLabel.Name = "recentHighLabel";
            recentHighLabel.Size = new Size(59, 21);
            recentHighLabel.TabIndex = 3;
            recentHighLabel.Text = "Recent High";
            recentHighLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // recentHighAsk
            // 
            recentHighAsk.AutoSize = true;
            recentHighAsk.Controls.Add(recentHighAskPrice);
            recentHighAsk.Controls.Add(recentHighAskTime);
            recentHighAsk.Dock = DockStyle.Fill;
            recentHighAsk.Location = new Point(71, 24);
            recentHighAsk.Margin = new Padding(4, 3, 4, 3);
            recentHighAsk.Name = "recentHighAsk";
            recentHighAsk.Size = new Size(25, 15);
            recentHighAsk.TabIndex = 4;
            // 
            // recentHighAskPrice
            // 
            recentHighAskPrice.AutoSize = true;
            recentHighAskPrice.Location = new Point(0, 0);
            recentHighAskPrice.Margin = new Padding(4, 0, 4, 0);
            recentHighAskPrice.Name = "recentHighAskPrice";
            recentHighAskPrice.Size = new Size(17, 15);
            recentHighAskPrice.TabIndex = 0;
            recentHighAskPrice.Text = "--";
            recentHighAskPrice.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // recentHighAskTime
            // 
            recentHighAskTime.AutoSize = true;
            recentHighAskTime.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            recentHighAskTime.Location = new Point(0, 0);
            recentHighAskTime.Margin = new Padding(4, 0, 4, 0);
            recentHighAskTime.Name = "recentHighAskTime";
            recentHighAskTime.Size = new Size(17, 15);
            recentHighAskTime.TabIndex = 1;
            recentHighAskTime.Text = "--";
            recentHighAskTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // recentHighBid
            // 
            recentHighBid.AutoSize = true;
            recentHighBid.Controls.Add(recentHighBidPrice);
            recentHighBid.Controls.Add(recentHighBidTime);
            recentHighBid.Dock = DockStyle.Fill;
            recentHighBid.Location = new Point(104, 24);
            recentHighBid.Margin = new Padding(4, 3, 4, 3);
            recentHighBid.Name = "recentHighBid";
            recentHighBid.Size = new Size(27, 15);
            recentHighBid.TabIndex = 5;
            // 
            // recentHighBidPrice
            // 
            recentHighBidPrice.AutoSize = true;
            recentHighBidPrice.Location = new Point(0, 0);
            recentHighBidPrice.Margin = new Padding(4, 0, 4, 0);
            recentHighBidPrice.Name = "recentHighBidPrice";
            recentHighBidPrice.Size = new Size(17, 15);
            recentHighBidPrice.TabIndex = 0;
            recentHighBidPrice.Text = "--";
            recentHighBidPrice.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // recentHighBidTime
            // 
            recentHighBidTime.AutoSize = true;
            recentHighBidTime.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            recentHighBidTime.Location = new Point(0, 0);
            recentHighBidTime.Margin = new Padding(4, 0, 4, 0);
            recentHighBidTime.Name = "recentHighBidTime";
            recentHighBidTime.Size = new Size(17, 15);
            recentHighBidTime.TabIndex = 1;
            recentHighBidTime.Text = "--";
            recentHighBidTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // currentPriceLabel
            // 
            currentPriceLabel.AutoSize = true;
            currentPriceLabel.Dock = DockStyle.Fill;
            currentPriceLabel.Font = new Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            currentPriceLabel.Location = new Point(4, 42);
            currentPriceLabel.Margin = new Padding(4, 0, 4, 0);
            currentPriceLabel.Name = "currentPriceLabel";
            currentPriceLabel.Size = new Size(59, 15);
            currentPriceLabel.TabIndex = 6;
            currentPriceLabel.Text = "Current Price";
            currentPriceLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // currentPriceAsk
            // 
            currentPriceAsk.AutoSize = true;
            currentPriceAsk.Dock = DockStyle.Fill;
            currentPriceAsk.Location = new Point(71, 42);
            currentPriceAsk.Margin = new Padding(4, 0, 4, 0);
            currentPriceAsk.Name = "currentPriceAsk";
            currentPriceAsk.Size = new Size(25, 15);
            currentPriceAsk.TabIndex = 7;
            currentPriceAsk.Text = "--";
            currentPriceAsk.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // currentPriceBid
            // 
            currentPriceBid.AutoSize = true;
            currentPriceBid.Dock = DockStyle.Fill;
            currentPriceBid.Location = new Point(104, 42);
            currentPriceBid.Margin = new Padding(4, 0, 4, 0);
            currentPriceBid.Name = "currentPriceBid";
            currentPriceBid.Size = new Size(27, 15);
            currentPriceBid.TabIndex = 8;
            currentPriceBid.Text = "--";
            currentPriceBid.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // recentLowLabel
            // 
            recentLowLabel.AutoSize = true;
            recentLowLabel.Dock = DockStyle.Fill;
            recentLowLabel.Font = new Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            recentLowLabel.Location = new Point(4, 57);
            recentLowLabel.Margin = new Padding(4, 0, 4, 0);
            recentLowLabel.Name = "recentLowLabel";
            recentLowLabel.Size = new Size(59, 21);
            recentLowLabel.TabIndex = 9;
            recentLowLabel.Text = "Recent Low";
            recentLowLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // recentLowAsk
            // 
            recentLowAsk.AutoSize = true;
            recentLowAsk.Controls.Add(recentLowAskPrice);
            recentLowAsk.Controls.Add(recentLowAskTime);
            recentLowAsk.Dock = DockStyle.Fill;
            recentLowAsk.Location = new Point(71, 60);
            recentLowAsk.Margin = new Padding(4, 3, 4, 3);
            recentLowAsk.Name = "recentLowAsk";
            recentLowAsk.Size = new Size(25, 15);
            recentLowAsk.TabIndex = 10;
            // 
            // recentLowAskPrice
            // 
            recentLowAskPrice.AutoSize = true;
            recentLowAskPrice.Location = new Point(0, 0);
            recentLowAskPrice.Margin = new Padding(4, 0, 4, 0);
            recentLowAskPrice.Name = "recentLowAskPrice";
            recentLowAskPrice.Size = new Size(17, 15);
            recentLowAskPrice.TabIndex = 0;
            recentLowAskPrice.Text = "--";
            recentLowAskPrice.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // recentLowAskTime
            // 
            recentLowAskTime.AutoSize = true;
            recentLowAskTime.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            recentLowAskTime.Location = new Point(0, 0);
            recentLowAskTime.Margin = new Padding(4, 0, 4, 0);
            recentLowAskTime.Name = "recentLowAskTime";
            recentLowAskTime.Size = new Size(17, 15);
            recentLowAskTime.TabIndex = 1;
            recentLowAskTime.Text = "--";
            recentLowAskTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // recentLowBid
            // 
            recentLowBid.AutoSize = true;
            recentLowBid.Controls.Add(recentLowBidPrice);
            recentLowBid.Controls.Add(recentLowBidTime);
            recentLowBid.Dock = DockStyle.Fill;
            recentLowBid.Location = new Point(104, 60);
            recentLowBid.Margin = new Padding(4, 3, 4, 3);
            recentLowBid.Name = "recentLowBid";
            recentLowBid.Size = new Size(27, 15);
            recentLowBid.TabIndex = 11;
            // 
            // recentLowBidPrice
            // 
            recentLowBidPrice.AutoSize = true;
            recentLowBidPrice.Location = new Point(0, 0);
            recentLowBidPrice.Margin = new Padding(4, 0, 4, 0);
            recentLowBidPrice.Name = "recentLowBidPrice";
            recentLowBidPrice.Size = new Size(17, 15);
            recentLowBidPrice.TabIndex = 0;
            recentLowBidPrice.Text = "--";
            recentLowBidPrice.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // recentLowBidTime
            // 
            recentLowBidTime.AutoSize = true;
            recentLowBidTime.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            recentLowBidTime.Location = new Point(0, 0);
            recentLowBidTime.Margin = new Padding(4, 0, 4, 0);
            recentLowBidTime.Name = "recentLowBidTime";
            recentLowBidTime.Size = new Size(17, 15);
            recentLowBidTime.TabIndex = 1;
            recentLowBidTime.Text = "--";
            recentLowBidTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // allTimeLowLabel
            // 
            allTimeLowLabel.AutoSize = true;
            allTimeLowLabel.Dock = DockStyle.Fill;
            allTimeLowLabel.Location = new Point(4, 78);
            allTimeLowLabel.Margin = new Padding(4, 0, 4, 0);
            allTimeLowLabel.Name = "allTimeLowLabel";
            allTimeLowLabel.Size = new Size(59, 45);
            allTimeLowLabel.TabIndex = 12;
            allTimeLowLabel.Text = "All Time Low";
            allTimeLowLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // allTimeLowAsk
            // 
            allTimeLowAsk.AutoSize = true;
            allTimeLowAsk.Controls.Add(allTimeLowAskPrice);
            allTimeLowAsk.Controls.Add(allTimeLowAskTime);
            allTimeLowAsk.Dock = DockStyle.Fill;
            allTimeLowAsk.Location = new Point(71, 81);
            allTimeLowAsk.Margin = new Padding(4, 3, 4, 3);
            allTimeLowAsk.Name = "allTimeLowAsk";
            allTimeLowAsk.Size = new Size(25, 39);
            allTimeLowAsk.TabIndex = 13;
            // 
            // allTimeLowAskPrice
            // 
            allTimeLowAskPrice.AutoSize = true;
            allTimeLowAskPrice.Location = new Point(0, 0);
            allTimeLowAskPrice.Margin = new Padding(4, 0, 4, 0);
            allTimeLowAskPrice.Name = "allTimeLowAskPrice";
            allTimeLowAskPrice.Size = new Size(17, 15);
            allTimeLowAskPrice.TabIndex = 0;
            allTimeLowAskPrice.Text = "--";
            allTimeLowAskPrice.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // allTimeLowAskTime
            // 
            allTimeLowAskTime.AutoSize = true;
            allTimeLowAskTime.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            allTimeLowAskTime.Location = new Point(0, 0);
            allTimeLowAskTime.Margin = new Padding(4, 0, 4, 0);
            allTimeLowAskTime.Name = "allTimeLowAskTime";
            allTimeLowAskTime.Size = new Size(17, 15);
            allTimeLowAskTime.TabIndex = 1;
            allTimeLowAskTime.Text = "--";
            allTimeLowAskTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // allTimeLowBid
            // 
            allTimeLowBid.AutoSize = true;
            allTimeLowBid.Controls.Add(allTimeLowBidPrice);
            allTimeLowBid.Controls.Add(allTimeLowBidTime);
            allTimeLowBid.Dock = DockStyle.Fill;
            allTimeLowBid.Location = new Point(104, 81);
            allTimeLowBid.Margin = new Padding(4, 3, 4, 3);
            allTimeLowBid.Name = "allTimeLowBid";
            allTimeLowBid.Size = new Size(27, 39);
            allTimeLowBid.TabIndex = 14;
            // 
            // allTimeLowBidPrice
            // 
            allTimeLowBidPrice.AutoSize = true;
            allTimeLowBidPrice.Location = new Point(0, 0);
            allTimeLowBidPrice.Margin = new Padding(4, 0, 4, 0);
            allTimeLowBidPrice.Name = "allTimeLowBidPrice";
            allTimeLowBidPrice.Size = new Size(17, 15);
            allTimeLowBidPrice.TabIndex = 0;
            allTimeLowBidPrice.Text = "--";
            allTimeLowBidPrice.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // allTimeLowBidTime
            // 
            allTimeLowBidTime.AutoSize = true;
            allTimeLowBidTime.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            allTimeLowBidTime.Location = new Point(0, 0);
            allTimeLowBidTime.Margin = new Padding(4, 0, 4, 0);
            allTimeLowBidTime.Name = "allTimeLowBidTime";
            allTimeLowBidTime.Size = new Size(17, 15);
            allTimeLowBidTime.TabIndex = 1;
            allTimeLowBidTime.Text = "--";
            allTimeLowBidTime.TextAlign = ContentAlignment.MiddleCenter;
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
            tradingMetricsGrid.Dock = DockStyle.Top;
            tradingMetricsGrid.Location = new Point(4, 132);
            tradingMetricsGrid.Margin = new Padding(4, 3, 4, 3);
            tradingMetricsGrid.Name = "tradingMetricsGrid";
            tradingMetricsGrid.RowCount = 9;
            tradingMetricsGrid.RowStyles.Add(new RowStyle());
            tradingMetricsGrid.RowStyles.Add(new RowStyle());
            tradingMetricsGrid.RowStyles.Add(new RowStyle());
            tradingMetricsGrid.RowStyles.Add(new RowStyle());
            tradingMetricsGrid.RowStyles.Add(new RowStyle());
            tradingMetricsGrid.RowStyles.Add(new RowStyle());
            tradingMetricsGrid.RowStyles.Add(new RowStyle());
            tradingMetricsGrid.RowStyles.Add(new RowStyle());
            tradingMetricsGrid.RowStyles.Add(new RowStyle());
            tradingMetricsGrid.Size = new Size(135, 225);
            tradingMetricsGrid.TabIndex = 1;
            // 
            // tradingMetricsHeader
            // 
            tradingMetricsHeader.AutoSize = true;
            tradingMetricsGrid.SetColumnSpan(tradingMetricsHeader, 2);
            tradingMetricsHeader.Dock = DockStyle.Fill;
            tradingMetricsHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            tradingMetricsHeader.Location = new Point(4, 0);
            tradingMetricsHeader.Margin = new Padding(4, 0, 4, 0);
            tradingMetricsHeader.Name = "tradingMetricsHeader";
            tradingMetricsHeader.Size = new Size(127, 15);
            tradingMetricsHeader.TabIndex = 0;
            tradingMetricsHeader.Text = "Trading Metrics";
            // 
            // rsiLabel
            // 
            rsiLabel.AutoSize = true;
            rsiLabel.Dock = DockStyle.Fill;
            rsiLabel.Location = new Point(4, 15);
            rsiLabel.Margin = new Padding(4, 0, 4, 0);
            rsiLabel.Name = "rsiLabel";
            rsiLabel.Size = new Size(59, 15);
            rsiLabel.TabIndex = 0;
            rsiLabel.Text = "RSI";
            rsiLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // rsiValue
            // 
            rsiValue.AutoSize = true;
            rsiValue.Dock = DockStyle.Fill;
            rsiValue.Location = new Point(71, 15);
            rsiValue.Margin = new Padding(4, 0, 4, 0);
            rsiValue.Name = "rsiValue";
            rsiValue.Size = new Size(60, 15);
            rsiValue.TabIndex = 1;
            rsiValue.Text = "--";
            rsiValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // macdLabel
            // 
            macdLabel.AutoSize = true;
            macdLabel.Dock = DockStyle.Fill;
            macdLabel.Location = new Point(4, 30);
            macdLabel.Margin = new Padding(4, 0, 4, 0);
            macdLabel.Name = "macdLabel";
            macdLabel.Size = new Size(59, 15);
            macdLabel.TabIndex = 2;
            macdLabel.Text = "MACD";
            macdLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // macdValue
            // 
            macdValue.AutoSize = true;
            macdValue.Dock = DockStyle.Fill;
            macdValue.Location = new Point(71, 30);
            macdValue.Margin = new Padding(4, 0, 4, 0);
            macdValue.Name = "macdValue";
            macdValue.Size = new Size(60, 15);
            macdValue.TabIndex = 3;
            macdValue.Text = "--";
            macdValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // emaLabel
            // 
            emaLabel.AutoSize = true;
            emaLabel.Dock = DockStyle.Fill;
            emaLabel.Location = new Point(4, 45);
            emaLabel.Margin = new Padding(4, 0, 4, 0);
            emaLabel.Name = "emaLabel";
            emaLabel.Size = new Size(59, 15);
            emaLabel.TabIndex = 4;
            emaLabel.Text = "EMA";
            emaLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // emaValue
            // 
            emaValue.AutoSize = true;
            emaValue.Dock = DockStyle.Fill;
            emaValue.Location = new Point(71, 45);
            emaValue.Margin = new Padding(4, 0, 4, 0);
            emaValue.Name = "emaValue";
            emaValue.Size = new Size(60, 15);
            emaValue.TabIndex = 5;
            emaValue.Text = "--";
            emaValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // bollingerLabel
            // 
            bollingerLabel.AutoSize = true;
            bollingerLabel.Dock = DockStyle.Fill;
            bollingerLabel.Location = new Point(4, 60);
            bollingerLabel.Margin = new Padding(4, 0, 4, 0);
            bollingerLabel.Name = "bollingerLabel";
            bollingerLabel.Size = new Size(59, 30);
            bollingerLabel.TabIndex = 6;
            bollingerLabel.Text = "Bollinger Bands";
            bollingerLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // bollingerValue
            // 
            bollingerValue.AutoSize = true;
            bollingerValue.Dock = DockStyle.Fill;
            bollingerValue.Location = new Point(71, 60);
            bollingerValue.Margin = new Padding(4, 0, 4, 0);
            bollingerValue.Name = "bollingerValue";
            bollingerValue.Size = new Size(60, 30);
            bollingerValue.TabIndex = 7;
            bollingerValue.Text = "--";
            bollingerValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // atrLabel
            // 
            atrLabel.AutoSize = true;
            atrLabel.Dock = DockStyle.Fill;
            atrLabel.Location = new Point(4, 90);
            atrLabel.Margin = new Padding(4, 0, 4, 0);
            atrLabel.Name = "atrLabel";
            atrLabel.Size = new Size(59, 15);
            atrLabel.TabIndex = 8;
            atrLabel.Text = "ATR";
            atrLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // atrValue
            // 
            atrValue.AutoSize = true;
            atrValue.Dock = DockStyle.Fill;
            atrValue.Location = new Point(71, 90);
            atrValue.Margin = new Padding(4, 0, 4, 0);
            atrValue.Name = "atrValue";
            atrValue.Size = new Size(60, 15);
            atrValue.TabIndex = 9;
            atrValue.Text = "--";
            atrValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // vwapLabel
            // 
            vwapLabel.AutoSize = true;
            vwapLabel.Dock = DockStyle.Fill;
            vwapLabel.Location = new Point(4, 105);
            vwapLabel.Margin = new Padding(4, 0, 4, 0);
            vwapLabel.Name = "vwapLabel";
            vwapLabel.Size = new Size(59, 15);
            vwapLabel.TabIndex = 10;
            vwapLabel.Text = "VWAP";
            vwapLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // vwapValue
            // 
            vwapValue.AutoSize = true;
            vwapValue.Dock = DockStyle.Fill;
            vwapValue.Location = new Point(71, 105);
            vwapValue.Margin = new Padding(4, 0, 4, 0);
            vwapValue.Name = "vwapValue";
            vwapValue.Size = new Size(60, 15);
            vwapValue.TabIndex = 11;
            vwapValue.Text = "--";
            vwapValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // stochasticLabel
            // 
            stochasticLabel.AutoSize = true;
            stochasticLabel.Dock = DockStyle.Fill;
            stochasticLabel.Location = new Point(4, 120);
            stochasticLabel.Margin = new Padding(4, 0, 4, 0);
            stochasticLabel.Name = "stochasticLabel";
            stochasticLabel.Size = new Size(59, 45);
            stochasticLabel.TabIndex = 12;
            stochasticLabel.Text = "Stochastic Oscillator";
            stochasticLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // stochasticValue
            // 
            stochasticValue.AutoSize = true;
            stochasticValue.Dock = DockStyle.Fill;
            stochasticValue.Location = new Point(71, 120);
            stochasticValue.Margin = new Padding(4, 0, 4, 0);
            stochasticValue.Name = "stochasticValue";
            stochasticValue.Size = new Size(60, 45);
            stochasticValue.TabIndex = 13;
            stochasticValue.Text = "--";
            stochasticValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // obvLabel
            // 
            obvLabel.AutoSize = true;
            obvLabel.Dock = DockStyle.Fill;
            obvLabel.Location = new Point(4, 165);
            obvLabel.Margin = new Padding(4, 0, 4, 0);
            obvLabel.Name = "obvLabel";
            obvLabel.Size = new Size(59, 60);
            obvLabel.TabIndex = 14;
            obvLabel.Text = "OBV";
            obvLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // obvValue
            // 
            obvValue.AutoSize = true;
            obvValue.Dock = DockStyle.Fill;
            obvValue.Location = new Point(71, 165);
            obvValue.Margin = new Padding(4, 0, 4, 0);
            obvValue.Name = "obvValue";
            obvValue.Size = new Size(60, 60);
            obvValue.TabIndex = 15;
            obvValue.Text = "--";
            obvValue.TextAlign = ContentAlignment.MiddleRight;
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
            rightColumn.Location = new Point(155, 3);
            rightColumn.Margin = new Padding(4, 3, 4, 3);
            rightColumn.Name = "rightColumn";
            rightColumn.RowCount = 3;
            rightColumn.RowStyles.Add(new RowStyle());
            rightColumn.RowStyles.Add(new RowStyle());
            rightColumn.RowStyles.Add(new RowStyle());
            rightColumn.Size = new Size(143, 533);
            rightColumn.TabIndex = 1;
            // 
            // otherInfoGrid
            // 
            otherInfoGrid.AutoSize = true;
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
            otherInfoGrid.Dock = DockStyle.Top;
            otherInfoGrid.Location = new Point(4, 3);
            otherInfoGrid.Margin = new Padding(4, 3, 4, 3);
            otherInfoGrid.Name = "otherInfoGrid";
            otherInfoGrid.RowCount = 7;
            otherInfoGrid.RowStyles.Add(new RowStyle());
            otherInfoGrid.RowStyles.Add(new RowStyle());
            otherInfoGrid.RowStyles.Add(new RowStyle());
            otherInfoGrid.RowStyles.Add(new RowStyle());
            otherInfoGrid.RowStyles.Add(new RowStyle());
            otherInfoGrid.RowStyles.Add(new RowStyle());
            otherInfoGrid.RowStyles.Add(new RowStyle());
            otherInfoGrid.Size = new Size(135, 180);
            otherInfoGrid.TabIndex = 0;
            // 
            // titleLabel
            // 
            titleLabel.AutoSize = true;
            titleLabel.Dock = DockStyle.Fill;
            titleLabel.Location = new Point(4, 0);
            titleLabel.Margin = new Padding(4, 0, 4, 0);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new Size(59, 15);
            titleLabel.TabIndex = 0;
            titleLabel.Text = "Title";
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // titleValue
            // 
            titleValue.AutoSize = true;
            titleValue.Dock = DockStyle.Fill;
            titleValue.Location = new Point(71, 0);
            titleValue.Margin = new Padding(4, 0, 4, 0);
            titleValue.Name = "titleValue";
            titleValue.Size = new Size(60, 15);
            titleValue.TabIndex = 1;
            titleValue.Text = "--";
            titleValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // subtitleLabel
            // 
            subtitleLabel.AutoSize = true;
            subtitleLabel.Dock = DockStyle.Fill;
            subtitleLabel.Location = new Point(4, 15);
            subtitleLabel.Margin = new Padding(4, 0, 4, 0);
            subtitleLabel.Name = "subtitleLabel";
            subtitleLabel.Size = new Size(59, 15);
            subtitleLabel.TabIndex = 2;
            subtitleLabel.Text = "Subtitle";
            subtitleLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // subtitleValue
            // 
            subtitleValue.AutoSize = true;
            subtitleValue.Dock = DockStyle.Fill;
            subtitleValue.Location = new Point(71, 15);
            subtitleValue.Margin = new Padding(4, 0, 4, 0);
            subtitleValue.Name = "subtitleValue";
            subtitleValue.Size = new Size(60, 15);
            subtitleValue.TabIndex = 3;
            subtitleValue.Text = "--";
            subtitleValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // marketTypeLabel
            // 
            marketTypeLabel.AutoSize = true;
            marketTypeLabel.Dock = DockStyle.Fill;
            marketTypeLabel.Location = new Point(4, 30);
            marketTypeLabel.Margin = new Padding(4, 0, 4, 0);
            marketTypeLabel.Name = "marketTypeLabel";
            marketTypeLabel.Size = new Size(59, 30);
            marketTypeLabel.TabIndex = 4;
            marketTypeLabel.Text = "Market Type";
            marketTypeLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // marketTypeValue
            // 
            marketTypeValue.AutoSize = true;
            marketTypeValue.Dock = DockStyle.Fill;
            marketTypeValue.Location = new Point(71, 30);
            marketTypeValue.Margin = new Padding(4, 0, 4, 0);
            marketTypeValue.Name = "marketTypeValue";
            marketTypeValue.Size = new Size(60, 30);
            marketTypeValue.TabIndex = 5;
            marketTypeValue.Text = "--";
            marketTypeValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // priceGoodBadLabel
            // 
            priceGoodBadLabel.AutoSize = true;
            priceGoodBadLabel.Dock = DockStyle.Fill;
            priceGoodBadLabel.Location = new Point(4, 60);
            priceGoodBadLabel.Margin = new Padding(4, 0, 4, 0);
            priceGoodBadLabel.Name = "priceGoodBadLabel";
            priceGoodBadLabel.Size = new Size(59, 45);
            priceGoodBadLabel.TabIndex = 6;
            priceGoodBadLabel.Text = "Price Good/Bad";
            priceGoodBadLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // priceGoodBadValue
            // 
            priceGoodBadValue.AutoSize = true;
            priceGoodBadValue.Dock = DockStyle.Fill;
            priceGoodBadValue.Location = new Point(71, 60);
            priceGoodBadValue.Margin = new Padding(4, 0, 4, 0);
            priceGoodBadValue.Name = "priceGoodBadValue";
            priceGoodBadValue.Size = new Size(60, 45);
            priceGoodBadValue.TabIndex = 7;
            priceGoodBadValue.Text = "--";
            priceGoodBadValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // marketBehaviorLabel
            // 
            marketBehaviorLabel.AutoSize = true;
            marketBehaviorLabel.Dock = DockStyle.Fill;
            marketBehaviorLabel.Location = new Point(4, 105);
            marketBehaviorLabel.Margin = new Padding(4, 0, 4, 0);
            marketBehaviorLabel.Name = "marketBehaviorLabel";
            marketBehaviorLabel.Size = new Size(59, 30);
            marketBehaviorLabel.TabIndex = 8;
            marketBehaviorLabel.Text = "Market Behavior";
            marketBehaviorLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // marketBehaviorValue
            // 
            marketBehaviorValue.AutoSize = true;
            marketBehaviorValue.Dock = DockStyle.Fill;
            marketBehaviorValue.Location = new Point(71, 105);
            marketBehaviorValue.Margin = new Padding(4, 0, 4, 0);
            marketBehaviorValue.Name = "marketBehaviorValue";
            marketBehaviorValue.Size = new Size(60, 30);
            marketBehaviorValue.TabIndex = 9;
            marketBehaviorValue.Text = "--";
            marketBehaviorValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // timeLeftLabel
            // 
            timeLeftLabel.AutoSize = true;
            timeLeftLabel.Dock = DockStyle.Fill;
            timeLeftLabel.Location = new Point(4, 135);
            timeLeftLabel.Margin = new Padding(4, 0, 4, 0);
            timeLeftLabel.Name = "timeLeftLabel";
            timeLeftLabel.Size = new Size(59, 15);
            timeLeftLabel.TabIndex = 10;
            timeLeftLabel.Text = "Time Left";
            timeLeftLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // timeLeftValue
            // 
            timeLeftValue.AutoSize = true;
            timeLeftValue.Dock = DockStyle.Fill;
            timeLeftValue.Location = new Point(71, 135);
            timeLeftValue.Margin = new Padding(4, 0, 4, 0);
            timeLeftValue.Name = "timeLeftValue";
            timeLeftValue.Size = new Size(60, 15);
            timeLeftValue.TabIndex = 11;
            timeLeftValue.Text = "--";
            timeLeftValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // marketAgeLabel
            // 
            marketAgeLabel.AutoSize = true;
            marketAgeLabel.Dock = DockStyle.Fill;
            marketAgeLabel.Location = new Point(4, 150);
            marketAgeLabel.Margin = new Padding(4, 0, 4, 0);
            marketAgeLabel.Name = "marketAgeLabel";
            marketAgeLabel.Size = new Size(59, 30);
            marketAgeLabel.TabIndex = 12;
            marketAgeLabel.Text = "Market Age";
            marketAgeLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // marketAgeValue
            // 
            marketAgeValue.AutoSize = true;
            marketAgeValue.Dock = DockStyle.Fill;
            marketAgeValue.Location = new Point(71, 150);
            marketAgeValue.Margin = new Padding(4, 0, 4, 0);
            marketAgeValue.Name = "marketAgeValue";
            marketAgeValue.Size = new Size(60, 30);
            marketAgeValue.TabIndex = 13;
            marketAgeValue.Text = "--";
            marketAgeValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // flowMomentumGrid
            // 
            flowMomentumGrid.AutoSize = true;
            flowMomentumGrid.ColumnCount = 2;
            flowMomentumGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            flowMomentumGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            flowMomentumGrid.Controls.Add(flowHeader, 0, 0);
            flowMomentumGrid.Controls.Add(topVelocityLabel, 0, 1);
            flowMomentumGrid.Controls.Add(topVelocityValue, 1, 1);
            flowMomentumGrid.Controls.Add(bottomVelocityLabel, 0, 2);
            flowMomentumGrid.Controls.Add(bottomVelocityValue, 1, 2);
            flowMomentumGrid.Controls.Add(netOrderRateLabel, 0, 3);
            flowMomentumGrid.Controls.Add(netOrderRateValue, 1, 3);
            flowMomentumGrid.Controls.Add(tradeVolumeLabel, 0, 4);
            flowMomentumGrid.Controls.Add(tradeVolumeValue, 1, 4);
            flowMomentumGrid.Controls.Add(avgTradeSizeLabel, 0, 5);
            flowMomentumGrid.Controls.Add(avgTradeSizeValue, 1, 5);
            flowMomentumGrid.Dock = DockStyle.Top;
            flowMomentumGrid.Location = new Point(4, 189);
            flowMomentumGrid.Margin = new Padding(4, 3, 4, 3);
            flowMomentumGrid.Name = "flowMomentumGrid";
            flowMomentumGrid.RowCount = 6;
            flowMomentumGrid.RowStyles.Add(new RowStyle());
            flowMomentumGrid.RowStyles.Add(new RowStyle());
            flowMomentumGrid.RowStyles.Add(new RowStyle());
            flowMomentumGrid.RowStyles.Add(new RowStyle());
            flowMomentumGrid.RowStyles.Add(new RowStyle());
            flowMomentumGrid.RowStyles.Add(new RowStyle());
            flowMomentumGrid.Size = new Size(135, 195);
            flowMomentumGrid.TabIndex = 1;
            // 
            // flowHeader
            // 
            flowHeader.AutoSize = true;
            flowMomentumGrid.SetColumnSpan(flowHeader, 2);
            flowHeader.Dock = DockStyle.Fill;
            flowHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            flowHeader.Location = new Point(4, 0);
            flowHeader.Margin = new Padding(4, 0, 4, 0);
            flowHeader.Name = "flowHeader";
            flowHeader.Size = new Size(127, 15);
            flowHeader.TabIndex = 0;
            flowHeader.Text = "Flow & Momentum";
            // 
            // topVelocityLabel
            // 
            topVelocityLabel.AutoSize = true;
            topVelocityLabel.Dock = DockStyle.Fill;
            topVelocityLabel.Location = new Point(4, 15);
            topVelocityLabel.Margin = new Padding(4, 0, 4, 0);
            topVelocityLabel.Name = "topVelocityLabel";
            topVelocityLabel.Size = new Size(59, 30);
            topVelocityLabel.TabIndex = 0;
            topVelocityLabel.Text = "Top Velocity";
            topVelocityLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // topVelocityValue
            // 
            topVelocityValue.AutoSize = true;
            topVelocityValue.Dock = DockStyle.Fill;
            topVelocityValue.Location = new Point(71, 15);
            topVelocityValue.Margin = new Padding(4, 0, 4, 0);
            topVelocityValue.Name = "topVelocityValue";
            topVelocityValue.Size = new Size(60, 30);
            topVelocityValue.TabIndex = 1;
            topVelocityValue.Text = "-- (--)";
            topVelocityValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // bottomVelocityLabel
            // 
            bottomVelocityLabel.AutoSize = true;
            bottomVelocityLabel.Dock = DockStyle.Fill;
            bottomVelocityLabel.Location = new Point(4, 45);
            bottomVelocityLabel.Margin = new Padding(4, 0, 4, 0);
            bottomVelocityLabel.Name = "bottomVelocityLabel";
            bottomVelocityLabel.Size = new Size(59, 30);
            bottomVelocityLabel.TabIndex = 2;
            bottomVelocityLabel.Text = "Bottom Velocity";
            bottomVelocityLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // bottomVelocityValue
            // 
            bottomVelocityValue.AutoSize = true;
            bottomVelocityValue.Dock = DockStyle.Fill;
            bottomVelocityValue.Location = new Point(71, 45);
            bottomVelocityValue.Margin = new Padding(4, 0, 4, 0);
            bottomVelocityValue.Name = "bottomVelocityValue";
            bottomVelocityValue.Size = new Size(60, 30);
            bottomVelocityValue.TabIndex = 3;
            bottomVelocityValue.Text = "-- (--)";
            bottomVelocityValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // netOrderRateLabel
            // 
            netOrderRateLabel.AutoSize = true;
            netOrderRateLabel.Dock = DockStyle.Fill;
            netOrderRateLabel.Location = new Point(4, 75);
            netOrderRateLabel.Margin = new Padding(4, 0, 4, 0);
            netOrderRateLabel.Name = "netOrderRateLabel";
            netOrderRateLabel.Size = new Size(59, 30);
            netOrderRateLabel.TabIndex = 4;
            netOrderRateLabel.Text = "Net Order Rate";
            netOrderRateLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // netOrderRateValue
            // 
            netOrderRateValue.AutoSize = true;
            netOrderRateValue.Dock = DockStyle.Fill;
            netOrderRateValue.Location = new Point(71, 75);
            netOrderRateValue.Margin = new Padding(4, 0, 4, 0);
            netOrderRateValue.Name = "netOrderRateValue";
            netOrderRateValue.Size = new Size(60, 30);
            netOrderRateValue.TabIndex = 5;
            netOrderRateValue.Text = "-- (--)";
            netOrderRateValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // tradeVolumeLabel
            // 
            tradeVolumeLabel.AutoSize = true;
            tradeVolumeLabel.Dock = DockStyle.Fill;
            tradeVolumeLabel.Location = new Point(4, 105);
            tradeVolumeLabel.Margin = new Padding(4, 0, 4, 0);
            tradeVolumeLabel.Name = "tradeVolumeLabel";
            tradeVolumeLabel.Size = new Size(59, 30);
            tradeVolumeLabel.TabIndex = 6;
            tradeVolumeLabel.Text = "Trade Volume";
            tradeVolumeLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // tradeVolumeValue
            // 
            tradeVolumeValue.AutoSize = true;
            tradeVolumeValue.Dock = DockStyle.Fill;
            tradeVolumeValue.Location = new Point(71, 105);
            tradeVolumeValue.Margin = new Padding(4, 0, 4, 0);
            tradeVolumeValue.Name = "tradeVolumeValue";
            tradeVolumeValue.Size = new Size(60, 30);
            tradeVolumeValue.TabIndex = 7;
            tradeVolumeValue.Text = "-- (--)";
            tradeVolumeValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // avgTradeSizeLabel
            // 
            avgTradeSizeLabel.AutoSize = true;
            avgTradeSizeLabel.Dock = DockStyle.Fill;
            avgTradeSizeLabel.Location = new Point(4, 135);
            avgTradeSizeLabel.Margin = new Padding(4, 0, 4, 0);
            avgTradeSizeLabel.Name = "avgTradeSizeLabel";
            avgTradeSizeLabel.Size = new Size(59, 60);
            avgTradeSizeLabel.TabIndex = 8;
            avgTradeSizeLabel.Text = "Average Trade Size";
            avgTradeSizeLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // avgTradeSizeValue
            // 
            avgTradeSizeValue.AutoSize = true;
            avgTradeSizeValue.Dock = DockStyle.Fill;
            avgTradeSizeValue.Location = new Point(71, 135);
            avgTradeSizeValue.Margin = new Padding(4, 0, 4, 0);
            avgTradeSizeValue.Name = "avgTradeSizeValue";
            avgTradeSizeValue.Size = new Size(60, 60);
            avgTradeSizeValue.TabIndex = 9;
            avgTradeSizeValue.Text = "-- (--)";
            avgTradeSizeValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // contextGrid
            // 
            contextGrid.AutoSize = true;
            contextGrid.ColumnCount = 2;
            contextGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            contextGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            contextGrid.Controls.Add(contextHeader, 0, 0);
            contextGrid.Controls.Add(spreadLabel, 0, 1);
            contextGrid.Controls.Add(spreadValue, 1, 1);
            contextGrid.Controls.Add(imbalLabel, 0, 2);
            contextGrid.Controls.Add(imbalValue, 1, 2);
            contextGrid.Controls.Add(depthTop4Label, 0, 3);
            contextGrid.Controls.Add(depthTop4Value, 1, 3);
            contextGrid.Controls.Add(centerMassLabel, 0, 4);
            contextGrid.Controls.Add(centerMassValue, 1, 4);
            contextGrid.Controls.Add(totalContractsLabel, 0, 5);
            contextGrid.Controls.Add(totalContractsValue, 1, 5);
            contextGrid.Dock = DockStyle.Top;
            contextGrid.Location = new Point(4, 390);
            contextGrid.Margin = new Padding(4, 3, 4, 3);
            contextGrid.Name = "contextGrid";
            contextGrid.RowCount = 6;
            contextGrid.RowStyles.Add(new RowStyle());
            contextGrid.RowStyles.Add(new RowStyle());
            contextGrid.RowStyles.Add(new RowStyle());
            contextGrid.RowStyles.Add(new RowStyle());
            contextGrid.RowStyles.Add(new RowStyle());
            contextGrid.RowStyles.Add(new RowStyle());
            contextGrid.Size = new Size(135, 195);
            contextGrid.TabIndex = 2;
            // 
            // contextHeader
            // 
            contextHeader.AutoSize = true;
            contextGrid.SetColumnSpan(contextHeader, 2);
            contextHeader.Dock = DockStyle.Fill;
            contextHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            contextHeader.Location = new Point(4, 0);
            contextHeader.Margin = new Padding(4, 0, 4, 0);
            contextHeader.Name = "contextHeader";
            contextHeader.Size = new Size(127, 30);
            contextHeader.TabIndex = 0;
            contextHeader.Text = "Context & Deeper Book";
            // 
            // spreadLabel
            // 
            spreadLabel.AutoSize = true;
            spreadLabel.Dock = DockStyle.Fill;
            spreadLabel.Location = new Point(4, 30);
            spreadLabel.Margin = new Padding(4, 0, 4, 0);
            spreadLabel.Name = "spreadLabel";
            spreadLabel.Size = new Size(59, 15);
            spreadLabel.TabIndex = 0;
            spreadLabel.Text = "Spread";
            spreadLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // spreadValue
            // 
            spreadValue.AutoSize = true;
            spreadValue.Dock = DockStyle.Fill;
            spreadValue.Location = new Point(71, 30);
            spreadValue.Margin = new Padding(4, 0, 4, 0);
            spreadValue.Name = "spreadValue";
            spreadValue.Size = new Size(60, 15);
            spreadValue.TabIndex = 1;
            spreadValue.Text = "--";
            spreadValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // imbalLabel
            // 
            imbalLabel.AutoSize = true;
            imbalLabel.Dock = DockStyle.Fill;
            imbalLabel.Location = new Point(4, 45);
            imbalLabel.Margin = new Padding(4, 0, 4, 0);
            imbalLabel.Name = "imbalLabel";
            imbalLabel.Size = new Size(59, 45);
            imbalLabel.TabIndex = 2;
            imbalLabel.Text = "Ask/Bid Imbal (Vol)";
            imbalLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // imbalValue
            // 
            imbalValue.AutoSize = true;
            imbalValue.Dock = DockStyle.Fill;
            imbalValue.Location = new Point(71, 45);
            imbalValue.Margin = new Padding(4, 0, 4, 0);
            imbalValue.Name = "imbalValue";
            imbalValue.Size = new Size(60, 45);
            imbalValue.TabIndex = 3;
            imbalValue.Text = "--";
            imbalValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // depthTop4Label
            // 
            depthTop4Label.AutoSize = true;
            depthTop4Label.Dock = DockStyle.Fill;
            depthTop4Label.Location = new Point(4, 90);
            depthTop4Label.Margin = new Padding(4, 0, 4, 0);
            depthTop4Label.Name = "depthTop4Label";
            depthTop4Label.Size = new Size(59, 30);
            depthTop4Label.TabIndex = 4;
            depthTop4Label.Text = "Depth Top 4";
            depthTop4Label.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // depthTop4Value
            // 
            depthTop4Value.AutoSize = true;
            depthTop4Value.Dock = DockStyle.Fill;
            depthTop4Value.Location = new Point(71, 90);
            depthTop4Value.Margin = new Padding(4, 0, 4, 0);
            depthTop4Value.Name = "depthTop4Value";
            depthTop4Value.Size = new Size(60, 30);
            depthTop4Value.TabIndex = 5;
            depthTop4Value.Text = "-- (--)";
            depthTop4Value.TextAlign = ContentAlignment.MiddleRight;
            // 
            // centerMassLabel
            // 
            centerMassLabel.AutoSize = true;
            centerMassLabel.Dock = DockStyle.Fill;
            centerMassLabel.Location = new Point(4, 120);
            centerMassLabel.Margin = new Padding(4, 0, 4, 0);
            centerMassLabel.Name = "centerMassLabel";
            centerMassLabel.Size = new Size(59, 30);
            centerMassLabel.TabIndex = 6;
            centerMassLabel.Text = "Center Mass";
            centerMassLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // centerMassValue
            // 
            centerMassValue.AutoSize = true;
            centerMassValue.Dock = DockStyle.Fill;
            centerMassValue.Location = new Point(71, 120);
            centerMassValue.Margin = new Padding(4, 0, 4, 0);
            centerMassValue.Name = "centerMassValue";
            centerMassValue.Size = new Size(60, 30);
            centerMassValue.TabIndex = 7;
            centerMassValue.Text = "-- (--)";
            centerMassValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // totalContractsLabel
            // 
            totalContractsLabel.AutoSize = true;
            totalContractsLabel.Dock = DockStyle.Fill;
            totalContractsLabel.Location = new Point(4, 150);
            totalContractsLabel.Margin = new Padding(4, 0, 4, 0);
            totalContractsLabel.Name = "totalContractsLabel";
            totalContractsLabel.Size = new Size(59, 45);
            totalContractsLabel.TabIndex = 8;
            totalContractsLabel.Text = "Total Contracts";
            totalContractsLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // totalContractsValue
            // 
            totalContractsValue.AutoSize = true;
            totalContractsValue.Dock = DockStyle.Fill;
            totalContractsValue.Location = new Point(71, 150);
            totalContractsValue.Margin = new Padding(4, 0, 4, 0);
            totalContractsValue.Name = "totalContractsValue";
            totalContractsValue.Size = new Size(60, 45);
            totalContractsValue.TabIndex = 9;
            totalContractsValue.Text = "-- (--)";
            totalContractsValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // positionsContainer
            // 
            positionsContainer.BorderStyle = BorderStyle.FixedSingle;
            positionsContainer.Controls.Add(positionsGrid);
            positionsContainer.Dock = DockStyle.Fill;
            positionsContainer.Location = new Point(4, 562);
            positionsContainer.Margin = new Padding(4, 3, 4, 3);
            positionsContainer.Name = "positionsContainer";
            positionsContainer.Padding = new Padding(6, 6, 6, 6);
            positionsContainer.Size = new Size(593, 75);
            positionsContainer.TabIndex = 2;
            // 
            // positionsGrid
            // 
            positionsGrid.AutoSize = true;
            positionsGrid.ColumnCount = 4;
            positionsGrid.ColumnStyles.Add(new ColumnStyle());
            positionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            positionsGrid.ColumnStyles.Add(new ColumnStyle());
            positionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
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
            positionsGrid.Dock = DockStyle.Top;
            positionsGrid.Location = new Point(6, 6);
            positionsGrid.Margin = new Padding(4, 3, 4, 3);
            positionsGrid.Name = "positionsGrid";
            positionsGrid.RowCount = 4;
            positionsGrid.RowStyles.Add(new RowStyle());
            positionsGrid.RowStyles.Add(new RowStyle());
            positionsGrid.RowStyles.Add(new RowStyle());
            positionsGrid.RowStyles.Add(new RowStyle());
            positionsGrid.Size = new Size(579, 60);
            positionsGrid.TabIndex = 0;
            // 
            // positionSizeLabel
            // 
            positionSizeLabel.AutoSize = true;
            positionSizeLabel.Dock = DockStyle.Fill;
            positionSizeLabel.Location = new Point(4, 0);
            positionSizeLabel.Margin = new Padding(4, 0, 4, 0);
            positionSizeLabel.Name = "positionSizeLabel";
            positionSizeLabel.Size = new Size(92, 15);
            positionSizeLabel.TabIndex = 0;
            positionSizeLabel.Text = "Position Size:";
            positionSizeLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // positionSizeValue
            // 
            positionSizeValue.AutoSize = true;
            positionSizeValue.Dock = DockStyle.Fill;
            positionSizeValue.Location = new Point(104, 0);
            positionSizeValue.Margin = new Padding(4, 0, 4, 0);
            positionSizeValue.Name = "positionSizeValue";
            positionSizeValue.Size = new Size(173, 15);
            positionSizeValue.TabIndex = 1;
            positionSizeValue.Text = "--";
            positionSizeValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lastTradeLabel
            // 
            lastTradeLabel.AutoSize = true;
            lastTradeLabel.Dock = DockStyle.Fill;
            lastTradeLabel.Location = new Point(285, 0);
            lastTradeLabel.Margin = new Padding(4, 0, 4, 0);
            lastTradeLabel.Name = "lastTradeLabel";
            lastTradeLabel.Size = new Size(108, 15);
            lastTradeLabel.TabIndex = 2;
            lastTradeLabel.Text = "Last Trade:";
            lastTradeLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lastTradeValue
            // 
            lastTradeValue.AutoSize = true;
            lastTradeValue.Dock = DockStyle.Fill;
            lastTradeValue.Location = new Point(401, 0);
            lastTradeValue.Margin = new Padding(4, 0, 4, 0);
            lastTradeValue.Name = "lastTradeValue";
            lastTradeValue.Size = new Size(174, 15);
            lastTradeValue.TabIndex = 3;
            lastTradeValue.Text = "--";
            lastTradeValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // positionRoiLabel
            // 
            positionRoiLabel.AutoSize = true;
            positionRoiLabel.Dock = DockStyle.Fill;
            positionRoiLabel.Location = new Point(4, 15);
            positionRoiLabel.Margin = new Padding(4, 0, 4, 0);
            positionRoiLabel.Name = "positionRoiLabel";
            positionRoiLabel.Size = new Size(92, 15);
            positionRoiLabel.TabIndex = 4;
            positionRoiLabel.Text = "Position ROI:";
            positionRoiLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // positionRoiValue
            // 
            positionRoiValue.AutoSize = true;
            positionRoiValue.Dock = DockStyle.Fill;
            positionRoiValue.Location = new Point(104, 15);
            positionRoiValue.Margin = new Padding(4, 0, 4, 0);
            positionRoiValue.Name = "positionRoiValue";
            positionRoiValue.Size = new Size(173, 15);
            positionRoiValue.TabIndex = 5;
            positionRoiValue.Text = "--";
            positionRoiValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // buyinPriceLabel
            // 
            buyinPriceLabel.AutoSize = true;
            buyinPriceLabel.Dock = DockStyle.Fill;
            buyinPriceLabel.Location = new Point(285, 15);
            buyinPriceLabel.Margin = new Padding(4, 0, 4, 0);
            buyinPriceLabel.Name = "buyinPriceLabel";
            buyinPriceLabel.Size = new Size(108, 15);
            buyinPriceLabel.TabIndex = 6;
            buyinPriceLabel.Text = "Buyin Price:";
            buyinPriceLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // buyinPriceValue
            // 
            buyinPriceValue.AutoSize = true;
            buyinPriceValue.Dock = DockStyle.Fill;
            buyinPriceValue.Location = new Point(401, 15);
            buyinPriceValue.Margin = new Padding(4, 0, 4, 0);
            buyinPriceValue.Name = "buyinPriceValue";
            buyinPriceValue.Size = new Size(174, 15);
            buyinPriceValue.TabIndex = 7;
            buyinPriceValue.Text = "--";
            buyinPriceValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // positionUpsideLabel
            // 
            positionUpsideLabel.AutoSize = true;
            positionUpsideLabel.Dock = DockStyle.Fill;
            positionUpsideLabel.Location = new Point(4, 30);
            positionUpsideLabel.Margin = new Padding(4, 0, 4, 0);
            positionUpsideLabel.Name = "positionUpsideLabel";
            positionUpsideLabel.Size = new Size(92, 15);
            positionUpsideLabel.TabIndex = 8;
            positionUpsideLabel.Text = "Position Upside:";
            positionUpsideLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // positionUpsideValue
            // 
            positionUpsideValue.AutoSize = true;
            positionUpsideValue.Dock = DockStyle.Fill;
            positionUpsideValue.Location = new Point(104, 30);
            positionUpsideValue.Margin = new Padding(4, 0, 4, 0);
            positionUpsideValue.Name = "positionUpsideValue";
            positionUpsideValue.Size = new Size(173, 15);
            positionUpsideValue.TabIndex = 9;
            positionUpsideValue.Text = "--";
            positionUpsideValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // positionDownsideLabel
            // 
            positionDownsideLabel.AutoSize = true;
            positionDownsideLabel.Dock = DockStyle.Fill;
            positionDownsideLabel.Location = new Point(285, 30);
            positionDownsideLabel.Margin = new Padding(4, 0, 4, 0);
            positionDownsideLabel.Name = "positionDownsideLabel";
            positionDownsideLabel.Size = new Size(108, 15);
            positionDownsideLabel.TabIndex = 10;
            positionDownsideLabel.Text = "Position Downside:";
            positionDownsideLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // positionDownsideValue
            // 
            positionDownsideValue.AutoSize = true;
            positionDownsideValue.Dock = DockStyle.Fill;
            positionDownsideValue.Location = new Point(401, 30);
            positionDownsideValue.Margin = new Padding(4, 0, 4, 0);
            positionDownsideValue.Name = "positionDownsideValue";
            positionDownsideValue.Size = new Size(174, 15);
            positionDownsideValue.TabIndex = 11;
            positionDownsideValue.Text = "--";
            positionDownsideValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // restingOrdersLabel
            // 
            restingOrdersLabel.AutoSize = true;
            restingOrdersLabel.Dock = DockStyle.Fill;
            restingOrdersLabel.Location = new Point(4, 45);
            restingOrdersLabel.Margin = new Padding(4, 0, 4, 0);
            restingOrdersLabel.Name = "restingOrdersLabel";
            restingOrdersLabel.Size = new Size(92, 15);
            restingOrdersLabel.TabIndex = 12;
            restingOrdersLabel.Text = "Resting Orders:";
            restingOrdersLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // restingOrdersValue
            // 
            restingOrdersValue.AutoSize = true;
            restingOrdersValue.Dock = DockStyle.Fill;
            restingOrdersValue.Location = new Point(104, 45);
            restingOrdersValue.Margin = new Padding(4, 0, 4, 0);
            restingOrdersValue.Name = "restingOrdersValue";
            restingOrdersValue.Size = new Size(173, 15);
            restingOrdersValue.TabIndex = 13;
            restingOrdersValue.Text = "--";
            restingOrdersValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // orderbookContainer
            // 
            orderbookContainer.BorderStyle = BorderStyle.FixedSingle;
            orderbookContainer.Controls.Add(orderbookGrid);
            orderbookContainer.Dock = DockStyle.Fill;
            orderbookContainer.Location = new Point(605, 562);
            orderbookContainer.Margin = new Padding(4, 3, 4, 3);
            orderbookContainer.Name = "orderbookContainer";
            orderbookContainer.Padding = new Padding(6, 6, 6, 6);
            orderbookContainer.Size = new Size(316, 75);
            orderbookContainer.TabIndex = 3;
            // 
            // orderbookGrid
            // 
            orderbookGrid.AllowUserToAddRows = false;
            orderbookGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            orderbookGrid.BorderStyle = BorderStyle.None;
            orderbookGrid.Columns.AddRange(new DataGridViewColumn[] { priceCol, sizeCol, valueCol });
            orderbookGrid.Dock = DockStyle.Fill;
            orderbookGrid.Location = new Point(6, 6);
            orderbookGrid.Margin = new Padding(4, 3, 4, 3);
            orderbookGrid.Name = "orderbookGrid";
            orderbookGrid.ReadOnly = true;
            orderbookGrid.RowHeadersVisible = false;
            orderbookGrid.Size = new Size(302, 61);
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
            backButton.Location = new Point(4, 649);
            backButton.Margin = new Padding(4, 3, 4, 3);
            backButton.Name = "backButton";
            backButton.Size = new Size(925, 40);
            backButton.TabIndex = 1;
            backButton.Text = "Back to Full Chart";
            backButton.UseVisualStyleBackColor = true;
            // 
            // chartHeader
            // 
            chartHeader.AutoSize = true;
            chartHeader.Location = new Point(4, 0);
            chartHeader.Margin = new Padding(4, 0, 4, 0);
            chartHeader.Name = "chartHeader";
            chartHeader.Size = new Size(76, 15);
            chartHeader.TabIndex = 0;
            chartHeader.Text = "Price Chart - ";
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
            positionsContainer.PerformLayout();
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
        private Label chartHeader;
    }
}