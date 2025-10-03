namespace MentionMarketBingo;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
        eventComboBox = new ComboBox();
        bingoPanel = new TableLayoutPanel();
        label1 = new Label();
        maxExposureLabel = new Label();
        maxExposureYesTextBox = new TextBox();
        maxNoPriceLabel = new Label();
        maxNoPriceTextBox = new TextBox();
        maxYesPriceLabel = new Label();
        maxYesPriceTextBox = new TextBox();
        refreshButton = new Button();
        buyNosButton = new Button();
        topPanel = new TableLayoutPanel();
        label3 = new Label();
        nextMilestoneTimeLabel = new Label();
        nextMilestoneCountdownLabel = new Label();
        maxExposureNoTextBox = new TextBox();
        label2 = new Label();
        bottomPanel = new Panel();
        orderLogTextBox = new RichTextBox();
        topPanel.SuspendLayout();
        bottomPanel.SuspendLayout();
        SuspendLayout();
        // 
        // eventComboBox
        // 
        topPanel.SetColumnSpan(eventComboBox, 2);
        eventComboBox.Dock = DockStyle.Fill;
        eventComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        eventComboBox.FormattingEnabled = true;
        eventComboBox.Location = new Point(108, 33);
        eventComboBox.Name = "eventComboBox";
        eventComboBox.Size = new Size(278, 23);
        eventComboBox.TabIndex = 0;
        eventComboBox.SelectedIndexChanged += eventComboBox_SelectedIndexChanged;
        // 
        // bingoPanel
        // 
        bingoPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        bingoPanel.AutoScroll = true;
        bingoPanel.ColumnCount = 5;
        bingoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        bingoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        bingoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        bingoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        bingoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        bingoPanel.Location = new Point(0, 60);
        bingoPanel.Name = "bingoPanel";
        bingoPanel.RowCount = 5;
        bingoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        bingoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        bingoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        bingoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        bingoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        bingoPanel.Size = new Size(1000, 386);
        bingoPanel.TabIndex = 1;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Dock = DockStyle.Fill;
        label1.Location = new Point(3, 30);
        label1.Name = "label1";
        label1.Size = new Size(99, 30);
        label1.TabIndex = 11;
        label1.Text = "Select Event:";
        label1.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // maxExposureLabel
        // 
        maxExposureLabel.AutoSize = true;
        maxExposureLabel.Dock = DockStyle.Fill;
        maxExposureLabel.Location = new Point(3, 0);
        maxExposureLabel.Name = "maxExposureLabel";
        maxExposureLabel.Size = new Size(99, 30);
        maxExposureLabel.TabIndex = 3;
        maxExposureLabel.Text = "Max Exposure Yes";
        maxExposureLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // maxExposureYesTextBox
        // 
        maxExposureYesTextBox.Dock = DockStyle.Fill;
        maxExposureYesTextBox.Location = new Point(108, 3);
        maxExposureYesTextBox.Name = "maxExposureYesTextBox";
        maxExposureYesTextBox.Size = new Size(136, 23);
        maxExposureYesTextBox.TabIndex = 4;
        maxExposureYesTextBox.Text = "10.00";
        maxExposureYesTextBox.TextChanged += maxExposureTextBox_TextChanged;
        // 
        // maxNoPriceLabel
        // 
        maxNoPriceLabel.AutoSize = true;
        maxNoPriceLabel.Dock = DockStyle.Fill;
        maxNoPriceLabel.Location = new Point(771, 0);
        maxNoPriceLabel.Name = "maxNoPriceLabel";
        maxNoPriceLabel.Size = new Size(80, 30);
        maxNoPriceLabel.TabIndex = 5;
        maxNoPriceLabel.Text = "Max No Price:";
        maxNoPriceLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // maxNoPriceTextBox
        // 
        maxNoPriceTextBox.Dock = DockStyle.Fill;
        maxNoPriceTextBox.Location = new Point(857, 3);
        maxNoPriceTextBox.Name = "maxNoPriceTextBox";
        maxNoPriceTextBox.Size = new Size(140, 23);
        maxNoPriceTextBox.TabIndex = 7;
        maxNoPriceTextBox.Text = "0.90";
        maxNoPriceTextBox.TextChanged += maxNoPriceTextBox_TextChanged;
        // 
        // maxYesPriceLabel
        // 
        maxYesPriceLabel.AutoSize = true;
        maxYesPriceLabel.Dock = DockStyle.Fill;
        maxYesPriceLabel.Location = new Point(534, 0);
        maxYesPriceLabel.Name = "maxYesPriceLabel";
        maxYesPriceLabel.Size = new Size(89, 30);
        maxYesPriceLabel.TabIndex = 7;
        maxYesPriceLabel.Text = "Max Yes Price:";
        maxYesPriceLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // maxYesPriceTextBox
        // 
        maxYesPriceTextBox.Dock = DockStyle.Fill;
        maxYesPriceTextBox.Location = new Point(629, 3);
        maxYesPriceTextBox.Name = "maxYesPriceTextBox";
        maxYesPriceTextBox.Size = new Size(136, 23);
        maxYesPriceTextBox.TabIndex = 8;
        maxYesPriceTextBox.Text = "0.50";
        maxYesPriceTextBox.TextChanged += maxYesPriceTextBox_TextChanged;
        // 
        // refreshButton
        // 
        refreshButton.Dock = DockStyle.Fill;
        refreshButton.Location = new Point(392, 33);
        refreshButton.Name = "refreshButton";
        refreshButton.Size = new Size(136, 24);
        refreshButton.TabIndex = 9;
        refreshButton.Text = "Refresh";
        refreshButton.UseVisualStyleBackColor = true;
        refreshButton.Click += refreshButton_Click;
        // 
        // buyNosButton
        // 
        buyNosButton.Dock = DockStyle.Fill;
        buyNosButton.Location = new Point(857, 33);
        buyNosButton.Name = "buyNosButton";
        buyNosButton.Size = new Size(140, 24);
        buyNosButton.TabIndex = 10;
        buyNosButton.Text = "Event Finished, Buy Nos";
        buyNosButton.UseVisualStyleBackColor = true;
        buyNosButton.Click += buyNosButton_Click;
        // 
        // topPanel
        // 
        topPanel.ColumnCount = 8;
        topPanel.ColumnStyles.Add(new ColumnStyle());
        topPanel.ColumnStyles.Add(new ColumnStyle());
        topPanel.ColumnStyles.Add(new ColumnStyle());
        topPanel.ColumnStyles.Add(new ColumnStyle());
        topPanel.ColumnStyles.Add(new ColumnStyle());
        topPanel.ColumnStyles.Add(new ColumnStyle());
        topPanel.ColumnStyles.Add(new ColumnStyle());
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
        topPanel.Controls.Add(label3, 4, 1);
        topPanel.Controls.Add(nextMilestoneTimeLabel, 5, 1);
        topPanel.Controls.Add(nextMilestoneCountdownLabel, 6, 1);
        topPanel.Controls.Add(maxExposureNoTextBox, 3, 0);
        topPanel.Controls.Add(label2, 2, 0);
        topPanel.Controls.Add(maxExposureLabel, 0, 0);
        topPanel.Controls.Add(maxExposureYesTextBox, 1, 0);
        topPanel.Controls.Add(maxNoPriceLabel, 6, 0);
        topPanel.Controls.Add(maxNoPriceTextBox, 7, 0);
        topPanel.Controls.Add(maxYesPriceLabel, 4, 0);
        topPanel.Controls.Add(maxYesPriceTextBox, 4, 0);
        topPanel.Controls.Add(label1, 0, 1);
        topPanel.Controls.Add(eventComboBox, 1, 1);
        topPanel.Controls.Add(refreshButton, 3, 1);
        topPanel.Controls.Add(buyNosButton, 7, 1);
        topPanel.Dock = DockStyle.Top;
        topPanel.Location = new Point(0, 0);
        topPanel.Name = "topPanel";
        topPanel.RowCount = 2;
        topPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        topPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        topPanel.Size = new Size(1000, 60);
        topPanel.TabIndex = 12;
        topPanel.Paint += topPanel_Paint;
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Dock = DockStyle.Fill;
        label3.Location = new Point(534, 30);
        label3.Name = "label3";
        label3.Size = new Size(89, 30);
        label3.TabIndex = 14;
        label3.Text = "Next Milestone:";
        label3.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // nextMilestoneTimeLabel
        // 
        nextMilestoneTimeLabel.AutoSize = true;
        nextMilestoneTimeLabel.Dock = DockStyle.Fill;
        nextMilestoneTimeLabel.Location = new Point(629, 30);
        nextMilestoneTimeLabel.Name = "nextMilestoneTimeLabel";
        nextMilestoneTimeLabel.Size = new Size(136, 30);
        nextMilestoneTimeLabel.TabIndex = 15;
        nextMilestoneTimeLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // nextMilestoneCountdownLabel
        // 
        nextMilestoneCountdownLabel.AutoSize = true;
        nextMilestoneCountdownLabel.Dock = DockStyle.Fill;
        nextMilestoneCountdownLabel.Location = new Point(771, 30);
        nextMilestoneCountdownLabel.Name = "nextMilestoneCountdownLabel";
        nextMilestoneCountdownLabel.Size = new Size(80, 30);
        nextMilestoneCountdownLabel.TabIndex = 16;
        nextMilestoneCountdownLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // maxExposureNoTextBox
        // 
        maxExposureNoTextBox.Dock = DockStyle.Fill;
        maxExposureNoTextBox.Location = new Point(392, 3);
        maxExposureNoTextBox.Name = "maxExposureNoTextBox";
        maxExposureNoTextBox.Size = new Size(136, 23);
        maxExposureNoTextBox.TabIndex = 13;
        maxExposureNoTextBox.Text = "10.00";
        maxExposureNoTextBox.TextChanged += maxExposureNoTextBox_TextChanged;
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Dock = DockStyle.Fill;
        label2.Location = new Point(250, 0);
        label2.Name = "label2";
        label2.Size = new Size(136, 30);
        label2.TabIndex = 12;
        label2.Text = "Max Exposure No";
        label2.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // bottomPanel
        // 
        bottomPanel.Controls.Add(orderLogTextBox);
        bottomPanel.Dock = DockStyle.Bottom;
        bottomPanel.Location = new Point(0, 350);
        bottomPanel.Name = "bottomPanel";
        bottomPanel.Size = new Size(1000, 150);
        bottomPanel.TabIndex = 13;
        // 
        // orderLogTextBox
        // 
        orderLogTextBox.BackColor = SystemColors.Window;
        orderLogTextBox.Dock = DockStyle.Fill;
        orderLogTextBox.Font = new Font("Consolas", 9F);
        orderLogTextBox.Location = new Point(0, 0);
        orderLogTextBox.Name = "orderLogTextBox";
        orderLogTextBox.ReadOnly = true;
        orderLogTextBox.Size = new Size(1000, 150);
        orderLogTextBox.TabIndex = 0;
        orderLogTextBox.Text = "";
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1000, 500);
        Controls.Add(topPanel);
        Controls.Add(bingoPanel);
        Controls.Add(bottomPanel);
        Icon = (Icon)resources.GetObject("$this.Icon");
        Name = "Form1";
        Text = "Backlash Studios' Mention Market Bingo";
        Load += Form1_Load;
        topPanel.ResumeLayout(false);
        topPanel.PerformLayout();
        bottomPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    private System.Windows.Forms.ComboBox eventComboBox;
    private System.Windows.Forms.TableLayoutPanel bingoPanel;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label maxExposureLabel;
    private System.Windows.Forms.TextBox maxExposureYesTextBox;
    private System.Windows.Forms.Label maxNoPriceLabel;
    private System.Windows.Forms.TextBox maxNoPriceTextBox;
    private System.Windows.Forms.Label maxYesPriceLabel;
    private System.Windows.Forms.TextBox maxYesPriceTextBox;
    private System.Windows.Forms.Button refreshButton;
    private System.Windows.Forms.Button buyNosButton;
    private System.Windows.Forms.TableLayoutPanel topPanel;
    private System.Windows.Forms.Panel bottomPanel;
    private System.Windows.Forms.RichTextBox orderLogTextBox;

    #endregion

    private TextBox maxExposureNoTextBox;
    private Label label2;
    private Label label3;
    private Label nextMilestoneTimeLabel;
    private Label nextMilestoneCountdownLabel;
}
