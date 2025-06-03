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
    public bool IsDemo { get; set; }
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
    
    //{"deal":{"uuid":"129382eb-25ff-46f0-b8f7-e8b160874aa1","uid":227177,"isDemo":true,"openTime":"2025-06-03 11:20:24","closeTime":"2025-06-03 11:21:24","openPrice":0.53109,"command":0,"amount":1,"amountUsd":1,"rate":1,"currency":"USD","source":0,"ip":"2404:3100:1c94:4018:e449:8ac2:3aed:8cca","percentProfit":88,"percentLoss":100,"asset":"AUDCHF_otc","optionType":0,"openMs":760,"balance":9606.02,"digit":2,"profit":0.88,"openTimestamp":1748949624,"closeTimestamp":1748949684,"closePrice":0}}
    
    [JsonPropertyName("deal")]
    public OpenOrderDeal Deal { get; set; }
    

    
}