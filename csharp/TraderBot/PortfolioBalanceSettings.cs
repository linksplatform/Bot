namespace TraderBot;

public class PortfolioBalanceSettings
{
    public bool Enabled { get; set; }
    public List<AssetAllocation> AssetAllocations { get; set; } = new();
    public decimal RebalanceThresholdPercent { get; set; } = 5.0m;
    public TimeSpan RebalanceCheckInterval { get; set; } = TimeSpan.FromMinutes(10);
}

public class AssetAllocation
{
    public string AssetType { get; set; } = string.Empty;
    public string Ticker { get; set; } = string.Empty;
    public decimal TargetPercent { get; set; }
    public Instrument Instrument { get; set; }
}