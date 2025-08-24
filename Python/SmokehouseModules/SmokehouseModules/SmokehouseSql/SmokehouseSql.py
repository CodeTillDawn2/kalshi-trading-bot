"""
db_manager_pyodbc.py

A reusable module for querying a SQL database using pyodbc.
Database connection parameters are loaded from a .env file.

"""

import os
import pyodbc
from dotenv import load_dotenv

# Load environment variables from the .env file.
load_dotenv()


def connect():
    """
    Returns a connection object
    """
    driver = os.getenv("DRIVER")
    server = os.getenv("SERVER")
    database = os.getenv("DATABASE")
    uid = os.getenv("UID")
    pwd = os.getenv("PWD")

    # Construct the connection string.
    connection_string = (
        f"DRIVER={{{driver}}};"
        f"SERVER={server};"
        f"DATABASE={database};"
        f"UID={uid};"
        f"PWD={pwd}"
    )
    try:
        return pyodbc.connect(connection_string)
    except Exception as e:
        print("Error connecting to the database:", e)

class DatabaseManager:
    def __init__(self):

        print('Initializing!')
        driver = os.getenv("DRIVER")
        server = os.getenv("SERVER")
        database = os.getenv("DATABASE")
        uid = os.getenv("UID")
        pwd = os.getenv("PWD")

        # Construct the connection string.
        connection_string = (
            f"DRIVER={{{driver}}};"
            f"SERVER={server};"
            f"DATABASE={database};"
            f"UID={uid};"
            f"PWD={pwd}"
        )
        try:
            self.connection = pyodbc.connect(connection_string)
            # Optional: You can set autocommit to True if desired.
            # self.connection.autocommit = True
        except Exception as e:
            print("Error connecting to the database:", e)
            self.connection = None

    def execute_sproc(self, sproc_name, params=None):
        """
        Executes a stored procedure with the given name and parameters.

        :param sproc_name: Name of the stored procedure.
        :param params: A list of parameters (in order) to pass to the stored procedure.
                       (pyodbc uses positional parameters.)
        :return: A list of tuples containing the rows returned, or None if no data or error.
        """
        if self.connection is None:
            print("No database connection available.")
            return None

        params = params or []
        cursor = self.connection.cursor()
        try:
            # Build the parameter placeholder string based on the number of parameters.
            # For example, if there are 2 parameters, param_placeholders becomes "?, ?"
            param_placeholders = ", ".join("?" for _ in params)
            # Construct the SQL command to execute the stored procedure.
            # Adjust the syntax if your database requires a different calling convention.
            sql_command = f"EXEC {sproc_name} {param_placeholders}"
            cursor.execute(sql_command, params)
            try:
                rows = cursor.fetchall()
                return rows
            except pyodbc.ProgrammingError:
                # If no rows are returned (e.g., the stored procedure only performs an action).
                self.connection.commit()
                return None
        except Exception as e:
            print(f"Error executing stored procedure '{sproc_name}': {e}")
            return None
        finally:
            cursor.close()

    def execute_query(self, query, params=None):
        """
        Executes a general SQL query with optional parameters.

        :param query: The SQL query string.
        :param params: A list of parameters to pass to the query.
        :return: A list of tuples containing the rows returned, or None if no data or error.
        """
        if self.connection is None:
            print("No database connection available.")
            return None

        params = params or []
        cursor = self.connection.cursor()
        try:
            cursor.execute(query, params)
            try:
                rows = cursor.fetchall()
                return rows
            except pyodbc.ProgrammingError:
                # No rows were returned.
                self.connection.commit()
                return None
        except Exception as e:
            print(f"Error executing query: {e}")
            return None
        finally:
            cursor.close()

    def close(self):
        """
        Closes the database connection.
        """
        if self.connection:
            self.connection.close()
            self.connection = None
