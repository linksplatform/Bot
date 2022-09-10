namespace TraderBot;

public class TradingSettings
{
    public Instrument Instrument { get; set; }
    public string? Ticker { get; set; }
    public string? CashCurrency { get; set; }
    public int AccountIndex { get; set; }
    public long MinimumProfitSteps { get; set; }
    public int MarketOrderBookDepth { get; set; }
    public long MinimumMarketOrderSizeToChangeBuyPrice { get; set; }
    public long MinimumMarketOrderSizeToChangeSellPrice { get; set; }
    public long MinimumMarketOrderSizeToBuy { get; set; }
    public long MinimumMarketOrderSizeToSell { get; set; }
    public string? MinimumTimeToBuy { get; set; }
    public string? MaximumTimeToBuy { get; set; }
    public long EarlySellOwnedLotsDelta { get; set; }
    public decimal EarlySellOwnedLotsMultiplier { get; set; }
    public DateTime LoadOperationsFrom { get; set; }
}