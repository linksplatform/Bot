using System.Collections.Concurrent;
using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;
using Platform.Collections;

namespace TraderBot;

public class TradingService : BackgroundService
{
    protected readonly InvestApiClient InvestApi;
    protected readonly ILogger<TradingService> Logger;
    protected readonly TradingSettings Settings;
    protected readonly Account CurrentAccount;
    protected readonly Etf CurrentInstrument;
    protected readonly decimal PriceStep;
    protected decimal CashBalance;
    protected readonly ConcurrentDictionary<string, OrderState> ActiveBuyOrders;
    protected readonly ConcurrentDictionary<string, OrderState> ActiveSellOrders;

    public TradingService(ILogger<TradingService> logger, InvestApiClient investApi, TradingSettings settings)
    {
        Logger = logger;
        InvestApi = investApi;
        Settings = settings;
        Logger.LogInformation($"ETF ticker: {Settings.EtfTicker}");
        Logger.LogInformation($"CashCurrency: {Settings.CashCurrency}");
        Logger.LogInformation($"AccountIndex: {Settings.AccountIndex}");
        Logger.LogInformation("Accounts:");
        var accounts = InvestApi.Users.GetAccounts().Accounts;
        for (int i = 0; i < accounts.Count; i++)
        {
            Logger.LogInformation($"[{i}]: {accounts[i]}");
        }
        CurrentAccount = accounts[Settings.AccountIndex];
        Logger.LogInformation($"CurrentAccount (with {Settings.AccountIndex} index): {CurrentAccount}");
        CurrentInstrument = InvestApi.Instruments.Etfs().Instruments.First(etf => etf.Ticker == Settings.EtfTicker);
        Logger.LogInformation($"CurrentInstrument: {CurrentInstrument}");
        PriceStep = QuotationToDecimal(CurrentInstrument.MinPriceIncrement);
        Logger.LogInformation($"PriceStep: {PriceStep}");
        ActiveBuyOrders = new ConcurrentDictionary<string, OrderState>();
        ActiveSellOrders = new ConcurrentDictionary<string, OrderState>();
    }

    protected async Task ReceiveTrades(CancellationToken cancellationToken)
    {
        var tradesStream = InvestApi.OrdersStream.TradesStream(new TradesStreamRequest()
        {
            Accounts = { CurrentAccount.Id }
        });
        await foreach (var data in tradesStream.ResponseStream.ReadAllAsync(cancellationToken))
        {
            Logger.LogInformation($"Trade: {data}");
            if (data.PayloadCase == TradesStreamResponse.PayloadOneofCase.OrderTrades)
            {
                var orderTrades = data.OrderTrades;
                TrySubtractTradesFromOrder(ActiveBuyOrders, orderTrades);
                TrySubtractTradesFromOrder(ActiveSellOrders, orderTrades);
            }
            else if (data.PayloadCase == TradesStreamResponse.PayloadOneofCase.Ping)
            {
                SyncActiveOrders();
            }
        }
    }

    protected void LogActiveOrders()
    {
        foreach (var order in ActiveBuyOrders)
        {
            Logger.LogInformation($"Active buy order: {order.Value}");
        }
        foreach (var order in ActiveSellOrders)
        {
            Logger.LogInformation($"Active sell order: {order.Value}");
        }
    }

    protected void SyncActiveOrders()
    {
        var orders = InvestApi.Orders.GetOrders(new GetOrdersRequest {AccountId = CurrentAccount.Id}).Orders;
        var deletedBuyOrders = new List<string>();
        foreach (var order in ActiveBuyOrders)
        {
            if (orders.All(o => o.OrderId != order.Key))
            {
                deletedBuyOrders.Add(order.Key);
            }
        }
        var deletedSellOrders = new List<string>();
        foreach (var order in ActiveSellOrders)
        {
            if (orders.All(o => o.OrderId != order.Key))
            {
                deletedSellOrders.Add(order.Key);
            }
        }
        foreach (var orderState in orders)
        {
            if (orderState.Figi == CurrentInstrument.Figi)
            {
                if (orderState.Direction == OrderDirection.Buy)
                {
                    ActiveBuyOrders.TryAdd(orderState.OrderId, orderState);
                }
                else if (orderState.Direction == OrderDirection.Sell)
                {
                    ActiveSellOrders.TryAdd(orderState.OrderId, orderState);
                }
            }
        }
        foreach (var orderId in deletedBuyOrders)
        {
            ActiveBuyOrders.TryRemove(orderId, out OrderState? orderState);
        }
        foreach (var orderId in deletedSellOrders)
        {
            ActiveSellOrders.TryRemove(orderId, out OrderState? orderState);
        }
    }

    protected void TrySubtractTradesFromOrder(ConcurrentDictionary<string, OrderState> orders, OrderTrades orderTrades)
    {
        Logger.LogInformation($"OrderTrades: {orderTrades}");
        if (orders.TryGetValue(orderTrades.OrderId, out var activeOrder))
        {
            foreach (var trade in orderTrades.Trades)
            {
                activeOrder.LotsRequested -= trade.Quantity;
            }
            Logger.LogInformation($"Active order: {activeOrder}");
            if (activeOrder.LotsRequested == 0)
            {
                orders.TryRemove(orderTrades.OrderId, out activeOrder);
                Logger.LogInformation($"Active order removed: {activeOrder}");
            }
        }
    }

    protected async Task SendOrders(CancellationToken cancellationToken)
    {
        var marketDataStream = InvestApi.MarketDataStream.MarketDataStream();
        await marketDataStream.RequestStream.WriteAsync(new MarketDataRequest
        {
            SubscribeOrderBookRequest = new SubscribeOrderBookRequest
            {
                Instruments = {new OrderBookInstrument() {Figi = CurrentInstrument.Figi, Depth = 1}},
                SubscriptionAction = SubscriptionAction.Subscribe
            },
        }).ContinueWith((task) =>
        {
            if (!task.IsCompletedSuccessfully)
            {
                throw new Exception("Error while subscribing to market data");
            }
            Logger.LogInformation("Subscribed to market data");
        }, cancellationToken);
        await foreach (var data in marketDataStream.ResponseStream.ReadAllAsync(cancellationToken))
        {
            if (data.PayloadCase == MarketDataResponse.PayloadOneofCase.Orderbook)
            {
                var orderBook = data.Orderbook;
                // Logger.LogInformation("Orderbook data received from stream: {OrderBook}", orderBook);
                
                var bestAskPrice = orderBook.Asks[0].Price;
                var bestAsk = QuotationToDecimal(bestAskPrice);
                var bestBidPrice = orderBook.Bids[0].Price;
                var bestBid = QuotationToDecimal(bestBidPrice);

                if (ActiveBuyOrders.Count == 0 && ActiveSellOrders.Count == 0)
                {
                    Logger.LogInformation($"ask: {bestAsk}, bid: {bestBid}.");
                    var isOrderPlaced = false;
                    // Process potential sell order
                    var openOperations = GetOpenOperations();
                    Logger.LogInformation($"Open operations count: {openOperations.Count}");
                    var openOperationsGroupedByPrice = openOperations.GroupBy(operation => operation.Price).ToList();
                    if (openOperationsGroupedByPrice.Any())
                    {
                        foreach (var group in openOperationsGroupedByPrice)
                        {
                            var groupPrice = MoneyValueToDecimal(group.Key);
                            Logger.LogInformation($"groupPrice: {groupPrice}");
                            var targetSellPriceCandidate = groupPrice + PriceStep;
                            Logger.LogInformation($"targetSellPriceCandidate: {targetSellPriceCandidate}");
                            var targetSellPrice = System.Math.Max(targetSellPriceCandidate, bestAsk);
                            Logger.LogInformation($"targetSellPrice: {targetSellPrice}");
                            var amount = group.Sum(o => o.Trades.Sum(t => t.Quantity));
                            Logger.LogInformation($"amount: {amount}");
                            isOrderPlaced |= await TryPlaceSellOrder(amount, targetSellPrice);
                        }
                    }
                    // Process potential buy order
                    var rubBalanceMoneyValue = InvestApi.Operations
                        .GetPositionsAsync(new PositionsRequest {AccountId = CurrentAccount.Id}).ResponseAsync.Result.Money
                        .First(moneyValue => moneyValue.Currency == Settings.CashCurrency);
                    CashBalance = MoneyValueToDecimal(rubBalanceMoneyValue);
                    Logger.LogInformation($"Cash ({Settings.CashCurrency}) amount: {CashBalance}");
                    var lotSize = CurrentInstrument.Lot;
                    var lotPrice = bestBid * lotSize;
                    if (CashBalance > lotPrice)
                    {
                        var lots = (long) (CashBalance / lotPrice);
                        isOrderPlaced |= await TryPlaceBuyOrder(lots, bestBid);
                    }
                    if (isOrderPlaced)
                    {
                        SyncActiveOrders();
                    }
                } else if (ActiveBuyOrders.Count == 1) {
                    Logger.LogInformation($"ask: {bestAsk}, bid: {bestBid}.");
                    var activeBuyOrder = ActiveBuyOrders.Single().Value;
                    var initialOrderPrice = MoneyValueToDecimal(activeBuyOrder.InitialOrderPrice);
                    Logger.LogInformation($"initialOrderPrice: {initialOrderPrice}");
                    if (initialOrderPrice < bestBid)
                    {
                        // Cancel order
                        await InvestApi.Orders.CancelOrderAsync(new CancelOrderRequest
                        {
                            OrderId = activeBuyOrder.OrderId,
                            AccountId = CurrentAccount.Id
                        });
                        // Place new order
                        var rubBalanceMoneyValue = InvestApi.Operations
                            .GetPositionsAsync(new PositionsRequest {AccountId = CurrentAccount.Id}).ResponseAsync.Result.Money
                            .First(moneyValue => moneyValue.Currency == Settings.CashCurrency);
                        CashBalance = MoneyValueToDecimal(rubBalanceMoneyValue);
                        Logger.LogInformation($"Cash ({Settings.CashCurrency}) amount: {CashBalance}");
                        var lotSize = CurrentInstrument.Lot;
                        var lotPrice = bestBid * lotSize;
                        var isOrderPlaced = false;
                        if (CashBalance > lotPrice)
                        {
                            var lots = (long) (CashBalance / lotPrice);
                            isOrderPlaced |= await TryPlaceBuyOrder(lots, bestBid);
                        }
                        if (isOrderPlaced)
                        {
                            SyncActiveOrders();
                        }
                    }
                }
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        SyncActiveOrders();
        LogActiveOrders();
        var tasks = new []
        {
            ReceiveTrades(cancellationToken),
            SendOrders(cancellationToken)
        };
        await Task.WhenAll(tasks);
    }

    public static decimal MoneyValueToDecimal(MoneyValue value) => value.Units + value.Nano / 1000000000m;
    
    public static decimal QuotationToDecimal(Quotation value) => value.Units + value.Nano / 1000000000m;

    public static Quotation DecimalToQuotation(decimal value)
    {
        var units = (long) System.Math.Truncate(value);
        var nano = (int) System.Math.Truncate((value - units) * 1000000000m);
        return new Quotation() { Units = units, Nano = nano };
    }

    private List<Operation> GetOpenOperations()
    {
        List<Operation> openOperations = new ();
        long totalSoldQuantity = 0;
        var operations = InvestApi.Operations.GetOperations(new OperationsRequest
        {
            AccountId = CurrentAccount.Id,
            State = OperationState.Executed,
            Figi = CurrentInstrument.Figi,
            From = CurrentAccount.OpenedDate,
            To = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(3))
        }).Operations;
        foreach (var operation in operations)
        {
            long quantity = operation.Trades.Count == 0 ? operation.Quantity : operation.Trades.Sum(trade => trade.Quantity);
            if (operation.OperationType == OperationType.Buy)
            {
                openOperations.Add(operation);
            }
            else if (operation.OperationType == OperationType.Sell)
            {
                totalSoldQuantity += quantity;
            }
        }
        openOperations.Sort((operation, operation1) => (operation.Date).CompareTo(operation1.Date));
        for (var i = 0; i < openOperations.Count; i++)
        {
            if (totalSoldQuantity == 0)
            {
                break;
            }
            var buyInstrumentOperation = openOperations[i];
            if (totalSoldQuantity < buyInstrumentOperation.Quantity)
            {
                buyInstrumentOperation.Quantity -= totalSoldQuantity;
                totalSoldQuantity = 0;
                continue;
            }
            totalSoldQuantity -= buyInstrumentOperation.Quantity;
            openOperations.RemoveAt(i);
            --i;
        }
        return openOperations;
    }

    private async Task<bool> TryPlaceSellOrder(long amount, decimal price)
    {
        PostOrderRequest sellOrderRequest = new()
        {
            OrderId = Guid.NewGuid().ToString(),
            AccountId = CurrentAccount.Id,
            Direction = OrderDirection.Sell,
            OrderType = OrderType.Limit,
            Figi = CurrentInstrument.Figi,
            Quantity = amount,
            Price = DecimalToQuotation(price)
        };
        var positions = await InvestApi.Operations.GetPositionsAsync(new PositionsRequest { AccountId = CurrentAccount.Id }).ResponseAsync;
        var securityPosition = positions.Securities.SingleOrDefault(x => x.Figi == CurrentInstrument.Figi);
        if (securityPosition == null)
        {
            return false;
        }
        Logger.LogInformation("Security position {SecurityPosition}", securityPosition);
        if (securityPosition.Balance < amount)
        {
            Logger.LogError($"Not enough amount to sell {amount} assets. Available amount: {securityPosition.Balance}");
            return false;
        }
        var sellOrderResponse = await InvestApi.Orders.PostOrderAsync(sellOrderRequest).ResponseAsync;
        Logger.LogInformation($"Sell order placed: {sellOrderResponse}");
        return true;
    }

    private async Task<bool> TryPlaceBuyOrder(long amount, decimal price)
    {
        PostOrderRequest buyOrderRequest = new()
        {
            OrderId = Guid.NewGuid().ToString(),
            AccountId = CurrentAccount.Id,
            Direction = OrderDirection.Buy,
            OrderType = OrderType.Limit,
            Figi = CurrentInstrument.Figi,
            Quantity = amount,
            Price = DecimalToQuotation(price),
        };
        var total = amount * price;
        if (CashBalance < total)
        {
            Logger.LogError($"Not enough money to buy {CurrentInstrument.Figi} asset.");
            return false;
        }
        var buyOrderResponse = await InvestApi.Orders.PostOrderAsync(buyOrderRequest).ResponseAsync;
        Logger.LogInformation($"Buy order placed: {buyOrderResponse}");
        return true;
    }
}
