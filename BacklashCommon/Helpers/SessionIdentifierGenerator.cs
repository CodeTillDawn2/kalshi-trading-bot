using System.Security.Cryptography;

namespace BacklashCommon.Helpers
{
    /// <summary>
    /// Utility class for generating unique session identifiers.
    /// </summary>
    public static class SessionIdentifierGenerator
    {
        /// <summary>
        /// Generates a random session identifier of the specified length.
        /// </summary>
        /// <param name="length">The length of the identifier to generate. Default is 5.</param>
        /// <returns>A string containing the generated session identifier.</returns>
        public static string GenerateSessionIdentifier(int length = 5)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            // Use more bytes for better entropy
            var data = new byte[length + 8]; // Extra 8 bytes for timestamp
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(data);
            }
            // Incorporate timestamp for additional entropy
            var timestamp = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
            for (int i = 0; i < Math.Min(8, data.Length); i++)
            {
                data[i] ^= timestamp[i];
            }
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[data[i] % chars.Length];
            }
            return new string(result);
        }
    }
}