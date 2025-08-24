// Services/UiRunnerService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TradingSimulator.Simulator; // adjust namespace if different

namespace SimulatorGUI.Services
{
    public sealed class UiRunnerService
    {
        private readonly SimulatorTests _tests;

        public UiRunnerService()
        {
            _tests = new SimulatorTests();

            // Hook the simulator’s events into GUI-friendly events.
            // If your names differ, map accordingly (e.g., _tests.Progress += ...).
            _tests.OnTestProgress += msg => TestProgress?.Invoke(msg);
            _tests.OnProfitLossUpdate += (m, pnl) => ProfitLossUpdate?.Invoke(m, pnl);
            _tests.OnMarketProcessed += m => MarketProcessed?.Invoke(m);

            _tests.Setup(); // if your fixture requires it; remove if not needed
        }

        // Events the VM listens to
        public event Action<string>? TestProgress;
        public event Action<string, double>? ProfitLossUpdate;
        public event Action<string>? MarketProcessed;

        public Task<HashSet<string>> GetSnapshotGroupNames()
            => _tests.GetSnapshotGroupNames();

        public Task RunWeightsForGuiAsync(bool writeToFile, int? maxGroups = null, List<string>? marketsToRun = null)
            => _tests.RunWeightsForGuiAsync(writeToFile, maxGroups, marketsToRun);

        public Task RunMultipleAllStrategiesForGuiAsync(bool writeToFile, int? maxGroups = null, List<string>? marketsToRun = null)
            => _tests.RunMultipleAllStrategiesForGuiAsync(writeToFile, maxGroups, marketsToRun);
    }
}
