# SmokehouseDataClean.py

from typing import List, Dict
from SmokehouseModules.CandlestickData import CandlestickData

def remove_insufficient(candlesticks: List["CandlestickData"],
                       min_count: int = 20,
                       quality_checks: Dict = None) -> List["CandlestickData"]:
    """
    Removes candlesticks that don't meet quality criteria.

    Args:
        candlesticks: List of CandlestickData objects
        min_count: Minimum number of candlesticks required
        quality_checks: Dict of data quality parameters:
            - max_missing_pct: Maximum percentage of missing data allowed
            - max_zero_volume_pct: Maximum percentage of zero volume allowed
    """
    default_checks = {
        'max_missing_pct': 20,
        'max_zero_volume_pct': 80,
    }

    if quality_checks:
        default_checks.update(quality_checks)

    # Group by market
    market_groups = {}
    for cs in candlesticks:
        if cs.market_ticker not in market_groups:
            market_groups[cs.market_ticker] = []
        market_groups[cs.market_ticker].append(cs)

    filtered_candlesticks = []

    for market, market_data in market_groups.items():
        if len(market_data) < min_count:
            continue

        n = len(market_data)
        missing_data = sum(1 for cs in market_data if cs.mid_price is None)
        zero_volume = sum(1 for cs in market_data if cs.volume == 0)

        missing_pct = (missing_data / n) * 100
        zero_volume_pct = (zero_volume / n) * 100

        if (missing_pct <= default_checks['max_missing_pct'] and
                zero_volume_pct <= default_checks['max_zero_volume_pct']):
            filtered_candlesticks.extend(market_data)

    return filtered_candlesticks