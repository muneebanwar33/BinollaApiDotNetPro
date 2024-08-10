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

public class WebSocketClientBinolla
{
    private bool _canSendWss = true;
    private bool _isAuthorized = false;
    private string _uri;
    private ClientWebSocket _webSocket;
    private string upcommingMessageType = "";
    public event WssCallBack OnWssMessage;
    

    public WebSocketClientBinolla(string uri = "wss://ws3.binolla.com/socket.io/?EIO=4&transport=websocket")
    {
        _uri = uri;
        _webSocket = new ClientWebSocket();
        _webSocket.Options.SetRequestHeader("Host", "ws3.binolla.com");
        _webSocket.Options.SetRequestHeader("Origin", "https://binolla.com");
        _webSocket.Options.SetRequestHeader("Cache-Control", "no-cache");
        _webSocket.Options.SetRequestHeader("User-Agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36");
        
        
    }
    
    public delegate void WssCallBack(string eventType, string message);
    
    private void Log(string msg, bool inboud = false)
    {   
        return;
        if (inboud)
        {
            Console.ForegroundColor = ConsoleColor.Red;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
        }
        
        //show daate time with millisecond
        Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg);
        Console.ResetColor();
    }

    public async Task ConnectAsync(Dictionary<string,string>? customCockies=null)
    {
        if (customCockies!= null)
        {
            foreach (var customCocky in customCockies)
            {
                _webSocket.Options.SetRequestHeader(customCocky.Key, customCocky.Value);
            }
            
        }
        
        await _webSocket.ConnectAsync(new Uri(_uri), CancellationToken.None);
        Log("Connected to the WebSocket server successfully.");
        //now we will run RecvMessageAsync in a new thread
        Task.Factory.StartNew(async () =>
        {
            while (true)
            {
                await ReceiveMessageAsync();
            }
        });
    }

    public async Task SendMessageAsync(string message)
    {
        while(!_canSendWss)//this ensures that the message is sent only after the previous message is sent
        {
            await Task.Delay(1);
        }
        _canSendWss = false;
        await _webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, CancellationToken.None);
        _canSendWss = true;
        // Console.WriteLine("Sent : " + message);
        Log($"Sent : {message}");
    }

    public async Task<string> ReceiveMessageAsync()
    {
        var buffer = new byte[1024 * 16];
        var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        //check if the recved message type is binary
        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
        
        try
        {
        if (result.MessageType == WebSocketMessageType.Binary)
        {   
            //recive data with high buffer size
            // buffer = new byte[1024 * 2];
            // result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            //check if the recved message type is binary
            // message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            
            if (message.Length < Int32.MaxValue  )
            {
                Log($"{upcommingMessageType} : {message}", true);
            }
            else
            {
                Log($"{upcommingMessageType} : Too Long", true);

            }
            if (upcommingMessageType == "s_balance/update")
            {
                var messageObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);

                //{"balance":9605.5,"isDemo":1}
                var balance = Convert.ToDouble(messageObject["balance"]);

                if (Convert.ToInt32(messageObject["isDemo"]) == 1)
                {
                    Values.BalanceDemo = balance;
                }
                else
                {
                    Values.BalanceReal = balance;
                }
                Values.BalanceUpdated = DateTime.Now;
                // Console.WriteLine($"Balance Updated : {Values.BalanceDemo} : {Values.BalanceReal}");
            }
            if (upcommingMessageType == "s_balances/list")
            {
                //we will recve this {"liveBalance":0,"demoBalance":9608.5}
                var messageObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);
                Values.BalanceDemo = Convert.ToDouble(messageObject["demoBalance"]);
                Values.BalanceReal = Convert.ToDouble(messageObject["liveBalance"]);
                Values.BalanceUpdated = DateTime.Now;
                // Console.WriteLine($"Balance Updated : {Values.BalanceDemo} : {Values.BalanceReal}");

            }
            else if (upcommingMessageType == "s_orders/open")
            {
                //{"deal":{"uuid":"283a8997-ad36-499b-b0e6-b73a4b59ea74","uid":227177,"openTime":"2024-08-03 11:40:15","closeTime":"2024-08-03 11:41:15","openPrice":0.56563,"command":0,"amount":1,"amountUsd":1,"rate":1,"currency":"USD","source":0,"percentProfit":88,"percentLoss":100,"asset":"AUDCHF_otc","isDemo":1,"requestId":0,"optionType":0,"openMs":576,"balance":9607.38,"digit":2,"profit":0.88,"openTimestamp":1722685215,"closeTimestamp":1722685275,"closePrice":0}}
                OpenedOrder order = JsonConvert.DeserializeObject<OpenedOrder>(message);
                
                Values.OrderData = order;
                Values.NewOpenOrder = order;
                Values.OrderOpenUuid = order.Deal.Uuid;                
            }
            else if (upcommingMessageType == "f_orders/open")
            {
                // {"error":"expiration","isDemo":1,"requestId":0,"amount":10,"asset":"AUDJPY_otc","time":1722539249}

                FailedOrderOpen failedOrder = JsonConvert.DeserializeObject<FailedOrderOpen>(message);
                Values.FailedOrderOpen = failedOrder;
            }
            else if (upcommingMessageType == "s_orders/close")
            {
                //{"profit":5.64,"deals":[{"id":26594560,"uuid":"3b85e7f6-5bab-49b3-863d-0c6177d5a6af","openTime":"2024-08-03 11:54:09","closeTime":"2024-08-03 11:55:09","openTimestamp":1722686049,"openMs":558,"closeMs":136,"closeTimestamp":1722686109,"uid":227177,"rate":1,"amount":1,"profit":0.88,"percentProfit":88,"refundTimestamp":null,"refundTime":null,"percentLoss":100,"openPrice":0.5649,"closePrice":0.56514,"command":0,"asset":"AUDCHF_otc","isDemo":1,"requestId":0,"currency":"USD","amountUsd":1,"processUpdate":true},{"id":26594559,"uuid":"949a314e-2d42-4be5-aeb2-e81e8731a00f","openTime":"2024-08-03 11:54:09","closeTime":"2024-08-03 11:55:09","openTimestamp":1722686049,"openMs":558,"closeMs":136,"closeTimestamp":1722686109,"uid":227177,"rate":1,"amount":1,"profit":0.88,"percentProfit":88,"refundTimestamp":null,"refundTime":null,"percentLoss":100,"openPrice":0.5649,"closePrice":0.56514,"command":0,"asset":"AUDCHF_otc","isDemo":1,"requestId":0,"currency":"USD","amountUsd":1,"processUpdate":true},{"id":26594557,"uuid":"08282365-d416-497f-819c-33893ce4ba06","openTime":"2024-08-03 11:54:09","closeTime":"2024-08-03 11:55:09","openTimestamp":1722686049,"openMs":558,"closeMs":136,"closeTimestamp":1722686109,"uid":227177,"rate":1,"amount":1,"profit":0.88,"percentProfit":88,"refundTimestamp":null,"refundTime":null,"percentLoss":100,"openPrice":0.5649,"closePrice":0.56514,"command":0,"asset":"AUDCHF_otc","isDemo":1,"requestId":0,"currency":"USD","amountUsd":1,"processUpdate":true}]}
                ClosedOrder closedOrder = JsonConvert.DeserializeObject<ClosedOrder>(message);
    

                Values.ClosedOrderData = closedOrder;
                
                foreach (var deal in closedOrder.Deals)
                {
                    Values.CloseOrderPl[deal.Uuid] = deal.Profit;
                }
                
            }
            else if (upcommingMessageType == "s_orders/closed/list")
            {
                
                //{"profit":5.64,"deals":[{"id":26594560,"uuid":"3b85e7f6-5bab-49b3-863d-0c6177d5a6af","openTime":"2024-08-03 11:54:09","closeTime":"2024-08-03 11:55:09","openTimestamp":1722686049,"openMs":558,"closeMs":136,"closeTimestamp":1722686109,"uid":227177,"rate":1,"amount":1,"profit":0.88,"percentProfit":88,"refundTimestamp":null,"refundTime":null,"percentLoss":100,"openPrice":0.5649,"closePrice":0.56514,"command":0,"asset":"AUDCHF_otc","isDemo":1,"requestId":0,"currency":"USD","amountUsd":1,"processUpdate":true},{"id":26594559,"uuid":"949a314e-2d42-4be5-aeb2-e81e8731a00f","openTime":"2024-08-03 11:54:09","closeTime":"2024-08-03 11:55:09","openTimestamp":1722686049,"openMs":558,"closeMs":136,"closeTimestamp":1722686109,"uid":227177,"rate":1,"amount":1,"profit":0.88,"percentProfit":88,"refundTimestamp":null,"refundTime":null,"percentLoss":100,"openPrice":0.5649,"closePrice":0.56514,"command":0,"asset":"AUDCHF_otc","isDemo":1,"requestId":0,"currency":"USD","amountUsd":1,"processUpdate":true},{"id":26594557,"uuid":"08282365-d416-497f-819c-33893ce4ba06","openTime":"2024-08-03 11:54:09","closeTime":"2024-08-03 11:55:09","openTimestamp":1722686049,"openMs":558,"closeMs":136,"closeTimestamp":1722686109,"uid":227177,"rate":1,"amount":1,"profit":0.88,"percentProfit":88,"refundTimestamp":null,"refundTime":null,"percentLoss":100,"openPrice":0.5649,"closePrice":0.56514,"command":0,"asset":"AUDCHF_otc","isDemo":1,"requestId":0,"currency":"USD","amountUsd":1,"processUpdate":true}]}
                ClosedOrder closedOrder = JsonConvert.DeserializeObject<ClosedOrder>(message);
                // Values.ClosedOrders.AddRange(closedOrder);
                foreach (var deal in closedOrder.Deals)
                {
                    Values.CloseOrderPl[deal.Uuid] = deal.Profit;
                }
                Values.ClosedOrders.Add(closedOrder);
            }
            else if (upcommingMessageType == "s_assets/list")
            {
                //[2, 'AUDCAD', 'AUD/CAD', 'currency', 5, 70, None, None, None, 0, None, None, None, 1722535200, True, None, 0, 0.13, 70, None, 0, -0.01, 0.03, 1.57, 0.42, None, None, 0, 'fixed_time']
                var assets = JsonConvert.DeserializeObject<List<object>>(JsonConvert.DeserializeObject<object>(message).ToString());
                List<AssetData> assetList = new List<AssetData>();
                
                foreach (var VARIABLE in assets)
                {   
                    //pRING THE VARIABLE
                    // Console.WriteLine(VARIABLE);
                    // continue;
                    var assetData = JsonConvert.DeserializeObject<List<object>>(VARIABLE.ToString());
                    
                    try
                    {
                        assetList.Add(
                            new AssetData()
                            {
                                ActiveId = Convert.ToInt32(assetData[0]),
                                Name = assetData[1].ToString(),
                                Description = assetData[2].ToString(),
                                Type = assetData[3].ToString(),
                                Precision = Convert.ToInt32(assetData[4]),
                                Payout = Convert.ToInt32(assetData[5]),
                                IsOpen = Convert.ToBoolean(assetData[14]),
                                TradeType = assetData[27].ToString() == "fixed_time" ? TradeType.fixed_time : TradeType.blitz
                        
                            });

                    }
                    catch (Exception e)
                    {
                        // Console.WriteLine(e);
                        // throw;
                    }
                    

                    
                }
                

                // Console.WriteLine("Total Assets : " + assetList.Count  + " Recved Assets: " + assets.Count);
                Values.PaymentAssets = assetList;
            }
            upcommingMessageType = "";
            return "";
            
        }

        }
        catch (Exception e)
        {
            // Console.WriteLine(e);
            // throw;
        }
        Log($"Recved : {message}");

        
            
        if (message.StartsWith("0") && message.Contains("sid"))
        {
            await SendMessageAsync("40");

        }
        else if (message.StartsWith("40") && message.Contains("sid"))
        {
            //send ssid
            await SendMessageAsync(Values.SSID);
        }
        //this means authrization is accepted
        else if (message.StartsWith("42") && message.Contains("s_authorization"))
        {
            Values.WebsocketIsConnected = true;
            Values.SSIDAccepted = true;
            
            
            await SendMessageAsync("42[\"account/change\",{\"demo\":1}]");
            await Task.Delay(10);

            await SendMessageAsync("42[\"orders/opened/list\"]");
            await Task.Delay(10);

            await SendMessageAsync("42[\"orders/closed/list\"]");
            await Task.Delay(10);

            await SendMessageAsync("42[\"assets/list\"]");
            await Task.Delay(10);

            await SendMessageAsync("42[\"alert/list\"]");
            await Task.Delay(10);

            await SendMessageAsync("42[\"alert/closed/list\"]");
            await Task.Delay(10);

            await SendMessageAsync("42[\"indicator/list\"]");
            await Task.Delay(10);

            await SendMessageAsync("42[\"drawing/load\"]");
            await Task.Delay(10);
            
            // await SendMessageAsync("42[\"asset/change\",{\"asset\":\"AUDJPY_otc\",\"period\":1}]");
            await Task.Delay(10);
            // await SendMessageAsync("42[\"asset/subscribe\",\"AUDJPY_otc\"]");
        }
        else if (message == "2")
        {
            await SendMessageAsync("3");
        }
        else if (message.StartsWith("451-["))
        {
            //451-["s_orders/opened/list",{"_placeholder":true,"num":0}]
            var jsonPart = message.Split('-')[1];
            var jsonMessage = JsonConvert.DeserializeObject<List<object>>(jsonPart);
            upcommingMessageType = jsonMessage[0].ToString();

        }
        else if (message.StartsWith("42") && message.Contains("NotAuthorized"))
        {
            // Handle authorization error
            Console.Error.WriteLine("User not Authorized: Please Change SSID for one valid");

        }
        
        
        
        return message;
    }


    public async Task DisconnectAsync()
    {
        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
        Console.WriteLine("Disconnected from the WebSocket server.");
    }
}
