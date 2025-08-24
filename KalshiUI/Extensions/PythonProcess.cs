
using KalshiUI.Constants;
using System.Diagnostics;
using SmokehousePatterns;
namespace KalshiUI.Extensions
{
    public class PythonProcess : Process
    {

        private string _apiType;

        private string _scriptLocation;
        public string ScriptLocation { get { return _scriptLocation; } }

        public bool IsRunning;
        public bool HasStarted;

        private List<string> _output = new List<string>();
        public List<string> Output { get { return _output; } }

        private string _scriptFeed = @"..\..\..\..\Python\KalshiAPI\main.py";
        private string _scriptEvents = @"..\..\..\..\Python\KalshiAPI\events.py";
        private string _scriptSeries = @"..\..\..\..\Python\KalshiAPI\series.py";
        private string _scriptCandlesticks = @"..\..\..\..\Python\KalshiAPI\candlesticks.py";
        private string _scriptCandlesticks2 = @"..\..\..\..\Python\KalshiAPI\candlesticks_by_market.py";
        private string _scriptMarkets = @"..\..\..\..\Python\KalshiAPI\markets.py";
        private string _scriptChartCandlesticks = @"..\..\..\..\Python\StatisticalAnalysis\chartCandlesticks.py";

        public PythonProcess(string APIType)
        {
            _apiType = APIType;
            switch (APIType)
            {
                case KalshiConstants.ScriptType_Candlestick:
                    _scriptLocation = _scriptCandlesticks2;
                    //_scriptLocation = _scriptCandlesticks;
                    break;
                case KalshiConstants.ScriptType_Event:
                    _scriptLocation = _scriptEvents;
                    break;
                case KalshiConstants.ScriptType_Market:
                    _scriptLocation = _scriptMarkets;
                    break;
                case KalshiConstants.ScriptType_Series:
                    _scriptLocation = _scriptSeries;
                    break;
                case KalshiConstants.ScriptType_ChartCandlesticks:
                    _scriptLocation = _scriptChartCandlesticks;
                    break;
                case KalshiConstants.ScriptType_Feed_Fill:
                case KalshiConstants.ScriptType_Feed_Lifecycle:
                case KalshiConstants.ScriptType_Feed_Orderbook:
                case KalshiConstants.ScriptType_Feed_Ticker:
                case KalshiConstants.ScriptType_Feed_Trade:
                    _scriptLocation = _scriptFeed;
                    break;
            }

            IsRunning = false;
        }

        public void RunScript(Dictionary<string, string>? argumentDict)
        {
            string? passedArgs = null;
            if (argumentDict != null)
            {
                passedArgs = string.Join(" ", argumentDict.Select(kv => $"--{kv.Key} {kv.Value}"));
            }
            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"{ScriptLocation} {passedArgs}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            StartInfo = startInfo;

            OutputDataReceived += HandleOutputDataReceived;

            ErrorDataReceived += HandleErrorDataReceived;

            IsRunning = true;
            HasStarted = true;
            Start();
            BeginOutputReadLine();
        }

        public void GracefulShutdown()
        {
            try
            {
                if (HasStarted)
                {

                    bool test = this.HasStarted;
                    OutputDataReceived -= HandleOutputDataReceived;
                    ErrorDataReceived -= HandleErrorDataReceived;
                    Close();
                }
            }
            catch { }
            finally { Dispose(); }
        }

        private void HandleOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                IsRunning = false;
                Output.Add("Stopped");
            }
            else if (e.Data == "")
            {
                //Do nothing
            }
            else
            {
                if (e.Data.ToString().Length >= 10 && e.Data.ToString().Substring(0, 10) == "COMPLETED:")
                {
                    IsRunning = false;
                }
                else
                {
                    if (Output.Count > 100)
                    {
                        Output.RemoveRange(0, Output.Count - 100);
                    }
                    Output.Add($"{DateTime.Now}: {e.Data}");
                    Debug.WriteLine(e.Data);
                }
            }
        }

        private void HandleErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Output.Add(e.Data);
                Debug.WriteLine($"Error: {DateTime.Now}: {e.Data}");
                throw new Exception($"Python error while running script: {_apiType}. {e.Data}");
            }
        }
    }
}
