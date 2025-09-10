using KalshiBotOverseer.Models;
using System.Collections.Concurrent;

namespace KalshiBotOverseer.Services
{
    public class BrainPersistenceService
    {
        private readonly ConcurrentDictionary<string, BrainPersistence> _brains = new();

        public BrainPersistence GetBrain(string brainInstanceName)
        {
            return _brains.GetOrAdd(brainInstanceName, name => new BrainPersistence { BrainInstanceName = name });
        }

        public void SaveBrain(BrainPersistence brain)
        {
            _brains[brain.BrainInstanceName] = brain;
        }

        public IEnumerable<string> GetTargetMarketTickers(string brainInstanceName)
        {
            var brain = GetBrain(brainInstanceName);
            return brain.TargetMarketTickers;
        }

        public void UpdateCurrentMarketTickers(string brainInstanceName, IEnumerable<string> tickers)
        {
            var brain = GetBrain(brainInstanceName);
            brain.CurrentMarketTickers = new HashSet<string>(tickers);
            SaveBrain(brain);
        }
    }
}