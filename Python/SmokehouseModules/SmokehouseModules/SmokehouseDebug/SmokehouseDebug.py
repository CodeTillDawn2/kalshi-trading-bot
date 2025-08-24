from typing import List
import pandas as pd
import os
from pathlib import Path

@staticmethod
def ReportBadPattern(errorMessage, BadPattern):
    test = ""

@staticmethod
def inspect_object(obj, name="Object", depth=2):
    """ Recursively prints detailed information about an object, including type, structure, and content.

    Args:
        obj: The object to inspect.
        name (str): A label for the object being inspected.
        depth (int): The maximum recursion depth for nested structures.
    """
    print(f"\n=== DEBUG: Inspecting {name} ===")
    print(f"Type: {type(obj)}")

    if isinstance(obj, dict):
        print(f"Dictionary with {len(obj)} keys")
        if depth > 0:
            for key, value in obj.items():
                print(f"  Key: {key} ({type(key)}) -> {type(value)}")
                if depth > 1 and (isinstance(value, (dict, list, tuple, set))):
                    inspect_object(value, name=f"Key {key}", depth=depth - 1)
                else:
                    print(f"    Value: {repr(value)}")
    
    elif isinstance(obj, list):
        print(f"List with {len(obj)} elements")
        if depth > 0:
            for i, item in enumerate(obj[:5]):  # Limit to first 5 items for brevity
                print(f"  [{i}]: {type(item)}")
                if depth > 1 and isinstance(item, (dict, list, tuple, set)):
                    inspect_object(item, name=f"List[{i}]", depth=depth - 1)
                else:
                    print(f"    Value: {repr(item)}")
        if len(obj) > 5:
            print(f"  ... ({len(obj) - 5} more items)")
    
    elif isinstance(obj, tuple):
        print(f"Tuple with {len(obj)} elements")
        if depth > 0:
            for i, item in enumerate(obj):
                print(f"  ({i}): {type(item)} -> {repr(item)}")

    elif isinstance(obj, set):
        print(f"Set with {len(obj)} elements")
        if depth > 0:
            for item in list(obj)[:5]:  # Convert to list to allow indexing
                print(f"  {type(item)} -> {repr(item)}")
        if len(obj) > 5:
            print(f"  ... ({len(obj) - 5} more items)")

    else:
        print(f"Value: {repr(obj)}")

    print(f"=== END DEBUG: {name} ===\n")

@staticmethod
def compare_dataframes(df1, df2, name1="DF1", name2="DF2"):
    print(f"\n🔍 Comparing {name1} vs. {name2}\n")

    # Print first and last index values
    print(f"📝 First Index Values: {name1} -> {df1.index[0] if not df1.empty else 'None'}, {name2} -> {df2.index[0] if not df2.empty else 'None'}")
    print(f"📝 Last Index Values: {name1} -> {df1.index[-1] if not df1.empty else 'None'}, {name2} -> {df2.index[-1] if not df2.empty else 'None'}")

    # ✅ 1. Compare Shape (Number of Rows & Columns)
    print(f"📌 Shape: {name1} -> {df1.shape}, {name2} -> {df2.shape}\n")

    # ✅ 2. Compare Column Names
    columns1 = set(df1.columns)
    columns2 = set(df2.columns)
    
    if columns1 != columns2:
        print("⚠️ Column Differences:")
        print(f"🔹 Columns in {name1} but not in {name2}: {columns1 - columns2}")
        print(f"🔹 Columns in {name2} but not in {name1}: {columns2 - columns1}\n")
    else:
        print("✅ Column names match\n")

    # ✅ 3. Compare Index Differences
    index_diff1 = df1.index.difference(df2.index)
    index_diff2 = df2.index.difference(df1.index)
    
    if not index_diff1.empty or not index_diff2.empty:
        print("⚠️ Index Differences:")
        print(f"🔹 In {name1} but not in {name2}: {len(index_diff1)} rows")
        print(f"🔹 In {name2} but not in {name1}: {len(index_diff2)} rows\n")
    else:
        print("✅ Index values match\n")

    # ✅ 4. Compare Summary Statistics for common columns
    common_columns = list(columns1 & columns2)
    if common_columns:
        print(f"📊 Summary Statistics for common columns:")
        print(f"  {name1}:")
        print(df1[common_columns].describe(), "\n")
        print(f"  {name2}:")
        print(df2[common_columns].describe(), "\n")
    else:
        print("⚠️ No common columns to compare summary statistics.\n")

    # ✅ 5. Find Data Differences (Only on Matching Columns & Index)
    common_index = df1.index.intersection(df2.index)
    if common_index.empty:
        print("⚠️ No common index to compare data.\n")
    else:
        df1_common = df1.loc[common_index, common_columns]
        df2_common = df2.loc[common_index, common_columns]

        differences = df1_common.compare(df2_common)
        if differences.empty:
            print("✅ No data differences detected in common rows/columns\n")
        else:
            print("⚠️ Data Differences:")
            print(differences.head(10))  # Print first 10 differences

    # ✅ 6. Check for Extra/Missing Rows
    if len(index_diff1) > 0:
        print(f"🔹 Example rows in {name1} but missing in {name2}:")
        print(df1.loc[index_diff1].head(), "\n")

    if len(index_diff2) > 0:
        print(f"🔹 Example rows in {name2} but missing in {name1}:")
        print(df2.loc[index_diff2].head(), "\n")

    print("✅ Comparison complete!\n")

@staticmethod
def compare_market_states(state1, state2):
    """
    Compares two MarketState instances and prints differences.

    :param state1: First MarketState instance
    :param state2: Second MarketState instance
    """
    print("🔍 Comparing MarketState Instances...\n")

    # Compare Market Tickers
    if state1.market_ticker != state2.market_ticker:
        print(f"❌ Market tickers differ: {state1.market_ticker} vs {state2.market_ticker}")
    else:
        print("✅ Market tickers match.")

    # Compare Timestamps (based on the most recent candlestick's timestamp)
    last_timestamp1 = state1.all_candlesticks[-1].date if state1.timestamp else None
    last_timestamp2 = state2.all_candlesticks[-1].date if state2.timestamp else None
    if last_timestamp1 != last_timestamp2:
        print(f"❌ Timestamps differ: {last_timestamp1} vs {last_timestamp2}")
    else:
        print("✅ Timestamps match.")

    # Compare Number of Candlesticks
    if len(state1.all_candlesticks) != len(state2.all_candlesticks):
        print(f"❌ Candlestick counts differ: {len(state1.all_candlesticks)} vs {len(state2.all_candlesticks)}")
    else:
        print("✅ Candlestick counts match.")

    # Compare Patterns with Deep Check
    if not compare_patterns(state1.patterns, state2.patterns):
        print(f"❌ Detected patterns differ.")
    else:
        print("✅ Detected patterns match.")

    # ✅ Corrected Feature DataFrame Comparison
    df1 = state1.features  # Generate the feature DataFrame dynamically
    df2 = state2.features  # Same for state2

    if df1.empty and df2.empty:
        print("✅ Both feature DataFrames are empty.")
    elif df1.shape != df2.shape:
        print(f"❌ Feature DataFrames have different shapes: {df1.shape} vs {df2.shape}")
    elif not df1.equals(df2):
        print("❌ Feature DataFrames differ in values.")
        print("🔹 Differences (first few rows):")
        print(df1.compare(df2))
    else:
        print("✅ Feature DataFrames match.")
        return True
    return False

def compare_patterns(patterns1: List[dict], patterns2: List[dict]) -> bool:
    """
    Compares two lists of pattern dictionaries, checking both structure and content.
    
    :param patterns1: First list of pattern dictionaries.
    :param patterns2: Second list of pattern dictionaries.
    :return: True if patterns match, False otherwise.
    """
    if len(patterns1) != len(patterns2):
        print(f"❌ Pattern lists differ in length: {len(patterns1)} vs {len(patterns2)}")
        return False

    for i, (dict1, dict2) in enumerate(zip(patterns1, patterns2)):
        # Compare dictionary keys
        if set(dict1.keys()) != set(dict2.keys()):
            print(f"❌ Dictionary {i} has different pattern keys.")
            print(f"Keys in state1: {set(dict1.keys())}")
            print(f"Keys in state2: {set(dict2.keys())}")
            return False

        for key in dict1.keys():
            pattern1 = dict1[key]
            pattern2 = dict2[key]

            # Ensure both patterns exist
            if pattern1 is None or pattern2 is None:
                print(f"❌ Pattern key '{key}' has a None value in one of the states.")
                return False

            # Handle DataFrames inside pattern objects safely
            if hasattr(pattern1, "__dict__") and hasattr(pattern2, "__dict__"):
                pattern1_attrs = vars(pattern1)
                pattern2_attrs = vars(pattern2)

                for attr_key in pattern1_attrs:
                    value1 = pattern1_attrs[attr_key]
                    value2 = pattern2_attrs.get(attr_key, None)

                    if isinstance(value1, pd.DataFrame) and isinstance(value2, pd.DataFrame):
                        if not value1.equals(value2):
                            print(f"❌ Pattern '{key}' differs in DataFrame attribute '{attr_key}'.")
                            print("🔹 Differences:")
                            print(value1.compare(value2))
                            return False
                    elif value1 != value2:
                        print(f"❌ Pattern '{key}' attribute '{attr_key}' differs: {value1} vs {value2}")
                        return False

            else:
                if pattern1 != pattern2:
                    print(f"❌ Patterns for key '{key}' differ.")
                    print(f"State1 pattern: {pattern1}")
                    print(f"State2 pattern: {pattern2}")
                    return False

    print("✅ Patterns match.")
    return True

def compare_parquets(folder_a, folder_b):
    # Convert folder paths to Path objects for easier handling
    folder_a_path = Path(folder_a)
    folder_b_path = Path(folder_b)

    # Ensure both folders exist
    if not folder_a_path.exists() or not folder_b_path.exists():
        print(f"One or both folders do not exist: {folder_a}, {folder_b}")
        return

    # Get list of Parquet files in each folder
    files_a = {f.name for f in folder_a_path.glob("*.parquet")}
    files_b = {f.name for f in folder_b_path.glob("*.parquet")}

    # Find common files
    common_files = files_a.intersection(files_b)

    if not common_files:
        print("No common Parquet files found between the two folders.")
        return

    print(f"Found {len(common_files)} common Parquet files to compare.\n")

    # List to track non-identical files
    non_identical_files = []

    # Compare each common file
    for file_name in sorted(common_files):
        print(f"Comparing: {file_name}")
        
        # Full paths to the files
        file_a = folder_a_path / file_name
        file_b = folder_b_path / file_name

        try:
            # Read Parquet files into DataFrames
            df_a = pd.read_parquet(file_a)
            df_b = pd.read_parquet(file_b)

            # Compare column names
            cols_a = set(df_a.columns)
            cols_b = set(df_b.columns)
            if cols_a != cols_b:
                print(f"  - Column mismatch:")
                print(f"    Folder A columns: {sorted(cols_a)}")
                print(f"    Folder B columns: {sorted(cols_b)}")
                print(f"    In A but not B: {sorted(cols_a - cols_b)}")
                print(f"    In B but not A: {sorted(cols_b - cols_a)}")
                non_identical_files.append(file_name)
                print()
                continue  # Skip further comparison if columns differ

            # Compare shapes
            if df_a.shape != df_b.shape:
                print(f"  - Shape mismatch:")
                print(f"    Folder A shape: {df_a.shape}")
                print(f"    Folder B shape: {df_b.shape}")
                non_identical_files.append(file_name)
                print()
                continue  # Skip data comparison if shapes differ

            # Compare data content
            # Ensure identical sorting for comparison (if there's a logical index like timestamp)
            if 'Timestamp' in df_a.columns:
                df_a = df_a.sort_values('Timestamp').reset_index(drop=True)
                df_b = df_b.sort_values('Timestamp').reset_index(drop=True)

            # Check for exact equality
            if not df_a.equals(df_b):
                print("  - Data content differs:")
                # Find rows that differ
                diff = df_a.compare(df_b)
                if not diff.empty:
                    print("    Differences found in rows:")
                    print(diff)
                else:
                    # If equals() fails but compare() finds no diff, likely a type or NaN issue
                    print("    Possible type mismatch or NaN handling difference.")
                    # Check dtypes
                    dtypes_a = df_a.dtypes
                    dtypes_b = df_b.dtypes
                    if not dtypes_a.equals(dtypes_b):
                        print("    Dtype differences:")
                        print(f"    Folder A dtypes:\n{dtypes_a}")
                        print(f"    Folder B dtypes:\n{dtypes_b}")
                non_identical_files.append(file_name)
            else:
                print("  - Files are identical.")

        except Exception as e:
            print(f"  - Error processing {file_name}: {str(e)}")
            non_identical_files.append(file_name)  # Consider it non-identical due to error

        print()  # Blank line for readability

    # Print summary of non-identical files
    print("=== Summary of Comparison ===")
    if non_identical_files:
        print(f"Found {len(non_identical_files)} non-identical files out of {len(common_files)} compared:")
        for file in sorted(non_identical_files):
            print(f"  - {file}")
    else:
        print(f"All {len(common_files)} compared files are identical.")
    print("=============================")

def main():
    # Example folder paths (replace with your actual paths)
    folder_a = r"C:\Users\Peter\Documents\GitHub\kalshi-bot\TestingOutput\CachedMarketData"
    folder_b = r"C:\Users\Peter\Documents\GitHub\kalshi-bot\TestingOutput\CachedMarketData_old"

    print(f"Comparing Parquet files between:\n  Folder A: {folder_a}\n  Folder B: {folder_b}\n")
    compare_parquets(folder_a, folder_b)

if __name__ == "__main__":
    main()