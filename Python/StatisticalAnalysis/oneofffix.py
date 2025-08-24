import json
import os

def get_pattern_lookback_candles(pattern_name, timestamp, all_patterns_dir):
    """Load LookbackCandles from the specific AllPatterns file for a given pattern and timestamp"""
    pattern_file = os.path.join(all_patterns_dir, f"{pattern_name}_all_occurrences.json")
    
    if not os.path.exists(pattern_file):
        print(f"Warning: Pattern file not found: {pattern_file}")
        return None
    
    with open(pattern_file, 'r', encoding='utf-8-sig') as file:
        try:
            data = json.load(file)
            for entry in data:
                if entry["Timestamp"] == timestamp:
                    return entry["LookbackCandles"]
            print(f"Warning: Timestamp {timestamp} not found in {pattern_file}")
            return None
        except json.JSONDecodeError as e:
            print(f"Error decoding JSON in {pattern_file}: {str(e)}")
            return None

def fix_or_remove_duplicate_timestamps(data, all_patterns_dir):
    """Fix entries with duplicates by replacing LookbackCandles, or remove if no replacement found"""
    fixed_data = []
    changes_made = False
    
    for entry in data:
        candle_timestamps = [candle["Timestamp"] for candle in entry["Candles"]]
        lookback_timestamps = [candle["Timestamp"] for candle in entry["LookbackCandles"]]
        
        has_duplicates = False
        
        # Check for duplicates between Candles and LookbackCandles
        duplicate_timestamps = set(candle_timestamps) & set(lookback_timestamps)
        
        if duplicate_timestamps:
            has_duplicates = True
            print(f"Found duplicates in entry at {entry['Timestamp']}: {duplicate_timestamps}")
            # Try to get replacement LookbackCandles
            new_lookback_candles = get_pattern_lookback_candles(entry["PatternName"], entry["Timestamp"], all_patterns_dir)
            if new_lookback_candles is not None:
                entry["LookbackCandles"] = new_lookback_candles
                print(f"Replaced LookbackCandles for {entry['PatternName']} at {entry['Timestamp']}")
                fixed_data.append(entry)
                changes_made = True
            else:
                print(f"Removing entry for {entry['PatternName']} at {entry['Timestamp']} - no replacement found")
                changes_made = True
                continue  # Skip adding this entry to fixed_data
        
        # Check for duplicates within LookbackCandles
        if len(lookback_timestamps) != len(set(lookback_timestamps)):
            has_duplicates = True
            print(f"Found internal duplicates in LookbackCandles for {entry['Timestamp']}")
            new_lookback_candles = get_pattern_lookback_candles(entry["PatternName"], entry["Timestamp"], all_patterns_dir)
            if new_lookback_candles is not None:
                entry["LookbackCandles"] = new_lookback_candles
                print(f"Replaced LookbackCandles due to internal duplicates for {entry['PatternName']} at {entry['Timestamp']}")
                fixed_data.append(entry)
                changes_made = True
            else:
                print(f"Removing entry for {entry['PatternName']} at {entry['Timestamp']} - no replacement found")
                changes_made = True
                continue  # Skip adding this entry to fixed_data
        
        # If no duplicates, keep the entry as is
        if not has_duplicates:
            fixed_data.append(entry)
    
    return fixed_data, changes_made

def process_false_positives(false_positives_dir, all_patterns_dir):
    total_files = 0
    total_entries = 0
    total_changes = 0
    total_removed = 0
    
    # Process FalsePositives files
    for root, dirs, files in os.walk(false_positives_dir):
        for filename in files:
            if filename.endswith('.json'):
                total_files += 1
                file_path = os.path.join(root, filename)
                
                with open(file_path, 'r', encoding='utf-8-sig') as file:
                    try:
                        data = json.load(file)
                    except json.JSONDecodeError as e:
                        print(f"Error decoding JSON in {file_path}: {str(e)}")
                        continue
                
                original_count = len(data)
                fixed_data, changes_made = fix_or_remove_duplicate_timestamps(data, all_patterns_dir)
                new_count = len(fixed_data)
                
                total_entries += original_count
                if changes_made:
                    total_changes += 1
                    total_removed += original_count - new_count
                    # Save the fixed data back to the file
                    with open(file_path, 'w', encoding='utf-8') as file:
                        json.dump(fixed_data, file, indent=2)
                    print(f"Processed and modified {file_path}:")
                    print(f"  Original entries: {original_count}")
                    print(f"  Entries after processing: {new_count}")
                    print(f"  Removed: {original_count - new_count}\n")
                else:
                    print(f"No changes needed for {file_path}")
    
    # Print summary
    print("\nProcessing Summary:")
    print(f"Total files processed: {total_files}")
    print(f"Total entries: {total_entries}")
    print(f"Files with changes: {total_changes}")
    print(f"Total entries removed: {total_removed}")

# Specify directories
false_positives_dir = r"C:\Users\Peter\Documents\GitHub\kalshi-bot\PatternsTest\FalsePositives"
all_patterns_dir = r"C:\Users\Peter\Documents\GitHub\kalshi-bot\TestingOutput\AllPatterns"

# Run the processing
process_false_positives(false_positives_dir, all_patterns_dir)