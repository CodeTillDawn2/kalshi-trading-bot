#Conventional API call for Markets

import requests
import pyodbc
from datetime import datetime
import logging
import sys
import json
import time
import argparse
from pprint import pformat
from requests.adapters import HTTPAdapter
from urllib3.util.retry import Retry
from SmokehouseModules import SmokehouseSql as ssql

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    stream=sys.stdout
)
logger = logging.getLogger(__name__)

def parse_arguments():
    """Parse command line arguments"""
    parser = argparse.ArgumentParser(description='Fetch and process Kalshi market data')
    
    parser.add_argument('--event-ticker', type=str, help='Event ticker to filter by')
    parser.add_argument('--series-ticker', type=str, help='Series ticker to filter by')
    parser.add_argument('--max-close-ts', type=str, help='Maximum close timestamp')
    parser.add_argument('--min-close-ts', type=str, help='Minimum close timestamp')
    parser.add_argument('--status', type=str, help='Status to filter by')
    parser.add_argument('--tickers', type=str, help='Comma-separated list of tickers')
    
    args = parser.parse_args()
    
    # Convert tickers string to list if provided
    if args.tickers:
        args.tickers = [t.strip() for t in args.tickers.split(',')]
    
    return args

def build_api_params(args):
    """Build API parameters from command line arguments"""
    params = {}
    
    if args.event_ticker:
        params['event_ticker'] = args.event_ticker
    if args.series_ticker:
        params['series_ticker'] = args.series_ticker
    if args.max_close_ts:
        params['max_close_ts'] = args.max_close_ts
    if args.min_close_ts:
        params['min_close_ts'] = args.min_close_ts
    if args.status:
        params['status'] = args.status
    if args.tickers:
        params['tickers'] = ','.join(args.tickers)
    
    return params

def check_market_table():
    """Check and report current state of market table"""
    try:
        conn = ssql.connect()
        cursor = conn.cursor()
        
        cursor.execute("SELECT COUNT(*) FROM t_Markets")
        count = cursor.fetchone()[0]
        logger.info(f"Current market count: {count}")
        
        cursor.close()
        conn.close()
        
    except Exception as e:
        logger.error(f"Error checking market table: {e}")
        raise

def process_single_market(cursor, market):
    """Process a single market record using the stored procedure"""
    try:
        # Parse dates, allowing NULL values
        open_time = datetime.fromisoformat(market['open_time'].replace('Z', '+00:00')) if market.get('open_time') else None
        close_time = datetime.fromisoformat(market['close_time'].replace('Z', '+00:00')) if market.get('close_time') else None
        expected_expiration = datetime.fromisoformat(market['expected_expiration_time'].replace('Z', '+00:00')) if market.get('expected_expiration_time') else None
        expiration_time = datetime.fromisoformat(market['expiration_time'].replace('Z', '+00:00')) if market.get('expiration_time') else None
        latest_expiration = datetime.fromisoformat(market['latest_expiration_time'].replace('Z', '+00:00')) if market.get('latest_expiration_time') else None
        
        # Execute stored procedure
        cursor.execute("""
            EXEC sp_InsertUpdateMarket 
                @market_ticker=?, @event_ticker=?, @market_type=?, @title=?, @subtitle=?,
                @yes_sub_title=?, @no_sub_title=?, @open_time=?, @close_time=?, 
                @expected_expiration_time=?, @expiration_time=?, @latest_expiration_time=?,
                @settlement_timer_seconds=?, @status=?, @response_price_units=?,
                @notional_value=?, @tick_size=?, @yes_bid=?, @yes_ask=?, @no_bid=?,
                @no_ask=?, @last_price=?, @previous_yes_bid=?, @previous_yes_ask=?,
                @previous_price=?, @volume=?, @volume_24h=?, @liquidity=?, @open_interest=?,
                @result=?, @can_close_early=?, @expiration_value=?, @category=?,
                @risk_limit_cents=?, @strike_type=?, @floor_strike=?, @rules_primary=?,
                @rules_secondary=?
        """, [
            market['ticker'],
            market['event_ticker'],
            market['market_type'],
            market['title'],
            market.get('subtitle'),
            market['yes_sub_title'],
            market['no_sub_title'],
            open_time,
            close_time,
            expected_expiration,
            expiration_time,
            latest_expiration,
            market['settlement_timer_seconds'],
            market['status'],
            market['response_price_units'],
            market['notional_value'],
            market['tick_size'],
            market.get('yes_bid', 0),
            market.get('yes_ask', 0),
            market.get('no_bid', 0),
            market.get('no_ask', 0),
            market.get('last_price', 0),
            market.get('previous_yes_bid', 0),
            market.get('previous_yes_ask', 0),
            market.get('previous_price', 0),
            market['volume'],
            market['volume_24h'],
            market['liquidity'],
            market['open_interest'],
            market.get('result', ''),
            market['can_close_early'],
            market.get('expiration_value', ''),
            market.get('category', ''),
            market['risk_limit_cents'],
            market.get('strike_type', ''),
            0,  # floor_strike - setting default value of 0 since it's required
            market.get('rules_primary', ''),
            market.get('rules_secondary', '')
        ])
        
        return True
        
    except Exception as e:
        logger.error(f"Error in process_single_market for {market.get('ticker', 'UNKNOWN')}: {e}")
        logger.error(f"Market data: {json.dumps(market, indent=2)}")
        return False
        
def get_markets_with_session(api_params):
    """Fetch markets from API and process immediately"""
    session = requests.Session()
    retry_strategy = Retry(
        total=3,
        backoff_factor=0.5,
        status_forcelist=[500, 502, 503, 504]
    )
    adapter = HTTPAdapter(max_retries=retry_strategy)
    session.mount("https://", adapter)
    
    conn = ssql.connect()
    cursor = conn.cursor()
    
    base_url = "https://api.elections.kalshi.com/trade-api/v2/markets"
    processed_count = 0
    error_count = 0
    cursor_token = ""
    
    try:
        while True:
            # Combine cursor with provided API parameters
            current_params = api_params.copy()
            if cursor_token:
                current_params['cursor'] = cursor_token
            
            try:
                response = session.get(base_url, params=current_params)
                response.raise_for_status()
                data = response.json()
                markets = data.get('markets', [])
                
                if not markets:
                    break
                
                logger.info(f"Processing batch of {len(markets)} markets...")
                
                for market in markets:
                    try:
                        if process_single_market(cursor, market):
                            processed_count += 1
                            conn.commit()
                        else:
                            error_count += 1
                            conn.rollback()
                    except Exception as e:
                        error_count += 1
                        conn.rollback()
                        logger.error(f"Failed to process market: {e}")
                
                logger.info(f"Processed: {processed_count}, Errors: {error_count}")
                
                cursor_token = data.get('cursor')
                if not cursor_token:
                    break
                    
                time.sleep(0.2)
                
            except requests.exceptions.RequestException as e:
                logger.error(f"API request failed: {e}")
                if hasattr(e, 'response'):
                    logger.error(f"Response: {e.response.text}")
                break
                
    except Exception as e:
        logger.error(f"Unexpected error: {e}")
        raise
        
    finally:
        cursor.close()
        conn.close()
        session.close()
        
    return processed_count, error_count

def main():
    """Main execution flow"""
    args = parse_arguments()
    api_params = build_api_params(args)
    
    logger.info("Starting market data retrieval and processing...")
    logger.info(f"Using API parameters: {api_params}")
    
    try:
        logger.info("Checking initial table status...")
        check_market_table()
        
        logger.info("Starting market retrieval and processing...")
        processed, errors = get_markets_with_session(api_params)
        
        logger.info(f"Completed processing. Successful: {processed}, Errors: {errors}")
        
        logger.info("Checking final table status...")
        check_market_table()
        
        print(f"COMPLETED: Markets")
        time.sleep(.05)
        
    except Exception as e:
        logger.error(f"Fatal error in main process: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()