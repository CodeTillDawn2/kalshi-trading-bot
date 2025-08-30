using SmokehouseDTOs;
using System.Text.Json;
using static SmokehouseInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Strategies.Strats
{
    public class BollingerBreakout : Strat
    {
        public string Name { get; private set; }
        public override double Weight { get; }

        private readonly ActionType _defaultAction;
        private readonly Dictionary<ParamKey, double> _mlParams;

        public enum ParamKey
        {
            VolumePercentageYes,
            VolumePercentageNo,
            VelocityPercentageBollinger,
            VelocityPercentageStandalone,
            SqueezeThreshold,
            AbsorptionThreshold,
            MinSignalStrength,
            MinNumberOfPointsFromResolved,
            MaxBidImbalance
        }

        public BollingerBreakout(string name = nameof(BollingerBreakout), double weight = 1.0, Dictionary<ParamKey, double> mlParams = null)
        {
            Name = name;
            Weight = weight;
            _mlParams = mlParams ?? new Dictionary<ParamKey, double>
            {
                { ParamKey.SqueezeThreshold, 0.05 },
                { ParamKey.AbsorptionThreshold, 10.0 },
                { ParamKey.MinSignalStrength, 2.0 },
                { ParamKey.VelocityPercentageBollinger, 0.05 },
                { ParamKey.VelocityPercentageStandalone, 0.10 },
                { ParamKey.VolumePercentageYes, 0.05 },
                { ParamKey.VolumePercentageNo, 0.05 },
                { ParamKey.MinNumberOfPointsFromResolved, 5},
                { ParamKey.MaxBidImbalance, 1000.0 }
            };
            _defaultAction = ActionType.None;
        }

        public override ActionDecision GetAction(MarketSnapshot snapshot, MarketSnapshot? previousSnapshot, int simulationPosition = 0)
        {
            if (!snapshot.ChangeMetricsMature)
                return new ActionDecision { Type = ActionType.None, Price = 0, Qty = 1, Memo = "Change metrics not mature" };

            string actionMemo = "";

            double currentMid = (snapshot.BestYesBid + snapshot.BestYesAsk) / 2.0;
            double squeezeThreshold = _mlParams.GetValueOrDefault(ParamKey.SqueezeThreshold, 0.05);
            double absorptionThreshold = _mlParams.GetValueOrDefault(ParamKey.AbsorptionThreshold, 10.0);
            double minSignalStrength = _mlParams.GetValueOrDefault(ParamKey.MinSignalStrength, 2.0);
            double velocityPercentageBollinger = _mlParams.GetValueOrDefault(ParamKey.VelocityPercentageBollinger, 0.05);
            double velocityPercentageStandalone = _mlParams.GetValueOrDefault(ParamKey.VelocityPercentageStandalone, 0.10);
            double volumePercentageYes = _mlParams.GetValueOrDefault(ParamKey.VolumePercentageYes, 0.05);
            double volumePercentageNo = _mlParams.GetValueOrDefault(ParamKey.VolumePercentageNo, 0.05);
            double MinNumberOfPointsFromResolved = _mlParams.GetValueOrDefault(ParamKey.MinNumberOfPointsFromResolved, 5);
            double MaxBidImbalance = _mlParams.GetValueOrDefault(ParamKey.MaxBidImbalance, 1000);

            // Compute dollar depth for scaling thresholds
            double yesDollarDepth = snapshot.TotalBidContracts_Yes * (snapshot.BestYesBid / 100.0);
            double noDollarDepth = snapshot.TotalBidContracts_No * (snapshot.BestNoBid / 100.0);

            // Dynamic thresholds as percentage of dollar depth
            double velocityThresholdBollingerYes = velocityPercentageBollinger * yesDollarDepth;
            double velocityThresholdBollingerNo = velocityPercentageBollinger * noDollarDepth;
            double velocityThresholdStandaloneYes = velocityPercentageStandalone * yesDollarDepth;
            double velocityThresholdStandaloneNo = velocityPercentageStandalone * noDollarDepth;
            double volumeThresholdYes = volumePercentageYes * yesDollarDepth;
            double volumeThresholdNo = volumePercentageNo * noDollarDepth;

            ActionType candidateAction = _defaultAction;
            double signalStrength = 0.0;

            if (snapshot.BollingerBands_Medium.Middle.HasValue)
            {
                double? upper = snapshot.BollingerBands_Medium.Upper;
                double? lower = snapshot.BollingerBands_Medium.Lower;
                double? middle = snapshot.BollingerBands_Medium.Middle;

                double bandWidth = upper.HasValue && lower.HasValue ? (upper.Value - lower.Value) / middle.Value : 0.0;
                bool isSqueeze = bandWidth < squeezeThreshold;

                actionMemo = $"Upper={upper},Lower={lower},Middle={middle},BandWidth={bandWidth},Squeeze={isSqueeze}" +
                    $",VelocityPerMinute_Top_Yes_Bid={snapshot.VelocityPerMinute_Top_Yes_Bid},VelocityPerMinute_Top_No_Bid={snapshot.VelocityPerMinute_Top_No_Bid}" +
                    $",VelocityThresholdStandaloneYes={velocityThresholdStandaloneYes},VelocityThresholdStandaloneNo={velocityThresholdStandaloneNo}" +
                    $",BidImbalance:{snapshot.BidCountImbalance}";

                // Stage 1: Bollinger Breakout Detection
                if (isSqueeze && upper.HasValue && currentMid > upper.Value
                    && snapshot.VelocityPerMinute_Top_Yes_Bid > velocityThresholdBollingerYes
                    && snapshot.BestYesAsk <= 100 - MinNumberOfPointsFromResolved
                    && snapshot.BidCountImbalance > -MaxBidImbalance && snapshot.BidCountImbalance < MaxBidImbalance
                    && snapshot.VelocityPerMinute_Top_Yes_Bid > snapshot.VelocityPerMinute_Top_No_Bid
                    )
                {
                    candidateAction = ActionType.Long;
                    signalStrength += 1.0; // Base score for breakout
                }
                else if (isSqueeze && lower.HasValue && currentMid < lower.Value
                    && snapshot.VelocityPerMinute_Top_No_Bid > velocityThresholdBollingerNo
                    && snapshot.BestYesBid >= MinNumberOfPointsFromResolved
                    && snapshot.BidCountImbalance > -MaxBidImbalance && snapshot.BidCountImbalance < MaxBidImbalance
                    && snapshot.VelocityPerMinute_Top_No_Bid > snapshot.VelocityPerMinute_Top_Yes_Bid
                    )
                {
                    candidateAction = ActionType.Short;
                    signalStrength += 1.0;
                }
            }

            // Standalone velocity as additional signal
            if (snapshot.VelocityPerMinute_Top_Yes_Bid > velocityThresholdStandaloneYes
                && snapshot.BestYesAsk <= 100 - MinNumberOfPointsFromResolved
                && snapshot.BidCountImbalance > -MaxBidImbalance && snapshot.BidCountImbalance < MaxBidImbalance
                )
            {
                if (candidateAction == ActionType.Long) signalStrength += 0.5;
                else if (candidateAction == _defaultAction)
                {
                    candidateAction = ActionType.Long;
                    signalStrength += 1.0;
                }
            }
            else if (snapshot.VelocityPerMinute_Top_No_Bid > velocityThresholdStandaloneNo
                && snapshot.BestYesBid >= MinNumberOfPointsFromResolved
                && snapshot.BidCountImbalance > -MaxBidImbalance && snapshot.BidCountImbalance < MaxBidImbalance
                )
            {
                if (candidateAction == ActionType.Short) signalStrength += 0.5;
                else if (candidateAction == _defaultAction)
                {
                    candidateAction = ActionType.Short;
                    signalStrength += 1.0;
                }
            }

            // Stage 2: Confirmation with MACD, Volume, and Absorption Ratio
            if (candidateAction != _defaultAction)
            {

                actionMemo += $",MACD_Medium.Histogram={snapshot.MACD_Medium.Histogram},SignalStrength={signalStrength}" +
                    $",candidateAction={candidateAction},volumeThresholdYes={volumeThresholdYes},volumeThresholdNo={volumeThresholdNo}";

                // MACD histogram confirmation
                if (snapshot.MACD_Medium.Histogram > 0 && candidateAction == ActionType.Long) signalStrength += 0.5;
                if (snapshot.MACD_Medium.Histogram < 0 && candidateAction == ActionType.Short) signalStrength += 0.5;

                // Volume confirmation
                if (candidateAction == ActionType.Long && snapshot.TradeVolumePerMinute_Yes > volumeThresholdYes) signalStrength += 0.5;
                if (candidateAction == ActionType.Short && snapshot.TradeVolumePerMinute_No > volumeThresholdNo) signalStrength += 0.5;

                // Absorption ratio: Using TradeVolumePerMinute as proxy for delta rate
                double yesDeltaRate = snapshot.TradeVolumePerMinute_Yes;
                double noDeltaRate = snapshot.TradeVolumePerMinute_No;
                double yesAbsorptionRatio = yesDeltaRate != 0 ? snapshot.TotalBidContracts_Yes / Math.Abs(yesDeltaRate) : double.MaxValue;
                double noAbsorptionRatio = noDeltaRate != 0 ? snapshot.TotalBidContracts_No / Math.Abs(noDeltaRate) : double.MaxValue;

                if (candidateAction == ActionType.Long && yesAbsorptionRatio < absorptionThreshold) signalStrength += 0.5;
                if (candidateAction == ActionType.Short && noAbsorptionRatio < absorptionThreshold) signalStrength += 0.5;
            }

            actionMemo += $",signalStrength={signalStrength},minSignalStrength={minSignalStrength}";


            // Decide based on signal strength
            if (signalStrength >= minSignalStrength)
            {
                return new ActionDecision { Type = candidateAction, Price = 0, Qty = 1, Memo = actionMemo };
            }

            return new ActionDecision { Type = _defaultAction, Price = 0, Qty = 1, Memo = actionMemo };
        }

        public override string ToJson()
        {
            var data = new
            {
                type = "BollingerBreakout",
                name = Name,
                weight = Weight,
                defaultAction = _defaultAction.ToString(),
                mlParams = _mlParams.ToDictionary(k => k.Key.ToString(), v => v.Value)
            };
            return JsonSerializer.Serialize(data);
        }

        public static BollingerBreakout FromJson(string json)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if ((string)data["type"] != "BollingerBreakout")
            {
                throw new ArgumentException("Invalid strategy type");
            }
            var name = (string)data["name"];
            var weight = Convert.ToDouble(data["weight"]);
            var defaultAction = Enum.Parse<ActionType>((string)data["defaultAction"]);
            var mlParamsJson = (JsonElement)data["mlParams"];
            var mlParams = mlParamsJson.Deserialize<Dictionary<string, double>>()
                .ToDictionary(kv => Enum.Parse<ParamKey>(kv.Key), kv => kv.Value);
            return new BollingerBreakout(name, weight, mlParams);
        }
    }
}