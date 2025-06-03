using System;
using System.Collections.Generic;

namespace BinollaApiDotNet.DataTypes
{
    /// <summary>
    /// Represents historical quote data from s_history/last message
    /// </summary>
    public class HistoryData
    {
        /// <summary>
        /// Asset symbol (e.g., "EURUSD")
        /// </summary>
        public string Asset { get; set; } = string.Empty;

        /// <summary>
        /// Time period in seconds
        /// </summary>
        public int Period { get; set; }

        /// <summary>
        /// List of historical tick data
        /// </summary>
        public List<TickData> TickHistory { get; set; } = new List<TickData>();

        /// <summary>
        /// OHLC candlestick data (if present)
        /// </summary>
        public CandlestickData? Candlestick { get; set; }

        /// <summary>
        /// List of all candlestick data from the candles array
        /// </summary>
        public List<CandlestickData> Candles { get; set; } = new List<CandlestickData>();

        /// <summary>
        /// Time when this history was received
        /// </summary>
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Returns a string representation of the history data
        /// </summary>
        public override string ToString()
        {
            return $"{Asset} ({Period}s): {TickHistory.Count} ticks" + 
                   (Candlestick != null ? " + OHLC" : "") +
                   (Candles.Count > 0 ? $" + {Candles.Count} candles" : "");
        }
    }

    /// <summary>
    /// Represents individual tick data
    /// </summary>
    public class TickData
    {
        /// <summary>
        /// Unix timestamp with milliseconds precision
        /// </summary>
        public double Timestamp { get; set; }

        /// <summary>
        /// Price at this tick
        /// </summary>
        public double Price { get; set; }

        /// <summary>
        /// Additional data (can be null or numeric)
        /// </summary>
        public object? AdditionalData { get; set; }

        /// <summary>
        /// DateTime representation of the timestamp
        /// </summary>
        public DateTime DateTime => DateTimeOffset.FromUnixTimeMilliseconds((long)(Timestamp * 1000)).DateTime;

        /// <summary>
        /// Constructor for creating tick data
        /// </summary>
        public TickData(double timestamp, double price, object? additionalData = null)
        {
            Timestamp = timestamp;
            Price = price;
            AdditionalData = additionalData;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TickData() { }

        /// <summary>
        /// Returns a string representation of the tick
        /// </summary>
        public override string ToString()
        {
            return $"{Price:F5} @ {DateTime:HH:mm:ss.fff}";
        }
    }

    /// <summary>
    /// Represents OHLC candlestick data
    /// </summary>
    public class CandlestickData
    {
        /// <summary>
        /// Candlestick timestamp
        /// </summary>
        public double Timestamp { get; set; }

        /// <summary>
        /// Opening price
        /// </summary>
        public double Open { get; set; }

        /// <summary>
        /// Lowest price
        /// </summary>
        public double Low { get; set; }

        /// <summary>
        /// Highest price
        /// </summary>
        public double High { get; set; }

        /// <summary>
        /// Closing price
        /// </summary>
        public double Close { get; set; }

        /// <summary>
        /// Volume (if available)
        /// </summary>
        public double? Volume { get; set; }

        /// <summary>
        /// End timestamp (if different from start)
        /// </summary>
        public double? EndTimestamp { get; set; }

        /// <summary>
        /// DateTime representation of the timestamp
        /// </summary>
        public DateTime DateTime => DateTimeOffset.FromUnixTimeMilliseconds((long)(Timestamp * 1000)).DateTime;

        /// <summary>
        /// DateTime representation of end timestamp
        /// </summary>
        public DateTime? EndDateTime => EndTimestamp.HasValue 
            ? DateTimeOffset.FromUnixTimeMilliseconds((long)(EndTimestamp.Value * 1000)).DateTime 
            : null;

        /// <summary>
        /// Constructor for creating candlestick data
        /// </summary>
        public CandlestickData(double timestamp, double open, double low, double high, double close, double? volume = null, double? endTimestamp = null)
        {
            Timestamp = timestamp;
            Open = open;
            Low = low;
            High = high;
            Close = close;
            Volume = volume;
            EndTimestamp = endTimestamp;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public CandlestickData() { }

        /// <summary>
        /// Returns a string representation of the candlestick
        /// </summary>
        public override string ToString()
        {
            return $"OHLC: {Open:F5}/{High:F5}/{Low:F5}/{Close:F5} @ {DateTime:HH:mm:ss}";
        }
    }
} 