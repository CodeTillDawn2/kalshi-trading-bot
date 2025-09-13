namespace TradingStrategies.Strategies
{
    /// <summary>
    /// Defines the various trading signals used by the strategy system to identify market conditions and technical indicators.
    /// These signals represent different market states, technical analysis patterns, and external factors that can influence trading decisions.
    /// Each signal corresponds to a specific condition that can be evaluated against market data to determine appropriate trading actions.
    /// </summary>
    public enum Signal
    {
        /// <summary>
        /// Mid price is above the EMA plus ATR band, indicating potential overbought conditions.
        /// </summary>
        MidAboveEmaPlusAtr,
        /// <summary>
        /// Mid price is below the EMA minus ATR band, indicating potential oversold conditions.
        /// </summary>
        MidBelowEmaMinusAtr,
        /// <summary>
        /// Mid price is above the upper Bollinger Band, suggesting potential reversal or overextension.
        /// </summary>
        MidAboveBollingerUpper,
        /// <summary>
        /// Mid price is below the lower Bollinger Band, suggesting potential reversal or overextension.
        /// </summary>
        MidBelowBollingerLower,
        /// <summary>
        /// MACD line is above the signal line, indicating bullish momentum.
        /// </summary>
        MacdAboveSignal,
        /// <summary>
        /// MACD line is below the signal line, indicating bearish momentum.
        /// </summary>
        MacdBelowSignal,
        /// <summary>
        /// Mid price is below the Volume Weighted Average Price (VWAP).
        /// </summary>
        MidBelowVwap,
        /// <summary>
        /// Mid price is above the Volume Weighted Average Price (VWAP).
        /// </summary>
        MidAboveVwap,
        /// <summary>
        /// Price is near a support level, potentially indicating a bounce opportunity.
        /// </summary>
        NearSupport,
        /// <summary>
        /// Price is near a resistance level, potentially indicating a reversal opportunity.
        /// </summary>
        NearResistance,
        /// <summary>
        /// Mid price is above the medium-term EMA.
        /// </summary>
        MidAboveEmaMedium,
        /// <summary>
        /// Mid price is below the medium-term EMA.
        /// </summary>
        MidBelowEmaMedium,
        /// <summary>
        /// Absolute position size is above the maximum allowed threshold.
        /// </summary>
        AbsPositionAboveMax,
        /// <summary>
        /// Depth at the best Yes bid is low, indicating thin liquidity on the buy side.
        /// </summary>
        DepthAtBestYesBidLow,
        /// <summary>
        /// Depth at the best Yes ask is low, indicating thin liquidity on the sell side.
        /// </summary>
        DepthAtBestYesAskLow,
        /// <summary>
        /// RSI indicates short-term overbought conditions.
        /// </summary>
        RsiShort,
        /// <summary>
        /// Trade volume per minute for Yes contracts is high.
        /// </summary>
        TradeVolumePerMinuteYesHigh,
        /// <summary>
        /// Trade volume per minute for No contracts is high.
        /// </summary>
        TradeVolumePerMinuteNoHigh,
        /// <summary>
        /// Stochastic K oscillator reading.
        /// </summary>
        StochasticK,
        /// <summary>
        /// On-Balance Volume (OBV) indicates medium-term accumulation/distribution.
        /// </summary>
        ObvMedium,
        /// <summary>
        /// Average True Range (ATR) indicates medium to high volatility.
        /// </summary>
        AtrMediumHigh,
        /// <summary>
        /// Bid side shows imbalance compared to ask side.
        /// </summary>
        BidImbalance,
        /// <summary>
        /// Velocity per minute at the top Yes bid price level.
        /// </summary>
        VelocityPerMinuteTopYesBid,
        /// <summary>
        /// Velocity per minute at the top No bid price level.
        /// </summary>
        VelocityPerMinuteTopNoBid,
        /// <summary>
        /// Extreme weather conditions affecting market sentiment.
        /// </summary>
        ExtremeWeather,
        /// <summary>
        /// Seasonal pattern influencing market behavior.
        /// </summary>
        SeasonalPattern,
        /// <summary>
        /// High volume buying activity detected.
        /// </summary>
        HighVolBuy,
        /// <summary>
        /// High volume selling activity detected.
        /// </summary>
        HighVolSell,
        /// <summary>
        /// Cryptocurrency-related news impacting markets.
        /// </summary>
        CryptoNews,
        /// <summary>
        /// Market expectation of interest rate cuts.
        /// </summary>
        RateCutExpectation,
        /// <summary>
        /// Macroeconomic event influencing market conditions.
        /// </summary>
        MacroEvent,
        /// <summary>
        /// MACD histogram shows positive divergence.
        /// </summary>
        PositiveHistogram,
        /// <summary>
        /// MACD histogram shows negative divergence.
        /// </summary>
        NegativeHistogram,
        /// <summary>
        /// Bullish momentum indicators are present.
        /// </summary>
        BullishMomentum,
        /// <summary>
        /// Bearish momentum indicators are present.
        /// </summary>
        BearishMomentum,
        /// <summary>
        /// Strong buying flow detected in the market.
        /// </summary>
        BuyFlow,
        /// <summary>
        /// Strong selling flow detected in the market.
        /// </summary>
        SellFlow,
        /// <summary>
        /// Asset appears to be undervalued relative to fundamentals.
        /// </summary>
        UndervaluedCandidate,
        /// <summary>
        /// Election timing is nearing, potentially affecting market volatility.
        /// </summary>
        ElectionNearing,
        /// <summary>
        /// Team or competitor favoritism influencing market sentiment.
        /// </summary>
        TeamFavorite,
        /// <summary>
        /// Live event occurring that may impact market outcomes.
        /// </summary>
        LiveEvent,
        /// <summary>
        /// Strong buying pressure detected.
        /// </summary>
        BuyPressure,
        /// <summary>
        /// Strong selling pressure detected.
        /// </summary>
        SellPressure,
        /// <summary>
        /// Price fading from recent highs.
        /// </summary>
        FadeHigh,
        /// <summary>
        /// Price fading from recent lows.
        /// </summary>
        FadeLow,
        /// <summary>
        /// Market indicators suggest overbought conditions.
        /// </summary>
        Overbought,
        /// <summary>
        /// Market indicators suggest oversold conditions.
        /// </summary>
        Oversold,
        /// <summary>
        /// Wide spread conditions favoring buy orders.
        /// </summary>
        WideSpreadBuy,
        /// <summary>
        /// Wide spread conditions favoring sell orders.
        /// </summary>
        WideSpreadSell,
        /// <summary>
        /// Bullish volume patterns detected.
        /// </summary>
        BullishVolume,
        /// <summary>
        /// Bearish volume patterns detected.
        /// </summary>
        BearishVolume,
        /// <summary>
        /// Large buy trades executed.
        /// </summary>
        LargeBuyTrades,
        /// <summary>
        /// On-Balance Volume (OBV) shows negative divergence.
        /// </summary>
        NegativeOBV,
        /// <summary>
        /// On-Balance Volume (OBV) shows positive divergence.
        /// </summary>
        PositiveOBV,
        /// <summary>
        /// Price breaking out to the upside.
        /// </summary>
        BreakoutUp,
        /// <summary>
        /// Price breaking out to the downside.
        /// </summary>
        BreakoutDown,
        /// <summary>
        /// Yes side showing active trading interest.
        /// </summary>
        ActiveYes
    }
}
