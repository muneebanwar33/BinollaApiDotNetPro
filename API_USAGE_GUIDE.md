# BinollaApiClient - Quote and Historical Data API Guide

This document provides comprehensive examples for using the quote and historical data methods available in the `BinollaApiClient`.

## Overview

The `BinollaApiClient` now provides access to:
- **Real-time Quote Data**: Current prices, latest quotes, quote history
- **Historical Data**: Tick data, candlestick data, OHLC information
- **Secure Access**: All data access is properly authorized and validated

All methods return `ApiResult<T>` objects with comprehensive error handling and status information.

## Table of Contents

1. [Quote Data Methods](#quote-data-methods)
2. [Historical Data Methods](#historical-data-methods)
3. [Usage Examples](#usage-examples)
4. [Error Handling](#error-handling)
5. [Best Practices](#best-practices)

## Quote Data Methods

### 1. GetLatestQuote(string pair)
Gets the most recent quote for a trading pair.

```csharp
var api = new BinollaApiClient("your_ssid");
await api.Connect();

var quoteResult = api.GetLatestQuote("EURUSD");
if (quoteResult.IsSuccess)
{
    var quote = quoteResult.Data;
    Console.WriteLine($"EURUSD: {quote.Price:F5} @ {quote.DateTime:HH:mm:ss.fff}");
}
else
{
    Console.WriteLine($"Error: {quoteResult.ErrorMessage}");
}
```

### 2. GetAllLatestQuotes()
Gets all available latest quotes.

```csharp
var quotesResult = api.GetAllLatestQuotes();
if (quotesResult.IsSuccess)
{
    foreach (var kvp in quotesResult.Data)
    {
        var symbol = kvp.Key;
        var quote = kvp.Value;
        Console.WriteLine($"{symbol}: {quote.Price:F5}");
    }
}
```

### 3. GetRecentQuotes(int count)
Gets recent quote history.

```csharp
var recentQuotesResult = api.GetRecentQuotes(50);
if (recentQuotesResult.IsSuccess)
{
    var quotes = recentQuotesResult.Data.ToList();
    Console.WriteLine($"Retrieved {quotes.Count} recent quotes");
    
    foreach (var quote in quotes.Take(10))
    {
        Console.WriteLine($"{quote.Pair}: {quote.Price:F5} @ {quote.DateTime:HH:mm:ss}");
    }
}
```

### 4. GetCurrentPrice(string pair)
Gets just the current price for a trading pair.

```csharp
var priceResult = api.GetCurrentPrice("EURUSD");
if (priceResult.IsSuccess)
{
    Console.WriteLine($"EURUSD Current Price: {priceResult.Data:F5}");
}
```

### 5. IsQuoteRecent(string pair)
Checks if quote data is fresh (within 30 seconds).

```csharp
var isRecentResult = api.IsQuoteRecent("EURUSD");
if (isRecentResult.IsSuccess)
{
    var status = isRecentResult.Data ? "FRESH" : "STALE";
    Console.WriteLine($"EURUSD quote status: {status}");
}
```

## Historical Data Methods

### 1. GetHistoricalData(string asset, int period)
Gets complete historical data including ticks and candles.

```csharp
var historyResult = api.GetHistoricalData("AUDCHF_otc", 60);
if (historyResult.IsSuccess)
{
    var history = historyResult.Data;
    
    Console.WriteLine($"Asset: {history.Asset}");
    Console.WriteLine($"Period: {history.Period} seconds");
    Console.WriteLine($"Tick Data: {history.TickHistory.Count} points");
    Console.WriteLine($"Candles: {history.Candles.Count} candles");
    Console.WriteLine($"Received: {history.ReceivedAt:yyyy-MM-dd HH:mm:ss}");
}
```

### 2. GetAllHistoricalData()
Gets all available historical datasets.

```csharp
var allHistoryResult = api.GetAllHistoricalData();
if (allHistoryResult.IsSuccess)
{
    Console.WriteLine($"Total datasets: {allHistoryResult.Data.Count}");
    
    foreach (var kvp in allHistoryResult.Data)
    {
        var key = kvp.Key;
        var history = kvp.Value;
        Console.WriteLine($"{key}: {history}");
    }
}
```

### 3. GetHistoricalDataForAsset(string asset)
Gets all historical data for a specific asset across different periods.

```csharp
var assetHistoryResult = api.GetHistoricalDataForAsset("EURUSD");
if (assetHistoryResult.IsSuccess)
{
    foreach (var history in assetHistoryResult.Data)
    {
        Console.WriteLine($"EURUSD {history.Period}s: {history.TickHistory.Count} ticks, {history.Candles.Count} candles");
    }
}
```

### 4. GetTickData(string asset, int period)
Gets just the tick data for an asset.

```csharp
var tickDataResult = api.GetTickData("AUDCHF_otc", 60);
if (tickDataResult.IsSuccess)
{
    var ticks = tickDataResult.Data;
    Console.WriteLine($"Retrieved {ticks.Count} tick data points");
    
    // Show last 5 ticks
    foreach (var tick in ticks.TakeLast(5))
    {
        Console.WriteLine($"  {tick.DateTime:HH:mm:ss.fff}: {tick.Price:F5}");
    }
}
```

### 5. GetCandlestickData(string asset, int period)
Gets candlestick/OHLC data for an asset.

```csharp
var candleDataResult = api.GetCandlestickData("AUDCHF_otc", 60);
if (candleDataResult.IsSuccess)
{
    var candles = candleDataResult.Data;
    Console.WriteLine($"Retrieved {candles.Count} candlesticks");
    
    // Show last 3 candles
    foreach (var candle in candles.TakeLast(3))
    {
        Console.WriteLine($"  {candle.DateTime:HH:mm:ss} OHLC: {candle.Open:F5}/{candle.High:F5}/{candle.Low:F5}/{candle.Close:F5}");
        if (candle.Volume.HasValue)
            Console.WriteLine($"    Volume: {candle.Volume:F0}");
    }
}
```

### 6. GetLatestCandlestick(string asset, int period)
Gets just the most recent candlestick.

```csharp
var latestCandleResult = api.GetLatestCandlestick("AUDCHF_otc", 60);
if (latestCandleResult.IsSuccess)
{
    var candle = latestCandleResult.Data;
    
    Console.WriteLine("Latest Candlestick:");
    Console.WriteLine($"  Time: {candle.DateTime:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine($"  Open:  {candle.Open:F5}");
    Console.WriteLine($"  High:  {candle.High:F5}");
    Console.WriteLine($"  Low:   {candle.Low:F5}");
    Console.WriteLine($"  Close: {candle.Close:F5}");
    
    if (candle.Volume.HasValue)
        Console.WriteLine($"  Volume: {candle.Volume:F0}");
}
```

## Usage Examples

### Complete Trading Analysis Example

```csharp
using BinollaApiDotNet;
using BinollaApiDotNet.DataTypes;

class Program
{
    static async Task Main(string[] args)
    {
        var api = new BinollaApiClient("your_ssid_here");
        
        // Connect to the API
        var connectResult = api.Connect();
        if (!connectResult.IsSuccess)
        {
            Console.WriteLine($"Connection failed: {connectResult.ErrorMessage}");
            return;
        }
        
        Console.WriteLine("Connected successfully!");
        
        // Get current price for trading decision
        await AnalyzeAsset(api, "EURUSD");
        await AnalyzeAsset(api, "AUDCHF_otc");
    }
    
    static async Task AnalyzeAsset(BinollaApiClient api, string asset)
    {
        Console.WriteLine($"\n=== Analysis for {asset} ===");
        
        // 1. Check current price and freshness
        var priceResult = api.GetCurrentPrice(asset);
        var isRecentResult = api.IsQuoteRecent(asset);
        
        if (priceResult.IsSuccess && isRecentResult.IsSuccess)
        {
            var status = isRecentResult.Data ? "FRESH" : "STALE";
            Console.WriteLine($"Current Price: {priceResult.Data:F5} [{status}]");
        }
        
        // 2. Get historical data for analysis
        var historyResult = api.GetHistoricalData(asset, 60);
        if (historyResult.IsSuccess)
        {
            var history = historyResult.Data;
            
            Console.WriteLine($"Historical Data: {history.TickHistory.Count} ticks, {history.Candles.Count} candles");
            
            // Analyze recent price movement
            if (history.TickHistory.Count > 10)
            {
                var recentTicks = history.TickHistory.TakeLast(10).ToList();
                var priceChange = recentTicks.Last().Price - recentTicks.First().Price;
                var direction = priceChange > 0 ? "UP" : "DOWN";
                
                Console.WriteLine($"Recent Trend: {direction} ({priceChange:+0.00000;-0.00000})");
            }
            
            // Show latest candlestick
            if (history.Candlestick != null)
            {
                var candle = history.Candlestick;
                var candleDirection = candle.Close > candle.Open ? "BULLISH" : "BEARISH";
                Console.WriteLine($"Latest Candle: {candleDirection} (O:{candle.Open:F5} C:{candle.Close:F5})");
            }
        }
        
        // 3. Get recent quote activity
        var recentQuotesResult = api.GetRecentQuotes(20);
        if (recentQuotesResult.IsSuccess)
        {
            var assetQuotes = recentQuotesResult.Data
                .Where(q => q.Pair.Equals(asset, StringComparison.OrdinalIgnoreCase))
                .ToList();
                
            Console.WriteLine($"Recent Activity: {assetQuotes.Count} quotes in recent history");
        }
    }
}
```

### Real-time Monitoring Example

```csharp
static async Task MonitorQuotes(BinollaApiClient api)
{
    var watchList = new[] { "EURUSD", "GBPUSD", "AUDCHF_otc" };
    
    while (true)
    {
        Console.Clear();
        Console.WriteLine("=== Live Quote Monitor ===");
        Console.WriteLine($"Updated: {DateTime.Now:HH:mm:ss}");
        Console.WriteLine();
        
        foreach (var symbol in watchList)
        {
            var quoteResult = api.GetLatestQuote(symbol);
            var isRecentResult = api.IsQuoteRecent(symbol);
            
            if (quoteResult.IsSuccess && isRecentResult.IsSuccess)
            {
                var quote = quoteResult.Data;
                var status = isRecentResult.Data ? "🟢" : "🔴";
                
                Console.WriteLine($"{symbol,-12} {quote.Price:F5} {status} @ {quote.DateTime:HH:mm:ss.fff}");
            }
            else
            {
                Console.WriteLine($"{symbol,-12} No data available");
            }
        }
        
        await Task.Delay(1000); // Update every second
    }
}
```

## Error Handling

All methods return `ApiResult<T>` with comprehensive error information:

```csharp
var result = api.GetLatestQuote("INVALID_SYMBOL");

if (!result.IsSuccess)
{
    Console.WriteLine($"Operation failed:");
    Console.WriteLine($"  Error: {result.ErrorMessage}");
    Console.WriteLine($"  Code: {result.ErrorCode}");
    Console.WriteLine($"  Time: {result.Timestamp}");
    
    // Handle specific error types
    switch (result.ErrorCode)
    {
        case ApiErrorCode.InvalidAsset:
            Console.WriteLine("Check the asset symbol");
            break;
        case ApiErrorCode.Timeout:
            Console.WriteLine("Operation timed out, try again");
            break;
        case ApiErrorCode.ConnectionError:
            Console.WriteLine("Connection issue, check network");
            break;
        default:
            Console.WriteLine("Unexpected error occurred");
            break;
    }
}
```

## Best Practices

### 1. Connection Management
```csharp
// Always check connection before data operations
var status = api.GetConnectionStatus();
if (!status.IsConnected)
{
    var connectResult = api.Connect();
    if (!connectResult.IsSuccess)
    {
        // Handle connection failure
        return;
    }
}
```

### 2. Data Freshness Checks
```csharp
// Always verify quote freshness for trading decisions
var priceResult = api.GetCurrentPrice("EURUSD");
var isRecentResult = api.IsQuoteRecent("EURUSD");

if (priceResult.IsSuccess && isRecentResult.IsSuccess && isRecentResult.Data)
{
    // Safe to use for trading decisions
    var price = priceResult.Data;
}
else
{
    // Quote may be stale, wait for fresh data
}
```

### 3. Efficient Data Access
```csharp
// For multiple assets, get all quotes at once
var allQuotesResult = api.GetAllLatestQuotes();
if (allQuotesResult.IsSuccess)
{
    var quotes = allQuotesResult.Data;
    
    // Process multiple symbols efficiently
    foreach (var symbol in watchList)
    {
        if (quotes.TryGetValue(symbol, out var quote))
        {
            // Process quote
        }
    }
}
```

### 4. Historical Data Analysis
```csharp
// Combine different data types for comprehensive analysis
var historyResult = api.GetHistoricalData("EURUSD", 60);
if (historyResult.IsSuccess)
{
    var history = historyResult.Data;
    
    // Analyze tick-level movements
    var tickMovement = AnalyzeTicks(history.TickHistory);
    
    // Analyze candlestick patterns
    var candlePattern = AnalyzeCandles(history.Candles);
    
    // Make trading decision based on combined analysis
    var signal = CombineSignals(tickMovement, candlePattern);
}
```

### 5. Error Recovery
```csharp
// Implement retry logic for temporary failures
async Task<ApiResult<QuoteData>> GetQuoteWithRetry(string symbol, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        var result = api.GetLatestQuote(symbol);
        
        if (result.IsSuccess || result.ErrorCode != ApiErrorCode.Timeout)
            return result;
            
        await Task.Delay(1000 * (i + 1)); // Exponential backoff
    }
    
    return ApiResult<QuoteData>.Failure("Max retries exceeded", ApiErrorCode.Timeout);
}
```

## Performance Considerations

- **Batch Operations**: Use `GetAllLatestQuotes()` instead of multiple `GetLatestQuote()` calls
- **Data Freshness**: Check `IsQuoteRecent()` before making trading decisions
- **Memory Management**: Historical data is automatically limited to prevent memory growth
- **Connection Pooling**: Reuse the same `BinollaApiClient` instance for multiple operations
- **Error Handling**: Always check `IsSuccess` before accessing `Data` property

## Security Notes

- All quote and historical data access is restricted to authorized API clients
- Direct access to `Globals` methods from external code will throw `UnauthorizedAccessException`
- Stack trace validation ensures only legitimate API client calls succeed
- All data access is logged and monitored for security compliance 