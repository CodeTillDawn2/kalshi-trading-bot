import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
from typing import Dict, Any
import seaborn as sns
import json
import os

class EmptyMetrics:
    """Stub class for handling empty trade scenarios"""
    def __init__(self):
        self.trades_df = pd.DataFrame(columns=[
            'market', 'direction', 'entry_time', 'exit_time',
            'entry_price', 'exit_price', 'position_size',
            'shares', 'pnl', 'balance_after', 'hold_periods', 'spread_cost'
        ])
        self.market_df = pd.DataFrame(columns=[
            'market', 'initial_balance', 'final_balance',
            'total_return', 'wins', 'losses', 'win_rate'
        ])
    
    def calculate_risk_adjusted_metrics(self) -> Dict[str, float]:
        return {
            'risk_adjusted_return': 0.0,
            'avg_risk_per_trade': 0.0,
            'profit_per_unit_risk': 0.0
        }
        
    def _calculate_win_loss_ratio(self) -> float:
        return 0.0
        
    def _calculate_profit_factor(self) -> float:
        return 0.0
        
    def _calculate_sharpe_ratio(self) -> float:
        return 0.0
        
    def _calculate_max_drawdown(self) -> float:
        return 0.0
        
    def calculate_value_at_risk(self) -> float:
        return 0.0

class TradingMetrics:
    """Handles core trading performance metrics calculations."""
    
    def __init__(self, trades_file: str, market_summary_file: str):
        self.trades_df = pd.read_csv(trades_file)
        self.market_df = pd.read_csv(market_summary_file)
        
        # Convert timestamps to datetime
        self.trades_df['entry_time'] = pd.to_datetime(self.trades_df['entry_time'])
        self.trades_df['exit_time'] = pd.to_datetime(self.trades_df['exit_time'])
        
        # Validate data after loading
        self._validate_trade_data()

    def _validate_trade_data(self):
        """Validate and clean trade data before calculations."""
        # Ensure required columns exist
        required_columns = ['entry_time', 'exit_time', 'pnl', 'balance_after']
        missing_columns = [col for col in required_columns if col not in self.trades_df.columns]
        if missing_columns:
            raise ValueError(f"Missing required columns: {missing_columns}")
        
        # Convert timestamps if they're strings
        if self.trades_df['entry_time'].dtype == 'object':
            self.trades_df['entry_time'] = pd.to_datetime(self.trades_df['entry_time'])
        if self.trades_df['exit_time'].dtype == 'object':
            self.trades_df['exit_time'] = pd.to_datetime(self.trades_df['exit_time'])
        
        # Replace infinite values with NaN
        self.trades_df = self.trades_df.replace([np.inf, -np.inf], np.nan)
        
        # Forward fill any NaN in balance_after using newer pandas method
        if 'balance_after' in self.trades_df.columns:
            self.trades_df['balance_after'] = self.trades_df['balance_after'].ffill()
        
        # Drop rows with critical NaN values
        self.trades_df = self.trades_df.dropna(subset=['pnl', 'balance_after'])
        
        return self.trades_df

    def _calculate_win_loss_ratio(self) -> float:
        """Calculate the ratio of winning trades to total trades."""
        if len(self.trades_df) == 0:
            return 0.0
        
        winning_trades = len(self.trades_df[self.trades_df['pnl'] > 0])
        return winning_trades / len(self.trades_df)

    def _calculate_profit_factor(self) -> float:
        """Calculate ratio of gross profits to gross losses."""
        if len(self.trades_df) == 0:
            return 0.0
        
        gross_profits = self.trades_df[self.trades_df['pnl'] > 0]['pnl'].sum()
        gross_losses = abs(self.trades_df[self.trades_df['pnl'] < 0]['pnl'].sum())
        
        return gross_profits / gross_losses if gross_losses != 0 else float('inf')

    def _calculate_sharpe_ratio(self, risk_free_rate: float = 0.02) -> float:
        """Calculate Sharpe ratio using daily returns."""
        if len(self.trades_df) == 0:
            return 0.0
        
        daily_returns = self.trades_df.groupby(
            self.trades_df['entry_time'].dt.date)['pnl'].sum()
        
        if len(daily_returns) == 0:
            return 0.0
            
        excess_returns = daily_returns.mean() - (risk_free_rate / 252)
        daily_std = daily_returns.std()
        
        return (excess_returns / daily_std * np.sqrt(252)) if daily_std != 0 else 0.0

    def _calculate_max_drawdown(self) -> float:
        """Calculate maximum peak-to-trough drawdown."""
        if len(self.trades_df) == 0:
            return 0.0
        
        cumulative_returns = self.trades_df['pnl'].cumsum()
        rolling_max = cumulative_returns.expanding().max()
        drawdowns = (cumulative_returns - rolling_max) / rolling_max
        
        return abs(drawdowns.min()) if len(drawdowns) > 0 else 0.0

    def _calculate_max_drawdown(self) -> float:
        """Calculate maximum peak-to-trough drawdown using account balance."""
        if len(self.trades_df) == 0:
            return 0.0
        
        balance = self.trades_df['balance_after'].values
        peak = np.maximum.accumulate(balance)
        drawdown = (peak - balance) / peak
        return float(np.max(drawdown)) if len(drawdown) > 0 else 0.0

    def calculate_value_at_risk(self, confidence_level: float = 0.95) -> float:
        """Calculate Value at Risk as percentage of account balance."""
        if len(self.trades_df) == 0:
            return 0.0
        
        # Calculate returns as percentage of account balance
        pct_returns = self.trades_df['pnl'] / self.trades_df['balance_after']
        var = np.percentile(pct_returns, (1 - confidence_level) * 100)
        return abs(float(var))

    def calculate_risk_adjusted_metrics(self) -> Dict[str, float]:
        """Calculate risk-adjusted performance metrics."""
        return {
            'risk_adjusted_return': (self.trades_df['pnl'] / 
                                self.trades_df['position_size']).mean(),
            'avg_risk_per_trade': (self.trades_df['position_size'] / 
                                self.trades_df['balance_after']).mean(),
            'profit_per_unit_risk': (self.trades_df['pnl'] / 
                                    self.trades_df['position_size']).sum()
        }


class RiskMetrics:
    """Handles risk-related metrics calculations."""
    
    def __init__(self, trades_file: str, market_summary_file: str):
        self.trades_df = pd.read_csv(trades_file)
        self.market_df = pd.read_csv(market_summary_file)
        
        # Convert timestamps
        self.trades_df['entry_time'] = pd.to_datetime(self.trades_df['entry_time'])
        self.trades_df['exit_time'] = pd.to_datetime(self.trades_df['exit_time'])

    def calculate_value_at_risk(self, confidence_level: float = 0.95) -> float:
        """Calculate Value at Risk using historical simulation method."""
        if len(self.trades_df) == 0:
            return 0.0
            
        returns = self.trades_df['pnl']
        var = np.percentile(returns, (1 - confidence_level) * 100)
        return abs(var)

    def calculate_volatility(self) -> float:
        """Calculate annualized volatility of returns."""
        if len(self.trades_df) == 0:
            return 0.0
            
        daily_returns = self.trades_df.groupby(
            pd.to_datetime(self.trades_df['entry_time']).dt.date)['pnl'].sum()
        return daily_returns.std() * np.sqrt(252) if len(daily_returns) > 0 else 0.0

    def calculate_risk_reward_ratio(self) -> float:
        """Calculate risk/reward ratio (average win / average loss)."""
        if len(self.trades_df) == 0:
            return 0.0
            
        wins = self.trades_df[self.trades_df['pnl'] > 0]['pnl']
        losses = self.trades_df[self.trades_df['pnl'] < 0]['pnl']
        
        avg_win = wins.mean() if len(wins) > 0 else 0
        avg_loss = abs(losses.mean()) if len(losses) > 0 else 0
        
        return avg_win / avg_loss if avg_loss != 0 else float('inf')

    def calculate_max_consecutive_losses(self) -> int:
        """Calculate maximum consecutive losing trades."""
        if len(self.trades_df) == 0:
            return 0
            
        trades = (self.trades_df['pnl'] > 0).astype(int)
        loss_streaks = ''.join(trades.astype(str)).split('1')
        return max(len(streak) for streak in loss_streaks)


class MarketTimingMetrics:
    """Handles market timing and trade duration metrics."""
    
    def __init__(self, trades_file: str, market_summary_file: str):
        self.trades_df = pd.read_csv(trades_file)
        self.market_df = pd.read_csv(market_summary_file)
        
        # Convert trade timestamps
        self.trades_df['entry_time'] = pd.to_datetime(self.trades_df['entry_time'])
        self.trades_df['exit_time'] = pd.to_datetime(self.trades_df['exit_time'])

    def calculate_average_hold_time(self) -> float:
        """Calculate average time trades are held (in hours)."""
        if len(self.trades_df) == 0:
            return 0.0
            
        hold_times = (self.trades_df['exit_time'] - 
                     self.trades_df['entry_time']).dt.total_seconds() / 3600
        return hold_times.mean()

    def calculate_best_trading_hour(self) -> int:
        """Determine most profitable hour of trading."""
        if len(self.trades_df) == 0:
            return 0
            
        hourly_pnl = self.trades_df.groupby(
            self.trades_df['entry_time'].dt.hour)['pnl'].mean()
        return hourly_pnl.idxmax() if not hourly_pnl.empty else 0

    def calculate_worst_trading_hour(self) -> int:
        """Determine least profitable hour of trading."""
        if len(self.trades_df) == 0:
            return 0
            
        hourly_pnl = self.trades_df.groupby(
            self.trades_df['entry_time'].dt.hour)['pnl'].mean()
        return hourly_pnl.idxmin() if not hourly_pnl.empty else 0

    def calculate_market_participation_rate(self) -> float:
        """Calculate market participation rate based on unique markets traded."""
        if len(self.trades_df) == 0 or len(self.market_df) == 0:
            return 0.0
        
        # Calculate participation based on markets traded vs available markets
        traded_markets = set(self.trades_df['market'].unique())
        available_markets = set(self.market_df['market'].unique())
        
        if len(available_markets) == 0:
            return 0.0
            
        return len(traded_markets) / len(available_markets)

class MetricsManager:
    """Main interface for all metrics calculations and reporting."""
    
    def __init__(self, trades_file: str, market_summary_file: str):
        try:
            # Check if files are empty
            if os.path.getsize(trades_file) == 0 or os.path.getsize(market_summary_file) == 0:
                print("Warning: Empty trade data files")
                # Create empty DataFrames with expected columns
                self.trading = self._create_empty_metrics()
                return
                
            self.trading = TradingMetrics(trades_file, market_summary_file)
            self.risk = RiskMetrics(trades_file, market_summary_file)
            self.timing = MarketTimingMetrics(trades_file, market_summary_file)
        except Exception as e:
            print(f"Error initializing metrics: {e}")
            self.trading = self._create_empty_metrics()
            
    def _create_empty_metrics(self):
        """Create empty metrics structure when no trades exist"""
        return type('EmptyMetrics', (), {
            'trades_df': pd.DataFrame(columns=[
                'market', 'direction', 'entry_time', 'exit_time',
                'entry_price', 'exit_price', 'position_size',
                'shares', 'pnl', 'balance_after', 'hold_periods', 'spread_cost'
            ]),
            'market_df': pd.DataFrame(columns=[
                'market', 'initial_balance', 'final_balance',
                'total_return', 'wins', 'losses', 'win_rate'
            ])
        })()

    def plot_metrics(self):
        """Generate visualization of key trading metrics."""
        try:
            plt.style.use('default')
            fig = plt.figure(figsize=(15, 10))
            gs = fig.add_gridspec(3, 2)
            
            trades_df = self.trading.trades_df  # Access through trading instance
            
            # 1. Equity Curve
            ax1 = fig.add_subplot(gs[0, :])
            if len(trades_df) > 0 and 'balance_after' in trades_df.columns:
                initial_balance = trades_df['balance_after'].iloc[0]
                equity_curve = trades_df['balance_after'].copy()
                
                ax1.plot(range(len(equity_curve)), equity_curve, 
                        label='Account Balance', color='blue', linewidth=2)
                ax1.set_title('Equity Curve Over Time')
                ax1.set_xlabel('Trade Number')
                ax1.set_ylabel('Account Balance ($)')
                ax1.grid(True)
                ax1.legend()
            
            # 2. Daily Returns Distribution
            ax2 = fig.add_subplot(gs[1, 0])
            if len(trades_df) > 0:
                daily_returns = trades_df.groupby(
                    trades_df['entry_time'].dt.date)['pnl'].sum()
                
                if len(daily_returns) > 0:
                    ax2.hist(daily_returns, bins=30, color='blue', alpha=0.7)
                    ax2.set_title('Daily Returns Distribution')
                    ax2.set_xlabel('Daily Return ($)')
                    ax2.set_ylabel('Frequency')
                    ax2.grid(True)
            
            # 3. Win/Loss Pie Chart
            ax3 = fig.add_subplot(gs[1, 1])
            wins = len(trades_df[trades_df['pnl'] > 0])
            losses = len(trades_df[trades_df['pnl'] <= 0])
            if wins + losses > 0:
                ax3.pie([wins, losses], labels=['Wins', 'Losses'], autopct='%1.1f%%',
                        colors=['green', 'red'])
                ax3.set_title('Win/Loss Distribution')
            
            # 4. Hourly Performance
            ax4 = fig.add_subplot(gs[2, 0])
            if len(trades_df) > 0:
                hourly_returns = trades_df.groupby(
                    trades_df['entry_time'].dt.hour)['pnl'].mean()
                
                hours = range(24)
                ax4.bar(hours, [hourly_returns.get(hour, 0) for hour in hours],
                        color='blue', alpha=0.7)
                ax4.set_title('Average Returns by Hour')
                ax4.set_xlabel('Hour of Day')
                ax4.set_ylabel('Average PnL ($)')
                ax4.grid(True)
            
            # 5. Trade Duration vs PnL
            ax5 = fig.add_subplot(gs[2, 1])
            if len(trades_df) > 0:
                duration = (trades_df['exit_time'] - 
                        trades_df['entry_time']).dt.total_seconds() / 3600
                pnl = trades_df['pnl']
                
                # Remove any remaining NaN values
                mask = ~(duration.isna() | pnl.isna())
                duration = duration[mask]
                pnl = pnl[mask]
                
                if len(duration) > 0 and len(pnl) > 0:
                    ax5.scatter(duration, pnl, alpha=0.5, color='blue')
                    ax5.set_title('Trade Duration vs PnL')
                    ax5.set_xlabel('Duration (hours)')
                    ax5.set_ylabel('PnL ($)')
                    ax5.grid(True)
            
            plt.tight_layout()
            timestamp = pd.Timestamp.now().strftime('%Y%m%d_%H%M%S')
            plt.savefig(f'trading_metrics_{timestamp}.png', dpi=300, bbox_inches='tight')
            plt.close()
            
        except Exception as e:
            print(f"Error generating plots: {str(e)}")
            plt.close('all')

    def generate_complete_report(self) -> Dict[str, Dict[str, Any]]:
        """Generate a comprehensive report combining all metrics."""
        # Get risk adjusted metrics
        risk_adjusted = self.trading.calculate_risk_adjusted_metrics()
        
        complete_report = {
            'trading_metrics': {
                'total_trades': len(self.trading.trades_df),
                'win_rate': self.trading._calculate_win_loss_ratio(),
                'profit_factor': self.trading._calculate_profit_factor(),
                'sharpe_ratio': self.trading._calculate_sharpe_ratio(),
                'max_drawdown': self.trading._calculate_max_drawdown(),
                'total_pnl': self.trading.trades_df['pnl'].sum(),
                'average_trade_pnl': self.trading.trades_df['pnl'].mean(),
                'risk_adjusted_return': risk_adjusted['risk_adjusted_return'],
                'avg_risk_per_trade': risk_adjusted['avg_risk_per_trade']
            },
            'risk_metrics': {
                'value_at_risk': self.risk.calculate_value_at_risk(),
                'volatility': self.risk.calculate_volatility(),
                'risk_reward_ratio': self.risk.calculate_risk_reward_ratio(),
                'max_consecutive_losses': self.risk.calculate_max_consecutive_losses(),
                'profit_per_unit_risk': risk_adjusted['profit_per_unit_risk']
            },
            'timing_metrics': {
                'average_hold_time': self.timing.calculate_average_hold_time(),
                'best_hour': self.timing.calculate_best_trading_hour(),
                'worst_hour': self.timing.calculate_worst_trading_hour(),
                'market_participation': self.timing.calculate_market_participation_rate(),
            }
        }
        
        return complete_report

    def save_report(self, report_path: str = None) -> None:
        """Save performance metrics report and generate visualizations.
        
        Args:
            report_path: Optional path to save the report. If None, uses timestamp.
        """
        # Generate timestamp for filenames if no path provided
        timestamp = pd.Timestamp.now().strftime('%Y%m%d_%H%M%S')
        if report_path is None:
            report_path = f'backtest_report_{timestamp}'
        
        # Get metrics report
        metrics = self.generate_complete_report()
        
        # Convert numpy types to native Python types for JSON serialization
        def convert_to_native_types(obj):
            if isinstance(obj, dict):
                return {key: convert_to_native_types(value) for key, value in obj.items()}
            elif isinstance(obj, (list, tuple)):
                return [convert_to_native_types(item) for item in obj]
            elif isinstance(obj, (np.integer, np.int32, np.int64)):
                return int(obj)
            elif isinstance(obj, (np.floating, np.float32, np.float64)):
                return float(obj)
            elif isinstance(obj, np.ndarray):
                return obj.tolist()
            else:
                return obj
                
        # Convert metrics to native Python types
        metrics = convert_to_native_types(metrics)
        
        # Create text report
        report_text = [
            "Backtest Performance Report",
            "=========================\n",
            "Trading Metrics:",
            f"Total Trades: {metrics['trading_metrics']['total_trades']}",
            f"Win Rate: {metrics['trading_metrics']['win_rate']:.4f}",
            f"Profit Factor: {metrics['trading_metrics']['profit_factor']:.4f}",
            f"Sharpe Ratio: {metrics['trading_metrics']['sharpe_ratio']:.4f}",
            f"Max Drawdown: {metrics['trading_metrics']['max_drawdown']:.4f}",
            f"Total PnL: ${metrics['trading_metrics']['total_pnl']:.2f}",
            f"Average Trade PnL: ${metrics['trading_metrics']['average_trade_pnl']:.2f}\n",
            "Risk Metrics:",
            f"Value at Risk: {metrics['risk_metrics']['value_at_risk']:.4f}",
            f"Volatility: {metrics['risk_metrics']['volatility']:.4f}",
            f"Risk/Reward Ratio: {metrics['risk_metrics']['risk_reward_ratio']:.4f}",
            f"Max Consecutive Losses: {metrics['risk_metrics']['max_consecutive_losses']}\n",
            "Timing Metrics:",
            f"Average Hold Time (hours): {metrics['timing_metrics']['average_hold_time']:.4f}",
            f"Best Trading Hour: {metrics['timing_metrics']['best_hour']}:00",
            f"Worst Trading Hour: {metrics['timing_metrics']['worst_hour']}:00",
            f"Market Participation Rate: {metrics['timing_metrics']['market_participation']:.4f}"
        ]
        
        try:
            # Save text report
            with open(f"{report_path}.txt", 'w') as f:
                f.write('\n'.join(report_text))
            
            # Save metrics as JSON
            with open(f"{report_path}.json", 'w') as f:
                json.dump(metrics, f, indent=4)
            
            # Generate and save visualizations
            self.plot_metrics()
            
            print(f"\nReport saved to {report_path}.txt")
            print(f"Metrics saved to {report_path}.json")
            print(f"Visualizations saved as trading_metrics_{timestamp}.png")
            
        except Exception as e:
            print(f"Error saving report: {str(e)}")
            raise