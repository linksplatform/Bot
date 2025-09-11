using Microsoft.Extensions.Logging;
using TraderBot.Interfaces;

namespace TraderBot.Providers;

public class MockApiProvider : ITradingApiProvider
{
    private readonly ILogger<MockApiProvider> _logger;
    private readonly List<IAccount> _accounts;
    private readonly Dictionary<string, IInstrument> _instruments;
    private readonly Random _random = new Random();

    public MockApiProvider(ILogger<MockApiProvider> logger)
    {
        _logger = logger;
        
        // Initialize mock data
        _accounts = new List<IAccount>
        {
            new MockAccount("mock-account-1", "Mock Trading Account", DateTime.UtcNow.AddYears(-1))
        };

        _instruments = new Dictionary<string, IInstrument>
        {
            ["MOCK"] = new MockInstrument("MOCK-FIGI", "MOCK", 1, 0.01m, InstrumentType.Etf),
            ["MOCKSHARE"] = new MockInstrument("MOCK-SHARE-FIGI", "MOCKSHARE", 1, 0.01m, InstrumentType.Shares)
        };
    }

    public Task InitializeAsync()
    {
        _logger.LogInformation("Mock API Provider initialized");
        return Task.CompletedTask;
    }

    public Task<IEnumerable<IAccount>> GetAccountsAsync()
    {
        _logger.LogInformation("Getting mock accounts");
        return Task.FromResult<IEnumerable<IAccount>>(_accounts);
    }

    public Task<IInstrument> GetInstrumentAsync(string ticker, InstrumentType instrumentType)
    {
        _logger.LogInformation($"Getting mock instrument: {ticker}");
        if (_instruments.TryGetValue(ticker, out var instrument))
        {
            return Task.FromResult(instrument);
        }
        throw new InvalidOperationException($"Instrument {ticker} not found in mock provider");
    }

    public Task<IOrderBookStream> SubscribeToOrderBookAsync(string figi, int depth)
    {
        _logger.LogInformation($"Subscribing to mock order book: {figi}");
        return Task.FromResult<IOrderBookStream>(new MockOrderBookStream(_logger));
    }

    public Task<ITradesStream> SubscribeToTradesAsync(string accountId)
    {
        _logger.LogInformation($"Subscribing to mock trades: {accountId}");
        return Task.FromResult<ITradesStream>(new MockTradesStream(_logger));
    }

    public Task<IEnumerable<IOrder>> GetOrdersAsync(string accountId)
    {
        _logger.LogInformation($"Getting mock orders for account: {accountId}");
        return Task.FromResult<IEnumerable<IOrder>>(new List<IOrder>());
    }

    public Task<IEnumerable<IPosition>> GetPositionsAsync(string accountId)
    {
        _logger.LogInformation($"Getting mock positions for account: {accountId}");
        return Task.FromResult<IEnumerable<IPosition>>(new List<IPosition>());
    }

    public Task<IPortfolio> GetPortfolioAsync(string accountId)
    {
        _logger.LogInformation($"Getting mock portfolio for account: {accountId}");
        return Task.FromResult<IPortfolio>(new MockPortfolio());
    }

    public Task<IEnumerable<IOperation>> GetOperationsAsync(string accountId, string figi, DateTime from, DateTime to)
    {
        _logger.LogInformation($"Getting mock operations for account: {accountId}, figi: {figi}");
        return Task.FromResult<IEnumerable<IOperation>>(new List<IOperation>());
    }

    public Task<IOrderResponse> PlaceOrderAsync(IOrderRequest orderRequest)
    {
        _logger.LogInformation($"Placing mock order: {orderRequest.Direction} {orderRequest.Quantity} @ {orderRequest.Price}");
        return Task.FromResult<IOrderResponse>(new MockOrderResponse(orderRequest.OrderId, orderRequest.Figi, orderRequest.Direction, orderRequest.Quantity, orderRequest.Price));
    }

    public Task<ICancelOrderResponse> CancelOrderAsync(string accountId, string orderId)
    {
        _logger.LogInformation($"Canceling mock order: {orderId}");
        return Task.FromResult<ICancelOrderResponse>(new MockCancelOrderResponse(orderId));
    }
}

// Mock implementations of interfaces
public class MockAccount : IAccount
{
    public MockAccount(string id, string name, DateTime openedDate)
    {
        Id = id;
        Name = name;
        OpenedDate = openedDate;
    }

    public string Id { get; }
    public string Name { get; }
    public DateTime OpenedDate { get; }

    public override string ToString() => $"MockAccount(Id={Id}, Name={Name})";
}

public class MockInstrument : IInstrument
{
    public MockInstrument(string figi, string ticker, int lot, decimal minPriceIncrement, InstrumentType instrumentType)
    {
        Figi = figi;
        Ticker = ticker;
        Lot = lot;
        MinPriceIncrement = minPriceIncrement;
        InstrumentType = instrumentType;
    }

    public string Figi { get; }
    public string Ticker { get; }
    public int Lot { get; }
    public decimal MinPriceIncrement { get; }
    public InstrumentType InstrumentType { get; }

    public override string ToString() => $"MockInstrument(Ticker={Ticker}, Figi={Figi})";
}

public class MockOrderBookStream : IOrderBookStream
{
    private readonly ILogger _logger;

    public MockOrderBookStream(ILogger logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerator<IOrderBook> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var random = new Random();
        while (!cancellationToken.IsCancellationRequested)
        {
            var basePrice = 100m + random.Next(-10, 10);
            var orderBook = new MockOrderBook("MOCK-FIGI", basePrice);
            yield return orderBook;
            await Task.Delay(1000, cancellationToken);
        }
    }
}

public class MockOrderBook : IOrderBook
{
    public MockOrderBook(string figi, decimal basePrice)
    {
        Figi = figi;
        var random = new Random();
        
        Bids = new List<IOrderBookEntry>
        {
            new MockOrderBookEntry(basePrice - 0.01m, 1000 + random.Next(0, 500)),
            new MockOrderBookEntry(basePrice - 0.02m, 500 + random.Next(0, 300)),
        };

        Asks = new List<IOrderBookEntry>
        {
            new MockOrderBookEntry(basePrice + 0.01m, 1000 + random.Next(0, 500)),
            new MockOrderBookEntry(basePrice + 0.02m, 500 + random.Next(0, 300)),
        };
    }

    public string Figi { get; }
    public IEnumerable<IOrderBookEntry> Bids { get; }
    public IEnumerable<IOrderBookEntry> Asks { get; }
}

public class MockOrderBookEntry : IOrderBookEntry
{
    public MockOrderBookEntry(decimal price, long quantity)
    {
        Price = price;
        Quantity = quantity;
    }

    public decimal Price { get; }
    public long Quantity { get; }
}

public class MockTradesStream : ITradesStream
{
    private readonly ILogger _logger;

    public MockTradesStream(ILogger logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerator<ITradeData> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Send ping periodically
            yield return new MockTradeData();
            await Task.Delay(30000, cancellationToken); // 30 second ping interval
        }
    }
}

public class MockTradeData : ITradeData
{
    public TradeDataType DataType => TradeDataType.Ping;
    public IOrderTrades? OrderTrades => null;
}

public class MockPortfolio : IPortfolio
{
    public IEnumerable<IPortfolioPosition> Positions => new List<IPortfolioPosition>();
    public IEnumerable<IMoneyValue> Money => new List<IMoneyValue>
    {
        new MockMoneyValue("rub", 10000m)
    };
    public IEnumerable<IMoneyValue> Blocked => new List<IMoneyValue>();
}

public class MockMoneyValue : IMoneyValue
{
    public MockMoneyValue(string currency, decimal value)
    {
        Currency = currency;
        Value = value;
    }

    public string Currency { get; }
    public decimal Value { get; }
}

public class MockOrderResponse : IOrderResponse
{
    public MockOrderResponse(string orderId, string figi, OrderDirection direction, long lotsRequested, decimal initialSecurityPrice)
    {
        OrderId = orderId;
        Figi = figi;
        Direction = direction;
        LotsRequested = lotsRequested;
        InitialSecurityPrice = initialSecurityPrice;
    }

    public string OrderId { get; }
    public string Figi { get; }
    public OrderDirection Direction { get; }
    public long LotsRequested { get; }
    public decimal InitialSecurityPrice { get; }

    public override string ToString() => $"MockOrder(Id={OrderId}, Direction={Direction}, Quantity={LotsRequested}, Price={InitialSecurityPrice})";
}

public class MockCancelOrderResponse : ICancelOrderResponse
{
    public MockCancelOrderResponse(string orderId)
    {
        OrderId = orderId;
    }

    public string OrderId { get; }
}