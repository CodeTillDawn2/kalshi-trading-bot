import os
import configparser
import pandas as pd
import numpy as np
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import OneHotEncoder
import xgboost as xgb
from SmokehouseModules import CandlestickData, PatternDetector, SmokehousePattern, SmokehouseDebug, CandlesticksLoader, MarketState
from datetime import datetime
import json

class PatternMLTrainer:
    def __init__(self, config_file="pattern_config.txt", save_results=True):
        """Initialize trainer, load pattern strengths & training parameters."""
        self.config = self._load_config(config_file)
        self.pattern_weights = self._load_pattern_weights()
        self.training_params = self._load_training_params()
        self.save_results = save_results  # Toggle saving results to file
        print(f"🔧 Initialized PatternMLTrainer with config: {config_file}")


    def _log(self, message):
        """Helper function to print messages with timestamps."""
        print(f"{datetime.now().strftime('%Y-%m-%d %H:%M:%S')} - {message}")

    MODEL_FILE = "pattern_model.json"

    def _load_config(self, config_file):
        """Reads configuration file."""
        config = configparser.ConfigParser()
        config.optionxform = str  # Preserve case sensitivity

        if not os.path.exists(config_file):
            print(f"⚠️ Config file {config_file} not found. Using defaults.")
            return config

        config.read(config_file)
        print(f"📄 Loaded config from {config_file}")
        return config

    def _load_pattern_weights(self):
        """Reads pattern strengths, certainty, and uncertainty from the config."""
        pattern_weights = {}

        if "Patterns" not in self.config:
            print("⚠️ No [Patterns] section found in config. Skipping pattern weights.")
            return {}

        for pattern, values in self.config["Patterns"].items():
            try:
                strength, certainty, uncertainty = map(float, values.split(","))
                pattern_weights[pattern.strip()] = (strength, certainty, uncertainty)
            except ValueError:
                print(f"⚠️ Invalid format in [Patterns] for '{pattern}': {values}. Skipping.")

        print(f"✅ Loaded {len(pattern_weights)} pattern weights.")
        return pattern_weights

    def _load_training_params(self):
        """Reads training parameters from the config."""
        defaults = {
            "learning_rate": 0.1,
            "n_estimators": 100,
            "test_size": 0.2,
            "future_candles": 5
        }

        if "Training" not in self.config:
            print("⚠️ No [Training] section found in config. Using default parameters.")
            return defaults

        for param, default_value in defaults.items():
            try:
                defaults[param] = float(self.config["Training"].get(param, default_value))
            except ValueError:
                print(f"⚠️ Invalid value for {param} in [Training]. Using default: {default_value}")

        print("✅ Loaded training parameters.")
        return defaults

    def prepare_training_data(self, filled_dfs, detected_patterns):
        """Prepares dataset for training, linking patterns to price data across multiple markets."""
        print("📊 [INFO] Merging all market data into a single training dataset...")
        rows = []
        self.trained_patterns = set()  # ✅ Track only trained patterns

        for market_ticker, filled_df in filled_dfs.items():
            print(f"🔍 [INFO] Processing market: {market_ticker} - DataFrame size: {len(filled_df)} rows")

            filled_df = filled_df.reset_index(drop=False)  # Ensure the DataFrame has a reset index

            # Ensure 'timestamp' is a datetime object and set it as the index
            filled_df['timestamp'] = pd.to_datetime(filled_df['timestamp'], unit='s')  # Convert timestamp if needed
            filled_df.set_index('timestamp', inplace=True)

            for pattern_dict in detected_patterns.get(market_ticker, []):
                for index, pattern in pattern_dict.items():
                    pattern_name = pattern.Name.strip()

                    if pattern_name not in self.pattern_weights:
                        print(f"⚠️ Pattern '{pattern_name}' not found in config. Skipping.")
                        continue

                    self.trained_patterns.add(pattern_name)  # ✅ Track it for reporting

                    strength, certainty, uncertainty = self.pattern_weights[pattern_name]

                    if pattern.End_Index >= len(filled_df):
                        print(f"⚠️ Pattern '{pattern_name}' has End_Index out of range in market {market_ticker}. Skipping.")
                        continue

                    row = filled_df.iloc[pattern.End_Index]
                    timestamp = row.name  # Since 'timestamp' is now the index

                    rows.append({
                        "timestamp": timestamp.timestamp(),  # Store as a timestamp for later use
                        "pattern": pattern_name,
                        "strength": strength,
                        "certainty": certainty,
                        "uncertainty": uncertainty,
                        "volume": row["Volume"],
                        "price_movement": self._compute_price_movement(filled_df, pattern.End_Index)
                    })

        df = pd.DataFrame(rows)
        print(f"✅ [INFO] Merged dataset prepared with {len(df)} rows.")

        if df.empty:
            print("⚠️ No valid training data available.")
            return df

        df["timestamp"] = df["timestamp"].astype("int64")

        # One-hot encode pattern names
        encoder = OneHotEncoder(handle_unknown="ignore")
        pattern_encoded = encoder.fit_transform(df[['pattern']]).toarray()
        pattern_columns = encoder.get_feature_names_out(['pattern'])
        df_patterns = pd.DataFrame(pattern_encoded, columns=pattern_columns)

        df = df.drop(columns=["pattern"]).reset_index(drop=True)
        df = pd.concat([df, df_patterns], axis=1)

        return df



    def _compute_price_movement(self, filled_df, index):
        """Computes the maximum or minimum price movement within the lookahead window after a detected pattern."""
        
        future_window = int(self.training_params["future_candles"])
        max_index = min(index + future_window, len(filled_df))  # Ensure we don't go out of range

        initial_price = filled_df.iloc[index]["Close"]
        
        # ✅ Get max and min closing price over the future window
        future_max_price = filled_df.iloc[index:max_index]["Close"].max()
        future_min_price = filled_df.iloc[index:max_index]["Close"].min()

        # ✅ Determine movement relative to initial price
        max_movement = (future_max_price - initial_price) / initial_price  # Max upward move
        min_movement = (future_min_price - initial_price) / initial_price  # Max downward move

        # ✅ Decide which to return: Strongest absolute move
        price_movement = max_movement if abs(max_movement) > abs(min_movement) else min_movement

        return price_movement

    def train_model(self, filled_dfs, detected_patterns, load_existing_model=True):
        """Trains the model and allows loading an existing one if available."""
        MODEL_FILE = "pattern_model.json"

        if load_existing_model and os.path.exists(MODEL_FILE):
            self._log(f"📂 Loading existing model from {MODEL_FILE}...")
            model = xgb.XGBRegressor()
            model.load_model(MODEL_FILE)
            return model, None, None, None, None  # Return model without training data

        self._log("⚡ [INFO] Training a single model across all markets...")
        df = self.prepare_training_data(filled_dfs, detected_patterns)
        df.dropna(inplace=True)

        if df.empty:
            self._log("⚠️ No valid training data available.")
            return None, None, None, None, None

        X = df.drop(columns=["price_movement"], errors="ignore")
        y = df["price_movement"]

        X_train, X_test, y_train, y_test = train_test_split(
            X, y, test_size=self.training_params["test_size"], random_state=42
        )

        self._log(f"🚀 Training XGBoost model with {len(X_train)} samples.")
        model = xgb.XGBRegressor(
            n_estimators=int(self.training_params["n_estimators"]),
            learning_rate=self.training_params["learning_rate"]
        )
        model.fit(X_train, y_train)

        test_score = model.score(X_test, y_test)
        self._log(f"✅ Model trained successfully. Test R² Score: {test_score:.4f}")

        # Save the model
        model.save_model(MODEL_FILE)
        self._log(f"📂 Model saved to {MODEL_FILE}")

        return model, X_train, X_test, y_train, y_test


    def _output_updated_config(self, save_to_file=False):
        """Outputs only the trained pattern strengths while preserving the full config structure in the file.
        
        :param save_to_file: If True, writes only trained patterns to 'pattern_config.txt'. Otherwise, prints them.
        """
        print("📊 [INFO] Preparing updated pattern strengths...")

        config = configparser.ConfigParser()
        config.optionxform = str  # Preserve case sensitivity
        config.read("pattern_config.txt")

        # Ensure [Patterns] exists and update only encountered patterns
        if "Patterns" not in config:
            config.add_section("Patterns")
        
        print("\n🔍 [INFO] Updated Pattern Strengths (Only Trained Patterns):")
        for pattern in self.pattern_weights.keys():
            if pattern in config["Patterns"]:  # Ensure it was originally in the config
                strength, certainty, uncertainty = self.pattern_weights[pattern]
                formatted_value = f"{pattern}={strength:.2f},{certainty:.2f},{uncertainty:.2f}"
                if pattern in self.trained_patterns:
                    print(formatted_value)  # ✅ Print only trained patterns

                # ✅ Modify the file only for trained patterns, keep others unchanged
                config["Patterns"][pattern] = f"{strength:.2f},{certainty:.2f},{uncertainty:.2f}"

        # Ensure [Training] section remains intact
        if "Training" not in config:
            config.add_section("Training")

        for param, value in self.training_params.items():
            if param not in config["Training"]:
                config["Training"][param] = str(value)  # Ensure missing values are added

        if save_to_file:
            with open("pattern_config.txt", "w") as configfile:
                config.write(configfile)
            print("✅ [INFO] Updated trained pattern strengths saved to pattern_config.txt.")

        print("📋 [INFO] Configuration settings in file remain unchanged except for trained patterns.\n")


    def _output_model_analytics(self, model, X_train, X_test, y_train, y_test, save_to_file=False):
        """Outputs model analytics including feature importance and performance metrics.
        
        :param model: Trained XGBoost model.
        :param X_train: Training features.
        :param X_test: Testing features.
        :param y_train: Training labels.
        :param y_test: Testing labels.
        :param save_to_file: If True, saves analytics to a file. Otherwise, prints them.
        """
        print("\n📊 [INFO] Model Performance Metrics:")

        # Compute performance scores
        train_score = model.score(X_train, y_train)
        test_score = model.score(X_test, y_test)
        print(f"🔹 Train R² Score: {train_score:.4f}")
        print(f"🔹 Test R² Score: {test_score:.4f}")

        # Compute feature importance
        print("\n📊 [INFO] Feature Importance:")
        importance = model.feature_importances_
        feature_names = model.get_booster().feature_names

        feature_importance_dict = {feature_names[i]: importance[i] for i in range(len(feature_names))}
        sorted_importance = sorted(feature_importance_dict.items(), key=lambda x: x[1], reverse=True)

        for feature, importance_value in sorted_importance:
            print(f"🔹 {feature}: {importance_value:.4f}")

        if save_to_file:
            with open("model_analytics.txt", "w") as f:
                f.write(f"Train R² Score: {train_score:.4f}\n")
                f.write(f"Test R² Score: {test_score:.4f}\n\n")
                f.write("Feature Importance:\n")
                for feature, importance_value in sorted_importance:
                    f.write(f"{feature}: {importance_value:.4f}\n")
            print("✅ [INFO] Model analytics saved to model_analytics.txt.")

def load_detected_patterns(fileName):
    """Load detected patterns from JSON if the data size matches, restoring DataFrames."""
    if os.path.exists(fileName):
        newPatterns = SmokehousePattern.load_patterns_from_json(fileName)
        return newPatterns
    return None

        


if __name__ == "__main__":

    useCached = False
    saveCached = True
    cachedMarkets = []
    filled_dfs = {}
    detected_patterns = {}
    market_states = {}
    market_states2 = {}

    print("🚀 Fetching market list...")

    filenamePrefix = rf"..\..\..\..\TestingOutput\CachedMarketData"
    distinctMarkets = CandlestickData.get_distinct_markets_with_candlesticks(market_tickers="KXBTC-25FEB1910-B93375")


    if useCached:
        uncachedMarkets, cachedDf = CandlesticksLoader.LoadCachedMarkets(distinctMarkets=distinctMarkets, filename=filenamePrefix, interval_type=1)
    else:
        uncachedMarkets = distinctMarkets.copy()
        

    commaDelimitedMarkets = ", ".join(uncachedMarkets)
    if commaDelimitedMarkets != "":
        print(f"🔍 Querying sql for markets: {commaDelimitedMarkets}")
        candlestick_data = CandlestickData.get_candlestick_data(interval_type=1, market_tickers=commaDelimitedMarkets)


    for market in uncachedMarkets:
        print(f"🔍 Processing market states: {market}")
        market_states[market] = MarketState.from_candlestick_list([c for c in candlestick_data if c.market_ticker == market])
        parquetFile = rf"{filenamePrefix}\{market}_market_states.parquet"
        MarketState.save_to_parquet(market_states[market], parquetFile)
        market_states2[market] = MarketState.load_from_parquet(parquetFile)
        if market_states[market] == market_states2[market]:
            print("pass")
        else:
            print("fail")

    file_list = [rf"{filenamePrefix}\{market}_market_states.parquet" for market in uncachedMarkets]
    output_file = rf"{filenamePrefix}\combined_market_states.parquet"
    CandlesticksLoader.concatenate_parquet_files(file_list, output_file)

    for market in uncachedMarkets:
        print(f"🔍 Processing uncached market: {market}")
        fileName = rf"{filenamePrefix}\{market}_candlesticks.json"
        filled_dfs[market] = CandlestickData.forward_fill_and_return_df(
            candlesticks=candlestick_data, 
            market_ticker=market, 
            interval_type=1
        )
        if saveCached:
            df_to_save = filled_dfs[market].reset_index().rename(columns={'Date': 'Date'})
            # Convert numeric columns to int where they are whole numbers
            numeric_cols = ['Open', 'High', 'Low', 'Close', 'Volume', 'Open_Interest']
            for col in numeric_cols:
                # Only convert if all values are whole numbers (no decimals)
                if df_to_save[col].dropna().apply(lambda x: x.is_integer()).all():
                    df_to_save[col] = df_to_save[col].astype(int)
            df_to_save.to_json(fileName, orient="records")

    

    if cachedDf is not None:
        for market_ticker, df in cachedDf.items():
            filled_dfs[market_ticker] = df  # Directly use the loaded DataFrame with correct index



    for market in distinctMarkets:
        print(f"🔍 Processing market patterns: {market}")
        patternsFile = rf"{filenamePrefix}\{market}_patterns.json"
        patterns = None  # Temporary variable
        if useCached:
            patterns = load_detected_patterns(fileName=patternsFile)
        if patterns is None:
            patterns = PatternDetector.detect_patterns_talib(filled_df=filled_dfs[market])
        detected_patterns[market] = patterns  # Assign to dictionary
        if saveCached:
            SmokehousePattern.save_patterns_to_json(patterns=detected_patterns[market], filename=patternsFile)
            print(f"🔍 Market patterns saved for market: {market}")


    print("⚡ Training model across all markets...")
    trainer = PatternMLTrainer(save_results=False)
    model, X_train, X_test, y_train, y_test = trainer.train_model(filled_dfs, detected_patterns, load_existing_model=False)

    # Log pattern occurrences
    pattern_counts = {}
    for ticker, patterns in detected_patterns.items():
        for pattern_dict in patterns:
            for index, pattern in pattern_dict.items():
                pattern_name = pattern.Name.strip()
                pattern_counts[pattern_name] = pattern_counts.get(pattern_name, 0) + 1

    print("\n📊 [INFO] Pattern Occurrences Across All Markets:")
    for pattern_name, count in pattern_counts.items():
        print(f"🔹 {pattern_name}: {count} occurrences")

    print("✅ All tasks completed.")
