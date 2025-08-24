from datetime import datetime
import pandas as pd
import plotly.graph_objects as go
from SmokehouseModules import CandlestickData
import pytz  # For timezone conversion
import argparse  # For handling command-line arguments

# Parse command-line arguments
parser = argparse.ArgumentParser(description="Generate an interactive candlestick chart for a given market ticker.")
parser.add_argument("--market-ticker", type=str, required=True, help="The market ticker to query (e.g., 'KXACAREPEAL-26-26JAN01').")
parser.add_argument("--start-date", type=str, required=True, help="Start date in YYYY-MM-DD format.")
parser.add_argument("--end-date", type=str, required=True, help="End date in YYYY-MM-DD format.")
parser.add_argument("--interval-type", type=int, required=True, choices=[1, 2, 3], help="Interval type: 1=minute, 2=hour, 3=day.")
args = parser.parse_args()

# Configurable parameters
ticker = args.market_ticker
start_date = datetime.strptime(args.start_date, "%Y-%m-%d")
end_date = datetime.strptime(args.end_date, "%Y-%m-%d")
interval_type = args.interval_type
local_timezone = 'America/New_York'

# Fetch data using the CandlestickData module
candlesticks = CandlestickData.get_candlestick_data(interval_type, start_date, end_date, ticker)

if not candlesticks:
    print("No data returned for the given parameters.")
    exit()

# Convert CandlestickData objects to DataFrame
data = [
    {
        'Date': datetime(cs.yes.year, cs.yes.month, cs.yes.day, cs.yes.hour, cs.yes.minute, tzinfo=pytz.utc),
        'Open': cs.yes.ask_open,
        'High': cs.yes.ask_high,
        'Low': cs.yes.ask_low,
        'Close': cs.yes.ask_close,
    }
    for cs in candlesticks
]

df = pd.DataFrame(data).sort_values(by="Date")

# **Convert UTC to Local Timezone**
df['Date'] = df['Date'].dt.tz_convert(local_timezone)

# Define interval frequency
df_dict = {1: "T", 2: "H", 3: "D"}  # T = minute, H = hour, D = day
resample_frequency = df_dict[interval_type]

# Create full time index for accurate forward-filling
all_timestamps = pd.date_range(df["Date"].min(), df["Date"].max(), freq=resample_frequency, tz=local_timezone)
filled_df = df.set_index("Date").reindex(all_timestamps)

# Forward-fill only the Close price
filled_df["Close"] = filled_df["Close"].ffill()

# Ensure Open, High, and Low are correctly set
filled_df["Open"] = filled_df["Close"].shift(1).fillna(filled_df["Close"])  # Open = Previous Close
filled_df["High"] = filled_df["Close"]
filled_df["Low"] = filled_df["Close"]

# Drop NaN rows where no previous Close existed
filled_df = filled_df.dropna()

# **Detect Candlestick Patterns**
patterns = CandlestickData.detect_patterns(candlesticks)

# Prepare pattern annotations
pattern_dates = []
pattern_labels = []

for index, pattern_list in patterns.items():
    if index < len(candlesticks):  # Ensure index is within range
        pattern_time = datetime(
            candlesticks[index].yes.year,
            candlesticks[index].yes.month,
            candlesticks[index].yes.day,
            candlesticks[index].yes.hour,
            candlesticks[index].yes.minute,
            tzinfo=pytz.utc
        ).astimezone(pytz.timezone(local_timezone))

        pattern_dates.append(pattern_time)
        pattern_labels.append(", ".join(pattern_list))  # Join multiple patterns

# **Create Interactive Plotly Chart**
fig = go.Figure()

# Add candlestick trace
fig.add_trace(go.Candlestick(
    x=filled_df.index,
    open=filled_df["Open"],
    high=filled_df["High"],
    low=filled_df["Low"],
    close=filled_df["Close"],
    name="Candlesticks"
))

# Add pattern annotations
fig.add_trace(go.Scatter(
    x=pattern_dates,
    y=[filled_df.loc[date, "High"] * 1.02 for date in pattern_dates],  # Position slightly above high price
    text=pattern_labels,
    mode="text",
    textposition="top center",
    marker=dict(color="red"),
    name="Pattern Detections"
))

# Update layout
fig.update_layout(
    title=f"{ticker} Interactive Candlestick Chart with Pattern Detection",
    xaxis_title="Time",
    yaxis_title="Price (cents)",
    xaxis_rangeslider=dict(visible=True, thickness=0.1),
    showlegend=False
)

# Show the chart
fig.show()
