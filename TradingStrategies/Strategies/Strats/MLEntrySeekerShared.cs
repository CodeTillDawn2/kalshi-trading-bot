// MLEntrySeekerShared.cs
using BacklashDTOs;
using TradingStrategies.ML;
using static BacklashInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Strategies.Strats
{
    public class MLEntrySeekerShared : Strat
    {
        public override double Weight { get; }
        public string Name { get; }

        public enum ParamKey
        {
            Threshold, HorizonMinutes, TauTicks, DdTicks, DedupPreMinutes, DedupPostMinutes,
            MinFromResolved, Lr, L2, Eps, MaxFeatures
        }

        private readonly bool _evaluationOnly;
        private readonly int _horizonMin;
        private readonly int _tauTicks;
        private readonly int _ddTicks;
        private readonly int _dedupPreMin;
        private readonly int _dedupPostMin;
        private readonly double _minFromResolved;
        private readonly double _threshold;
        private readonly double _lr, _l2, _eps;
        private readonly int _maxFeat;
        private readonly string[] _featureKeys;
        private readonly FeatureExtractor _fx;
        private readonly OnlineLogReg _model;
        private readonly List<OpenEval> _open = new();
        private readonly HashSet<string> _peaksSeen = new();

        public MLEntrySeekerShared(
            string name, bool evaluationOnly, double weight, Dictionary<ParamKey, double> p, string[]? featureKeys = null)
        {
            Name = name;
            _evaluationOnly = evaluationOnly;
            Weight = weight;
            _threshold = p.GetValueOrDefault(ParamKey.Threshold, 0.70);
            _horizonMin = (int)p.GetValueOrDefault(ParamKey.HorizonMinutes, 30);
            _tauTicks = (int)p.GetValueOrDefault(ParamKey.TauTicks, 3);
            _ddTicks = (int)p.GetValueOrDefault(ParamKey.DdTicks, 4);
            _dedupPreMin = (int)p.GetValueOrDefault(ParamKey.DedupPreMinutes, 3);
            _dedupPostMin = (int)p.GetValueOrDefault(ParamKey.DedupPostMinutes, 6);
            _minFromResolved = p.GetValueOrDefault(ParamKey.MinFromResolved, 5);
            _lr = p.GetValueOrDefault(ParamKey.Lr, 0.05);
            _l2 = p.GetValueOrDefault(ParamKey.L2, 1e-4);
            _eps = p.GetValueOrDefault(ParamKey.Eps, 1e-8);
            _maxFeat = Math.Max(16, (int)p.GetValueOrDefault(ParamKey.MaxFeatures, 64));
            _featureKeys = featureKeys ?? FeatureExtractor.DefaultFeatureKeys();
            _fx = new FeatureExtractor(_featureKeys, _maxFeat);
            _model = new OnlineLogReg(_fx.Dim + 1, _lr, _l2, _eps);
        }

        public override ActionDecision GetAction(MarketSnapshot s, MarketSnapshot? prev, int simulationPosition = 0)
        {
            try
            {
                AdvanceOpen(s);
                if (!s.ChangeMetricsMature)
                {
                    Console.WriteLine($"Skipped {s.MarketTicker}: Not mature");
                    return new ActionDecision { Type = ActionType.None, Memo = "Not mature" };
                }
                if (!FarFromRails(s))
                {
                    Console.WriteLine($"Skipped {s.MarketTicker}: Near rails (Bid={s.BestYesBid}, Ask={s.BestYesAsk})");
                    return new ActionDecision { Type = ActionType.None, Memo = "Near rails" };
                }

                var x = _fx.Vectorize(s);
                double pLong = _model.Predict(WithDir(x, +1));
                double pShort = _model.Predict(WithDir(x, -1));
                double p = Math.Max(pLong, pShort);
                var side = ActionType.None;
                if (p >= _threshold)
                    side = (pLong >= pShort) ? ActionType.Long : ActionType.Short;

                string memo = $"p={p:F3} (L={pLong:F3}, S={pShort:F3}) thr={_threshold:F2} BYB={s.BestYesBid} BYA={s.BestYesAsk} Imb={s.BidCountImbalance} Fx={_fx.Dim}";
                if (side != ActionType.None)
                {
                    Console.WriteLine($"Signal triggered for {s.MarketTicker}: Side={side}, {memo}");
                    StartEval(s, side, memo, x, pLong, pShort);
                }
                else
                {
                    Console.WriteLine($"No signal for {s.MarketTicker}: {memo}");
                }

                if (_evaluationOnly || side == ActionType.None)
                    return new ActionDecision { Type = ActionType.None, Memo = memo };
                return new ActionDecision { Type = side, Qty = 1, Memo = memo };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAction for {s.MarketTicker}: {ex.Message}");
                return new ActionDecision { Type = ActionType.None, Memo = "Error occurred" };
            }
        }

        public override string ToJson() => "{}";

        private void StartEval(MarketSnapshot s, ActionType side, string memo, double[] x, double pL, double pS)
        {
            int mid = (s.BestYesBid + s.BestYesAsk) / 2;
            _open.Add(new OpenEval
            {
                Market = s.MarketTicker,
                Side = side,
                Dir = side == ActionType.Long ? +1 : -1,
                Entry = s.Timestamp,
                End = s.Timestamp.AddMinutes(_horizonMin),
                EntryMid = mid,
                X = (double[])x.Clone(),
                PLong = pL,
                PShort = pS,
                Memo = memo
            });
        }

        private void AdvanceOpen(MarketSnapshot s)
        {
            int currMid = (s.BestYesBid + s.BestYesAsk) / 2;
            for (int i = _open.Count - 1; i >= 0; i--)
            {
                var ev = _open[i];
                int delta = ev.Dir * (currMid - ev.EntryMid);
                if (ev.FirstTau == null && delta >= _tauTicks) ev.FirstTau = s.Timestamp;
                if (ev.FirstDD == null && delta <= -_ddTicks) ev.FirstDD = s.Timestamp;
                if (delta > ev.MFE) { ev.MFE = delta; ev.PeakTime = s.Timestamp; ev.PeakSize = delta; }
                if (delta < ev.MAE) { ev.MAE = delta; }
                if (s.Timestamp >= ev.End)
                {
                    FinalizeAndLearn(ev);
                    _open.RemoveAt(i);
                }
            }
        }

        private void FinalizeAndLearn(OpenEval ev)
        {
            try
            {
                bool success = ev.FirstTau.HasValue && (!ev.FirstDD.HasValue || ev.FirstTau.Value <= ev.FirstDD.Value);
                int y = success ? 1 : 0;
                var xDir = WithDir(ev.X, ev.Dir);
                _model.Update(xDir, y);

                string episodeHash = $"{ev.Market}_{ev.Side}_{ev.EntryMid}_{ev.PeakSize}_{ev.PeakTime:yyyyMMddHHmm}";
                if (_peaksSeen.Contains(episodeHash))
                {
                    Console.WriteLine($"Skipped duplicate entry for {ev.Market}: Hash={episodeHash}");
                    return;
                }
                _peaksSeen.Add(episodeHash);

                double timeNorm = (ev.PeakTime == default)
                    ? 1.0
                    : Math.Min(1.0, (ev.PeakTime - ev.Entry).TotalMinutes / _horizonMin);
                double score = (1.0 * ev.PeakSize) - (0.5 * timeNorm * _tauTicks) - (0.5 * Math.Abs(ev.MAE));

                Console.WriteLine($"Entry logged for {ev.Market}: Score={score:F3}, PeakSize={ev.PeakSize}, TimeToPeak={(ev.PeakTime == default ? TimeSpan.FromMinutes(_horizonMin) : ev.PeakTime - ev.Entry).TotalSeconds:F3}");

                ResearchBus.RecordEntry(new ResearchBus.EntryResearch(
                    MarketTicker: ev.Market,
                    Side: ev.Side,
                    EntryTime: ev.Entry,
                    HorizonEnd: ev.End,
                    EntryMidTicks: ev.EntryMid,
                    TauTicks: _tauTicks,
                    DdTicks: _ddTicks,
                    HitTau: success,
                    MfeTicks: ev.MFE,
                    MaeTicks: Math.Abs(ev.MAE),
                    PeakSizeTicks: ev.PeakSize,
                    TimeToPeak: ev.PeakTime == default ? TimeSpan.FromMinutes(_horizonMin) : ev.PeakTime - ev.Entry,
                    PLongAtEntry: ev.PLong,
                    PShortAtEntry: ev.PShort,
                    ThresholdUsed: _threshold,
                    Score: score,
                    Memo: ev.Memo,
                    ParameterSet: Name
                ));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in FinalizeAndLearn for {ev.Market}: {ex.Message}");
            }
        }

        private bool FarFromRails(MarketSnapshot s)
            => s.BestYesAsk <= 100 - _minFromResolved && s.BestYesBid >= _minFromResolved;

        private double[] WithDir(double[] x, int dir)
        {
            var v = new double[_fx.Dim + 1];
            Array.Copy(x, v, _fx.Dim);
            v[_fx.Dim] = dir;
            return v;
        }

        public void PreTrain(Dictionary<string, List<MarketSnapshot>> historicalDataByMarket)
        {
            foreach (var kvp in historicalDataByMarket)
            {
                string market = kvp.Key;
                var snapshots = kvp.Value.OrderBy(s => s.Timestamp).ToList();
                Console.WriteLine($"Pre-training on {snapshots.Count} snapshots for {market}");
                for (int i = 0; i < snapshots.Count; i++)
                {
                    var s = snapshots[i];
                    if (!FarFromRails(s))
                    {
                        Console.WriteLine($"PreTrain skipped {s.MarketTicker}: Near rails");
                        continue;
                    }
                    if (!s.ChangeMetricsMature)
                    {
                        Console.WriteLine($"PreTrain skipped {s.MarketTicker}: Not mature");
                        continue;
                    }
                    var x = _fx.Vectorize(s);
                    var label = SimulateLabel(s, snapshots, i);
                    if (label == null)
                    {
                        Console.WriteLine($"PreTrain skipped {s.MarketTicker}: No label generated");
                        continue;
                    }
                    _model.Update(WithDir(x, +1), label.Value.longSuccess ? 1 : 0);
                    _model.Update(WithDir(x, -1), label.Value.shortSuccess ? 1 : 0);
                }
            }
            Console.WriteLine("Offline pre-training completed.");
        }

        private (bool longSuccess, bool shortSuccess)? SimulateLabel(MarketSnapshot start, List<MarketSnapshot> sequence, int startIdx)
        {
            DateTime endTime = start.Timestamp.AddMinutes(_horizonMin);
            int entryMid = (start.BestYesBid + start.BestYesAsk) / 2;
            bool longTauHit = false, shortTauHit = false;
            bool longDdHit = false, shortDdHit = false;
            DateTime? longTauTime = null, shortTauTime = null;
            DateTime? longDdTime = null, shortDdTime = null;

            for (int j = startIdx + 1; j < sequence.Count; j++)
            {
                var curr = sequence[j];
                int currMid = (curr.BestYesBid + curr.BestYesAsk) / 2;
                int longDelta = currMid - entryMid;
                int shortDelta = entryMid - currMid;

                if (!longTauHit && longDelta >= _tauTicks)
                {
                    longTauHit = true;
                    longTauTime = curr.Timestamp;
                }
                if (!longDdHit && longDelta <= -_ddTicks)
                {
                    longDdHit = true;
                    longDdTime = curr.Timestamp;
                }
                if (!shortTauHit && shortDelta >= _tauTicks)
                {
                    shortTauHit = true;
                    shortTauTime = curr.Timestamp;
                }
                if (!shortDdHit && shortDelta <= -_ddTicks)
                {
                    shortDdHit = true;
                    shortDdTime = curr.Timestamp;
                }
            }

            // Allow partial horizons for small datasets
            bool longSuccess = longTauHit || !longDdHit; // Relaxed: Success if tau hit or no adverse movement
            bool shortSuccess = shortTauHit || !shortDdHit;
            Console.WriteLine($"Label for {start.MarketTicker}: LongSuccess={longSuccess}, ShortSuccess={shortSuccess}");
            return (longSuccess, shortSuccess);
        }

        public (double Accuracy, double AvgScore) EvaluateOffline(Dictionary<string, List<MarketSnapshot>> testDataByMarket)
        {
            int total = 0;
            int correctLong = 0, correctShort = 0;
            double totalScore = 0;

            foreach (var kvp in testDataByMarket)
            {
                string market = kvp.Key;
                var snapshots = kvp.Value.OrderBy(s => s.Timestamp).ToList();
                Console.WriteLine($"Evaluating on {snapshots.Count} snapshots for {market}");
                for (int i = 0; i < snapshots.Count; i++)
                {
                    var s = snapshots[i];
                    if (!FarFromRails(s) || !s.ChangeMetricsMature) continue;
                    var x = _fx.Vectorize(s);
                    double pLong = _model.Predict(WithDir(x, +1));
                    double pShort = _model.Predict(WithDir(x, -1));
                    var trueLabel = SimulateLabel(s, snapshots, i);
                    if (trueLabel == null) continue;

                    total++;
                    bool predLong = pLong >= _threshold;
                    bool predShort = pShort >= _threshold;
                    if (predLong == trueLabel.Value.longSuccess) correctLong++;
                    if (predShort == trueLabel.Value.shortSuccess) correctShort++;

                    double mockMFE = trueLabel.Value.longSuccess || trueLabel.Value.shortSuccess ? _tauTicks + 1 : 0;
                    double mockMAE = trueLabel.Value.longSuccess || trueLabel.Value.shortSuccess ? 0 : _ddTicks;
                    double timeNorm = 1.0;
                    double score = mockMFE - (0.5 * timeNorm * _tauTicks) - (0.5 * mockMAE);
                    totalScore += score;
                }
            }

            double accuracy = total > 0 ? (correctLong + correctShort) / (double)(2 * total) : 0;
            double avgScore = total > 0 ? totalScore / total : 0;
            Console.WriteLine($"Offline evaluation: Accuracy={accuracy:F3}, AvgScore={avgScore:F3}, TotalEntries={total}");
            return (accuracy, avgScore);
        }

        private class OpenEval
        {
            public string Market { get; set; }
            public ActionType Side { get; set; }
            public int Dir { get; set; }
            public DateTime Entry { get; set; }
            public DateTime End { get; set; }
            public int EntryMid { get; set; }
            public int MFE { get; set; } = 0;
            public int MAE { get; set; } = 0;
            public DateTime? FirstTau { get; set; }
            public DateTime? FirstDD { get; set; }
            public DateTime PeakTime { get; set; }
            public int PeakSize { get; set; }
            public double[] X { get; set; }
            public double PLong { get; set; }
            public double PShort { get; set; }
            public string Memo { get; set; }
        }

        public static List<(string Name, Dictionary<ParamKey, double> Parameters)> MLSharedParameterSets = new()
        {
            ("Default", new Dictionary<ParamKey, double>
            {
                { ParamKey.Threshold, 0.72 }, { ParamKey.HorizonMinutes, 30 }, { ParamKey.TauTicks, 3 },
                { ParamKey.DdTicks, 4 }, { ParamKey.DedupPreMinutes, 3 }, { ParamKey.DedupPostMinutes, 6 },
                { ParamKey.MinFromResolved, 5 }, { ParamKey.Lr, 0.05 }, { ParamKey.L2, 0.0001 },
                { ParamKey.Eps, 1e-8 }, { ParamKey.MaxFeatures, 64 }
            }),
            ("Aggressive", new Dictionary<ParamKey, double>
            {
                { ParamKey.Threshold, 0.65 }, { ParamKey.HorizonMinutes, 15 }, { ParamKey.TauTicks, 2 },
                { ParamKey.DdTicks, 3 }, { ParamKey.DedupPreMinutes, 2 }, { ParamKey.DedupPostMinutes, 4 },
                { ParamKey.MinFromResolved, 4 }, { ParamKey.Lr, 0.1 }, { ParamKey.L2, 0.00005 },
                { ParamKey.Eps, 1e-7 }, { ParamKey.MaxFeatures, 128 }
            }),
            ("Thresh_060", new Dictionary<ParamKey, double>
            {
                { ParamKey.Threshold, 0.60 }, { ParamKey.HorizonMinutes, 30 }, { ParamKey.TauTicks, 3 },
                { ParamKey.DdTicks, 4 }, { ParamKey.DedupPreMinutes, 3 }, { ParamKey.DedupPostMinutes, 6 },
                { ParamKey.MinFromResolved, 5 }, { ParamKey.Lr, 0.05 }, { ParamKey.L2, 0.0001 },
                { ParamKey.Eps, 1e-8 }, { ParamKey.MaxFeatures, 64 }
            }),
            ("Thresh_065", new Dictionary<ParamKey, double>
            {
                { ParamKey.Threshold, 0.65 }, { ParamKey.HorizonMinutes, 30 }, { ParamKey.TauTicks, 3 },
                { ParamKey.DdTicks, 4 }, { ParamKey.DedupPreMinutes, 3 }, { ParamKey.DedupPostMinutes, 6 },
                { ParamKey.MinFromResolved, 5 }, { ParamKey.Lr, 0.05 }, { ParamKey.L2, 0.0001 },
                { ParamKey.Eps, 1e-8 }, { ParamKey.MaxFeatures, 64 }
            }),
            ("Thresh_070", new Dictionary<ParamKey, double>
            {
                { ParamKey.Threshold, 0.70 }, { ParamKey.HorizonMinutes, 30 }, { ParamKey.TauTicks, 3 },
                { ParamKey.DdTicks, 4 }, { ParamKey.DedupPreMinutes, 3 }, { ParamKey.DedupPostMinutes, 6 },
                { ParamKey.MinFromResolved, 5 }, { ParamKey.Lr, 0.05 }, { ParamKey.L2, 0.0001 },
                { ParamKey.Eps, 1e-8 }, { ParamKey.MaxFeatures, 64 }
            }),
            ("Thresh_075", new Dictionary<ParamKey, double>
            {
                { ParamKey.Threshold, 0.75 }, { ParamKey.HorizonMinutes, 30 }, { ParamKey.TauTicks, 3 },
                { ParamKey.DdTicks, 4 }, { ParamKey.DedupPreMinutes, 3 }, { ParamKey.DedupPostMinutes, 6 },
                { ParamKey.MinFromResolved, 5 }, { ParamKey.Lr, 0.05 }, { ParamKey.L2, 0.0001 },
                { ParamKey.Eps, 1e-8 }, { ParamKey.MaxFeatures, 64 }
            }),
            ("Thresh_080", new Dictionary<ParamKey, double>
            {
                { ParamKey.Threshold, 0.80 }, { ParamKey.HorizonMinutes, 30 }, { ParamKey.TauTicks, 3 },
                { ParamKey.DdTicks, 4 }, { ParamKey.DedupPreMinutes, 3 }, { ParamKey.DedupPostMinutes, 6 },
                { ParamKey.MinFromResolved, 5 }, { ParamKey.Lr, 0.05 }, { ParamKey.L2, 0.0001 },
                { ParamKey.Eps, 1e-8 }, { ParamKey.MaxFeatures, 64 }
            }),
            ("Horiz_10", new Dictionary<ParamKey, double>
            {
                { ParamKey.Threshold, 0.72 }, { ParamKey.HorizonMinutes, 10 }, { ParamKey.TauTicks, 3 },
                { ParamKey.DdTicks, 4 }, { ParamKey.DedupPreMinutes, 3 }, { ParamKey.DedupPostMinutes, 6 },
                { ParamKey.MinFromResolved, 5 }, { ParamKey.Lr, 0.05 }, { ParamKey.L2, 0.0001 },
                { ParamKey.Eps, 1e-8 }, { ParamKey.MaxFeatures, 64 }
            }),
            ("Horiz_20", new Dictionary<ParamKey, double>
            {
                { ParamKey.Threshold, 0.72 }, { ParamKey.HorizonMinutes, 20 }, { ParamKey.TauTicks, 3 },
                { ParamKey.DdTicks, 4 }, { ParamKey.DedupPreMinutes, 3 }, { ParamKey.DedupPostMinutes, 6 },
                { ParamKey.MinFromResolved, 5 }, { ParamKey.Lr, 0.05 }, { ParamKey.L2, 0.0001 },
                { ParamKey.Eps, 1e-8 }, { ParamKey.MaxFeatures, 64 }
            }),
            ("Horiz_40", new Dictionary<ParamKey, double>
            {
                { ParamKey.Threshold, 0.72 }, { ParamKey.HorizonMinutes, 40 }, { ParamKey.TauTicks, 3 },
                { ParamKey.DdTicks, 4 }, { ParamKey.DedupPreMinutes, 3 }, { ParamKey.DedupPostMinutes, 6 },
                { ParamKey.MinFromResolved, 5 }, { ParamKey.Lr, 0.05 }, { ParamKey.L2, 0.0001 },
                { ParamKey.Eps, 1e-8 }, { ParamKey.MaxFeatures, 64 }
            }),
            ("Horiz_50", new Dictionary<ParamKey, double>
            {
                { ParamKey.Threshold, 0.72 }, { ParamKey.HorizonMinutes, 50 }, { ParamKey.TauTicks, 3 },
                { ParamKey.DdTicks, 4 }, { ParamKey.DedupPreMinutes, 3 }, { ParamKey.DedupPostMinutes, 6 },
                { ParamKey.MinFromResolved, 5 }, { ParamKey.Lr, 0.05 }, { ParamKey.L2, 0.0001 },
                { ParamKey.Eps, 1e-8 }, { ParamKey.MaxFeatures, 64 }
            }),
            ("Horiz_60", new Dictionary<ParamKey, double>
            {
                { ParamKey.Threshold, 0.72 }, { ParamKey.HorizonMinutes, 60 }, { ParamKey.TauTicks, 3 },
                { ParamKey.DdTicks, 4 }, { ParamKey.DedupPreMinutes, 3 }, { ParamKey.DedupPostMinutes, 6 },
                { ParamKey.MinFromResolved, 5 }, { ParamKey.Lr, 0.05 }, { ParamKey.L2, 0.0001 },
                { ParamKey.Eps, 1e-8 }, { ParamKey.MaxFeatures, 64 }
            }),
            ("LowThresh_ShortHoriz", new Dictionary<ParamKey, double>
            {
                { ParamKey.Threshold, 0.60 }, { ParamKey.HorizonMinutes, 10 }, { ParamKey.TauTicks, 2 },
                { ParamKey.DdTicks, 3 }, { ParamKey.DedupPreMinutes, 2 }, { ParamKey.DedupPostMinutes, 4 },
                { ParamKey.MinFromResolved, 4 }, { ParamKey.Lr, 0.1 }, { ParamKey.L2, 0.00005 },
                { ParamKey.Eps, 1e-7 }, { ParamKey.MaxFeatures, 128 }
            }),
            ("HighThresh_LongHoriz", new Dictionary<ParamKey, double>
            {
                { ParamKey.Threshold, 0.80 }, { ParamKey.HorizonMinutes, 60 }, { ParamKey.TauTicks, 4 },
                { ParamKey.DdTicks, 5 }, { ParamKey.DedupPreMinutes, 4 }, { ParamKey.DedupPostMinutes, 8 },
                { ParamKey.MinFromResolved, 6 }, { ParamKey.Lr, 0.02 }, { ParamKey.L2, 0.001 },
                { ParamKey.Eps, 1e-9 }, { ParamKey.MaxFeatures, 32 }
            }),
            ("TightRisk", new Dictionary<ParamKey, double>
            {
                { ParamKey.Threshold, 0.72 }, { ParamKey.HorizonMinutes, 30 }, { ParamKey.TauTicks, 2 },
                { ParamKey.DdTicks, 3 }, { ParamKey.DedupPreMinutes, 3 }, { ParamKey.DedupPostMinutes, 6 },
                { ParamKey.MinFromResolved, 5 }, { ParamKey.Lr, 0.05 }, { ParamKey.L2, 0.0001 },
                { ParamKey.Eps, 1e-8 }, { ParamKey.MaxFeatures, 64 }
            }),
            ("LooseRisk", new Dictionary<ParamKey, double>
            {
                { ParamKey.Threshold, 0.72 }, { ParamKey.HorizonMinutes, 30 }, { ParamKey.TauTicks, 5 },
                { ParamKey.DdTicks, 6 }, { ParamKey.DedupPreMinutes, 3 }, { ParamKey.DedupPostMinutes, 6 },
                { ParamKey.MinFromResolved, 5 }, { ParamKey.Lr, 0.05 }, { ParamKey.L2, 0.0001 },
                { ParamKey.Eps, 1e-8 }, { ParamKey.MaxFeatures, 64 }
            }),
            ("HighLr", new Dictionary<ParamKey, double>
            {
                { ParamKey.Threshold, 0.72 }, { ParamKey.HorizonMinutes, 30 }, { ParamKey.TauTicks, 3 },
                { ParamKey.DdTicks, 4 }, { ParamKey.DedupPreMinutes, 3 }, { ParamKey.DedupPostMinutes, 6 },
                { ParamKey.MinFromResolved, 5 }, { ParamKey.Lr, 0.1 }, { ParamKey.L2, 0.0001 },
                { ParamKey.Eps, 1e-8 }, { ParamKey.MaxFeatures, 64 }
            }),
            ("LowLr", new Dictionary<ParamKey, double>
            {
                { ParamKey.Threshold, 0.72 }, { ParamKey.HorizonMinutes, 30 }, { ParamKey.TauTicks, 3 },
                { ParamKey.DdTicks, 4 }, { ParamKey.DedupPreMinutes, 3 }, { ParamKey.DedupPostMinutes, 6 },
                { ParamKey.MinFromResolved, 5 }, { ParamKey.Lr, 0.01 }, { ParamKey.L2, 0.0001 },
                { ParamKey.Eps, 1e-8 }, { ParamKey.MaxFeatures, 64 }
            }),
            ("HighFeat", new Dictionary<ParamKey, double>
            {
                { ParamKey.Threshold, 0.72 }, { ParamKey.HorizonMinutes, 30 }, { ParamKey.TauTicks, 3 },
                { ParamKey.DdTicks, 4 }, { ParamKey.DedupPreMinutes, 3 }, { ParamKey.DedupPostMinutes, 6 },
                { ParamKey.MinFromResolved, 5 }, { ParamKey.Lr, 0.05 }, { ParamKey.L2, 0.0001 },
                { ParamKey.Eps, 1e-8 }, { ParamKey.MaxFeatures, 128 }
            }),
            ("LowFeat", new Dictionary<ParamKey, double>
            {
                { ParamKey.Threshold, 0.72 }, { ParamKey.HorizonMinutes, 30 }, { ParamKey.TauTicks, 3 },
                { ParamKey.DdTicks, 4 }, { ParamKey.DedupPreMinutes, 3 }, { ParamKey.DedupPostMinutes, 6 },
                { ParamKey.MinFromResolved, 5 }, { ParamKey.Lr, 0.05 }, { ParamKey.L2, 0.0001 },
                { ParamKey.Eps, 1e-8 }, { ParamKey.MaxFeatures, 32 }
            })
        };

        private sealed class FeatureExtractor
        {
            private readonly Dictionary<string, int> _index = new();
            private readonly int _cap;
            public int Dim => _cap;

            public FeatureExtractor(string[] keys, int cap)
            {
                _cap = cap;
                int assigned = 0;
                for (int i = 0; i < keys.Length; i++)
                {
                    if (assigned < _cap)
                    {
                        _index[keys[i]] = assigned++;
                    }
                }
                if (keys.Length > _cap)
                    Console.WriteLine($"Warning: Feature keys ({keys.Length}) exceed cap ({_cap}); truncated {keys.Length - _cap} features.");
            }

            public static string[] DefaultFeatureKeys() => new[]
            {
                "BestYesBid", "BestYesAsk", "BestNoBid", "NoSpread", "DepthAtBestYesBid", "DepthAtBestNoBid",
                "TotalBidContracts_Yes", "TotalBidContracts_No", "BidCountImbalance",
                "TradeRatePerMinute_Yes", "TradeRatePerMinute_No", "TradeVolumePerMinute_Yes", "TradeVolumePerMinute_No",
                "TradeCount_Yes", "TradeCount_No", "AverageTradeSize_Yes", "AverageTradeSize_No",
                "RSI_Medium", "MACD_Medium_Hist", "MidYes"
            };

            public double[] Vectorize(MarketSnapshot s)
            {
                var x = new double[_cap];
                void Put(string k, double v)
                {
                    if (_index.TryGetValue(k, out int i) && i < _cap) x[i] = v;
                }

                Put("BestYesBid", s.BestYesBid);
                Put("BestYesAsk", s.BestYesAsk);
                Put("BestNoBid", s.BestNoBid);
                Put("NoSpread", s.NoSpread);
                Put("DepthAtBestYesBid", s.DepthAtBestYesBid);
                Put("DepthAtBestNoBid", s.DepthAtBestNoBid);
                Put("TotalBidContracts_Yes", s.TotalBidContracts_Yes);
                Put("TotalBidContracts_No", s.TotalBidContracts_No);
                Put("BidCountImbalance", s.BidCountImbalance);
                Put("TradeRatePerMinute_Yes", s.TradeRatePerMinute_Yes);
                Put("TradeRatePerMinute_No", s.TradeRatePerMinute_No);
                Put("TradeVolumePerMinute_Yes", s.TradeVolumePerMinute_Yes);
                Put("TradeVolumePerMinute_No", s.TradeVolumePerMinute_No);
                Put("TradeCount_Yes", s.TradeCount_Yes);
                Put("TradeCount_No", s.TradeCount_No);
                Put("AverageTradeSize_Yes", s.AverageTradeSize_Yes);
                Put("AverageTradeSize_No", s.AverageTradeSize_No);
                if (s.RSI_Medium != null) Put("RSI_Medium", s.RSI_Medium.Value);
                if (s.MACD_Medium.Histogram != null) Put("MACD_Medium_Hist", s.MACD_Medium.Histogram.Value);

                int mid = (s.BestYesBid + s.BestYesAsk) / 2;
                Put("MidYes", mid);

                for (int i = 0; i < x.Length; i++)
                {
                    double v = x[i];
                    if (v == 0) continue;
                    if (v > 150) v = Math.Log(1 + v);
                    else if (v >= 0 && v <= 100) v = (v - 50) / 50.0;
                    else v = v / 100.0;
                    x[i] = v;
                }
                return x;
            }
        }

        private sealed class OnlineLogReg
        {
            private readonly double[] _w, _g2;
            private readonly double _lr, _l2, _eps;

            public OnlineLogReg(int dim, double lr, double l2, double eps)
            {
                _w = new double[dim];
                _g2 = new double[dim];
                _lr = lr;
                _l2 = l2;
                _eps = eps;
            }

            public double Predict(double[] x)
            {
                double z = 0;
                for (int i = 0; i < _w.Length && i < x.Length; i++) z += _w[i] * x[i];
                return z >= 0 ? 1.0 / (1.0 + Math.Exp(-z)) : Math.Exp(z) / (1.0 + Math.Exp(z));
            }

            public void Update(double[] x, int y)
            {
                double p = Predict(x);
                double err = p - y;
                for (int i = 0; i < _w.Length && i < x.Length; i++)
                {
                    double gi = err * x[i] + _l2 * _w[i];
                    _g2[i] += gi * gi;
                    double step = _lr / Math.Sqrt(_g2[i] + _eps);
                    _w[i] -= step * gi;
                }
            }
        }
    }
}
