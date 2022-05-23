using System.Net.NetworkInformation;
using Google.Protobuf.WellKnownTypes;
using GraphQL.Client.Serializer.Newtonsoft;
using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;
using TinkoffOperationType = Tinkoff.InvestApi.V1.OperationType;

namespace TraderBot;

public class AsyncService : BackgroundService
{
    private readonly InvestApiClient _investApi;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<AsyncService> _logger;
    private MoneyValue? _rubWithdrawLimit;
    public readonly int Quantity = 1;
    
    public readonly string EtfTicker;
    
    public Etf Instrument;

    public Quotation? InstrumentQuantity;
    public decimal RubBalance;
    public readonly Account? Account;

    public AsyncService(ILogger<AsyncService> logger, InvestApiClient investApi, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _investApi = investApi;
        _lifetime = lifetime;

        Account = _investApi.Users.GetAccounts().Accounts[0];

        var rubBalanceMoneyValue = investApi.Operations.GetPositionsAsync(new PositionsRequest() { AccountId = Account.Id }).ResponseAsync.Result.Money.First(moneyValue => moneyValue.Currency == "rub");
        RubBalance = MoneyValueToDecimal(rubBalanceMoneyValue);

        _logger.LogInformation($"Rub amount: {RubBalance}");
        var etfs = _investApi.Instruments.Etfs();
        EtfTicker = "TRUR";
        Instrument = etfs.Instruments.First(etf => etf.Ticker == EtfTicker);

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

                if (buyInstrumentOperationsGroupedByPrice.Any())
                {
                    var bestAskPrice = orderBook.Asks[0].Price;
                    Decimal bestAsk = QuotationToDecimal(bestAskPrice);
                    _logger.LogInformation($"bestAsk: {bestAsk}");
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
                    
                        await PlaceSellOrder(amount, targetSellPrice);
                    }
                    _lifetime.StopApplication();
                }
                
                // if (RubBalance > )
                // {
                //     
                // }
                
                _logger.LogInformation($"Bids[0]: {orderBook.Bids[0].Price}");
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

    private async Task PlaceSellOrder(long amount, decimal price)
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
}
