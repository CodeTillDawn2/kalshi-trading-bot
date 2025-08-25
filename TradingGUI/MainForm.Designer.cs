namespace SimulatorWinForms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.DataGridView dgvMarkets;
        private System.Windows.Forms.RichTextBox rtbLog;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.Button btnReload;
        private System.Windows.Forms.Button btnRunSet;
        private ScottPlot.FormsPlot formsPlot1;
        private System.Windows.Forms.ToolTip toolTip1;
        private Button btnCheckAll;
        private Button btnUncheckAll;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);

            this.dgvMarkets = new System.Windows.Forms.DataGridView();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.btnRun = new System.Windows.Forms.Button();
            this.btnReload = new System.Windows.Forms.Button();
            this.btnRunSet = new System.Windows.Forms.Button();
            this.formsPlot1 = new ScottPlot.FormsPlot();
            var layout = new System.Windows.Forms.TableLayoutPanel();

            ((System.ComponentModel.ISupportInitialize)(this.dgvMarkets)).BeginInit();
            this.SuspendLayout();

            // layout
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.RowCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));

            // dgvMarkets
            this.dgvMarkets.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvMarkets.Columns.Add("Market", "Market");
            this.dgvMarkets.Columns.Add("PnL", "PnL");
            this.dgvMarkets.Dock = DockStyle.Fill;
            this.dgvMarkets.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // formsPlot1
            this.formsPlot1.Dock = DockStyle.Fill;

            // rtbLog
            this.rtbLog.ReadOnly = true;
            this.rtbLog.Dock = DockStyle.Fill;
            this.rtbLog.Font = new System.Drawing.Font("Consolas", 9);
            this.rtbLog.BackColor = System.Drawing.Color.Black;
            this.rtbLog.ForeColor = System.Drawing.Color.Lime;

            // Buttons layout
            var buttonPanel = new FlowLayoutPanel();
            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.FlowDirection = FlowDirection.LeftToRight;
            buttonPanel.Controls.Add(this.btnRun);
            buttonPanel.Controls.Add(this.btnReload);
            buttonPanel.Controls.Add(this.btnRunSet);

            // Buttons
            this.btnRun.Text = "Run Strategy";
            this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
            this.btnReload.Text = "Reload Cache";
            this.btnReload.Click += new System.EventHandler(this.btnReload_Click);
            this.btnRunSet.Text = "Run Set";
            this.btnRunSet.Click += new System.EventHandler(this.btnRunSet_Click);

            this.btnCheckAll = new Button();
            this.btnUncheckAll = new Button();

            this.btnCheckAll.Text = "Check All";
            this.btnUncheckAll.Text = "Uncheck All";

            this.btnCheckAll.Click += BtnCheckAll_Click;
            this.btnUncheckAll.Click += BtnUncheckAll_Click;

            buttonPanel.Controls.Add(this.btnCheckAll);
            buttonPanel.Controls.Add(this.btnUncheckAll);


            // ToolTip
            this.toolTip1.InitialDelay = 100;
            this.toolTip1.ReshowDelay = 100;
            this.toolTip1.AutoPopDelay = 5000;

            // Layout assembly
            layout.Controls.Add(this.dgvMarkets, 0, 0);
            layout.Controls.Add(this.formsPlot1, 1, 0);
            layout.Controls.Add(this.rtbLog, 0, 1);
            layout.Controls.Add(buttonPanel, 1, 1);
            layout.SetColumnSpan(this.rtbLog, 2);

            // MainForm
            this.ClientSize = new System.Drawing.Size(1100, 650);
            this.Controls.Add(layout);
            this.Text = "Trading Strategy Viewer";

            ((System.ComponentModel.ISupportInitialize)(this.dgvMarkets)).EndInit();
            this.ResumeLayout(false);
        }

    }
}
