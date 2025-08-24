import os
import logging
from datetime import datetime, timedelta
import pandas as pd
import pytz
import argparse
from collections import defaultdict
from SmokehouseModules import CandlestickData, SmokehouseCharts, PatternDetector, SmokehouseDebug

# Ensure Kaleido is installed for exporting images
try:
    import kaleido
except ImportError:
    print("Warning: Kaleido not found. Run 'pip install kaleido' to enable image export.")

# Setup logging
logging.basicConfig(level=logging.INFO, format="%(asctime)s - %(levelname)s - %(message)s")

# Default values
DEFAULT_START_DATE = (datetime.now() - timedelta(days=365)).strftime("%Y-%m-%d %H:%M")
DEFAULT_END_DATE = datetime.now().strftime("%Y-%m-%d %H:%M")
DEFAULT_INTERVAL_TYPE = 1

# Parse command-line arguments
parser = argparse.ArgumentParser(description="Generate an interactive candlestick chart and export pattern snapshots.")
parser.add_argument("--start-date", type=str, default=DEFAULT_START_DATE, help="Start date (YYYY-MM-DD).")
parser.add_argument("--end-date", type=str, default=DEFAULT_END_DATE, help="End date (YYYY-MM-DD).")
parser.add_argument("--interval-type", type=int, default=DEFAULT_INTERVAL_TYPE, choices=[1, 2, 3], help="Interval type: 1=minute, 2=hour, 3=day.")
args = parser.parse_args()

# Configurable parameters
start_date = datetime.strptime(args.start_date, "%Y-%m-%d %H:%M")
end_date = datetime.strptime(args.end_date, "%Y-%m-%d %H:%M")
interval_type = args.interval_type

logging.info(f"Fetching candlestick data from {start_date} to {end_date} at interval type {interval_type}...")

# Fetch and process candlestick data
candlesticks = CandlestickData.get_candlestick_data(interval_type, Return_Top=250)
if not candlesticks:
    logging.warning("No data returned for the given parameters. Exiting...")
    exit()

# Extract unique market tickers
market_tickers = list(set(candle.market_ticker for candle in candlesticks))
logging.info(f"Found {len(market_tickers)} unique market tickers.")

pattern_counts = defaultdict(int)

for market in market_tickers:
    logging.info(f"Processing market: {market}")

    # Filter candlesticks for the current market only
    market_candlesticks = [candle for candle in candlesticks if candle.market_ticker == market]
    logging.info(f"Filtered {len(market_candlesticks)} candlesticks for market: {market}")

    # Forward-fill only the relevant candlestick data
    filled_df = CandlestickData.forward_fill_and_return_df(
        market_ticker=market, 
        candlesticks=market_candlesticks, 
        interval_type=interval_type
    )
    logging.info(f"Forward-filled missing data for market: {market}")

    # Detect patterns
    patterns_talib = PatternDetector.detect_patterns_talib(filled_df=filled_df)
    logging.info(f"Detected {len(patterns_talib)} patterns in market: {market}")

    # Count occurrences of each pattern
    for pattern_dict in patterns_talib:
        for pattern_name, pattern in pattern_dict.items():
            if pattern_counts[pattern.Name] <= 20:
                output_dir = rf"..\..\TestingOutput\imageCandlestickPatterns\{pattern.Name}"
                # Create directories for saving pattern snapshots
                os.makedirs(output_dir, exist_ok=True)
                # Save snapshots
                patternPass = [{0:pattern}]
                SmokehouseCharts.save_pattern_snapshot(patterns=patternPass, output_dir=output_dir)
                logging.info(f"Saved pattern snapshot for pattern: {pattern.Name}")
            pattern_counts[pattern.Name] += 1

    # Output total counts of each pattern type
    logging.info("\nPattern Occurrences:")
    for pattern, count in sorted(pattern_counts.items(), key=lambda x: -x[1]):  # Sort by highest count
        logging.info(f"{pattern}: {count}")
            
    # Generate and display interactive chart (currently commented out)
    # SmokehouseCharts.create_interactive_chart(filled_df, patterns_talib, candlesticks, market_tickers)
    


logging.info("Processing complete.")
