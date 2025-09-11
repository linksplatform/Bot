using Tinkoff.InvestApi.V1;
using TraderBot.Interfaces;

namespace TraderBot.Providers.Tinkoff;

public class TinkoffOrder : IOrder
{
    private readonly OrderState _orderState;

    public TinkoffOrder(OrderState orderState)
    {
        _orderState = orderState;
    }

    public string OrderId => _orderState.OrderId;
    public string Figi => _orderState.Figi;
    public TradingOrderDirection Direction => (TradingOrderDirection)_orderState.Direction;
    public long LotsRequested { get => _orderState.LotsRequested; set => _orderState.LotsRequested = value; }
    public decimal InitialSecurityPrice => MoneyValueToDecimal(_orderState.InitialSecurityPrice);
    public decimal InitialOrderPrice => MoneyValueToDecimal(_orderState.InitialOrderPrice);

    private static decimal MoneyValueToDecimal(MoneyValue value) => value.Units + value.Nano / 1000000000m;
}

public class TinkoffOrderResponse : IOrderResponse
{
    private readonly PostOrderResponse _response;

    public TinkoffOrderResponse(PostOrderResponse response)
    {
        _response = response;
    }

    public string OrderId => _response.OrderId;
    public string Figi => _response.Figi;
    public TradingOrderDirection Direction => (TradingOrderDirection)_response.Direction;
    public long LotsRequested => _response.LotsRequested;
    public decimal InitialSecurityPrice => MoneyValueToDecimal(_response.InitialSecurityPrice);

    private static decimal MoneyValueToDecimal(MoneyValue value) => value.Units + value.Nano / 1000000000m;
}

public class TinkoffCancelOrderResponse : ICancelOrderResponse
{
    private readonly CancelOrderResponse _response;

    public TinkoffCancelOrderResponse(CancelOrderResponse response)
    {
        _response = response;
    }

    public string OrderId => _response.OrderId;
}

public class TinkoffOrderRequest : IOrderRequest
{
    public string OrderId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public TradingOrderDirection Direction { get; set; }
    public TradingOrderType OrderType { get; set; }
    public string Figi { get; set; } = string.Empty;
    public long Quantity { get; set; }
    public decimal Price { get; set; }
}