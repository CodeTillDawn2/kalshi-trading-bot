using BacklashDTOs;
using System.Text.Json;
using static BacklashInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Strategies.Strats
{
    public class Breakout : Strat
    {
        public string Name { get; private set; }
        public override double Weight { get; }

        private readonly ActionType _defaultAction;
        private readonly Dictionary<ParamKey, double> _mlParams;

        public enum ParamKey
        {
            AbsorptionThreshold,
            MinSignalStrength,
            VelocityToDepthRatio,
            MinNumberOfPointsFromResolved,
            MaxBidImbalance,
            MinRatioDifference
        }

        public Breakout(string name = nameof(Breakout), double weight = 1.0, Dictionary<ParamKey, double>? mlParams = null)
        {
            Name = name;
            Weight = weight;
            _mlParams = mlParams ?? new Dictionary<ParamKey, double>
            {
                { ParamKey.AbsorptionThreshold, 10.0 },
                { ParamKey.MinSignalStrength, 2.0 },
                { ParamKey.VelocityToDepthRatio, 0.10 },
                { ParamKey.MinNumberOfPointsFromResolved, 5 },
                { ParamKey.MaxBidImbalance, 1000.0 },
                { ParamKey.MinRatioDifference, 0.05 }
            };
            _defaultAction = ActionType.None;
        }

        public override ActionDecision GetAction(MarketSnapshot snapshot, MarketSnapshot? previousSnapshot, int simulationPosition = 0)
        {
            if (!snapshot.ChangeMetricsMature)
                return new ActionDecision { Type = ActionType.None, Price = 0, Qty = 1, Memo = "Change metrics not mature" };

            string actionMemo = "";

            double currentMid = (snapshot.BestYesBid + snapshot.BestYesAsk) / 2.0;
            double absorptionThreshold = _mlParams.GetValueOrDefault(ParamKey.AbsorptionThreshold, 10.0);
            double minSignalStrength = _mlParams.GetValueOrDefault(ParamKey.MinSignalStrength, 2.0);
            double velocityToDepthRatio = _mlParams.GetValueOrDefault(ParamKey.VelocityToDepthRatio, 0.10);
            double minNumberOfPointsFromResolved = _mlParams.GetValueOrDefault(ParamKey.MinNumberOfPointsFromResolved, 5);
            double maxBidImbalance = _mlParams.GetValueOrDefault(ParamKey.MaxBidImbalance, 1000.0);
            double minRatioDifference = _mlParams.GetValueOrDefault(ParamKey.MinRatioDifference, 0.05);

            // Velocity sums (deltas)
            double velocitySumYes = snapshot.VelocityPerMinute_Top_Yes_Bid + snapshot.VelocityPerMinute_Bottom_Yes_Bid;
            double velocitySumNo = snapshot.VelocityPerMinute_Top_No_Bid + snapshot.VelocityPerMinute_Bottom_No_Bid;

            // Convert depth from cents to dollars for consistent units
            double totalOrderbookDepthYesDollars = snapshot.TotalOrderbookDepth_Yes / 100.0;
            double totalOrderbookDepthNoDollars = snapshot.TotalOrderbookDepth_No / 100.0;

            // Velocity thresholds (in dollars, consistent with velocity sums)
            double velocityThresholdYes = velocityToDepthRatio * totalOrderbookDepthYesDollars;
            double velocityThresholdNo = velocityToDepthRatio * totalOrderbookDepthNoDollars;

            ActionType candidateAction = _defaultAction;
            double signalStrength = 0.0;

            // Calculate signed ratios to preserve direction
            double YesCurrentRatio = velocitySumYes / (totalOrderbookDepthYesDollars > 0 ? totalOrderbookDepthYesDollars : 1);
            double NoCurrentRatio = velocitySumNo / (totalOrderbookDepthNoDollars > 0 ? totalOrderbookDepthNoDollars : 1);

            // Updated memo with explicit numbers used in calculations
            actionMemo = $"VelocityPerMinute_Top_Yes_Bid={snapshot.VelocityPerMinute_Top_Yes_Bid}," +
                         $"VelocityPerMinute_Bottom_Yes_Bid={snapshot.VelocityPerMinute_Bottom_Yes_Bid}," +
                         $"VelocityPerMinute_Top_No_Bid={snapshot.VelocityPerMinute_Top_No_Bid}," +
                         $"VelocityPerMinute_Bottom_No_Bid={snapshot.VelocityPerMinute_Bottom_No_Bid}," +
                         $"VelocitySumYes={velocitySumYes}," +
                         $"VelocitySumNo={velocitySumNo}," +
                         $"YesTotalDepth={totalOrderbookDepthYesDollars}," + // Log in dollars
                         $"NoTotalDepth={totalOrderbookDepthNoDollars}," +   // Log in dollars
                         $"YesCurrentRatio={YesCurrentRatio}," +
                         $"NoCurrentRatio={NoCurrentRatio}," +
                         $"TradeRatePerMinute_Yes={snapshot.TradeRatePerMinute_Yes}," +
                         $"TradeRatePerMinute_No={snapshot.TradeRatePerMinute_No}," +
                         $"RSI_Short={snapshot.RSI_Short}," +
                         $"velocityThresholdYes={velocityThresholdYes}," +
                         $"velocityThresholdNo={velocityThresholdNo}," +
                         $"HighestVolume_Minute={snapshot.HighestVolume_Minute}," +
                         $"EMA_Medium={snapshot.EMA_Medium}," +
                         $"RecentVolume_LastHour={snapshot.RecentVolume_LastHour}," +
                         $"StochasticOscillator_Short={snapshot.StochasticOscillator_Short}," +
                         $"VWAP_Short={snapshot.VWAP_Short}," +
                         $"BidImbalance={snapshot.BidCountImbalance}";

            // Primary: Velocity Breakout Detection with directionality
            // Long trigger: Positive velocity on Yes (support building for Yes)
            if (velocitySumYes > velocityThresholdYes &&
                snapshot.BestYesAsk <= 100 - minNumberOfPointsFromResolved &&
                Math.Abs(snapshot.BidCountImbalance) < maxBidImbalance &&
                YesCurrentRatio > Math.Abs(NoCurrentRatio) + minRatioDifference)
            {
                candidateAction = ActionType.Long;
                signalStrength += 2.0; // Increased weight for velocity
            }

            // Long trigger: Negative velocity on No (support weakening for No, equivalent to bullish for Yes)
            double scaledVelocityNo = -velocitySumNo * Math.Min(Math.Max(totalOrderbookDepthYesDollars / totalOrderbookDepthNoDollars, 0.5), 2.0);
            if (velocitySumNo < -velocityThresholdNo &&
                scaledVelocityNo > velocityThresholdYes &&
                snapshot.BestYesAsk <= 100 - minNumberOfPointsFromResolved &&
                Math.Abs(snapshot.BidCountImbalance) < maxBidImbalance &&
                -NoCurrentRatio > Math.Abs(YesCurrentRatio) + minRatioDifference &&
                YesCurrentRatio >= 0)
            {
                if (candidateAction == ActionType.Long || candidateAction == _defaultAction)
                {
                    candidateAction = ActionType.Long;
                    signalStrength += 2.0; // Increased weight for velocity
                }
            }

            // Short trigger: Positive velocity on No (support building for No)
            if (velocitySumNo > velocityThresholdNo &&
                snapshot.BestYesBid >= minNumberOfPointsFromResolved &&
                Math.Abs(snapshot.BidCountImbalance) < maxBidImbalance &&
                NoCurrentRatio > Math.Abs(YesCurrentRatio) + minRatioDifference)
            {
                candidateAction = ActionType.Short;
                signalStrength += 2.0; // Increased weight for velocity
            }

            // Short trigger: Negative velocity on Yes (support weakening for Yes)
            double scaledVelocityYes = -velocitySumYes * Math.Min(Math.Max(totalOrderbookDepthNoDollars / totalOrderbookDepthYesDollars, 0.5), 2.0);
            if (velocitySumYes < -velocityThresholdYes &&
                scaledVelocityYes > velocityThresholdNo &&
                snapshot.BestYesBid >= minNumberOfPointsFromResolved &&
                Math.Abs(snapshot.BidCountImbalance) < maxBidImbalance &&
                -YesCurrentRatio > Math.Abs(NoCurrentRatio) + minRatioDifference &&
                NoCurrentRatio >= 0)
            {
                if (candidateAction == ActionType.Short || candidateAction == _defaultAction)
                {
                    candidateAction = ActionType.Short;
                    signalStrength += 2.0; // Increased weight for velocity
                }
            }

            actionMemo += $",MACD_Medium.Histogram={snapshot.MACD_Medium.Histogram},SignalStrength={signalStrength}";

            // Stage 2: Confirmation with MACD and Absorption Ratio
            if (candidateAction != _defaultAction)
            {
                // MACD histogram confirmation
                if (snapshot.MACD_Medium.Histogram > 0 && candidateAction == ActionType.Long) signalStrength += 0.5;
                if (snapshot.MACD_Medium.Histogram < 0 && candidateAction == ActionType.Short) signalStrength += 0.5;

                // Absorption ratio
                double yesDeltaRate = snapshot.TradeVolumePerMinute_Yes;
                double noDeltaRate = snapshot.TradeVolumePerMinute_No;
                double yesAbsorptionRatio = yesDeltaRate != 0 ? snapshot.TotalBidContracts_Yes / Math.Abs(yesDeltaRate) : double.MaxValue;
                double noAbsorptionRatio = noDeltaRate != 0 ? snapshot.TotalBidContracts_No / Math.Abs(noDeltaRate) : double.MaxValue;

                if (candidateAction == ActionType.Long && yesAbsorptionRatio < absorptionThreshold) signalStrength += 0.5;
                if (candidateAction == ActionType.Short && noAbsorptionRatio < absorptionThreshold) signalStrength += 0.5;
            }

            actionMemo += $",minSignalStrength={minSignalStrength}";

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
                type = "Breakout",
                name = Name,
                weight = Weight,
                defaultAction = _defaultAction.ToString(),
                mlParams = _mlParams.ToDictionary(k => k.Key.ToString(), v => v.Value)
            };
            return JsonSerializer.Serialize(data);
        }

        public static Breakout FromJson(string json)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (data == null || !data.TryGetValue("type", out var typeObj) || typeObj is not string type || type != "Breakout")
            {
                throw new ArgumentException("Invalid strategy type");
            }
            var name = (string?)data["name"];
            var weight = Convert.ToDouble(data["weight"]);
            var defaultAction = Enum.Parse<ActionType>((string?)data["defaultAction"]);
            var mlParamsJson = (JsonElement)data["mlParams"];
            var mlParamsDict = mlParamsJson.Deserialize<Dictionary<string, double>>();
            if (mlParamsDict == null) mlParamsDict = new Dictionary<string, double>();
            var mlParams = mlParamsDict.ToDictionary(kv => Enum.Parse<ParamKey>(kv.Key), kv => kv.Value);
            return new Breakout(name, weight, mlParams);
        }
    }
}
