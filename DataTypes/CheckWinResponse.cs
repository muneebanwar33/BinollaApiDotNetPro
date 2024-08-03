namespace BinollaApiDotNet.DataTypes;

public class CheckWinResponse
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public double? WinAmount { get; set; }
}