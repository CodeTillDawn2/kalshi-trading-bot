import os
import pandas as pd
from typing import List, Dict
from SmokehouseModules import CandlestickData, CandlesticksLoader, MarketState

class CandlesticksLoader:
    @staticmethod
    def LoadCachedMarkets(distinctMarkets: List[str], filename: str, interval_type: int) -> tuple[List[str], pd.DataFrame]:
        # Mock implementation - replace with your actual cache loader
        uncached = [m for m in distinctMarkets if not os.path.exists(f"{filename}\\{m}_market_states.parquet")]
        return uncached, pd.DataFrame()

    @staticmethod
    def concatenate_parquet_files(file_list: List[str], output_file: str):
        # Mock implementation - replace with your actual concatenation logic
        if file_list:
            dfs = [pd.read_parquet(f) for f in file_list]
            pd.concat(dfs).to_parquet(output_file)

class MarketDataProcessor:
    def __init__(self, use_cached: bool = True, save_cached: bool = True):
        self.base_dir = r"..\..\TestingOutput\CachedMarketData"
        self.use_cached = use_cached
        self.save_cached = save_cached
        self.filename_prefix = self.base_dir
        os.makedirs(self.base_dir, exist_ok=True)

    def process_markets(self, top_n: int = 1000) -> tuple[Dict[str, MarketState], Dict[str, pd.DataFrame]]:
        market_states = {}
        filled_dfs = {}
        
        print("🚀 Fetching market list...")
        distinct_markets = CandlestickData.get_distinct_markets_with_candlesticks(
            Return_Top=top_n, only_closed=True
        )

        # Handle caching
        if self.use_cached:
            uncached_markets, _ = CandlesticksLoader.LoadCachedMarkets(
                distinctMarkets=distinct_markets,
                filename=self.filename_prefix,
                interval_type=1
            )
        else:
            uncached_markets = distinct_markets.copy()

        # Fetch candlestick data for uncached markets
        comma_delimited_markets = ", ".join(uncached_markets)
        candlestick_data = []
        if comma_delimited_markets:
            print(f"🔍 Querying sql for markets: {comma_delimited_markets}")
            candlestick_data = CandlestickData.get_candlestick_data(
                interval_type=1,
                market_tickers=comma_delimited_markets
            )

        # Process market states
        for market in distinct_markets:
            parquet_file = f"{self.filename_prefix}\\{market}_market_states.parquet"
            if self.use_cached and os.path.exists(parquet_file):
                print(f"📂 Loading cached market states for {market}")
                market_states[market] = MarketState.load_from_parquet(parquet_file)
            else:
                print(f"🔍 Processing market states: {market}")
                market_candles = [c for c in candlestick_data if c.market_ticker == market]
                if market_candles or market not in uncached_markets:
                    market_states[market] = MarketState.from_candlestick_list(market_candles)
                    if self.save_cached:
                        MarketState.save_to_parquet(market_states[market], parquet_file)

        # Process candlestick dataframes
        for market in distinct_markets:
            json_file = f"{self.filename_prefix}\\{market}_candlesticks.json"
            if self.use_cached and os.path.exists(json_file):
                print(f"📂 Loading cached candlesticks for {market}")
                filled_dfs[market] = pd.read_json(json_file)
            else:
                print(f"🔍 Processing candlesticks: {market}")
                market_candles = [c for c in candlestick_data if c.market_ticker == market]
                if market_candles or market not in uncached_markets:
                    filled_dfs[market] = CandlestickData.forward_fill_and_return_df(
                        candlesticks=candlestick_data,
                        market_ticker=market,
                        interval_type=1
                    )
                    if self.save_cached:
                        df_to_save = filled_dfs[market].reset_index().rename(columns={'Date': 'Date'})
                        numeric_cols = ['Open', 'High', 'Low', 'Close', 'Volume', 'Open_Interest']
                        for col in numeric_cols:
                            if col in df_to_save.columns and df_to_save[col].dropna().apply(lambda x: x.is_integer()).all():
                                df_to_save[col] = df_to_save[col].astype(int)
                        df_to_save.to_json(json_file, orient="records")

        # Combine market states
        file_list = [f"{self.filename_prefix}\\{market}_market_states.parquet" 
                    for market in distinct_markets 
                    if os.path.exists(f"{self.filename_prefix}\\{market}_market_states.parquet")]
        if file_list:
            output_file = f"{self.filename_prefix}\\combined_market_states.parquet"
            CandlesticksLoader.concatenate_parquet_files(file_list, output_file)

        return market_states, filled_dfs

def main():
    processor = MarketDataProcessor(use_cached=True, save_cached=True)
    market_states, filled_dfs = processor.process_markets(top_n=5)
    
    print("\nResults:")
    print(f"Processed {len(market_states)} market states")
    print(f"Processed {len(filled_dfs)} candlestick datasets")

if __name__ == "__main__":
    main()