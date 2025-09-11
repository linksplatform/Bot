namespace TraderBot.Interfaces;

public interface IInstrument
{
    string Figi { get; }
    string Ticker { get; }
    int Lot { get; }
    decimal MinPriceIncrement { get; }
    TradingInstrumentType InstrumentType { get; }
}

public enum TradingInstrumentType
{
    Etf,
    Shares
}