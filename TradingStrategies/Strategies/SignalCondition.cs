using static SmokehouseInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Strategies
{
    public class SignalCondition
    {
        public Signal Signal { get; }
        public ComparisonOperator Operator { get; }
        public int Threshold { get; }
        public ActionType ActionType { get; }

        public SignalCondition(Signal signal, ComparisonOperator op, int threshold, ActionType action)
        {
            Signal = signal;
            Operator = op;
            Threshold = threshold;
            ActionType = action;
        }
    }
}
