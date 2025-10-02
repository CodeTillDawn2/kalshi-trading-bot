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
        this.components = new System.ComponentModel.Container();
        this.eventComboBox = new System.Windows.Forms.ComboBox();
        this.bingoPanel = new System.Windows.Forms.FlowLayoutPanel();
        this.label1 = new System.Windows.Forms.Label();
        this.SuspendLayout();
        //
        // eventComboBox
        //
        this.eventComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.eventComboBox.FormattingEnabled = true;
        this.eventComboBox.Location = new System.Drawing.Point(12, 32);
        this.eventComboBox.Name = "eventComboBox";
        this.eventComboBox.Size = new System.Drawing.Size(300, 28);
        this.eventComboBox.TabIndex = 0;
        this.eventComboBox.SelectedIndexChanged += new System.EventHandler(this.eventComboBox_SelectedIndexChanged);
        //
        // bingoPanel
        //
        this.bingoPanel.AutoScroll = true;
        this.bingoPanel.Location = new System.Drawing.Point(12, 66);
        this.bingoPanel.Name = "bingoPanel";
        this.bingoPanel.Size = new System.Drawing.Size(776, 372);
        this.bingoPanel.TabIndex = 1;
        //
        // label1
        //
        this.label1.AutoSize = true;
        this.label1.Location = new System.Drawing.Point(12, 9);
        this.label1.Name = "label1";
        this.label1.Size = new System.Drawing.Size(89, 20);
        this.label1.TabIndex = 2;
        this.label1.Text = "Select Event:";
        //
        // Form1
        //
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 450);
        this.Controls.Add(this.label1);
        this.Controls.Add(this.bingoPanel);
        this.Controls.Add(this.eventComboBox);
        this.Name = "Form1";
        this.Text = "Mention Market Bingo";
        this.Load += new System.EventHandler(this.Form1_Load);
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private System.Windows.Forms.ComboBox eventComboBox;
    private System.Windows.Forms.FlowLayoutPanel bingoPanel;
    private System.Windows.Forms.Label label1;

    #endregion
}
