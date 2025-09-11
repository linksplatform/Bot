using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi.V1;
using TraderBot.Interfaces;

namespace TraderBot.Providers.Tinkoff;

public class TinkoffTradesStream : ITradesStream
{
    private readonly OrdersStreamService.OrdersStreamClient.TradesStream _stream;
    private readonly ILogger _logger;

    public TinkoffTradesStream(OrdersStreamService.OrdersStreamClient.TradesStream stream, ILogger logger)
    {
        _stream = stream;
        _logger = logger;
    }

    public async IAsyncEnumerator<ITradeData> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        await foreach (var data in _stream.ResponseStream.ReadAllAsync(cancellationToken))
        {
            yield return new TinkoffTradeData(data);
        }
    }
}

public class TinkoffTradeData : ITradeData
{
    private readonly TradesStreamResponse _data;

    public TinkoffTradeData(TradesStreamResponse data)
    {
        _data = data;
    }

    public TradeDataType DataType => _data.PayloadCase switch
    {
        TradesStreamResponse.PayloadOneofCase.OrderTrades => TradeDataType.OrderTrades,
        TradesStreamResponse.PayloadOneofCase.Ping => TradeDataType.Ping,
        _ => throw new InvalidOperationException($"Unknown trade data type: {_data.PayloadCase}")
    };

    public IOrderTrades? OrderTrades => _data.PayloadCase == TradesStreamResponse.PayloadOneofCase.OrderTrades 
        ? new TinkoffOrderTrades(_data.OrderTrades) 
        : null;
}

public class TinkoffOrderTrades : IOrderTrades
{
    private readonly OrderTrades _orderTrades;

    public TinkoffOrderTrades(OrderTrades orderTrades)
    {
        _orderTrades = orderTrades;
    }

    public string OrderId => _orderTrades.OrderId;
    public OrderDirection Direction => (OrderDirection)_orderTrades.Direction;
    public IEnumerable<ITrade> Trades => _orderTrades.Trades.Select(trade => new TinkoffTrade(trade));
}

public class TinkoffTrade : ITrade
{
    private readonly OrderTrade _trade;

    public TinkoffTrade(OrderTrade trade)
    {
        _trade = trade;
    }

    public long Quantity => _trade.Quantity;
    public decimal Price => QuotationToDecimal(_trade.Price);
    public DateTime DateTime => _trade.DateTime.ToDateTime();

    private static decimal QuotationToDecimal(Quotation value) => value.Units + value.Nano / 1000000000m;
}