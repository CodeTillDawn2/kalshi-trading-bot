import requests
import pyodbc
import time
import json
import argparse
import os
from datetime import datetime
from SmokehouseModules import SmokehouseSql as ssql

# API Configuration
BASE_URL = "https://api.elections.kalshi.com/trade-api/v2"

# Time intervals in seconds with maximum retrieval lengths in days and cushion
INTERVALS = {
    "minute": {"minutes": 1, "db_type": 1, "max_days": 3, "cushion_seconds": 60},       # 1 minute cushion
    "hour": {"minutes": 60, "db_type": 2, "max_days": 7, "cushion_seconds": 3600},     # 1 hour cushion
    "day": {"minutes": 1440, "db_type": 3, "max_days": 15, "cushion_seconds": 86400}  # 1 day cushion
}

# Network shared directory and SQL Server credentials
SHARED_DIR = r"\\DESKTOP-ITC50UT\SmokehouseCandlestickImport" 

def parse_arguments():
    parser = argparse.ArgumentParser(description='Fetch and store candlestick data from Kalshi API with chunking and cushion')
    parser.add_argument('--series-ticker', default="KXEURUSD", help='Series ticker symbol (default: KXEPSTEIN)')
    parser.add_argument('--market-ticker', default="EURUSD-23DEC2510-B1.089", help='Market ticker symbol (default: KXEPSTEIN-25)')
    parser.add_argument('--start-ts-m', type=int, default=1677042000, help='Start timestamp for minute data (Unix timestamp in seconds). Default: 1736571600 (Jan 13, 2025 05:00:00 UTC)')
    parser.add_argument('--start-ts-h', type=int, default=1677042000, help='Start timestamp for hour data (Unix timestamp in seconds). Default: 1736571600 (Jan 13, 2025 05:00:00 UTC)')
    parser.add_argument('--start-ts-d', type=int, default=1677042000, help='Start timestamp for day data (Unix timestamp in seconds). Default: 1736571600 (Jan 13, 2025 05:00:00 UTC)')
    parser.add_argument('--end-ts', type=int, help='End timestamp (Unix timestamp in seconds). Default: current time')
    return parser.parse_args()

def calculate_time_diff(start_ts, end_ts):
    diff_seconds = end_ts - start_ts
    minutes = diff_seconds // 60
    hours = minutes // 60
    days = hours // 24
    return minutes, hours, days

def format_status(market, counts):
    return (f"Market: {market}... Data found: M: {counts['minute']} H: {counts['hour']} D: {counts['day']} ")

def get_candlesticks(series_ticker, market_ticker, interval, start_ts, end_ts, counts):
    endpoint = f"{BASE_URL}/series/{series_ticker}/markets/{market_ticker}/candlesticks"
    params = {
        "start_ts": start_ts,
        "end_ts": end_ts,
        "period_interval": INTERVALS[interval]["minutes"]
    }
    
    print(format_status(market_ticker, counts), flush=True)
    try:
        response = requests.get(endpoint, params=params)
        response.raise_for_status()
        data = response.json()
        
        if "candlesticks" in data:
            flattened_candlesticks = []
            for candlestick in data["candlesticks"]:
                flattened = {
                    "end_ts": candlestick["end_period_ts"],
                    "open_interest": candlestick["open_interest"],
                    "volume": candlestick["volume"],
                    "close": candlestick["price"].get("close"),
                    "high": candlestick["price"].get("high"),
                    "low": candlestick["price"].get("low"),
                    "mean": candlestick["price"].get("mean"),
                    "open": candlestick["price"].get("open"),
                    "previous": candlestick["price"].get("previous"),
                    "yes_ask_close": candlestick["yes_ask"].get("close"),
                    "yes_ask_high": candlestick["yes_ask"].get("high"),
                    "yes_ask_low": candlestick["yes_ask"].get("low"),
                    "yes_ask_open": candlestick["yes_ask"].get("open"),
                    "yes_bid_close": candlestick["yes_bid"].get("close"),
                    "yes_bid_high": candlestick["yes_bid"].get("high"),
                    "yes_bid_low": candlestick["yes_bid"].get("low"),
                    "yes_bid_open": candlestick["yes_bid"].get("open")
                }
                flattened_candlesticks.append(flattened)
            counts[interval] += len(flattened_candlesticks)
            print(format_status(market_ticker, counts), flush=True)
            return {"ticker": data["ticker"], "candlesticks": flattened_candlesticks}, counts
        print(format_status(market_ticker, counts), flush=True)
        return data, counts
    
    except requests.exceptions.RequestException:
        print(format_status(market_ticker, counts), flush=True)
        return None, counts

def upload_and_trigger_import(market_ticker, candlesticks, interval_type, start_ts, counts):
    end_ts = candlesticks[-1]["end_ts"] if candlesticks else start_ts
    print(format_status(market_ticker, counts), flush=True)
    
    json_candlesticks = [
        {
            "market_ticker": market_ticker,
            "interval_type": INTERVALS[interval_type]["db_type"],
            "end_period_ts": candlestick["end_ts"],
            "open_interest": candlestick["open_interest"],
            "price_close": candlestick["close"],
            "price_high": candlestick["high"],
            "price_low": candlestick["low"],
            "price_mean": candlestick["mean"],
            "price_open": candlestick["open"],
            "price_previous": candlestick["previous"],
            "volume": candlestick["volume"],
            "yes_ask_close": candlestick["yes_ask_close"],
            "yes_ask_high": candlestick["yes_ask_high"],
            "yes_ask_low": candlestick["yes_ask_low"],
            "yes_ask_open": candlestick["yes_ask_open"],
            "yes_bid_close": candlestick["yes_bid_close"],
            "yes_bid_high": candlestick["yes_bid_high"],
            "yes_bid_low": candlestick["yes_bid_low"],
            "yes_bid_open": candlestick["yes_bid_open"]
        }
        for candlestick in candlesticks
    ]

    filename = f"candlesticks_{interval_type}_{start_ts}.json"
    remote_path = os.path.join(SHARED_DIR, filename)
    
    print(format_status(market_ticker, counts), flush=True)
    os.makedirs(SHARED_DIR, exist_ok=True)
    
    with open(remote_path, 'w', encoding='utf-16-le') as f:
        f.write('\ufeff')  # BOM for UTF-16 LE
        json.dump(json_candlesticks, f)
    print(format_status(market_ticker, counts), flush=True)

def fetch_and_store_all_intervals(args):
    end_ts = args.end_ts if args.end_ts else int(time.time())
    start_ts_m = args.start_ts_m
    start_ts_h = args.start_ts_h
    start_ts_d = args.start_ts_d
    counts = {"minute": 0, "hour": 0, "day": 0}
    
    print(format_status(args.market_ticker, counts), flush=True)
    for interval, config in INTERVALS.items():
        max_seconds = config["max_days"] * 24 * 60 * 60  # Max chunk size in seconds
        cushion_seconds = config["cushion_seconds"]      # Overlap cushion
        if interval == "minute":
            start_ts = start_ts_m
        elif interval == "hour":
            start_ts = start_ts_h
        else:
            start_ts = start_ts_d
        current_start = start_ts
        
        while current_start < end_ts:
            current_end = min(current_start + max_seconds, end_ts)
            print(format_status(args.market_ticker, counts), flush=True)
            
            response_data, counts = get_candlesticks(args.series_ticker, args.market_ticker, interval, current_start, current_end, counts)
            
            if response_data and 'candlesticks' in response_data:
                candlesticks = response_data['candlesticks']
                if candlesticks:
                    upload_and_trigger_import(args.market_ticker, candlesticks, interval, current_start, counts)
                    print(format_status(args.market_ticker, counts), flush=True)
            
            current_start = current_end - cushion_seconds if current_end < end_ts else end_ts
            time.sleep(0.05)  # Small delay to avoid overwhelming the API
    print(format_status(args.market_ticker, counts), flush=True)

def main():
    args = parse_arguments()
    counts = {"minute": 0, "hour": 0, "day": 0}
    print(format_status(args.market_ticker, counts), flush=True)
    fetch_and_store_all_intervals(args)
    print(f"COMPLETED: {args.market_ticker}", flush=True)

if __name__ == "__main__":
    main()