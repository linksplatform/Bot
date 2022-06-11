namespace TraderBot;

public class TradingSettings
{
    public string? EtfTicker { get; set; }
    public string? CashCurrency { get; set; }
    public int AccountIndex { get; set; }
    public long MinimumProfitSteps { get; set; }
    public long MinimumMarketOrderSizeToChangeBuyPrice { get; set; }
    public long MinimumMarketOrderSizeToChangeSellPrice { get; set; }
    public long MinimumMarketOrderSizeToBuy { get; set; }
    public long MinimumMarketOrderSizeToSell { get; set; }
    public long EarlySellOwnedLotsDelta { get; set; }
    public decimal EarlySellOwnedLotsMultiplier { get; set; }
    public DateTime LoadOperationsFrom { get; set; }
}