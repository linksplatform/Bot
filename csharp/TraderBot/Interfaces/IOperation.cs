namespace TraderBot.Interfaces;

public interface IOperation
{
    string Id { get; }
    TradingOperationType OperationType { get; }
    DateTime Date { get; }
    long Quantity { get; }
    decimal Price { get; }
    long QuantityRest { get; }
    IEnumerable<ITrade>? Trades { get; }
    long GetActualQuantity();
}

public enum TradingOperationType
{
    Buy,
    Sell
}