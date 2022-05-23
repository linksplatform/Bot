using System.Net.NetworkInformation;
using Google.Protobuf.WellKnownTypes;
using GraphQL.Client.Serializer.Newtonsoft;
using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Collections.Stacks;
using Platform.Converters;
using Platform.Data;
using Platform.Data.Doublets;
using Platform.Data.Doublets.CriterionMatchers;
using Platform.Data.Doublets.Memory.United.Generic;
using Platform.Data.Doublets.Numbers.Rational;
using Platform.Data.Doublets.Numbers.Raw;
using Platform.Data.Doublets.Sequences.Converters;
using Platform.Data.Doublets.Sequences.Walkers;
using Platform.Data.Doublets.Unicode;
using Platform.Data.Numbers.Raw;
using Platform.Memory;
using Platform.Numbers;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;
using TinkoffOperationType = Tinkoff.InvestApi.V1.OperationType;
using TLinkAddress = System.UInt64;


namespace TraderBot;

public class AsyncService : BackgroundService
{
    private readonly InvestApiClient _investApi;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<AsyncService> _logger;
    private MoneyValue? _rubWithdrawLimit;
    public readonly int Quantity = 1;
    public readonly TLinkAddress EtfType;
    public readonly ILinks<TLinkAddress> Storage;
    public CachingConverterDecorator<string, ulong> StringToUnicodeSequenceConverter;
    public CachingConverterDecorator<ulong, string> UnicodeSequenceToStringConverter;
    private readonly TLinkAddress AssetType;
    public readonly string EtfTicker;
    private readonly TLinkAddress BalanceType;
    public readonly static EqualityComparer<ulong> EqualityComparer = EqualityComparer<ulong>.Default;
    private readonly TLinkAddress OperationCurrencyFieldType;
    private readonly TLinkAddress AmountType;
    private readonly TLinkAddress RubType;
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
    public readonly Account? Account;
    private ulong OperationType;
    public readonly ulong OperationFieldType;
    public ulong IdOperationFieldType;
    public ulong ParentOperationIdOperationFieldType;
    public ulong PaymentOperationFieldType;
    public ulong PriceOperationFieldType;
    private ulong StateOperationFieldType;
    private ulong QuantityOperationFieldType;
    public ulong QuantityRestOperationFieldType;
    public ulong FigiOperationFieldType;
    public ulong InstrumentTypeOperationFieldType;
    public readonly ulong DateOperationFieldType;
    public readonly ulong TypeAsStringOperationFieldType;
    public readonly ulong TypeAsEnumOperationFieldType;
    public readonly ulong TradesOperationFieldType;
    public ulong SequenceType;

    public AsyncService(ILogger<AsyncService> logger, InvestApiClient investApi, IHostApplicationLifetime lifetime)
    {
        HeapResizableDirectMemory memory = new();
        UnitedMemoryLinks<TLinkAddress> storage = new (memory);
        SynchronizedLinks<TLinkAddress> synchronizedStorage = new(storage);
        Storage = synchronizedStorage;
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
        SequenceType = GetOrCreateType(Type, nameof(SequenceType));
        OperationType = GetOrCreateType(Type, nameof(OperationType));
        OperationFieldType = GetOrCreateType(OperationType, nameof(OperationFieldType));
        IdOperationFieldType = GetOrCreateType(OperationFieldType, nameof(IdOperationFieldType));
        ParentOperationIdOperationFieldType = GetOrCreateType(OperationFieldType, nameof(ParentOperationIdOperationFieldType));
        OperationCurrencyFieldType = GetOrCreateType(OperationFieldType, nameof(OperationCurrencyFieldType));
        PaymentOperationFieldType = GetOrCreateType(OperationFieldType, nameof(PaymentOperationFieldType));
        PriceOperationFieldType = GetOrCreateType(OperationFieldType, nameof(PriceOperationFieldType));
        StateOperationFieldType = GetOrCreateType(OperationFieldType, nameof(StateOperationFieldType));
        QuantityOperationFieldType = GetOrCreateType(OperationFieldType, nameof(QuantityOperationFieldType));
        QuantityRestOperationFieldType = GetOrCreateType(OperationFieldType, nameof(QuantityRestOperationFieldType));
        FigiOperationFieldType = GetOrCreateType(OperationFieldType, nameof(FigiOperationFieldType));
        InstrumentTypeOperationFieldType = GetOrCreateType(OperationFieldType, nameof(InstrumentTypeOperationFieldType));
        DateOperationFieldType = GetOrCreateType(OperationFieldType, nameof(DateOperationFieldType));
        TypeAsStringOperationFieldType = GetOrCreateType(OperationFieldType, nameof(TypeAsStringOperationFieldType));
        TypeAsEnumOperationFieldType = GetOrCreateType(OperationFieldType, nameof(TypeAsEnumOperationFieldType));
        TradesOperationFieldType = GetOrCreateType(OperationFieldType, nameof(TradesOperationFieldType));
        AssetType = GetOrCreateType(Type, nameof(AssetType));
        BalanceType = GetOrCreateType(Type, nameof(BalanceType));
        EtfType = GetOrCreateType(AssetType, nameof(EtfType));
        OperationCurrencyFieldType = GetOrCreateType(AssetType, nameof(OperationCurrencyFieldType));
        RubType = GetOrCreateType(OperationCurrencyFieldType, nameof(RubType));
        AmountType = GetOrCreateType(Type, nameof(AmountType));
        Account = _investApi.Users.GetAccounts().Accounts[0];


        var rubBalanceMoneyValue = investApi.Operations.GetPositionsAsync(new PositionsRequest() { AccountId = Account.Id }).ResponseAsync.Result.Money.First(moneyValue => moneyValue.Currency == "rub");
        RubBalance = new Quotation(){Nano = rubBalanceMoneyValue.Nano, Units = rubBalanceMoneyValue.Units};
        var amountAddress = Storage.GetOrCreate(AmountType, DecimalToRationalConverter.Convert(RubBalance));
        var rubAmountAddress = Storage.GetOrCreate(RubType, amountAddress);
        var runBalanceAddress = Storage.GetOrCreate(BalanceType, rubAmountAddress);
        _logger.LogInformation($"Rub amount: {RubBalance}");
        var etfs = _investApi.Instruments.Etfs();
        EtfTicker = "TRUR";
        Instrument = etfs.Instruments.First(etf => etf.Ticker == EtfTicker);
        // var InstrumentTickerLink = StringToUnicodeSequenceConverter.Convert(InstrumentTicker);
        foreach (var portfolioPosition in investApi.Operations.GetPortfolio(new PortfolioRequest(){AccountId = Account.Id}).Positions)
        {
            if (portfolioPosition.Figi != Instrument.Figi)
            {
                continue;
            }
            InstrumentQuantity = portfolioPosition.Quantity;
            amountAddress = Storage.GetOrCreate(AmountType, DecimalToRationalConverter.Convert(InstrumentQuantity));
            var etfAmountAddress = Storage.GetOrCreate(EtfType, amountAddress);
            var etfBalanceAddress = Storage.GetOrCreate(BalanceType, etfAmountAddress);
            _logger.LogInformation($"[{portfolioPosition.Figi} {Instrument.Ticker}] quantity: {portfolioPosition.Quantity}");
        }
        // var brokerReportGenerateResponseResult = _investApi.Operations.GetBrokerReportAsync(new BrokerReportRequest()
        //     {
        //         GenerateBrokerReportRequest = new GenerateBrokerReportRequest()
        //         {
        //             From = Timestamp.FromDateTime(DateTime.UtcNow.AddYears(-2)), To = Timestamp.FromDateTime(DateTime.UtcNow), AccountId = account.Id
        //         }
        //     })
        //     .ResponseAsync.Result.GenerateBrokerReportResponse;
        // var brokerReportTaskId = brokerReportGenerateResponseResult.TaskId;
        // var brokerReportResponseResult = investApi.Operations.GetBrokerReportAsync(new BrokerReportRequest()
        //     {
        //         GetBrokerReportRequest = new GetBrokerReportRequest()
        //         {
        //             TaskId = brokerReportTaskId
        //         }
        //     })
        //     .ResponseAsync.Result.GetBrokerReportResponse;

        // List<Operation> buyInstrumentOperations = new List<Operation>();
        // long totalSoldQuantity = 0;
        // var operations = _investApi.Operations.GetOperations(new OperationsRequest()
        //     {
        //         Figi = Instrument.Figi,
        //         AccountId = account.Id,
        //         State = OperationState.Executed,
        //         From = Timestamp.FromDateTime(new DateTime(2022, 2, 12).ToUniversalTime()),
        //         // To = Timestamp.FromDateTime(new DateTime(2022, 2, 15).ToUniversalTime()),
        //         // From = Timestamp.FromDateTime(DateTime.UtcNow.AddMonths(-30)),
        //         To = Timestamp.FromDateTime(DateTime.UtcNow.AddMonths(1))
        //     })
        //     .Operations;
        // Console.WriteLine($"{operations.First().Date}");
        // Console.WriteLine($"{operations.Last().Date}");
        // foreach (var operation in operations)
        // {
        //     long quantity = operation.Trades.Count == 0 ? operation.Quantity : operation.Trades.Sum(trade => trade.Quantity);
        //     if (operation.OperationType == OperationType.Buy)
        //     {
        //         buyInstrumentOperations.Add(new Operation()
        //         {
        //             Price = new Quotation() {Nano = operation.Price.Nano, Units = operation.Price.Units},
        //             Quantity = quantity,
        //             Date = operation.Date,
        //             OperationType = operation.OperationType
        //         });
        //     }
        //     else if (operation.OperationType == OperationType.Sell)
        //     {
        //         totalSoldQuantity += quantity;
        //     }
        // }
        //
        // buyInstrumentOperations.Sort((operation, operation1) => ((decimal)operation.Price).CompareTo((decimal)operation1.Price));
        // for (var i = 0; i < buyInstrumentOperations.Count; i++)
        // {
        //     if (totalSoldQuantity == 0)
        //     {
        //         break;
        //     }
        //     var buyInstrumentOperation = buyInstrumentOperations[i];
        //     if (totalSoldQuantity < buyInstrumentOperation.Quantity)
        //     {
        //         buyInstrumentOperation.Quantity -= totalSoldQuantity;
        //         totalSoldQuantity = 0;
        //         continue;
        //     }
        //     totalSoldQuantity -= buyInstrumentOperation.Quantity;
        //     buyInstrumentOperations.RemoveAt(i);
        //     --i;
        // }
        // Console.WriteLine(buyInstrumentOperations);
        // var a  = buyInstrumentOperations.GroupBy(operation => operation.Price).ToList();
        // Console.WriteLine(buyInstrumentOperations.Sum(operation => operation.Quantity));
        // Console.WriteLine();

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
        if (!EqualityComparer.Equals(amountType, AmountType))
        {
            return null;
        }
        var amountValueAddress = Storage.GetTarget(amountAddress);
        return RationalToDecimalConverter.Convert(amountValueAddress);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var buyInstrumentOperations = GetBuyInstrumentOperationsOrNull();
        var buyInstrumentOperationsGroupedByPrice  = buyInstrumentOperations.GroupBy(operation => operation.Price).ToList();
        var marketDataStream = _investApi.MarketDataStream.MarketDataStream();
        await marketDataStream.RequestStream.WriteAsync(new MarketDataRequest()
            {
                // SubscribeInfoRequest = new SubscribeInfoRequest()
                // {
                //     Instruments = { new InfoInstrument() { Figi = Instrument.Figi } },
                //     SubscriptionAction = SubscriptionAction.Subscribe
                // },
                SubscribeOrderBookRequest = new SubscribeOrderBookRequest()
                {
                    Instruments = { new OrderBookInstrument() { Figi = Instrument.Figi, Depth = 1 } },
                    SubscriptionAction = SubscriptionAction.Subscribe
                },
                // SubscribeTradesRequest = new SubscribeTradesRequest()
                // {
                //     Instruments = { new TradeInstrument(){Figi = Instrument.Figi} },
                //     SubscriptionAction = SubscriptionAction.Subscribe
                // }
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
                
                _logger.LogInformation($"Asks[0]: {orderBook.Asks[0].Price}");
                _logger.LogInformation($"Asks[0].Nano/1000000000: {(decimal)orderBook.Asks[0].Price.Nano / 1000000000m}");

                var bestAskPrice = orderBook.Asks[0].Price;
                Decimal bestAsk = QuotationToDecimal(bestAskPrice);
                
                _logger.LogInformation($"bestAsk: {bestAsk}");
                _logger.LogInformation($"Bids[0]: {orderBook.Bids[0].Price}");
                
                foreach (var group in buyInstrumentOperationsGroupedByPrice)
                {
                    decimal groupPrice = MoneyValueToDecimal(group.Key);
                    _logger.LogInformation($"groupPrice: {groupPrice}");

                    decimal targetSellPriceCandidate = groupPrice + 0.01m;
                    _logger.LogInformation($"targetSellPriceCandidate: {targetSellPriceCandidate}");

                    decimal targetSellPrice = System.Math.Max(targetSellPriceCandidate, bestAsk);
                    _logger.LogInformation($"targetSellPrice: {targetSellPrice}");

                    long amount = group.Sum(o => o.Trades.Sum(t => t.Quantity));
                    _logger.LogInformation($"amount: {amount}");
                    
                    PlaceSellOrder(amount, targetSellPrice);
                    
                    // foreach (var operation in buyInstrumentOperation)
                    // {
                    //     Asset asset = new()
                    //     {
                    //         Amount = operation.Quantity,
                    //         Price = operation.Price
                    //     };
                    //     TradeAssets(asset, AccountType.Id, orderBook, Instrument.Figi);
                    // }
                }
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
        }
        _lifetime.StopApplication();
    }

    private static decimal MoneyValueToDecimal(MoneyValue value) => value.Units + value.Nano / 1000000000m;
    
    private static decimal QuotationToDecimal(Quotation value) => value.Units + value.Nano / 1000000000m;

    private static Quotation DecimalToQuatation(decimal value)
    {
        long units = (long) System.Math.Truncate(value);
        int nano = (int) System.Math.Truncate((value - units) * 1000000000m);
        return new Quotation() { Units = units, Nano = nano };
    } 

    private List<Operation>? GetBuyInstrumentOperationsOrNull()
    {
        List<Operation> buyInstrumentOperations = new ();
        // var @continue = Storage.Constants.Continue;
        // var any = Storage.Constants.Any;
        // TLinkAddress operationFieldsSequenceLinkAddress = default;
        //  Storage.Each(new Link<TLinkAddress>(any, OperationType, any), linkAddress =>
        // {
        //     var sequence = Storage.GetTarget(linkAddress);
        //     var sequenceType = Storage.GetSource(sequence);
        //     if (!EqualityComparer.Equals(sequenceType, SequenceType))
        //     {
        //         return @continue;
        //     }
        //     operationFieldsSequenceLinkAddress = sequence;
        //     return Storage.Constants.Break;
        // });
        //  if (EqualityComparer.Equals(operationFieldsSequenceLinkAddress, default))
        //  {
        //      return default;
        //  }
        // RightSequenceWalker<TLinkAddress> rightSequenceWalker = new(Storage, new DefaultStack<TLinkAddress>(), linkAddress =>
        // {
        //     var operationFieldTypeSubtype = Storage.GetSource(linkAddress);
        //     var operationFieldType = Storage.GetSource(operationFieldTypeSubtype);
        //     return EqualityComparer.Equals(operationFieldType, OperationFieldType);
        // });
        // var operationFieldLinkAddresses = rightSequenceWalker.Walk(operationFieldsSequenceLinkAddress);
        // foreach (var operationFieldLinkAddress in operationFieldLinkAddresses)
        // {
        //     Tinkoff.InvestApi.V1.Operation operation = new();
        //     if (EqualityComparer.Equals(operationFieldLinkAddress, IdOperationFieldType))
        //     {
        //         var idLink = Storage.GetTarget(operationFieldLinkAddress);
        //         operation.Id = UnicodeSequenceToStringConverter.Convert(idLink);
        //     }
        // }
        long totalSoldQuantity = 0;
        var operations = _investApi.Operations.GetOperations(new OperationsRequest()
            {
                Figi = Instrument.Figi,
                AccountId = Account.Id,
                State = OperationState.Executed,
                From = Timestamp.FromDateTime(new DateTime(2022, 2, 12).ToUniversalTime()),
                // To = Timestamp.FromDateTime(new DateTime(2022, 2, 15).ToUniversalTime()),
                // From = Timestamp.FromDateTime(DateTime.UtcNow.AddMonths(-30)),
                To = Timestamp.FromDateTime(DateTime.UtcNow.AddMonths(1))
            })
            .Operations;
        Console.WriteLine($"{operations.First().Date}");
        Console.WriteLine($"{operations.Last().Date}");
        foreach (var operation in operations)
        {
            long quantity = operation.Trades.Count == 0 ? operation.Quantity : operation.Trades.Sum(trade => trade.Quantity);
            if (operation.OperationType == TinkoffOperationType.Buy)
            {
                buyInstrumentOperations.Add(operation);
            }
            else if (operation.OperationType == TinkoffOperationType.Sell)
            {
                totalSoldQuantity += quantity;
            }
        }
        buyInstrumentOperations.Sort((operation, operation1) => (operation.Date).CompareTo(operation1.Date));
        for (var i = 0; i < buyInstrumentOperations.Count; i++)
        {
            if (totalSoldQuantity == 0)
            {
                break;
            }
            var buyInstrumentOperation = buyInstrumentOperations[i];
            if (totalSoldQuantity < buyInstrumentOperation.Quantity)
            {
                buyInstrumentOperation.Quantity -= totalSoldQuantity;
                totalSoldQuantity = 0;
                continue;
            }
            totalSoldQuantity -= buyInstrumentOperation.Quantity;
            buyInstrumentOperations.RemoveAt(i);
            --i;
        }
        return buyInstrumentOperations;
    }

    private TLinkAddress GetOrCreateType(TLinkAddress baseType, string typeId)
    {
        return Storage.GetOrCreate(baseType, StringToUnicodeSequenceConverter.Convert(typeId));
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
            if (RubBalance < cheapestAskOrder.Price)
            {
                _logger.LogError("Not enough money to buy {Asset}", asset);
                return;
            }
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

    private async void PlaceSellOrder(long amount, decimal price)
    {
        PostOrderRequest sellOrderRequest = new()
        {
            Figi = Instrument.Figi,
            Quantity = amount,
            Price = DecimalToQuatation(price),
            Direction = OrderDirection.Sell,
            AccountId = Account.Id,
            OrderType = OrderType.Limit,
            OrderId = Guid.NewGuid().ToString()
        };
        _logger.LogInformation("Placing sell order {SellOrderRequest}", sellOrderRequest);

        var positions = await _investApi.Operations.GetPositionsAsync(new PositionsRequest() { AccountId = Account.Id }).ResponseAsync;
        var securityPosition = positions.Securities.SingleOrDefault(x => x.Figi == Instrument.Figi);
        if (securityPosition != null)
        {
            _logger.LogInformation("Security position {SecurityPosition}", securityPosition);
        
            if (securityPosition.Balance >= amount)
            {
                var sellOrderResponse = await _investApi.Orders.PostOrderAsync(sellOrderRequest).ResponseAsync;
                _logger.LogInformation($"Sell order placed: {sellOrderResponse}");
            }
            else
            {
                _logger.LogError($"Not enough amount to sell {amount} assets. Available amount: {securityPosition.Balance}");
            }
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

    // class Operation
    // {
    //     public Timestamp Date;
    //     public long Quantity;
    //     public Quotation Price;
    //     public OperationType OperationType;
    // }

}
