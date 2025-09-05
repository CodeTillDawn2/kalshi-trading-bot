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
        // Panel container for the right side of the layout. This enables swapping the
        // chart view with other controls (e.g., a dashboard) at runtime without
        // disrupting the table layout structure.
        private Panel rightPane;
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
            components = new System.ComponentModel.Container();
            toolTip1 = new ToolTip(components);
            dgvMarkets = new DataGridView();
            dataGridViewTextBoxColumn1 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
            rtbLog = new RichTextBox();
            btnRun = new Button();
            btnReload = new Button();
            btnRunSet = new Button();
            formsPlot1 = new ScottPlot.FormsPlot();
            rightPane = new Panel();
            layout = new TableLayoutPanel();
            buttonPanel = new FlowLayoutPanel();
            btnCheckAll = new Button();
            btnUncheckAll = new Button();
            btnRunML = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvMarkets).BeginInit();
            rightPane.SuspendLayout();
            layout.SuspendLayout();
            buttonPanel.SuspendLayout();
            SuspendLayout();
            // 
            // toolTip1
            // 
            toolTip1.AutoPopDelay = 5000;
            toolTip1.InitialDelay = 100;
            toolTip1.ReshowDelay = 100;
            // 
            // dgvMarkets
            // 
            dgvMarkets.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvMarkets.Columns.AddRange(new DataGridViewColumn[] { dataGridViewTextBoxColumn1, dataGridViewTextBoxColumn2 });
            dgvMarkets.Dock = DockStyle.Fill;
            dgvMarkets.Location = new Point(3, 3);
            dgvMarkets.Name = "dgvMarkets";
            dgvMarkets.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvMarkets.Size = new Size(324, 546);
            dgvMarkets.TabIndex = 0;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.HeaderText = "Market";
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.HeaderText = "PnL";
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            // 
            // rtbLog
            // 
            rtbLog.BackColor = Color.Black;
            layout.SetColumnSpan(rtbLog, 2);
            rtbLog.Dock = DockStyle.Fill;
            rtbLog.Font = new Font("Consolas", 9F);
            rtbLog.ForeColor = Color.Lime;
            rtbLog.Location = new Point(3, 555);
            rtbLog.Name = "rtbLog";
            rtbLog.ReadOnly = true;
            rtbLog.Size = new Size(1094, 59);
            rtbLog.TabIndex = 2;
            rtbLog.Text = "";
            // 
            // btnRun
            // 
            btnRun.Location = new Point(3, 3);
            btnRun.Name = "btnRun";
            btnRun.Size = new Size(75, 23);
            btnRun.TabIndex = 0;
            btnRun.Text = "Run All";
            btnRun.Click += btnRun_Click;
            // 
            // btnReload
            // 
            btnReload.Location = new Point(84, 3);
            btnReload.Name = "btnReload";
            btnReload.Size = new Size(90, 23);
            btnReload.TabIndex = 1;
            btnReload.Text = "Reload Cache";
            btnReload.Click += btnReload_Click;
            // 
            // btnRunSet
            // 
            btnRunSet.Location = new Point(180, 3);
            btnRunSet.Name = "btnRunSet";
            btnRunSet.Size = new Size(75, 23);
            btnRunSet.TabIndex = 2;
            btnRunSet.Text = "Run Set";
            btnRunSet.Click += btnRunSet_Click;
            // 
            // formsPlot1
            // 
            formsPlot1.Dock = DockStyle.Fill;
            formsPlot1.Location = new Point(0, 0);
            formsPlot1.Margin = new Padding(0);
            formsPlot1.Name = "formsPlot1";
            formsPlot1.Size = new Size(770, 552);
            formsPlot1.TabIndex = 1;
            // 
            // rightPane
            // 
            rightPane.Controls.Add(formsPlot1);
            rightPane.Dock = DockStyle.Fill;
            rightPane.Location = new Point(330, 0);
            rightPane.Margin = new Padding(0);
            rightPane.Name = "rightPane";
            rightPane.Size = new Size(770, 552);
            rightPane.TabIndex = 1;
            // 
            // layout
            // 
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            layout.Controls.Add(dgvMarkets, 0, 0);
            layout.Controls.Add(rightPane, 1, 0);
            layout.Controls.Add(rtbLog, 0, 1);
            layout.Controls.Add(buttonPanel, 0, 2);
            layout.Dock = DockStyle.Fill;
            layout.Location = new Point(0, 0);
            layout.Name = "layout";
            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 85F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 5F));
            layout.Size = new Size(1100, 650);
            layout.TabIndex = 0;
            // 
            // buttonPanel
            // 
            buttonPanel.AutoScroll = true;
            layout.SetColumnSpan(buttonPanel, 2);
            buttonPanel.Controls.Add(btnRun);
            buttonPanel.Controls.Add(btnReload);
            buttonPanel.Controls.Add(btnRunSet);
            buttonPanel.Controls.Add(btnCheckAll);
            buttonPanel.Controls.Add(btnUncheckAll);
            buttonPanel.Controls.Add(btnRunML);
            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.Location = new Point(3, 620);
            buttonPanel.Name = "buttonPanel";
            buttonPanel.Size = new Size(1094, 27);
            buttonPanel.TabIndex = 3;
            buttonPanel.WrapContents = false;
            // 
            // btnCheckAll
            // 
            btnCheckAll.Location = new Point(261, 3);
            btnCheckAll.Name = "btnCheckAll";
            btnCheckAll.Size = new Size(75, 23);
            btnCheckAll.TabIndex = 3;
            btnCheckAll.Text = "Check All";
            btnCheckAll.Click += BtnCheckAll_Click;
            // 
            // btnUncheckAll
            // 
            btnUncheckAll.Location = new Point(342, 3);
            btnUncheckAll.Name = "btnUncheckAll";
            btnUncheckAll.Size = new Size(90, 23);
            btnUncheckAll.TabIndex = 4;
            btnUncheckAll.Text = "Uncheck All";
            btnUncheckAll.Click += BtnUncheckAll_Click;
            // 
            // btnRunML
            // 
            btnRunML.Location = new Point(438, 3);
            btnRunML.Name = "btnRunML";
            btnRunML.Size = new Size(90, 23);
            btnRunML.TabIndex = 5;
            btnRunML.Text = "Run ML";
            btnRunML.Click += btnRunML_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1100, 650);
            Controls.Add(layout);
            MinimumSize = new Size(800, 600);
            Name = "MainForm";
            Text = "Trading Strategy Viewer";
            ((System.ComponentModel.ISupportInitialize)dgvMarkets).EndInit();
            rightPane.ResumeLayout(false);
            layout.ResumeLayout(false);
            buttonPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private TableLayoutPanel layout;
        private FlowLayoutPanel buttonPanel;
        private Button btnRunML;
    }
}
