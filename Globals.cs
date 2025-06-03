using BinollaApiDotNet.DataTypes;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BinollaApiDotNet;

/// <summary>
/// Balance type enumeration for global use
/// </summary>
public enum BalanceType
{
    Real = 0,
    Demo = 1
}

/// <summary>
/// Asset category enumeration for global use
/// </summary>
public enum AssetCategory
{
    Currency = 0,
    Commodity = 1,
    Stock = 2,
    Cryptocurrency = 3,
    Index = 4
}

/// <summary>
/// Thread-safe global state manager for Binolla API operations
/// </summary>
public sealed class Globals
{
    #region Singleton Implementation
    
    private static readonly Lazy<Globals> _instance = new(() => new Globals());
    private static readonly object _lock = new object();

    private Globals() 
    {
        InitializeCollections();
    }

    public static Globals Values => _instance.Value;

    #endregion

    #region Connection & Authentication State

    private volatile bool _websocketIsConnected = false;
    private volatile bool _ssidAccepted = false;
    private volatile string _ssid = string.Empty;
    private volatile string _websocketErrorReason = string.Empty;

    /// <summary>
    /// Gets or sets the WebSocket connection status
    /// </summary>
    public bool WebsocketIsConnected
    {
        get => _websocketIsConnected;
        set => _websocketIsConnected = value;
    }

    /// <summary>
    /// Gets or sets whether the SSID has been accepted by the server
    /// </summary>
    public bool SSIDAccepted
    {
        get => _ssidAccepted;
        set
        {
            _ssidAccepted = value;
            if (value)
            {
                ConnectionEstablishedAt = DateTime.UtcNow;
                _websocketErrorReason = string.Empty;
            }
        }
    }

    /// <summary>
    /// Gets or sets the SSID for authentication
    /// </summary>
    public string SSID
    {
        get => _ssid;
        set => _ssid = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the WebSocket error reason
    /// </summary>
    public string WebsocketErrorReason
    {
        get => _websocketErrorReason;
        set => _websocketErrorReason = value ?? string.Empty;
    }

    /// <summary>
    /// Gets the timestamp when connection was established
    /// </summary>
    public DateTime? ConnectionEstablishedAt { get; private set; }

    /// <summary>
    /// Gets the connection duration if connected
    /// </summary>
    public TimeSpan? ConnectionDuration => 
        ConnectionEstablishedAt.HasValue ? DateTime.UtcNow - ConnectionEstablishedAt.Value : null;

    #endregion

    #region Balance Management

    private volatile int _balanceType = 1; // Default to demo
    private decimal _balanceReal = 0m;
    private decimal _balanceDemo = 0m;
    private DateTime? _balanceUpdated = null;
    private readonly object _balanceLock = new object();

    /// <summary>
    /// Gets or sets the current balance type (0 = Real, 1 = Demo)
    /// </summary>
    public int BalanceType
    {
        get => _balanceType;
        set
        {
            if (value is 0 or 1)
            {
                _balanceType = value;
                var balanceTypeEnum = value == 0 ? BinollaApiDotNet.BalanceType.Real : BinollaApiDotNet.BalanceType.Demo;
                OnBalanceTypeChanged?.Invoke(balanceTypeEnum);
            }
        }
    }

    /// <summary>
    /// Gets or sets the real account balance
    /// </summary>
    public double? BalanceReal
    {
        get
        {
            lock (_balanceLock)
            {
                return (double?)_balanceReal;
            }
        }
        set
        {
            lock (_balanceLock)
            {
                _balanceReal = (decimal)(value ?? 0);
                _balanceUpdated = DateTime.UtcNow;
                OnBalanceUpdated?.Invoke(_balanceReal, _balanceDemo);
            }
        }
    }

    /// <summary>
    /// Gets or sets the demo account balance
    /// </summary>
    public double? BalanceDemo
    {
        get
        {
            lock (_balanceLock)
            {
                return (double?)_balanceDemo;
            }
        }
        set
        {
            lock (_balanceLock)
            {
                _balanceDemo = (decimal)(value ?? 0);
                _balanceUpdated = DateTime.UtcNow;
                OnBalanceUpdated?.Invoke(_balanceReal, _balanceDemo);
            }
        }
    }

    /// <summary>
    /// Gets or sets the last balance update timestamp
    /// </summary>
    public DateTime? BalanceUpdated
    {
        get
        {
            lock (_balanceLock)
            {
                return _balanceUpdated;
            }
        }
        set
        {
            lock (_balanceLock)
            {
                _balanceUpdated = value;
            }
        }
    }

    /// <summary>
    /// Gets the current active balance based on balance type
    /// </summary>
    public decimal CurrentBalance
    {
        get
        {
            lock (_balanceLock)
            {
                return _balanceType == 1 ? _balanceDemo : _balanceReal;
            }
        }
    }

    /// <summary>
    /// Gets comprehensive balance information
    /// </summary>
    public BalanceInfo GetBalanceInfo()
    {
        lock (_balanceLock)
        {
            return new BalanceInfo
            {
                RealBalance = _balanceReal,
                DemoBalance = _balanceDemo,
                CurrentType = _balanceType == 1 ? BinollaApiDotNet.BalanceType.Demo : BinollaApiDotNet.BalanceType.Real,
                LastUpdated = _balanceUpdated ?? DateTime.UtcNow,
                Currency = "USD"
            };
        }
    }

    #endregion

    #region Order Management

    private readonly ConcurrentDictionary<string, double> _closeOrderPl = new();
    private readonly ConcurrentQueue<OpenedOrder> _openedOrdersQueue = new();
    private readonly ConcurrentQueue<ClosedOrder> _closedOrdersQueue = new();
    private volatile OpenedOrder? _orderData = null;
    private volatile OpenedOrder? _newOpenOrder = null;
    private volatile FailedOrderOpen? _failedOrderOpen = null;
    private volatile string? _orderOpenUuid = null;

    /// <summary>
    /// Thread-safe dictionary for tracking order profit/loss by UUID
    /// </summary>
    public ConcurrentDictionary<string, double> CloseOrderPl => _closeOrderPl;

    /// <summary>
    /// Gets or sets the current order data
    /// </summary>
    public OpenedOrder? OrderData
    {
        get => _orderData;
        set
        {
            _orderData = value;
            if (value != null)
            {
                OnOrderOpened?.Invoke(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the new open order
    /// </summary>
    public OpenedOrder? NewOpenOrder
    {
        get => _newOpenOrder;
        set => _newOpenOrder = value;
    }

    /// <summary>
    /// Gets or sets failed order information
    /// </summary>
    public FailedOrderOpen? FailedOrderOpen
    {
        get => _failedOrderOpen;
        set
        {
            _failedOrderOpen = value;
            if (value != null)
            {
                OnOrderFailed?.Invoke(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the order open UUID
    /// </summary>
    public string? OrderOpenUuid
    {
        get => _orderOpenUuid;
        set => _orderOpenUuid = value;
    }

    /// <summary>
    /// Adds a closed order and updates profit/loss tracking
    /// </summary>
    /// <param name="closedOrder">The closed order to add</param>
    public void AddClosedOrder(ClosedOrder closedOrder)
    {
        if (closedOrder?.Deals == null) return;

        _closedOrdersQueue.Enqueue(closedOrder);
        
        foreach (var deal in closedOrder.Deals)
        {
            _closeOrderPl.TryAdd(deal.Uuid, deal.Profit);
            OnOrderClosed?.Invoke(deal.Uuid, deal.Profit);
        }
    }

    /// <summary>
    /// Gets the most recent closed orders (up to specified count)
    /// </summary>
    /// <param name="count">Maximum number of orders to return</param>
    /// <returns>Collection of recent closed orders</returns>
    public IEnumerable<ClosedOrder> GetRecentClosedOrders(int count = 10)
    {
        var orders = new List<ClosedOrder>();
        var tempQueue = new List<ClosedOrder>(_closedOrdersQueue);
        
        return tempQueue.TakeLast(count);
    }

    #endregion

    #region Asset Management

    private List<AssetData> _paymentAssets = new();
    private readonly object _assetsLock = new object();

    /// <summary>
    /// Gets or sets the available payment assets
    /// </summary>
    public List<AssetData> PaymentAssets
    {
        get
        {
            lock (_assetsLock)
            {
                return new List<AssetData>(_paymentAssets);
            }
        }
        set
        {
            lock (_assetsLock)
            {
                _paymentAssets = value ?? new List<AssetData>();
                OnAssetsUpdated?.Invoke(_paymentAssets);
            }
        }
    }

    /// <summary>
    /// Gets open trading assets as TradingAsset objects
    /// </summary>
    /// <returns>Dictionary of open trading assets</returns>
    public Dictionary<string, TradingAsset> GetOpenTradingAssets()
    {
        lock (_assetsLock)
        {
            var result = new Dictionary<string, TradingAsset>();
            
            foreach (var asset in _paymentAssets.Where(a => a.IsOpen))
            {
                var tradingAsset = new TradingAsset
                {
                    Symbol = asset.Name,
                    Description = asset.Description ?? asset.Name,
                    IsOpen = asset.IsOpen,
                    PayoutPercentage = asset.Payout,
                    Category = ParseAssetCategory(asset.Type),
                    LastPriceUpdate = DateTime.UtcNow,
                    MinimumAmount = 1m,
                    MaximumAmount = 1000m,
                    AvailableExpiryTimes = new[] { 60, 120, 300, 600, 1800, 3600 }
                };
                
                result[asset.Name] = tradingAsset;
            }
            
            return result;
        }
    }

    /// <summary>
    /// Finds an asset by name or symbol
    /// </summary>
    /// <param name="nameOrSymbol">Asset name or symbol to search for</param>
    /// <returns>The asset if found, null otherwise</returns>
    public AssetData? FindAsset(string nameOrSymbol)
    {
        if (string.IsNullOrWhiteSpace(nameOrSymbol)) return null;
        
        lock (_assetsLock)
        {
            return _paymentAssets.FirstOrDefault(a => 
                string.Equals(a.Name, nameOrSymbol, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Finds an asset by its ActiveId
    /// </summary>
    /// <param name="activeId">Asset ActiveId to search for</param>
    /// <returns>The asset if found, null otherwise</returns>
    public AssetData? FindAssetById(int activeId)
    {
        lock (_assetsLock)
        {
            return _paymentAssets.FirstOrDefault(a => a.ActiveId == activeId);
        }
    }

    /// <summary>
    /// Gets assets filtered by type
    /// </summary>
    /// <param name="assetType">Asset type to filter by (e.g., "currency", "stock", "commodity", "cryptocurrency")</param>
    /// <returns>List of assets of the specified type</returns>
    public List<AssetData> GetAssetsByType(string assetType)
    {
        if (string.IsNullOrWhiteSpace(assetType)) return new List<AssetData>();
        
        lock (_assetsLock)
        {
            return _paymentAssets
                .Where(a => string.Equals(a.Type, assetType, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    /// <summary>
    /// Gets only open assets available for trading
    /// </summary>
    /// <returns>List of open assets</returns>
    public List<AssetData> GetOpenAssets()
    {
        lock (_assetsLock)
        {
            return _paymentAssets.Where(a => a.IsOpen).ToList();
        }
    }

    /// <summary>
    /// Gets OTC (Over The Counter) assets
    /// </summary>
    /// <returns>List of OTC assets</returns>
    public List<AssetData> GetOTCAssets()
    {
        lock (_assetsLock)
        {
            return _paymentAssets.Where(a => a.IsOTC).ToList();
        }
    }

    /// <summary>
    /// Gets rush trading assets (short-term/5s)
    /// </summary>
    /// <returns>List of rush assets</returns>
    public List<AssetData> GetRushAssets()
    {
        lock (_assetsLock)
        {
            return _paymentAssets.Where(a => a.IsRush).ToList();
        }
    }

    /// <summary>
    /// Gets assets with the highest payout percentages
    /// </summary>
    /// <param name="count">Number of top assets to return</param>
    /// <returns>List of assets with highest payouts</returns>
    public List<AssetData> GetHighestPayoutAssets(int count = 10)
    {
        lock (_assetsLock)
        {
            return _paymentAssets
                .Where(a => a.IsOpen)
                .OrderByDescending(a => a.Payout)
                .Take(count)
                .ToList();
        }
    }

    #endregion

    #region Quote Management

    private readonly ConcurrentDictionary<string, QuoteData> _latestQuotes = new();
    private readonly ConcurrentQueue<QuoteData> _quoteHistory = new();
    private readonly ConcurrentDictionary<string, HistoryData> _historicalData = new();
    private readonly object _quotesLock = new object();
    private const int MaxQuoteHistorySize = 1000;
    private const int MaxHistoricalDataSize = 100;

    // Access control - only BinollaApiClient can access quote functionality
    private static readonly HashSet<string> _authorizedCallers = new HashSet<string>
    {
        "BinollaApiDotNet.BinollaApiClient",
        "BinollaApiDotNet.WebSocketClientBinolla",
        "BinollaApiDotNet.ChartWebSocketClient",
        "BinollaApiDotNet.MessageProcessor"
    };

    /// <summary>
    /// Validates that the caller is authorized to access quote functionality
    /// </summary>
    private static bool IsAuthorizedCaller()
    {
        var stackTrace = new System.Diagnostics.StackTrace();
        for (int i = 1; i < Math.Min(stackTrace.FrameCount, 10); i++)
        {
            var frame = stackTrace.GetFrame(i);
            var method = frame?.GetMethod();
            var declaringType = method?.DeclaringType?.FullName;
            
            if (declaringType != null && _authorizedCallers.Contains(declaringType))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the latest quote for a specific trading pair
    /// </summary>
    /// <param name="pair">Trading pair symbol</param>
    /// <returns>Latest quote data if available</returns>
    public QuoteData? GetLatestQuote(string pair)
    {
        if (!IsAuthorizedCaller())
            throw new UnauthorizedAccessException("Quote access is restricted to authorized API clients only");
            
        if (string.IsNullOrWhiteSpace(pair)) return null;
        
        return _latestQuotes.TryGetValue(pair.ToUpperInvariant(), out var quote) ? quote : null;
    }

    /// <summary>
    /// Gets all latest quotes for all trading pairs
    /// </summary>
    /// <returns>Dictionary of all latest quotes</returns>
    public Dictionary<string, QuoteData> GetAllLatestQuotes()
    {
        if (!IsAuthorizedCaller())
            throw new UnauthorizedAccessException("Quote access is restricted to authorized API clients only");
            
        return new Dictionary<string, QuoteData>(_latestQuotes);
    }

    /// <summary>
    /// Gets recent quote history (up to MaxQuoteHistorySize entries)
    /// </summary>
    /// <param name="count">Number of recent quotes to retrieve</param>
    /// <returns>Collection of recent quotes</returns>
    public IEnumerable<QuoteData> GetRecentQuotes(int count = 100)
    {
        if (!IsAuthorizedCaller())
            throw new UnauthorizedAccessException("Quote access is restricted to authorized API clients only");
            
        var quotes = new List<QuoteData>();
        var tempQueue = new Queue<QuoteData>();
        
        // Extract quotes from concurrent queue
        while (_quoteHistory.TryDequeue(out var quote) && quotes.Count < count)
        {
            quotes.Add(quote);
            tempQueue.Enqueue(quote);
        }
        
        // Put them back
        while (tempQueue.TryDequeue(out var quote))
        {
            _quoteHistory.Enqueue(quote);
        }
        
        return quotes.OrderByDescending(q => q.Timestamp).Take(count);
    }

    /// <summary>
    /// Updates quote data for a trading pair
    /// </summary>
    /// <param name="quote">Quote data to update</param>
    public void UpdateQuote(QuoteData quote)
    {
        if (quote == null || string.IsNullOrWhiteSpace(quote.Pair)) return;

        var pairKey = quote.Pair.ToUpperInvariant();
        
        // Update latest quote
        _latestQuotes.AddOrUpdate(pairKey, quote, (key, oldValue) => quote);
        
        // Add to history
        _quoteHistory.Enqueue(quote);
        
        // Maintain history size limit
        lock (_quotesLock)
        {
            while (_quoteHistory.Count > MaxQuoteHistorySize)
            {
                _quoteHistory.TryDequeue(out _);
            }
        }
        
        // Fire event
        OnQuoteUpdated?.Invoke(quote);
    }

    /// <summary>
    /// Gets the current price for a trading pair
    /// </summary>
    /// <param name="pair">Trading pair symbol</param>
    /// <returns>Current price if available</returns>
    public double? GetCurrentPrice(string pair)
    {
        if (!IsAuthorizedCaller())
            throw new UnauthorizedAccessException("Quote access is restricted to authorized API clients only");
            
        var quote = GetLatestQuote(pair);
        return quote?.Price;
    }

    /// <summary>
    /// Checks if quote data is recent (within last 30 seconds)
    /// </summary>
    /// <param name="pair">Trading pair symbol</param>
    /// <returns>True if quote is recent</returns>
    public bool IsQuoteRecent(string pair)
    {
        if (!IsAuthorizedCaller())
            throw new UnauthorizedAccessException("Quote access is restricted to authorized API clients only");
            
        var quote = GetLatestQuote(pair);
        if (quote == null) return false;
        
        return (DateTime.UtcNow - quote.ReceivedAt).TotalSeconds <= 30;
    }

    /// <summary>
    /// Updates historical data for an asset
    /// </summary>
    /// <param name="historyData">Historical data to store</param>
    public void UpdateHistoricalData(HistoryData historyData)
    {
        if (!IsAuthorizedCaller())
            throw new UnauthorizedAccessException("Historical data access is restricted to authorized API clients only");
            
        if (historyData == null || string.IsNullOrWhiteSpace(historyData.Asset)) return;

        var assetKey = $"{historyData.Asset}_{historyData.Period}";
        
        // Update historical data
        _historicalData.AddOrUpdate(assetKey, historyData, (key, oldValue) => historyData);
        
        // Maintain size limit
        lock (_quotesLock)
        {
            if (_historicalData.Count > MaxHistoricalDataSize)
            {
                // Remove oldest entries by asset key (simple implementation)
                var keysToRemove = _historicalData.Keys.Take(_historicalData.Count - MaxHistoricalDataSize).ToList();
                foreach (var key in keysToRemove)
                {
                    _historicalData.TryRemove(key, out _);
                }
            }
        }
        
        // Fire event
        OnHistoricalDataUpdated?.Invoke(historyData);
    }

    /// <summary>
    /// Gets historical data for a specific asset and period
    /// </summary>
    /// <param name="asset">Asset symbol</param>
    /// <param name="period">Time period in seconds</param>
    /// <returns>Historical data if available</returns>
    public HistoryData? GetHistoricalData(string asset, int period)
    {
        if (!IsAuthorizedCaller())
            throw new UnauthorizedAccessException("Historical data access is restricted to authorized API clients only");
            
        if (string.IsNullOrWhiteSpace(asset)) return null;
        
        var assetKey = $"{asset}_{period}";
        return _historicalData.TryGetValue(assetKey, out var historyData) ? historyData : null;
    }

    /// <summary>
    /// Gets all available historical data
    /// </summary>
    /// <returns>Dictionary of all historical data</returns>
    public Dictionary<string, HistoryData> GetAllHistoricalData()
    {
        if (!IsAuthorizedCaller())
            throw new UnauthorizedAccessException("Historical data access is restricted to authorized API clients only");
            
        return new Dictionary<string, HistoryData>(_historicalData);
    }

    /// <summary>
    /// Gets historical data for a specific asset across all periods
    /// </summary>
    /// <param name="asset">Asset symbol</param>
    /// <returns>List of historical data for different periods</returns>
    public List<HistoryData> GetHistoricalDataForAsset(string asset)
    {
        if (!IsAuthorizedCaller())
            throw new UnauthorizedAccessException("Historical data access is restricted to authorized API clients only");
            
        if (string.IsNullOrWhiteSpace(asset)) return new List<HistoryData>();
        
        return _historicalData.Values
            .Where(h => string.Equals(h.Asset, asset, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    #endregion

    #region Events

    /// <summary>
    /// Event fired when balance is updated
    /// </summary>
    public event Action<decimal, decimal>? OnBalanceUpdated;

    /// <summary>
    /// Event fired when balance type changes
    /// </summary>
    public event Action<BinollaApiDotNet.BalanceType>? OnBalanceTypeChanged;

    /// <summary>
    /// Event fired when an order is opened
    /// </summary>
    public event Action<OpenedOrder>? OnOrderOpened;

    /// <summary>
    /// Event fired when an order fails
    /// </summary>
    public event Action<FailedOrderOpen>? OnOrderFailed;

    /// <summary>
    /// Event fired when an order is closed
    /// </summary>
    public event Action<string, double>? OnOrderClosed;

    /// <summary>
    /// Event fired when assets are updated
    /// </summary>
    public event Action<List<AssetData>>? OnAssetsUpdated;

    /// <summary>
    /// Event fired when connection status changes
    /// </summary>
    public event Action<bool>? OnConnectionStatusChanged;

    /// <summary>
    /// Event fired when a quote is updated
    /// </summary>
    public event Action<QuoteData>? OnQuoteUpdated;

    /// <summary>
    /// Event fired when historical data is updated
    /// </summary>
    public event Action<HistoryData>? OnHistoricalDataUpdated;

    #endregion

    #region Legacy Properties (for backward compatibility)

    /// <summary>
    /// Legacy property for SSL mutual exclusion
    /// </summary>
    [Obsolete("Use proper connection management instead")]
    public bool SslMutualExclusion { get; set; }

    /// <summary>
    /// Legacy property for SSL mutual exclusion write
    /// </summary>
    [Obsolete("Use proper connection management instead")]
    public bool SslMutualExclusionWrite { get; set; }

    /// <summary>
    /// Legacy property for reason
    /// </summary>
    [Obsolete("Use WebsocketErrorReason instead")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Legacy property for checking WebSocket errors
    /// </summary>
    [Obsolete("Use proper error handling instead")]
    public bool CheckWebsocketIfError { get; set; }

    /// <summary>
    /// Legacy property for raw balances
    /// </summary>
    [Obsolete("Use GetBalanceInfo() instead")]
    public Dictionary<string, double> BalancesRaw { get; set; } = new Dictionary<string, double>();

    /// <summary>
    /// Legacy property for payments
    /// </summary>
    [Obsolete("Use GetOpenTradingAssets() instead")]
    public Dictionary<string, double> Payments { get; set; } = new Dictionary<string, double>();

    /// <summary>
    /// Legacy property for result
    /// </summary>
    [Obsolete("Use proper result handling instead")]
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// Legacy property for raw close order data
    /// </summary>
    [Obsolete("Use GetRecentClosedOrders() instead")]
    public List<object> CloseOrderDataRaw { get; set; } = new List<object>();

    /// <summary>
    /// Legacy property for opened orders
    /// </summary>
    [Obsolete("Use order management methods instead")]
    public List<OpenedOrder> OpenedOrders { get; set; } = new List<OpenedOrder>();

    /// <summary>
    /// Legacy property for closed orders
    /// </summary>
    [Obsolete("Use GetRecentClosedOrders() instead")]
    public List<ClosedOrder> ClosedOrders { get; set; } = new List<ClosedOrder>();

    /// <summary>
    /// Legacy property for closed order data
    /// </summary>
    [Obsolete("Use order management methods instead")]
    public ClosedOrder? ClosedOrderData { get; set; } = null;

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Initialize thread-safe collections
    /// </summary>
    private void InitializeCollections()
    {
        // Collections are already initialized as fields
    }

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

    #endregion

    #region Utility Methods

    /// <summary>
    /// Resets all state to initial values
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _websocketIsConnected = false;
            _ssidAccepted = false;
            _websocketErrorReason = string.Empty;
            ConnectionEstablishedAt = null;
            
            lock (_balanceLock)
            {
                _balanceReal = 0m;
                _balanceDemo = 0m;
                _balanceUpdated = null;
            }
            
            _closeOrderPl.Clear();
            _orderData = null;
            _newOpenOrder = null;
            _failedOrderOpen = null;
            _orderOpenUuid = null;
            
            // Clear queues
            while (_openedOrdersQueue.TryDequeue(out _)) { }
            while (_closedOrdersQueue.TryDequeue(out _)) { }
            
            lock (_assetsLock)
            {
                _paymentAssets.Clear();
            }
        }
    }

    /// <summary>
    /// Gets comprehensive system status
    /// </summary>
    /// <returns>System status information</returns>
    public SystemStatus GetSystemStatus()
    {
        return new SystemStatus
        {
            IsConnected = _websocketIsConnected && _ssidAccepted,
            ConnectionDuration = ConnectionDuration,
            CurrentBalance = CurrentBalance,
            BalanceType = _balanceType == 1 ? BinollaApiDotNet.BalanceType.Demo : BinollaApiDotNet.BalanceType.Real,
            ActiveAssetsCount = _paymentAssets.Count(a => a.IsOpen),
            PendingOrdersCount = _openedOrdersQueue.Count,
            LastBalanceUpdate = _balanceUpdated,
            ErrorReason = _websocketErrorReason
        };
    }

    #endregion
}

/// <summary>
/// System status information
/// </summary>
public class SystemStatus
{
    public bool IsConnected { get; set; }
    public TimeSpan? ConnectionDuration { get; set; }
    public decimal CurrentBalance { get; set; }
    public BinollaApiDotNet.BalanceType BalanceType { get; set; }
    public int ActiveAssetsCount { get; set; }
    public int PendingOrdersCount { get; set; }
    public DateTime? LastBalanceUpdate { get; set; }
    public string ErrorReason { get; set; } = string.Empty;
}

/// <summary>
/// Enhanced balance information
/// </summary>
public class BalanceInfo
{
    public decimal RealBalance { get; set; }
    public decimal DemoBalance { get; set; }
    public BinollaApiDotNet.BalanceType CurrentType { get; set; }
    public DateTime LastUpdated { get; set; }
    public string Currency { get; set; } = "USD";

    public decimal CurrentBalance => CurrentType == BinollaApiDotNet.BalanceType.Real ? RealBalance : DemoBalance;
}

/// <summary>
/// Enhanced asset information with trading capabilities
/// </summary>
public class TradingAsset
{
    public string Symbol { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
    public decimal PayoutPercentage { get; set; }
    public decimal MinimumAmount { get; set; }
    public decimal MaximumAmount { get; set; }
    public int[] AvailableExpiryTimes { get; set; } = Array.Empty<int>();
    public AssetCategory Category { get; set; }
    public DateTime LastPriceUpdate { get; set; }
    public decimal? CurrentPrice { get; set; }
}

/// <summary>
/// Extension methods for enhanced functionality
/// </summary>
public static class GlobalsExtensions
{
    /// <summary>
    /// Extension method for null-conditional operations
    /// </summary>
    public static TResult? Let<T, TResult>(this T? obj, Func<T, TResult> func) 
        where T : struct
        where TResult : struct
    {
        return obj.HasValue ? func(obj.Value) : null;
    }
}