import requests
import pyodbc
import time
from datetime import datetime, timedelta
import json
import argparse
from SmokehouseModules import SmokehouseSql as ssql

def parse_arguments():
    parser = argparse.ArgumentParser(description='Fetch and store candlestick data from Kalshi API')

    parser.add_argument('--series-ticker',
                        required=True,
                        help='Series ticker symbol')

    parser.add_argument('--market-ticker',
                        required=True,
                        help='Market ticker symbol')
    
    parser.add_argument('--interval',
                        choices=['day','hour','minute'],
                        default='hour',
                        help='Interval type (day, hour, minute)')
    
    parser.add_argument('--start-ts',
                        type=int,
                        help='Start timestamp (Unix timestamp in seconds). Default: 30 days ago')
    
    parser.add_argument('--end-ts',
                        type=int,
                        help='End timestamp (Unix timestamp in seconds). Default: current time')

    return parser.parse_args()

# API Configuration
BASE_URL = "https://api.elections.kalshi.com/trade-api/v2"

# Time intervals in seconds - only use minute intervals since that's what works
INTERVALS = {
    "minute": {"minutes": 1, "db_type": 1},
    "hour": {"minutes": 60, "db_type": 2},
    "day": {"minutes": 1440, "db_type": 3}
}

def get_candlesticks(args):
    endpoint = f"{BASE_URL}/series/{args.series_ticker}/markets/{args.market_ticker}/candlesticks"
    
    # Use provided timestamps or defaults
    end_ts = args.end_ts if args.end_ts else int(time.time())
    start_ts = args.start_ts if args.start_ts else (end_ts - (30 * 24 * 60 * 60))
    
    params = {
        "start_ts": start_ts,
        "end_ts": end_ts,
        "period_interval": INTERVALS[args.interval]["minutes"]
    }
    
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
            
            return {"ticker": data["ticker"], "candlesticks": flattened_candlesticks}
        return data
    
    except requests.exceptions.RequestException as e:
        print(f"API request failed: {str(e)}")
        return None

def insert_candlesticks(market_ticker, candlesticks, interval_type):
    conn = ssql.connect()
    cursor = conn.cursor()

    for candlestick in candlesticks:
        try:            
            cursor.execute("""
                EXEC sp_InsertUpdateCandlestick 
                    @market_ticker=?, @interval_type=?, 
                    @end_period_ts=?, @open_interest=?, @price_close=?,
                    @price_high=?, @price_low=?, @price_mean=?, @price_open=?,
                    @price_previous=?, @volume=?, @yes_ask_close=?, @yes_ask_high=?,
                    @yes_ask_low=?, @yes_ask_open=?, @yes_bid_close=?, @yes_bid_high=?,
                    @yes_bid_low=?, @yes_bid_open=?
                """,
                market_ticker,
                INTERVALS[interval_type]["db_type"],
                candlestick["end_ts"],
                candlestick["open_interest"],
                candlestick["close"],
                candlestick["high"],
                candlestick["low"],
                candlestick["mean"],
                candlestick["open"],
                candlestick["previous"],
                candlestick["volume"],
                candlestick["yes_ask_close"],
                candlestick["yes_ask_high"],
                candlestick["yes_ask_low"],
                candlestick["yes_ask_open"],
                candlestick["yes_bid_close"],
                candlestick["yes_bid_high"],
                candlestick["yes_bid_low"],
                candlestick["yes_bid_open"]
            )
            conn.commit()
            
        except Exception as e:
            print(f"Error processing candlestick: {str(e)}", flush=True)
            print("Candlestick data:", json.dumps(candlestick, indent=2), flush=True)
            continue

    cursor.close()
    conn.close()

def main():
    
    args = parse_arguments()
    
    try:
        print(f"\nFetching {args.interval} candlesticks...")
        response_data = get_candlesticks(args)
        
        if response_data and 'candlesticks' in response_data:
            candlesticks = response_data['candlesticks']
            if candlesticks:
                insert_candlesticks(args.market_ticker, candlesticks, args.interval)
                print(f"COMPLETED: Candlesticks interval {args.interval}, {args.interval} candlesticks...", flush=True)
                time.sleep(.05)
            else:
                print(f"No candlesticks found in data for {args.interval} interval", flush=True)
        else:
            print(f"Invalid response format for {args.interval} interval")
            
            
    except Exception as e:
        print(f"Error processing {args.interval} interval: {str(e)}")
        import traceback
        print(traceback.format_exc())

if __name__ == "__main__":
    main()