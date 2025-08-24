# Pulls all events and markets from standard Kalshi API and saves them to SQL Server. 
import time
import requests
import pyodbc
import argparse
from datetime import datetime
from SmokehouseModules import SmokehouseSql as ssql

def parse_arguments():
    parser = argparse.ArgumentParser(description='Fetch and save Kalshi events and markets')
    
    parser.add_argument('--status',
                        choices=['open', 'closed', 'settled'],
                        default='open',
                        help='Status of events to fetch (open, closed, or settled)')
    
    parser.add_argument('--series-ticker',
                        type=str,
                        help='Specific series ticker to fetch')
    
    parser.add_argument('--with-nested-markets',
                        type=str,
                        choices=['true', 'false'],
                        default='true',
                        help='Whether to include nested market data')

    return parser.parse_args()

def fetch_all_events(status='open', series_ticker=None, with_nested_markets='true'):
    all_events_kalshi = []
    url = "https://api.elections.kalshi.com/trade-api/v2/events"

    params = {
        "with_nested_markets": with_nested_markets,
        "status": status,
        "limit": 200
    }

    # Add series_ticker to params if provided
    if series_ticker:
        params["series_ticker"] = series_ticker

    while True:
        r = requests.get(url, params=params)
        response = r.json()
        if 'events' in response:
            all_events_kalshi.extend(response['events'])
        else:
            break

        cursor = response.get('cursor', None)
        if not cursor:
            break
        params['cursor'] = cursor

    return all_events_kalshi

def save_data_to_sql(events):
    print("Starting save_data_to_sql function")
    
    conn = ssql.connect()
    cursor = conn.cursor()

    print(f"Total events to process: {len(events)}")

    try:
        # First, process all events
        for event in events:
            try:
                # Execute event stored procedure
                cursor.execute("""
                    EXEC sp_InsertUpdateEvent 
                        @event_ticker=?, 
                        @series_ticker=?, 
                        @title=?, 
                        @sub_title=?, 
                        @collateral_return_type=?, 
                        @mutually_exclusive=?, 
                        @category=?
                """, 
                    event.get('event_ticker', ''),              # event_ticker
                    event.get('series_ticker', ''),             # series_ticker
                    event.get('title', ''),                     # title
                    event.get('sub_title', ''),                 # sub_title
                    event.get('collateral_return_type', ''),    # collateral_return_type
                    event.get('mutually_exclusive', False),     # mutually_exclusive
                    event.get('category', '')                   # category
                )
                conn.commit()
                print(f"Processed event: {event.get('event_ticker')}")

                # Then process all markets for this event
                for market in event.get('markets', []):
                    # Convert ISO format dates to datetime objects
                    open_time = datetime.fromisoformat(market.get('open_time', '').replace('Z', '+00:00')) if market.get('open_time') else None
                    close_time = datetime.fromisoformat(market.get('close_time', '').replace('Z', '+00:00')) if market.get('close_time') else None
                    expected_expiration_time = datetime.fromisoformat(market.get('expected_expiration_time', '').replace('Z', '+00:00')) if market.get('expected_expiration_time') else None
                    expiration_time = datetime.fromisoformat(market.get('expiration_time', '').replace('Z', '+00:00')) if market.get('expiration_time') else None
                    latest_expiration_time = datetime.fromisoformat(market.get('latest_expiration_time', '').replace('Z', '+00:00')) if market.get('latest_expiration_time') else None

                    # Execute market stored procedure
                    cursor.execute("""
                        EXEC sp_InsertUpdateMarket 
                            @market_ticker=?, @event_ticker=?, @market_type=?, @title=?, 
                            @subtitle=?, @yes_sub_title=?, @no_sub_title=?, @open_time=?, 
                            @close_time=?, @expected_expiration_time=?, @expiration_time=?, 
                            @latest_expiration_time=?, @settlement_timer_seconds=?, @status=?,
                            @response_price_units=?, @notional_value=?, @tick_size=?, 
                            @yes_bid=?, @yes_ask=?, @no_bid=?, @no_ask=?, @last_price=?,
                            @previous_yes_bid=?, @previous_yes_ask=?, @previous_price=?,
                            @volume=?, @volume_24h=?, @liquidity=?, @open_interest=?,
                            @result=?, @can_close_early=?, @expiration_value=?, @category=?,
                            @risk_limit_cents=?, @strike_type=?, @floor_strike=?,
                            @rules_primary=?, @rules_secondary=?
                    """, 
                        market.get('ticker', ''),                    # market_ticker
                        event.get('event_ticker', ''),              # event_ticker
                        market.get('market_type', ''),               # market_type
                        market.get('title', ''),                    # title
                        market.get('subtitle', ''),                 # subtitle
                        market.get('yes_sub_title', ''),           # yes_sub_title
                        market.get('no_sub_title', ''),            # no_sub_title
                        open_time,                                  # open_time
                        close_time,                                # close_time
                        expected_expiration_time,                  # expected_expiration_time
                        expiration_time,                          # expiration_time
                        latest_expiration_time,                   # latest_expiration_time
                        market.get('settlement_timer_seconds', 0), # settlement_timer_seconds
                        market.get('status', ''),                  # status
                        market.get('response_price_units', ''),   # response_price_units
                        market.get('notional_value', 0),          # notional_value
                        market.get('tick_size', 0),               # tick_size
                        market.get('yes_bid', 0),                 # yes_bid
                        market.get('yes_ask', 0),                 # yes_ask
                        market.get('no_bid', 0),                  # no_bid
                        market.get('no_ask', 0),                  # no_ask
                        market.get('last_price', 0),              # last_price
                        market.get('previous_yes_bid', 0),        # previous_yes_bid
                        market.get('previous_yes_ask', 0),        # previous_yes_ask
                        market.get('previous_price', 0),          # previous_price
                        market.get('volume', 0),                  # volume
                        market.get('volume_24h', 0),              # volume_24h
                        market.get('liquidity', 0),               # liquidity
                        market.get('open_interest', 0),           # open_interest
                        market.get('result', ''),                 # result
                        market.get('can_close_early', False),     # can_close_early
                        market.get('expiration_value', ''),       # expiration_value
                        market.get('category', ''),               # category
                        market.get('risk_limit_cents', 0),        # risk_limit_cents
                        market.get('strike_type', 'unknown'),     # strike_type
                        market.get('floor_strike', 0),            # floor_strike
                        market.get('rules_primary', ''),          # rules_primary
                        market.get('rules_secondary', '')         # rules_secondary
                    )
                    conn.commit()
                    print(f"Processed market: {market.get('ticker')}", flush=True)

            except Exception as e:
                print(f"Error processing event {event.get('event_ticker')}: {str(e)}")
                conn.rollback()
                
    except Exception as e:
        print(f"Error in save_data_to_sql: {str(e)}")
        conn.rollback()
    
    finally:
        cursor.close()
        conn.close()
        print(f"COMPLETED: Events")
        time.sleep(.05)

def main():
    args = parse_arguments()
    
    print(f"Starting Events", flush=True)
    
    # Fetch events with provided arguments
    events = fetch_all_events(
        status=args.status,
        series_ticker=args.series_ticker,
        with_nested_markets=args.with_nested_markets
    )
    
    # Save the fetched data
    save_data_to_sql(events)

if __name__ == "__main__":
    main()