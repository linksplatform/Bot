using Tinkoff.InvestApi.V1;

namespace TraderBot;

public static class OperationExtensions
{
    public static long GetActualQuantity(this Operation operation) => (operation.Trades == null || operation.Trades.Count <= 0) ? operation.Quantity - operation.QuantityRest : operation.Trades.Sum(trade => trade.Quantity);
}