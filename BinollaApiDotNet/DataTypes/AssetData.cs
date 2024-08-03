namespace BinollaApiDotNet.DataTypes;

public class AssetData
{
    //[2, 'AUDCAD', 'AUD/CAD', 'currency', 5, 70, None, None, None, 0, None, None, None, 1722535200, True, None, 0, 0.13, 70, None, 0, -0.01, 0.03, 1.57, 0.42, None, None, 0, 'fixed_time']
    public int ActiveId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public int Precision { get; set; }
    public int Payout { get; set; }
    public bool IsOpen { get; set; }
    public TradeType? TradeType { get; set; }
    
}