namespace KalshiUI
{
    public partial class ModalPopup : Form
    {

        private List<string> data = new List<string>();
        private TextBox txtLogs;

        public ModalPopup()
        {
            InitializeComponent();
            txtLogs = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.Black,
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Courier New", 10),
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true
            };


            // Add the TextBox to the form
            this.Controls.Add(txtLogs);


        }

        public void RefreshLogs(List<string> logs)
        {
            // Assuming 'data' holds the logs currently displayed in the TextBox
            List<string> newLogs = logs.Except(data).ToList();  // Find the new logs that are not yet added

            // Add the new logs to the TextBox
            foreach (var log in newLogs)
            {
                txtLogs.AppendText(log + Environment.NewLine);
            }

            // Update the 'data' list with the newly added logs
            data.AddRange(newLogs);  // Keep track of the logs already displayed
        }

        private void ModalPopup_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.Text = "";
            data = new List<string>();
        }

        private void ModalPopup_Shown(object sender, EventArgs e)
        {
            data = new List<string>();
        }
    }
}
