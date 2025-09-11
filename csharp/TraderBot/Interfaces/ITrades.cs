namespace TraderBot.Interfaces;

public interface ITradesStream : IAsyncEnumerable<ITradeData>
{
}

public interface ITradeData
{
    TradeDataType DataType { get; }
    IOrderTrades? OrderTrades { get; }
}

public interface IOrderTrades
{
    string OrderId { get; }
    TradingOrderDirection Direction { get; }
    IEnumerable<ITrade> Trades { get; }
}

public interface ITrade
{
    long Quantity { get; }
    decimal Price { get; }
    DateTime DateTime { get; }
}

public enum TradeDataType
{
    OrderTrades,
    Ping
}