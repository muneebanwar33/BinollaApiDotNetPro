namespace BinollaApiDotNet;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Globals;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// WebSocket client for Binolla chart/price data
/// </summary>
public class ChartWebSocketClient : IDisposable
{
    #region Private Fields

    private readonly string _uri;
    private readonly SemaphoreSlim _sendSemaphore;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly object _connectionLock = new object();
    
    private ClientWebSocket? _webSocket;
    private Task? _receiveTask;
    private volatile bool _isConnected = false;
    private volatile bool _isDisposed = false;
    private volatile int _reconnectAttempts = 0;
    
    // Logging control options
    private volatile bool _enableLogging = true;
    private volatile bool _enableDebugLogging = true;
    private volatile bool _enableInfoLogging = true;
    private volatile bool _enableWarningLogging = true;
    private volatile bool _enableErrorLogging = true;
    
    private const int MaxReconnectAttempts = 5;
    private const int ReceiveBufferSize = 1024 * 16;
    private const int SendTimeoutMs = 5000;

    #endregion

    #region Events

    /// <summary>
    /// Delegate for chart WebSocket message callbacks
    /// </summary>
    /// <param name="message">Message content</param>
    public delegate void ChartMessageCallback(string message);
    
    /// <summary>
    /// Event fired when a chart message is received
    /// </summary>
    public event ChartMessageCallback? OnChartMessage;
    
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
    /// Initialize chart WebSocket client
    /// </summary>
    /// <param name="uri">WebSocket server URI</param>
    /// <param name="enableLogging">Enable or disable all logging (default: true)</param>
    public ChartWebSocketClient(string uri = "wss://ws2.binolla.com/ws", bool enableLogging = true)
    {
        _uri = uri;
        _sendSemaphore = new SemaphoreSlim(1, 1);
        _cancellationTokenSource = new CancellationTokenSource();
        
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
    /// Connect to the chart WebSocket server
    /// </summary>
    /// <param name="timeout">Connection timeout</param>
    /// <returns>Connection result</returns>
    public async Task<bool> ConnectAsync(TimeSpan? timeout = null)
    {
        if (_isDisposed)
            return false;

        try
        {
            lock (_connectionLock)
            {
                if (_isConnected)
                    return true;

                InitializeWebSocket();
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
            
            OnConnectionStateChanged?.Invoke(true);
            Log("Connected to chart WebSocket server successfully", LogLevel.Info);
            
            // Send SSID after connection
            await SendSSIDAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
            Log($"Chart connection failed: {ex.Message}", LogLevel.Error);
            return false;
        }
    }

    /// <summary>
    /// Send a message through the WebSocket
    /// </summary>
    /// <param name="message">Message to send</param>
    /// <param name="timeout">Send timeout</param>
    /// <returns>Send result</returns>
    public async Task<bool> SendMessageAsync(string message, TimeSpan? timeout = null)
    {
        if (_isDisposed || string.IsNullOrEmpty(message))
            return false;

        if (!IsConnected)
            return false;

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
                
                Log($"Chart sent: {message}", LogLevel.Debug);
                return true;
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
            Log($"Chart send failed: {ex.Message}", LogLevel.Error);
            return false;
        }
    }

    /// <summary>
    /// Send asset change message for chart data
    /// </summary>
    /// <param name="asset">Asset symbol (e.g., "EURUSD")</param>
    /// <param name="period">Time period in seconds (e.g., 60)</param>
    /// <returns>Success status</returns>
    public async Task<bool> SendAssetChangeAsync(string asset = "EURUSD", int period = 60)
    {
        var message = JsonConvert.SerializeObject(new object[] 
        { 
            "asset/change", 
            new { asset = asset, period = period } 
        });
        
        return await SendMessageAsync(message);
    }

    /// <summary>
    /// Disconnect from the WebSocket server gracefully
    /// </summary>
    /// <param name="timeout">Disconnect timeout</param>
    /// <returns>Disconnect result</returns>
    public async Task<bool> DisconnectAsync(TimeSpan? timeout = null)
    {
        if (_isDisposed)
            return true;

        try
        {
            _isConnected = false;
            
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
            Log("Disconnected from chart WebSocket server", LogLevel.Info);
            
            return true;
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
            Log($"Chart disconnect failed: {ex.Message}", LogLevel.Error);
            return false;
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
        
        // Configure WebSocket options - same headers as main client but different host
        _webSocket.Options.SetRequestHeader("Host", "ws2.binolla.com");
        _webSocket.Options.SetRequestHeader("Origin", "https://binolla.com");
        _webSocket.Options.SetRequestHeader("Cache-Control", "no-cache");
        _webSocket.Options.SetRequestHeader("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36");
        
        _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
    }

    /// <summary>
    /// Send SSID after connection
    /// </summary>
    private async Task SendSSIDAsync()
    {
        if (!string.IsNullOrEmpty(Values.SSID))
        {
            await SendMessageAsync(Values.SSID);
        }
    }

    /// <summary>
    /// Main message receiving loop
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
                        Log("Chart server initiated close", LogLevel.Info);
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        await ProcessChartMessageAsync(message);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                {
                    Log("Chart connection closed prematurely, attempting reconnect", LogLevel.Warning);
                    _ = Task.Run(() => AttemptReconnectAsync());
                    break;
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex);
                    Log($"Chart receive error: {ex.Message}", LogLevel.Error);
                    
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
    /// Process incoming chart messages
    /// </summary>
    /// <param name="message">Received message</param>
    private async Task ProcessChartMessageAsync(string message)
    {
        try
        {
            Log($"Chart received: {message}", LogLevel.Debug);

            // Fire event for external handling
            OnChartMessage?.Invoke(message);

            // Handle specific chart protocol messages if needed
            if (message.Contains("connected") || message.Contains("auth"))
            {
                // Send asset change message after successful connection/auth
                await SendAssetChangeAsync();
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
            Log($"Chart message processing error: {ex.Message}", LogLevel.Error);
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
        
        Log($"Attempting chart reconnect #{_reconnectAttempts} in {delay.TotalSeconds} seconds", LogLevel.Info);
        
        try
        {
            await Task.Delay(delay, _cancellationTokenSource.Token);
            var result = await ConnectAsync();
            
            if (result)
            {
                Log("Chart reconnection successful", LogLevel.Info);
                _reconnectAttempts = 0;
            }
            else
            {
                Log("Chart reconnection failed", LogLevel.Warning);
            }
        }
        catch (Exception ex)
        {
            Log($"Chart reconnection attempt failed: {ex.Message}", LogLevel.Error);
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
        Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} [CHART-{level}] {message}");
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