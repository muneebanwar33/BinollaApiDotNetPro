using System;

namespace BinollaApiDotNet.DataTypes
{
    /// <summary>
    /// Represents real-time quote data for a trading asset
    /// </summary>
    public class QuoteData
    {
        /// <summary>
        /// Currency pair or asset symbol (e.g., "EURUSD")
        /// </summary>
        public string Pair { get; set; } = string.Empty;

        /// <summary>
        /// Unix timestamp with milliseconds precision
        /// </summary>
        public double Timestamp { get; set; }

        /// <summary>
        /// Current price/quote value
        /// </summary>
        public double Price { get; set; }

        /// <summary>
        /// Additional data field (can be null or numeric)
        /// </summary>
        public object? AdditionalData { get; set; }

        /// <summary>
        /// DateTime representation of the timestamp
        /// </summary>
        public DateTime DateTime => DateTimeOffset.FromUnixTimeMilliseconds((long)(Timestamp * 1000)).DateTime;

        /// <summary>
        /// Time when this quote was received/processed
        /// </summary>
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Constructor for creating a quote from raw data array
        /// </summary>
        /// <param name="pair">Currency pair</param>
        /// <param name="timestamp">Unix timestamp</param>
        /// <param name="price">Price value</param>
        /// <param name="additionalData">Additional data</param>
        public QuoteData(string pair, double timestamp, double price, object? additionalData = null)
        {
            Pair = pair;
            Timestamp = timestamp;
            Price = price;
            AdditionalData = additionalData;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public QuoteData() { }

        /// <summary>
        /// Returns a string representation of the quote
        /// </summary>
        public override string ToString()
        {
            return $"{Pair}: {Price} @ {DateTime:HH:mm:ss.fff}";
        }
    }
} 