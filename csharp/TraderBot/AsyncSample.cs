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
    private readonly TLinkAddress RubCurrency;
    public static AddressToRawNumberConverter<ulong> AddressToNumberConverter;

    public AsyncService(ILogger<AsyncService> logger, InvestApiClient investApi, IHostApplicationLifetime lifetime)
    {
        LinksGqlAdapter linksGqlAdapter = new(new GraphQLHttpClient("", new NewtonsoftJsonSerializer()), new LinksConstants<TLinkAddress>());
        _logger = logger;
        _investApi = investApi;
        _lifetime = lifetime;
        TLinkAddress zero = default;
        TLinkAddress one = Arithmetic.Increment(zero);
        var type = one;
        var meaningRoot = linksGqlAdapter.GetOrCreate(type, type);
        var unicodeSymbolMarker = linksGqlAdapter.GetOrCreate(meaningRoot, Arithmetic.Increment(ref type));
        var unicodeSequenceMarker = linksGqlAdapter.GetOrCreate(meaningRoot, Arithmetic.Increment(ref type));
        BalancedVariantConverter<TLinkAddress> balancedVariantConverter = new(linksGqlAdapter);
        RawNumberToAddressConverter<TLinkAddress> NumberToAddressConverter = new();
        AddressToNumberConverter = new();
        TargetMatcher<TLinkAddress> unicodeSymbolCriterionMatcher = new(linksGqlAdapter, unicodeSymbolMarker);
        TargetMatcher<TLinkAddress> unicodeSequenceCriterionMatcher = new(linksGqlAdapter, unicodeSequenceMarker);
        CharToUnicodeSymbolConverter<TLinkAddress> charToUnicodeSymbolConverter =
            new(linksGqlAdapter, AddressToNumberConverter, unicodeSymbolMarker);
        UnicodeSymbolToCharConverter<TLinkAddress> unicodeSymbolToCharConverter =
            new(linksGqlAdapter, NumberToAddressConverter, unicodeSymbolCriterionMatcher);
        StringToUnicodeSequenceConverter = new CachingConverterDecorator<string, TLinkAddress>(
            new StringToUnicodeSequenceConverter<TLinkAddress>(linksGqlAdapter, charToUnicodeSymbolConverter,
                balancedVariantConverter, unicodeSequenceMarker));
        RightSequenceWalker<TLinkAddress> sequenceWalker =
            new(linksGqlAdapter, new DefaultStack<TLinkAddress>(), unicodeSymbolCriterionMatcher.IsMatched);
        UnicodeSequenceToStringConverter = new CachingConverterDecorator<TLinkAddress, string>(
            new UnicodeSequenceToStringConverter<TLinkAddress>(linksGqlAdapter, unicodeSequenceCriterionMatcher, sequenceWalker,
                unicodeSymbolToCharConverter));
        InstrumentTicker = "TRUR";
        Asset = linksGqlAdapter.GetOrCreate(meaningRoot, Arithmetic.Increment(ref type));
        Balance = linksGqlAdapter.GetOrCreate(meaningRoot, Arithmetic.Increment(ref type));
        Etf = Arithmetic.Increment(ref type);
        if (!linksGqlAdapter.Exists(Etf))
        {
            linksGqlAdapter.Create();
            linksGqlAdapter.Update(Etf, Asset, Etf);
        }
        Currency = Arithmetic.Increment(ref type);
        if (!linksGqlAdapter.Exists(Currency))
        {
            linksGqlAdapter.Create();
            linksGqlAdapter.Update(Currency, Currency, Currency);
        }
        RubCurrency = Arithmetic.Increment(ref type);
        if (!linksGqlAdapter.Exists(RubCurrency))
        {
            linksGqlAdapter.Create();
            linksGqlAdapter.Update(RubCurrency, Currency, RubCurrency);
        }
        Amount = Arithmetic.Increment(ref type);
        if (!linksGqlAdapter.Exists(Amount))
        {
            linksGqlAdapter.Create();
            linksGqlAdapter.Update(Amount, Amount, Amount);
        }
        var account = _investApi.Users.GetAccounts().Accounts[0];
        var withdrawLimitsResponse = _investApi.Operations.GetWithdrawLimits(new WithdrawLimitsRequest() { AccountId = account.Id });
        _rubWithdrawLimit = withdrawLimitsResponse.Money.First(moneyValue => moneyValue.Currency == "rub");
        var etfs = _investApi.Instruments.Etfs();
        var instrument = etfs.Instruments.First(etf => etf.Ticker == InstrumentTicker);
        var instrumentTickerLink = StringToUnicodeSequenceConverter.Convert(InstrumentTicker);
        Asset = LinksGqlAdapter.GetOrCreate(Asset, instrumentTickerLink);
        _logger.LogInformation();
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
            if (!EqualityComparer.Equals(currency, RubCurrency))
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
        LinksGqlAdapter.Create(RubCurrency)
        LinksGqlAdapter.GetOrCreate(instrumentMarker, );
        var marketDataStream = _investApi.MarketDataStream.MarketDataStream();
        await marketDataStream.RequestStream.WriteAsync(new MarketDataRequest()
            {
                SubscribeInfoRequest = new SubscribeInfoRequest()
                {
                    Instruments = { new InfoInstrument() { Figi = instrument.Figi } },
                    SubscriptionAction = SubscriptionAction.Subscribe
                },
                SubscribeOrderBookRequest = new SubscribeOrderBookRequest()
                {
                    Instruments = { new OrderBookInstrument() { Figi = instrument.Figi, Depth = 1 } },
                    SubscriptionAction = SubscriptionAction.Subscribe
                },
                SubscribeTradesRequest = new SubscribeTradesRequest()
                {
                    Instruments = { new TradeInstrument(){Figi = instrument.Figi} },
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
                TradeAssets(asset, account.Id, orderBook, instrument.Figi);
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
        await ;
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
