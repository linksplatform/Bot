using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi.V1;
using TraderBot.Interfaces;

namespace TraderBot.Providers.Tinkoff;

public class TinkoffOrderBookStream : IOrderBookStream
{
    private readonly MarketDataStreamService.MarketDataStreamClient.MarketDataStream _stream;
    private readonly ILogger _logger;

    public TinkoffOrderBookStream(MarketDataStreamService.MarketDataStreamClient.MarketDataStream stream, ILogger logger)
    {
        _stream = stream;
        _logger = logger;
    }

    public async IAsyncEnumerator<IOrderBook> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        await foreach (var data in _stream.ResponseStream.ReadAllAsync(cancellationToken))
        {
            if (data.PayloadCase == MarketDataResponse.PayloadOneofCase.Orderbook)
            {
                yield return new TinkoffOrderBook(data.Orderbook);
            }
        }
    }
}

public class TinkoffOrderBook : IOrderBook
{
    private readonly OrderBook _orderBook;

    public TinkoffOrderBook(OrderBook orderBook)
    {
        _orderBook = orderBook;
    }

    public string Figi => _orderBook.Figi;
    public IEnumerable<IOrderBookEntry> Bids => _orderBook.Bids.Select(bid => new TinkoffOrderBookEntry(bid));
    public IEnumerable<IOrderBookEntry> Asks => _orderBook.Asks.Select(ask => new TinkoffOrderBookEntry(ask));
}

public class TinkoffOrderBookEntry : IOrderBookEntry
{
    private readonly Order _order;

    public TinkoffOrderBookEntry(Order order)
    {
        _order = order;
    }

    public decimal Price => QuotationToDecimal(_order.Price);
    public long Quantity => _order.Quantity;

    private static decimal QuotationToDecimal(Quotation value) => value.Units + value.Nano / 1000000000m;
}