namespace TraderBot.Interfaces;

public interface IOrder
{
    string OrderId { get; }
    string Figi { get; }
    TradingOrderDirection Direction { get; }
    long LotsRequested { get; set; }
    decimal InitialSecurityPrice { get; }
    decimal InitialOrderPrice { get; }
}

public interface IOrderRequest
{
    string OrderId { get; }
    string AccountId { get; }
    TradingOrderDirection Direction { get; }
    TradingOrderType OrderType { get; }
    string Figi { get; }
    long Quantity { get; }
    decimal Price { get; }
}

public interface IOrderResponse
{
    string OrderId { get; }
    string Figi { get; }
    TradingOrderDirection Direction { get; }
    long LotsRequested { get; }
    decimal InitialSecurityPrice { get; }
}

public interface ICancelOrderResponse
{
    string OrderId { get; }
}

public enum TradingOrderDirection
{
    Buy,
    Sell
}

public enum TradingOrderType
{
    Limit,
    Market
}