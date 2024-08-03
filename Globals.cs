using BinollaApiDotNet.DataTypes;

namespace BinollaApiDotNet;

public class Globals
{
    // Singleton instance
    private static Globals _instance;
    private static readonly object _lock = new object();

    
    // Properties to hold settings
    public bool WebsocketIsConnected { get; set; } = false;
    public bool SslMutualExclusion { get; set; }
    public bool SslMutualExclusionWrite { get; set; }

    public string SSID { get; set; } = "";
    
    public string Reason { get; set; }
    
    public bool CheckWebsocketIfError { get; set; }
    public string WebsocketErrorReason { get; set; }
    public bool SSIDAccepted { get; set; } = false;
    
    public int BalanceType { get; set; } = 1;
    public double? BalanceReal { get; set; } = null;
    public double? BalanceDemo { get; set; } = null;
    public DateTime? BalanceUpdated { get; set; } = null;
    public Dictionary<string, double> BalancesRaw { get; set; } = new Dictionary<string, double>();
    public Dictionary<string, double> Payments { get; set; } = new Dictionary<string, double>();
    public string Result { get; set; }
    public List<object> CloseOrderDataRaw { get; set; } = new List<object>();
    public Dictionary<string, double> CloseOrderPl { get; set; } = new Dictionary<string, double>();


    public FailedOrderOpen? FailedOrderOpen { get; set; } = null;
    
    public List<OpenedOrder> OpenedOrders { get; set; } = new List<OpenedOrder>();
    public OpenedOrder? OrderData { get; set; } = null;
    public OpenedOrder? NewOpenOrder { get; set; } = null;
    public string? OrderOpenUuid { get; set; }
    
    public List<ClosedOrder> ClosedOrders { get; set; } = new List<ClosedOrder>();
    public ClosedOrder? ClosedOrderData { get; set; } = null;
    
    public List<AssetData> PaymentAssets { get; set; } = new List<AssetData>();
    private Globals() { }

    public static Globals Values
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new Globals();
                }
                return _instance;
            }
        }
    }
  
}