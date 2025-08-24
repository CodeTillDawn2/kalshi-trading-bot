# KalshiDataCleaner.py

from typing import List, Dict, Optional, Tuple
import pandas as pd
import logging
from datetime import datetime
from pathlib import Path
from SmokehouseModules.CandlestickData import CandlestickData
from SmokehouseModules.MarketState import MarketState
import sys
import os
import psutil
import numpy as np
import pandas as pd

# Memory optimization settings
np.set_printoptions(threshold=1000)
pd.options.display.max_rows = 1000

# Limit numpy threads
os.environ["MKL_NUM_THREADS"] = "1"
os.environ["NUMEXPR_NUM_THREADS"] = "1"
os.environ["OMP_NUM_THREADS"] = "1"

# Basic pandas display settings
pd.set_option('display.precision', 3)
pd.set_option('display.float_format', lambda x: '%.3f' % x)

# Monitor memory usage
def get_memory_usage():
    process = psutil.Process(os.getpid())
    return process.memory_info().rss / 1024 / 1024  # in MB

# Function to replace inf values with NaN explicitly
def replace_inf_with_nan(df):
    return df.replace([np.inf, -np.inf], np.nan)

# Set numpy to use less memory
np.set_printoptions(threshold=sys.maxsize, precision=3)

class KalshiDataValidator:
    """Validates Kalshi market data including technical indicators."""
    
    def __init__(self):
        self.logger = logging.getLogger(__name__)
        
    def validate_price_range(self, price: int) -> bool:
        """Validate price is within 0-100 range and is a whole number."""
        if not isinstance(price, (int, np.integer)):
            raise ValueError(f"Price {price} is not a whole number")
        if not 0 <= price <= 100:
            raise ValueError(f"Price {price} outside valid range [0, 100]")
        return True

    def validate_technical_indicators(self, df: pd.DataFrame) -> List[str]:
        """Validate technical indicators for anomalies."""
        anomalies = []
        
        # RSI Validation (should be 0-100)
        if 'RSI' in df.columns:
            invalid_rsi = df[~df['RSI'].between(0, 100)]['RSI']
            if not invalid_rsi.empty:
                anomalies.append(f"Invalid RSI values found: {invalid_rsi.tolist()}")
        
        # MACD Validation
        if all(col in df.columns for col in ['MACD', 'MACD_signal']):
            extreme_divergence = df[abs(df['MACD'] - df['MACD_signal']) > 10]
            if not extreme_divergence.empty:
                anomalies.append(f"Extreme MACD divergence detected at: {extreme_divergence.index.tolist()}")
        
        # Moving Average Validation
        if all(col in df.columns for col in ['SMA_5', 'SMA_10']):
            invalid_cross = df[df['SMA_5'] == df['SMA_10']]
            if not invalid_cross.empty:
                anomalies.append(f"Invalid MA cross detected at: {invalid_cross.index.tolist()}")
        
        # ATR Validation
        if 'ATR' in df.columns:
            invalid_atr = df[df['ATR'] < 0]['ATR']
            if not invalid_atr.empty:
                anomalies.append(f"Negative ATR values found: {invalid_atr.tolist()}")
        
        return anomalies

class KalshiDataCleaner:
    """Cleans and validates Kalshi market data."""
    
    def __init__(self, quality_checks: Dict = None):
        self.quality_checks = quality_checks or {
            'max_missing_pct': 20,
            'max_zero_volume_pct': 80,
            'min_candlesticks': 20,
            'max_spread': 20
        }
        self.validator = KalshiDataValidator()
        self._setup_logging()
        
    def _setup_logging(self):
        log_dir = Path('logs')
        log_dir.mkdir(exist_ok=True)
        
        logging.basicConfig(
            level=logging.INFO,
            format='%(asctime)s - %(levelname)s - %(message)s',
            handlers=[
                logging.FileHandler(f'logs/data_cleaning_{datetime.now():%Y%m%d_%H%M%S}.log'),
                logging.StreamHandler()
            ]
        )
        self.logger = logging.getLogger(__name__)

    def _clean_dataframe(self, df: pd.DataFrame) -> pd.DataFrame:
        """Clean the DataFrame by removing invalid or problematic records."""
        initial_len = len(df)
        self.logger.info(f"Starting DataFrame cleaning with {initial_len} records")
        
        # Remove rows with missing values if they exceed threshold
        missing_pct = (df.isnull().sum() / len(df)) * 100
        cols_to_drop = missing_pct[missing_pct > self.quality_checks['max_missing_pct']].index
        df = df.drop(columns=cols_to_drop)
        
        # Remove rows with all missing values
        df = df.dropna(how='all')
        
        # Check volume data if present
        if 'volume' in df.columns:
            zero_volume_pct = (df['volume'] == 0).mean() * 100
            if zero_volume_pct > self.quality_checks['max_zero_volume_pct']:
                self.logger.warning(f"High percentage of zero volume: {zero_volume_pct:.2f}%")
        
        # Validate price data
        if 'close' in df.columns:
            df = df[df['close'].apply(lambda x: self.validator.validate_price_range(x))]
        
        # Check for minimum number of candlesticks per market
        market_counts = df['market_ticker'].value_counts()
        valid_markets = market_counts[market_counts >= self.quality_checks['min_candlesticks']].index
        df = df[df['market_ticker'].isin(valid_markets)]
        
        # Check spread if both bid and ask are present
        if all(col in df.columns for col in ['bid', 'ask']):
            df['spread'] = df['ask'] - df['bid']
            df = df[df['spread'] <= self.quality_checks['max_spread']]
        
        final_len = len(df)
        self.logger.info(f"Finished cleaning. Removed {initial_len - final_len} records")
        
        return df

    def clean_market_data(self, candlesticks: List[CandlestickData]) -> Tuple[pd.DataFrame, Dict]:
        """Clean and validate market data using MarketState."""
        import gc
        self.logger.info(f"Starting data cleaning for {len(candlesticks)} candlesticks")
        
        cleaning_stats = {
            'total_records': len(candlesticks),
            'invalid_records': 0,
            'anomalies': [],
            'markets_processed': set()
        }
        
        try:
            # Group candlesticks by market ticker
            market_groups = {}
            for cs in candlesticks:
                if cs.market_ticker not in market_groups:
                    market_groups[cs.market_ticker] = []
                market_groups[cs.market_ticker].append(cs)
            
            self.logger.info(f"Found {len(market_groups)} unique markets")
            
            # Process markets in chunks and store results directly to disk
            temp_dir = Path('temp_market_states')
            temp_dir.mkdir(exist_ok=True)
            
            processed_markets = 0
            total_markets = len(market_groups)
            chunk_files = []
            
            # Process markets in smaller batches
            batch_size = 10  # Further reduced batch size
            market_items = list(market_groups.items())
            
            for i in range(0, len(market_items), batch_size):
                batch = market_items[i:i + batch_size]
                
                for market_ticker, market_candlesticks in batch:
                    processed_markets += 1
                    self.logger.info(f"Processing market {market_ticker} ({processed_markets}/{total_markets})"
                                f" with {len(market_candlesticks)} candlesticks")
                    
                    try:
                        # Skip markets with too few candlesticks
                        if len(market_candlesticks) < 2:
                            self.logger.warning(f"Skipping {market_ticker}: insufficient data")
                            continue
                        
                        # Process market in sub-batches
                        sub_batch_size = 100  # Process 100 candlesticks at a time
                        market_states = []
                        
                        # Sort candlesticks by date
                        market_candlesticks = sorted(market_candlesticks, key=lambda x: x.date)
                        
                        # Process in sub-batches
                        for j in range(0, len(market_candlesticks), sub_batch_size):
                            sub_batch = market_candlesticks[j:j + sub_batch_size]
                            
                            # Very conservative lookback periods
                            max_lookback = min(3, len(sub_batch) - 1)
                            pattern_lookback = min(2, max_lookback - 1)
                            
                            sub_states = MarketState.from_candlestick_list(
                                candlesticks=sub_batch,
                                pattern_lookback=pattern_lookback,
                                lookback=max_lookback
                            )
                            
                            if sub_states:
                                market_states.extend(sub_states)
                            
                            # Force garbage collection after each sub-batch
                            gc.collect()
                        
                        # Save market states to temporary file immediately
                        if market_states:
                            chunk_file = temp_dir / f'market_states_{market_ticker}_{len(chunk_files)}.parquet'
                            chunk_df = MarketState.combine_market_states(market_states)
                            
                            # Convert to float32 immediately
                            for col in chunk_df.select_dtypes(include=['float64']).columns:
                                chunk_df[col] = chunk_df[col].astype('float32')
                            
                            chunk_df.to_parquet(chunk_file, compression='snappy')
                            chunk_files.append(chunk_file)
                            del chunk_df
                            del market_states
                            
                    except Exception as e:
                        self.logger.error(f"Error creating market states for {market_ticker}: {str(e)}")
                        continue
                    
                    # Force garbage collection after each market
                    gc.collect()
                
                # Additional garbage collection after batch
                gc.collect()
            
            # Combine all chunks with memory-efficient reading
            final_df = pd.DataFrame()
            
            for chunk_file in chunk_files:
                try:
                    chunk_df = pd.read_parquet(chunk_file)
                    
                    if final_df.empty:
                        final_df = chunk_df
                    else:
                        final_df = pd.concat([final_df, chunk_df], ignore_index=True)
                    
                    del chunk_df
                    gc.collect()
                    
                except Exception as e:
                    self.logger.error(f"Error reading chunk file {chunk_file}: {str(e)}")
                    continue
                finally:
                    # Clean up temporary file immediately
                    chunk_file.unlink()
            
            # Clean up temporary directory
            temp_dir.rmdir()
            
            if final_df.empty:
                raise ValueError("No valid data after combining market states")
            
            # Validate technical indicators
            tech_anomalies = self.validator.validate_technical_indicators(final_df)
            if tech_anomalies:
                cleaning_stats['anomalies'].extend(tech_anomalies)
            
            # Clean the data
            cleaned_df = self._clean_dataframe(final_df)
            cleaning_stats['invalid_records'] = len(final_df) - len(cleaned_df)
            cleaning_stats['markets_processed'] = set(cleaned_df['market_ticker'].unique())
            
            # Log cleaning summary
            self._log_cleaning_summary(cleaning_stats)
            
            return cleaned_df, cleaning_stats
            
        except Exception as e:
            self.logger.error(f"Error during data cleaning: {str(e)}")
            raise

    def _log_cleaning_summary(self, cleaning_stats: Dict) -> None:
        """Log summary of data cleaning process."""
        self.logger.info("=== Data Cleaning Summary ===")
        self.logger.info(f"Total records processed: {cleaning_stats['total_records']}")
        self.logger.info(f"Invalid records removed: {cleaning_stats['invalid_records']}")
        self.logger.info(f"Markets processed: {len(cleaning_stats['markets_processed'])}")
        
        if cleaning_stats['anomalies']:
            self.logger.warning("Anomalies detected:")
            for anomaly in cleaning_stats['anomalies']:
                self.logger.warning(f"- {anomaly}")
        
        # Calculate success rate
        success_rate = ((cleaning_stats['total_records'] - cleaning_stats['invalid_records']) 
                    / cleaning_stats['total_records'] * 100)
        self.logger.info(f"Data cleaning success rate: {success_rate:.2f}%")
        
        # Log market-specific information
        self.logger.info("Markets processed:")
        for market in cleaning_stats['markets_processed']:
            self.logger.info(f"- {market}")

def main():
    cleaner = KalshiDataCleaner()
    
    try:
        # Get candlestick data for all markets
        candlesticks = CandlestickData.get_candlestick_data(interval_type=1)
        
        if not candlesticks:
            raise ValueError("No candlestick data received")
            
        # Log initial data summary
        market_counts = {}
        date_ranges = {}
        for cs in candlesticks:
            market_counts[cs.market_ticker] = market_counts.get(cs.market_ticker, 0) + 1
            if cs.market_ticker not in date_ranges:
                date_ranges[cs.market_ticker] = {'min': cs.date, 'max': cs.date}
            else:
                date_ranges[cs.market_ticker]['min'] = min(date_ranges[cs.market_ticker]['min'], cs.date)
                date_ranges[cs.market_ticker]['max'] = max(date_ranges[cs.market_ticker]['max'], cs.date)
        
        print(f"\nProcessing {len(market_counts)} markets:")
        for market, count in market_counts.items():
            date_range = date_ranges[market]
            print(f"{market}: {count} candlesticks, {date_range['min']} to {date_range['max']}")
            
        # Process the data
        cleaned_data, cleaning_stats = cleaner.clean_market_data(candlesticks)
        
        # Save cleaned data
        output_file = f'cleaned_kalshi_data_{datetime.now():%Y%m%d_%H%M%S}.parquet'
        cleaned_data.to_parquet(output_file, 
                              engine='pyarrow', 
                              compression='snappy')
        
        print(f"\nCleaning completed:")
        print(f"- Input records: {cleaning_stats['total_records']}")
        print(f"- Output records: {len(cleaned_data)}")
        print(f"- Markets processed: {len(cleaning_stats['markets_processed'])}")
        print(f"- Saved to: {output_file}")
                               
    except Exception as e:
        print(f"Error in main: {str(e)}")
        raise

if __name__ == "__main__":
    main()