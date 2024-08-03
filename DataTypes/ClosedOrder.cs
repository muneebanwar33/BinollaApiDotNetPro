using System.Text.Json.Serialization;

namespace BinollaApiDotNet.DataTypes;


public class Deal
{
    public int Id { get; set; }
    public string Uuid { get; set; }
    public string OpenTime { get; set; }
    public string CloseTime { get; set; }
    public long OpenTimestamp { get; set; }
    public int OpenMs { get; set; }
    public int CloseMs { get; set; }
    public long CloseTimestamp { get; set; }
    public int Uid { get; set; }
    public int Rate { get; set; }
    public int Amount { get; set; }
    public double Profit { get; set; }
    public int PercentProfit { get; set; }
    public object RefundTimestamp { get; set; } // Use `object` for nullable fields
    public object RefundTime { get; set; }
    public int PercentLoss { get; set; }
    public double OpenPrice { get; set; }
    public double ClosePrice { get; set; }
    public int Command { get; set; }
    public string Asset { get; set; }
    public int IsDemo { get; set; }
    public int RequestId { get; set; }
    public string Currency { get; set; }
    public double AmountUsd { get; set; }
    public bool ProcessUpdate { get; set; }
}

public class ClosedOrder
{
    //{"profit":5.64,"deals":[{"id":26594560,"uuid":"3b85e7f6-5bab-49b3-863d-0c6177d5a6af","openTime":"2024-08-03 11:54:09","closeTime":"2024-08-03 11:55:09","openTimestamp":1722686049,"openMs":558,"closeMs":136,"closeTimestamp":1722686109,"uid":227177,"rate":1,"amount":1,"profit":0.88,"percentProfit":88,"refundTimestamp":null,"refundTime":null,"percentLoss":100,"openPrice":0.5649,"closePrice":0.56514,"command":0,"asset":"AUDCHF_otc","isDemo":1,"requestId":0,"currency":"USD","amountUsd":1,"processUpdate":true},{"id":26594559,"uuid":"949a314e-2d42-4be5-aeb2-e81e8731a00f","openTime":"2024-08-03 11:54:09","closeTime":"2024-08-03 11:55:09","openTimestamp":1722686049,"openMs":558,"closeMs":136,"closeTimestamp":1722686109,"uid":227177,"rate":1,"amount":1,"profit":0.88,"percentProfit":88,"refundTimestamp":null,"refundTime":null,"percentLoss":100,"openPrice":0.5649,"closePrice":0.56514,"command":0,"asset":"AUDCHF_otc","isDemo":1,"requestId":0,"currency":"USD","amountUsd":1,"processUpdate":true},{"id":26594557,"uuid":"08282365-d416-497f-819c-33893ce4ba06","openTime":"2024-08-03 11:54:09","closeTime":"2024-08-03 11:55:09","openTimestamp":1722686049,"openMs":558,"closeMs":136,"closeTimestamp":1722686109,"uid":227177,"rate":1,"amount":1,"profit":0.88,"percentProfit":88,"refundTimestamp":null,"refundTime":null,"percentLoss":100,"openPrice":0.5649,"closePrice":0.56514,"command":0,"asset":"AUDCHF_otc","isDemo":1,"requestId":0,"currency":"USD","amountUsd":1,"processUpdate":true}]}
    [JsonPropertyName("profit")]
    public double Profit { get; set; }
    [JsonPropertyName("deals")]
    public List<Deal> Deals { get; set; } = new List<Deal>();
    
}