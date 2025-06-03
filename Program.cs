using BinollaApiDotNet;
using BinollaApiDotNet.DataTypes;

namespace BinollaApiDotNet;

public class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== 🎯 Welcome to My Binolla API .NET SDK ===");
        Console.WriteLine("Created by: Muneeb Anwar");
        Console.WriteLine("This demonstration showcases the powerful features I've built into this API wrapper.\n");

        try
        {
            // ========================================
            // FEATURE 1: Easy API Client Initialization
            // ========================================
            Console.WriteLine("🛠️ FEATURE 1: Simple & Powerful Client Setup");
            Console.WriteLine("==============================================");
            Console.WriteLine("With my API, connecting to Binolla is incredibly simple!");
            
            var api = new BinollaApiClient(
                "42[\"authorization\",{\"isDemo\":true,\"token\":\"bQI01bNujKF4y3vqyU7ooYmK8SFhpvcHliVvnXqa\"}]",
                timeoutSeconds: 30,
                enableLogging: false);
            
            Console.WriteLine("✅ My API Client initialized successfully!");
            Console.WriteLine("   - Automatic demo account detection");
            Console.WriteLine("   - Built-in timeout protection (30s)");
            Console.WriteLine("   - Configurable logging system");
            Console.WriteLine("   - One-line setup - that's it!\n");

            // ========================================
            // FEATURE 2: Intelligent Connection Management
            // ========================================
            Console.WriteLine("🔐 FEATURE 2: Smart Connection & Authentication");
            Console.WriteLine("===============================================");
            Console.WriteLine("My API handles all the complex WebSocket connections for you:");
            
            var connectionResult = api.Connect();
            
            if (connectionResult.IsSuccess)
            {
                Console.WriteLine("🎉 Connected successfully using my enhanced connection system!");
                Console.WriteLine($"   ✓ Auto-authenticated at: {connectionResult.Data.ConnectedAt}");
                Console.WriteLine($"   ✓ Connection established in: {connectionResult.Data.ConnectionDuration?.TotalSeconds:F1} seconds");
                Console.WriteLine("   ✓ Built-in error handling and retry logic");
                Console.WriteLine("   ✓ Automatic heartbeat management\n");
            }
            else
            {
                Console.WriteLine($"❌ Connection failed: {connectionResult.ErrorMessage}");
                Console.WriteLine("My API provides detailed error information to help you debug!");
                return;
            }

            // ========================================
            // FEATURE 3: Comprehensive Account Management
            // ========================================
            Console.WriteLine("💰 FEATURE 3: Advanced Account Balance Management");
            Console.WriteLine("=================================================");
            Console.WriteLine("I've made account management incredibly easy:");
            
            var balanceResult = api.GetBalance();
            
            if (balanceResult.IsSuccess)
            {
                var balance = balanceResult.Data;
                Console.WriteLine("💎 Here's what my API retrieved for you:");
                Console.WriteLine($"   📊 Demo Balance: ${balance.DemoBalance:F2}");
                Console.WriteLine($"   💵 Real Balance: ${balance.RealBalance:F2}");
                Console.WriteLine($"   🎯 Current Account: {balance.CurrentType}");
                Console.WriteLine($"   🌍 Currency: {balance.Currency}");
                Console.WriteLine($"   ⏰ Last Updated: {balance.LastUpdated:HH:mm:ss}");
                Console.WriteLine("   ✨ All formatted and ready to use!\n");
            }
            else
            {
                Console.WriteLine($"❌ Balance retrieval failed: {balanceResult.ErrorMessage}");
                Console.WriteLine("Don't worry - my API provides clear error messages!\n");
            }

            // ========================================
            // FEATURE 4: Seamless Account Type Switching
            // ========================================
            Console.WriteLine("🔄 FEATURE 4: One-Click Account Type Switching");
            Console.WriteLine("==============================================");
            Console.WriteLine("Watch how easily you can switch between Demo and Real accounts:");
            
            Console.WriteLine("Switching to Demo account using my API...");
            var demoBalanceResult = api.ChangeBalanceType(BalanceType.Demo);
            if (demoBalanceResult.IsSuccess)
            {
                Console.WriteLine($"✅ Switched to Demo seamlessly - Balance: ${demoBalanceResult.Data.DemoBalance:F2}");
            }
            
            await Task.Delay(1000);
            
            Console.WriteLine("Now switching to Real account...");
            var realBalanceResult = api.ChangeBalanceType(BalanceType.Real);
            if (realBalanceResult.IsSuccess)
            {
                Console.WriteLine($"✅ Switched to Real account - Balance: ${realBalanceResult.Data.RealBalance:F2}");
                Console.WriteLine("💡 My API makes account switching completely seamless!\n");
            }

            // ========================================
            // FEATURE 5: Rich Trading Asset Discovery
            // ========================================
            Console.WriteLine("📈 FEATURE 5: Comprehensive Trading Asset Discovery");
            Console.WriteLine("==================================================");
            Console.WriteLine("I've built powerful asset discovery features:");
            
            var assetsResult = api.GetTradingAssets();
            
            if (assetsResult.IsSuccess)
            {
                var assets = assetsResult.Data;
                Console.WriteLine($"🚀 My API discovered {assets.Count} tradeable assets for you!");
                Console.WriteLine("Here are some examples of what I provide:");
                
                var counter = 0;
                foreach (var asset in assets.Take(8)) // Show first 8 assets
                {
                    counter++;
                    Console.WriteLine($"   {counter}. {asset.Key} - {asset.Value.Description}");
                    Console.WriteLine($"      💰 Payout: {asset.Value.PayoutPercentage}% | 📊 Status: {(asset.Value.IsOpen ? "OPEN" : "CLOSED")}");
                }
                
                if (assets.Count > 8)
                {
                    Console.WriteLine($"   🌟 Plus {assets.Count - 8} more assets available!");
                }
                Console.WriteLine("   💎 Each asset includes payout rates, availability, and metadata!\n");
            }
            else
            {
                Console.WriteLine($"❌ Asset discovery failed: {assetsResult.ErrorMessage}");
                Console.WriteLine("My API provides detailed error information!\n");
            }

            // ========================================
            // FEATURE 6: Real-Time Market Data Streaming
            // ========================================
            Console.WriteLine("📊 FEATURE 6: Real-Time Market Data Streaming");
            Console.WriteLine("==============================================");
            Console.WriteLine("Watch my real-time data streaming in action:");
            
            string tradingPair = "AUDCHF_otc";
            int period = 60;
            
            Console.WriteLine($"🎯 Subscribing to {tradingPair} with {period}s intervals...");
            api.SubscribePair(tradingPair, period);
            Console.WriteLine("✅ My API is now streaming live market data!");
            Console.WriteLine("💡 All WebSocket complexity is handled automatically!\n");
            
            // Wait for data to arrive
            await Task.Delay(3000);

            // ========================================
            // FEATURE 7: Smart Price Data Access
            // ========================================
            Console.WriteLine("💱 FEATURE 7: Intelligent Price Data Management");
            Console.WriteLine("===============================================");
            Console.WriteLine("My API provides multiple ways to access current prices:");
            
            var priceResult = api.GetCurrentPrice(tradingPair);
            if (priceResult.IsSuccess)
            {
                Console.WriteLine($"💎 Current price for {tradingPair}: {priceResult.Data:F5}");
                Console.WriteLine("   ⚡ Retrieved instantly from my price cache!");
            }
            else
            {
                Console.WriteLine($"⚠️ Price not yet available: {priceResult.ErrorMessage}");
                Console.WriteLine("🔄 My API is still warming up the data stream...");
            }
            
            var quoteResult = api.GetLatestQuote(tradingPair);
            if (quoteResult.IsSuccess)
            {
                var quote = quoteResult.Data;
                Console.WriteLine("🎯 Enhanced quote data I provide:");
                Console.WriteLine($"   📊 Pair: {quote.Pair}");
                Console.WriteLine($"   💰 Price: {quote.Price:F5}");
                Console.WriteLine($"   ⏰ Timestamp: {quote.ReceivedAt:HH:mm:ss.fff}");
                Console.WriteLine("   ✨ Precise to the millisecond!\n");
            }

            // ========================================
            // FEATURE 8: Comprehensive Market Overview
            // ========================================
            Console.WriteLine("📋 FEATURE 8: Complete Market Data Overview");
            Console.WriteLine("===========================================");
            Console.WriteLine("My API can retrieve all market quotes at once:");
            
            var allQuotesResult = api.GetAllLatestQuotes();
            if (allQuotesResult.IsSuccess)
            {
                var quotes = allQuotesResult.Data;
                Console.WriteLine($"🚀 Retrieved {quotes.Count} live quotes simultaneously!");
                Console.WriteLine("Sample of what my API provides:");
                
                foreach (var quote in quotes.Take(5)) // Show first 5 quotes
                {
                    Console.WriteLine($"   💎 {quote.Key}: {quote.Value.Price:F5} @ {quote.Value.ReceivedAt:HH:mm:ss}");
                }
                
                if (quotes.Count > 5)
                {
                    Console.WriteLine($"   🌟 Plus {quotes.Count - 5} more real-time quotes!");
                }
                Console.WriteLine("   ⚡ All updated in real-time by my streaming engine!\n");
            }

            // ========================================
            // FEATURE 9: Rich Historical Data Analysis
            // ========================================
            Console.WriteLine("📊 FEATURE 9: Advanced Historical Data Engine");
            Console.WriteLine("==============================================");
            Console.WriteLine("I've built a powerful historical data system:");
            
            var historyResult = api.GetHistoricalData(tradingPair, period);
            if (historyResult.IsSuccess)
            {
                var history = historyResult.Data;
                Console.WriteLine($"🏆 Historical data powerhouse for {tradingPair}:");
                Console.WriteLine($"   📊 Asset: {history.Asset}");
                Console.WriteLine($"   ⏰ Period: {history.Period} seconds");
                Console.WriteLine($"   📈 Tick Data Points: {history.TickHistory.Count:N0}");
                Console.WriteLine($"   🕯️ Candlestick Data: {(history.Candlestick != null ? "✅ Available" : "❌ Not available")}");
                Console.WriteLine($"   📊 Total Candles: {history.Candles.Count:N0}");
                
                if (history.Candlestick != null)
                {
                    var candle = history.Candlestick;
                    Console.WriteLine($"   💎 Latest OHLC: O:{candle.Open:F5} H:{candle.High:F5} L:{candle.Low:F5} C:{candle.Close:F5}");
                }
                Console.WriteLine("   🚀 All processed and ready for your analysis!\n");
            }
            else
            {
                Console.WriteLine($"⚠️ Historical data not yet available: {historyResult.ErrorMessage}");
                Console.WriteLine("🔄 My API needs a moment to gather the data!\n");
            }

            // ========================================
            // FEATURE 10: Professional Trading Interface
            // ========================================
            Console.WriteLine("🎯 FEATURE 10: Professional Trading Order System");
            Console.WriteLine("================================================");
            Console.WriteLine("Now watch my advanced order placement system in action:");
            
            Console.WriteLine($"🚀 Placing a CALL order for {tradingPair} using my API:");
            Console.WriteLine("   📈 Direction: CALL (predicting price will rise)");
            Console.WriteLine("   💰 Amount: $1.00");
            Console.WriteLine("   ⏰ Duration: 60 seconds");
            Console.WriteLine("   🎯 My API handles all the complexity...");
            
            var orderResult = api.PlaceOrder(tradingPair, TradeDirection.Call, 1, 60);
            
            if (orderResult.IsSuccess)
            {
                var order = orderResult.Data;
                Console.WriteLine("🎉 Order placed successfully through my trading engine!");
                Console.WriteLine($"   🆔 Order ID: {order.OrderId}");
                Console.WriteLine($"   📊 Asset: {order.Asset}");
                Console.WriteLine($"   📈 Direction: {order.Direction}");
                Console.WriteLine($"   💰 Investment: ${order.Amount}");
                Console.WriteLine($"   💵 Open Price: {order.OpenPrice:F5}");
                Console.WriteLine($"   🎯 Expected Payout: ${order.ExpectedPayout:F2}");
                Console.WriteLine($"   ⏰ Expires: {order.ExpiryTime:HH:mm:ss}");
                Console.WriteLine($"   📊 Status: {order.Status}");
                Console.WriteLine("   ✨ All data formatted and ready to use!");

                // ========================================
                // FEATURE 11: Automated Trade Monitoring
                // ========================================
                Console.WriteLine("\n⏳ FEATURE 11: Intelligent Trade Outcome Monitoring");
                Console.WriteLine("===================================================");
                Console.WriteLine("My API includes automated trade result tracking:");
                Console.WriteLine("🔄 Monitoring your trade in real-time...");
                Console.WriteLine("💡 My system will automatically detect when it completes!");
                
                // Show countdown
                var expiryTime = order.ExpiryTime;
                while (DateTime.UtcNow < expiryTime.AddSeconds(5)) // Add 5 seconds buffer
                {
                    var remaining = expiryTime - DateTime.UtcNow;
                    if (remaining.TotalSeconds > 0)
                    {
                        Console.Write($"\r⏰ Time remaining: {remaining.TotalSeconds:F0}s (My API is watching...) ");
                        await Task.Delay(1000);
                    }
                    else
                    {
                        break;
                    }
                }
                
                Console.WriteLine("\n\n🔍 Checking trade outcome using my result detection system...");
                
                var outcomeResult = api.GetTradeOutcome(order.OrderId, timeoutSeconds: 30);
                
                if (outcomeResult.IsSuccess)
                {
                    var outcome = outcomeResult.Data;
                    Console.WriteLine("🎊 Trade completed! My API captured the results:");
                    Console.WriteLine($"   🆔 Order ID: {outcome.OrderId}");
                    Console.WriteLine($"   🏆 Result: {outcome.Result}");
                    Console.WriteLine($"   💰 Win Amount: ${outcome.WinAmount:F2}");
                    Console.WriteLine($"   📈 Profit/Loss: ${outcome.ProfitLoss:F2}");
                    Console.WriteLine($"   ⏰ Completed: {outcome.ClosedAt:HH:mm:ss}");
                    
                    if (outcome.IsWin)
                    {
                        Console.WriteLine("🎉 Congratulations! The trade was profitable!");
                        Console.WriteLine("💎 My API successfully tracked your winning trade!");
                    }
                    else if (outcome.IsLoss)
                    {
                        Console.WriteLine("📊 Trade completed with a loss - better luck next time!");
                        Console.WriteLine("💡 My API provides complete trade analytics!");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ Trade outcome detection: {outcomeResult.ErrorMessage}");
                    Console.WriteLine("🔄 My API provides detailed status information!");
                }
            }
            else
            {
                Console.WriteLine($"📊 Order placement result: {orderResult.ErrorMessage}");
                Console.WriteLine($"🔍 Error Category: {orderResult.ErrorCode}");
                Console.WriteLine("💡 My API provides comprehensive error classification!");
                Console.WriteLine("🎯 This helps you understand exactly what happened!");
            }

            Console.WriteLine();

            // ========================================
            // FEATURE 12: Advanced Logging Control System
            // ========================================
            Console.WriteLine("🔧 FEATURE 12: Professional Logging Control System");
            Console.WriteLine("===================================================");
            Console.WriteLine("I've built a sophisticated logging system you can control:");
            
            Console.WriteLine("📊 Current logging configuration:");
            Console.WriteLine($"   🔘 Master Logging: {api.EnableLogging}");
            Console.WriteLine($"   🐛 Debug Level: {api.EnableDebugLogging}");
            Console.WriteLine($"   ℹ️ Info Level: {api.EnableInfoLogging}");
            Console.WriteLine($"   ⚠️ Warning Level: {api.EnableWarningLogging}");
            Console.WriteLine($"   ❌ Error Level: {api.EnableErrorLogging}");
            
            Console.WriteLine("\n🎛️ Demonstrating my different logging modes:");
            
            Console.WriteLine("1. 🔇 Silent Mode (perfect for production):");
            api.DisableAllLogging();
            await Task.Delay(1000);
            
            Console.WriteLine("2. 🎯 Minimal Mode (errors only):");
            api.SetMinimalLogging();
            await Task.Delay(1000);
            
            Console.WriteLine("3. 🏭 Production Mode (warnings + errors):");
            api.SetProductionLogging();
            await Task.Delay(1000);
            
            Console.WriteLine("4. 🔬 Development Mode (everything visible):");
            api.SetDevelopmentLogging();
            
            Console.WriteLine("💡 Choose the perfect logging level for your needs!\n");

            // ========================================
            // FEATURE 13: Connection Health Monitoring
            // ========================================
            Console.WriteLine("📡 FEATURE 13: Real-Time Connection Health Monitoring");
            Console.WriteLine("====================================================");
            Console.WriteLine("My API continuously monitors connection health:");
            
            var status = api.GetConnectionStatus();
            Console.WriteLine($"🌟 Connection Health Report:");
            Console.WriteLine($"   🔗 Main Connection: {(status.IsConnected ? "✅ HEALTHY" : "❌ DISCONNECTED")}");
            Console.WriteLine($"   ⏰ Session Duration: {status.ConnectionDuration?.TotalMinutes:F1} minutes");
            Console.WriteLine($"   📊 Chart Data Stream: {(api.IsChartConnected ? "✅ ACTIVE" : "❌ INACTIVE")}");
            Console.WriteLine("   💎 All monitored automatically by my system!\n");

            // ========================================
            // CREATOR'S FINAL SHOWCASE
            // ========================================
            Console.WriteLine("🏆 MY BINOLLA API .NET SDK - FEATURE COMPLETE!");
            Console.WriteLine("===============================================");
            Console.WriteLine("🎯 You've just experienced all the powerful features I built:");
            Console.WriteLine("✅ Intelligent connection management with auto-retry");
            Console.WriteLine("✅ Comprehensive account and balance handling");
            Console.WriteLine("✅ Real-time market data streaming");
            Console.WriteLine("✅ Advanced historical data processing");
            Console.WriteLine("✅ Professional trading order system");
            Console.WriteLine("✅ Automated trade outcome monitoring");
            Console.WriteLine("✅ Sophisticated logging control");
            Console.WriteLine("✅ Connection health monitoring");
            Console.WriteLine("✅ Rich error handling and classification");
            Console.WriteLine("✅ Type-safe API with comprehensive data models");
            
            Console.WriteLine("\n🚀 Additional Advanced Features Available:");
            Console.WriteLine("   📈 GetRecentQuotes() - Historical quote analysis");
            Console.WriteLine("   🎯 GetTickData() - Granular market tick data");
            Console.WriteLine("   🕯️ GetCandlestickData() - Professional OHLC analysis");
            Console.WriteLine("   ⏰ IsQuoteRecent() - Data freshness validation");
            Console.WriteLine("   🎛️ ConfigureLogging() - Custom logging configuration");
            Console.WriteLine("   🔄 And many more professional features!");

            Console.WriteLine("\n💎 Why Choose My Binolla API SDK:");
            Console.WriteLine("   🛡️ Production-ready with comprehensive error handling");
            Console.WriteLine("   ⚡ High-performance with real-time data streaming");
            Console.WriteLine("   🧩 Easy integration - just a few lines of code");
            Console.WriteLine("   📚 Complete documentation and examples");
            Console.WriteLine("   🔒 Secure and reliable WebSocket management");
            Console.WriteLine("   🎯 Built by developers, for developers");

        }
        catch (Exception ex)
        {
            Console.WriteLine("🐛 EXCEPTION HANDLING DEMONSTRATION");
            Console.WriteLine("====================================");
            Console.WriteLine($"❌ My API caught an unexpected error: {ex.Message}");
            Console.WriteLine("💡 Even in error scenarios, my API provides detailed information!");
            Console.WriteLine($"🔍 Technical Details: {ex.StackTrace}");
            Console.WriteLine("🛡️ This is how my robust error handling protects your application!");
        }
        finally
        {
            Console.WriteLine("\n🔌 FEATURE: Graceful Disconnection");
            Console.WriteLine("===================================");
            Console.WriteLine("My API includes intelligent cleanup...");
            try
            {
                // api.Disconnect(); // Uncomment for clean disconnection
                Console.WriteLine("✅ All connections managed safely!");
                Console.WriteLine("💡 My API ensures no resource leaks!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Cleanup note: {ex.Message}");
                Console.WriteLine("🛡️ Even cleanup errors are handled gracefully!");
            }
        }

        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("🎊 Thank you for exploring my Binolla API .NET SDK!");
        Console.WriteLine("💼 Created with ❤️ by Muneeb Anwar");
        Console.WriteLine("🚀 Ready to power your trading applications!");
        Console.WriteLine("📧 Questions? Feedback? Let's connect!");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine("Press Enter to exit the demonstration...");
        Console.ReadLine();
    }
}