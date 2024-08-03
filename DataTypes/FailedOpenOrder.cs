namespace BinollaApiDotNet.DataTypes;

public class FailedOrderOpen
{
    public string Error { get; set; }
    public int IsDemo { get; set; }
    public int RequestId { get; set; }
    public int Amount { get; set; }
    public string Asset { get; set; }
    public long Time { get; set; }
}