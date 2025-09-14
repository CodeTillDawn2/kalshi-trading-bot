using System.Text;
using System.Threading.Tasks;

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
        /// Includes validation for null prices, invalid period, and NaN/Infinity values.
        /// </summary>
        /// <param name="prices">The list of price values. Must not be null, contain at least 'period' elements, and not contain NaN or Infinity.</param>
        /// <param name="period">The period for the EMA calculation. Must be at least 1.</param>
        /// <param name="previousEMA">Optional previous EMA value for incremental calculation.</param>
        /// <returns>The calculated EMA value or null if insufficient data, invalid inputs, or invalid result.</returns>
        public static double? CalculateEMA(List<double> prices, int period, double? previousEMA = null)
        {
            if (period < 1)
            {
                throw new ArgumentException("Period must be at least 1.", nameof(period));
            }
            if (prices == null)
            {
                return null;
            }
            if (prices.Count < period)
            {
                return null;
            }
            if (prices.Any(double.IsNaN) || prices.Any(double.IsInfinity))
            {
                return null;
            }
            double multiplier = 2.0 / (period + 1);
            double result;
            if (previousEMA == null)
            {
                result = prices.Take(period).Average();
                // Iterate from the second price onward
                for (int i = period; i < prices.Count; i++)
                {
                    result = (prices[i] * multiplier) + (result * (1 - multiplier));
                }
            }
            else
            {
                result = (prices.Last() * multiplier) + (previousEMA.Value * (1 - multiplier));
            }
            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                return null;
            }
            return result;
        }

        /// <summary>
        /// Asynchronously calculates the Exponential Moving Average (EMA) for a list of prices over a specified period.
        /// Useful for high-frequency scenarios where computation can be offloaded.
        /// </summary>
        /// <param name="prices">The list of price values.</param>
        /// <param name="period">The period for the EMA calculation.</param>
        /// <param name="previousEMA">Optional previous EMA value for incremental calculation.</param>
        /// <returns>A task that represents the asynchronous operation, containing the EMA value or null.</returns>
        public static async Task<double?> CalculateEMAAsync(List<double> prices, int period, double? previousEMA = null)
        {
            return await Task.Run(() => CalculateEMA(prices, period, previousEMA));
        }

        /// <summary>
        /// Calculates the Exponential Moving Average (EMA) iteratively up to a specified end index.
        /// This method computes EMA for a subset of prices ending at the given index.
        /// Includes validation for null prices, invalid period, indices, and NaN/Infinity values.
        /// </summary>
        /// <param name="prices">The list of price values. Must not be null, contain valid indices, and not contain NaN or Infinity.</param>
        /// <param name="period">The period for the EMA calculation. Must be at least 1.</param>
        /// <param name="endIndex">The end index in the prices list to calculate EMA up to. Must be >= period-1 and &lt; prices.Count.</param>
        /// <returns>The calculated EMA value.</returns>
        /// <exception cref="ArgumentException">Thrown if inputs are invalid.</exception>
        public static double CalculateIterativeEMA(List<double> prices, int period, int endIndex)
        {
            if (prices == null)
            {
                throw new ArgumentNullException(nameof(prices));
            }
            if (period < 1)
            {
                throw new ArgumentException("Period must be at least 1.", nameof(period));
            }
            if (endIndex < period - 1 || prices.Count <= endIndex)
            {
                throw new ArgumentException("Invalid indices for EMA calculation.");
            }
            if (prices.Any(double.IsNaN) || prices.Any(double.IsInfinity))
            {
                throw new ArgumentException("Prices contain invalid values.");
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
        /// Asynchronously calculates the Exponential Moving Average (EMA) iteratively up to a specified end index.
        /// Useful for high-frequency scenarios where computation can be offloaded.
        /// </summary>
        /// <param name="prices">The list of price values.</param>
        /// <param name="period">The period for the EMA calculation.</param>
        /// <param name="endIndex">The end index in the prices list to calculate EMA up to.</param>
        /// <returns>A task that represents the asynchronous operation, containing the EMA value.</returns>
        public static async Task<double> CalculateIterativeEMAAsync(List<double> prices, int period, int endIndex)
        {
            return await Task.Run(() => CalculateIterativeEMA(prices, period, endIndex));
        }

        /// <summary>
        /// Truncates a DateTime to the minute level, setting seconds and milliseconds to zero.
        /// Includes validation to ensure the DateTime is valid.
        /// </summary>
        /// <param name="dt">The DateTime to truncate. Must be a valid DateTime.</param>
        /// <returns>A new DateTime with seconds and milliseconds set to zero, in UTC.</returns>
        /// <exception cref="ArgumentException">Thrown if the DateTime is invalid.</exception>
        public static DateTime TruncateDateTimeToMinute(DateTime dt)
        {
            if (dt == DateTime.MinValue || dt == DateTime.MaxValue || dt.Kind == DateTimeKind.Unspecified)
            {
                throw new ArgumentException("Invalid DateTime provided.", nameof(dt));
            }
            var result = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, DateTimeKind.Utc);
            return result;
        }

        /// <summary>
        /// Generates a Gaussian kernel for smoothing the price frequency distribution.
        /// Includes validation for sigma to ensure positive value.
        /// </summary>
        /// <param name="sigma">Standard deviation of the Gaussian function, in cents. Must be positive.</param>
        /// <returns>An array representing the normalized Gaussian kernel.</returns>
        /// <exception cref="ArgumentException">Thrown if sigma is not positive.</exception>
        public static double[] GenerateGaussianKernel(double sigma)
        {
            if (sigma <= 0 || double.IsNaN(sigma) || double.IsInfinity(sigma))
            {
                throw new ArgumentException("Sigma must be a positive finite number.", nameof(sigma));
            }
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
        /// Includes validation for null arrays and invalid values.
        /// </summary>
        /// <param name="input">The input array to be smoothed (e.g., price frequencies). Must not be null or empty, and not contain NaN or Infinity.</param>
        /// <param name="kernel">The convolution kernel (e.g., Gaussian kernel). Must not be null or empty, and not contain NaN or Infinity.</param>
        /// <returns>The smoothed array of the same length as the input.</returns>
        /// <exception cref="ArgumentException">Thrown if inputs are invalid.</exception>
        public static double[] Convolve(double[] input, double[] kernel)
        {
            if (input == null || input.Length == 0)
            {
                throw new ArgumentException("Input array must not be null or empty.", nameof(input));
            }
            if (kernel == null || kernel.Length == 0)
            {
                throw new ArgumentException("Kernel array must not be null or empty.", nameof(kernel));
            }
            if (input.Any(double.IsNaN) || input.Any(double.IsInfinity) || kernel.Any(double.IsNaN) || kernel.Any(double.IsInfinity))
            {
                throw new ArgumentException("Input and kernel must not contain NaN or Infinity.");
            }
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
        /// Asynchronously convolves the input array with a kernel to produce a smoothed output.
        /// Useful for high-frequency scenarios where computation can be offloaded.
        /// </summary>
        /// <param name="input">The input array to be smoothed.</param>
        /// <param name="kernel">The convolution kernel.</param>
        /// <returns>A task that represents the asynchronous operation, containing the smoothed array.</returns>
        public static async Task<double[]> ConvolveAsync(double[] input, double[] kernel)
        {
            return await Task.Run(() => Convolve(input, kernel));
        }

        /// <summary>
        /// Identifies indices of local maxima in the input array.
        /// Includes validation for null array and invalid values.
        /// </summary>
        /// <param name="data">The input array to search for maxima. Must not be null or empty, and not contain NaN or Infinity.</param>
        /// <returns>A list of indices where the array has local maxima.</returns>
        /// <exception cref="ArgumentException">Thrown if data is invalid.</exception>
        public static List<int> FindLocalMaxima(double[] data)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Data array must not be null or empty.", nameof(data));
            }
            if (data.Any(double.IsNaN) || data.Any(double.IsInfinity))
            {
                throw new ArgumentException("Data array must not contain NaN or Infinity.");
            }
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
        /// Asynchronously identifies indices of local maxima in the input array.
        /// Useful for high-frequency scenarios where computation can be offloaded.
        /// </summary>
        /// <param name="data">The input array to search for maxima.</param>
        /// <returns>A task that represents the asynchronous operation, containing the list of indices.</returns>
        public static async Task<List<int>> FindLocalMaximaAsync(double[] data)
        {
            return await Task.Run(() => FindLocalMaxima(data));
        }

        /// <summary>
        /// Calculates the buy-in price from market exposure and position size.
        /// Includes validation for positionSize and logging.
        /// </summary>
        /// <param name="marketExposure">Total market exposure in dollars. Can be negative.</param>
        /// <param name="positionSize">Number of contracts (positive for Yes, negative for No). Must not be zero.</param>
        /// <returns>Buy-in price per contract, rounded to 2 decimal places. Returns 0 if positionSize is zero.</returns>
        /// <exception cref="ArgumentException">Thrown if positionSize is zero.</exception>
        public static double CalculateBuyinPrice(double marketExposure, int positionSize)
        {
            if (positionSize == 0)
            {
                return 0;
            }
            if (double.IsNaN(marketExposure) || double.IsInfinity(marketExposure))
            {
                throw new ArgumentException("Market exposure must be a finite number.", nameof(marketExposure));
            }
            double result = Math.Round(Math.Abs(marketExposure) / Math.Abs(positionSize), 2);
            return result;
        }

        /// <summary>
        /// Calculates the liquidation price for a position based on orderbook levels.
        /// Includes validation for null orderbook and invalid prices/quantities.
        /// </summary>
        /// <param name="positionSize">Number of contracts to liquidate (positive for Yes, negative for No).</param>
        /// <param name="orderbookLevels">List of (Price, Quantity) tuples, sorted by price descending. Must not be null, prices and quantities must be positive.</param>
        /// <returns>Liquidation price per contract in dollars. Returns 0 if positionSize is zero or orderbook is empty/invalid.</returns>
        /// <exception cref="ArgumentException">Thrown if orderbookLevels contain invalid values.</exception>
        public static double CalculateLiquidationPrice(int positionSize, List<(int Price, int Quantity)> orderbookLevels)
        {
            if (positionSize == 0)
            {
                return 0;
            }
            if (orderbookLevels == null)
            {
                return 0;
            }
            if (orderbookLevels.Any(x => x.Price <= 0 || x.Quantity <= 0))
            {
                throw new ArgumentException("Orderbook levels must have positive prices and quantities.");
            }

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
                double result = remainingShares > 0 ? totalValue / (sharesToLiquidate - remainingShares) : totalValue / sharesToLiquidate;
                return result;
            }
            return 0;
        }

        /// <summary>
        /// Calculates trading fees for a given number of contracts at a price.
        /// Includes validation for inputs and logging.
        /// </summary>
        /// <param name="contracts">Number of contracts. Must be positive.</param>
        /// <param name="priceInDollars">Price per contract in dollars. Must be between 0 and 1.</param>
        /// <returns>Fees in dollars, rounded up to the next cent.</returns>
        /// <exception cref="ArgumentException">Thrown if inputs are invalid.</exception>
        public static double CalculateTradingFees(int contracts, double priceInDollars)
        {
            if (contracts <= 0)
            {
                throw new ArgumentException("Contracts must be positive.", nameof(contracts));
            }
            if (priceInDollars < 0 || priceInDollars > 1 || double.IsNaN(priceInDollars) || double.IsInfinity(priceInDollars))
            {
                throw new ArgumentException("Price must be a finite number between 0 and 1.", nameof(priceInDollars));
            }
            double fee = 0.07 * contracts * priceInDollars * (1 - priceInDollars);
            double result = Math.Ceiling(fee * 100) / 100;
            return result;
        }

        /// <summary>
        /// Calculates the expected fees for liquidating a position.
        /// Includes validation for null orderbook and invalid values.
        /// </summary>
        /// <param name="positionSize">Number of contracts to liquidate.</param>
        /// <param name="orderbookLevels">List of (Price, Quantity) tuples, sorted by price descending. Must not be null, prices and quantities must be positive.</param>
        /// <returns>Total expected fees in dollars, rounded to 2 decimal places. Returns 0 if positionSize is zero or orderbook is empty/invalid.</returns>
        /// <exception cref="ArgumentException">Thrown if orderbookLevels contain invalid values.</exception>
        public static double CalculateExpectedFees(int positionSize, List<(int Price, int Quantity)> orderbookLevels)
        {
            if (positionSize == 0)
            {
                return 0;
            }
            if (orderbookLevels == null)
            {
                return 0;
            }
            if (orderbookLevels.Any(x => x.Price <= 0 || x.Quantity <= 0))
            {
                throw new ArgumentException("Orderbook levels must have positive prices and quantities.");
            }

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
                double result = Math.Round(totalFees, 2);
                return result;
            }
            return 0;
        }

        /// <summary>
        /// Calculates the ROI amount and percentage for a position.
        /// Includes validation for prices and fees.
        /// </summary>
        /// <param name="positionSize">Number of contracts.</param>
        /// <param name="liquidationPrice">Price at which the position can be liquidated (in dollars). Must be finite.</param>
        /// <param name="buyinPrice">Buy-in price per contract (in dollars). Must be positive and finite.</param>
        /// <param name="expectedFees">Total expected fees in dollars. Must be finite.</param>
        /// <returns>(ROI Amount, ROI Percentage) tuple, both rounded to 2 decimal places. Returns (0,0) if positionSize is zero.</returns>
        /// <exception cref="ArgumentException">Thrown if inputs are invalid.</exception>
        public static (double ROIAmount, double ROIPercentage) CalculateROI(int positionSize, double liquidationPrice, double buyinPrice, double expectedFees)
        {
            if (positionSize == 0)
            {
                return (0, 0);
            }
            if (double.IsNaN(liquidationPrice) || double.IsInfinity(liquidationPrice) ||
                double.IsNaN(buyinPrice) || double.IsInfinity(buyinPrice) || buyinPrice <= 0 ||
                double.IsNaN(expectedFees) || double.IsInfinity(expectedFees))
            {
                throw new ArgumentException("Prices and fees must be finite numbers, buyinPrice must be positive.");
            }

            double sharesToLiquidate = Math.Abs(positionSize);
            double cost = buyinPrice * sharesToLiquidate;
            double profitPerShare = liquidationPrice - buyinPrice;
            double roiAmount = Math.Round((profitPerShare * sharesToLiquidate) - expectedFees, 2);
            double roiPercentage = Math.Round((roiAmount / cost) * 100, 2);
            return (roiAmount, roiPercentage);
        }

        /// <summary>
        /// Calculates the upside and downside potential for a position.
        /// Includes validation for prices and fees.
        /// </summary>
        /// <param name="positionSize">Number of contracts (positive for Yes, negative for No).</param>
        /// <param name="liquidationPrice">Price at which the position can be liquidated (in dollars). Must be finite.</param>
        /// <param name="expectedFees">Total expected fees in dollars. Must be finite.</param>
        /// <returns>(Upside, Downside) tuple, both rounded to 2 decimal places. Returns (0,0) if positionSize is zero.</returns>
        /// <exception cref="ArgumentException">Thrown if inputs are invalid.</exception>
        public static (double Upside, double Downside) CalculateUpsideDownside(int positionSize, double liquidationPrice, double expectedFees)
        {
            if (positionSize == 0)
            {
                return (0, 0);
            }
            if (double.IsNaN(liquidationPrice) || double.IsInfinity(liquidationPrice) ||
                double.IsNaN(expectedFees) || double.IsInfinity(expectedFees))
            {
                throw new ArgumentException("Prices and fees must be finite numbers.");
            }

            double sharesToLiquidate = Math.Abs(positionSize);
            double totalPayout = 1.00 * sharesToLiquidate; // $1 per contract
            double currentValue = liquidationPrice * sharesToLiquidate;
            double upside = Math.Round(totalPayout - currentValue - expectedFees, 2);
            double downside = Math.Round(-(currentValue + expectedFees), 2);
            return (upside, downside);
        }
    }
}
