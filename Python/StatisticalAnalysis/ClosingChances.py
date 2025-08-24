import pandas as pd
import glob
import numpy as np
from datetime import timedelta
from scipy.stats import linregress
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import classification_report, roc_auc_score

# Load data
files = glob.glob('path/to/parquet/files/*.parquet')
df = pd.concat([pd.read_parquet(f) for f in files], ignore_index=True)
df = df.sort_values(['market_ticker', 'timestamp'])

# Define resolution and labels
M = 30
M_delta = timedelta(minutes=M)
grouped = df.groupby('market_ticker')
t_resolves = {ticker: group.loc[(group['ask_close'] == 100) | (group['bid_close'] == 0), 'timestamp'].min()
              for ticker, group in grouped if ((group['ask_close'] == 100) | (group['bid_close'] == 0)).any()}
df['y'] = 0
for ticker, t_resolve in t_resolves.items():
    mask = (df['market_ticker'] == ticker) & (df['timestamp'] >= t_resolve - M_delta) & (df['timestamp'] < t_resolve)
    df.loc[mask, 'y'] = 1
df = df[df.apply(lambda row: row['timestamp'] < t_resolves.get(row['market_ticker'], row['timestamp'] + timedelta(minutes=1)), axis=1)]

# Feature engineering
def calculate_slope(series):
    if len(series) < 2:
        return np.nan
    x = np.arange(len(series))
    slope, _, _, _, _ = linregress(x, series)
    return slope

for name, group in grouped:
    idx = group.index
    df.loc[idx, 'mid_price_change_5'] = group['mid_price'].diff(5)
    df.loc[idx, 'mid_price_change_10'] = group['mid_price'].diff(10)
    df.loc[idx, 'mid_price_slope_5'] = group['mid_price'].rolling(window=5).apply(calculate_slope, raw=True)
    df.loc[idx, 'mid_price_slope_10'] = group['mid_price'].rolling(window=10).apply(calculate_slope, raw=True)
df['distance_to_100'] = 100 - df['ask_close']
df['distance_to_0'] = df['bid_close']
df = df.dropna()

# Features
features = [
    'mid_price', 'spread', 'volume', 'open_interest',
    'mid_price_slope_5', 'mid_price_slope_10',
    'mid_price_change_5', 'mid_price_change_10',
    'SMA_5', 'SMA_10', 'MACD', 'MACD_signal', 'RSI', 'ATR',
    'distance_to_100', 'distance_to_0',
    'hour', 'minute', 'day',
    'pivot_point', 'support1', 'support2', 'support3',
    'resistance1', 'resistance2', 'resistance3'
]

# Split data
tickers = df['market_ticker'].unique()
train_tickers, test_tickers = train_test_split(tickers, test_size=0.2, random_state=42)
train_df = df[df['market_ticker'].isin(train_tickers)]
test_df = df[df['market_ticker'].isin(test_tickers)]
X_train = train_df[features]
y_train = train_df['y']
X_test = test_df[features]
y_test = test_df['y']

# Train model
model = RandomForestClassifier(n_estimators=100, random_state=42)
model.fit(X_train, y_train)

# Evaluate
y_pred = model.predict(X_test)
y_pred_proba = model.predict_proba(X_test)[:, 1]
print("Classification Report:")
print(classification_report(y_test, y_pred))
print("AUC-ROC:", roc_auc_score(y_test, y_pred_proba))

# Feature importances
importances = pd.Series(model.feature_importances_, index=features).sort_values(ascending=False)
print("Feature Importances:")
print(importances)