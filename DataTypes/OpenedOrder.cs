using System.Text.Json.Serialization;

namespace BinollaApiDotNet.DataTypes;


public class OpenOrderDeal
{
    public string Uuid { get; set; }
    public int Uid { get; set; }
    public string OpenTime { get; set; }
    public string CloseTime { get; set; }
    public double OpenPrice { get; set; }
    public int Command { get; set; }
    public int Amount { get; set; }
    public double AmountUsd { get; set; }
    public double Rate { get; set; }
    public string Currency { get; set; }
    public int Source { get; set; }
    public int PercentProfit { get; set; }
    public int PercentLoss { get; set; }
    public string Asset { get; set; }
    public int IsDemo { get; set; }
    public int RequestId { get; set; }
    public int OptionType { get; set; }
    public int OpenMs { get; set; }
    public double Balance { get; set; }
    public int Digit { get; set; }
    public double Profit { get; set; }
    public long OpenTimestamp { get; set; }
    public long CloseTimestamp { get; set; }
    public double ClosePrice { get; set; }
}
public class OpenedOrder
{   
    
    ////{"deal":{"uuid":"283a8997-ad36-499b-b0e6-b73a4b59ea74","uid":227177,"openTime":"2024-08-03 11:40:15","closeTime":"2024-08-03 11:41:15","openPrice":0.56563,"command":0,"amount":1,"amountUsd":1,"rate":1,"currency":"USD","source":0,"percentProfit":88,"percentLoss":100,"asset":"AUDCHF_otc","isDemo":1,"requestId":0,"optionType":0,"openMs":576,"balance":9607.38,"digit":2,"profit":0.88,"openTimestamp":1722685215,"closeTimestamp":1722685275,"closePrice":0}}
    [JsonPropertyName("deal")]
    public OpenOrderDeal Deal { get; set; }
    

    
}