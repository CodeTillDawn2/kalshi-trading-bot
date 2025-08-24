import os
from datetime import datetime
import pandas as pd
import pytz
import argparse
from collections import defaultdict, Counter
from SmokehouseModules import CandlestickData, SmokehouseCharts, PatternDetector

# Ensure Kaleido is installed for exporting images
try:
    import kaleido
except ImportError:
    print("Warning: Kaleido not found. Run 'pip install kaleido' to enable image export.")

# Define available interval types
INTERVAL_TYPES = {1: "Minute", 2: "Hour", 3: "Day"}
BASE_OUTPUT_DIR = "..\\..\\TestingOutput\\FullPatternAnalysis"

# Store pattern counts per interval
pattern_counts_per_interval = defaultdict(Counter)

# Iterate over all interval types
for interval_type, interval_name in INTERVAL_TYPES.items():
    
    # Fetch and process candlestick data
    candlesticks = CandlestickData.get_candlestick_data(interval_type=interval_type, market_tickers="KXEPSTEIN-25")
    if not candlesticks:
        print(f"No data returned for interval type {interval_name}.")
        continue

    # Group by market ticker
    market_tickers = set(candle.market_ticker for candle in candlesticks)
    
    for market_ticker in market_tickers:
        # Create output directory for this market ticker and interval type
        output_dir = os.path.join(BASE_OUTPUT_DIR, interval_name, market_ticker)
        os.makedirs(output_dir, exist_ok=True)
        
        # Filter data for this specific market ticker
        ticker_candlesticks = [c for c in candlesticks if c.market_ticker == market_ticker]
        
        # Forward fill missing values
        filled_df = CandlestickData.forward_fill_and_return_df(
            candlesticks=ticker_candlesticks, interval_type=interval_type
        )
        
        # Detect patterns using TA-Lib
        patterns_talib = PatternDetector.detect_patterns_talib(filled_df=filled_df)
        
        # Save snapshots for detected patterns
        SmokehouseCharts.save_pattern_snapshots(filled_df=filled_df, patterns=patterns_talib, output_dir=output_dir)
        
        print(f"Processed {market_ticker} for {interval_name} interval.")

        # Count detected patterns per interval
        for pattern_dict in patterns_talib:
            for pattern in pattern_dict.values():
                pattern_counts_per_interval[interval_name][pattern.Name] += 1

# Print final pattern count summary per interval
print("\nPattern Detection Summary:")
for interval, pattern_counts in pattern_counts_per_interval.items():
    print(f"\n{interval} Interval:")
    for pattern, count in pattern_counts.items():
        print(f"{pattern}: {count}")