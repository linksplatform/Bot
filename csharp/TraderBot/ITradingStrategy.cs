namespace TraderBot;

public interface ITradingStrategy
{
    Task<IEnumerable<TradingAction>> CalculateActions(TradingContext context);
    string Name { get; }
}

public class TradingContext
{
    public decimal CurrentPrice { get; set; }
    public List<OrderBook> OrderBook { get; set; } = new();
    public decimal CashBalance { get; set; }
    public decimal AssetBalance { get; set; }
    public List<ActiveOrder> ActiveOrders { get; set; } = new();
    public DateTime CurrentTime { get; set; }
    public TradingSettings? Settings { get; set; }
}

public class ActiveOrder
{
    public string Id { get; set; } = "";
    public OrderType Type { get; set; }
    public decimal Price { get; set; }
    public int Lots { get; set; }
    public OrderState State { get; set; }
    public DateTime PlacedAt { get; set; }
}

public abstract class TradingAction
{
    public abstract Task Execute(ITradeApiProvider apiProvider);
}

public class PlaceBuyOrderAction : TradingAction
{
    public string Symbol { get; set; } = "";
    public int Lots { get; set; }
    public decimal Price { get; set; }

    public override async Task Execute(ITradeApiProvider apiProvider)
    {
        await apiProvider.PlaceBuyOrder(Symbol, Lots, Price);
    }
}

public class PlaceSellOrderAction : TradingAction
{
    public string Symbol { get; set; } = "";
    public int Lots { get; set; }
    public decimal Price { get; set; }

    public override async Task Execute(ITradeApiProvider apiProvider)
    {
        await apiProvider.PlaceSellOrder(Symbol, Lots, Price);
    }
}

public class CancelOrderAction : TradingAction
{
    public string OrderId { get; set; } = "";

    public override async Task Execute(ITradeApiProvider apiProvider)
    {
        await apiProvider.CancelOrder(OrderId);
    }
}

public class MoveBuyOrderAction : TradingAction
{
    public string OrderId { get; set; } = "";
    public string Symbol { get; set; } = "";
    public int Lots { get; set; }
    public decimal NewPrice { get; set; }

    public override async Task Execute(ITradeApiProvider apiProvider)
    {
        await apiProvider.CancelOrder(OrderId);
        await Task.Delay(100); // Small delay to ensure cancellation
        await apiProvider.PlaceBuyOrder(Symbol, Lots, NewPrice);
    }
}