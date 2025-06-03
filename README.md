# 🚀 JOIN RISING DEVELOPERS COMMUNITY

[![Discord](https://img.shields.io/discord/843927072789317376?color=blue&label=Join%20Community&logo=discord&style=for-the-badge)](https://discord.gg/UW4C5VhWCH)
[![GitHub stars](https://img.shields.io/github/stars/muneebanwar33/BinollaApiDotNet?style=for-the-badge)](https://github.com/muneebanwar33/BinollaApiDotNet/stargazers)
[![GitHub issues](https://img.shields.io/github/issues/muneebanwar33/BinollaApiDotNet?style=for-the-badge)](https://github.com/muneebanwar33/BinollaApiDotNet/issues)
[![License](https://img.shields.io/github/license/muneebanwar33/BinollaApiDotNet?style=for-the-badge)](LICENSE)

# 💎 BinollaApiDotNet (Unofficial)

**The most comprehensive C# API for Binolla.com trading automation** 🎯

BinollaApiDotNet is a powerful, feature-rich C# library designed to provide seamless integration with the Binolla trading platform through WebSocket connections. This unofficial API enables developers to build sophisticated trading bots, automated strategies, and custom trading applications with enterprise-grade reliability and performance.

## 📋 Table of Contents

- [🌟 Key Features](#-key-features)
- [🔧 Technical Specifications](#-technical-specifications)
- [📦 Installation & Setup](#-installation--setup)
- [🚀 Quick Start Guide](#-quick-start-guide)
- [📖 Detailed API Documentation](#-detailed-api-documentation)
- [💡 Advanced Usage Examples](#-advanced-usage-examples)
- [🛡️ Security & Best Practices](#️-security--best-practices)
- [🔧 Troubleshooting](#-troubleshooting)
- [🤝 Contributing](#-contributing)
- [📞 Support & Contact](#-support--contact)
- [📄 License](#-license)

## 🌟 Key Features

### 🔗 Core Connectivity
- **Real-time WebSocket Connection**: Ultra-fast, low-latency connection to Binolla's trading infrastructure
- **🕵️ Stealth Authentication**: Browser-session mimicking technology for enhanced security
- **🔄 Auto-Reconnection**: Intelligent reconnection handling with exponential backoff
- **📡 Event-Driven Architecture**: Reactive programming model for real-time market updates

### 💰 Account Management
- **💳 Balance Retrieval**: Real-time account balance monitoring
- **🔄 Balance Type Switching**: Seamless switching between Demo and Real accounts
- **👤 Account Information**: Comprehensive account details and status
- **📊 Portfolio Tracking**: Monitor your trading performance and statistics

### 📈 Trading Operations
- **🛒 Order Placement**: Execute Call/Put orders with precision timing
- **📊 Asset Information**: Real-time asset prices and payout rates
- **🏆 Win/Loss Tracking**: Automated trade result monitoring
- **⏱️ Custom Timeframes**: Support for multiple expiration times
- **💸 Flexible Investment**: Custom investment amounts per trade

### 🔍 Market Data
- **📊 Live Pricing**: Real-time asset price feeds
- **💰 Payout Rates**: Current payout percentages for all assets
- **📈 Market Status**: Trading session information and asset availability
- **🕐 Server Time**: Synchronized time for accurate trade execution

## 🔧 Technical Specifications

- **Framework**: .NET 8.0 (LTS)
- **Architecture**: Event-driven, asynchronous programming model
- **Protocol**: WebSocket (RFC 6455) with Socket.IO implementation
- **Data Format**: JSON with custom Binolla protocol
- **Dependencies**: Minimal external dependencies for optimal performance
- **Platform Support**: Windows, macOS, Linux (cross-platform)

## 📦 Installation & Setup

### Prerequisites

- **.NET 8.0 SDK** or later
- **Visual Studio 2022** or **Visual Studio Code** (recommended)
- **Active Binolla Account** with browser session access

### 1. Clone the Repository

```bash
git clone https://github.com/muneebanwar33/BinollaApiDotNet.git
cd BinollaApiDotNet
```

### 2. Add to Your Solution

```bash
# Add to existing solution
dotnet sln add BinollaApiDotNet.csproj

# Or create new solution
dotnet new sln -n MyTradingBot
dotnet sln add BinollaApiDotNet.csproj
```

### 3. Install Dependencies

```bash
dotnet restore
dotnet build
```

## 🚀 Quick Start Guide

### Step 1: Obtain Your Session Token 🔑

**Important**: This API uses browser session authentication for enhanced security.

#### 📸 Visual Guide: How to Extract Your Session Token

1. **Login to Binolla.com** in your browser
2. **Open Developer Tools** (Press `F12` or right-click → "Inspect")
3. **Navigate to Network tab**
4. **Click on "WS" filter** to show WebSocket connections only
5. **Refresh the page** to capture the WebSocket handshake
6. **Look for the authorization message** in the messages list
7. **Right-click on the authorization message** and select "Copy message"

![How to get session token](ssid%20help.png)

The authorization message should look like this:

```json
42["authorization",{"isDemo":true,"token":"h4ZUUsfGs1yOQ9Rhowyb3AZHeNKKSkRrwWfV6wLN"}]
```

#### 🔍 Step-by-Step Instructions:

1. **🌐 Navigate to Binolla.com** and log into your account
2. **🔧 Open Developer Tools**:
   - **Windows/Linux**: Press `F12` or `Ctrl+Shift+I`
   - **macOS**: Press `Cmd+Option+I`
   - **Alternative**: Right-click anywhere → "Inspect Element"
3. **📡 Go to Network Tab** in the developer tools
4. **🔌 Filter WebSocket Messages**:
   - Click on **"WS"** button to filter WebSocket connections
   - This will show only WebSocket traffic
5. **🔄 Refresh the Page** (`F5` or `Ctrl+R` / `Cmd+R`)
6. **🔍 Find the Authorization Message**:
   - Look for a message that starts with `42["authorization"`
   - It will contain your session token
7. **📋 Copy the Token**:
   - Right-click on the authorization message
   - Select **"Copy message"**
   - This copies the entire authentication string you need

#### ⚠️ Important Security Notes:

- **🔒 Keep your token private** - never share it publicly
- **⏰ Tokens expire** - you may need to get a fresh token periodically
- **🔄 Token format** - make sure to copy the entire message including `42[` at the beginning
- **💻 Demo vs Real** - the `isDemo` field shows whether you're on demo or real account

### Step 2: Basic Implementation 🛠️

```csharp
using BinollaApiDotNet;

class Program
{
    static async Task Main(string[] args)
    {
        // Your session token from browser
        string sessionToken = "42[\"authorization\",{\"isDemo\":true,\"token\":\"h4ZUUsfGs1yOQ9Rhowyb3AZHeNKKSkRrwWfV6wLN\"}]";
        
        // Initialize the API client
        var binollaApi = new BinollaApi(sessionToken);
        
        try
        {
            // Connect to Binolla
            bool isConnected = await binollaApi.ConnectAsync();
            
            if (isConnected)
            {
                Console.WriteLine("🎉 Successfully connected to Binolla!");
                
                // Get account balance
                var balance = await binollaApi.GetBalanceAsync();
                Console.WriteLine($"💰 Current Balance: ${balance:F2}");
                
                // Get available assets and payouts
                var assets = await binollaApi.GetPaymentAsync();
                Console.WriteLine("📊 Available Assets:");
                foreach (var asset in assets)
                {
                    Console.WriteLine($"   {asset.Key}: {asset.Value:P} payout");
                }
            }
            else
            {
                Console.WriteLine("❌ Failed to connect to Binolla");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🚨 Error: {ex.Message}");
        }
    }
}
```

## 📖 Detailed API Documentation

### 🔌 Connection Management

#### `ConnectAsync()`
Establishes WebSocket connection to Binolla platform.

```csharp
public async Task<bool> ConnectAsync()
```

**Returns**: `true` if connection successful, `false` otherwise

**Example**:
```csharp
var api = new BinollaApi(sessionToken);
bool connected = await api.ConnectAsync();

if (connected)
{
    Console.WriteLine("🎉 Connected successfully!");
}
```

#### `DisconnectAsync()`
Gracefully closes the WebSocket connection.

```csharp
public async Task DisconnectAsync()
```

### 💰 Account Operations

#### `GetBalanceAsync()`
Retrieves current account balance.

```csharp
public async Task<decimal> GetBalanceAsync()
```

**Returns**: Current account balance as decimal

**Example**:
```csharp
decimal balance = await api.GetBalanceAsync();
Console.WriteLine($"💰 Balance: ${balance:F2}");
```

#### `ChangeBalanceTypeAsync(string accountType)`
Switches between Demo and Real account.

```csharp
public async Task<decimal> ChangeBalanceTypeAsync(string accountType)
```

**Parameters**:
- `accountType`: "DEMO" or "REAL"

**Returns**: New account balance

**Example**:
```csharp
// Switch to real account
decimal newBalance = await api.ChangeBalanceTypeAsync("REAL");
Console.WriteLine($"🔄 Switched to REAL account. New balance: ${newBalance:F2}");

// Switch to demo account
decimal demoBalance = await api.ChangeBalanceTypeAsync("DEMO");
Console.WriteLine($"🔄 Switched to DEMO account. New balance: ${demoBalance:F2}");
```

### 📊 Market Data

#### `GetPaymentAsync()`
Retrieves current asset payouts and availability.

```csharp
public async Task<Dictionary<string, double>> GetPaymentAsync()
```

**Returns**: Dictionary with asset symbols and their payout percentages

**Example**:
```csharp
var payouts = await api.GetPaymentAsync();

Console.WriteLine("📊 Current Payouts:");
foreach (var asset in payouts)
{
    Console.WriteLine($"   💎 {asset.Key}: {asset.Value:P} payout");
}
```

### 🛒 Trading Operations

#### `BuyAsync(string asset, string direction, decimal amount, int duration)`
Places a new trade order.

```csharp
public async Task<TradeResponse> BuyAsync(string asset, string direction, decimal amount, int duration)
```

**Parameters**:
- `asset`: Asset symbol (e.g., "EURUSD", "BTCUSD")
- `direction`: "call" for up, "put" for down
- `amount`: Investment amount
- `duration`: Expiration time in seconds

**Returns**: `TradeResponse` object with trade details

**Example**:
```csharp
// Place a CALL order on EUR/USD
var response = await api.BuyAsync("EURUSD", "call", 100, 60);

if (response.IsSuccessful)
{
    Console.WriteLine($"🎯 Trade placed successfully! ID: {response.TradeId}");
    Console.WriteLine($"💰 Amount: ${response.Amount}");
    Console.WriteLine($"⏱️ Duration: {response.Duration}s");
}
else
{
    Console.WriteLine($"❌ Trade failed: {response.ErrorMessage}");
}
```

#### `CheckWinAsync(string tradeId)`
Monitors trade result until completion.

```csharp
public async Task<WinResult> CheckWinAsync(string tradeId)
```

**Parameters**:
- `tradeId`: Trade identifier from buy response

**Returns**: `WinResult` object with trade outcome

**Example**:
```csharp
var winResult = await api.CheckWinAsync(tradeId);

if (winResult.IsSuccess)
{
    if (winResult.IsWin)
    {
        Console.WriteLine($"🏆 Trade WON! Profit: ${winResult.WinAmount:F2}");
    }
    else
    {
        Console.WriteLine($"💸 Trade LOST. Loss: ${winResult.LossAmount:F2}");
    }
}
else
{
    Console.WriteLine($"🚨 Error checking trade: {winResult.Message}");
}
```

## 💡 Advanced Usage Examples

### 🤖 Simple Trading Bot

```csharp
public class SimpleTradingBot
{
    private readonly BinollaApi _api;
    private readonly decimal _investmentAmount = 10m;
    
    public SimpleTradingBot(string sessionToken)
    {
        _api = new BinollaApi(sessionToken);
    }
    
    public async Task StartTradingAsync()
    {
        await _api.ConnectAsync();
        
        // Switch to demo for testing
        await _api.ChangeBalanceTypeAsync("DEMO");
        
        while (true)
        {
            try
            {
                // Get available assets
                var assets = await _api.GetPaymentAsync();
                
                // Find best payout
                var bestAsset = assets.OrderByDescending(x => x.Value).First();
                
                if (bestAsset.Value > 0.80) // 80% payout threshold
                {
                    // Simple strategy: trade based on market momentum
                    string direction = await DetermineDirection(bestAsset.Key);
                    
                    var trade = await _api.BuyAsync(bestAsset.Key, direction, _investmentAmount, 60);
                    
                    if (trade.IsSuccessful)
                    {
                        Console.WriteLine($"🎯 Placed {direction.ToUpper()} trade on {bestAsset.Key}");
                        
                        // Monitor trade result
                        var result = await _api.CheckWinAsync(trade.TradeId);
                        LogTradeResult(result);
                    }
                }
                
                // Wait before next trade
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 Bot error: {ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }
    }
    
    private async Task<string> DetermineDirection(string asset)
    {
        // Implement your trading strategy here
        // This is a simple random example
        return Random.Shared.Next(2) == 0 ? "call" : "put";
    }
    
    private void LogTradeResult(WinResult result)
    {
        if (result.IsWin)
        {
            Console.WriteLine($"🏆 WIN! Profit: +${result.WinAmount:F2}");
        }
        else
        {
            Console.WriteLine($"💸 LOSS: -${result.LossAmount:F2}");
        }
    }
}
```

### 📊 Portfolio Manager

```csharp
public class PortfolioManager
{
    private readonly BinollaApi _api;
    private readonly List<TradeRecord> _trades = new();
    
    public async Task<PortfolioStats> GetStatsAsync()
    {
        var balance = await _api.GetBalanceAsync();
        
        return new PortfolioStats
        {
            CurrentBalance = balance,
            TotalTrades = _trades.Count,
            WinRate = CalculateWinRate(),
            TotalProfit = CalculateTotalProfit(),
            BestTrade = _trades.OrderByDescending(t => t.Profit).FirstOrDefault(),
            WorstTrade = _trades.OrderBy(t => t.Profit).FirstOrDefault()
        };
    }
    
    private double CalculateWinRate()
    {
        if (_trades.Count == 0) return 0;
        return (double)_trades.Count(t => t.IsWin) / _trades.Count * 100;
    }
    
    private decimal CalculateTotalProfit()
    {
        return _trades.Sum(t => t.Profit);
    }
}
```

### 🔄 Risk Management System

```csharp
public class RiskManager
{
    private decimal _dailyLossLimit;
    private decimal _maxTradeAmount;
    private decimal _currentDailyLoss = 0;
    
    public bool CanTrade(decimal amount)
    {
        // Check daily loss limit
        if (_currentDailyLoss >= _dailyLossLimit)
        {
            Console.WriteLine("🛑 Daily loss limit reached!");
            return false;
        }
        
        // Check maximum trade amount
        if (amount > _maxTradeAmount)
        {
            Console.WriteLine("🛑 Trade amount exceeds maximum!");
            return false;
        }
        
        return true;
    }
    
    public void RecordLoss(decimal amount)
    {
        _currentDailyLoss += amount;
        Console.WriteLine($"⚠️ Daily loss: ${_currentDailyLoss:F2} / ${_dailyLossLimit:F2}");
    }
}
```

## 🛡️ Security & Best Practices

### 🔐 Authentication Security

1. **Never hardcode session tokens** in your source code
2. **Store tokens securely** using environment variables or secure storage
3. **Rotate tokens regularly** by re-authenticating in browser
4. **Monitor for unauthorized access** by checking account activity

```csharp
// ✅ Good: Use environment variables
string token = Environment.GetEnvironmentVariable("BINOLLA_SESSION_TOKEN");

// ❌ Bad: Hardcoded tokens
string token = "42[\"authorization\",{\"token\":\"..."}]";
```

### 💰 Trading Safety

1. **Always start with demo account** for testing
2. **Implement proper risk management** with stop-loss mechanisms
3. **Monitor API rate limits** to avoid account restrictions
4. **Log all trading activities** for audit purposes

```csharp
public class SafeTradingExample
{
    private readonly decimal MAX_DAILY_LOSS = 100m;
    private decimal _dailyLoss = 0;
    
    public async Task<bool> SafeTrade(string asset, string direction, decimal amount)
    {
        // Risk check
        if (_dailyLoss + amount > MAX_DAILY_LOSS)
        {
            Console.WriteLine("🛑 Risk limit exceeded!");
            return false;
        }
        
        var result = await _api.BuyAsync(asset, direction, amount, 60);
        
        // Update risk tracking
        if (!result.IsSuccessful)
        {
            _dailyLoss += amount;
        }
        
        return result.IsSuccessful;
    }
}
```

### 🚨 Error Handling

```csharp
public class RobustTradingBot
{
    private readonly BinollaApi _api;
    private int _reconnectAttempts = 0;
    private const int MAX_RECONNECT_ATTEMPTS = 5;
    
    public async Task HandleConnectionLoss()
    {
        while (_reconnectAttempts < MAX_RECONNECT_ATTEMPTS)
        {
            try
            {
                Console.WriteLine($"🔄 Reconnection attempt {_reconnectAttempts + 1}...");
                
                bool connected = await _api.ConnectAsync();
                if (connected)
                {
                    Console.WriteLine("✅ Reconnected successfully!");
                    _reconnectAttempts = 0;
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Reconnection failed: {ex.Message}");
            }
            
            _reconnectAttempts++;
            
            // Exponential backoff
            int delay = (int)Math.Pow(2, _reconnectAttempts) * 1000;
            await Task.Delay(delay);
        }
        
        Console.WriteLine("🚨 Max reconnection attempts reached. Manual intervention required.");
    }
}
```

## 🔧 Troubleshooting

### Common Issues & Solutions

#### 🔍 Connection Problems

**Issue**: `Unable to connect to Binolla`
```
❌ Failed to connect to Binolla
```

**Solutions**:
1. Verify your session token is current and valid
2. Check your internet connection
3. Ensure Binolla website is accessible
4. Try refreshing your browser session

#### 🔑 Authentication Errors

**Issue**: `Invalid session token`
```
🚨 Authentication failed: Invalid token
```

**Solutions**:
1. Get a fresh session token from your browser
2. Ensure the token format is correct
3. Check if you're logged into the correct account type (demo/real)

#### 💰 Trading Errors

**Issue**: `Insufficient balance`
```
❌ Trade failed: Insufficient balance
```

**Solutions**:
1. Check your account balance: `await api.GetBalanceAsync()`
2. Reduce trade amount
3. Switch to demo account for testing

#### 📊 Market Data Issues

**Issue**: `No assets available`
```
📊 Available Assets: (empty)
```

**Solutions**:
1. Check if markets are open
2. Verify your account status
3. Try reconnecting to the API

### 🐛 Debug Mode

Enable detailed logging for troubleshooting:

```csharp
var api = new BinollaApi(sessionToken, debugMode: true);
```

This will output detailed WebSocket messages and API responses to help diagnose issues.

## 📈 Performance Optimization

### 🚀 Best Practices for High-Performance Trading

1. **Use async/await properly** for non-blocking operations
2. **Implement connection pooling** for multiple accounts
3. **Cache market data** to reduce API calls
4. **Batch operations** when possible

```csharp
public class HighPerformanceTrader
{
    private readonly SemaphoreSlim _tradingSemaphore = new(5); // Max 5 concurrent trades
    private readonly Dictionary<string, DateTime> _priceCache = new();
    
    public async Task<List<TradeResponse>> ExecuteMultipleTradesAsync(List<TradeRequest> requests)
    {
        var tasks = requests.Select(async request =>
        {
            await _tradingSemaphore.WaitAsync();
            try
            {
                return await _api.BuyAsync(request.Asset, request.Direction, request.Amount, request.Duration);
            }
            finally
            {
                _tradingSemaphore.Release();
            }
        });
        
        return (await Task.WhenAll(tasks)).ToList();
    }
}
```

## 🤝 Contributing

We welcome contributions from the community! Here's how you can help:

### 🌟 How to Contribute

1. **🍴 Fork the repository**
2. **🌿 Create a feature branch**: `git checkout -b feature/amazing-feature`
3. **💻 Make your changes** with proper testing
4. **📝 Commit your changes**: `git commit -m 'Add amazing feature'`
5. **🚀 Push to the branch**: `git push origin feature/amazing-feature`
6. **🔀 Open a Pull Request**

### 🐛 Reporting Issues

Please use our issue tracker to report bugs or request features:

1. **Search existing issues** first
2. **Use issue templates** when available
3. **Provide detailed reproduction steps**
4. **Include system information** and error logs

### 💡 Feature Requests

We're always looking for ways to improve! Submit feature requests with:

- **Clear description** of the proposed feature
- **Use cases** and benefits
- **Implementation ideas** (if you have any)

## 📞 Support & Contact

### 🆘 Getting Help

- **📚 Documentation**: Check this README and code comments
- **💬 Discord Community**: [Join our Discord](https://discord.gg/UW4C5VhWCH)
- **🐛 GitHub Issues**: [Report bugs or request features](https://github.com/muneebanwar33/BinollaApiDotNet/issues)

### 📱 Direct Contact

**👨‍💻 Developer: MUNEEB ANWAR**

- **📧 Email**: [email@muneebanwar.com](mailto:email@muneebanwar.com)
- **💬 Discord**: `muneebanwar`
- **📱 Telegram**: [t.me/muneebanwar](https://t.me/muneebanwar)
- **🐙 GitHub**: [@muneebanwar33](https://github.com/muneebanwar33)

### ⏰ Response Times

- **🚨 Critical bugs**: Within 24 hours
- **💡 Feature requests**: Within 1 week
- **❓ General questions**: Within 48 hours

## 🎯 Roadmap

### 🔮 Upcoming Features

- **🤖 AI Trading**: Machine learning integration for smart trading
- **📊 Advanced Analytics**: Comprehensive trading statistics
- **📈 Strategy Backtesting**: Historical data testing capabilities

### 🏆 Version History

- **v1.0.0**: Initial release with core trading features
- **v1.1.0**: Added advanced error handling and reconnection
- **v1.2.0**: Performance optimizations and caching
- **v2.0.0** (Coming Soon): Multi-account support and AI features

## ⚖️ Legal Disclaimer

This is an **unofficial API** for educational and research purposes. Users are responsible for:

- **📋 Compliance** with Binolla's Terms of Service
- **💰 Risk management** and trading decisions
- **🔒 Account security** and token protection
- **📊 Trading losses** and profit taxation

**⚠️ Important**: Trading involves significant risk. Never trade with money you cannot afford to lose.

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

```
MIT License

Copyright (c) 2024 Muneeb Anwar

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## 🙏 Acknowledgments

- **🌐 Binolla.com** for providing the trading platform
- **👥 Community contributors** for bug reports and feature suggestions
- **🔧 .NET Team** for the excellent framework and tools
- **📚 Open source libraries** that make this project possible

---

<div align="center">

### 🌟 Star this repository if you find it useful!

**Made with ❤️ by [Muneeb Anwar](https://github.com/muneebanwar33)**

[🏠 Back to Top](#-join-rising-developers-community)

</div>
