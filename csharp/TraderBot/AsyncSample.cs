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
    public readonly ILinks<TLinkAddress> Storage;
    public CachingConverterDecorator<string, ulong> StringToUnicodeSequenceConverter;
    public CachingConverterDecorator<ulong, string> UnicodeSequenceToStringConverter;
    private readonly TLinkAddress Asset;
    public readonly string EtfTicker;
    private readonly TLinkAddress Balance;
    public readonly static EqualityComparer<ulong> EqualityComparer = EqualityComparer<ulong>.Default;
    private readonly TLinkAddress Currency;
    private readonly TLinkAddress Amount;
    private readonly TLinkAddress Rub;
    public static AddressToRawNumberConverter<ulong> AddressToNumberConverter;
    public ulong Type;
    public Etf Instrument;
    public readonly RawNumberSequenceToBigIntegerConverter<TLinkAddress> RawNumberSequenceToBigIntegerConverter;
    public readonly BigIntegerToRawNumberSequenceConverter<TLinkAddress> BigIntegerToRawNumberSequenceConverter;
    public readonly TLinkAddress NegativeNumberMarker;
    private readonly DecimalToRationalConverter<TLinkAddress> DecimalToRationalConverter;
    private readonly RationalToDecimalConverter<TLinkAddress> RationalToDecimalConverter;
    public Quotation? InstrumentQuantity;
    public Quotation RubBalance;

    public AsyncService(ILinks<TLinkAddress> storage, ILogger<AsyncService> logger, InvestApiClient investApi, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _investApi = investApi;
        _lifetime = lifetime;
        Storage = storage;
        TLinkAddress zero = default;
        TLinkAddress one = Arithmetic.Increment(zero);
        var typeIndex = one;
        Type = Storage.GetOrCreate(typeIndex, typeIndex);
        var typeId = Storage.GetOrCreate(Type, Arithmetic.Increment(ref typeIndex));
        var meaningRoot = Storage.GetOrCreate(Type, Type);
        var unicodeSymbolMarker = Storage.GetOrCreate(meaningRoot, Arithmetic.Increment(ref Type));
        var unicodeSequenceMarker = Storage.GetOrCreate(meaningRoot, Arithmetic.Increment(ref Type));
        NegativeNumberMarker = Storage.GetOrCreate(meaningRoot, Arithmetic.Increment(ref Type));
        BalancedVariantConverter<TLinkAddress> balancedVariantConverter = new(Storage);
        RawNumberToAddressConverter<TLinkAddress> NumberToAddressConverter = new();
        AddressToNumberConverter = new();
        TargetMatcher<TLinkAddress> unicodeSymbolCriterionMatcher = new(Storage, unicodeSymbolMarker);
        TargetMatcher<TLinkAddress> unicodeSequenceCriterionMatcher = new(Storage, unicodeSequenceMarker);
        CharToUnicodeSymbolConverter<TLinkAddress> charToUnicodeSymbolConverter =
            new(Storage, AddressToNumberConverter, unicodeSymbolMarker);
        UnicodeSymbolToCharConverter<TLinkAddress> unicodeSymbolToCharConverter =
            new(Storage, NumberToAddressConverter, unicodeSymbolCriterionMatcher);
        StringToUnicodeSequenceConverter = new CachingConverterDecorator<string, TLinkAddress>(
            new StringToUnicodeSequenceConverter<TLinkAddress>(Storage, charToUnicodeSymbolConverter,
                balancedVariantConverter, unicodeSequenceMarker));
        RightSequenceWalker<TLinkAddress> sequenceWalker =
            new(Storage, new DefaultStack<TLinkAddress>(), unicodeSymbolCriterionMatcher.IsMatched);
        UnicodeSequenceToStringConverter = new CachingConverterDecorator<TLinkAddress, string>(
            new UnicodeSequenceToStringConverter<TLinkAddress>(Storage, unicodeSequenceCriterionMatcher, sequenceWalker,
                unicodeSymbolToCharConverter));
        BigIntegerToRawNumberSequenceConverter =
            new(Storage, AddressToNumberConverter, balancedVariantConverter, NegativeNumberMarker);
        RawNumberSequenceToBigIntegerConverter = new(Storage, NumberToAddressConverter, NegativeNumberMarker);
        DecimalToRationalConverter = new(Storage, BigIntegerToRawNumberSequenceConverter);
        RationalToDecimalConverter = new(Storage, RawNumberSequenceToBigIntegerConverter);
        Asset = GetOrCreateType(Type, nameof(Asset));
        Balance = GetOrCreateType(Type, nameof(Balance));
        Etf = GetOrCreateType(Asset, nameof(Etf));
        Currency = GetOrCreateType(Asset, nameof(Currency));
        Rub = GetOrCreateType(Currency, nameof(Rub));
        Amount = GetOrCreateType(Type, nameof(Amount));
        var account = _investApi.Users.GetAccounts().Accounts[0];
        var rubBalanceMoneyValue = investApi.Operations.GetPositionsAsync(new PositionsRequest() { AccountId = account.Id }).ResponseAsync.Result.Money.First(moneyValue => moneyValue.Currency == "rub");
        RubBalance = new Quotation(){Nano = rubBalanceMoneyValue.Nano, Units = rubBalanceMoneyValue.Units};
        var amountAddress = Storage.GetOrCreate(Amount, DecimalToRationalConverter.Convert(RubBalance));
        var rubAmountAddress = Storage.GetOrCreate(Rub, amountAddress);
        var runBalanceAddress = Storage.GetOrCreate(Balance, rubAmountAddress);
        _logger.LogInformation($"Rub amount: {RubBalance}");
        var etfs = _investApi.Instruments.Etfs();
        EtfTicker = "TRUR";
        Instrument = etfs.Instruments.First(etf => etf.Ticker == EtfTicker);
        // var InstrumentTickerLink = StringToUnicodeSequenceConverter.Convert(InstrumentTicker);
        foreach (var portfolioPosition in investApi.Operations.GetPortfolio(new PortfolioRequest(){AccountId = account.Id}).Positions)
        {
            if (portfolioPosition.Figi != Instrument.Figi)
            {
                continue;
            }
            InstrumentQuantity = portfolioPosition.Quantity;
            amountAddress = Storage.GetOrCreate(Amount, DecimalToRationalConverter.Convert(InstrumentQuantity));
            var etfAmountAddress = Storage.GetOrCreate(Etf, amountAddress);
            var etfBalanceAddress = Storage.GetOrCreate(Balance, etfAmountAddress);
            _logger.LogInformation($"[{portfolioPosition.Figi} {Instrument.Ticker}] quantity: {portfolioPosition.Quantity}");
        }
        // Storage.Each(new Link<TLinkAddress>(any, any, any), link =>
        // {
        //     var balance = Storage.GetSource(link);
        //     if (!EqualityComparer.Equals(balance, Balance))
        //     {
        //         return @continue;
        //     }
        //     var balanceValue = Storage.GetTarget(link);
        //     var balanceValueType = Storage.GetSource(balanceValue);
        //     if (EqualityComparer.Equals(balanceValueType, Rub))
        //     {
        //         var amountAddress = Storage.GetTarget(balanceValue);
        //         var amountValue = GetAmountValueOrDefault(amountAddress);
        //         if (!amountValue.HasValue)
        //         {
        //             return @continue;
        //         }
        //         rubBalance = amountValue;
        //         _logger.LogInformation($"Rub amount: {amountValue}");
        //     }
        //     else if (EqualityComparer.Equals(balanceValueType, Etf))
        //     {
        //         var amountAddress = Storage.GetTarget(balanceValue);
        //         var amountValue = GetAmountValueOrDefault(amountAddress);
        //         if (!amountValue.HasValue)
        //         {
        //             return @continue;
        //         }
        //         _logger.LogInformation($"{EtfTicker} amount: {amountValue}");
        //     }
        //     return @continue;
        // });
    }

    private decimal? GetAmountValueOrDefault(TLinkAddress amountAddress)
    {
        var amountType = Storage.GetSource(amountAddress);
        if (!EqualityComparer.Equals(amountType, Amount))
        {
            return null;
        }
        var amountValueAddress = Storage.GetTarget(amountAddress);
        return RationalToDecimalConverter.Convert(amountValueAddress);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
        await foreach (var data in marketDataStream.ResponseStream.ReadAllAsync(stoppingToken))
        {
            if (data.PayloadCase == MarketDataResponse.PayloadOneofCase.Orderbook)
            {
                var orderBook = data.Orderbook;
                _logger.LogInformation("Orderbook data received from stream: {OrderBook}", orderBook);
                TradeAssets(asset, account.Id, orderBook, Instrument.Figi);
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
        return Storage.GetOrCreate(baseType, StringToUnicodeSequenceConverter.Convert(typeId));
    }

    private async void TradeAssets(Asset? asset, string accountId, OrderBook marketOrderBook, string figi)
    {
        var cheapestBidOrder = marketOrderBook.Bids[0];
        var cheapestAskOrder = marketOrderBook.Asks[0];
        if (RubBalance < cheapestAskOrder.Price)
        {
            _logger.LogError("Not enough money to buy {Asset}", asset);
            return;
        }
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
