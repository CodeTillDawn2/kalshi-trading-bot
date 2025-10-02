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
        SuspendLayout();
        // 
        // eventComboBox
        // 
        eventComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        eventComboBox.FormattingEnabled = true;
        eventComboBox.Location = new Point(12, 32);
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
        bingoPanel.Location = new Point(12, 66);
        bingoPanel.Name = "bingoPanel";
        bingoPanel.RowCount = 5;
        bingoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        bingoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        bingoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        bingoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        bingoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        bingoPanel.Size = new Size(776, 372);
        bingoPanel.TabIndex = 1;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(12, 9);
        label1.Name = "label1";
        label1.Size = new Size(73, 15);
        label1.TabIndex = 2;
        label1.Text = "Select Event:";
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 450);
        Controls.Add(label1);
        Controls.Add(bingoPanel);
        Controls.Add(eventComboBox);
        Icon = (Icon)resources.GetObject("$this.Icon");
        Name = "Form1";
        Text = "Mention Market Bingo";
        ResumeLayout(false);
        PerformLayout();
    }

    private System.Windows.Forms.ComboBox eventComboBox;
    private System.Windows.Forms.TableLayoutPanel bingoPanel;
    private System.Windows.Forms.Label label1;

    #endregion
}
