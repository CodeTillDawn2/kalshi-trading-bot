using System.ComponentModel.DataAnnotations;

namespace TradingStrategies.Configuration;

/// <summary>
/// Configuration class for risk management parameters in trading strategies.
/// Controls position limits, stop-loss thresholds, and portfolio-level risk constraints.
/// </summary>
public class RiskManagementConfig
{
    [Range(0.01, 1.0)]
    /// <summary>
    /// Maximum position size limit as a percentage of total portfolio value.
    /// This prevents over-concentration in any single market position.
    /// Typical values: 0.05-0.20 (5%-20%) depending on risk tolerance and diversification strategy.
    /// Used by TradingStrategy to limit position sizes during simulation.
    /// </summary>
    public double MaxPositionSizePercent { get; set; } = 0.10;

    [Range(0.01, 2.0)]
    /// <summary>
    /// Maximum total exposure limit as a percentage of total portfolio value.
    /// This controls the overall risk level across all positions.
    /// Typical values: 0.50-2.0 (50%-200%) depending on leverage and risk tolerance.
    /// Used by TradingStrategy to prevent excessive total exposure.
    /// </summary>
    public double MaxTotalExposurePercent { get; set; } = 1.0;

    [Range(0.01, 1.0)]
    /// <summary>
    /// Stop-loss threshold as a percentage of entry price.
    /// Positions will be automatically closed if losses exceed this threshold.
    /// Typical values: 0.05-0.20 (5%-20%) depending on volatility and risk tolerance.
    /// Used by TradingStrategy for risk management during simulation.
    /// </summary>
    public double StopLossPercent { get; set; } = 0.10;

    [Range(0.01, 1.0)]
    /// <summary>
    /// Take-profit threshold as a percentage of entry price.
    /// Positions will be automatically closed if gains exceed this threshold.
    /// Typical values: 0.05-0.50 (5%-50%) depending on profit targets and market conditions.
    /// Used by TradingStrategy for profit-taking during simulation.
    /// </summary>
    public double TakeProfitPercent { get; set; } = 0.20;

    [Range(1, 100)]
    /// <summary>
    /// Maximum number of concurrent positions allowed.
    /// This limits portfolio diversification and position management complexity.
    /// Typical values: 5-20 depending on available capital and management capacity.
    /// Used by TradingStrategy to prevent over-diversification.
    /// </summary>
    public int MaxConcurrentPositions { get; set; } = 10;

    [Range(0.01, 1.0)]
    /// <summary>
    /// Maximum drawdown limit as a percentage of total portfolio value.
    /// Simulation will pause or stop if drawdown exceeds this threshold.
    /// Typical values: 0.10-0.30 (10%-30%) depending on risk tolerance.
    /// Used by TradingStrategy for portfolio-level risk management.
    /// </summary>
    public double MaxDrawdownPercent { get; set; } = 0.20;
}