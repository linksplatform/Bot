using Tinkoff.InvestApi.V1;
using TraderBot.Interfaces;

namespace TraderBot.Providers.Tinkoff;

public class TinkoffOperation : IOperation
{
    private readonly Operation _operation;

    public TinkoffOperation(Operation operation)
    {
        _operation = operation;
    }

    public string Id => _operation.Id;
    public OperationType OperationType => (OperationType)_operation.OperationType;
    public DateTime Date => _operation.Date.ToDateTime();
    public long Quantity => _operation.Quantity;
    public decimal Price => MoneyValueToDecimal(_operation.Price);
    public long QuantityRest => _operation.QuantityRest;
    public IEnumerable<ITrade>? Trades => _operation.Trades?.Select(trade => new TinkoffOperationTrade(trade));

    public long GetActualQuantity() => (_operation.Trades == null || _operation.Trades.Count <= 0) 
        ? _operation.Quantity - _operation.QuantityRest 
        : _operation.Trades.Sum(trade => trade.Quantity);

    private static decimal MoneyValueToDecimal(MoneyValue value) => value.Units + value.Nano / 1000000000m;
}

public class TinkoffOperationTrade : ITrade
{
    private readonly OperationTrade _trade;

    public TinkoffOperationTrade(OperationTrade trade)
    {
        _trade = trade;
    }

    public long Quantity => _trade.Quantity;
    public decimal Price => MoneyValueToDecimal(_trade.Price);
    public DateTime DateTime => _trade.DateTime.ToDateTime();

    private static decimal MoneyValueToDecimal(MoneyValue value) => value.Units + value.Nano / 1000000000m;
}