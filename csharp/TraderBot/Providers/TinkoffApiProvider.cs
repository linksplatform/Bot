using Grpc.Core;
using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;
using TraderBot.Interfaces;
using TraderBot.Providers.Tinkoff;
using Google.Protobuf.WellKnownTypes;

namespace TraderBot.Providers;

public class TinkoffApiProvider : ITradingApiProvider
{
    private readonly InvestApiClient _investApi;
    private readonly ILogger<TinkoffApiProvider> _logger;

    public TinkoffApiProvider(InvestApiClient investApi, ILogger<TinkoffApiProvider> logger)
    {
        _investApi = investApi;
        _logger = logger;
    }

    public Task InitializeAsync()
    {
        // Tinkoff API is initialized during construction
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<IAccount>> GetAccountsAsync()
    {
        var response = await _investApi.Users.GetAccountsAsync();
        return response.Accounts.Select(account => new TinkoffAccount(account));
    }

    public async Task<IInstrument> GetInstrumentAsync(string ticker, InstrumentType instrumentType)
    {
        if (instrumentType == InstrumentType.Etf)
        {
            var response = await _investApi.Instruments.EtfsAsync();
            var instrument = response.Instruments.First(etf => etf.Ticker == ticker);
            return new TinkoffInstrument(instrument, InstrumentType.Etf);
        }
        else if (instrumentType == InstrumentType.Shares)
        {
            var response = await _investApi.Instruments.SharesAsync();
            var instrument = response.Instruments.First(share => share.Ticker == ticker);
            return new TinkoffInstrument(instrument, InstrumentType.Shares);
        }
        else
        {
            throw new InvalidOperationException("Not supported instrument type.");
        }
    }

    public async Task<IOrderBookStream> SubscribeToOrderBookAsync(string figi, int depth)
    {
        var marketDataStream = _investApi.MarketDataStream.MarketDataStream();
        await marketDataStream.RequestStream.WriteAsync(new MarketDataRequest
        {
            SubscribeOrderBookRequest = new SubscribeOrderBookRequest
            {
                Instruments = { new OrderBookInstrument { Figi = figi, Depth = depth } },
                SubscriptionAction = SubscriptionAction.Subscribe
            },
        });

        return new TinkoffOrderBookStream(marketDataStream, _logger);
    }

    public async Task<ITradesStream> SubscribeToTradesAsync(string accountId)
    {
        var tradesStream = _investApi.OrdersStream.TradesStream(new TradesStreamRequest
        {
            Accounts = { accountId }
        });

        return new TinkoffTradesStream(tradesStream, _logger);
    }

    public async Task<IEnumerable<IOrder>> GetOrdersAsync(string accountId)
    {
        var response = await _investApi.Orders.GetOrdersAsync(new GetOrdersRequest { AccountId = accountId });
        return response.Orders.Select(order => new TinkoffOrder(order));
    }

    public async Task<IEnumerable<IPosition>> GetPositionsAsync(string accountId)
    {
        var response = await _investApi.Operations.GetPositionsAsync(new PositionsRequest { AccountId = accountId });
        return response.Securities.Select(position => new TinkoffPosition(position));
    }

    public async Task<IPortfolio> GetPortfolioAsync(string accountId)
    {
        var response = await _investApi.Operations.GetPortfolioAsync(new PortfolioRequest { AccountId = accountId });
        return new TinkoffPortfolio(response);
    }

    public async Task<IEnumerable<IOperation>> GetOperationsAsync(string accountId, string figi, DateTime from, DateTime to)
    {
        var response = await _investApi.Operations.GetOperationsAsync(new OperationsRequest
        {
            AccountId = accountId,
            State = OperationState.Executed,
            Figi = figi,
            From = Timestamp.FromDateTime(from),
            To = Timestamp.FromDateTime(to)
        });

        return response.Operations.Select(operation => new TinkoffOperation(operation));
    }

    public async Task<IOrderResponse> PlaceOrderAsync(IOrderRequest orderRequest)
    {
        var tinkoffRequest = new PostOrderRequest
        {
            OrderId = orderRequest.OrderId,
            AccountId = orderRequest.AccountId,
            Direction = (Tinkoff.InvestApi.V1.OrderDirection)orderRequest.Direction,
            OrderType = (Tinkoff.InvestApi.V1.OrderType)orderRequest.OrderType,
            Figi = orderRequest.Figi,
            Quantity = orderRequest.Quantity,
            Price = DecimalToQuotation(orderRequest.Price)
        };

        var response = await _investApi.Orders.PostOrderAsync(tinkoffRequest);
        return new TinkoffOrderResponse(response);
    }

    public async Task<ICancelOrderResponse> CancelOrderAsync(string accountId, string orderId)
    {
        var response = await _investApi.Orders.CancelOrderAsync(new CancelOrderRequest
        {
            AccountId = accountId,
            OrderId = orderId,
        });
        return new TinkoffCancelOrderResponse(response);
    }

    private static Quotation DecimalToQuotation(decimal value)
    {
        var units = (long)Math.Truncate(value);
        var nano = (int)Math.Truncate((value - units) * 1000000000m);
        return new Quotation { Units = units, Nano = nano };
    }
}