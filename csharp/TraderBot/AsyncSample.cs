using System.Net.NetworkInformation;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Collections.Stacks;
using Platform.Converters;
using Platform.Data;
using Platform.Data.Doublets;
using Platform.Data.Doublets.CriterionMatchers;
using Platform.Data.Doublets.Gql.Client;
using Platform.Data.Doublets.Numbers.Rational;
using Platform.Data.Doublets.Numbers.Raw;
using Platform.Data.Doublets.Sequences.Converters;
using Platform.Data.Doublets.Sequences.Walkers;
using Platform.Data.Doublets.Unicode;
using Platform.Data.Numbers.Raw;
using Platform.Numbers;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;
using TLinkAddress = System.UInt64;


namespace TraderBot;

public class AsyncService : BackgroundService
{
    private readonly InvestApiClient _investApi;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<AsyncService> _logger;
    private MoneyValue? _rubWithdrawLimit;
    public readonly int Quantity = 1;
    public readonly TLinkAddress Etf;
    public readonly LinksGqlAdapter LinksGqlAdapter;
    public CachingConverterDecorator<string, ulong> StringToUnicodeSequenceConverter;
    public CachingConverterDecorator<ulong, string> UnicodeSequenceToStringConverter;
    private readonly TLinkAddress Asset;
    public readonly string InstrumentTicker;
    private readonly TLinkAddress Balance;
    public readonly static EqualityComparer<ulong> EqualityComparer = EqualityComparer<ulong>.Default;
    private readonly TLinkAddress Currency;
    private readonly TLinkAddress Amount;
    private readonly TLinkAddress Rub;
    public static AddressToRawNumberConverter<ulong> AddressToNumberConverter;
    public ulong Type;
    public Etf? Instrument;
    public readonly RawNumberSequenceToBigIntegerConverter<TLinkAddress> RawNumberSequenceToBigIntegerConverter;
    public readonly BigIntegerToRawNumberSequenceConverter<TLinkAddress> BigIntegerToRawNumberSequenceConverter;
    public readonly TLinkAddress NegativeNumberMarker;
    private readonly DecimalToRationalConverter<TLinkAddress> DecimalToRationalConverter;
    private readonly RationalToDecimalConverter<TLinkAddress> RationalToDecimalConverter;

    public AsyncService(ILogger<AsyncService> logger, InvestApiClient investApi, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _investApi = investApi;
        _lifetime = lifetime;
        LinksGqlAdapter = new(new GraphQLHttpClient("https", new NewtonsoftJsonSerializer()), new LinksConstants<TLinkAddress>());
        TLinkAddress zero = default;
        TLinkAddress one = Arithmetic.Increment(zero);
        var typeIndex = one;
        Type = LinksGqlAdapter.GetOrCreate(typeIndex, typeIndex);
        var typeId = LinksGqlAdapter.GetOrCreate(Type, Arithmetic.Increment(ref typeIndex));
        var meaningRoot = LinksGqlAdapter.GetOrCreate(Type, Type);
        var unicodeSymbolMarker = LinksGqlAdapter.GetOrCreate(meaningRoot, Arithmetic.Increment(ref Type));
        var unicodeSequenceMarker = LinksGqlAdapter.GetOrCreate(meaningRoot, Arithmetic.Increment(ref Type));
        NegativeNumberMarker = LinksGqlAdapter.GetOrCreate(meaningRoot, Arithmetic.Increment(ref Type));
        BalancedVariantConverter<TLinkAddress> balancedVariantConverter = new(LinksGqlAdapter);
        RawNumberToAddressConverter<TLinkAddress> NumberToAddressConverter = new();
        AddressToNumberConverter = new();
        TargetMatcher<TLinkAddress> unicodeSymbolCriterionMatcher = new(LinksGqlAdapter, unicodeSymbolMarker);
        TargetMatcher<TLinkAddress> unicodeSequenceCriterionMatcher = new(LinksGqlAdapter, unicodeSequenceMarker);
        CharToUnicodeSymbolConverter<TLinkAddress> charToUnicodeSymbolConverter =
            new(LinksGqlAdapter, AddressToNumberConverter, unicodeSymbolMarker);
        UnicodeSymbolToCharConverter<TLinkAddress> unicodeSymbolToCharConverter =
            new(LinksGqlAdapter, NumberToAddressConverter, unicodeSymbolCriterionMatcher);
        StringToUnicodeSequenceConverter = new CachingConverterDecorator<string, TLinkAddress>(
            new StringToUnicodeSequenceConverter<TLinkAddress>(LinksGqlAdapter, charToUnicodeSymbolConverter,
                balancedVariantConverter, unicodeSequenceMarker));
        RightSequenceWalker<TLinkAddress> sequenceWalker =
            new(LinksGqlAdapter, new DefaultStack<TLinkAddress>(), unicodeSymbolCriterionMatcher.IsMatched);
        UnicodeSequenceToStringConverter = new CachingConverterDecorator<TLinkAddress, string>(
            new UnicodeSequenceToStringConverter<TLinkAddress>(LinksGqlAdapter, unicodeSequenceCriterionMatcher, sequenceWalker,
                unicodeSymbolToCharConverter));
        BigIntegerToRawNumberSequenceConverter =
            new(LinksGqlAdapter, AddressToNumberConverter, balancedVariantConverter, NegativeNumberMarker);
        RawNumberSequenceToBigIntegerConverter = new(LinksGqlAdapter, NumberToAddressConverter, NegativeNumberMarker);
        DecimalToRationalConverter = new(LinksGqlAdapter, BigIntegerToRawNumberSequenceConverter);
        RationalToDecimalConverter = new(LinksGqlAdapter, RawNumberSequenceToBigIntegerConverter);
        Asset = GetOrCreateType(Type, nameof(Asset));
        Balance = GetOrCreateType(Type, nameof(Balance));
        Etf = GetOrCreateType(Asset, nameof(Etf));
        Currency = GetOrCreateType(Asset, nameof(Currency));
        Rub = GetOrCreateType(Currency, nameof(Rub));
        Amount = GetOrCreateType(Type, nameof(Amount));
        var account = _investApi.Users.GetAccounts().Accounts[0];
        var rubBalanceMoneyValue = investApi.Operations.GetPositionsAsync(new PositionsRequest() { AccountId = account.Id }).ResponseAsync.Result.Money.First(moneyValue => moneyValue.Currency == "rub");
        var rubBalance = new Quotation(){Nano = rubBalanceMoneyValue.Nano, Units = rubBalanceMoneyValue.Units};
        Quotation? instrumentQuantity = null;
        var etfs = _investApi.Instruments.Etfs();
        InstrumentTicker = "TRUR";
        Instrument = etfs.Instruments.First(etf => etf.Ticker == InstrumentTicker);
        // var InstrumentTickerLink = StringToUnicodeSequenceConverter.Convert(InstrumentTicker);
        foreach (var portfolioPosition in investApi.Operations.GetPortfolio(new PortfolioRequest(){AccountId = account.Id}).Positions)
        {
            if (portfolioPosition.Figi != Instrument.Figi)
            {
                continue;
            }
            instrumentQuantity = portfolioPosition.Quantity;
        }
        if (instrumentQuantity != null)
        {
            _logger.LogInformation($"{InstrumentTicker} quantity: {instrumentQuantity.Units}");
        }
        var any = LinksGqlAdapter.Constants.Any;
        var @continue = LinksGqlAdapter.Constants.Continue;
        LinksGqlAdapter.Each(new Link<TLinkAddress>(any, any, any), link =>
        {
            var balance = LinksGqlAdapter.GetSource(link);
            if (!EqualityComparer.Equals(balance, Balance))
            {
                return @continue;
            }
            var balanceValue = LinksGqlAdapter.GetTarget(link);
            var balanceValueType = LinksGqlAdapter.GetSource(balanceValue);
            if (EqualityComparer.Equals(balanceValueType, Etf))
            {
                var etfAmount = LinksGqlAdapter.GetTarget(balanceValue);
                var amountType = LinksGqlAdapter.GetSource(etfAmount);
                if (!EqualityComparer.Equals(amountType, Amount))
                {
                    return @continue;
                }
                var amountValueAddress = LinksGqlAdapter.GetTarget(etfAmount);
                var amountValue = RationalToDecimalConverter.Convert(amountValueAddress);
                _logger.LogInformation($"{InstrumentTicker} amount: {amountValue}");
            }
            return @continue;
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TLinkAddress lotQuantityLink;
        LinksGqlAdapter.Each(link =>
        {
            var balance = LinksGqlAdapter.GetSource(link);
            if (!EqualityComparer.Equals(balance, Balance))
            {
                return LinksGqlAdapter.Constants.Continue;
            }
            var balanceValue = LinksGqlAdapter.GetTarget(link);
            var currency = LinksGqlAdapter.GetSource(balanceValue);
            if (!EqualityComparer.Equals(currency, Rub))
            {
                return LinksGqlAdapter.Constants.Continue;
            }
            var amountLink = LinksGqlAdapter.GetTarget(balanceValue);
            var amount = LinksGqlAdapter.GetSource(amountLink);
            if (EqualityComparer.Equals(amount, Amount))
            {
                var amountIdkHowToName = AddressToNumberConverter.Convert(LinksGqlAdapter.GetTarget(amountLink));
            }
            return LinksGqlAdapter.Constants.Continue;
        });
        var marketDataStream = _investApi.MarketDataStream.MarketDataStream();
        await marketDataStream.RequestStream.WriteAsync(new MarketDataRequest()
            {
                SubscribeInfoRequest = new SubscribeInfoRequest()
                {
                    Instruments = { new InfoInstrument() { Figi = Instrument.Figi } },
                    SubscriptionAction = SubscriptionAction.Subscribe
                },
                SubscribeOrderBookRequest = new SubscribeOrderBookRequest()
                {
                    Instruments = { new OrderBookInstrument() { Figi = Instrument.Figi, Depth = 1 } },
                    SubscriptionAction = SubscriptionAction.Subscribe
                },
                SubscribeTradesRequest = new SubscribeTradesRequest()
                {
                    Instruments = { new TradeInstrument(){Figi = Instrument.Figi} },
                    SubscriptionAction = SubscriptionAction.Subscribe
                }
            })
            .ContinueWith((task) =>
            {
                if (!task.IsCompletedSuccessfully)
                {
                    throw new Exception("Error while subscribing to market data");
                }
                _logger.LogInformation("Subscribed to market data");
            }, stoppingToken);
        var repsonseStream = marketDataStream.ResponseStream;
        await foreach (var data in repsonseStream.ReadAllAsync(stoppingToken))
        {
            if (data.PayloadCase == MarketDataResponse.PayloadOneofCase.Orderbook)
            {
                var orderBook = data.Orderbook;
                _logger.LogInformation("Orderbook data received from stream: {OrderBook}", orderBook);
                // TradeAssets(asset, account.Id, orderBook, Instrument.Figi);
            }
            else if (data.PayloadCase == MarketDataResponse.PayloadOneofCase.Trade)
            {
                var trade = data.Trade;
                if (trade.Direction == TradeDirection.Buy)
                {

                }
                else if (trade.Direction == TradeDirection.Sell)
                {

                }
                ;
            }
            _lifetime.StopApplication();
        }
    }

    private TLinkAddress GetOrCreateType(TLinkAddress baseType, string typeId)
    {
        return LinksGqlAdapter.GetOrCreate(baseType, StringToUnicodeSequenceConverter.Convert(typeId));
    }

    private async void TradeAssets(Asset? asset, string accountId, OrderBook marketOrderBook, string figi)
    {
        var cheapestBidOrder = marketOrderBook.Bids[0];
        var cheapestAskOrder = marketOrderBook.Asks[0];
        if (asset == null)
        {
            PostOrderRequest buyOrderRequest = new()
            {
                Figi = figi,
                Quantity = Quantity,
                Price = cheapestAskOrder.Price,
                Direction = OrderDirection.Buy,
                AccountId = accountId,
                OrderType = OrderType.Limit,
                OrderId = ""
            };
            PostBuyOrder(buyOrderRequest);
        }
        else
        {
            PostOrderRequest sellOrderRequest = new()
            {
                Figi = figi,
                Quantity = Quantity,
                Price = cheapestBidOrder.Price,
                Direction = OrderDirection.Sell,
                AccountId = accountId,
                OrderType = OrderType.Limit,
                OrderId = ""
            };
            PostSellOrderIfProfitable(asset.Value, sellOrderRequest);
        }

    }

    private async void PostBuyOrder(PostOrderRequest sellOrderRequest)
    {
        var buyOrderResponse = await _investApi.Orders.PostOrderAsync(sellOrderRequest).ResponseAsync;
        _logger.LogInformation($"Buy order placed: {buyOrderResponse}");
    }

    private async void PostSellOrderIfProfitable(Asset asset, PostOrderRequest sellOrderRequest)
    {
        if (sellOrderRequest.Price < asset.Price)
        {
            return;
        }
        var sellOrderResponse = await _investApi.Orders.PostOrderAsync(sellOrderRequest).ResponseAsync;
        _logger.LogInformation($"Sell order placed: {sellOrderResponse}");
    }
}
