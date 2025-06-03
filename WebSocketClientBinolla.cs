namespace BinollaApiDotNet;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Globals;
using System.Collections.Generic;
using DataTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

/// <summary>
/// Enhanced WebSocket client for Binolla API with improved error handling and performance
/// </summary>
public class WebSocketClientBinolla : IDisposable
{
    #region Private Fields

    private readonly string _uri;
    private readonly SemaphoreSlim _sendSemaphore;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ConcurrentQueue<string> _incomingMessages;
    private readonly Timer _heartbeatTimer;
    private readonly object _connectionLock = new object();
    
    private ClientWebSocket? _webSocket;
    private Task? _receiveTask;
    private volatile bool _isConnected = false;
    private volatile bool _isDisposed = false;
    private volatile string _upcomingMessageType = string.Empty;
    private volatile int _reconnectAttempts = 0;
    
    // Logging control options
    private volatile bool _enableLogging = true;
    private volatile bool _enableDebugLogging = true;
    private volatile bool _enableInfoLogging = true;
    private volatile bool _enableWarningLogging = true;
    private volatile bool _enableErrorLogging = true;
    
    private const int MaxReconnectAttempts = 5;
    private const int HeartbeatIntervalMs = 25000; // 25 seconds
    private const int ReceiveBufferSize = 1024 * 100; // 100 KB
    private const int SendTimeoutMs = 5000;

    #endregion

    #region Events

    /// <summary>
    /// Delegate for WebSocket message callbacks
    /// </summary>
    /// <param name="eventType">Type of the event</param>
    /// <param name="message">Message content</param>
    public delegate void WssCallBack(string eventType, string message);
    
    /// <summary>
    /// Event fired when a WebSocket message is received
    /// </summary>
    public event WssCallBack? OnWssMessage;
    
    /// <summary>
    /// Event fired when connection state changes
    /// </summary>
    public event Action<bool>? OnConnectionStateChanged;
    
    /// <summary>
    /// Event fired when an error occurs
    /// </summary>
    public event Action<Exception>? OnError;

    #endregion

    #region Constructor

    /// <summary>
    /// Initialize WebSocket client with enhanced configuration
    /// </summary>
    /// <param name="uri">WebSocket server URI</param>
    /// <param name="enableLogging">Enable or disable all logging (default: true)</param>
    public WebSocketClientBinolla(string uri = "wss://ws3.binolla.com/socket.io/?EIO=4&transport=websocket", bool enableLogging = true)
    {
        _uri = uri;
        _sendSemaphore = new SemaphoreSlim(1, 1);
        _cancellationTokenSource = new CancellationTokenSource();
        _incomingMessages = new ConcurrentQueue<string>();
        
        // Setup heartbeat timer for connection health
        _heartbeatTimer = new Timer(SendHeartbeat, null, Timeout.Infinite, HeartbeatIntervalMs);
        
        // Initialize logging settings
        _enableLogging = enableLogging;
        
        InitializeWebSocket();
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the current connection state
    /// </summary>
    public bool IsConnected => _isConnected && _webSocket?.State == WebSocketState.Open;

    /// <summary>
    /// Gets the current WebSocket state
    /// </summary>
    public WebSocketState? State => _webSocket?.State;

    /// <summary>
    /// Gets the number of reconnection attempts made
    /// </summary>
    public int ReconnectAttempts => _reconnectAttempts;

    /// <summary>
    /// Gets or sets whether logging is enabled (master switch)
    /// </summary>
    public bool EnableLogging 
    { 
        get => _enableLogging; 
        set => _enableLogging = value; 
    }

    /// <summary>
    /// Gets or sets whether debug level logging is enabled
    /// </summary>
    public bool EnableDebugLogging 
    { 
        get => _enableDebugLogging; 
        set => _enableDebugLogging = value; 
    }

    /// <summary>
    /// Gets or sets whether info level logging is enabled
    /// </summary>
    public bool EnableInfoLogging 
    { 
        get => _enableInfoLogging; 
        set => _enableInfoLogging = value; 
    }

    /// <summary>
    /// Gets or sets whether warning level logging is enabled
    /// </summary>
    public bool EnableWarningLogging 
    { 
        get => _enableWarningLogging; 
        set => _enableWarningLogging = value; 
    }

    /// <summary>
    /// Gets or sets whether error level logging is enabled
    /// </summary>
    public bool EnableErrorLogging 
    { 
        get => _enableErrorLogging; 
        set => _enableErrorLogging = value; 
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Connect to the WebSocket server with enhanced error handling
    /// </summary>
    /// <param name="customHeaders">Optional custom headers</param>
    /// <param name="timeout">Connection timeout</param>
    /// <returns>Connection result</returns>
    public async Task<ConnectionResult> ConnectAsync(
        Dictionary<string, string>? customHeaders = null, 
        TimeSpan? timeout = null)
    {
        if (_isDisposed)
            return ConnectionResult.Failure("Client has been disposed");

        try
        {
            lock (_connectionLock)
            {
                if (_isConnected)
                    return ConnectionResult.Success("Already connected");

                InitializeWebSocket();
                ApplyCustomHeaders(customHeaders);
            }

            var connectTimeout = timeout ?? TimeSpan.FromSeconds(30);
            using var timeoutCts = new CancellationTokenSource(connectTimeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _cancellationTokenSource.Token, timeoutCts.Token);

            await _webSocket!.ConnectAsync(new Uri(_uri), combinedCts.Token);
            
            _isConnected = true;
            _reconnectAttempts = 0;
            
            // Start message receiving loop
            _receiveTask = Task.Run(async () => await ReceiveLoopAsync(combinedCts.Token), combinedCts.Token);
            
            // Start heartbeat
            _heartbeatTimer.Change(HeartbeatIntervalMs, HeartbeatIntervalMs);
            
            OnConnectionStateChanged?.Invoke(true);
            Log("Connected to WebSocket server successfully", LogLevel.Info);
            
            return ConnectionResult.Success("Connected successfully");
        }
        catch (OperationCanceledException) when (timeout.HasValue)
        {
            return ConnectionResult.Failure("Connection timed out");
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
            Log($"Connection failed: {ex.Message}", LogLevel.Error);
            return ConnectionResult.Failure($"Connection failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Send a message through the WebSocket with improved reliability
    /// </summary>
    /// <param name="message">Message to send</param>
    /// <param name="timeout">Send timeout</param>
    /// <returns>Send result</returns>
    public async Task<SendResult> SendMessageAsync(string message, TimeSpan? timeout = null)
    {
        if (_isDisposed || string.IsNullOrEmpty(message))
            return SendResult.Failure("Invalid state or message");

        if (!IsConnected)
            return SendResult.Failure("Not connected to server");

        var sendTimeout = timeout ?? TimeSpan.FromMilliseconds(SendTimeoutMs);
        
        try
        {
            using var timeoutCts = new CancellationTokenSource(sendTimeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _cancellationTokenSource.Token, timeoutCts.Token);

            await _sendSemaphore.WaitAsync(combinedCts.Token);
            
            try
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await _webSocket!.SendAsync(
                    new ArraySegment<byte>(buffer), 
                    WebSocketMessageType.Text, 
                    true, 
                    combinedCts.Token);
                
                Log($"Sent: {message}", LogLevel.Debug);
                return SendResult.Success("Message sent successfully");
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }
        catch (OperationCanceledException) when (timeout.HasValue)
        {
            return SendResult.Failure("Send operation timed out");
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
            Log($"Send failed: {ex.Message}", LogLevel.Error);
            return SendResult.Failure($"Send failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Disconnect from the WebSocket server gracefully
    /// </summary>
    /// <param name="timeout">Disconnect timeout</param>
    /// <returns>Disconnect result</returns>
    public async Task<DisconnectResult> DisconnectAsync(TimeSpan? timeout = null)
    {
        if (_isDisposed)
            return DisconnectResult.Success("Already disposed");

        try
        {
            _isConnected = false;
            _heartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
            
            var disconnectTimeout = timeout ?? TimeSpan.FromSeconds(10);
            using var timeoutCts = new CancellationTokenSource(disconnectTimeout);

            if (_webSocket?.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure, 
                    "Closed by client", 
                    timeoutCts.Token);
            }

            // Wait for receive task to complete
            if (_receiveTask != null && !_receiveTask.IsCompleted)
            {
                await _receiveTask.WaitAsync(timeoutCts.Token);
            }

            OnConnectionStateChanged?.Invoke(false);
            Log("Disconnected from WebSocket server", LogLevel.Info);
            
            return DisconnectResult.Success("Disconnected successfully");
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
            Log($"Disconnect failed: {ex.Message}", LogLevel.Error);
            return DisconnectResult.Failure($"Disconnect failed: {ex.Message}");
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Initialize WebSocket with proper configuration
    /// </summary>
    private void InitializeWebSocket()
    {
        _webSocket?.Dispose();
        _webSocket = new ClientWebSocket();
        
        // Configure WebSocket options
        _webSocket.Options.SetRequestHeader("Host", "ws3.binolla.com");
        _webSocket.Options.SetRequestHeader("Origin", "https://binolla.com");
        _webSocket.Options.SetRequestHeader("Cache-Control", "no-cache");
        _webSocket.Options.SetRequestHeader("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36");
        
        _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
    }

    /// <summary>
    /// Apply custom headers to WebSocket
    /// </summary>
    /// <param name="customHeaders">Headers to apply</param>
    private void ApplyCustomHeaders(Dictionary<string, string>? customHeaders)
    {
        if (customHeaders == null || _webSocket == null) return;

        foreach (var header in customHeaders)
        {
            try
            {
                _webSocket.Options.SetRequestHeader(header.Key, header.Value);
            }
            catch (Exception ex)
            {
                Log($"Failed to set header {header.Key}: {ex.Message}", LogLevel.Warning);
            }
        }
    }

    /// <summary>
    /// Main message receiving loop with improved error handling
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[ReceiveBufferSize];
        
        try
        {
            while (!cancellationToken.IsCancellationRequested && IsConnected)
            {
                try
                {
                    var result = await _webSocket!.ReceiveAsync(
                        new ArraySegment<byte>(buffer), 
                        cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
        {
                        Log("Server initiated close", LogLevel.Info);
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    
                    if (!string.IsNullOrWhiteSpace(message))
            {
                        await ProcessMessageAsync(message, result.MessageType);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                {
                    Log("Connection closed prematurely, attempting reconnect", LogLevel.Warning);
                    _ = Task.Run(() => AttemptReconnectAsync());
                    break;
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex);
                    Log($"Receive error: {ex.Message}", LogLevel.Error);
                    
                    if (ShouldAttemptReconnect(ex))
                    {
                        _ = Task.Run(() => AttemptReconnectAsync());
                        break;
                    }
                }
            }
        }
        finally
        {
            _isConnected = false;
            OnConnectionStateChanged?.Invoke(false);
        }
    }

    /// <summary>
    /// Process incoming messages with improved parsing
    /// </summary>
    /// <param name="message">Received message</param>
    /// <param name="messageType">Message type</param>
    private async Task ProcessMessageAsync(string message, WebSocketMessageType messageType)
    {
        try
        {
            Log($"Received: {message}", LogLevel.Debug);

            if (messageType == WebSocketMessageType.Binary)
            {
                await ProcessBinaryMessageAsync(message);
            }
            else
        {
                await ProcessTextMessageAsync(message);
        }

            OnWssMessage?.Invoke(_upcomingMessageType, message);
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
            Log($"Message processing error: {ex.Message}", LogLevel.Error);
        }
    }

    /// <summary>
    /// Process binary messages (data messages)
    /// </summary>
    /// <param name="message">Message content</param>
    private async Task ProcessBinaryMessageAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(_upcomingMessageType)) return;

        Log($"{_upcomingMessageType}: {(message.Length > 1000 ? "Large message" : message)}", LogLevel.Debug);

        var messageProcessor = new MessageProcessor();
        await messageProcessor.ProcessAsync(_upcomingMessageType, message);

        _upcomingMessageType = string.Empty;
    }

    /// <summary>
    /// Process text messages (control messages)
    /// </summary>
    /// <param name="message">Message content</param>
    private async Task ProcessTextMessageAsync(string message)
    {
        var controlProcessor = new ControlMessageProcessor(this);
        await controlProcessor.ProcessAsync(message);
    }

    /// <summary>
    /// Send heartbeat to maintain connection
    /// </summary>
    /// <param name="state">Timer state</param>
    private async void SendHeartbeat(object? state)
    {
        if (!IsConnected) return;
        
        try
        {
            //await SendMessageAsync("2");
        }
        catch (Exception ex)
        {
            Log($"Heartbeat failed: {ex.Message}", LogLevel.Warning);
        }
    }

    /// <summary>
    /// Determine if reconnection should be attempted
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    /// <returns>True if reconnection should be attempted</returns>
    private bool ShouldAttemptReconnect(Exception exception)
    {
        return _reconnectAttempts < MaxReconnectAttempts && 
               !_isDisposed && 
               exception is not OperationCanceledException;
    }

    /// <summary>
    /// Attempt to reconnect with exponential backoff
    /// </summary>
    private async Task AttemptReconnectAsync()
    {
        if (_isDisposed || _reconnectAttempts >= MaxReconnectAttempts) return;

        _reconnectAttempts++;
        var delay = TimeSpan.FromSeconds(Math.Pow(2, _reconnectAttempts)); // Exponential backoff
        
        Log($"Attempting reconnect #{_reconnectAttempts} in {delay.TotalSeconds} seconds", LogLevel.Info);
        
        try
        {
            await Task.Delay(delay, _cancellationTokenSource.Token);
            var result = await ConnectAsync();
            
            if (result.IsSuccess)
            {
                Log("Reconnection successful", LogLevel.Info);
                _reconnectAttempts = 0;
            }
            else
            {
                Log($"Reconnection failed: {result.ErrorMessage}", LogLevel.Warning);
            }
        }
        catch (Exception ex)
        {
            Log($"Reconnection attempt failed: {ex.Message}", LogLevel.Error);
        }
    }

    /// <summary>
    /// Enhanced logging with levels and configurable options
    /// </summary>
    /// <param name="message">Log message</param>
    /// <param name="level">Log level</param>
    private void Log(string message, LogLevel level)
    {
        // Check if logging is enabled globally
        if (!_enableLogging) return;
        
        // Check if specific log level is enabled
        var shouldLog = level switch
        {
            LogLevel.Debug => _enableDebugLogging,
            LogLevel.Info => _enableInfoLogging,
            LogLevel.Warning => _enableWarningLogging,
            LogLevel.Error => _enableErrorLogging,
            _ => true
        };
        
        if (!shouldLog) return;

        var color = level switch
        {
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Info => ConsoleColor.Green,
            LogLevel.Debug => ConsoleColor.Gray,
            _ => ConsoleColor.White
        };

        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} [{level}] {message}");
        Console.ForegroundColor = originalColor;
    }

    /// <summary>
    /// Configure all logging settings at once
    /// </summary>
    /// <param name="enableLogging">Master logging switch</param>
    /// <param name="enableDebug">Enable debug logging</param>
    /// <param name="enableInfo">Enable info logging</param>
    /// <param name="enableWarning">Enable warning logging</param>
    /// <param name="enableError">Enable error logging</param>
    public void ConfigureLogging(bool enableLogging = true, bool? enableDebug = null, 
        bool? enableInfo = null, bool? enableWarning = null, bool? enableError = null)
    {
        _enableLogging = enableLogging;
        
        if (enableDebug.HasValue) _enableDebugLogging = enableDebug.Value;
        if (enableInfo.HasValue) _enableInfoLogging = enableInfo.Value;
        if (enableWarning.HasValue) _enableWarningLogging = enableWarning.Value;
        if (enableError.HasValue) _enableErrorLogging = enableError.Value;
    }

    /// <summary>
    /// Disable all logging (silent mode)
    /// </summary>
    public void DisableAllLogging()
    {
        _enableLogging = false;
    }

    /// <summary>
    /// Enable only error logging (minimal mode)
    /// </summary>
    public void SetMinimalLogging()
    {
        _enableLogging = true;
        _enableDebugLogging = false;
        _enableInfoLogging = false;
        _enableWarningLogging = false;
        _enableErrorLogging = true;
    }

    /// <summary>
    /// Enable only error and warning logging (production mode)
    /// </summary>
    public void SetProductionLogging()
    {
        _enableLogging = true;
        _enableDebugLogging = false;
        _enableInfoLogging = false;
        _enableWarningLogging = true;
        _enableErrorLogging = true;
    }

    /// <summary>
    /// Enable all logging (development mode)
    /// </summary>
    public void SetDevelopmentLogging()
    {
        _enableLogging = true;
        _enableDebugLogging = true;
        _enableInfoLogging = true;
        _enableWarningLogging = true;
        _enableErrorLogging = true;
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Dispose of resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose method
    /// </summary>
    /// <param name="disposing">Whether disposing</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            _isConnected = false;
            
            _cancellationTokenSource.Cancel();
            _heartbeatTimer?.Dispose();
            _sendSemaphore?.Dispose();
            
            try
            {
                _receiveTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch { /* Ignore timeout */ }

            _webSocket?.Dispose();
            _cancellationTokenSource?.Dispose();
        }

        _isDisposed = true;
    }

    #endregion
}

#region Result Types

/// <summary>
/// Base result type for operations
/// </summary>
public abstract class OperationResult
{
    public bool IsSuccess { get; protected set; }
    public string Message { get; protected set; } = string.Empty;
    public string? ErrorMessage => IsSuccess ? null : Message;
    public DateTime Timestamp { get; protected set; } = DateTime.UtcNow;
}

/// <summary>
/// Connection operation result
/// </summary>
public class ConnectionResult : OperationResult
{
    private ConnectionResult() { }

    public static ConnectionResult Success(string message)
    {
        return new ConnectionResult { IsSuccess = true, Message = message };
    }

    public static ConnectionResult Failure(string message)
    {
        return new ConnectionResult { IsSuccess = false, Message = message };
    }
}

/// <summary>
/// Send operation result
/// </summary>
public class SendResult : OperationResult
{
    private SendResult() { }

    public static SendResult Success(string message)
    {
        return new SendResult { IsSuccess = true, Message = message };
    }

    public static SendResult Failure(string message)
    {
        return new SendResult { IsSuccess = false, Message = message };
    }
}

/// <summary>
/// Disconnect operation result
/// </summary>
public class DisconnectResult : OperationResult
{
    private DisconnectResult() { }

    public static DisconnectResult Success(string message)
    {
        return new DisconnectResult { IsSuccess = true, Message = message };
    }

    public static DisconnectResult Failure(string message)
    {
        return new DisconnectResult { IsSuccess = false, Message = message };
    }
}

#endregion

#region Helper Classes

/// <summary>
/// Log levels for enhanced logging
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

/// <summary>
/// Enhanced message processor for binary messages
/// </summary>
public class MessageProcessor
{
    /// <summary>
    /// Process different types of binary messages
    /// </summary>
    /// <param name="messageType">Type of message</param>
    /// <param name="content">Message content</param>
    public async Task ProcessAsync(string messageType, string content)
    {
        try
        {
            switch (messageType)
            {
                case "s_balance/update":
                    await ProcessBalanceUpdateAsync(content);
                    break;
                case "s_balances/list":
                    await ProcessBalanceListAsync(content);
                    break;
                case "s_orders/open":
                    await ProcessOrderOpenAsync(content);
                    break;
                case "f_orders/open":
                    await ProcessOrderFailedAsync(content);
                    break;
                case "s_orders/close":
                case "s_orders/closed/list":
                    await ProcessOrderCloseAsync(content);
                    break;
                case "s_assets/list":
                    await ProcessAssetsListAsync(content);
                    break;
                case "s_quotes/list":
                    await ProcessQuotesListAsync(content);
                    break;
                case "s_history/last":
                    await ProcessHistoryLastAsync(content);
                    break;
                default:
                    // Unknown message type - log for debugging
                    break;
            }
        }
        catch (Exception ex)
        {
            throw new MessageProcessingException($"Failed to process {messageType}: {ex.Message}", ex);
        }
    }

    private async Task ProcessBalanceUpdateAsync(string content)
    {
        
        
        var balanceData = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
        if (balanceData == null) return;

        var balance = Convert.ToDouble(balanceData["balance"]);
        
        // Handle both boolean and integer values for isDemo
        bool isDemo;
        var isDemoValue = balanceData["isDemo"];
        if (isDemoValue is bool boolValue)
        {
            isDemo = boolValue;
        }
        else
        {
            isDemo = Convert.ToInt32(isDemoValue) == 1;
        }

        if (isDemo)
                {
                    Values.BalanceDemo = balance;
                }
                else
                {
                    Values.BalanceReal = balance;
                }

                Values.BalanceUpdated = DateTime.Now;
        await Task.CompletedTask;
    }

    private async Task ProcessBalanceListAsync(string content)
    {
        var balanceData = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
        if (balanceData == null) return;

        Values.BalanceDemo = Convert.ToDouble(balanceData["demoBalance"]);
        Values.BalanceReal = Convert.ToDouble(balanceData["liveBalance"]);
                Values.BalanceUpdated = DateTime.Now;
        await Task.CompletedTask;
            }

    private async Task ProcessOrderOpenAsync(string content)
            {
        var order = JsonConvert.DeserializeObject<OpenedOrder>(content);
        if (order == null) return;
                
                Values.OrderData = order;
                Values.NewOpenOrder = order;
                Values.OrderOpenUuid = order.Deal.Uuid;                
        await Task.CompletedTask;
            }

    private async Task ProcessOrderFailedAsync(string content)
            {
        var failedOrder = JsonConvert.DeserializeObject<FailedOrderOpen>(content);
                Values.FailedOrderOpen = failedOrder;
        await Task.CompletedTask;
    }

    private async Task ProcessOrderCloseAsync(string content)
    {
        try
        {
            // Handle different message types for closed orders
            if (content.StartsWith("["))
            {
                // This is s_orders/closed/list - array of individual deals
                var deals = JsonConvert.DeserializeObject<List<Deal>>(content);
                if (deals != null && deals.Count > 0)
                {
                    // Process each deal individually
                    foreach (var deal in deals)
                    {
                        if (deal?.Uuid != null)
                        {
                            // Create a ClosedOrder wrapper for each deal for compatibility
                            var closedOrder = new ClosedOrder
                            {
                                Profit = deal.Profit,
                                Deals = new List<Deal> { deal }
                            };
                            
                            Values.AddClosedOrder(closedOrder);
                        }
                    }
                }
            }
            else if (content.StartsWith("{"))
            {
                // This is s_orders/close - single order with deals array
                var closedOrder = JsonConvert.DeserializeObject<ClosedOrder>(content);
                if (closedOrder?.Deals != null)
                {
                    Values.AddClosedOrder(closedOrder);
                }
            }
            else
            {
                //Log($"Unexpected closed order data format: {content.Substring(0, Math.Min(100, content.Length))}", LogLevel.Warning);
            }
        }
        catch (JsonException ex)
        {
            // Try alternative: maybe it's a single deal object
            try
            {
                var deal = JsonConvert.DeserializeObject<Deal>(content);
                if (deal?.Uuid != null)
                {
                    var closedOrder = new ClosedOrder
                    {
                        Profit = deal.Profit,
                        Deals = new List<Deal> { deal }
                    };
                    
                    Values.AddClosedOrder(closedOrder);
                }
            }
            catch (JsonException ex2)
            {
                throw new MessageProcessingException(
                    $"Failed to deserialize closed order data as array, object, or single deal: {ex.Message}; {ex2.Message}", ex);
            }
        }
        
        await Task.CompletedTask;
    }

    private async Task ProcessAssetsListAsync(string content)
    {
        try
        {
            // Parse the JSON array containing asset data arrays
            var assetsData = JsonConvert.DeserializeObject<List<List<object>>>(content);
            if (assetsData == null) return;

            var assetList = new List<AssetData>();

            foreach (var assetArray in assetsData)
            {
                try
                {
                    // Each asset array should have at least 28 elements
                    // Expected format: [ActiveId, Name, Description, Type, Precision, Payout, ..., IsOpen(14), ..., TradeType(27)]
                    if (assetArray == null || assetArray.Count < 28) continue;

                    var asset = new AssetData
                    {
                        ActiveId = Convert.ToInt32(assetArray[0]),
                        Name = assetArray[1]?.ToString() ?? string.Empty,
                        Description = assetArray[2]?.ToString() ?? string.Empty,
                        Type = assetArray[3]?.ToString() ?? string.Empty,
                        Precision = Convert.ToInt32(assetArray[4]),
                        Payout = Convert.ToInt32(assetArray[5]),
                        // Array position 10 seems to be OTC ID
                        OtcId = assetArray[10] != null && int.TryParse(assetArray[10].ToString(), out var otcId) ? otcId : null,
                        // Array position 11 seems to be base asset ID  
                        BaseAssetId = assetArray[11] != null && int.TryParse(assetArray[11].ToString(), out var baseId) ? baseId : null,
                        // Array position 13 is close timestamp
                        CloseTimestamp = assetArray[13] != null && long.TryParse(assetArray[13].ToString(), out var closeTime) ? closeTime : null,
                        IsOpen = Convert.ToBoolean(assetArray[14]),
                        // Array position 17 seems to be price change
                        PriceChange = assetArray[17] != null && double.TryParse(assetArray[17].ToString(), out var priceChange) ? priceChange : null,
                        // Array position 18 might be alternative payout
                        AlternativePayout = assetArray[18] != null && int.TryParse(assetArray[18].ToString(), out var altPayout) ? altPayout : null,
                        // Array position 19 might be max payout
                        MaxPayout = assetArray[19] != null && int.TryParse(assetArray[19].ToString(), out var maxPayout) ? maxPayout : null,
                        // Price indicators from positions 21-26
                        PriceIndicator1 = assetArray.Count > 21 && assetArray[21] != null && double.TryParse(assetArray[21].ToString(), out var p1) ? p1 : null,
                        PriceIndicator2 = assetArray.Count > 22 && assetArray[22] != null && double.TryParse(assetArray[22].ToString(), out var p2) ? p2 : null,
                        PriceIndicator3 = assetArray.Count > 23 && assetArray[23] != null && double.TryParse(assetArray[23].ToString(), out var p3) ? p3 : null,
                        PriceIndicator4 = assetArray.Count > 24 && assetArray[24] != null && double.TryParse(assetArray[24].ToString(), out var p4) ? p4 : null,
                        PriceIndicator5 = assetArray.Count > 25 && assetArray[25] != null && double.TryParse(assetArray[25].ToString(), out var p5) ? p5 : null,
                        PriceIndicator6 = assetArray.Count > 26 && assetArray[26] != null && double.TryParse(assetArray[26].ToString(), out var p6) ? p6 : null,
                        // Status code from position 27
                        StatusCode = assetArray.Count > 27 && assetArray[27] != null && int.TryParse(assetArray[27].ToString(), out var status) ? status : null,
                        // Trade type from position 28 (last position)
                        TradeType = assetArray.Count > 28 && assetArray[28]?.ToString() == "fixed_time" ? TradeType.fixed_time : TradeType.blitz
                    };

                    assetList.Add(asset);
                }
                catch (Exception ex)
                {
                    // Log individual asset parsing error but continue with others
                    // This ensures one malformed asset doesn't break the entire batch
                    continue;
                }
            }

            // Update the global assets storage
            Values.PaymentAssets = assetList;
            
            // Log the number of assets processed
            //Console.WriteLine($"Processed {assetList.Count} assets from s_assets/list");
        }
        catch (JsonException ex)
        {
            throw new MessageProcessingException($"Failed to deserialize assets data: {ex.Message}", ex);
        }
        
        await Task.CompletedTask;
    }

    private async Task ProcessQuotesListAsync(string content)
    {
        try
        {
            // Parse the JSON array containing quote data
            var quotesData = JsonConvert.DeserializeObject<List<List<object>>>(content);
            if (quotesData == null) return;

            // Process each quote in the array
            foreach (var quoteArray in quotesData)
            {
                try
                {
                    // Expected format: ["EURUSD", 1748946165.002, 1.14041, 0]
                    // [0] = Pair, [1] = Timestamp, [2] = Price, [3] = Additional Data
                    if (quoteArray.Count < 3) continue;

                    var pair = quoteArray[0]?.ToString();
                    if (string.IsNullOrWhiteSpace(pair)) continue;

                    // Parse timestamp (Unix timestamp with milliseconds)
                    if (!double.TryParse(quoteArray[1]?.ToString(), out var timestamp)) continue;

                    // Parse price
                    if (!double.TryParse(quoteArray[2]?.ToString(), out var price)) continue;

                    // Parse additional data (can be null, numeric, or other)
                    var additionalData = quoteArray.Count > 3 ? quoteArray[3] : null;

                    // Create quote data object
                    var quote = new QuoteData(pair, timestamp, price, additionalData);

                    // Update the global quote storage
                    Values.UpdateQuote(quote);
                }
                catch (Exception ex)
                {
                    // Log individual quote parsing error but continue with others
                    // This ensures one malformed quote doesn't break the entire batch
                    continue;
                }
            }
        }
        catch (JsonException ex)
        {
            throw new MessageProcessingException($"Failed to deserialize quotes data: {ex.Message}", ex);
        }
        
        await Task.CompletedTask;
    }

    private async Task ProcessHistoryLastAsync(string content)
    {
        try
        {
            // Parse the JSON object containing history data
            var historyMessage = JsonConvert.DeserializeObject<JObject>(content);
            if (historyMessage == null) return;

            var asset = historyMessage["asset"]?.ToString();
            if (string.IsNullOrWhiteSpace(asset)) return;

            var period = historyMessage["period"]?.ToObject<int>() ?? 60;

            var historyData = new HistoryData
            {
                Asset = asset,
                Period = period,
                ReceivedAt = DateTime.UtcNow
            };

            // Process tick data from "history" array
            var historyArray = historyMessage["history"] as JArray;
            if (historyArray != null)
            {
                foreach (var historyItem in historyArray)
                {
                    try
                    {
                        var itemArray = historyItem as JArray;
                        if (itemArray == null || itemArray.Count < 2) continue;

                        // Tick data format: [timestamp, price, additional_data]
                        if (!double.TryParse(itemArray[0]?.ToString(), out var timestamp)) continue;
                        if (!double.TryParse(itemArray[1]?.ToString(), out var price)) continue;

                        var additionalData = itemArray.Count > 2 ? itemArray[2] : null;
                        var tickData = new TickData(timestamp, price, additionalData);
                        
                        historyData.TickHistory.Add(tickData);
                    }
                    catch (Exception ex)
                    {
                        // Log individual tick parsing error but continue with others
                        continue;
                    }
                }
            }

            // Process candlestick data from "candles" array
            var candlesArray = historyMessage["candles"] as JArray;
            if (candlesArray != null && candlesArray.Count > 0)
            {
                // Process all candles in the array
                foreach (var candleItem in candlesArray)
                {
                    try
                    {
                        var candleArray = candleItem as JArray;
                        if (candleArray == null || candleArray.Count < 5) continue;

                        // Candlestick format: [timestamp, open, low, high, close, volume?, end_timestamp?]
                        if (double.TryParse(candleArray[0]?.ToString(), out var timestamp) &&
                            double.TryParse(candleArray[1]?.ToString(), out var open) &&
                            double.TryParse(candleArray[2]?.ToString(), out var low) &&
                            double.TryParse(candleArray[3]?.ToString(), out var high) &&
                            double.TryParse(candleArray[4]?.ToString(), out var close))
                        {
                            double? volume = null;
                            double? endTimestamp = null;

                            if (candleArray.Count > 5 && candleArray[5] != null && double.TryParse(candleArray[5].ToString(), out var vol))
                                volume = vol;

                            if (candleArray.Count > 6 && candleArray[6] != null && double.TryParse(candleArray[6].ToString(), out var endTime))
                                endTimestamp = endTime;

                            var candleData = new CandlestickData(timestamp, open, low, high, close, volume, endTimestamp);
                            historyData.Candles.Add(candleData);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log individual candle parsing error but continue with others
                        continue;
                    }
                }

                // Set the most recent candle as the primary Candlestick for backward compatibility
                if (historyData.Candles.Count > 0)
                {
                    historyData.Candlestick = historyData.Candles.Last();
                }
            }

            // Update the global historical data storage
            Values.UpdateHistoricalData(historyData);
            
            // Log the processing result
            //Console.WriteLine($"Processed history for {asset} ({period}s): {historyData.TickHistory.Count} ticks" +(historyData.Candlestick != null ? " + OHLC data" : "") +(candlesArray?.Count > 0 ? $" + {candlesArray.Count} candles" : ""));
        }
        catch (JsonException ex)
        {
            throw new MessageProcessingException($"Failed to deserialize history data: {ex.Message}", ex);
        }
        
        await Task.CompletedTask;
    }
}

/// <summary>
/// Control message processor for text messages
/// </summary>
public class ControlMessageProcessor
{
    private readonly WebSocketClientBinolla _client;

    public ControlMessageProcessor(WebSocketClientBinolla client)
    {
        _client = client;
    }

    /// <summary>
    /// Process control messages
    /// </summary>
    /// <param name="message">Message content</param>
    public async Task ProcessAsync(string message)
    {
        if (message.StartsWith("0") && message.Contains("sid"))
        {
            await _client.SendMessageAsync("40");
        }
        else if (message.StartsWith("40") && message.Contains("sid"))
        {
            await _client.SendMessageAsync(Values.SSID);
        }
        else if (message.StartsWith("42") && message.Contains("s_authorization"))
        {
            await HandleAuthorizationAsync();
        }
        else if (message == "2")
        {
            await _client.SendMessageAsync("3");
        }
        else if (message.StartsWith("451-["))
        {
            await HandleBinaryMessageHeaderAsync(message);
        }
        else if (message.StartsWith("42") && message.Contains("NotAuthorized"))
        {
            HandleAuthorizationError();
        }
    }

    private async Task HandleAuthorizationAsync()
    {
        Values.WebsocketIsConnected = true;
        Values.SSIDAccepted = true;

        // Send initialization commands with delays
        var initCommands = new[]
        {
            "42[\"account/change\",{\"demo\":1}]",
            "42[\"orders/opened/list\"]",
            "42[\"orders/closed/list\"]",
            "42[\"assets/list\"]",
            "42[\"alert/list\"]",
            "42[\"alert/closed/list\"]",
            "42[\"indicator/list\"]",
            "42[\"drawing/load\"]",
            //"42[\"asset/change\",{\"asset\":\"EURUSD\",\"period\":60}]"
        };

        foreach (var command in initCommands)
        {
            await _client.SendMessageAsync(command);
            await Task.Delay(10);
        }
    }

    private async Task HandleBinaryMessageHeaderAsync(string message)
    {
        try
        {
            var jsonPart = message.Split('-')[1];
            var jsonMessage = JsonConvert.DeserializeObject<List<object>>(jsonPart);
            
            if (jsonMessage?.Count > 0)
            {
                // This is a hacky way to set the upcoming message type
                // In a real implementation, this should be handled more elegantly
                var field = typeof(WebSocketClientBinolla).GetField("_upcomingMessageType", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(_client, jsonMessage[0]?.ToString() ?? string.Empty);
            }
        }
        catch (Exception ex)
        {
            // Log parsing error
        }
        
        await Task.CompletedTask;
    }

    private void HandleAuthorizationError()
    {
        Values.WebsocketErrorReason = "User not Authorized: Please change SSID for one valid";
        Console.Error.WriteLine(Values.WebsocketErrorReason);
    }
}

/// <summary>
/// Custom exception for message processing errors
/// </summary>
public class MessageProcessingException : Exception
{
    public MessageProcessingException(string message) : base(message) { }
    public MessageProcessingException(string message, Exception innerException) : base(message, innerException) { }
}

#endregion
