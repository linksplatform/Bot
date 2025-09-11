using Tinkoff.InvestApi.V1;
using TraderBot.Interfaces;

namespace TraderBot.Providers.Tinkoff;

public class TinkoffInstrument : IInstrument
{
    public TinkoffInstrument(Etf etf, TradingInstrumentType instrumentType)
    {
        Figi = etf.Figi;
        Ticker = etf.Ticker;
        Lot = etf.Lot;
        MinPriceIncrement = QuotationToDecimal(etf.MinPriceIncrement);
        InstrumentType = instrumentType;
    }

    public TinkoffInstrument(Share share, TradingInstrumentType instrumentType)
    {
        Figi = share.Figi;
        Ticker = share.Ticker;
        Lot = share.Lot;
        MinPriceIncrement = QuotationToDecimal(share.MinPriceIncrement);
        InstrumentType = instrumentType;
    }

    public string Figi { get; }
    public string Ticker { get; }
    public int Lot { get; }
    public decimal MinPriceIncrement { get; }
    public TradingInstrumentType InstrumentType { get; }

    private static decimal QuotationToDecimal(Quotation value) => value.Units + value.Nano / 1000000000m;
}