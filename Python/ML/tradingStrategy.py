from abc import ABC, abstractmethod
from typing import Dict, List, Optional, Tuple
from dataclasses import dataclass
import numpy as np

@dataclass
class TradeSignal:
    market: str
    direction: str  # 'BUY' or 'SELL'
    timestamp: int
    confidence: float
    suggested_size: float
    max_spread: float

class BaseStrategy(ABC):
    def __init__(self, 
                 initial_balance: float = 1000,
                 max_position_size: float = 100,
                 min_position_size: float = 20,
                 max_spread_ratio: float = 0.02):
        self.balance = initial_balance
        self.max_position_size = max_position_size
        self.min_position_size = min_position_size
        self.max_spread_ratio = max_spread_ratio
        self.positions = {}
        
    @abstractmethod
    def calculate_position_size(self, 
                              price: float, 
                              spread: float,
                              confidence: float) -> float:
        """Calculate appropriate position size based on current conditions"""
        pass
        
    @abstractmethod
    def should_enter(self,
                    candlestick: object,
                    ml_prediction: float,
                    market_data: Dict) -> Optional[TradeSignal]:
        """Determine if we should enter a trade"""
        pass
        
    @abstractmethod
    def should_exit(self,
                   position: Dict,
                   candlestick: object,
                   market_data: Dict) -> bool:
        """Determine if we should exit a trade"""
        pass
        
    def validate_spread(self, price: float, spread: float) -> bool:
        """Check if spread is acceptable relative to price"""
        spread_ratio = spread / price
        return spread_ratio <= self.max_spread_ratio

class FedRateStrategy(BaseStrategy):
    """Strategy specialized for FED interest rate markets"""
    def __init__(self, 
                 initial_balance: float = 1000,
                 max_position_size: float = 100,
                 min_position_size: float = 20,
                 max_spread_ratio: float = 2.0):  # Increased from 0.02 to 2.0 for binary markets
        super().__init__(initial_balance, max_position_size, min_position_size, max_spread_ratio)

    def validate_spread(self, price: float, spread: float) -> bool:
        """Modified spread validation for binary markets"""
        if price <= 0:  # Avoid division by zero
            return False
            
        spread_ratio = spread / price
        if spread_ratio > self.max_spread_ratio:
            print(f"Spread validation failed - ratio: {spread_ratio:.2f}")
            return False
            
        return True
    
    def calculate_position_size(self, price: float, spread: float, confidence: float) -> float:
        """Calculate position size based on price, spread and ML confidence"""
        # Start with base size relative to balance
        base_size = self.balance * 0.02
        
        # Adjust for spread
        spread_ratio = spread / price
        if spread_ratio > self.max_spread_ratio:
            return 0
            
        # Scale by confidence
        size = base_size * confidence
        
        # Apply limits
        size = max(min(size, self.max_position_size), self.min_position_size)
        
        return size
        
    def should_enter(self, candlestick: object, ml_prediction: float, market_data: Dict) -> Optional[TradeSignal]:
        """Determine entry signal for FED markets"""
        print(f"\nDetailed Entry Analysis:")
        print(f"Price: {candlestick.yes.mid_price}")
        print(f"Spread: {candlestick.yes.spread}")
        print(f"Raw ML Prediction: {ml_prediction}")
        
        # Transform prediction to probability-like score
        # Assuming model output is already scaled between -1 and 1
        confidence = (ml_prediction + 1) / 2  # Transform to 0-1 range
        print(f"Transformed Confidence: {confidence}")
        
        # Require strong signals
        if not (confidence < 0.3 or confidence > 0.7):  # Look for clear directional signals
            print("Signal not strong enough")
            return None
            
        if not self.validate_spread(candlestick.yes.mid_price, candlestick.yes.spread):
            return None
            
        # Calculate position size based on confidence
        size = self.calculate_position_size(
            candlestick.yes.mid_price,
            candlestick.yes.spread,
            abs(confidence - 0.5) * 2  # Scale confidence for position sizing
        )
        
        if size > 0:
            return TradeSignal(
                market=candlestick.yes.market_ticker,
                direction='BUY' if confidence > 0.7 else 'SELL',
                timestamp=candlestick.yes.end_period_ts,
                confidence=confidence,
                suggested_size=size,
                max_spread=candlestick.yes.spread
            )
            
        return None
        
    def should_exit(self, position: Dict, candlestick: object, market_data: Dict) -> bool:
        """Determine exit signal for FED markets"""
        # For now, use simple stop loss and take profit
        entry_price = position['entry_price']
        current_price = candlestick.yes.mid_price
        
        stop_loss = -0.02  # 2% loss
        take_profit = 0.03  # 3% gain
        
        return_pct = (current_price - entry_price) / entry_price
        
        return return_pct <= stop_loss or return_pct >= take_profit