using Tinkoff.InvestApi;

namespace TraderBot;

public class TradingConfiguration
{
    public string Name { get; set; } = string.Empty;
    public TradingSettings TradingSettings { get; set; } = new();
    public InvestApiSettings InvestApiSettings { get; set; } = new();
}

public class MultiTradingConfiguration
{
    public TradingConfiguration[] Configurations { get; set; } = Array.Empty<TradingConfiguration>();
}