using System.Collections.Concurrent;

namespace KalshiBotOverseer.State
{
    public class DataCache
    {
        private double _accountBalance = 0.0;
        private double _portfolioValue = 0.0;

        public double AccountBalance
        {
            get => _accountBalance;
            set => _accountBalance = value;
        }

        public double PortfolioValue
        {
            get => _portfolioValue;
            set => _portfolioValue = value;
        }
    }
}
