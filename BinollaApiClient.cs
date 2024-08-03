namespace BinollaApiDotNet;
using System;
using System.Threading;
using static BinollaApiDotNet.Globals;
using System.Collections.Generic;

using BinollaApiDotNet.DataTypes;

public class BinollaApiClient

{
    private WebSocketClientBinolla _wsClient;
    
    public BinollaApiClient(string ssid)
    {
        Values.SSID = ssid;
        _wsClient = new WebSocketClientBinolla();
    }
    /// <summary>
    /// Method to connect to websocket server
    /// </summary>
    /// <returns>Connection Status</returns>
    public bool Connect()
    {   
        
        _wsClient.ConnectAsync();
        DateTime start = DateTime.Now;
        while (true)
        {
            if (Values.SSIDAccepted)
            {
                Values.WebsocketErrorReason = "";
                return true;
            }
            if ((DateTime.Now - start).TotalSeconds > 60)
            {
                
                Values.WebsocketErrorReason = "Authrization Token Not Accepted By Server : Please Check SSID";
                return false;
            }
            
        }
    }

    /// <summary>
    /// Method to get current balance
    /// </summary>
    /// <returns>Current Balance</returns>
    public double GetBalance()
    {   
        DateTime start = DateTime.Now;
        while (Values.BalanceUpdated == null)
        {
            if ((DateTime.Now - start).TotalSeconds > 60)
            {
                return -1;
            }
            
        }
        //if bl type is 1 then return demo bl
        if (Values.BalanceType == 1)
        {
            return Values.BalanceDemo ?? -1;
        }
        else
        {
            return Values.BalanceReal ?? -1;
        }
        
    }
    
    /// <summary>
    /// Method to change balance type
    /// </summary>
    /// <param name="string"> REAL OR DEMO (IgnoreCase)</param>
    /// <returns>Selected Balance</returns>
    public double ChangeBalanceType(string type)
    {
        //42["account/change",{"demo":0}] for real
        //42["account/change",{"demo":1}] for demo
        string sendStr = "";
        type = type.ToUpper();
        if (type.StartsWith("R"))
        {
            if (Values.BalanceType == 0)
            {
                return GetBalance();
            }
            
            sendStr = "42[\"account/change\",{{\"demo\":0}}]";
            Values.BalanceType = 0;
            SendWss(sendStr);

        }
        else
        {
            if (Values.BalanceType == 1)
            {
                return GetBalance();
            }
            sendStr = "42[\"account/change\",{{\"demo\":1}}]";
            Values.BalanceType = 1;
            SendWss(sendStr);
        }
        return GetBalance();

    }
    
    /// <summary>
    /// Check Availbale Open Assets And thier Payout
    /// </summary>
    /// <returns>Dict of Assets And payout</returns>
    public Dictionary<string, double> GetPayment()
    {
        var result = new Dictionary<string, double>();
        DateTime start = DateTime.Now;
        while (Values.PaymentAssets.Count == 0)
        {
            if ((DateTime.Now - start).TotalSeconds > 60)
            {
                Console.WriteLine("Timeout:Payment not Updated");
                return result;
            }
        }

        foreach (var asset in Values.PaymentAssets)
        {
            if (asset.IsOpen)
            {
                result[asset.Name] = asset.Payout;
            }
            
        }

        return result;
    }
    /// <summary>
    /// Method to Open A new Binary Order
    /// </summary>
    /// <param name="active"></param>
    /// <param name="direction"></param>
    /// <param name="amount"></param>
    /// <param name="expiry"></param>
    /// <returns></returns>
    public OrderBuyResponse Buy(string active, string direction, double amount, int expiry)
    {
        var result = new OrderBuyResponse();
        //42["orders/open",{"asset":"AUDJPY_otc","time":1722693885,"amount":1,"cmd":0}]
        var currenctTimeStamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        var expiryts = (int)currenctTimeStamp + expiry+1;
        string sendStr = $"42[\"orders/open\",{{\"asset\":\"{active}\",\"time\":{expiryts},\"amount\":{amount},\"cmd\":{(direction == "call" ? 0 : 1)}}}]";
        
        Values.NewOpenOrder = null;
        Values.OrderOpenUuid = null;
        Values.OrderData = null;
        Values.FailedOrderOpen = null;
        result.IsSuccess = false;
        result.Message = "Unknown Error";
        SendWss(sendStr);
        DateTime start = DateTime.Now;
        while (Values.NewOpenOrder == null)
        {
            if ((DateTime.Now - start).TotalSeconds > 10)
            {
                Console.WriteLine("Timeout Order not Opened");
                result.IsSuccess = false;
                result.Message = "Timeout Order not Opened";
                return result;
            }

            if (Values.FailedOrderOpen != null)
            {
                result.IsSuccess = false;
                result.Message = Values.FailedOrderOpen.Error;
                return result;
            }

            if (Values.OrderData != null)
            {
                result.IsSuccess = true;
                result.Message = Values.OrderData.Deal.Uuid;
                return result;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Method to Check The Win Amount of Order
    /// </summary>
    /// <param name="uuid"></param>
    /// <returns>CheckWinResponse</returns>
    public CheckWinResponse CheckWin(string uuid)
    {
        var result = new CheckWinResponse();
        DateTime start = DateTime.Now;
        while (Values.CloseOrderPl.ContainsKey(uuid) == false)
        {
            if ((DateTime.Now - start).TotalSeconds > 300)
            {
                result.IsSuccess = false;
                result.Message = "Timeout Order Closed not Found";
                return result;
            }
            
            Thread.Sleep(100);//Save CPU
        }
        result.WinAmount = Values.CloseOrderPl[uuid];
        result.IsSuccess = true;
        
        return result;
        
    }
    /// <summary>
    /// Private member to Send Message to Binolla Websocket Via Connection
    /// </summary>
    /// <param name="message"></param>
    private void SendWss(string message)
    {
        _wsClient.SendMessageAsync(message).GetAwaiter().GetResult();
    }


    public void Disconnect()
    {
        _wsClient.DisconnectAsync().GetAwaiter().GetResult();
    }
}