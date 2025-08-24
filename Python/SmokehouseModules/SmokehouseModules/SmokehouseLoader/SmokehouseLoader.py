import os
import pandas as pd
from typing import List

class CandlesticksLoader:


    @staticmethod
    def LoadCachedMarkets(distinctMarkets, filename, interval_type=1):
        loaded_dataframes = {}
        dataNeededMarkets = distinctMarkets.copy()
        freq_map = {1: 'min', 2: 'h', 3: 'd'}
        freq = freq_map.get(interval_type, 'min')
        
        for market in distinctMarkets:
            fullFileName = rf"{filename}\{market}_candlesticks.json"
            if os.path.exists(fullFileName):
                # Load with explicit dtypes where possible
                df = pd.read_json(fullFileName, dtype={
                    'Open': int, 'High': int, 'Low': int, 'Close': int, 'Volume': int,
                    'Market': str
                })
                
                # Handle 'index' or 'Date'
                if 'index' in df.columns:
                    df['Date'] = pd.to_datetime(df['index'], unit='ms')
                    df = df.drop(columns=['index'])
                    df.set_index('Date', inplace=True)
                elif 'Date' in df.columns:
                    df['Date'] = pd.to_datetime(df['Date'], unit='ms')
                    df.set_index('Date', inplace=True)
                else:
                    print(f"⚠️ Error: No 'Date' or 'index' column in {fullFileName}. Cannot set datetime index.")
                    continue
                
                # Set frequency if regular
                try:
                    df.index.freq = pd.tseries.frequencies.to_offset(freq)
                except ValueError:
                    print(f"⚠️ Warning: Index for {market} is not regularly spaced at {freq}. Frequency not set.")
                
                loaded_dataframes[market] = df
                dataNeededMarkets.remove(market)
        return dataNeededMarkets, loaded_dataframes
    

    @staticmethod
    def concatenate_parquet_files(file_list: List[str], output_filename: str):
        """
        Concatenate multiple Parquet files into a single Parquet file.

        :param file_list: List of paths to Parquet files to concatenate.
        :param output_filename: Path to the output Parquet file.
        """
        import pandas as pd
        
        combined_df = pd.DataFrame()
        for file in file_list:
            df = pd.read_parquet(file, engine="pyarrow")
            combined_df = pd.concat([combined_df, df], ignore_index=False)
        
        combined_df.to_parquet(output_filename, index=True, engine="pyarrow")