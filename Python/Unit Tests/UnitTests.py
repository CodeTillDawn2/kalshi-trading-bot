import json
from SmokehouseModules import CandlestickData, PatternDetector, SmokehousePattern, MarketState, compare_market_states
from datetime import datetime

class UnitTester:
    @staticmethod
    def UnitTest_SaveLoadPatterns():
        with open('testCandlesticks.json', 'r') as file:
            data = json.load(file)

        candlesticks = [CandlestickData(tuple(row.values())) for row in data]
        ffdf = CandlestickData.forward_fill_and_return_df(candlesticks, "BTCUSD", 1)
        patterns = PatternDetector.detect_patterns_talib(ffdf)

        # Save and load patterns
        SmokehousePattern.save_patterns_to_json(patterns, "testPatterns.json")
        newPatterns = SmokehousePattern.load_patterns_from_json("testPatterns.json")

        return patterns == newPatterns

    @staticmethod
    def UnitTest_MarketStates():
        market_tickers = "KXEPSTEIN-25,ADAMS-25APR"
        candlesticks = CandlestickData.get_candlestick_data(interval_type=1, market_tickers=market_tickers)
        market_state1 = MarketState(market_ticker="KXEPSTEIN-25", timestamp=datetime(2025, 2, 5, 21, 57), candlesticks=candlesticks)
        market_state2 = MarketState(market_ticker="ADAMS-25APR", timestamp=datetime(2024, 12, 3, 21, 52), candlesticks=candlesticks)
        market_state3 = MarketState(market_ticker="ADAMS-25APR", timestamp=datetime(2024, 11, 3, 21, 52), candlesticks=candlesticks)
        market_state4 = MarketState(market_ticker="KXEPSTEIN-25", timestamp=datetime(2025, 2, 5, 21, 57), candlesticks=candlesticks)
        # Compare MarketState instances
        differentMarket = compare_market_states(market_state1, market_state2)
        differentTime = compare_market_states(market_state2, market_state3)
        same = compare_market_states(market_state1, market_state4)

        if differentMarket == False and differentMarket == False and same == True:
            return True
        return False

TestResults = {}

# Run tests and store results
TestResults['SaveLoadPatterns'] = UnitTester.UnitTest_SaveLoadPatterns()
TestResults['MarketStates'] = UnitTester.UnitTest_MarketStates()

# Print out the results
for test_name, result in TestResults.items():
    print(f"Test '{test_name}': {'Pass' if result else 'Fail'}")