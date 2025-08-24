# Websocket clients for Kalshi API. Clients are referenced by main.py for execution. 
# Live data is streamed through the websocket connection and saved to SQL Server.

import requests
import base64
import time
import asyncio
from typing import Any, Dict, Optional
from datetime import datetime, timedelta
from enum import Enum
import json
import pyodbc
from requests.exceptions import HTTPError
import argparse

from cryptography.hazmat.primitives import serialization, hashes
from cryptography.hazmat.primitives.asymmetric import padding, rsa
from cryptography.exceptions import InvalidSignature
from SmokehouseModules import SmokehouseSql as ssql

import websockets

def parse_arguments():
    parser = argparse.ArgumentParser(description='Kalshi WebSocket Client')
    
    parser.add_argument('--action',
                        required=True,
                        choices=['orderbook', 'ticker', 'trade', 'fill', 'lifecycle'],
                        help='Type of feed to subscribe to')
    
    parser.add_argument('--market-tickers',
                        nargs='+',  # This allows for multiple values
                        help='List of market tickers (required for orderbook, optional for ticker and trade feeds)')
    args = parser.parse_args()
    
    # Validate that market_tickers is provided when action is orderbook
    if args.action == 'orderbook' and not args.market_tickers:
        parser.error("--market-tickers is required when action is 'orderbook'")
    
    return args

class Environment(Enum):
    DEMO = "demo"
    PROD = "prod"

class KalshiBaseClient:
    """Base client class for interacting with the Kalshi API."""
    def __init__(
        self,
        key_id: str,
        action: str,
        private_key: rsa.RSAPrivateKey,
        environment: Environment = Environment.PROD
    ):
        """Initializes the client with the provided API key and private key."""
        self.key_id = key_id
        self.private_key = private_key
        self.Action = action
        self.environment = environment
        self.last_api_call = datetime.now()

        if self.environment == Environment.DEMO:
            self.HTTP_BASE_URL = "https://demo-api.kalshi.co"
            self.WS_BASE_URL = "wss://demo-api.kalshi.co"
        elif self.environment == Environment.PROD:
            self.HTTP_BASE_URL = "https://api.elections.kalshi.com"
            self.WS_BASE_URL = "wss://api.elections.kalshi.com"
        else:
            raise ValueError("Invalid environment")

    def request_headers(self, method: str, path: str) -> Dict[str, Any]:
        """Generates the required authentication headers for API requests."""
        current_time_milliseconds = int(time.time() * 1000)
        timestamp_str = str(current_time_milliseconds)

        # Remove query params from path
        path_parts = path.split('?')

        msg_string = timestamp_str + method + path_parts[0]
        signature = self.sign_pss_text(msg_string)

        headers = {
            "Content-Type": "application/json",
            "KALSHI-ACCESS-KEY": self.key_id,
            "KALSHI-ACCESS-SIGNATURE": signature,
            "KALSHI-ACCESS-TIMESTAMP": timestamp_str,
        }
        return headers

    def sign_pss_text(self, text: str) -> str:
        """Signs the text using RSA-PSS and returns the base64 encoded signature."""
        message = text.encode('utf-8')
        try:
            signature = self.private_key.sign(
                message,
                padding.PSS(
                    mgf=padding.MGF1(hashes.SHA256()),
                    salt_length=padding.PSS.DIGEST_LENGTH
                ),
                hashes.SHA256()
            )
            return base64.b64encode(signature).decode('utf-8')
        except InvalidSignature as e:
            raise ValueError("RSA sign PSS failed") from e

class KalshiHttpClient(KalshiBaseClient):
    """Client for handling HTTP connections to the Kalshi API."""
    def __init__(
        self,
        key_id: str,
        action: str,
        private_key: rsa.RSAPrivateKey,
        environment: Environment = Environment.PROD
    ):
        super().__init__(key_id, action, private_key, environment)
        self.host = self.HTTP_BASE_URL
        self.exchange_url = "/trade-api/v2/exchange"
        self.markets_url = "/trade-api/v2/markets"
        self.portfolio_url = "/trade-api/v2/portfolio"

    def rate_limit(self) -> None:
        """Built-in rate limiter to prevent exceeding API rate limits."""
        THRESHOLD_IN_MILLISECONDS = 100
        now = datetime.now()
        threshold_in_microseconds = 1000 * THRESHOLD_IN_MILLISECONDS
        threshold_in_seconds = THRESHOLD_IN_MILLISECONDS / 1000
        if now - self.last_api_call < timedelta(microseconds=threshold_in_microseconds):
            time.sleep(threshold_in_seconds)
        self.last_api_call = datetime.now()

    def raise_if_bad_response(self, response: requests.Response) -> None:
        """Raises an HTTPError if the response status code indicates an error."""
        if response.status_code not in range(200, 299):
            response.raise_for_status()

    def post(self, path: str, body: dict) -> Any:
        """Performs an authenticated POST request to the Kalshi API."""
        self.rate_limit()
        response = requests.post(
            self.host + path,
            json=body,
            headers=self.request_headers("POST", path)
        )
        self.raise_if_bad_response(response)
        return response.json()

    def get(self, path: str, params: Dict[str, Any] = {}) -> Any:
        """Performs an authenticated GET request to the Kalshi API."""
        self.rate_limit()
        response = requests.get(
            self.host + path,
            headers=self.request_headers("GET", path),
            params=params
        )
        self.raise_if_bad_response(response)
        return response.json()

    def delete(self, path: str, params: Dict[str, Any] = {}) -> Any:
        """Performs an authenticated DELETE request to the Kalshi API."""
        self.rate_limit()
        response = requests.delete(
            self.host + path,
            headers=self.request_headers("DELETE", path),
            params=params
        )
        self.raise_if_bad_response(response)
        return response.json()

    def get_balance(self) -> Dict[str, Any]:
        """Retrieves the account balance."""
        return self.get(self.portfolio_url + '/balance')

    def get_exchange_status(self) -> Dict[str, Any]:
        """Retrieves the exchange status."""
        return self.get(self.exchange_url + "/status")

    def get_trades(
        self,
        ticker: Optional[str] = None,
        limit: Optional[int] = None,
        cursor: Optional[str] = None,
        max_ts: Optional[int] = None,
        min_ts: Optional[int] = None,
    ) -> Dict[str, Any]:
        """Retrieves trades based on provided filters."""
        params = {
            'ticker': ticker,
            'limit': limit,
            'cursor': cursor,
            'max_ts': max_ts,
            'min_ts': min_ts,
        }
        # Remove None values
        params = {k: v for k, v in params.items() if v is not None}
        return self.get(self.markets_url + '/trades', params=params)

class KalshiWebSocketClient(KalshiBaseClient):
    """Client for handling WebSocket connections to the Kalshi API."""
    def __init__(
        self,
        key_id: str,
        action: str,
        private_key: rsa.RSAPrivateKey,
        environment: Environment = Environment.PROD,
        market_tickers: list = None
    ):
        super().__init__(key_id, action, private_key, environment)
        self.ws = None
        self.url_suffix = "/trade-api/ws/v2"
        self.message_id = 1
        self.market_tickers = market_tickers
        self.conn = ssql.connect()
        self.cursor = self.conn.cursor()
        self.last_heartbeat = time.time()
        self.heartbeat_interval = 10  # seconds
        self.subscriptions = set()  # Track active subscriptions for reconnection

    async def on_open(self):
        """Callback when WebSocket connection is opened."""
        print("WebSocket connection opened.")
        print("Initiating subscriptions...")

        if self.Action == "orderbook":
            await self.subscribe_to_orderbook(self.market_tickers)
            print("Orderbook subscription request sent")
        elif self.Action == "ticker":
            await self.subscribe_to_tickers(self.market_tickers)
            print("Ticker subscription request sent")
        elif self.Action == "trade":
            await self.subscribe_to_trades(self.market_tickers)
            print("Trade subscription request sent")
        elif self.Action == "fill":
            await self.subscribe_to_fills()
            print("Fill subscription request sent")
        elif self.Action == "lifecycle":
            await self.subscribe_to_lifecycle()
            print("Market lifecycle subscription request sent")
        else:
            print("Action unknown")

    async def connect(self):
        """Establishes a WebSocket connection using authentication."""
        host = self.WS_BASE_URL + self.url_suffix
        auth_headers = self.request_headers("GET", self.url_suffix)
        
        while True:
            try:
                async with websockets.connect(
                    host, 
                    additional_headers=auth_headers,
                    ping_interval=None,  # Disable automatic ping
                    ping_timeout=None,   # Disable automatic ping timeout
                    close_timeout=10
                ) as websocket:
                    self.ws = websocket
                    self.last_heartbeat = time.time()
                    await self.on_open()
                    
                    # Start the heartbeat monitoring task
                    heartbeat_task = asyncio.create_task(self.heartbeat_monitor())
                    
                    try:
                        await self.handler()
                    finally:
                        heartbeat_task.cancel()
                        try:
                            await heartbeat_task
                        except asyncio.CancelledError:
                            pass
                        
            except websockets.ConnectionClosed as e:
                print(f"Connection closed with code {e.code}: {e.reason}")
                if e.code in [1001, 1006]:  # Going away or abnormal closure
                    print("Attempting to reconnect...")
                    await asyncio.sleep(5)  # Wait before reconnecting
                    continue
                break
                
            except Exception as e:
                print(f"Connection error: {e}")
                await asyncio.sleep(5)  # Wait before reconnecting
                continue

    async def heartbeat_monitor(self):
        """Monitor connection health using heartbeats."""
        while True:
            try:
                current_time = time.time()
                if current_time - self.last_heartbeat > self.heartbeat_interval * 2:
                    print("No heartbeat received for too long, closing connection...")
                    if self.ws:
                        try:
                            await self.ws.close(code=1001, reason="Heartbeat timeout")
                        except:
                            pass  # Connection might already be closed
                    break
                
                # Send ping to keep connection alive
                if self.ws:
                    try:
                        print("Ping", flush=True)
                        pong_waiter = await self.ws.ping()
                        await asyncio.wait_for(pong_waiter, timeout=self.heartbeat_interval)
                        self.last_heartbeat = time.time()
                    except (asyncio.TimeoutError, websockets.ConnectionClosed):
                        print("Ping failed, connection might be dead")
                        break
                    except Exception as e:
                        print(f"Error sending ping: {e}")
                        break
                
                await asyncio.sleep(self.heartbeat_interval)
                
            except asyncio.CancelledError:
                break
            except Exception as e:
                print(f"Error in heartbeat monitor: {e}")
                if self.ws:
                    try:
                        await self.ws.close(code=1001, reason=str(e))
                    except:
                        pass  # Connection might already be closed
                break

    async def subscribe_to_orderbook(self, market_tickers):
        """Subscribe to orderbook updates for specific markets."""
        subscription_message = {
            "id": self.message_id,
            "cmd": "subscribe",
            "params": {
                "channels": ["orderbook_delta"],
                "market_tickers": market_tickers if isinstance(market_tickers, list) else [market_tickers]
            }
        }
        print(f"Subscribing to orderbook with message: {json.dumps(subscription_message, indent=2)}")
        await self.ws.send(json.dumps(subscription_message))
        self.message_id += 1

    async def subscribe_to_tickers(self, market_tickers=None):
        """Subscribe to ticker updates."""
        subscription_message = {
            "id": self.message_id,
            "cmd": "subscribe",
            "params": {
                "channels": ["ticker"]
            }
        }
        if market_tickers:
            subscription_message["params"]["market_tickers"] = market_tickers
            
        print(f"Subscribing to tickers with message: {json.dumps(subscription_message, indent=2)}")
        await self.ws.send(json.dumps(subscription_message))
        self.message_id += 1

    async def subscribe_to_trades(self, market_tickers=None):
        """Subscribe to trade updates."""
        subscription_message = {
            "id": self.message_id,
            "cmd": "subscribe",
            "params": {
                "channels": ["trade"]
            }
        }
        if market_tickers:
            subscription_message["params"]["market_tickers"] = market_tickers
            
        print(f"Subscribing to trades with message: {json.dumps(subscription_message, indent=2)}")
        await self.ws.send(json.dumps(subscription_message))
        self.message_id += 1

    async def subscribe_to_fills(self):
        """Subscribe to fill updates for all markets."""
        subscription_message = {
            "id": self.message_id,
            "cmd": "subscribe",
            "params": {
                "channels": ["fill"]
            }
        }
        print(f"Subscribing to fills with message: {json.dumps(subscription_message, indent=2)}")
        await self.ws.send(json.dumps(subscription_message))
        self.message_id += 1

    async def subscribe_to_lifecycle(self):
        """Subscribe to market lifecycle updates."""
        subscription_message = {
            "id": self.message_id,
            "cmd": "subscribe",
            "params": {
                "channels": ["market_lifecycle"]
            }
        }
        print(f"Subscribing to market lifecycle with message: {json.dumps(subscription_message, indent=2)}")
        await self.ws.send(json.dumps(subscription_message))
        self.message_id += 1

    def save_orderbook_to_sql(self, data, offer_type):
        """Save orderbook data to SQL using stored procedure."""
        try:
            print(f"\nProcessing orderbook data:")
            print(f"Data: {json.dumps(data, indent=2)}")
            print(f"Offer type: {offer_type}")

            message = data['msg']
            kalshi_seq = data.get('seq')
            market_ticker = message.get('market_ticker')
            sid = data.get('sid')
            current_time = datetime.now()

            if offer_type == "SNP":  # Snapshot
                for side in ['yes', 'no']:
                    orders = message.get(side, [])
                    if not orders:
                        print(f"No {side} orders found in data")
                        continue
                    
                    print(f"Processing {len(orders)} {side} orders")
                    for price_level in orders:
                        sql = """
                        EXEC dbo.sp_InsertFeed_OrderBook 
                            @market_id = ?,
                            @sid = ?,
                            @kalshi_seq = ?,
                            @market_ticker = ?,
                            @offer_type = ?,
                            @price = ?,
                            @delta = ?,
                            @side = ?,
                            @resting_contracts = ?,
                            @LoggedDate = ?
                        """
                        
                        if side == "no":
                            price_level[0] = 100 - price_level[0]
                        
                        params = (
                            message.get('market_id', '00000000-0000-0000-0000-000000000000'),
                            sid, 
                            kalshi_seq,
                            market_ticker,
                            offer_type,
                            price_level[0],  # price
                            None,            # delta null for snapshot
                            side, 
                            price_level[1],  # count
                            current_time
                        )
                        print(f"\nExecuting SQL with params: {params}")
                        self.cursor.execute(sql, params)
                        print("SQL executed successfully")
                            
            elif offer_type == "DEL":  # Delta
                sql = """
                EXEC dbo.sp_InsertFeed_OrderBook 
                    @market_id = ?,
                    @sid = ?,
                    @kalshi_seq = ?,
                    @market_ticker = ?,
                    @offer_type = ?,
                    @price = ?,
                    @delta = ?,
                    @side = ?,
                    @resting_contracts = ?,
                    @LoggedDate = ?
                """
                
                price = message.get('price', 0)
                
                if message.get('side') == "no":
                    price = 100 - price
                
                params = (
                    message.get('market_id', '00000000-0000-0000-0000-000000000000'),
                    sid,
                    kalshi_seq,
                    market_ticker,
                    offer_type,
                    price,
                    message.get('delta', 0),
                    message.get('side'),
                    0,  # resting_contracts not provided in delta
                    current_time
                )
                print(f"\nExecuting SQL with params: {params}")
                self.cursor.execute(sql, params)
                print("SQL executed successfully")

            self.conn.commit()
            print("Transaction committed")
            
        except Exception as e:
            print(f"Error saving orderbook to SQL: {e}")
            print(f"Last SQL: {sql}")
            print(f"Last parameters: {params}")
            self.conn.rollback()
            print("Transaction rolled back")

    def save_ticker_to_sql(self, ticker_data):
        """Save ticker data to SQL using stored procedure."""
        try:
            print(f"\nProcessing ticker data:")
            print(f"Data: {json.dumps(ticker_data, indent=2)}")

            message = ticker_data['msg']

            sql = """
            EXEC dbo.sp_InsertFeed_Ticker 
                @market_id = ?,
                @market_ticker = ?,
                @price = ?,
                @yes_bid = ?,
                @yes_ask = ?,
                @volume = ?,
                @open_interest = ?,
                @dollar_volume = ?,
                @dollar_open_interest = ?,
                @ts = ?,
                @LoggedDate = ?
            """
            
            params = (
                message.get('market_id', '00000000-0000-0000-0000-000000000000'),
                message['market_ticker'],
                message['price'],
                message['yes_bid'],
                message['yes_ask'],
                message['volume'],
                message['open_interest'],
                message['dollar_volume'],
                message['dollar_open_interest'],
                message['ts'],
                datetime.now()
            )
            
            print(f"\nExecuting SQL with params: {params}")
            self.cursor.execute(sql, params)
            self.conn.commit()
            print("Transaction committed")

        except Exception as e:
            print(f"Error saving to SQL: {e}")
            print(f"Parameters: {params}")
            self.conn.rollback()
            print("Transaction rolled back")

    def save_trade_to_sql(self, trade_data):
        """Save trade data to SQL using stored procedure."""
        try:
            print(f"\nProcessing trade data:")
            print(f"Data: {json.dumps(trade_data, indent=2)}")

            message = trade_data['msg']

            sql = """
            EXEC dbo.sp_InsertFeed_Trade 
                @market_ticker = ?,
                @yes_price = ?,
                @no_price = ?,
                @count = ?,
                @taker_side = ?,
                @ts = ?,
                @LoggedDate = ?
            """
            
            params = (
                message['market_ticker'],
                message['yes_price'],
                message['no_price'],
                message['count'],
                message['taker_side'],
                message['ts'],
                datetime.now()
            )
            
            print(f"\nExecuting SQL with params: {params}")
            self.cursor.execute(sql, params)
            self.conn.commit()
            print("Transaction committed")

        except Exception as e:
            print(f"Error saving to SQL: {e}")
            print(f"Parameters: {params}")
            self.conn.rollback()
            print("Transaction rolled back")

    def save_fill_to_sql(self, fill_data):
        """Save fill data to SQL using stored procedure."""
        try:
            print(f"\nProcessing fill data:")
            print(f"Data: {json.dumps(fill_data, indent=2)}")

            message = fill_data['msg']

            sql = """
            EXEC dbo.sp_InsertFeed_Fill 
                @market_id = ?,
                @sid = ?,
                @trade_id = ?,
                @order_id = ?,
                @market_ticker = ?,
                @is_taker = ?,
                @yes_price = ?,
                @no_price = ?,
                @count = ?,
                @action = ?,
                @ts = ?,
                @LoggedDate = ?
            """
            
            params = (
                message.get('market_id', '00000000-0000-0000-0000-000000000000'),
                fill_data.get('sid'),
                message['trade_id'],
                message['order_id'],
                message['market_ticker'],
                1 if message['is_taker'] else 0,  # Convert boolean to bit
                message['yes_price'],
                message['no_price'],
                message['count'],
                message['action'],
                message['ts'],
                datetime.now()
            )
            
            print(f"\nExecuting SQL with params: {params}")
            self.cursor.execute(sql, params)
            self.conn.commit()
            print("Transaction committed")

        except Exception as e:
            print(f"Error saving to SQL: {e}")
            print(f"Parameters: {params}")
            self.conn.rollback()
            print("Transaction rolled back")

    def save_lifecycle_to_sql(self, lifecycle_data):
        """Save market lifecycle data to SQL using stored procedure."""
        try:
            print(f"\nProcessing market lifecycle data:")
            print(f"Data: {json.dumps(lifecycle_data, indent=2)}")

            message = lifecycle_data['msg']

            sql = """
            EXEC dbo.sp_InsertFeed_LifeCycle
                @market_ticker = ?,
                @open_ts = ?,
                @close_ts = ?,
                @determination_ts = ?,
                @settled_ts = ?,
                @result = ?,
                @is_deactivated = ?,
                @LoggedDate = ?
            """
            
            params = (
                message['market_ticker'],
                message['open_ts'],
                message['close_ts'],
                message.get('determination_ts'),  # Optional field
                message.get('settled_ts'),        # Optional field
                message['result'],                           # Optional field
                1 if message['is_deactivated'] else 0,  # Convert boolean to bit
                datetime.now()
            )
            
            print(f"\nExecuting SQL with params: {params}")
            self.cursor.execute(sql, params)
            self.conn.commit()
            print("Transaction committed")

        except Exception as e:
            print(f"Error saving to SQL: {e}")
            print(f"Parameters: {params}")
            self.conn.rollback()
            print("Transaction rolled back")

    async def handler(self):
        """Main message handler loop."""
        while True:
            try:
                message = await self.ws.recv()
                await self.on_message(message)
            except websockets.ConnectionClosed:
                break
            except Exception as e:
                print(f"Error in handler: {e}")
                break

    async def on_message(self, message):
        """Callback for handling incoming messages."""
        try:
            # Handle heartbeat responses
            if message == "pong":
                self.last_heartbeat = time.time()
                print(message)
                return

            data = json.loads(message)
            msg_type = data.get("type")
            
            if msg_type == "orderbook_snapshot":
                if 'msg' in data:
                    print("\nProcessing orderbook snapshot")
                    self.save_orderbook_to_sql(data, "SNP")
                else:
                    print("Warning: orderbook_snapshot missing 'msg' field")
                
            elif msg_type == "orderbook_delta":
                if 'msg' in data:
                    print("\nProcessing orderbook delta")
                    self.save_orderbook_to_sql(data, "DEL")
                else:
                    print("Warning: orderbook_delta missing 'msg' field")
                
            elif msg_type == "ticker":
                if 'msg' in data:
                    print("\nProcessing ticker update")
                    self.save_ticker_to_sql(data)
                else:
                    print("Warning: ticker missing 'msg' field")
            
            elif msg_type == "trade":
                if 'msg' in data:
                    print("\nProcessing trade update")
                    self.save_trade_to_sql(data)
                else:
                    print("Warning: trade missing 'msg' field")

            elif msg_type == "fill":
                if 'msg' in data:
                    print("\nProcessing fill update")
                    self.save_fill_to_sql(data)
                else:
                    print("Warning: fill missing 'msg' field")

            elif msg_type == "market_lifecycle":
                if 'msg' in data:
                    print("\nProcessing market lifecycle update")
                    self.save_lifecycle_to_sql(data)
                else:
                    print("Warning: market_lifecycle missing 'msg' field")
            
            elif msg_type == "subscribed":
                print(f"\nSubscription confirmed:")
                print(f"Channel: {data['msg'].get('channel')}")
                print(f"Subscription ID: {data['msg'].get('sid')}")
                self.subscriptions.add(data['msg'].get('channel'))
                
            elif msg_type == "error":
                print(f"\nReceived error message:")
                print(f"Error code: {data['msg'].get('code')}")
                print(f"Error message: {data['msg'].get('msg')}")
                
            else:
                print(f"\nUnhandled message type: {msg_type}")
                print(f"Message content: {json.dumps(data, indent=2)}")
                
        except json.JSONDecodeError:
            print("Failed to parse message:", message)
        except Exception as e:
            print(f"Error in on_message: {e}")
            print(f"Message was: {message}")

    async def on_error(self, error):
        """Callback for handling errors."""
        print(f"WebSocket error: {error}")
        if self.ws and self.ws.open:
            await self.ws.close(code=1001, reason=str(error))

    async def on_close(self, close_status_code, close_msg):
        """Callback when WebSocket connection is closed."""
        print(f"WebSocket connection closed with code: {close_status_code} and message: {close_msg}")

    def __del__(self):
        """Cleanup database connections."""
        if hasattr(self, 'cursor') and self.cursor:
            self.cursor.close()
        if hasattr(self, 'conn') and self.conn:
            self.conn.close()        