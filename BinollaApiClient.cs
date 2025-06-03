namespace BinollaApiDotNet;
using System;
using System.Threading;
using static BinollaApiDotNet.Globals;
using System.Collections.Generic;

using BinollaApiDotNet.DataTypes;

#region Enhanced Data Types

/// <summary>
/// Represents the result of an operation with success status and optional error information
/// </summary>
/// <typeparam name="T">The type of data returned on success</typeparam>
public class ApiResult<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }
    public ApiErrorCode ErrorCode { get; private set; }
    public DateTime Timestamp { get; private set; }

    private ApiResult() { Timestamp = DateTime.UtcNow; }

    public static ApiResult<T> Success(T data)
    {
        return new ApiResult<T>
        {
            IsSuccess = true,
            Data = data,
            ErrorCode = ApiErrorCode.None
        };
    }

    public static ApiResult<T> Failure(string errorMessage, ApiErrorCode errorCode = ApiErrorCode.Unknown)
    {
        return new ApiResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }

    public static ApiResult<T> Timeout(string? customMessage = null)
    {
        return new ApiResult<T>
        {
            IsSuccess = false,
            ErrorMessage = customMessage ?? "Operation timed out",
            ErrorCode = ApiErrorCode.Timeout
        };
    }
}

/// <summary>
/// Enumeration of possible API error codes
/// </summary>
public enum ApiErrorCode
{
    None = 0,
    Unknown = 1,
    Timeout = 2,
    AuthenticationFailed = 3,
    InsufficientBalance = 4,
    InvalidAsset = 5,
    InvalidAmount = 6,
    MarketClosed = 7,
    ConnectionError = 8,
    ServerError = 9
}

/// <summary>
/// Enhanced order response with comprehensive information
/// </summary>
public class OrderResponse
{
    public string? OrderId { get; set; }
    public string Asset { get; set; } = string.Empty;
    public TradeDirection Direction { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpiryTime { get; set; }
    public DateTime PlacedAt { get; set; }
    public decimal? OpenPrice { get; set; }
    public decimal ExpectedPayout { get; set; }
    public OrderStatus Status { get; set; }
    public BalanceType BalanceType { get; set; }

    public OrderResponse()
    {
        PlacedAt = DateTime.UtcNow;
        Status = OrderStatus.Pending;
    }
}

/// <summary>
/// Enhanced win check response with detailed trade outcome
/// </summary>
public class TradeOutcome
{
    public string OrderId { get; set; } = string.Empty;
    public decimal WinAmount { get; set; }
    public decimal? ClosePrice { get; set; }
    public DateTime? ClosedAt { get; set; }
    public TradeResult Result { get; set; }
    public decimal ProfitLoss { get; set; }
    public decimal ReturnOnInvestment { get; set; }

    public bool IsWin => Result == TradeResult.Win;
    public bool IsLoss => Result == TradeResult.Loss;
}



/// <summary>
/// Trade direction enumeration
/// </summary>
public enum TradeDirection
{
    Call = 0,  // Price will go up
    Put = 1    // Price will go down
}

/// <summary>
/// Order status enumeration
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    Open = 1,
    Closed = 2,
    Cancelled = 3,
    Failed = 4
}

/// <summary>
/// Trade result enumeration
/// </summary>
public enum TradeResult
{
    Pending = 0,
    Win = 1,
    Loss = 2,
    Tie = 3
}




/// <summary>
/// Connection status information
/// </summary>
public class ConnectionStatus
{
    public bool IsConnected { get; set; }
    public DateTime? ConnectedAt { get; set; }
    public string? ErrorReason { get; set; }
    public TimeSpan? ConnectionDuration => ConnectedAt.HasValue ? DateTime.UtcNow - ConnectedAt.Value : null;
}

#endregion

public class BinollaApiClient
{
    private WebSocketClientBinolla _wsClient;
    private ChartWebSocketClient _chartWsClient;
    private readonly int _defaultTimeoutSeconds;

    /// <summary>
    /// Constructor for BinollaApiClient class
    /// </summary>
    /// <param name="ssid">The ssid to be used for authentication</param>
    /// <param name="timeoutSeconds">Default timeout for operations in seconds (default: 60)</param>
    /// <param name="enableLogging">Enable or disable WebSocket logging (default: true)</param>
    public BinollaApiClient(string ssid, int timeoutSeconds = 60, bool enableLogging = false)
    {
        Values.SSID = ssid;
        _wsClient = new WebSocketClientBinolla(enableLogging: enableLogging);
        _chartWsClient = new ChartWebSocketClient(enableLogging: enableLogging);
        _defaultTimeoutSeconds = timeoutSeconds;
    }

    /// <summary>
    /// Method to connect to websocket server with enhanced error handling
    /// </summary>
    /// <param name="timeoutSeconds">Connection timeout in seconds</param>
    /// <returns>Connection result with detailed status</returns>
    public ApiResult<ConnectionStatus> Connect(int? timeoutSeconds = null)
    {

        ConnectChartAsync().GetAwaiter().GetResult();
        var timeout = timeoutSeconds ?? _defaultTimeoutSeconds;
        _wsClient.ConnectAsync();
        DateTime start = DateTime.Now;
        
        while (true)
        {
            if (Values.SSIDAccepted)
            {
                Values.WebsocketErrorReason = "";
                var status = new ConnectionStatus 
                { 
                    IsConnected = true, 
                    ConnectedAt = DateTime.UtcNow 
                };
                return ApiResult<ConnectionStatus>.Success(status);
            }
            
            if ((DateTime.Now - start).TotalSeconds > timeout)
            {
                Values.WebsocketErrorReason = "Authorization Token Not Accepted By Server: Please Check SSID";
                var status = new ConnectionStatus 
                { 
                    IsConnected = false, 
                    ErrorReason = Values.WebsocketErrorReason 
                };
                return ApiResult<ConnectionStatus>.Failure(
                    Values.WebsocketErrorReason, 
                    ApiErrorCode.AuthenticationFailed
                );
            }
        }
    }

    /// <summary>
    /// Method to get current balance with enhanced information
    /// </summary>
    /// <param name="timeoutSeconds">Timeout for balance retrieval</param>
    /// <returns>Enhanced balance information</returns>
    public ApiResult<BalanceInfo> GetBalance(int? timeoutSeconds = null)
    {
        var timeout = timeoutSeconds ?? _defaultTimeoutSeconds;
        DateTime start = DateTime.Now;
        
        while (Values.BalanceUpdated == null)
        {
            if ((DateTime.Now - start).TotalSeconds > timeout)
            {
                return ApiResult<BalanceInfo>.Timeout("Balance not updated within timeout period");
            }
        }

        var balanceInfo = new BalanceInfo
        {
            RealBalance = (decimal)(Values.BalanceReal ?? 0),
            DemoBalance = (decimal)(Values.BalanceDemo ?? 0),
            CurrentType = Values.BalanceType == 1 ? BalanceType.Demo : BalanceType.Real,
            LastUpdated = DateTime.UtcNow,
            Currency = "USD"
        };

        return ApiResult<BalanceInfo>.Success(balanceInfo);
    }
    

    public ApiResult<BalanceInfo> ChangeBalanceType(BalanceType balanceType)
    {
        string sendStr;
        var targetType = (int)balanceType;
        
        if (Values.BalanceType == targetType)
        {
            // Already on the requested balance type
            return GetBalance();
        }
        
        if (balanceType == BalanceType.Real)
        {
            sendStr = "42[\"account/change\",{\"demo\":0}]";
            Values.BalanceType = 0;
        }
        else
        {
            sendStr = "42[\"account/change\",{\"demo\":1}]";
            Values.BalanceType = 1;
        }
        
        SendWss(sendStr);
        return GetBalance();
    }

  
    public ApiResult<Dictionary<string, TradingAsset>> GetTradingAssets(int? timeoutSeconds = null)
    {
        var timeout = timeoutSeconds ?? _defaultTimeoutSeconds;
        var result = new Dictionary<string, TradingAsset>();
        DateTime start = DateTime.Now;
        
        while (Values.PaymentAssets.Count == 0)
        {
            if ((DateTime.Now - start).TotalSeconds > timeout)
            {
                return ApiResult<Dictionary<string, TradingAsset>>.Timeout(
                    "Payment assets not updated within timeout period"
                );
            }
        }

        foreach (var asset in Values.PaymentAssets)
        {
            if (asset.IsOpen)
            {
                var tradingAsset = new TradingAsset
                {
                    Symbol = asset.Name,
                    Description = asset.Description ?? asset.Name,
                    IsOpen = asset.IsOpen,
                    PayoutPercentage = asset.Payout,
                    Category = ParseAssetCategory(asset.Type),
                    LastPriceUpdate = DateTime.UtcNow,
                    MinimumAmount = 1, // Default values - could be enhanced with real data
                    MaximumAmount = 1000,
                    AvailableExpiryTimes = new[] { 60, 120, 300, 600, 1800, 3600 }
                };
                
                result[asset.Name] = tradingAsset;
            }
        }

        return ApiResult<Dictionary<string, TradingAsset>>.Success(result);
    }

    /// <summary>
    /// Method to place a buy order with enhanced validation and response
    /// </summary>
    /// <param name="asset">The asset to be traded</param>
    /// <param name="direction">The direction of the trade</param>
    /// <param name="amount">The amount to be traded</param>
    /// <param name="expirySeconds">The expiry time for the trade in seconds</param>
    /// <param name="timeoutSeconds">Timeout for order placement</param>
    /// <returns>Enhanced order response</returns>
    public ApiResult<OrderResponse> PlaceOrder(string asset, TradeDirection direction, decimal amount, 
        int expirySeconds, int? timeoutSeconds = null)
    {
        var timeout = timeoutSeconds ?? 10; // Shorter timeout for order placement
        
        // Input validation
        if (string.IsNullOrWhiteSpace(asset))
        {
            return ApiResult<OrderResponse>.Failure("Asset cannot be empty", ApiErrorCode.InvalidAsset);
        }
        
        if (amount <= 0)
        {
            return ApiResult<OrderResponse>.Failure("Amount must be greater than zero", ApiErrorCode.InvalidAmount);
        }

        var currentTimeStamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        var expiryTimestamp = (int)currentTimeStamp + expirySeconds + 1;
        var cmd = direction == TradeDirection.Call ? 0 : 1;
        
        string sendStr = $"42[\"orders/open\",{{\"asset\":\"{asset}\",\"time\":{expiryTimestamp},\"amount\":{amount},\"cmd\":{cmd}}}]";
        
        // Reset order tracking variables
        Values.NewOpenOrder = null;
        Values.OrderOpenUuid = null;
        Values.OrderData = null;
        Values.FailedOrderOpen = null;
        
        SendWss(sendStr);
        DateTime start = DateTime.Now;
        
        while (Values.NewOpenOrder == null)
        {
            if ((DateTime.Now - start).TotalSeconds > timeout)
            {
                return ApiResult<OrderResponse>.Timeout("Order not opened within timeout period");
            }

            if (Values.FailedOrderOpen != null)
            {
                var errorCode = ParseOrderError(Values.FailedOrderOpen.Error);
                return ApiResult<OrderResponse>.Failure(Values.FailedOrderOpen.Error, errorCode);
            }

            if (Values.OrderData != null)
            {
                var orderResponse = new OrderResponse
                {
                    OrderId = Values.OrderData.Deal.Uuid,
                    Asset = asset,
                    Direction = direction,
                    Amount = amount,
                    ExpiryTime = DateTime.UtcNow.AddSeconds(expirySeconds),
                    OpenPrice = (decimal?)Values.OrderData.Deal.OpenPrice,
                    ExpectedPayout = (decimal)Values.OrderData.Deal.Profit,
                    Status = OrderStatus.Open,
                    BalanceType = Values.BalanceType == 1 ? BalanceType.Demo : BalanceType.Real
                };
                
                return ApiResult<OrderResponse>.Success(orderResponse);
            }
        }
        
        return ApiResult<OrderResponse>.Failure("Unknown error occurred", ApiErrorCode.Unknown);
    }

    /// <summary>
    /// Method to check trade outcome with enhanced information
    /// </summary>
    /// <param name="orderId">The unique identifier of the order</param>
    /// <param name="timeoutSeconds">Timeout for result retrieval</param>
    /// <returns>Enhanced trade outcome information</returns>
    public ApiResult<TradeOutcome> GetTradeOutcome(string orderId, int? timeoutSeconds = null)
    {
        var timeout = timeoutSeconds ?? 300; // 5 minutes default for trade completion
        
        if (string.IsNullOrWhiteSpace(orderId))
        {
            return ApiResult<TradeOutcome>.Failure("Order ID cannot be empty", ApiErrorCode.Unknown);
        }

        DateTime start = DateTime.Now;
        
        while (!Values.CloseOrderPl.ContainsKey(orderId))
        {
            if ((DateTime.Now - start).TotalSeconds > timeout)
            {
                return ApiResult<TradeOutcome>.Timeout("Trade outcome not available within timeout period");
            }
            
            Thread.Sleep(100); // CPU optimization
        }

        var winAmount = (decimal)Values.CloseOrderPl[orderId];
        var tradeOutcome = new TradeOutcome
        {
            OrderId = orderId,
            WinAmount = winAmount,
            ClosedAt = DateTime.UtcNow,
            Result = winAmount > 0 ? TradeResult.Win : (winAmount < 0 ? TradeResult.Loss : TradeResult.Tie),
            ProfitLoss = winAmount,
            ReturnOnInvestment = winAmount // This could be enhanced with investment amount calculation
        };
        
        return ApiResult<TradeOutcome>.Success(tradeOutcome);
    }

    /// <summary>
    /// Enhanced method to get current connection status
    /// </summary>
    /// <returns>Current connection status</returns>
    public ConnectionStatus GetConnectionStatus()
    {
        return new ConnectionStatus
        {
            IsConnected = Values.SSIDAccepted,
            ConnectedAt = Values.SSIDAccepted ? DateTime.UtcNow : null,
            ErrorReason = Values.WebsocketErrorReason
        };
    }

    #region Quote Data Methods
    
    
    public void SubscribePair(string pair, int period = 60)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(pair))
                throw new ArgumentException("Pair symbol cannot be null or empty", nameof(pair));

            if (period <= 0)
                throw new ArgumentException("Period must be greater than 0", nameof(period));
            
            if(Values.PaymentAssets.Find(x => x.Name == pair) == null)
                throw new ArgumentException($"Pair {pair} is not available for trading", nameof(pair));
            
            // Send subscription message to WebSocket 
            // 42["asset/change",{"asset":"AUDCHF_otc","period":60}]
            
            SendWss("42[\"alert/list\"]");
            SendWss("42[\"alert/closed/list\"]");
            
            string sendStr = $"42[\"asset/change\",{{\"asset\":\"{pair}\",\"period\":{period}}}]";
            SendWss(sendStr);
            

            
        }
        catch (Exception ex)
        {
            // Handle subscription errors
            Console.WriteLine($"Error subscribing to pair {pair}: {ex.Message}");
        }
    }
    
    
    
    
    
    
    
    /// <summary>
    /// Gets the latest quote for a specific trading pair
    /// </summary>
    /// <param name="pair">Trading pair symbol (e.g., "EURUSD")</param>
    /// <returns>Latest quote data if available</returns>
    public ApiResult<QuoteData> GetLatestQuote(string pair)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(pair))
                return ApiResult<QuoteData>.Failure("Pair symbol cannot be null or empty", ApiErrorCode.InvalidAsset);

            var quote = Values.GetLatestQuote(pair);
            
            if (quote == null)
                return ApiResult<QuoteData>.Failure($"No quote data available for {pair}", ApiErrorCode.InvalidAsset);

            return ApiResult<QuoteData>.Success(quote);
        }
        catch (Exception ex)
        {
            return ApiResult<QuoteData>.Failure($"Error retrieving quote: {ex.Message}", ApiErrorCode.Unknown);
        }
    }

    /// <summary>
    /// Gets all latest quotes for all trading pairs
    /// </summary>
    /// <returns>Dictionary of all latest quotes</returns>
    public ApiResult<Dictionary<string, QuoteData>> GetAllLatestQuotes()
    {
        try
        {
            var quotes = Values.GetAllLatestQuotes();
            return ApiResult<Dictionary<string, QuoteData>>.Success(quotes);
        }
        catch (Exception ex)
        {
            return ApiResult<Dictionary<string, QuoteData>>.Failure($"Error retrieving quotes: {ex.Message}", ApiErrorCode.Unknown);
        }
    }

    /// <summary>
    /// Gets recent quote history
    /// </summary>
    /// <param name="count">Number of recent quotes to retrieve (default: 100)</param>
    /// <returns>Collection of recent quotes</returns>
    public ApiResult<IEnumerable<QuoteData>> GetRecentQuotes(int count = 100)
    {
        try
        {
            if (count <= 0)
                return ApiResult<IEnumerable<QuoteData>>.Failure("Count must be greater than 0", ApiErrorCode.Unknown);

            var quotes = Values.GetRecentQuotes(count);
            return ApiResult<IEnumerable<QuoteData>>.Success(quotes);
        }
        catch (Exception ex)
        {
            return ApiResult<IEnumerable<QuoteData>>.Failure($"Error retrieving recent quotes: {ex.Message}", ApiErrorCode.Unknown);
        }
    }

    /// <summary>
    /// Gets the current price for a trading pair
    /// </summary>
    /// <param name="pair">Trading pair symbol</param>
    /// <returns>Current price if available</returns>
    public ApiResult<double> GetCurrentPrice(string pair)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(pair))
                return ApiResult<double>.Failure("Pair symbol cannot be null or empty", ApiErrorCode.InvalidAsset);

            var price = Values.GetCurrentPrice(pair);
            
            if (!price.HasValue)
                return ApiResult<double>.Failure($"No price data available for {pair}", ApiErrorCode.InvalidAsset);

            return ApiResult<double>.Success(price.Value);
        }
        catch (Exception ex)
        {
            return ApiResult<double>.Failure($"Error retrieving price: {ex.Message}", ApiErrorCode.Unknown);
        }
    }

    /// <summary>
    /// Checks if quote data is recent (within last 30 seconds)
    /// </summary>
    /// <param name="pair">Trading pair symbol</param>
    /// <returns>True if quote is recent</returns>
    public ApiResult<bool> IsQuoteRecent(string pair)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(pair))
                return ApiResult<bool>.Failure("Pair symbol cannot be null or empty", ApiErrorCode.InvalidAsset);

            var isRecent = Values.IsQuoteRecent(pair);
            return ApiResult<bool>.Success(isRecent);
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Failure($"Error checking quote freshness: {ex.Message}", ApiErrorCode.Unknown);
        }
    }

    #endregion

    #region Historical Data Methods

    /// <summary>
    /// Gets historical data for a specific asset and period
    /// </summary>
    /// <param name="asset">Asset symbol (e.g., "EURUSD", "AUDCHF_otc")</param>
    /// <param name="period">Time period in seconds (e.g., 60, 300, 3600)</param>
    /// <returns>Historical data if available</returns>
    public ApiResult<HistoryData> GetHistoricalData(string asset, int period)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(asset))
                return ApiResult<HistoryData>.Failure("Asset symbol cannot be null or empty", ApiErrorCode.InvalidAsset);

            if (period <= 0)
                return ApiResult<HistoryData>.Failure("Period must be greater than 0", ApiErrorCode.Unknown);

            var history = Values.GetHistoricalData(asset, period);
            
            if (history == null)
                return ApiResult<HistoryData>.Failure($"No historical data available for {asset} with period {period}s", ApiErrorCode.Unknown);

            return ApiResult<HistoryData>.Success(history);
        }
        catch (Exception ex)
        {
            return ApiResult<HistoryData>.Failure($"Error retrieving historical data: {ex.Message}", ApiErrorCode.Unknown);
        }
    }

    /// <summary>
    /// Gets all available historical data
    /// </summary>
    /// <returns>Dictionary of all historical data</returns>
    public ApiResult<Dictionary<string, HistoryData>> GetAllHistoricalData()
    {
        try
        {
            var allHistory = Values.GetAllHistoricalData();
            return ApiResult<Dictionary<string, HistoryData>>.Success(allHistory);
        }
        catch (Exception ex)
        {
            return ApiResult<Dictionary<string, HistoryData>>.Failure($"Error retrieving historical data: {ex.Message}", ApiErrorCode.Unknown);
        }
    }

    /// <summary>
    /// Gets historical data for a specific asset across all periods
    /// </summary>
    /// <param name="asset">Asset symbol</param>
    /// <returns>List of historical data for different periods</returns>
    public ApiResult<List<HistoryData>> GetHistoricalDataForAsset(string asset)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(asset))
                return ApiResult<List<HistoryData>>.Failure("Asset symbol cannot be null or empty", ApiErrorCode.InvalidAsset);

            var historyList = Values.GetHistoricalDataForAsset(asset);
            return ApiResult<List<HistoryData>>.Success(historyList);
        }
        catch (Exception ex)
        {
            return ApiResult<List<HistoryData>>.Failure($"Error retrieving historical data for asset: {ex.Message}", ApiErrorCode.Unknown);
        }
    }

    /// <summary>
    /// Gets tick data for a specific asset and period
    /// </summary>
    /// <param name="asset">Asset symbol</param>
    /// <param name="period">Time period in seconds</param>
    /// <returns>List of tick data if available</returns>
    public ApiResult<List<TickData>> GetTickData(string asset, int period)
    {
        try
        {
            var historyResult = GetHistoricalData(asset, period);
            if (!historyResult.IsSuccess)
                return ApiResult<List<TickData>>.Failure(historyResult.ErrorMessage, historyResult.ErrorCode);

            var tickData = historyResult.Data?.TickHistory ?? new List<TickData>();
            return ApiResult<List<TickData>>.Success(tickData);
        }
        catch (Exception ex)
        {
            return ApiResult<List<TickData>>.Failure($"Error retrieving tick data: {ex.Message}", ApiErrorCode.Unknown);
        }
    }

    /// <summary>
    /// Gets candlestick data for a specific asset and period
    /// </summary>
    /// <param name="asset">Asset symbol</param>
    /// <param name="period">Time period in seconds</param>
    /// <returns>List of candlestick data if available</returns>
    public ApiResult<List<CandlestickData>> GetCandlestickData(string asset, int period)
    {
        try
        {
            var historyResult = GetHistoricalData(asset, period);
            if (!historyResult.IsSuccess)
                return ApiResult<List<CandlestickData>>.Failure(historyResult.ErrorMessage, historyResult.ErrorCode);

            var candleData = historyResult.Data?.Candles ?? new List<CandlestickData>();
            return ApiResult<List<CandlestickData>>.Success(candleData);
        }
        catch (Exception ex)
        {
            return ApiResult<List<CandlestickData>>.Failure($"Error retrieving candlestick data: {ex.Message}", ApiErrorCode.Unknown);
        }
    }

    /// <summary>
    /// Gets the most recent candlestick for a specific asset and period
    /// </summary>
    /// <param name="asset">Asset symbol</param>
    /// <param name="period">Time period in seconds</param>
    /// <returns>Most recent candlestick data if available</returns>
    public ApiResult<CandlestickData> GetLatestCandlestick(string asset, int period)
    {
        try
        {
            var historyResult = GetHistoricalData(asset, period);
            if (!historyResult.IsSuccess)
                return ApiResult<CandlestickData>.Failure(historyResult.ErrorMessage, historyResult.ErrorCode);

            var latestCandle = historyResult.Data?.Candlestick;
            if (latestCandle == null)
                return ApiResult<CandlestickData>.Failure($"No candlestick data available for {asset} with period {period}s", ApiErrorCode.Unknown);

            return ApiResult<CandlestickData>.Success(latestCandle);
        }
        catch (Exception ex)
        {
            return ApiResult<CandlestickData>.Failure($"Error retrieving latest candlestick: {ex.Message}", ApiErrorCode.Unknown);
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Parse asset category from string type
    /// </summary>
    /// <param name="type">Asset type string</param>
    /// <returns>Parsed asset category</returns>
    private static AssetCategory ParseAssetCategory(string? type)
    {
        return type?.ToLower() switch
        {
            "currency" => AssetCategory.Currency,
            "commodity" => AssetCategory.Commodity,
            "stock" => AssetCategory.Stock,
            "crypto" or "cryptocurrency" => AssetCategory.Cryptocurrency,
            "index" => AssetCategory.Index,
            _ => AssetCategory.Currency
        };
    }

    /// <summary>
    /// Parse order error to appropriate error code
    /// </summary>
    private static ApiErrorCode ParseOrderError(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
            return ApiErrorCode.Unknown;

        var lowerError = error.ToLower();
        
        return lowerError switch
        {
            var e when e.Contains("balance") => ApiErrorCode.InsufficientBalance,
            var e when e.Contains("asset") => ApiErrorCode.InvalidAsset,
            var e when e.Contains("amount") => ApiErrorCode.InvalidAmount,
            var e when e.Contains("market") => ApiErrorCode.MarketClosed,
            var e when e.Contains("connection") => ApiErrorCode.ConnectionError,
            var e when e.Contains("server") => ApiErrorCode.ServerError,
            _ => ApiErrorCode.Unknown
        };
    }

    /// <summary>
    /// Private method to send a message to the Binolla WebSocket
    /// </summary>
    /// <param name="message">The message to be sent</param>
    private void SendWss(string message)
    {
        _wsClient.SendMessageAsync(message).GetAwaiter().GetResult();
    }

    #endregion

    /// <summary>
    /// Method to connect to chart WebSocket for price data
    /// </summary>
    /// <param name="timeoutSeconds">Connection timeout in seconds</param>
    /// <returns>Connection result</returns>
    public async Task<bool> ConnectChartAsync(int? timeoutSeconds = 30)
    {
        var timeout = timeoutSeconds != null ? TimeSpan.FromSeconds(timeoutSeconds.Value) : (TimeSpan?)null;
        return await _chartWsClient.ConnectAsync(timeout);
    }

    /// <summary>
    /// Method to send asset change message to chart WebSocket
    /// </summary>
    /// <param name="asset">Asset symbol (e.g., "EURUSD")</param>
    /// <param name="period">Time period in seconds (e.g., 60)</param>
    /// <returns>Success status</returns>
    public async Task<bool> ChangeChartAssetAsync(string asset = "EURUSD", int period = 60)
    {
        return await _chartWsClient.SendAssetChangeAsync(asset, period);
    }

    /// <summary>
    /// Method to subscribe to chart messages
    /// </summary>
    /// <param name="callback">Callback to handle chart messages</param>
    public void SubscribeToChartMessages(Action<string> callback)
    {
        _chartWsClient.OnChartMessage += (message) => callback(message);
    }

    /// <summary>
    /// Method to get chart connection status
    /// </summary>
    /// <returns>True if chart WebSocket is connected</returns>
    public bool IsChartConnected => _chartWsClient.IsConnected;

    /// <summary>
    /// Method to disconnect from the WebSocket server
    /// </summary>
    public void Disconnect()
    {
        _wsClient.DisconnectAsync().GetAwaiter().GetResult();
        _chartWsClient.DisconnectAsync().GetAwaiter().GetResult();
    }

    #region Logging Control

    /// <summary>
    /// Configure WebSocket logging settings for both main and chart connections
    /// </summary>
    /// <param name="enableLogging">Master logging switch</param>
    /// <param name="enableDebug">Enable debug logging</param>
    /// <param name="enableInfo">Enable info logging</param>
    /// <param name="enableWarning">Enable warning logging</param>
    /// <param name="enableError">Enable error logging</param>
    public void ConfigureLogging(bool enableLogging = true, bool? enableDebug = null, 
        bool? enableInfo = null, bool? enableWarning = null, bool? enableError = null)
    {
        _wsClient.ConfigureLogging(enableLogging, enableDebug, enableInfo, enableWarning, enableError);
        _chartWsClient.ConfigureLogging(enableLogging, enableDebug, enableInfo, enableWarning, enableError);
    }

    /// <summary>
    /// Disable all WebSocket logging (silent mode) for both connections
    /// </summary>
    public void DisableAllLogging()
    {
        _wsClient.DisableAllLogging();
        _chartWsClient.DisableAllLogging();
    }

    /// <summary>
    /// Enable only error logging (minimal mode) for both connections
    /// </summary>
    public void SetMinimalLogging()
    {
        _wsClient.SetMinimalLogging();
        _chartWsClient.SetMinimalLogging();
    }

    /// <summary>
    /// Enable only error and warning logging (production mode) for both connections
    /// </summary>
    public void SetProductionLogging()
    {
        _wsClient.SetProductionLogging();
        _chartWsClient.SetProductionLogging();
    }

    /// <summary>
    /// Enable all logging (development mode) for both connections
    /// </summary>
    public void SetDevelopmentLogging()
    {
        _wsClient.SetDevelopmentLogging();
        _chartWsClient.SetDevelopmentLogging();
    }

    /// <summary>
    /// Gets or sets whether WebSocket logging is enabled for both connections
    /// </summary>
    public bool EnableLogging 
    { 
        get => _wsClient.EnableLogging; 
        set 
        { 
            _wsClient.EnableLogging = value; 
            _chartWsClient.EnableLogging = value; 
        } 
    }

    /// <summary>
    /// Gets or sets whether debug level logging is enabled for both connections
    /// </summary>
    public bool EnableDebugLogging 
    { 
        get => _wsClient.EnableDebugLogging; 
        set 
        { 
            _wsClient.EnableDebugLogging = value; 
            _chartWsClient.EnableDebugLogging = value; 
        } 
    }

    /// <summary>
    /// Gets or sets whether info level logging is enabled for both connections
    /// </summary>
    public bool EnableInfoLogging 
    { 
        get => _wsClient.EnableInfoLogging; 
        set 
        { 
            _wsClient.EnableInfoLogging = value; 
            _chartWsClient.EnableInfoLogging = value; 
        } 
    }

    /// <summary>
    /// Gets or sets whether warning level logging is enabled for both connections
    /// </summary>
    public bool EnableWarningLogging 
    { 
        get => _wsClient.EnableWarningLogging; 
        set 
        { 
            _wsClient.EnableWarningLogging = value; 
            _chartWsClient.EnableWarningLogging = value; 
        } 
    }

    /// <summary>
    /// Gets or sets whether error level logging is enabled for both connections
    /// </summary>
    public bool EnableErrorLogging 
    { 
        get => _wsClient.EnableErrorLogging; 
        set 
        { 
            _wsClient.EnableErrorLogging = value; 
            _chartWsClient.EnableErrorLogging = value; 
        } 
    }

    #endregion
}