import pandas as pd
import numpy as np
from sklearn.ensemble import RandomForestClassifier
from sklearn.preprocessing import LabelEncoder
import joblib
from SmokehouseModules import CandlestickData
from SmokehouseModules.TradingMetrics import MetricsManager
from tabulate import tabulate
import os
import pickle
from tradingStrategy import FedRateStrategy, BaseStrategy, TradeSignal

# Keep all feature calculation functions as they're needed for ML
def calculate_momentum(history, period=10):
    prices = [c.yes.mid_price for c in history]
    if len(prices) < 2:
        return 0
    return (prices[-1] - prices[0]) / prices[0]

def identify_volatility_regime(history):
    prices = [c.yes.mid_price for c in history]
    if len(prices) < 2:
        return 0
    return np.std(prices)

def calculate_trend_strength(history):
    prices = [c.yes.mid_price for c in history]
    if len(prices) < 2:
        return 0
        
    if len(set(prices)) == 1:
        return 0
        
    try:
        corr = np.corrcoef(range(len(prices)), prices)[0, 1]
        return 0 if np.isnan(corr) else corr
    except:
        return 0

def calculate_spread_momentum(history):
    spreads = [c.yes.spread for c in history]
    if len(spreads) < 2:
        return 0
    return (spreads[-1] - spreads[0]) / spreads[0] if spreads[0] != 0 else 0

def analyze_volume_profile(history):
    volumes = [c.yes.volume for c in history]
    if not volumes:
        return 0
    return np.mean(volumes)

def calculate_rsi(history, periods=14):
    prices = [c.yes.mid_price for c in history]
    if len(prices) < periods + 1:
        return 50
    
    deltas = np.diff(prices)
    gain = np.sum([d if d > 0 else 0 for d in deltas])
    loss = np.sum([-d if d < 0 else 0 for d in deltas])
    
    avg_gain = gain / periods
    avg_loss = loss / periods
    
    if avg_loss == 0:
        return 100
    
    rs = avg_gain / avg_loss
    return 100 - (100 / (1 + rs))

def calculate_macd(history):
    prices = [c.yes.mid_price for c in history]
    if len(prices) < 26:
        return 0
    
    # Proper EMA calculation
    def ema(data, span):
        alpha = 2 / (span + 1)
        result = [data[0]]  # First value is simple average
        for n in range(1, len(data)):
            result.append(alpha * data[n] + (1 - alpha) * result[n-1])
        return result[-1]  # Return last value
    
    ema12 = ema(prices[-12:], 12)
    ema26 = ema(prices[-26:], 26)
    return ema12 - ema26

# Load data and model
try:
    with open('candlestick_objects.pkl', 'rb') as f:
        candlestick_objects = pickle.load(f)
    print("Candlestick objects loaded successfully.")
except Exception as e:
    print(f"Error loading candlestick objects: {e}")

try:
    model = joblib.load('random_forest_model.pkl')
    print("Model loaded successfully.")
except Exception as e:
    print(f"Error loading model: {e}")

# After loading model
def validate_model(model):
    """Validate model predictions and feature importance"""
    print("\nModel Validation:")
    
    # Check feature importances
    importances = dict(zip(model.feature_names_in_, model.feature_importances_))
    problematic_features = [f for f, imp in importances.items() if imp == 0]
    if problematic_features:
        print(f"Warning: Features with zero importance: {problematic_features}")
    
    # Verify prediction range
    test_data = pd.DataFrame([[0]*len(model.feature_names_in_)], 
                            columns=model.feature_names_in_)
    pred = model.predict(test_data)[0]
    print(f"Test prediction range: {pred}")
    
    return len(problematic_features) == 0

# Use in main code
if not validate_model(model):
    print("Warning: Model validation failed - consider retraining")

def calculate_max_hold_time(market_type):
    max_hold_times = {
        'DAILY': 24,
        'WEEKLY': 72,
        'MONTHLY': 168
    }
    return max_hold_times.get(market_type, 48)

def process_by_market(candlesticks):
    markets = {}
    for cs in candlesticks:
        if cs.yes.market_ticker not in markets:
            markets[cs.yes.market_ticker] = []
        markets[cs.yes.market_ticker].append(cs)
    
    for ticker in markets:
        markets[ticker].sort(key=lambda x: x.yes.end_period_ts)
        
    print(f"Processed {len(markets)} markets.")
    return markets

def backtest(train_size=0.7, future_steps=5, initial_balance=1000):
    results = []
    trades_list = []
    markets = process_by_market(candlestick_objects)
    total_markets = len(markets)
    
    le = LabelEncoder()
    
    strategy = FedRateStrategy(initial_balance=initial_balance)
    print("Strategy initialized")
    
    try:
        for idx, (ticker, market_data) in enumerate(markets.items(), 1):
            if idx > 100:  # Limit to first 100 markets
                break
            print(f"\nProcessing market {idx}/{total_markets}: {ticker}")
            
            balance = initial_balance
            wins = losses = 0
            
            market_features = []
            lookback_period = 14
            
            for i in range(lookback_period, len(market_data) - future_steps):
                history = market_data[i-lookback_period:i+1]
                cs = market_data[i]
                
                feature_dict = {
                    "market_ticker": cs.yes.market_ticker,
                    "timestamp": cs.yes.end_period_ts,
                    "mid_price": cs.yes.mid_price,
                    "spread": cs.yes.spread,
                    "open_interest": cs.yes.open_interest,
                    "volume": cs.yes.volume,
                    "hour": cs.yes.hour,
                    "minute": cs.yes.minute,
                    
                    "price_momentum": calculate_momentum(history),
                    "volatility_regime": identify_volatility_regime(history),
                    "trend_strength": calculate_trend_strength(history),
                    "spread_momentum": calculate_spread_momentum(history),
                    "volume_profile": analyze_volume_profile(history),
                    "rsi": calculate_rsi(history),
                    "macd": calculate_macd(history)
                }
                market_features.append(feature_dict)
            
            df = pd.DataFrame(market_features)
            if len(df) < future_steps + 14:
                print(f"Not enough data for market {ticker}. Skipping.")
                continue
            
            split_idx = int(len(df) * train_size)
            if split_idx < 14:
                print(f"Not enough training data for market {ticker}. Skipping.")
                continue
            
            df['market_ticker'] = le.fit_transform(df['market_ticker'])
            current_position = None
            last_trade_time = -future_steps
            
            market_type = getattr(market_data[0], 'market_type', 'DAILY')
            max_hold_time = calculate_max_hold_time(market_type)
            
            for i in range(split_idx, len(df) - future_steps):
                try:
                    pred = model.predict(df.iloc[[i]])[0]
                    print(f"Features for prediction: {df.iloc[i].to_dict()}")  # Debug features
                    print(f"Model prediction: {pred}")  # Debug prediction

                    if i - last_trade_time < future_steps:
                        continue
                        
                    pred = model.predict(df.iloc[[i]])[0]
                    
                    if current_position is None:
                        signal = strategy.should_enter(
                            candlestick=market_data[i],
                            ml_prediction=pred,
                            market_data={'history': market_data[i-lookback_period:i+1]}
                        )
                        
                        if signal:
                            current_price = market_data[i].yes.mid_price
                            position_size = signal.suggested_size
                            
                            current_position = {
                                'entry_price': current_price,
                                'position_size': position_size,
                                'entry_time': signal.timestamp,
                                'direction': signal.direction,
                                'shares': position_size / current_price
                            }
                            last_trade_time = i
                            
                    elif current_position is not None:
                        should_exit = strategy.should_exit(
                            position=current_position,
                            candlestick=market_data[i],
                            market_data={'history': market_data[i-lookback_period:i+1]}
                        )
                        
                        if should_exit:
                            exit_price = market_data[i].yes.mid_price
                            shares = current_position['shares']
                            spread_cost = market_data[i].yes.spread
                            
                            dollar_pl = shares * (exit_price - current_position['entry_price'])
                            dollar_pl -= (shares * spread_cost)
                            
                            balance += dollar_pl
                            
                            trade = {
                                'market': ticker,
                                'direction': current_position['direction'],
                                'entry_time': pd.to_datetime(current_position['entry_time'], unit='s'),
                                'exit_time': pd.to_datetime(market_data[i].yes.end_period_ts, unit='s'),
                                'entry_price': current_position['entry_price'],
                                'exit_price': exit_price,
                                'position_size': current_position['position_size'],
                                'shares': shares,
                                'pnl': dollar_pl,
                                'balance_after': balance,
                                'hold_periods': i - last_trade_time,
                                'spread_cost': spread_cost
                            }
                            trades_list.append(trade)
                            
                            if dollar_pl > 0:
                                wins += 1
                            else:
                                losses += 1
                                
                            current_position = None
                            
                except Exception as e:
                    print(f"Error in prediction at {i}: {e}")
                    continue
            
            results.append({
                'market': ticker,
                'initial_balance': initial_balance,
                'final_balance': balance,
                'total_return': (balance - initial_balance) / initial_balance * 100,
                'wins': wins,
                'losses': losses,
                'win_rate': wins/(wins + losses) if (wins + losses) > 0 else 0
            })
            
    except KeyboardInterrupt:
        print("\nBacktest interrupted by user")
    
    timestamp = pd.Timestamp.now().strftime('%Y%m%d_%H%M%S')
    trades_file = f'trades_{timestamp}.csv'
    market_summary_file = f'market_summary_{timestamp}.csv'
    
    trades_df = pd.DataFrame(trades_list)
    results_df = pd.DataFrame(results)
    
    trades_df.to_csv(trades_file, index=False)
    results_df.to_csv(market_summary_file, index=False)
    
    print(f"\nResults saved to {market_summary_file}")
    print(f"Trades saved to {trades_file}")
    print("\nModel Information:")
    print(f"Feature names: {model.feature_names_in_}")
    print(f"Number of estimators: {model.n_estimators}")
    print(f"Feature importances: {list(zip(model.feature_names_in_, model.feature_importances_))}")
    
    return results_df, trades_df, trades_file, market_summary_file

if __name__ == "__main__":
    results_df, trades_df, trades_file, market_summary_file = backtest()
    metrics_manager = MetricsManager(trades_file, market_summary_file)
    metrics_manager.save_report()