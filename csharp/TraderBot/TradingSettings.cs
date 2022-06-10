namespace TraderBot;

public class TradingSettings
{
    public string? EtfTicker { get; set; }
    public string? CashCurrency { get; set; }
    public int AccountIndex { get; set; }
    public long MinimumProfitSteps { get; set; }
    public long MinimumSecuritiesAmountToChangePrice { get; set; }
    public long MinimumSecuritiesAmountToBuy { get; set; }
    public long EarlySellOwnedLotsDelta { get; set; }
    public decimal EarlySellOwnedLotsMultiplier { get; set; }
}