import requests
import pyodbc
import json
import argparse

def parse_arguments():
    parser = argparse.ArgumentParser(description='Fetch and save Kalshi series data')
    parser.add_argument(
        '--series-ticker',
        type=str,
        required=True,
        help='The ticker symbol for the series to fetch'
    )
    return parser.parse_args()

def execute_stored_procedure(cursor, proc_name, params):
    """Execute a stored procedure with the given parameters"""
    sql = f"EXEC {proc_name} {','.join(['?' for _ in params])}"
    cursor.execute(sql, params)

def insert_raw_series(cursor, raw_data):
    series_data = raw_data['series']
    
    # Execute main series stored procedure
    execute_stored_procedure(cursor, 'sp_InsertUpdateSeries', 
        (series_data['ticker'],
         series_data.get('frequency', ''),
         series_data.get('title', ''),
         series_data.get('category', ''),
         series_data.get('contract_url', '')))
    
    # Insert settlement sources
    settlement_sources = series_data.get('settlement_sources', []) or []
    for source in settlement_sources:
        execute_stored_procedure(cursor, 'sp_InsertUpdateSeries_SettlementSource',
            (series_data['ticker'],
             source.get('name', ''),
             source.get('url', '')))
    
    # Insert tags
    tags = series_data.get('tags', []) or []
    for tag in tags:
        execute_stored_procedure(cursor, 'sp_InsertUpdateSeries_Tag',
            (series_data['ticker'],
             tag))

def main():
    # Parse command line arguments
    args = parse_arguments()
    series = args.series_ticker
    
    # Make API call
    url = f"https://api.elections.kalshi.com/trade-api/v2/series/{series}"
    headers = {"accept": "application/json"}
    response = requests.get(url, headers=headers)
    
    # Print raw response for debugging
    print("API Response:", response.text)
    
    # Parse JSON response
    try:
        data = json.loads(response.text)
    except json.JSONDecodeError as e:
        print(f"Failed to parse JSON response: {e}")
        print(f"Response content: {response.text}")
        return
    
    # Validate response structure
    if 'series' not in data:
        print("Error: Response does not contain 'series' data")
        print(f"Response content: {data}")
        return
    
    # Connect to database
    conn = pyodbc.connect('Driver={SQL Server};'
                         'Server=192.168.1.210;'
                         'Database=KalshiBot-Dev;'
                         'UID=KalshiBotUpdater;'
                         'PWD=YetiTheCat123;')
    cursor = conn.cursor()
    
    try:
        # Start transaction
        cursor.execute("BEGIN TRANSACTION")
        
        # Insert/update data
        insert_raw_series(cursor, data)
        
        # Commit transaction
        cursor.execute("COMMIT TRANSACTION")
        conn.commit()
        print(f"Successfully inserted data for series {series}")
        
    except Exception as e:
        conn.rollback()
        print(f"Error occurred: {str(e)}")
        raise
        
    finally:
        cursor.close()
        conn.close()

if __name__ == "__main__":
    main()