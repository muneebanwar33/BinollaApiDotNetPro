namespace BinollaApiDotNet.DataTypes;

/// <summary>
/// Represents trading asset data from Binolla API
/// </summary>
public class AssetData
{
    // Asset array format: [ActiveId, Name, Description, Type, Precision, Payout, ..., IsOpen(14), ..., TradeType(27)]
    // Example: [2, "AUDCAD", "AUD/CAD", "currency", 5, 20, null, null, null, 0, null, null, null, 1748959200, false, null, 0, 0.58, 20, null, 0, -0.01, 0.01, 0.58, -1.91, -0.07, 0.67, 0, "fixed_time"]
    
    /// <summary>
    /// Unique asset identifier
    /// </summary>
    public int ActiveId { get; set; }
    
    /// <summary>
    /// Asset symbol/name (e.g., "EURUSD")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable asset description (e.g., "EUR/USD")
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Asset type (e.g., "currency", "stock", "commodity", "cryptocurrency")
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Price precision (decimal places)
    /// </summary>
    public int Precision { get; set; }
    
    /// <summary>
    /// Payout percentage
    /// </summary>
    public int Payout { get; set; }
    
    /// <summary>
    /// Whether the asset is currently open for trading
    /// </summary>
    public bool IsOpen { get; set; }
    
    /// <summary>
    /// Trading type (fixed_time or blitz)
    /// </summary>
    public TradeType? TradeType { get; set; }
    
    /// <summary>
    /// Associated OTC asset ID (if applicable)
    /// </summary>
    public int? OtcId { get; set; }
    
    /// <summary>
    /// Base asset ID (if this is an OTC version)
    /// </summary>
    public int? BaseAssetId { get; set; }
    
    /// <summary>
    /// Market close timestamp
    /// </summary>
    public long? CloseTimestamp { get; set; }
    
    /// <summary>
    /// Current price change percentage
    /// </summary>
    public double? PriceChange { get; set; }
    
    /// <summary>
    /// Alternative payout percentage
    /// </summary>
    public int? AlternativePayout { get; set; }
    
    /// <summary>
    /// Maximum payout percentage
    /// </summary>
    public int? MaxPayout { get; set; }
    
    /// <summary>
    /// Various price movement indicators (array positions 21-26)
    /// </summary>
    public double? PriceIndicator1 { get; set; }
    public double? PriceIndicator2 { get; set; }
    public double? PriceIndicator3 { get; set; }
    public double? PriceIndicator4 { get; set; }
    public double? PriceIndicator5 { get; set; }
    public double? PriceIndicator6 { get; set; }
    
    /// <summary>
    /// Asset status indicator
    /// </summary>
    public int? StatusCode { get; set; }
    
    /// <summary>
    /// DateTime representation of close timestamp
    /// </summary>
    public DateTime? CloseDateTime => CloseTimestamp.HasValue 
        ? DateTimeOffset.FromUnixTimeSeconds(CloseTimestamp.Value).DateTime 
        : null;
    
    /// <summary>
    /// Indicates if this is an OTC (Over The Counter) asset
    /// </summary>
    public bool IsOTC => Name.Contains("_otc", StringComparison.OrdinalIgnoreCase);
    
    /// <summary>
    /// Indicates if this is a rush/short-term trading asset
    /// </summary>
    public bool IsRush => Name.Contains("_rush", StringComparison.OrdinalIgnoreCase);
    
    /// <summary>
    /// Returns the base symbol name without OTC or rush suffixes
    /// </summary>
    public string BaseSymbol => Name.Replace("_otc", "").Replace("_rush", "").ToUpperInvariant();
    
    /// <summary>
    /// Returns a string representation of the asset
    /// </summary>
    public override string ToString()
    {
        var status = IsOpen ? "OPEN" : "CLOSED";
        var typeInfo = IsOTC ? " (OTC)" : IsRush ? " (RUSH)" : "";
        return $"{Name}{typeInfo}: {Description} [{status}] - Payout: {Payout}%";
    }
}