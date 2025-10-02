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
        maxExposureTextBox = new TextBox();
        maxNoPriceLabel = new Label();
        maxNoPriceTextBox = new TextBox();
        maxYesPriceLabel = new Label();
        maxYesPriceTextBox = new TextBox();
        refreshButton = new Button();
        buyNosButton = new Button();
        topPanel = new TableLayoutPanel();
        topPanel.SuspendLayout();
        SuspendLayout();
        // 
        // eventComboBox
        // 
        topPanel.SetColumnSpan(eventComboBox, 2);
        eventComboBox.Dock = DockStyle.Fill;
        eventComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        eventComboBox.FormattingEnabled = true;
        eventComboBox.Location = new Point(169, 33);
        eventComboBox.Name = "eventComboBox";
        eventComboBox.Size = new Size(326, 23);
        eventComboBox.TabIndex = 0;
        eventComboBox.SelectedIndexChanged += eventComboBox_SelectedIndexChanged;
        // 
        // bingoPanel
        // 
        bingoPanel.AutoScroll = true;
        bingoPanel.ColumnCount = 5;
        bingoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        bingoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        bingoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        bingoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        bingoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        bingoPanel.Dock = DockStyle.Fill;
        bingoPanel.Location = new Point(0, 60);
        bingoPanel.Name = "bingoPanel";
        bingoPanel.RowCount = 5;
        bingoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        bingoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        bingoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        bingoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        bingoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        bingoPanel.Size = new Size(1000, 440);
        bingoPanel.TabIndex = 1;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Dock = DockStyle.Fill;
        label1.Location = new Point(3, 30);
        label1.Name = "label1";
        label1.Size = new Size(160, 30);
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
        maxExposureLabel.Size = new Size(160, 30);
        maxExposureLabel.TabIndex = 3;
        maxExposureLabel.Text = "Max Exposure:";
        maxExposureLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // maxExposureTextBox
        // 
        maxExposureTextBox.Dock = DockStyle.Fill;
        maxExposureTextBox.Location = new Point(169, 3);
        maxExposureTextBox.Name = "maxExposureTextBox";
        maxExposureTextBox.Size = new Size(160, 23);
        maxExposureTextBox.TabIndex = 4;
        maxExposureTextBox.Text = "10.00";
        maxExposureTextBox.TextChanged += maxExposureTextBox_TextChanged;
        // 
        // maxNoPriceLabel
        // 
        maxNoPriceLabel.AutoSize = true;
        maxNoPriceLabel.Dock = DockStyle.Fill;
        maxNoPriceLabel.Location = new Point(335, 0);
        maxNoPriceLabel.Name = "maxNoPriceLabel";
        maxNoPriceLabel.Size = new Size(160, 30);
        maxNoPriceLabel.TabIndex = 5;
        maxNoPriceLabel.Text = "Max No Price:";
        maxNoPriceLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // maxNoPriceTextBox
        // 
        maxNoPriceTextBox.Dock = DockStyle.Fill;
        maxNoPriceTextBox.Location = new Point(501, 3);
        maxNoPriceTextBox.Name = "maxNoPriceTextBox";
        maxNoPriceTextBox.Size = new Size(160, 23);
        maxNoPriceTextBox.TabIndex = 6;
        maxNoPriceTextBox.Text = "0.90";
        maxNoPriceTextBox.TextChanged += maxNoPriceTextBox_TextChanged;
        // 
        // maxYesPriceLabel
        // 
        maxYesPriceLabel.AutoSize = true;
        maxYesPriceLabel.Dock = DockStyle.Fill;
        maxYesPriceLabel.Location = new Point(667, 0);
        maxYesPriceLabel.Name = "maxYesPriceLabel";
        maxYesPriceLabel.Size = new Size(160, 30);
        maxYesPriceLabel.TabIndex = 7;
        maxYesPriceLabel.Text = "Max Yes Price:";
        maxYesPriceLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // maxYesPriceTextBox
        // 
        maxYesPriceTextBox.Dock = DockStyle.Fill;
        maxYesPriceTextBox.Location = new Point(833, 3);
        maxYesPriceTextBox.Name = "maxYesPriceTextBox";
        maxYesPriceTextBox.Size = new Size(164, 23);
        maxYesPriceTextBox.TabIndex = 8;
        maxYesPriceTextBox.Text = "0.50";
        maxYesPriceTextBox.TextChanged += maxYesPriceTextBox_TextChanged;
        // 
        // refreshButton
        // 
        refreshButton.Dock = DockStyle.Fill;
        refreshButton.Location = new Point(501, 33);
        refreshButton.Name = "refreshButton";
        refreshButton.Size = new Size(160, 24);
        refreshButton.TabIndex = 9;
        refreshButton.Text = "Refresh";
        refreshButton.UseVisualStyleBackColor = true;
        refreshButton.Click += refreshButton_Click;
        // 
        // buyNosButton
        // 
        buyNosButton.Dock = DockStyle.Fill;
        buyNosButton.Location = new Point(667, 33);
        buyNosButton.Name = "buyNosButton";
        buyNosButton.Size = new Size(160, 24);
        buyNosButton.TabIndex = 10;
        buyNosButton.Text = "Event Finished, Buy Nos";
        buyNosButton.UseVisualStyleBackColor = true;
        buyNosButton.Click += buyNosButton_Click;
        // 
        // topPanel
        // 
        topPanel.ColumnCount = 6;
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.67F));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.67F));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.67F));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.67F));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.67F));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.67F));
        topPanel.Controls.Add(maxExposureLabel, 0, 0);
        topPanel.Controls.Add(maxExposureTextBox, 1, 0);
        topPanel.Controls.Add(maxNoPriceLabel, 2, 0);
        topPanel.Controls.Add(maxNoPriceTextBox, 3, 0);
        topPanel.Controls.Add(maxYesPriceLabel, 4, 0);
        topPanel.Controls.Add(maxYesPriceTextBox, 5, 0);
        topPanel.Controls.Add(label1, 0, 1);
        topPanel.Controls.Add(eventComboBox, 1, 1);
        topPanel.Controls.Add(refreshButton, 3, 1);
        topPanel.Controls.Add(buyNosButton, 4, 1);
        topPanel.Dock = DockStyle.Top;
        topPanel.Location = new Point(0, 0);
        topPanel.Name = "topPanel";
        topPanel.RowCount = 2;
        topPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        topPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        topPanel.Size = new Size(1000, 60);
        topPanel.TabIndex = 12;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1000, 500);
        Controls.Add(bingoPanel);
        Controls.Add(topPanel);
        Icon = (Icon)resources.GetObject("$this.Icon");
        Name = "Form1";
        Text = "Mention Market Bingo";
        Load += Form1_Load;
        topPanel.ResumeLayout(false);
        topPanel.PerformLayout();
        ResumeLayout(false);
    }

    private System.Windows.Forms.ComboBox eventComboBox;
    private System.Windows.Forms.TableLayoutPanel bingoPanel;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label maxExposureLabel;
    private System.Windows.Forms.TextBox maxExposureTextBox;
    private System.Windows.Forms.Label maxNoPriceLabel;
    private System.Windows.Forms.TextBox maxNoPriceTextBox;
    private System.Windows.Forms.Label maxYesPriceLabel;
    private System.Windows.Forms.TextBox maxYesPriceTextBox;
    private System.Windows.Forms.Button refreshButton;
    private System.Windows.Forms.Button buyNosButton;
    private System.Windows.Forms.TableLayoutPanel topPanel;

    #endregion
}
