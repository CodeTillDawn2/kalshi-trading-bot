using System.Text;

namespace BacklashBot.Helpers
{
    /// <summary>
    /// Provides static utility methods for various trading calculations, including technical indicators, position metrics, and mathematical operations.
    /// This class is used throughout the trading bot for computing EMA, liquidation prices, fees, ROI, and other essential calculations.
    /// All methods are designed to be thread-safe and efficient for real-time trading operations.
    /// </summary>
    public static class TradingCalculations
    {

        /// <summary>
        /// Calculates the Exponential Moving Average (EMA) for a list of prices over a specified period.
        /// EMA gives more weight to recent prices, making it responsive to price changes.
        /// </summary>
        /// <param name="prices">The list of price values.</param>
        /// <param name="period">The period for the EMA calculation.</param>
        /// <param name="previousEMA">Optional previous EMA value for incremental calculation.</param>
        /// <returns>The calculated EMA value or null if insufficient data or invalid result.</returns>
        public static double? CalculateEMA(List<double> prices, int period, double? previousEMA = null)
        {
            var log = new StringBuilder();
            if (period < 1)
            {
                throw new ArgumentException("Period must be at least 1.", nameof(period));
            }
            if (prices == null || prices.Count < period)
            {
                log.AppendLine($"Insufficient data. Prices: {prices?.Count ?? 0}, Required: {period}.");
                return null;
            }
            double multiplier = 2.0 / (period + 1);
            log.AppendLine($"Multiplier: {multiplier}");
            double result;
            if (previousEMA == null)
            {
                result = prices.Take(period).Average();
                log.AppendLine($"Initial EMA: {result}");
                // Iterate from the second price onward
                for (int i = period; i < prices.Count; i++)
                {
                    result = (prices[i] * multiplier) + (result * (1 - multiplier));
                }
            }
            else
            {
                result = (prices.Last() * multiplier) + (previousEMA.Value * (1 - multiplier));
                log.AppendLine($"CurrentPrice: {prices.Last()}, PreviousEMA: {previousEMA}, EMA: {result}");
            }
            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                log.AppendLine($"Invalid EMA: {result}.");
                return null;
            }
            log.AppendLine($"Final EMA: {result}");
            return result;
        }

        /// <summary>
        /// Calculates the Exponential Moving Average (EMA) iteratively up to a specified end index.
        /// This method computes EMA for a subset of prices ending at the given index.
        /// </summary>
        /// <param name="prices">The list of price values.</param>
        /// <param name="period">The period for the EMA calculation.</param>
        /// <param name="endIndex">The end index in the prices list to calculate EMA up to.</param>
        /// <returns>The calculated EMA value.</returns>
        public static double CalculateIterativeEMA(List<double> prices, int period, int endIndex)
        {

            if (endIndex < period - 1 || prices.Count <= endIndex)
            {
                throw new ArgumentException("Insufficient data");
            }

            double multiplier = 2.0 / (period + 1);

            // Calculate initial SMA for the first period prices
            var initialPrices = prices.Take(period).ToList();
            double ema = initialPrices.Average();

            // Iterate over all prices from period to endIndex
            for (int i = period; i <= endIndex; i++)
            {
                ema = (prices[i] * multiplier) + (ema * (1 - multiplier));
            }

            return ema;
        }

        /// <summary>
        /// Truncates a DateTime to the minute level, setting seconds and milliseconds to zero.
        /// </summary>
        /// <param name="dt">The DateTime to truncate.</param>
        /// <returns>A new DateTime with seconds and milliseconds set to zero, in UTC.</returns>
        public static DateTime TruncateToMinute(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, DateTimeKind.Utc);
        }

        /// <summary>
        /// Generates a Gaussian kernel for smoothing the price frequency distribution.
        /// </summary>
        /// <param name="sigma">Standard deviation of the Gaussian function, in cents.</param>
        /// <returns>An array representing the normalized Gaussian kernel.</returns>
        public static double[] GenerateGaussianKernel(double sigma)
        {
            int size = (int)Math.Ceiling(3 * sigma) * 2 + 1;
            double[] kernel = new double[size];
            double sum = 0;
            int center = size / 2;
            for (int i = 0; i < size; i++)
            {
                double x = i - center;
                kernel[i] = Math.Exp(-0.5 * (x * x) / (sigma * sigma));
                sum += kernel[i];
            }
            for (int i = 0; i < size; i++)
            {
                kernel[i] /= sum;
            }
            return kernel;
        }

        /// <summary>
        /// Convolves the input array with a kernel to produce a smoothed output.
        /// </summary>
        /// <param name="input">The input array to be smoothed (e.g., price frequencies).</param>
        /// <param name="kernel">The convolution kernel (e.g., Gaussian kernel).</param>
        /// <returns>The smoothed array of the same length as the input.</returns>
        public static double[] Convolve(double[] input, double[] kernel)
        {
            int n = input.Length;
            int k = kernel.Length;
            int pad = k / 2;
            double[] paddedInput = new double[n + 2 * pad];
            for (int i = 0; i < pad; i++)
            {
                paddedInput[i] = input[pad - i];
                paddedInput[n + pad + i] = input[n - 1 - i];
            }
            for (int i = 0; i < n; i++)
            {
                paddedInput[pad + i] = input[i];
            }
            double[] output = new double[n];
            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                for (int j = 0; j < k; j++)
                {
                    sum += paddedInput[i + j] * kernel[j];
                }
                output[i] = sum;
            }
            return output;
        }

        /// <summary>
        /// Identifies indices of local maxima in the input array.
        /// </summary>
        /// <param name="data">The input array to search for maxima.</param>
        /// <returns>A list of indices where the array has local maxima.</returns>
        public static List<int> FindLocalMaxima(double[] data)
        {
            var maxima = new List<int>();
            for (int i = 1; i < data.Length - 1; i++)
            {
                if (data[i] > data[i - 1] && data[i] > data[i + 1])
                {
                    maxima.Add(i);
                }
            }
            return maxima;
        }

        /// <summary>
        /// Calculates the buy-in price from market exposure and position size.
        /// </summary>
        /// <param name="marketExposure">Total market exposure in dollars.</param>
        /// <param name="positionSize">Number of contracts (positive for Yes, negative for No).</param>
        /// <returns>Buy-in price per contract, rounded to 2 decimal places.</returns>
        public static double CalculateBuyinPrice(double marketExposure, int positionSize)
        {
            return Math.Abs(positionSize) != 0 ? Math.Round(Math.Abs(marketExposure) / Math.Abs(positionSize), 2) : 0;
        }

        /// <summary>
        /// Calculates the liquidation price for a position based on orderbook levels.
        /// </summary>
        /// <param name="positionSize">Number of contracts to liquidate (positive for Yes, negative for No).</param>
        /// <param name="orderbookLevels">List of (Price, Quantity) tuples, sorted by price descending.</param>
        /// <param name="currentPrice">Fallback price (in cents) if orderbook is empty (Yes bid for Yes position, 100 - Yes ask for No position).</param>
        /// <returns>Liquidation price per contract in dollars.</returns>
        public static double CalculateLiquidationPrice(int positionSize, List<(int Price, int Quantity)> orderbookLevels)
        {
            if (positionSize == 0) return 0;

            double sharesToLiquidate = Math.Abs(positionSize);
            double totalValue = 0;
            double remainingShares = sharesToLiquidate;

            if (orderbookLevels.Count > 0)
            {
                foreach (var (price, quantity) in orderbookLevels.OrderByDescending(x => x.Price))
                {
                    if (remainingShares <= 0) break;
                    double pricePerShare = price / 100.0;
                    double sharesAtThisPrice = Math.Min(remainingShares, quantity);
                    totalValue += sharesAtThisPrice * pricePerShare;
                    remainingShares -= sharesAtThisPrice;
                }
                return remainingShares > 0 ? totalValue / (sharesToLiquidate - remainingShares) : totalValue / sharesToLiquidate;
            }
            return 0;
        }

        /// <summary>
        /// Calculates trading fees for a given number of contracts at a price.
        /// </summary>
        /// <param name="contracts">Number of contracts.</param>
        /// <param name="priceInDollars">Price per contract in dollars.</param>
        /// <returns>Fees in dollars, rounded up to the next cent.</returns>
        public static double CalculateTradingFees(int contracts, double priceInDollars)
        {
            double fee = 0.07 * contracts * priceInDollars * (1 - priceInDollars);
            return Math.Ceiling(fee * 100) / 100; // Round up to the next cent
        }

        /// <summary>
        /// Calculates the expected fees for liquidating a position.
        /// </summary>
        /// <param name="positionSize">Number of contracts to liquidate.</param>
        /// <param name="orderbookLevels">List of (Price, Quantity) tuples, sorted by price descending.</param>
        /// <param name="currentPrice">Fallback price (in cents) if orderbook is empty.</param>
        /// <returns>Total expected fees in dollars, rounded to 2 decimal places.</returns>
        public static double CalculateExpectedFees(int positionSize, List<(int Price, int Quantity)> orderbookLevels)
        {
            if (positionSize == 0) return 0;

            double sharesToLiquidate = Math.Abs(positionSize);
            double totalFees = 0;
            double remainingShares = sharesToLiquidate;

            if (orderbookLevels.Count > 0)
            {
                foreach (var (price, quantity) in orderbookLevels)
                {
                    if (remainingShares <= 0) break;
                    double pricePerShare = price / 100.0;
                    double sharesAtThisPrice = Math.Min(remainingShares, quantity);
                    totalFees += CalculateTradingFees((int)sharesAtThisPrice, pricePerShare);
                    remainingShares -= sharesAtThisPrice;
                }
                return Math.Round(totalFees, 2);
            }
            return 0;
        }

        /// <summary>
        /// Calculates the ROI amount and percentage for a position.
        /// </summary>
        /// <param name="positionSize">Number of contracts.</param>
        /// <param name="liquidationPrice">Price at which the position can be liquidated (in dollars).</param>
        /// <param name="buyinPrice">Buy-in price per contract (in dollars).</param>
        /// <param name="expectedFees">Total expected fees in dollars.</param>
        /// <returns>(ROI Amount, ROI Percentage) tuple, both rounded to 2 decimal places.</returns>
        public static (double ROIAmount, double ROIPercentage) CalculateROI(int positionSize, double liquidationPrice, double buyinPrice, double expectedFees)
        {
            if (positionSize == 0) return (0, 0);

            double sharesToLiquidate = Math.Abs(positionSize);
            double cost = buyinPrice * sharesToLiquidate;
            double profitPerShare = liquidationPrice - buyinPrice;
            double roiAmount = Math.Round((profitPerShare * sharesToLiquidate) - expectedFees, 2);
            double roiPercentage = buyinPrice != 0 ? Math.Round((roiAmount / cost) * 100, 2) : 0;
            return (roiAmount, roiPercentage);
        }

        /// <summary>
        /// Calculates the upside and downside potential for a position.
        /// </summary>
        /// <param name="positionSize">Number of contracts (positive for Yes, negative for No).</param>
        /// <param name="liquidationPrice">Price at which the position can be liquidated (in dollars).</param>
        /// <param name="expectedFees">Total expected fees in dollars.</param>
        /// <returns>(Upside, Downside) tuple, both rounded to 2 decimal places.</returns>
        public static (double Upside, double Downside) CalculateUpsideDownside(int positionSize, double liquidationPrice, double expectedFees)
        {
            if (positionSize == 0) return (0, 0);

            double sharesToLiquidate = Math.Abs(positionSize);
            double totalPayout = 1.00 * sharesToLiquidate; // $1 per contract
            double currentValue = liquidationPrice * sharesToLiquidate;
            double upside = Math.Round(totalPayout - currentValue - expectedFees, 2);
            double downside = Math.Round(-(currentValue + expectedFees), 2);
            return (upside, downside);
        }
    }
}
