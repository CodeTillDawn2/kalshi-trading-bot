// NothingEverHappensStrat.cs
using BacklashDTOs;
using System.Text.Json;
using static BacklashInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Strategies.Strats
{
    public class NothingEverHappensStrat : Strat
    {
        public string Name { get; private set; }
        public override double Weight { get; }

        private readonly ActionType _defaultAction;
        private readonly Dictionary<ParamKey, double> _mlParams;

        public enum ParamKey
        {
            ProbNoThreshold,
            VelocityThresholdStandaloneYes,
            VelocityThresholdStandaloneNo,
            VolumePercentageYes,
            VolumePercentageNo,
            AbsorptionThreshold,
            MinSignalStrengthEntry,
            MinSignalStrengthExit,
            MinNumberOfPointsFromResolved,
            MaxBidImbalance
        }

        public NothingEverHappensStrat(string name = nameof(NothingEverHappensStrat), double weight = 1.0, Dictionary<ParamKey, double>? mlParams = null)
        {
            Name = name;
            Weight = weight;
            _defaultAction = ActionType.None;
            _mlParams = mlParams ?? new Dictionary<ParamKey, double>
            {
                { ParamKey.ProbNoThreshold, 0.60 },
                { ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                { ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                { ParamKey.VolumePercentageYes, 0.05 },
                { ParamKey.VolumePercentageNo, 0.05 },
                { ParamKey.AbsorptionThreshold, 10.0 },
                { ParamKey.MinSignalStrengthEntry, 3.0 },
                { ParamKey.MinSignalStrengthExit, 2.0 },
                { ParamKey.MinNumberOfPointsFromResolved, 5 },
                { ParamKey.MaxBidImbalance, 1000.0 }
            };
        }

        public override ActionDecision GetAction(MarketSnapshot snapshot, MarketSnapshot? previousSnapshot, int simulationPosition = 0)
        {
            if (!snapshot.ChangeMetricsMature)
                return new ActionDecision { Type = ActionType.None, Price = 0, Quantity = 1, Memo = "Change metrics not mature" };

            string actionMemo = "";

            double midYes = (snapshot.BestYesBid + snapshot.BestYesAsk) / 200.0;
            double probNo = 1 - midYes;
            double currentMid = (snapshot.BestYesBid + snapshot.BestYesAsk) / 2.0;

            double absorptionThreshold = _mlParams.GetValueOrDefault(ParamKey.AbsorptionThreshold, 10.0);
            double minSignalStrengthEntry = _mlParams.GetValueOrDefault(ParamKey.MinSignalStrengthEntry, 3.0);
            double minSignalStrengthExit = _mlParams.GetValueOrDefault(ParamKey.MinSignalStrengthExit, 2.0);
            double velocityPercentageStandaloneYes = _mlParams.GetValueOrDefault(ParamKey.VelocityThresholdStandaloneYes, 0.10);
            double velocityPercentageStandaloneNo = _mlParams.GetValueOrDefault(ParamKey.VelocityThresholdStandaloneNo, 0.10);
            double volumePercentageYes = _mlParams.GetValueOrDefault(ParamKey.VolumePercentageYes, 0.05);
            double volumePercentageNo = _mlParams.GetValueOrDefault(ParamKey.VolumePercentageNo, 0.05);
            double minNumberOfPointsFromResolved = _mlParams.GetValueOrDefault(ParamKey.MinNumberOfPointsFromResolved, 5);
            double maxBidImbalance = _mlParams.GetValueOrDefault(ParamKey.MaxBidImbalance, 1000.0);

            // Compute dollar depth for scaling thresholds
            double yesDollarDepth = snapshot.TotalBidContracts_Yes * (snapshot.BestYesBid / 100.0);
            double noDollarDepth = snapshot.TotalBidContracts_No * (snapshot.BestNoBid / 100.0);

            // Dynamic thresholds as percentage of dollar depth
            double velocityThresholdStandaloneYes = velocityPercentageStandaloneYes * yesDollarDepth;
            double velocityThresholdStandaloneNo = velocityPercentageStandaloneNo * noDollarDepth;
            double volumeThresholdYes = volumePercentageYes * yesDollarDepth;
            double volumeThresholdNo = volumePercentageNo * noDollarDepth;

            // Velocity sums (deltas)
            double velocitySumYes = snapshot.VelocityPerMinute_Top_Yes_Bid + snapshot.VelocityPerMinute_Bottom_Yes_Bid;
            double velocitySumNo = snapshot.VelocityPerMinute_Top_No_Bid + snapshot.VelocityPerMinute_Bottom_No_Bid;

            // Calculate signed ratios to preserve direction
            double YesCurrentRatio = velocitySumYes / (yesDollarDepth > 0 ? yesDollarDepth : 1);
            double NoCurrentRatio = velocitySumNo / (noDollarDepth > 0 ? noDollarDepth : 1);

            ActionType candidateAction = _defaultAction;
            double signalStrength = 0.0;

            actionMemo = $"ProbNo={probNo:F2},VelocityTopYesBid={snapshot.VelocityPerMinute_Top_Yes_Bid},VelocityTopNoBid={snapshot.VelocityPerMinute_Top_No_Bid}" +
                $",VelThreshYes={velocityThresholdStandaloneYes:F2},VelThreshNo={velocityThresholdStandaloneNo:F2}" +
                $",BidImbalance:{snapshot.BidCountImbalance}";

            if (simulationPosition == 0)
            {
                // Entry logic: Short if standard technical indicators suggest bearish trend (towards no)
                if (probNo > _mlParams[ParamKey.ProbNoThreshold] &&
                    snapshot.BestYesBid >= minNumberOfPointsFromResolved &&
                    snapshot.BidCountImbalance > -maxBidImbalance && snapshot.BidCountImbalance < maxBidImbalance)
                {
                    candidateAction = ActionType.Short;

                    // Use standard metrics (TA indicators) for signal strength
                    if (snapshot.EMA_Medium.HasValue && currentMid < snapshot.EMA_Medium.Value) signalStrength += 1.0; // Price below EMA (bearish)
                    if (snapshot.MACD_Medium.Histogram.HasValue && snapshot.MACD_Medium.Histogram < 0) signalStrength += 1.0; // Negative MACD histogram
                    if (snapshot.RSI_Medium.HasValue && snapshot.RSI_Medium > 70) signalStrength += 1.0; // Overbought RSI
                    if (snapshot.StochasticOscillator_Medium.K.HasValue && snapshot.StochasticOscillator_Medium.D.HasValue &&
                        snapshot.StochasticOscillator_Medium.K > 80) signalStrength += 1.0; // Overbought Stochastic
                    if (snapshot.OBV_Medium < 0) signalStrength += 1.0; // Negative OBV (bearish volume)
                    if (snapshot.BollingerBands_Medium.Upper.HasValue && currentMid > snapshot.BollingerBands_Medium.Upper.Value) signalStrength += 1.0; // Above upper Bollinger (potential reversion short)

                    // Include volume and absorption as additional standard metrics
                    if (snapshot.TradeVolumePerMinute_No > volumeThresholdNo) signalStrength += 0.5;

                    double yesDeltaRate = snapshot.TradeVolumePerMinute_Yes;
                    double noDeltaRate = snapshot.TradeVolumePerMinute_No;
                    double noAbsorptionRatio = noDeltaRate > 0 ? snapshot.TotalBidContracts_No / noDeltaRate : double.MaxValue;

                    if (noAbsorptionRatio < absorptionThreshold) signalStrength += 0.5;

                    actionMemo += $",Mid={currentMid:F2},EMA={snapshot.EMA_Medium:F2},MACD_Hist={snapshot.MACD_Medium.Histogram:F2}" +
                                  $",RSI={snapshot.RSI_Medium:F2},StochK={snapshot.StochasticOscillator_Medium.K:F2},OBV={snapshot.OBV_Medium}" +
                                  $",BBUpper={snapshot.BollingerBands_Medium.Upper:F2},TradeVolNo={snapshot.TradeVolumePerMinute_No:F2}" +
                                  $",NoAbsRatio={noAbsorptionRatio:F2},SignalStrength={signalStrength:F2}";

                    if (signalStrength >= minSignalStrengthEntry)
                    {
                        return new ActionDecision { Type = candidateAction, Price = 0, Quantity = 1, Memo = actionMemo };
                    }
                }
            }
            else if (simulationPosition < 0)
            {
                // Exit logic: Detect sudden trend to yes using velocity metrics

                // Exit trigger: Positive velocity on Yes (support building for Yes)
                if (velocitySumYes > velocityThresholdStandaloneYes &&
                    snapshot.BestYesAsk <= 100 - minNumberOfPointsFromResolved &&
                    snapshot.BidCountImbalance > -maxBidImbalance && snapshot.BidCountImbalance < maxBidImbalance &&
                    YesCurrentRatio > NoCurrentRatio)
                {
                    candidateAction = ActionType.Exit;
                    signalStrength += 1.0;
                }

                // Exit trigger: Negative velocity on No (support weakening for No, equivalent to bullish for Yes)
                double scaledVelocityNo = -velocitySumNo * Math.Min(Math.Max(yesDollarDepth / noDollarDepth, 0.5), 2.0);
                if (velocitySumNo < -velocityThresholdStandaloneNo &&
                    scaledVelocityNo > velocityThresholdStandaloneYes &&
                    snapshot.BestYesAsk <= 100 - minNumberOfPointsFromResolved &&
                    snapshot.BidCountImbalance > -maxBidImbalance && snapshot.BidCountImbalance < maxBidImbalance &&
                    -NoCurrentRatio > YesCurrentRatio)
                {
                    if (candidateAction == ActionType.Exit || candidateAction == _defaultAction)
                    {
                        candidateAction = ActionType.Exit;
                        signalStrength += 1.0;
                    }
                }

                // Confirmation with TA, volume, and absorption
                if (candidateAction == ActionType.Exit)
                {
                    actionMemo += $",MACD_Hist={snapshot.MACD_Medium.Histogram},SignalStrength={signalStrength}" +
                        $",candidateAction={candidateAction},volumeThresholdYes={volumeThresholdYes}";

                    // MACD histogram confirmation (positive for yes trend)
                    if (snapshot.MACD_Medium.Histogram > 0) signalStrength += 0.5;

                    // Volume confirmation
                    if (snapshot.TradeVolumePerMinute_Yes > volumeThresholdYes) signalStrength += 0.5;

                    // Absorption ratio for yes
                    double yesDeltaRate = snapshot.TradeVolumePerMinute_Yes;
                    double noDeltaRate = snapshot.TradeVolumePerMinute_No;
                    double yesAbsorptionRatio = yesDeltaRate > 0 ? snapshot.TotalBidContracts_Yes / yesDeltaRate : double.MaxValue;

                    if (yesAbsorptionRatio < absorptionThreshold) signalStrength += 0.5;

                    actionMemo += $",SignalStrength={signalStrength},minSignalStrengthExit={minSignalStrengthExit}";

                    // Decide based on signal strength
                    if (signalStrength >= minSignalStrengthExit)
                    {
                        return new ActionDecision { Type = candidateAction, Price = 0, Quantity = 1, Memo = actionMemo };
                    }
                }
            }
            else if (simulationPosition > 0)
            {
                // If somehow long, exit (this strat doesn't enter long)
                return new ActionDecision { Type = ActionType.Exit, Price = 0, Quantity = 1, Memo = "Unexpected long position; exiting" };
            }

            return new ActionDecision { Type = _defaultAction, Price = 0, Quantity = 1, Memo = actionMemo };
        }

        public override string ToJson()
        {
            var data = new
            {
                type = "NothingEverHappensStrat",
                name = Name,
                weight = Weight,
                defaultAction = _defaultAction.ToString(),
                mlParams = _mlParams.ToDictionary(k => k.Key.ToString(), v => v.Value)
            };
            return JsonSerializer.Serialize(data);
        }

        public static NothingEverHappensStrat FromJson(string json)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if ((string)data["type"] != "NothingEverHappensStrat")
            {
                throw new ArgumentException("Invalid strategy type");
            }
            var name = (string)data["name"];
            var weight = Convert.ToDouble(data["weight"]);
            var defaultAction = Enum.Parse<ActionType>((string)data["defaultAction"]);
            var mlParamsJson = (JsonElement)data["mlParams"];
            var mlParams = mlParamsJson.Deserialize<Dictionary<string, double>>()
                .ToDictionary(kv => Enum.Parse<ParamKey>(kv.Key), kv => kv.Value);
            return new NothingEverHappensStrat(name, weight, mlParams);
        }
    }
}
