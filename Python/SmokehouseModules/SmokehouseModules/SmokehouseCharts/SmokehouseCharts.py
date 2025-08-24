import os
import plotly.graph_objects as go
from datetime import timedelta
import pandas as pd
import random
import logging

logger = logging.getLogger(__name__)

# Global counter for market resolution scenarios by timeframe
resolution_stats = {
    "Minutely": {"resolved_at_0": 0, "resolved_at_100": 0, "never_resolved": 0, "unresolved": 0},
    "Hourly": {"resolved_at_0": 0, "resolved_at_100": 0, "never_resolved": 0, "unresolved": 0},
    "Daily": {"resolved_at_0": 0, "resolved_at_100": 0, "never_resolved": 0, "unresolved": 0}
}

# Track resolved markets for folder categorization
resolved_markets = set()

def get_unique_filename(directory, base_filename, extension=".png"):
    if not os.path.exists(directory):
        os.makedirs(directory)
    file_path = os.path.join(directory, f"{base_filename}{extension}")
    counter = 1
    while os.path.exists(file_path):
        file_path = os.path.join(directory, f"{base_filename}_{counter}{extension}")
        counter += 1
    return file_path

def generate_chart(candles, lookback_candles, lookforward_candles, pattern_name, market_name, pattern_indices=None,
                   with_volume=True, with_legend=True, highlight_pattern=True, annotate_patterns=True):
    all_candles = lookback_candles + candles + lookforward_candles
    if not all_candles:
        logger.warning("No candle data provided. Returning empty figure.")
        return go.Figure()

    if not candles:
        raise ValueError("No pattern candles provided. Cannot generate chart without pattern data.")

    df = pd.DataFrame(all_candles)
    df['Timestamp'] = pd.to_datetime(df['Timestamp'], format='%Y-%m-%dT%H:%M:%SZ', utc=True)
    df.set_index('Timestamp', inplace=True)

    if df.empty or len(df) < 2:
        logger.warning(f"Insufficient data ({len(df)} candles). Charting may be limited.")
        return go.Figure()

    fig = go.Figure()
    fig.add_trace(go.Candlestick(
        x=df.index,
        open=df["Open"],
        high=df["High"],
        low=df["Low"],
        close=df["Close"],
        name="Candlesticks"
    ))

    if with_volume and "Volume" in df.columns:
        max_volume = max(df["Volume"].max(), 1)
        fig.add_trace(go.Bar(
            x=df.index,
            y=df["Volume"],
            name="Volume",
            marker_color="blue",
            yaxis="y2",
        ))
        fig.update_layout(
            yaxis2=dict(domain=[0, 0.20], title="Volume", range=[0, max_volume], showgrid=False, tickformat=".0f"),
            yaxis=dict(domain=[0.30, 1])
        )
    else:
        fig.update_layout(yaxis=dict(domain=[0, 1]))

    fig.update_layout(
        yaxis_title="Price (cents)",
        xaxis_title="Time",
        showlegend=with_legend,
        yaxis=dict(tickformat="d", range=[0, 100])
    )

    min_time = df.index.min() - timedelta(minutes=2)
    max_time = df.index.max() + timedelta(minutes=2)
    fig.update_xaxes(range=[min_time, max_time])

    if highlight_pattern and candles:
        pattern_start_time = pd.to_datetime(candles[0]['Timestamp'], format='%Y-%m-%dT%H:%M:%SZ', utc=True)
        pattern_end_time = pd.to_datetime(candles[-1]['Timestamp'], format='%Y-%m-%dT%H:%M:%SZ', utc=True)
        candlestick_interval = df.index.to_series().diff().median()
        x_start = pattern_start_time - (candlestick_interval * 0.6)
        x_end = pattern_end_time + (candlestick_interval * 0.6)
        fig.add_shape(
            type="rect",
            x0=x_start, x1=x_end,
            y0=0, y1=100,
            fillcolor=generate_random_color(0.3),
            line=dict(width=0),
            layer="below"
        )

    if annotate_patterns and pattern_name and candles:
        pattern_start_time = pd.to_datetime(candles[0]['Timestamp'], format='%Y-%m-%dT%H:%M:%SZ', utc=True)
        pattern_end_time = pd.to_datetime(candles[-1]['Timestamp'], format='%Y-%m-%dT%H:%M:%SZ', utc=True)
        mid_pattern_time = pattern_start_time + (pattern_end_time - pattern_start_time) / 2
        fig.add_annotation(
            x=mid_pattern_time,
            y=1.15,
            text=pattern_name.replace("_", " "),
            showarrow=False,
            xref="x",
            yref="paper",
            align="center",
            font=dict(size=10, color="black"),
            bgcolor="rgba(255,255,255,0.8)"
        )
        fig.add_annotation(
            x=mid_pattern_time,
            y=1.25,
            text=f"Market: {market_name}",
            showarrow=False,
            xref="x",
            yref="paper",
            align="center",
            font=dict(size=10, color="black"),
            bgcolor="rgba(255,255,255,0.8)"
        )

    return fig

def generate_full_market_chart(parquet_file, patterns_data, market_name, output_dir=None):
    global resolution_stats, resolved_markets

    df = pd.read_parquet(parquet_file)
    df['Timestamp'] = pd.to_datetime(df['timestamp'], utc=True)
    df.set_index('Timestamp', inplace=True)

    # Determine timeframe and base market name
    if market_name.endswith("_Hourly"):
        timeframe = "Hourly"
        candle_total = 24
        candle_duration = timedelta(hours=1)
        total_duration = timedelta(hours=candle_total)
        base_market_name = market_name.replace("_Hourly", "")
    elif market_name.endswith("_Daily"):
        timeframe = "Daily"
        candle_total = 7
        candle_duration = timedelta(days=1)
        total_duration = timedelta(days=candle_total)
        base_market_name = market_name.replace("_Daily", "")
    else:
        timeframe = "Minutely"
        candle_total = 60 * 8
        candle_duration = timedelta(minutes=1)
        total_duration = timedelta(minutes=candle_total)
        base_market_name = market_name

    if df.empty or len(df) < 2:
        logger.warning(f"Insufficient data in {parquet_file} ({len(df)} rows). Skipping.")
        resolution_stats[timeframe]["unresolved"] += 1
        return

    resolved_idx_0 = df.index[df['ask_close'] == 0].min()
    resolved_idx_100 = df.index[df['ask_close'] == 100].min()
    
    if pd.isna(resolved_idx_0) and pd.isna(resolved_idx_100):
        resolved_idx = df.index[-1]
        resolution_stats[timeframe]["never_resolved"] += 1
    elif pd.isna(resolved_idx_0) or (not pd.isna(resolved_idx_100) and resolved_idx_100 < resolved_idx_0):
        resolved_idx = resolved_idx_100
        resolution_stats[timeframe]["resolved_at_100"] += 1
        resolved_markets.add(base_market_name)
    else:
        resolved_idx = resolved_idx_0
        resolution_stats[timeframe]["resolved_at_0"] += 1
        resolved_markets.add(base_market_name)

    max_post_resolution = 5
    resolved_end_idx = min(df.index.get_loc(resolved_idx) + max_post_resolution, len(df) - 1)
    end_time = df.index[resolved_end_idx]

    start_time = resolved_idx - (candle_duration * (candle_total - 1))
    df = df.loc[start_time:end_time]

    if df.empty or len(df) < 2:
        logger.warning(f"Insufficient data in {parquet_file} ({len(df)} rows after filtering). Skipping.")
        resolution_stats[timeframe]["unresolved"] += 1
        return

    fig = go.Figure()
    # Add single candlestick trace
    fig.add_trace(go.Candlestick(
        x=df.index,
        open=df["ask_open"],
        high=df["ask_high"],
        low=df["ask_low"],
        close=df["ask_close"],
        name="Candlesticks"
    ))

    # Add volume trace only if explicitly enabled (default off for full charts)
    if "volume" in df.columns and False:  # Set to False to disable volume for full charts
        max_volume = max(df["volume"].max(), 1)
        fig.add_trace(go.Bar(
            x=df.index,
            y=df["volume"],
            name="Volume",
            marker_color="blue",
            yaxis="y2",
        ))
        fig.update_layout(
            yaxis2=dict(domain=[0, 0.20], title="Volume", range=[0, max_volume], showgrid=False, tickformat=".0f"),
            yaxis=dict(domain=[0.30, 1])
        )
    else:
        fig.update_layout(yaxis=dict(domain=[0, 1]))

    actual_candle_count = len(df)
    fig.update_layout(
        title=f"{market_name} - Last {actual_candle_count} {timeframe} Candlesticks with Patterns (Resolved at {resolved_idx})",
        yaxis_title="Price (cents)",
        xaxis_title="Time",
        showlegend=True,
        yaxis=dict(tickformat="d", range=[0, 100])
    )

    fig.add_annotation(
        x=df.index.min(),
        y=1.25,
        text=f"Market: {market_name}",
        showarrow=False,
        xref="x",
        yref="paper",
        align="left",
        font=dict(size=12, color="black"),
        bgcolor="rgba(255,255,255,0.8)"
    )
    fig.add_annotation(
        x=df.index.max(),
        y=1.25,
        text=f"Timeframe: {timeframe} (Total: {total_duration})",
        showarrow=False,
        xref="x",
        yref="paper",
        align="right",
        font=dict(size=12, color="black"),
        bgcolor="rgba(255,255,255,0.8)"
    )

    # Track pattern positions and offsets
    pattern_offsets = {}
    last_pattern_end = None

    if patterns_data:  # Use patterns_data to overlay all patterns
        candlestick_interval = candle_duration
        for occurrence in patterns_data:
            pattern_name = occurrence['PatternName']
            candles = occurrence['Candles']
            pattern_start_time = pd.to_datetime(candles[0]['Timestamp'], format='%Y-%m-%dT%H:%M:%SZ', utc=True)
            pattern_end_time = pd.to_datetime(candles[-1]['Timestamp'], format='%Y-%m-%dT%H:%M:%SZ', utc=True)

            if pattern_end_time >= start_time and pattern_start_time <= end_time:
                # Highlight pattern
                x_start = pattern_start_time - (candlestick_interval * 0.6)
                x_end = pattern_end_time + (candlestick_interval * 0.6)
                fig.add_shape(
                    type="rect",
                    x0=x_start, x1=x_end,
                    y0=0, y1=100,
                    fillcolor=generate_random_color(0.3),
                    line=dict(width=0),
                    layer="below"
                )

                mid_pattern_time = pattern_start_time + (pattern_end_time - pattern_start_time) / 2
                # Calculate offset based on gaps
                if last_pattern_end is not None:
                    time_diff = (pattern_start_time - last_pattern_end).total_seconds()
                    candlestick_seconds = candlestick_interval.total_seconds()
                    gap_candles = time_diff // candlestick_seconds if candlestick_seconds > 0 else 0
                    if gap_candles >= 2:
                        pattern_offsets.clear()  # Reset offset for gaps of 2 or more candlesticks
                last_pattern_end = pattern_end_time

                # Apply offset (increment y position downward)
                current_offset = len(pattern_offsets) * 0.05  # Each pattern moves down by 0.05 paper units
                pattern_offsets[pattern_name] = current_offset
                y_position = 1.05 - current_offset  # Start at 1.05 and move downward

                fig.add_annotation(
                    x=mid_pattern_time,
                    y=y_position,
                    text=pattern_name.replace("_", " "),
                    showarrow=False,
                    xref="x",
                    yref="paper",
                    align="center",
                    font=dict(size=10, color="black"),
                    bgcolor="rgba(255,255,255,0.8)"
                )

    if output_dir:
        resolution_folder = "Resolved" if base_market_name in resolved_markets else "Unresolved"
        output_subdir = os.path.join(output_dir, resolution_folder, base_market_name)
        timeframe_map = {
            "Minutely": "1-Minutes",
            "Hourly": "2-Hours",
            "Daily": "3-Days"
        }
        new_timeframe_name = timeframe_map[timeframe]
        snapshot_path = get_unique_filename(output_subdir, f"{base_market_name}_{new_timeframe_name}", ".png")
        fig.write_image(snapshot_path, width=2560, height=1440)  # Increased resolution
        logger.info(f"✅ Saved high-res chart: {snapshot_path}")

def print_resolution_summary():
    logger.info("📊 Market Resolution Summary by Timeframe:")
    for timeframe in ["Minutely", "Hourly", "Daily"]:
        stats = resolution_stats[timeframe]
        logger.info(f"  {timeframe}:")
        logger.info(f"    Resolved at 0: {stats['resolved_at_0']}")
        logger.info(f"    Resolved at 100: {stats['resolved_at_100']}")
        logger.info(f"    Never resolved: {stats['never_resolved']}")
        logger.info(f"    Unresolved (insufficient data): {stats['unresolved']}")

def generate_random_color(alpha=0.3):
    r = random.randint(0, 255)
    g = random.randint(0, 255)
    b = random.randint(0, 255)
    return f"rgba({r}, {g}, {b}, {alpha})"
