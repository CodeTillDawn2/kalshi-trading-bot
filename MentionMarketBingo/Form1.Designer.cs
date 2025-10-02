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
        SuspendLayout();
        // 
        // eventComboBox
        // 
        eventComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        eventComboBox.FormattingEnabled = true;
        eventComboBox.Location = new Point(12, 58);
        eventComboBox.Name = "eventComboBox";
        eventComboBox.Size = new Size(300, 23);
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
        bingoPanel.Location = new Point(12, 92);
        bingoPanel.Name = "bingoPanel";
        bingoPanel.RowCount = 5;
        bingoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        bingoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        bingoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        bingoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        bingoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        bingoPanel.Size = new Size(976, 372);
        bingoPanel.TabIndex = 1;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(12, 35);
        label1.Name = "label1";
        label1.Size = new Size(73, 15);
        label1.TabIndex = 11;
        label1.Text = "Select Event:";
        // 
        // maxExposureLabel
        // 
        maxExposureLabel.AutoSize = true;
        maxExposureLabel.Location = new Point(12, 9);
        maxExposureLabel.Name = "maxExposureLabel";
        maxExposureLabel.Size = new Size(82, 15);
        maxExposureLabel.TabIndex = 3;
        maxExposureLabel.Text = "Max Exposure:";
        // 
        // maxExposureTextBox
        // 
        maxExposureTextBox.Location = new Point(100, 9);
        maxExposureTextBox.Name = "maxExposureTextBox";
        maxExposureTextBox.Size = new Size(80, 23);
        maxExposureTextBox.TabIndex = 4;
        maxExposureTextBox.Text = "100";
        maxExposureTextBox.TextChanged += maxExposureTextBox_TextChanged;
        // 
        // maxNoPriceLabel
        // 
        maxNoPriceLabel.AutoSize = true;
        maxNoPriceLabel.Location = new Point(190, 9);
        maxNoPriceLabel.Name = "maxNoPriceLabel";
        maxNoPriceLabel.Size = new Size(80, 15);
        maxNoPriceLabel.TabIndex = 5;
        maxNoPriceLabel.Text = "Max No Price:";
        // 
        // maxNoPriceTextBox
        // 
        maxNoPriceTextBox.Location = new Point(280, 9);
        maxNoPriceTextBox.Name = "maxNoPriceTextBox";
        maxNoPriceTextBox.Size = new Size(80, 23);
        maxNoPriceTextBox.TabIndex = 6;
        maxNoPriceTextBox.Text = "50";
        maxNoPriceTextBox.TextChanged += maxNoPriceTextBox_TextChanged;
        // 
        // maxYesPriceLabel
        // 
        maxYesPriceLabel.AutoSize = true;
        maxYesPriceLabel.Location = new Point(370, 9);
        maxYesPriceLabel.Name = "maxYesPriceLabel";
        maxYesPriceLabel.Size = new Size(81, 15);
        maxYesPriceLabel.TabIndex = 7;
        maxYesPriceLabel.Text = "Max Yes Price:";
        // 
        // maxYesPriceTextBox
        // 
        maxYesPriceTextBox.Location = new Point(460, 9);
        maxYesPriceTextBox.Name = "maxYesPriceTextBox";
        maxYesPriceTextBox.Size = new Size(80, 23);
        maxYesPriceTextBox.TabIndex = 8;
        maxYesPriceTextBox.Text = "0.50";
        maxYesPriceTextBox.TextChanged += maxYesPriceTextBox_TextChanged;
        // 
        // refreshButton
        // 
        refreshButton.Location = new Point(320, 58);
        refreshButton.Name = "refreshButton";
        refreshButton.Size = new Size(80, 23);
        refreshButton.TabIndex = 9;
        refreshButton.Text = "Refresh";
        refreshButton.UseVisualStyleBackColor = true;
        refreshButton.Click += refreshButton_Click;
        // 
        // buyNosButton
        // 
        buyNosButton.Location = new Point(581, 8);
        buyNosButton.Name = "buyNosButton";
        buyNosButton.Size = new Size(200, 23);
        buyNosButton.TabIndex = 10;
        buyNosButton.Text = "Event Finished, Buy Nos";
        buyNosButton.UseVisualStyleBackColor = true;
        buyNosButton.Click += buyNosButton_Click;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1000, 500);
        Controls.Add(maxYesPriceTextBox);
        Controls.Add(maxYesPriceLabel);
        Controls.Add(maxNoPriceTextBox);
        Controls.Add(maxNoPriceLabel);
        Controls.Add(maxExposureTextBox);
        Controls.Add(maxExposureLabel);
        Controls.Add(buyNosButton);
        Controls.Add(refreshButton);
        Controls.Add(label1);
        Controls.Add(bingoPanel);
        Controls.Add(eventComboBox);
        Icon = (Icon)resources.GetObject("$this.Icon");
        Name = "Form1";
        Text = "Mention Market Bingo";
        Load += Form1_Load;
        ResumeLayout(false);
        PerformLayout();
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

    #endregion
}
