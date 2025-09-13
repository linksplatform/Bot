namespace TraderBot;

public interface ITradeApiProvider
{
    Task<decimal> GetCurrentPrice(string symbol);
    Task<string> PlaceBuyOrder(string symbol, int lots, decimal price);
    Task<string> PlaceSellOrder(string symbol, int lots, decimal price);
    Task<bool> CancelOrder(string orderId);
    Task<OrderState> GetOrderStatus(string orderId);
    Task<(decimal Free, decimal Locked)> GetBalance(string currency);
    Task<List<OrderBook>> GetOrderBook(string symbol, int depth);
}

public class OrderBook
{
    public decimal Price { get; set; }
    public long Quantity { get; set; }
    public OrderType Type { get; set; } // Buy or Sell
}

public enum OrderType
{
    Buy,
    Sell
}

public enum OrderState
{
    New,
    PartiallyFilled,
    Filled,
    Cancelled,
    Rejected
}