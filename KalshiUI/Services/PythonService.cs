using KalshiUI.Constants;
using KalshiUI.Extensions;
using System.Diagnostics;
using SmokehousePatterns;

namespace KalshiUI.Services
{
    public class PythonService
    {
        private PythonProcess _orderbookProcess;
        private PythonProcess _lifecycleProcess;
        private PythonProcess _fillProcess;
        private PythonProcess _tradeProcess;
        private PythonProcess _tickerProcess;
        private PythonProcess _eventsProcess;
        private PythonProcess _candlestickProcess;
        private PythonProcess _seriesProcess;
        private PythonProcess _marketsProcess;
        private PythonProcess _chartCandlesticksProcess;

        public PythonProcess OrderbookProcess { get { return AllProcesses[KalshiConstants.ScriptType_Feed_Orderbook]; } }
        public PythonProcess LifecycleProcess { get { return AllProcesses[KalshiConstants.ScriptType_Feed_Lifecycle]; } }
        public PythonProcess FillProcess { get { return AllProcesses[KalshiConstants.ScriptType_Feed_Fill]; } }
        public PythonProcess TradeProcess { get { return AllProcesses[KalshiConstants.ScriptType_Feed_Trade]; } }
        public PythonProcess TickerProcess { get { return AllProcesses[KalshiConstants.ScriptType_Feed_Ticker]; } }
        public PythonProcess EventsProcess { get { return AllProcesses[KalshiConstants.ScriptType_Event]; } }
        public PythonProcess CandlestickProcess { get { return AllProcesses[KalshiConstants.ScriptType_Candlestick]; } }
        public PythonProcess SeriesProcess { get { return AllProcesses[KalshiConstants.ScriptType_Series]; } }
        public PythonProcess MarketsProcess { get { return AllProcesses[KalshiConstants.ScriptType_Market]; } }
        public PythonProcess ChartCandlesticksProcess { get { return AllProcesses[KalshiConstants.ScriptType_ChartCandlesticks]; } }

        public static Dictionary<string, PythonProcess> AllProcesses;

        public void StopOrderbookProcess() { try { OrderbookProcess.Kill(); } catch { } }
        public void StopLifecycleProcess() { try { LifecycleProcess.Kill(); } catch { } }
        public void StopFillProcess() { try { FillProcess.Kill(); } catch { } }
        public void StopTradeProcess() { try { TradeProcess.Kill(); } catch { } }
        public void StopTickerProcess() { try { TickerProcess.Kill(); } catch { } }
        public void StopEventsProcess() { try { EventsProcess.Kill(); } catch { } }
        public void StopCandlestickProcess() { try { CandlestickProcess.Kill(); } catch { } }
        public void StopSeriesProcess() { try { SeriesProcess.Kill(); } catch { } }
        public void StopMarketsProcess() { try { MarketsProcess.Kill(); } catch { } }
        public void StopChartCandlesticksProcess() { try { ChartCandlesticksProcess.Kill(); } catch { } }



        public PythonService()
        {
            _orderbookProcess = new PythonProcess(KalshiConstants.ScriptType_Feed_Orderbook);
            _lifecycleProcess = new PythonProcess(KalshiConstants.ScriptType_Feed_Lifecycle);
            _fillProcess = new PythonProcess(KalshiConstants.ScriptType_Feed_Fill);
            _tradeProcess = new PythonProcess(KalshiConstants.ScriptType_Feed_Trade);
            _tickerProcess = new PythonProcess(KalshiConstants.ScriptType_Feed_Ticker);

            _eventsProcess = new PythonProcess(KalshiConstants.ScriptType_Event);
            _marketsProcess = new PythonProcess(KalshiConstants.ScriptType_Market);
            _seriesProcess = new PythonProcess(KalshiConstants.ScriptType_Series);
            _candlestickProcess = new PythonProcess(KalshiConstants.ScriptType_Candlestick);
            _chartCandlesticksProcess = new PythonProcess(KalshiConstants.ScriptType_ChartCandlesticks);

            AllProcesses = new Dictionary<string, PythonProcess>();
            AllProcesses.Add(KalshiConstants.ScriptType_Feed_Orderbook, _orderbookProcess);
            AllProcesses.Add(KalshiConstants.ScriptType_Feed_Lifecycle, _lifecycleProcess);
            AllProcesses.Add(KalshiConstants.ScriptType_Feed_Fill, _fillProcess);
            AllProcesses.Add(KalshiConstants.ScriptType_Feed_Trade, _tradeProcess);
            AllProcesses.Add(KalshiConstants.ScriptType_Feed_Ticker, _tickerProcess);
            AllProcesses.Add(KalshiConstants.ScriptType_Event, _eventsProcess);
            AllProcesses.Add(KalshiConstants.ScriptType_Candlestick, _candlestickProcess);
            AllProcesses.Add(KalshiConstants.ScriptType_Series, _seriesProcess);
            AllProcesses.Add(KalshiConstants.ScriptType_Market, _marketsProcess);
            AllProcesses.Add(KalshiConstants.ScriptType_ChartCandlesticks, _chartCandlesticksProcess);
        }

        public bool MarketsRunning()
        {
            if (MarketsProcess != null)
            {
                return MarketsProcess.IsRunning;
            }
            return false;
        }

        public bool EventsRunning()
        {
            if (EventsProcess != null)
            {
                return EventsProcess.IsRunning;
            }
            return false;
        }

        public async Task RunOrderbookAsync(string Markets)
        {
            ResetProcess(OrderbookProcess, KalshiConstants.ScriptType_Feed_Orderbook);
            OrderbookProcess.RunScript(new Dictionary<string, string>() {
                            { KalshiConstants.Parameter_Action, KalshiConstants.ScriptType_Feed_Orderbook },
                            { KalshiConstants.Parameter_Market_Ticker, Markets }
                        });
        }

        public async Task RunFillAsync()
        {
            ResetProcess(FillProcess, KalshiConstants.ScriptType_Feed_Fill);
            FillProcess.RunScript(new Dictionary<string, string>() { { KalshiConstants.Parameter_Action, KalshiConstants.ScriptType_Feed_Fill } });
        }

        public async Task RunLifecycleAsync()
        {
            ResetProcess(LifecycleProcess, KalshiConstants.ScriptType_Feed_Lifecycle);
            LifecycleProcess.RunScript(new Dictionary<string, string>() { { KalshiConstants.Parameter_Action, KalshiConstants.ScriptType_Feed_Lifecycle } });
        }

        public async Task RunChartCandlesticksAsync(string marketTicker)
        {
            ResetProcess(ChartCandlesticksProcess, KalshiConstants.ScriptType_ChartCandlesticks);
            ChartCandlesticksProcess.RunScript(new Dictionary<string, string>() { { KalshiConstants.Parameter_Market_Ticker, marketTicker } });
        }

        public async Task RunTickerAsync()
        {
            ResetProcess(TickerProcess, KalshiConstants.ScriptType_Feed_Ticker);
            TickerProcess.RunScript(new Dictionary<string, string>() { { KalshiConstants.Parameter_Action, KalshiConstants.ScriptType_Feed_Ticker } });
        }

        public async Task RunTradeAsync()
        {
            ResetProcess(TradeProcess, KalshiConstants.ScriptType_Feed_Trade);
            TradeProcess.RunScript(new Dictionary<string, string>() { { KalshiConstants.Parameter_Action, KalshiConstants.ScriptType_Feed_Trade } });
        }

        public async Task RunCandlesticksAsync(string seriesTicker, string marketTicker, DateTime minuteStartTime,
            DateTime hourStartTime, DateTime dayStartTime, DateTime? endDate = null)
        {
            long end_ts = endDate.HasValue ? UnixService.ConvertToUnixTimestamp(endDate.Value) : UnixService.ConvertToUnixTimestamp(DateTime.UtcNow);

            ResetProcess(CandlestickProcess, KalshiConstants.ScriptType_Candlestick);
            CandlestickProcess.RunScript(new Dictionary<string, string>
                {
                    { KalshiConstants.Parameter_Series_Ticker, seriesTicker },
                    { KalshiConstants.Parameter_Market_Ticker, marketTicker },
                    { KalshiConstants.Parameter_Start_Ts_m, UnixService.ConvertToUnixTimestamp(minuteStartTime).ToString() },
                    { KalshiConstants.Parameter_Start_Ts_h, UnixService.ConvertToUnixTimestamp(hourStartTime).ToString() },
                    { KalshiConstants.Parameter_Start_Ts_d, UnixService.ConvertToUnixTimestamp(dayStartTime).ToString() },
                    { KalshiConstants.Parameter_End_Ts, end_ts.ToString() }
                });
            await WaitForProcess(CandlestickProcess);
            CandlestickProcess.GracefulShutdown();
        }

        public async Task RunEventsAsync(string seriesTicker, string status = "", bool withNestedMarkets = false)
        {

            Dictionary<string, string> args = new Dictionary<string, string>();
            if (seriesTicker != "") args.Add(KalshiConstants.Parameter_Series_Ticker, seriesTicker);
            if (status != "") args.Add(KalshiConstants.Parameter_Status, status);
            args.Add(KalshiConstants.Parameter_With_Nested_Markets, withNestedMarkets.ToString().ToLower());
            ResetProcess(EventsProcess, KalshiConstants.ScriptType_Event);
            EventsProcess.RunScript(args);
            await WaitForProcess(EventsProcess);
        }

        public async Task RunSeriesAsync(string SeriesTicker)
        {
            ResetProcess(SeriesProcess, KalshiConstants.ScriptType_Series);
            SeriesProcess.RunScript(new Dictionary<string, string>() {
                            { KalshiConstants.Parameter_Series_Ticker, SeriesTicker }
                        });
            await WaitForProcess(SeriesProcess);
        }

        public async Task RunMarketsAsync(string tickers = "", string eventTicker = "", string seriesTicker = "",
            long minCloseTS = 0, long maxCloseTS = 0, string status = "")
        {

            Dictionary<string, string> args = new Dictionary<string, string>();
            if (eventTicker != "") args.Add(KalshiConstants.Parameter_Event_Ticker, eventTicker);
            if (seriesTicker != "") args.Add(KalshiConstants.Parameter_Series_Ticker, seriesTicker);
            if (minCloseTS != 0) args.Add(KalshiConstants.Parameter_Min_Close_TS, minCloseTS.ToString());
            if (maxCloseTS != 0) args.Add(KalshiConstants.Parameter_Max_Close_TS, maxCloseTS.ToString());
            if (status != "") args.Add(KalshiConstants.Parameter_Status, status);
            if (tickers != "") args.Add(KalshiConstants.Parameter_Tickers, tickers);

            ResetProcess(MarketsProcess, KalshiConstants.ScriptType_Market);
            MarketsProcess.RunScript(args);
            await WaitForProcess(MarketsProcess);
        }


        public static void TerminatePythonProcesses()
        {
            try
            {
                // Retrieve all running processes
                Process[] processes = Process.GetProcesses();

                foreach (Process process in processes)
                {
                    try
                    {
                        // Check if the process is a Python process
                        if (process.ProcessName.ToLower().Contains("python"))
                        {
                            System.Diagnostics.Debug.WriteLine($"Terminating process: {process.ProcessName} (PID: {process.Id})");
                            process.Kill();
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Handle access denied exceptions (e.g., for system processes)
                        System.Diagnostics.Debug.WriteLine($"Access denied to terminate process: {process.ProcessName} (PID: {process.Id})");
                    }
                    catch (Exception ex)
                    {
                        // Handle other exceptions
                        System.Diagnostics.Debug.WriteLine($"Error terminating process: {process.ProcessName} (PID: {process.Id}) - {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"An error occurred while retrieving processes: {ex.Message}");
            }
        }

        private async Task WaitForProcess(PythonProcess process)
        {
            while (process.IsRunning)
            {
                await Task.Delay(100);
            }
        }

        private void ResetProcess(PythonProcess Process, string APIType)
        {
            Process.GracefulShutdown();
            Process.Dispose();
            Process = new PythonProcess(APIType);
            AllProcesses[APIType] = Process;
        }
    }
}
