using KalshiUI.Constants;
using KalshiUI.Data;
using KalshiUI.Extensions;
using KalshiUI.Models;
using KalshiUI.Services;
using Microsoft.Extensions.Configuration;
using System.Text;
using SmokehousePatterns;

namespace KalshiUI
{
    public partial class KalshiUI : Form
    {
        private StringBuilder _orderbookOutput;
        private StringBuilder _lifecycleOutput;
        private StringBuilder _fillOutput;
        private StringBuilder _tradeOutput;
        private StringBuilder _tickerOutput;
        private IConfiguration Configuration { get; set; }
        private System.Windows.Forms.Timer UIUpdateTimer;
        private System.Windows.Forms.Timer SynchTimer;

        private SynchronizationService synchronizationService;

        private ModalPopup LogsPopup = null;

        private List<string> originalWatchedItems;
        private List<string> originalUnwatchedItems;

        public KalshiUI()
        {
            InitializeComponent();
            LoadConfiguration();
            if (Configuration["Environment"] == null)
            {
                throw new Exception("Please set Environment variable");
            }
            this.Text = $"KalshiUI - {Configuration["Environment"]?.ToString()}";

            synchronizationService = new SynchronizationService();

            UIUpdateTimer = new System.Windows.Forms.Timer();
            UIUpdateTimer.Interval = 1000;
            UIUpdateTimer.Tick += UpdateTimer_Tick;
            UIUpdateTimer.Start();

            SynchTimer = new System.Windows.Forms.Timer();
            SynchTimer.Interval = 5000;
            SynchTimer.Tick += SynchTimer_Tick;

            using (KalshiBotContext context = new KalshiBotContext())
            {
                synchronizationService.WatchList = context.MarketWatches.ToList();
                synchronizationService.MarketList = context.Markets
                    .Where(x => x.status == "active")
                    .ToList();

                PopulateMarketWatchList();

                lbWatched.Items.Clear();

                foreach (Market market in synchronizationService.MarketList)
                    lbUnwatched.Items.Add(market.market_ticker);

                foreach (MarketWatch watch in synchronizationService.WatchList)
                {
                    lbWatched.Items.Add(watch.market_ticker);
                    lbUnwatched.Items.Remove(watch.market_ticker);
                }
            }

            // Ensure lists are initialized for filtering
            InitializeLists();


        }

        private async void btnStartAutomation_Click(object sender, EventArgs e)
        {

            synchronizationService.SynchStopped = false;
            await Task.Run(async () =>
            {
                SynchronizationService.TerminatePythonProcesses();
                await synchronizationService.InitialSetup();
                await synchronizationService.StartAllProcesses();
            });
            SynchTimer.Start();
        }



        private void PopulateMarketWatchList()
        {
            List<Market> filteredMarketList = synchronizationService.MarketList;


            if (!cbShowAll.Checked)
            {
                cbMarket.DataSource = synchronizationService.MarketList
                    .Select(x => new { x.market_ticker, DisplayText = x.market_ticker + " - " + x.title })
                    .Where(x => synchronizationService.WatchList.Select(x => x.market_ticker).ToList().Contains(x.market_ticker))
                    .ToList();
            }
            else
            {
                cbMarket.DataSource = synchronizationService.MarketList
                    .Select(x => new { x.market_ticker, DisplayText = x.market_ticker + " - " + x.title })
                    .ToList();
            }

            cbMarket.DisplayMember = "DisplayText";
            cbMarket.ValueMember = "market_ticker";
        }

        private void LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();
        }

        private void KalshiUI_Load(object sender, EventArgs e)
        {
            SynchronizationService.TerminatePythonProcesses();

        }


        private async void SynchTimer_Tick(object sender, EventArgs e)
        {
            if (synchronizationService.IsRunning == false) await synchronizationService.Synch();
        }


        private void UpdateProcessGUI(PythonProcess process, Label label)
        {
            if (InvokeRequired)
            {
                if (process.Output.Count > 0) Invoke(new Action(() => label.Text = process.Output.Last()));

            }
            else
            {
                if (process.Output.Count > 0) label.Text = process.Output.Last();
            }
        }

        private void InitializeLists()
        {
            originalWatchedItems = lbWatched.Items.Cast<string>().ToList();
            originalUnwatchedItems = lbUnwatched.Items.Cast<string>().ToList();
        }

        private async void UpdateTimer_Tick(object sender, EventArgs e)
        {

            //RefreshMarketWatch();

            if (SchedulerService.CurrentMode == SchedulerService.Mode_DayTime)
            {
                pbMode.Image = Image.FromFile(@"icons\sunicon.png");
            }
            else if (SchedulerService.CurrentMode == SchedulerService.Mode_NightTime)
            {
                pbMode.Image = Image.FromFile(@"icons\moonicon.png");
            }
            else
            {
                pbMode.Image = null;
            }

            lblAutomationStatus.Text = synchronizationService.AutomationStatusText;

            UpdateProcessGUI(synchronizationService.OrderbookProcess, lblSyncOrderbookStatus);
            UpdateProcessGUI(synchronizationService.FillProcess, lblSyncFillStatus);
            UpdateProcessGUI(synchronizationService.LifecycleProcess, lblSyncLifecycleStatus);
            UpdateProcessGUI(synchronizationService.TickerProcess, lblSyncTickerStatus);
            UpdateProcessGUI(synchronizationService.TradeProcess, lblSyncTradeStatus);

            UpdateProcessGUI(synchronizationService.MarketsProcess, lblSyncMarketStatus);
            UpdateProcessGUI(synchronizationService.EventsProcess, lblSyncEventStatus);
            UpdateProcessGUI(synchronizationService.SeriesProcess, lblSyncSeriesStatus);
            UpdateProcessGUI(synchronizationService.CandlestickProcess, lblSyncCandlesticksStatus);


            if (LogsPopup != null && LogsPopup.Visible)
            {

                if (LogsPopup.Text == UIConstants.FillLogName)
                {
                    LogsPopup.RefreshLogs(synchronizationService.FillProcess.Output);
                }
                else if (LogsPopup.Text == UIConstants.OrderbookLogName)
                {
                    LogsPopup.RefreshLogs(synchronizationService.OrderbookProcess.Output);
                }
                else if (LogsPopup.Text == UIConstants.TradeLogName)
                {
                    LogsPopup.RefreshLogs(synchronizationService.TradeProcess.Output);
                }
                else if (LogsPopup.Text == UIConstants.LifecycleLogName)
                {
                    LogsPopup.RefreshLogs(synchronizationService.LifecycleProcess.Output);
                }
                else if (LogsPopup.Text == UIConstants.TickerLogName)
                {
                    LogsPopup.RefreshLogs(synchronizationService.TickerProcess.Output);
                }
                else if (LogsPopup.Text == UIConstants.MarketsLogName)
                {
                    LogsPopup.RefreshLogs(synchronizationService.MarketsProcess.Output);
                }
                else if (LogsPopup.Text == UIConstants.EventsLogName)
                {
                    LogsPopup.RefreshLogs(synchronizationService.EventsProcess.Output);
                }
                else if (LogsPopup.Text == UIConstants.SeriesLogName)
                {
                    LogsPopup.RefreshLogs(synchronizationService.SeriesProcess.Output);
                }
                else if (LogsPopup.Text == UIConstants.CandlesticksLogName)
                {
                    LogsPopup.RefreshLogs(synchronizationService.CandlestickProcess.Output);
                }

            }
        }



        private void KalshiUI_FormClosed(object sender, FormClosedEventArgs e)
        {
            SynchronizationService.TerminatePythonProcesses();
            if (UIUpdateTimer != null)
            {
                UIUpdateTimer.Tick -= UpdateTimer_Tick;
                UIUpdateTimer.Stop();
                UIUpdateTimer.Dispose();
            }

            if (SynchTimer != null)
            {
                SynchTimer.Tick -= SynchTimer_Tick;
                SynchTimer.Stop();
                SynchTimer.Dispose();
            }
        }


        private async void btnStopAutomation_Click(object sender, EventArgs e)
        {
            SynchTimer.Stop();
            synchronizationService.SynchStopped = true;
            SynchronizationService.TerminatePythonProcesses();
        }



        private void RefreshMarketWatch()
        {
            using (KalshiBotContext context = new())
            {
                var activeMarkets = context.Markets.Where(x => x.status == KalshiConstants.Status_Active);

                int freshCount = activeMarkets.Where(x => x.LastCandlestick > DateTime.UtcNow.AddDays(-1)).Count();
                int staleCount = activeMarkets.Where(x => x.LastCandlestick <= DateTime.UtcNow.AddDays(-1)).Count();

                lblStale.Text = "Stale: " + staleCount.ToString();
                lblFresh.Text = "Fresh: " + freshCount.ToString();

                Market? selectedMarket = null;
                if (cbMarket.Text.Length > 0)
                {

                    selectedMarket = activeMarkets.FirstOrDefault(x => x.market_ticker == cbMarket.SelectedValue.ToString());
                    if (selectedMarket != null)
                    {
                        lblMarketWatch_Price.Text = selectedMarket.last_price.ToString();
                        lblMarketWatch_YesBid.Text = selectedMarket.yes_bid.ToString();
                        lblMarketWatch_YesAsk.Text = selectedMarket.yes_ask.ToString();
                        lblMarketWatch_NoBid.Text = selectedMarket.no_bid.ToString();
                        lblMarketWatch_NoAsk.Text = selectedMarket.no_ask.ToString();
                        lblMarketWatch_Volume.Text = selectedMarket.volume.ToString();
                        lblMarketWatch_Interest.Text = selectedMarket.open_interest.ToString();

                        lblLastCandlestick.Text = selectedMarket.LastCandlestick.ToString();
                        //lblMarketWatch_Interest.Text = selectedMarket..ToString();
                    }
                }
            }
        }

        private void HandleLogsPopup(List<string> output, string Name)
        {
            if (LogsPopup != null)
            {
                LogsPopup.Close();
                LogsPopup = null;
            }

            LogsPopup = new ModalPopup();
            LogsPopup.Text = Name;
            LogsPopup.RefreshLogs(output);
            LogsPopup.ShowDialog();
        }

        private void btnLogs_Fill_Click(object sender, EventArgs e)
        {
            HandleLogsPopup(synchronizationService.FillProcess.Output, UIConstants.FillLogName);
        }

        private void btnLogs_Lifecycle_Click(object sender, EventArgs e)
        {
            HandleLogsPopup(synchronizationService.LifecycleProcess.Output, UIConstants.LifecycleLogName);
        }

        private void btnLogs_Orderbook_Click(object sender, EventArgs e)
        {
            HandleLogsPopup(synchronizationService.OrderbookProcess.Output, UIConstants.OrderbookLogName);
        }

        private void btnLogs_Ticker_Click(object sender, EventArgs e)
        {
            HandleLogsPopup(synchronizationService.TickerProcess.Output, UIConstants.TickerLogName);
        }

        private void btnLogs_Trade_Click(object sender, EventArgs e)
        {
            HandleLogsPopup(synchronizationService.TradeProcess.Output, UIConstants.TradeLogName);
        }

        private void cbMarket_SelectedIndexChanged(object sender, EventArgs e)
        {

        }



        private void btnLogs_Markets_Click(object sender, EventArgs e)
        {
            HandleLogsPopup(synchronizationService.MarketsProcess.Output, UIConstants.MarketsLogName);
        }

        private void btnLogs_Events_Click(object sender, EventArgs e)
        {
            HandleLogsPopup(synchronizationService.EventsProcess.Output, UIConstants.EventsLogName);
        }

        private void btnLogs_Series_Click(object sender, EventArgs e)
        {
            HandleLogsPopup(synchronizationService.SeriesProcess.Output, UIConstants.SeriesLogName);
        }

        private void btnLogs_Candlesticks_Click(object sender, EventArgs e)
        {
            HandleLogsPopup(synchronizationService.CandlestickProcess.Output, UIConstants.CandlesticksLogName);
        }

        private void cbShowAll_CheckedChanged(object sender, EventArgs e)
        {
            PopulateMarketWatchList();
        }

        private void btnSendRight_Click(object sender, EventArgs e)
        {
            if (lbWatched.SelectedItem != null)
            {
                string MovedItem = lbWatched.SelectedItem.ToString();
                lbUnwatched.Items.Add(MovedItem);
                lbWatched.Items.Remove(MovedItem);

                using (KalshiBotContext context = new())
                {
                    MarketWatch watch = context.MarketWatches.FirstOrDefault(x => x.market_ticker == MovedItem);
                    if (watch != null)
                    {
                        context.MarketWatches.Remove(watch);
                        context.SaveChanges();
                    }
                }

                UpdateWatchedList();
                UpdateUnwatchedList();
            }
        }

        private void btnSendLeft_Click(object sender, EventArgs e)
        {
            if (lbUnwatched.SelectedItem != null)
            {
                string MovedItem = lbUnwatched.SelectedItem.ToString();
                lbWatched.Items.Add(MovedItem);
                lbUnwatched.Items.Remove(MovedItem);

                using (KalshiBotContext context = new KalshiBotContext())
                {
                    MarketWatch watch = context.MarketWatches.FirstOrDefault(x => x.market_ticker == MovedItem);
                    if (watch == null)
                    {
                        watch = new MarketWatch() { market_ticker = MovedItem };
                        context.MarketWatches.Add(watch);
                        context.SaveChanges();
                    }
                }

                UpdateWatchedList();
                UpdateUnwatchedList();
            }
        }

        private void UpdateWatchedList()
        {
            originalWatchedItems = lbWatched.Items.Cast<string>().ToList();
        }

        private void UpdateUnwatchedList()
        {
            originalUnwatchedItems = lbUnwatched.Items.Cast<string>().ToList();
        }

        private async void btnShowCandlesticks_Click(object sender, EventArgs e)
        {
            if (cbMarket.SelectedValue.ToString() != "")
                await synchronizationService.ShowCandlesticks(cbMarket.SelectedValue.ToString());
        }

        private void filterWatched_TextChanged(object sender, EventArgs e)
        {
            string filter = filterWatched.Text.ToLower();
            lbWatched.Items.Clear();
            foreach (var item in originalWatchedItems)
            {
                if (item.ToLower().Contains(filter))
                    lbWatched.Items.Add(item);
            }
        }

        private void filterUnwatched_TextChanged(object sender, EventArgs e)
        {
            string filter = filterUnwatched.Text.ToLower();
            lbUnwatched.Items.Clear();
            foreach (var item in originalUnwatchedItems)
            {
                if (item.ToLower().Contains(filter))
                    lbUnwatched.Items.Add(item);
            }
        }

        private async void btnRunParquets_Click(object sender, EventArgs e)
        {
            await RunParquets();
        }


        private async Task RunParquets()
        {
            if (!MarketStateService.Running)
            {
                // Create an action to update the UI label text
                Action<string> updateStatus = (status) =>
                {
                    if (lblParqetStatus.InvokeRequired)
                    {
                        lblParqetStatus.Invoke(new Action(() => lblParqetStatus.Text = status));
                    }
                    else
                    {
                        lblParqetStatus.Text = status;
                    }
                };

                // Start the async operation
                await MarketStateService.StartExportingParquets(updateStatus);
            }
        }

        private async void btnRunPatterns_Click(object sender, EventArgs e)
        {
            await RunPatterns();
        }

        private async void btnRunConfig_Click(object sender, EventArgs e)
        {
            await RunConfigs();
        }

        private async Task RunPatterns()
        {
            if (!PatternDetectionService.PatternsRunning)
            {
                // Create an action to update the UI label text
                Action<string> updateStatus = (status) =>
                {
                    if (lblPatternStatus.InvokeRequired)
                    {
                        lblPatternStatus.Invoke(new Action(() => lblPatternStatus.Text = status));
                    }
                    else
                    {
                        lblPatternStatus.Text = status;
                    }
                };

                // Disable the button to prevent multiple clicks
                btnRunPatterns.Enabled = false;

                try
                {
                    // Offload the pattern detection to a background thread and await it
                    PatternDetectionService service = new(Configuration);
                    await Task.Run(() => service.StartDetectingPatterns(updateStatus));
                }
                finally
                {
                    // Re-enable the button when done
                    btnRunPatterns.Enabled = true;
                }
            }
        }

        private async Task RunConfigs()
        {
            if (!PatternDetectionService.ConfigsRunning)
            {
                // Create an action to update the UI label text
                Action<string> updateStatus = (status) =>
                {
                    if (lblConfigStatus.InvokeRequired)
                    {
                        lblConfigStatus.Invoke(new Action(() => lblConfigStatus.Text = status));
                    }
                    else
                    {
                        lblConfigStatus.Text = status;
                    }
                };

                // Disable the button to prevent multiple clicks
                btnRunConfig.Enabled = false;

                try
                {
                    // Offload the pattern detection to a background thread and await it
                    PatternDetectionService service = new PatternDetectionService(Configuration);
                    await Task.Run(() => service.BuildPatternConfigs(updateStatus, false));
                }
                finally
                {
                    // Re-enable the button when done
                    btnRunConfig.Enabled = true;
                }
            }
        }

        private async void btnRunBoth_Click(object sender, EventArgs e)
        {
            if (!PatternDetectionService.PatternsRunning && !PatternDetectionService.ConfigsRunning)
            {
                await RunParquets();
                await RunPatterns();
                //await RunConfigs();
            }
        }
    }
}
