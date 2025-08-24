import pandas as pd
import json

@staticmethod
def save_dataframe_as_json(df: pd.DataFrame, filepath: str, orient='records') -> None:
    """
    Saves a pandas DataFrame as JSON file.

    :param df: The DataFrame to save.
    :param filepath: The path where the JSON file will be saved.
    :param orient: The orientation of the JSON output. Default is 'records' which 
                    produces a list of JSON objects with each row as an object.
                    Other options include 'split', 'index', 'columns', 'values'.
    """
    # Convert DataFrame to JSON string
    json_string = df.to_json(orient=orient)
    
    # Write JSON string to file
    with open(filepath, 'w') as file:
        json.dump(json.loads(json_string), file, indent=4)  # indent for pretty printing