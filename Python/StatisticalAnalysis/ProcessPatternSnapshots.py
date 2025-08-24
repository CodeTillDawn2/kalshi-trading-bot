import os
import json
import pandas as pd
import logging
import shutil
import random
from SmokehouseModules import generate_chart, get_unique_filename, generate_full_market_chart
import numpy as np
from scipy.stats import chi2

logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

# Utility Functions
def clean_directory(directory):
    if os.path.exists(directory):
        try:
            shutil.rmtree(directory)
            logger.info(f"🧹 Cleaned existing directory: {directory}")
        except Exception as e:
            logger.error(f"⚠️ Failed to clean directory {directory}: {e}")
            raise
    os.makedirs(directory)
    logger.info(f"📁 Created fresh directory: {directory}")

def filter_occurrence_lite(occurrence, market_name):
    required_fields = ["PatternName", "Timestamp", "Candles", "LookbackCandles"]
    for field in required_fields:
        if field not in occurrence:
            logger.warning(f"⚠️ Missing field '{field}' in occurrence for market '{market_name}' at timestamp '{occurrence.get('Timestamp', 'unknown')}'")
    
    # Relabel existing OHLC as Ask and include Bid variants
    def relabel_candles(candles):
        return [{
            "Timestamp": c.get("Timestamp", c.get("Timestamp")),
            "AskOpen": c.get("AskOpen", c.get("AskOpen")),
            "AskHigh": c.get("AskHigh", c.get("AskHigh")),
            "AskLow": c.get("AskLow", c.get("AskLow")),
            "AskClose": c.get("AskClose", c.get("AskClose")),
            "BidOpen": c.get("BidOpen", c.get("BidOpen")),
            "BidHigh": c.get("BidHigh", c.get("BidHigh")),
            "BidLow": c.get("BidLow", c.get("BidLow")),
            "BidClose": c.get("BidClose", c.get("BidClose")),
        } for c in candles]
    
    return {
        "PatternName": occurrence["PatternName"],
        "Timestamp": occurrence["Timestamp"],
        "Candles": relabel_candles(occurrence["Candles"]),
        "LookbackCandles": relabel_candles(occurrence["LookbackCandles"]),
        "MarketName": market_name
    }

def filter_occurrence_grouped(occurrence, market_name):
    required_fields = ["PatternName", "Timestamp", "Candles", "LookbackCandles"]
    for field in required_fields:
        if field not in occurrence:
            logger.warning(f"⚠️ Missing field '{field}' in occurrence for market '{market_name}' at timestamp '{occurrence.get('Timestamp', 'unknown')}'")
    
    if market_name.endswith('_Daily'):
        lookback_limit = 5
    elif market_name.endswith('_Hourly'):
        lookback_limit = 5
    else:
        lookback_limit = 15
    
    def calculate_mid_candles(candles):
        return [{
            "Timestamp": c.get("Timestamp"),
            "MidOpen": (c.get("AskOpen", 0) + c.get("BidOpen", 0)) / 2.0,
            "MidHigh": (c.get("AskHigh", 0) + c.get("BidHigh", 0)) / 2.0,
            "MidLow": (c.get("AskLow", 0) + c.get("BidLow", 0)) / 2.0,
            "MidClose": (c.get("AskClose", 0) + c.get("BidClose", 0)) / 2.0
        } for c in candles]
    
    lookback_candles = calculate_mid_candles(occurrence["LookbackCandles"])[-lookback_limit:] if occurrence["LookbackCandles"] else []
    
    return {
        "PatternName": occurrence["PatternName"],
        "Timestamp": occurrence["Timestamp"],
        "Candles": calculate_mid_candles(occurrence["Candles"]),
        "LookbackCandles": lookback_candles,
        "MarketName": market_name
    }

def get_timeframe_folder(market_name):
    if market_name.endswith('_Hourly'): return 'Hour'
    if market_name.endswith('_Daily'): return 'Day'
    return 'Minute'

# Step 1: Collect all patterns and generate CSV
def collect_all_patterns(input_dir, output_dir, market_name=None):
    clean_directory(output_dir)
    pattern_files = [f for f in os.listdir(input_dir) if f.endswith("_patterns.json")]
    if market_name:
        pattern_files = [f for f in pattern_files if f.startswith(market_name + "_")]
    
    if not pattern_files:
        logger.warning(f"No pattern files found in {input_dir}{' for market ' + market_name if market_name else ''}")
        return

    all_occurrences = {}
    for pattern_file in pattern_files:
        current_market_name = pattern_file.replace("_patterns.json", "")
        with open(os.path.join(input_dir, pattern_file), 'r') as f:
            patterns_data = json.load(f)
        for occ in patterns_data:
            pattern_name = occ['PatternName']
            # Apply filtering to include full Ask/Bid OHLC
            filtered_occ = filter_occurrence_lite(occ, current_market_name)
            if pattern_name not in all_occurrences:
                all_occurrences[pattern_name] = []
            all_occurrences[pattern_name].append(filtered_occ)

    # Save all occurrences
    for pattern_name, occurrences in all_occurrences.items():
        output_path = os.path.join(output_dir, f"{pattern_name}_all_occurrences.json")
        with open(output_path, 'w') as f:
            json.dump(occurrences, f, indent=4)
        logger.info(f"✅ Saved {len(occurrences)} occurrences to {output_path}")

    # Generate CSV
    pattern_counts = {pattern: len(occs) for pattern, occs in all_occurrences.items()}
    df = pd.DataFrame(list(pattern_counts.items()), columns=['PatternName', 'Occurrences'])
    csv_path = os.path.join(output_dir, "pattern_counts.csv")
    df.to_csv(csv_path, index=False)
    logger.info(f"✅ Saved pattern counts to {csv_path}")

# Step 2: Extract 50 random occurrences per pattern
def extract_random_50(input_dir, output_dir):
    clean_directory(output_dir)
    for pattern_file in os.listdir(input_dir):
        if not pattern_file.endswith("_all_occurrences.json"):
            continue
        pattern_name = pattern_file.replace("_all_occurrences.json", "")
        with open(os.path.join(input_dir, pattern_file), 'r') as f:
            occurrences = json.load(f)
        
        if not occurrences:
            continue
        
        sample_size = min(50, len(occurrences))
        random_50 = random.sample(occurrences, sample_size)
        filtered_random_50 = [filter_occurrence_grouped(occ, occ['MarketName']) for occ in random_50]
        
        output_path = os.path.join(output_dir, f"{pattern_name}_random_{sample_size}.json")
        with open(output_path, 'w') as f:
            json.dump(filtered_random_50, f, indent=4)
        logger.info(f"✅ Saved {len(filtered_random_50)} random occurrences to {output_path}")

# Step 3: Generate pattern snapshots
def generate_pattern_snapshots(input_dir, output_dir, num_images=5):
    clean_directory(output_dir)
    for timeframe in ['Minute', 'Hour', 'Day']:
        os.makedirs(os.path.join(output_dir, timeframe), exist_ok=True)
    
    for pattern_file in os.listdir(input_dir):
        if not pattern_file.endswith("_random_50.json"):
            continue
        pattern_name = pattern_file.replace("_random_50.json", "")
        with open(os.path.join(input_dir, pattern_file), 'r') as f:
            occurrences = json.load(f)
        
        selected = random.sample(occurrences, min(num_images, len(occurrences)))
        for idx, occ in enumerate(selected):
            market_name = occ['MarketName']
            timeframe_folder = get_timeframe_folder(market_name)
            snapshot_subdir = os.path.join(output_dir, timeframe_folder, pattern_name)
            os.makedirs(snapshot_subdir, exist_ok=True)
            
            fig = generate_chart(
                candles=occ['Candles'],
                lookback_candles=occ['LookbackCandles'],
                lookforward_candles=[],
                pattern_name=pattern_name,
                market_name=market_name,
                with_volume=True,
                with_legend=False,
                highlight_pattern=True,
                annotate_patterns=True
            )
            snapshot_base = f"{pattern_name}_{idx}"
            snapshot_path = get_unique_filename(snapshot_subdir, snapshot_base, ".png")
            fig.write_image(snapshot_path, width=1280, height=720)
            logger.info(f"✅ Generated snapshot: {snapshot_path}")
            
            json_path = os.path.splitext(snapshot_path)[0] + ".json"
            with open(json_path, 'w') as f:
                json.dump(occ, f, indent=4)

# Step 4: Generate full market images
def generate_full_market_images(input_dir, output_dir, market_name=None):
    clean_directory(output_dir)
    parquet_dir = os.path.abspath(os.path.join(input_dir, "../CachedMarketData/"))
    pattern_files = [f for f in os.listdir(input_dir) if f.endswith("_patterns.json")]
    if market_name:
        pattern_files = [f for f in pattern_files if f.startswith(market_name + "_")]
    
    for pattern_file in pattern_files:
        market_name = pattern_file.replace("_patterns.json", "")
        parquet_file = os.path.join(parquet_dir, f"{market_name}_MarketStates.parquet")
        if os.path.exists(parquet_file):
            with open(os.path.join(input_dir, pattern_file), 'r') as f:
                patterns_data = json.load(f)
            generate_full_market_chart(
                parquet_file=parquet_file,
                patterns_data=patterns_data,
                market_name=market_name,
                output_dir=output_dir
            )
            logger.info(f"✅ Generated full market chart for {market_name}")


logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

# Utility Functions (unchanged)
def clean_directory(directory):
    if os.path.exists(directory):
        try:
            shutil.rmtree(directory)
            logger.info(f"🧹 Cleaned existing directory: {directory}")
        except Exception as e:
            logger.error(f"⚠️ Failed to clean directory {directory}: {e}")
            raise
    os.makedirs(directory)
    logger.info(f"📁 Created fresh directory: {directory}")

def filter_occurrence_grouped(occurrence, market_name):
    required_fields = ["PatternName", "Timestamp", "Candles", "LookbackCandles"]
    for field in required_fields:
        if field not in occurrence:
            logger.warning(f"⚠️ Missing field '{field}' in occurrence for market '{market_name}' at timestamp '{occurrence.get('Timestamp', 'unknown')}'")
    
    # Set lookback limits based on timeframe
    if market_name.endswith('_Daily'):
        lookback_limit = 5
    elif market_name.endswith('_Hourly'):
        lookback_limit = 5
    else:  # Minute timeframe
        lookback_limit = 15
    
    # Relabel existing OHLC as Ask and include Bid variants
    def relabel_candles(candles):
        return [{
            "Timestamp": c.get("Timestamp"),
            "MidOpen": (c.get("AskOpen") + c.get("BidOpen")) / 2.0,
            "MidHigh": (c.get("AskHigh") + c.get("BidHigh")) / 2.0,
            "MidLow": (c.get("AskLow") + c.get("BidLow")) / 2.0,
            "MidClose": (c.get("AskClose") + c.get("BidClose")) / 2.0
        } for c in candles]
    
    lookback_candles = relabel_candles(occurrence["LookbackCandles"])[-lookback_limit:] if occurrence["LookbackCandles"] else []
    
    return {
        "PatternName": occurrence["PatternName"],
        "Timestamp": occurrence["Timestamp"],
        "Candles": relabel_candles(occurrence["Candles"]),
        "LookbackCandles": lookback_candles,
        "MarketName": market_name
    }

def get_timeframe_folder(market_name):
    if market_name.endswith('_Hourly'): return 'Hour'
    if market_name.endswith('_Daily'): return 'Day'
    return 'Minute'

def extract_pattern_features(candles):
    if not candles:
        return [0] * 10  # Adjust based on expected feature count

    prices = [(c['MidOpen'], c['MidClose'], c['MidHigh'], c['MidLow']) for c in candles]
    pattern_range = max(c[2] for c in prices) - min(c[3] for c in prices) or 1

    features = []
    # Normalized candle features
    for i, (o, c, h, l) in enumerate(prices):
        body = (c - o) / pattern_range
        upper_wick = (h - max(o, c)) / pattern_range
        lower_wick = (min(o, c) - l) / pattern_range
        gap = (o - prices[i-1][1]) / pattern_range if i > 0 else 0
        features.extend([body, upper_wick, lower_wick, gap])

    # Overlap ratios
    epsilon = 1e-6  # Small constant to avoid division by zero
    for i in range(1, len(candles)):
        high_i, low_i = prices[i][2], prices[i][3]
        high_prev, low_prev = prices[i-1][2], prices[i-1][3]
        numerator = min(high_i, high_prev) - max(low_i, low_prev)
        denominator = max(high_i - low_i, high_prev - low_prev)
        if denominator <= 0:
            overlap = 0.0  # No meaningful overlap if one or both ranges are zero
        else:
            overlap = numerator / (denominator + epsilon)  # Add epsilon to denominator
        features.append(min(max(overlap, -1.0), 1.0))  # Clamp to [-1, 1] range

    # Pattern volatility and consistency
    total_range = max(c[2] for c in prices) - min(c[3] for c in prices)
    body_range = max(c[1] for c in prices) - min(c[0] for c in prices)
    candle_ranges = [c[2] - c[3] for c in prices]
    range_std = np.std(candle_ranges) if len(candle_ranges) > 1 else 0
    features.extend([total_range, body_range, range_std])

    return features



# Adjust extract_lookback_features to return only requested features
def extract_lookback_features(lookback_candles, lookback_limit):
    if not lookback_candles or len(lookback_candles) < 1:
        return [0.0] * 4  # Only 4 features now

    valid_lookback = min(lookback_limit, len(lookback_candles))
    candles = lookback_candles[-valid_lookback:]
    closes = [c['MidClose'] for c in candles]

    # Volatility measures
    lookback_volatility = np.std(closes) if len(closes) > 1 else 0.0
    lookback_range = np.mean([c['MidHigh'] - c['MidLow'] for c in candles])

    # Trend measures
    total_change = sum((c['MidClose'] - c['MidOpen']) for c in candles)
    mean_trend = total_change / valid_lookback if valid_lookback > 0 else 0.0

    trend_count = sum(1 if c['MidClose'] < c['MidOpen'] else -1 for c in candles)
    trend_consistency = trend_count / valid_lookback if valid_lookback > 0 else 0.0

    return [lookback_volatility, lookback_range, mean_trend, trend_consistency]


def compute_mahalanobis_distances(feature_matrix):
    mean = np.mean(feature_matrix, axis=0)
    standardized = feature_matrix - mean
    cov_matrix = np.cov(standardized, rowvar=False)
    try:
        inv_cov_matrix = np.linalg.inv(cov_matrix)
    except np.linalg.LinAlgError:
        logger.warning("Covariance matrix is singular; adding jitter.")
        cov_matrix += np.eye(cov_matrix.shape[0]) * 1e-6
        inv_cov_matrix = np.linalg.inv(cov_matrix)
    
    diff = standardized
    distances = np.sqrt(np.sum(diff @ inv_cov_matrix * diff, axis=1))
    return distances

def identify_outliers(input_dir, output_dir):
    clean_directory(output_dir)
    timeframe_limits = {'Minute': 15, 'Hour': 5, 'Day': 5}
    batch_size = 20
    
    # Define trend-independent patterns where lookback metrics should be zeroed
    trend_independent_patterns = {
        "ClosingMarubozu", "Doji", "DragonflyDoji", "GravestoneDoji",
        "Hammer", "HangingMan", "HighWaveCandle", "InvertedHammer", "LongLeggedDoji",
        "LongLineCandle", "Marubozu", "RickshawMan", "ShootingStar", "ShortLineCandle",
        "SpinningTop", "Takuri", "Kicking", "KickingByLength", "Tristar"
    }
    
    for pattern_file in os.listdir(input_dir):
        if not pattern_file.endswith("_all_occurrences.json"):
            continue
        pattern_name = pattern_file.replace("_all_occurrences.json", "")
        with open(os.path.join(input_dir, pattern_file), 'r') as f:
            occurrences = json.load(f)
        
        if not occurrences:
            continue
        
        timeframe_groups = {}
        for occ in occurrences:
            timeframe = get_timeframe_folder(occ['MarketName'])
            timeframe_groups.setdefault(timeframe, []).append(occ)
        
        pattern_subfolder = os.path.join(output_dir, pattern_name)
        os.makedirs(pattern_subfolder, exist_ok=True)
        
        for timeframe, occ_group in timeframe_groups.items():
            max_candles = timeframe_limits[timeframe]
            unique_configs = {}
            unique_outliers = {}
            feature_matrix = []
            occurrence_indices = []
            
            max_pattern_length = max(len(occ['Candles']) for occ in occ_group)
            feature_len_per_candle = 4
            total_pattern_features = max_pattern_length * feature_len_per_candle
            
            for idx, occ in enumerate(occ_group):
                candles = occ['Candles']
                pattern_length = len(candles)
                
                def normalize_pattern(candle_list):
                    if not candle_list:
                        return tuple([(0, 0, 0, 0)] * len(candle_list))
                    prices = [
                        (
                            (c['AskOpen'] + c['BidOpen']) / 2.0,
                            (c['AskClose'] + c['BidClose']) / 2.0,
                            (c['AskHigh'] + c['BidHigh']) / 2.0,
                            (c['AskLow'] + c['BidLow']) / 2.0
                        ) for c in candle_list
                    ]
                    price_range = max(c[2] for c in prices) - min(c[3] for c in prices) or 1
                    normalized = []
                    for i, (o, c, h, l) in enumerate(prices):
                        body = (c - o) / price_range
                        upper_wick = (h - max(o, c)) / price_range
                        lower_wick = (min(o, c) - l) / price_range
                        gap = (o - (prices[i-1][1] if i > 0 else o)) / price_range if c > 0 else 0
                        normalized.append((body, upper_wick, lower_wick, gap))
                    return tuple(normalized)
                
                pattern_sig = normalize_pattern(candles)
                
                filtered_occ = filter_occurrence_grouped(occ, occ['MarketName'])
                lookback_features = extract_lookback_features(filtered_occ['LookbackCandles'], max_candles)
                # Zero out trend_sig for trend-independent patterns using a "starts with" check
                pattern_base_name = filtered_occ['PatternName']  # Use the occurrence's PatternName
                if any(pattern_base_name.startswith(base) for base in trend_independent_patterns):
                    trend_sig = (0, 0, 0, 0)
                else:
                    trend_sig = tuple(round(f, 1) for f in lookback_features[:4])  # Round trend metrics to 1 decimal
                
                config_key = (pattern_length, pattern_sig, trend_sig)
                
                if config_key not in unique_configs:
                    unique_configs[config_key] = filtered_occ
                
                pattern_features = extract_pattern_features(filtered_occ['Candles'])
                features = pattern_features + lookback_features
                feature_matrix.append(features)
                occurrence_indices.append(idx)
            
            feature_matrix = np.array(feature_matrix)
            
            if feature_matrix.shape[0] > 1:
                distances = compute_mahalanobis_distances(feature_matrix)
                threshold = np.sqrt(chi2.ppf(0.95, feature_matrix.shape[1]))
                outliers = distances > threshold
                logger.info(f"Found {np.sum(outliers)} outliers in {pattern_name} ({timeframe})")
            else:
                distances = np.array([0.0])
                outliers = np.array([False])
                logger.info(f"Insufficient data for outlier detection in {pattern_name} ({timeframe})")
            
            for idx, dist, is_outlier in zip(occurrence_indices, distances, outliers):
                occ_group[idx]['MahalanobisDistance'] = float(dist)
                occ_group[idx]['IsOutlier'] = bool(is_outlier)
                if is_outlier:
                    occ = occ_group[idx]
                    pattern_sig = normalize_pattern(occ['Candles'])
                    lookback_features = extract_lookback_features(
                        filter_occurrence_grouped(occ, occ['MarketName'])['LookbackCandles'], 
                        max_candles
                    )
                    pattern_base_name = occ['PatternName']  # Use the occurrence's PatternName
                    if any(pattern_base_name.startswith(base) for base in trend_independent_patterns):
                        trend_sig = (0, 0, 0, 0)
                    else:
                        trend_sig = tuple(round(f, 1) for f in lookback_features[:4])  # Round trend metrics to 1 decimal
                    outlier_key = (len(occ['Candles']), pattern_sig, trend_sig)
                    if outlier_key not in unique_outliers:
                        unique_outliers[outlier_key] = occ
            
            unique_list = list(unique_configs.values())
            total_configs = len(unique_list)
            logger.info(f"Found {total_configs} unique configurations for {pattern_name} ({timeframe})")
            
            for batch_idx, start_idx in enumerate(range(0, total_configs, batch_size)):
                batch = unique_list[start_idx:start_idx + batch_size]
                batch_num = batch_idx + 1
                output_filename = f"{pattern_name}_{timeframe}_unique_batch_{batch_num}.json"
                output_path = os.path.join(pattern_subfolder, output_filename)
                with open(output_path, 'w') as f:
                    json.dump(batch, f, indent=4)
                logger.info(f"✅ Saved {len(batch)} unique configurations to {output_path} (Batch {batch_num})")
            
            for occ in occ_group:
                occ['Candles'] = to_mid_candles(occ['Candles'])
                occ['LookbackCandles'] = to_mid_candles(occ['LookbackCandles'])

            all_output_path = os.path.join(pattern_subfolder, f"{pattern_name}_{timeframe}_all_with_scores.json")
            with open(all_output_path, 'w') as f:
                json.dump(occ_group, f, indent=4)
            logger.info(f"✅ Saved {len(occ_group)} occurrences with scores to {all_output_path}")
            
            df = pd.DataFrame({
                'Timestamp': [occ['Timestamp'] for occ in occ_group],
                'MahalanobisDistance': [occ['MahalanobisDistance'] for occ in occ_group],
                'IsOutlier': [occ['IsOutlier'] for occ in occ_group]
            })
            csv_path = os.path.join(pattern_subfolder, f"{pattern_name}_{timeframe}_outlier_summary.csv")
            df.to_csv(csv_path, index=False)
            logger.info(f"✅ Saved outlier summary to {csv_path}")
            
            outliers_list = list(unique_outliers.values())
            total_outliers = len(outliers_list)
            logger.info(f"Found {total_outliers} unique outliers for {pattern_name} ({timeframe})")
            
            if total_outliers > 0:
                for batch_idx, start_idx in enumerate(range(0, total_outliers, batch_size)):
                    batch = outliers_list[start_idx:start_idx + batch_size]
                    batch_num = batch_idx + 1
                    output_filename = f"{pattern_name}_{timeframe}_outliers_batch_{batch_num}.json"
                    output_path = os.path.join(pattern_subfolder, output_filename)
                    with open(output_path, 'w') as f:
                        json.dump(batch, f, indent=4)
                    logger.info(f"✅ Saved {len(batch)} unique outliers to {output_path} (Batch {batch_num})")
            else:
                logger.info(f"No unique outliers to batch for {pattern_name} ({timeframe})")

def to_mid_candles(candles):
    return [{
        "Timestamp": c.get("Timestamp"),
        "MidOpen": (c.get("AskOpen", 0) + c.get("BidOpen", 0)) / 2.0,
        "MidHigh": (c.get("AskHigh", 0) + c.get("BidHigh", 0)) / 2.0,
        "MidLow": (c.get("AskLow", 0) + c.get("BidLow", 0)) / 2.0,
        "MidClose": (c.get("AskClose", 0) + c.get("BidClose", 0)) / 2.0
    } for c in candles]

if __name__ == "__main__":
    base_dir = os.path.dirname(os.path.abspath(__file__))
    input_dir = os.path.join(base_dir, "../../TestingOutput/PatternResults/")
    unique_configs_dir = os.path.join(base_dir, "../../TestingOutput/UniqueConfigs/")
    all_patterns_dir = os.path.join(base_dir, "../../TestingOutput/AllPatterns/")
    random_50_dir = os.path.join(base_dir, "../../TestingOutput/Random50/")
    snapshots_dir = os.path.join(base_dir, "../../TestingOutput/PatternSnapshots/")
    full_charts_dir = os.path.join(base_dir, "../../TestingOutput/FullMarketCharts/")

    # Execute steps separately
    collect_all_patterns(input_dir, all_patterns_dir)
    identify_outliers(all_patterns_dir, unique_configs_dir)
    #extract_random_50(all_patterns_dir, random_50_dir)
    #generate_pattern_snapshots(random_50_dir, snapshots_dir, num_images=30)
    #generate_full_market_images(input_dir, full_charts_dir)