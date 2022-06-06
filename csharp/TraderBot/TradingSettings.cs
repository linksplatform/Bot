namespace TraderBot;

public class TradingSettings
{
    public string? EtfTicker { get; set; }
    public string? CashCurrency { get; set; }
    public int AccountIndex { get; set; }
    public bool AllowSamePriceSell { get; set; }
    public string? FileToLog { get; set; }
}