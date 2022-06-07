namespace TraderBot;

public class TradingSettings
{
    public string? EtfTicker { get; set; }
    public string? CashCurrency { get; set; }
    public int AccountIndex { get; set; }
    public long MinimumProfitSteps { get; set; }
    public long SecuritiesAmountThresholdForOrderPriceChange { get; set; }
}