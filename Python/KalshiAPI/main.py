#Executes clients.py to open Kalshi API websocket connection. Channel selection in .env file for manual execution.

import os
import argparse
from dotenv import load_dotenv
from cryptography.hazmat.primitives import serialization
import asyncio
from clients import KalshiHttpClient, KalshiWebSocketClient, Environment

# Load environment variables
load_dotenv()
env = Environment.PROD  # Hardcoded environment
KEYID = os.getenv('DEMO_KEYID') if env == Environment.DEMO else os.getenv('PROD_KEYID')
KEYFILE = os.getenv('DEMO_KEYFILE') if env == Environment.DEMO else os.getenv('PROD_KEYFILE')

# Set up argument parser
parser = argparse.ArgumentParser(description="WebSocket Client for Kalshi API")
parser.add_argument(
    '--action',
    type=str,
    choices=['orderbook', 'ticker', 'trade', 'fill', 'lifecycle'],
    help="Specify the action to perform: 'orderbook', 'ticker', 'trade', 'fill', or 'lifecycle'.",
    required=False
)
parser.add_argument(
    '--market-tickers',
    nargs='+',
    help='List of market tickers (required for orderbook, optional for ticker and trade feeds)',
    required=False
)
args = parser.parse_args()

# Set action from args or environment variable
ACTION = args.action if args.action else os.getenv('Action')

# Validate that market_tickers is provided when action is orderbook
if ACTION == 'orderbook' and not args.market_tickers:
    parser.error("--market-tickers is required when action is 'orderbook'")

try:
    with open(KEYFILE, "rb") as key_file:
        private_key = serialization.load_pem_private_key(
            key_file.read(),
            password=None  # Provide the password if your key is encrypted
        )
except FileNotFoundError:
    raise FileNotFoundError(f"Private key file not found at {KEYFILE}")
except Exception as e:
    raise Exception(f"Error loading private key: {str(e)}")

# Initialize the HTTP client
http_client = KalshiHttpClient(
    key_id=KEYID,
    action=ACTION,
    private_key=private_key,
    environment=env
)

# Get account balance
balance = http_client.get_balance()
print("Balance:", balance)

# Initialize the WebSocket client
ws_client = KalshiWebSocketClient(
    key_id=KEYID,
    action=ACTION,
    private_key=private_key,
    environment=env,
    market_tickers=args.market_tickers
)

# Connect via WebSocket
asyncio.run(ws_client.connect())