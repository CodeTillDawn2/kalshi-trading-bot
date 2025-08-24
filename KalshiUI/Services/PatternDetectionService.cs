// PatternDetectionService.cs
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SmokehousePatterns;
using SmokehousePatterns.Helpers;

namespace KalshiUI.Services
{
    public class PatternDetectionService
    {
        public static bool PatternsRunning = false;
        public static bool ConfigsRunning = false;
        private readonly IConfiguration _configuration;

        public PatternDetectionService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task StartDetectingPatterns(
            Action<string> updateStatus,
            bool cleanupTempConfigs = true)
        {
            PatternsRunning = true;
            const int batchSize = 10;

            string lastRunningMarket = "";


            string parquetDirectory = _configuration["Paths:ParquetDirectory"];
            string outputDirectory = _configuration["Paths:OutputDirectory"];

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var parquetFiles = Directory.GetFiles(parquetDirectory, "*_MarketStates.parquet")
                .OrderBy(f => f)
                .ToList();

            int totalFiles = parquetFiles.Count;
            int totalBatches = (int)Math.Ceiling(totalFiles / (double)batchSize);

            updateStatus?.Invoke($"Starting to process {totalFiles} Parquet files in {totalBatches} batches of {batchSize}.");

            var patternDetector = new PatternDetector();
            string finalConfigPath = Path.Combine(outputDirectory, $"Config_{DateTime.Now:yyyyMMdd_HHmmss}.json");

            for (int batch = 0; batch < totalBatches && PatternsRunning; batch++)
            {
                int offset = batch * batchSize;
                updateStatus?.Invoke($"Batch: {batch + 1} of {totalBatches}.");

                var batchFiles = parquetFiles.Skip(offset).Take(batchSize).ToList();

                foreach (var filePath in batchFiles)
                {
                    string marketName = Path.GetFileNameWithoutExtension(filePath).Replace("_MarketStates", "");
                    lastRunningMarket = marketName;

                    (int Lookback, int Lookforward) patternSettings;
                    int lookback;
                    int lookForward;
                    int intervalType = 1;

                    if (marketName.Contains("_Daily"))
                    {
                        intervalType = 3;
                        patternSettings = ConfigService.GetDayPatternSettings();
                        lookback = patternSettings.Lookback;
                        lookForward = patternSettings.Lookforward;
                    }
                    else if (marketName.Contains("_Hourly"))
                    {
                        intervalType = 2;
                        patternSettings = ConfigService.GetHourPatternSettings();
                        lookback = patternSettings.Lookback;
                        lookForward = patternSettings.Lookforward;
                    }
                    else
                    {
                        patternSettings = ConfigService.GetMinutePatternSettings();
                        lookback = patternSettings.Lookback;
                        lookForward = patternSettings.Lookforward;
                    }

                    updateStatus?.Invoke($"Processing - Batch: {batch + 1} of {totalBatches}. Market: {marketName}.");

                    await Task.Run(() => patternDetector.DetectAndExportPatterns(
                        parquetFilePath: filePath,
                        outputDirectory: outputDirectory,
                        lookback, lookForward, intervalType));
                }
            }



            PatternsRunning = false;
        }

        public async Task BuildPatternConfigs(
        Action<string> updateStatus,
        bool cleanupTempConfigs = true)
        {
            ConfigsRunning = true;
            try
            {
                string outputDirectory = _configuration["Paths:OutputDirectory"];
                string tempConfigDir = Path.Combine(outputDirectory, "TempConfigs");
                if (!Directory.Exists(tempConfigDir))
                {
                    Directory.CreateDirectory(tempConfigDir);
                }

                var patternFiles = Directory.GetFiles(outputDirectory, "*_patterns.json")
                    .OrderBy(f => f)
                    .ToList();

                int totalFiles = patternFiles.Count;
                updateStatus?.Invoke($"Starting to build configs for {totalFiles} pattern files.");

                var patternDetector = new PatternDetector();

                foreach (var patternFile in patternFiles)
                {
                    string marketName = Path.GetFileNameWithoutExtension(patternFile).Replace("_patterns", "");

                    (int Lookback, int Lookforward) patternSettings;
                    int lookback;
                    int lookForward;
                    int intervalType;

                    if (marketName.Contains("_Daily"))
                    {
                        patternSettings = ConfigService.GetDayPatternSettings();
                        lookback = patternSettings.Lookback;
                        lookForward = patternSettings.Lookforward;
                        intervalType = 3; // Day
                    }
                    else if (marketName.Contains("_Hourly"))
                    {
                        patternSettings = ConfigService.GetHourPatternSettings();
                        lookback = patternSettings.Lookback;
                        lookForward = patternSettings.Lookforward;
                        intervalType = 2; // Hour
                    }
                    else
                    {
                        patternSettings = ConfigService.GetMinutePatternSettings();
                        lookback = patternSettings.Lookback;
                        lookForward = patternSettings.Lookforward;
                        intervalType = 1; // Minute
                    }

                    updateStatus?.Invoke($"Building config for {marketName} (Interval: {intervalType}).");

                    await patternDetector.BuildPatternConfig(
                        patternsFilePath: patternFile,
                        configOutputDir: tempConfigDir,
                        lookback: lookback,
                        lookforward: lookForward,
                        intervalType: intervalType);
                }

                // Merge configs by interval type
                await MergeConfigFilesByInterval(tempConfigDir, outputDirectory, cleanupTempConfigs, updateStatus);
                updateStatus?.Invoke("Config building and merging completed.");
            }
            catch (Exception ex)
            {
                updateStatus?.Invoke($"Error in BuildPatternConfigs: {ex.Message}");
                Console.WriteLine($"Error in BuildPatternConfigs: {ex}");
            }
            finally
            {
                ConfigsRunning = false;
            }
        }

        private async Task MergeConfigFilesByInterval(string tempConfigDir, string finalConfigDir, bool cleanupTempConfigs, Action<string> updateStatus)
        {
            var configFiles = Directory.GetFiles(tempConfigDir, "Config_*.json").ToList();

            if (!configFiles.Any())
            {
                updateStatus?.Invoke("No temporary config files found to merge.");
                return;
            }

            // Group configs by interval type (1, 2, 3)
            var configsByInterval = configFiles
                .GroupBy(f =>
                {
                    string fileName = Path.GetFileNameWithoutExtension(f);
                    if (fileName.Contains("_Daily")) return 3;
                    if (fileName.Contains("_Hourly")) return 2;
                    return 1; // Default to minute
                })
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var intervalGroup in configsByInterval)
            {
                int intervalType = intervalGroup.Key;
                string intervalLabel = intervalType == 1 ? "minute" : intervalType == 2 ? "hour" : "day";
                string finalConfigPath = Path.Combine(finalConfigDir, $"Config_{intervalType}{intervalLabel}_{DateTime.Now:yyyyMMdd_HHmmss}.json");

                var mergedMetrics = new Dictionary<string, (double TotalStrength, double TotalCertainty, double TotalUncertainty, int TotalCount)>();

                // Aggregate metrics across all files for this interval
                foreach (var configFile in intervalGroup.Value)
                {
                    var metrics = JsonConvert.DeserializeObject<Dictionary<string, PatternMetricWithCount>>(File.ReadAllText(configFile));
                    foreach (var kvp in metrics)
                    {
                        string patternName = kvp.Key;
                        var metric = kvp.Value;

                        if (!mergedMetrics.ContainsKey(patternName))
                        {
                            mergedMetrics[patternName] = (
                                TotalStrength: 0,
                                TotalCertainty: 0,
                                TotalUncertainty: 0,
                                TotalCount: 0
                            );
                        }

                        var existing = mergedMetrics[patternName];
                        mergedMetrics[patternName] = (
                            TotalStrength: existing.TotalStrength + (metric.Strength * metric.OccurrenceCount),
                            TotalCertainty: existing.TotalCertainty + (metric.Certainty * metric.OccurrenceCount),
                            TotalUncertainty: existing.TotalUncertainty + (metric.Uncertainty * metric.OccurrenceCount),
                            TotalCount: existing.TotalCount + metric.OccurrenceCount
                        );
                    }
                }

                var finalMetrics = mergedMetrics.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new PatternMetricWithCount
                    {
                        Strength = kvp.Value.TotalCount > 0 ? Math.Round(kvp.Value.TotalStrength / kvp.Value.TotalCount) : 0,
                        Certainty = kvp.Value.TotalCount > 0 ? Math.Round(kvp.Value.TotalCertainty / kvp.Value.TotalCount) : 0,
                        Uncertainty = kvp.Value.TotalCount > 0 ? Math.Round(kvp.Value.TotalUncertainty / kvp.Value.TotalCount) : 0,
                        OccurrenceCount = kvp.Value.TotalCount
                    });

                await File.WriteAllTextAsync(finalConfigPath, JsonConvert.SerializeObject(finalMetrics, Formatting.Indented));
                updateStatus?.Invoke($"Merged {intervalGroup.Value.Count} config files for interval {intervalType}{intervalLabel} into {finalConfigPath} with total counts.");
            }

            if (cleanupTempConfigs)
            {
                try
                {
                    Directory.Delete(tempConfigDir, true);
                    updateStatus?.Invoke("Cleaned up temporary config files.");
                }
                catch (Exception ex)
                {
                    updateStatus?.Invoke($"Failed to clean up temporary config files: {ex.Message}");
                    Console.WriteLine($"Error cleaning up temp config directory: {ex}");
                }
            }
        }

        private static void MergeConfigFiles(string tempConfigDir, string finalConfigPath, bool cleanupTempConfigs, Action<string> updateStatus)
        {
            var configFiles = Directory.GetFiles(tempConfigDir, "Config_*.json")
                .ToList();

            if (!configFiles.Any())
            {
                updateStatus?.Invoke("No temporary config files found to merge.");
                return;
            }

            var mergedMetrics = new Dictionary<string, (double Strength, double Certainty, double Uncertainty, int Count)>();

            foreach (var configFile in configFiles)
            {
                var metrics = JsonConvert.DeserializeObject<Dictionary<string, PatternMetricWithCount>>(File.ReadAllText(configFile));
                foreach (var kvp in metrics)
                {
                    if (!mergedMetrics.ContainsKey(kvp.Key))
                    {
                        mergedMetrics[kvp.Key] = (kvp.Value.Strength, kvp.Value.Certainty, kvp.Value.Uncertainty, kvp.Value.OccurrenceCount);
                    }
                    else
                    {
                        var existing = mergedMetrics[kvp.Key];
                        mergedMetrics[kvp.Key] = (
                            existing.Strength + kvp.Value.Strength,
                            existing.Certainty + kvp.Value.Certainty,
                            existing.Uncertainty + kvp.Value.Uncertainty,
                            existing.Count + kvp.Value.OccurrenceCount);
                    }
                }
            }

            var finalMetrics = mergedMetrics.ToDictionary(
                kvp => kvp.Key,
                kvp => new FinalPatternMetric
                {
                    Strength = kvp.Value.Count > 0 ? Math.Round(kvp.Value.Strength / kvp.Value.Count) : 0,
                    Certainty = kvp.Value.Count > 0 ? Math.Round(kvp.Value.Certainty / kvp.Value.Count) : 0,
                    Uncertainty = kvp.Value.Count > 0 ? Math.Round(kvp.Value.Uncertainty / kvp.Value.Count) : 0
                });

            File.WriteAllText(finalConfigPath, JsonConvert.SerializeObject(finalMetrics, Formatting.Indented));
            updateStatus?.Invoke($"Merged {configFiles.Count} config files into {finalConfigPath}.");

            if (cleanupTempConfigs)
            {
                try
                {
                    Directory.Delete(tempConfigDir, true);
                    updateStatus?.Invoke("Cleaned up temporary config files.");
                }
                catch (Exception ex)
                {
                    updateStatus?.Invoke($"Failed to clean up temporary config files: {ex.Message}");
                    Console.WriteLine($"Error cleaning up temp config directory: {ex}");
                }
            }
        }
    }
}