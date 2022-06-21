using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Principal;
using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;

namespace TraderBot;

public class TradingService : BackgroundService
{
    protected readonly InvestApiClient InvestApi;
    protected readonly ILogger<TradingService> Logger;
    protected readonly IHostApplicationLifetime Lifetime;
    protected readonly TradingSettings Settings;
    protected readonly Account CurrentAccount;
    protected readonly Etf CurrentInstrument;
    protected readonly decimal PriceStep;
    protected decimal CashBalance;
    protected DateTime LastOperationsCheckpoint;
    protected readonly ConcurrentDictionary<string, OrderState> ActiveBuyOrders;
    protected readonly ConcurrentDictionary<string, OrderState> ActiveSellOrders;
    protected readonly ConcurrentDictionary<decimal, long> LotsSets;
    protected readonly ConcurrentDictionary<string, decimal> ActiveSellOrderSourcePrice;

    public TradingService(ILogger<TradingService> logger, InvestApiClient investApi, IHostApplicationLifetime lifetime, TradingSettings settings)
    {
        Logger = logger;
        InvestApi = investApi;
        Lifetime = lifetime;
        Settings = settings;
        Logger.LogInformation($"ETF ticker: {Settings.EtfTicker}");
        Logger.LogInformation($"CashCurrency: {Settings.CashCurrency}");
        Logger.LogInformation($"AccountIndex: {Settings.AccountIndex}");
        Logger.LogInformation($"MinimumProfitSteps: {Settings.MinimumProfitSteps}");
        Logger.LogInformation($"MarketOrderBookDepth: {Settings.MarketOrderBookDepth}");
        Logger.LogInformation($"MinimumMarketOrderSizeToChangeBuyPrice: {Settings.MinimumMarketOrderSizeToChangeBuyPrice}");
        Logger.LogInformation($"MinimumMarketOrderSizeToChangeSellPrice: {Settings.MinimumMarketOrderSizeToChangeSellPrice}");
        Logger.LogInformation($"MinimumMarketOrderSizeToBuy: {Settings.MinimumMarketOrderSizeToBuy}");
        Logger.LogInformation($"MinimumMarketOrderSizeToSell: {Settings.MinimumMarketOrderSizeToSell}");
        Logger.LogInformation($"EarlySellOwnedLotsDelta: {Settings.EarlySellOwnedLotsDelta}");
        Logger.LogInformation($"EarlySellOwnedLotsMultiplier: {Settings.EarlySellOwnedLotsMultiplier}");
        Logger.LogInformation($"LoadOperationsFrom: {Settings.LoadOperationsFrom}");
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
        LotsSets = new ConcurrentDictionary<decimal, long>();
        ActiveSellOrderSourcePrice = new ConcurrentDictionary<string, decimal>();
        LastOperationsCheckpoint = Settings.LoadOperationsFrom;
    }

    protected async Task ReceiveTrades(CancellationToken cancellationToken)
    {
        var tradesStream = InvestApi.OrdersStream.TradesStream(new TradesStreamRequest
        {
            Accounts = { CurrentAccount.Id }
        });
        await foreach (var data in tradesStream.ResponseStream.ReadAllAsync(cancellationToken))
        {
            Logger.LogInformation($"Trade: {data}");
            if (data.PayloadCase == TradesStreamResponse.PayloadOneofCase.OrderTrades)
            {
                var orderTrades = data.OrderTrades;
                TryUpdateLots(orderTrades);
                TrySubtractTradesFromOrder(ActiveBuyOrders, orderTrades);
                TrySubtractTradesFromOrder(ActiveSellOrders, orderTrades);
            }
            else if (data.PayloadCase == TradesStreamResponse.PayloadOneofCase.Ping)
            {
                SyncActiveOrders();
                SyncLots();
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

    protected void SyncActiveOrders(bool forceReset = false)
    {
        if (forceReset)
        {
            ActiveBuyOrders.Clear();
            ActiveSellOrders.Clear();
            ActiveSellOrderSourcePrice.Clear();
        }
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
            ActiveSellOrderSourcePrice.TryRemove(orderId, out decimal sourcePrice);
        }
    }

    protected void SyncLots(bool forceReset = false)
    {
        if (forceReset)
        {
            LotsSets.Clear();
        }
        var openOperations = GetOpenOperations();
        // Logger.LogInformation($"Open operations count: {openOperations.Count}");
        var openOperationsGroupedByPrice = openOperations.GroupBy(operation => operation.Price).ToList();

        var deletedLotsSets = new List<decimal>();
        foreach (var lotsSet in LotsSets)
        {
            if (openOperationsGroupedByPrice.All(openOperation => openOperation.Key != lotsSet.Key))
            {
                deletedLotsSets.Add(lotsSet.Key);
            }
        }
        foreach (var group in openOperationsGroupedByPrice)
        {
            LotsSets.TryAdd(group.Key, group.Sum(o => o.Quantity));
        }
        foreach (var lotsSet in deletedLotsSets)
        {
            LotsSets.TryRemove(lotsSet, out long lot);
        }
    }

    protected void TrySubtractTradesFromOrder(ConcurrentDictionary<string, OrderState> orders, OrderTrades orderTrades)
    {
        Logger.LogInformation($"TrySubtractTradesFromOrder.orderTrades: {orderTrades}");
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
                ActiveSellOrderSourcePrice.TryRemove(orderTrades.OrderId, out decimal sourcePrice);
                Logger.LogInformation($"Active order removed: {activeOrder}");
            }
        }
    }

    protected void LogLots()
    {
        foreach (var lot in LotsSets)
        {
            Logger.LogInformation($"{lot.Value} lots with {lot.Key} price");
        }
    }
    
    protected void TryUpdateLots(OrderTrades orderTrades)
    {
        Logger.LogInformation($"TryUpdateLots.orderTrades: {orderTrades}");
        foreach (var trade in orderTrades.Trades)
        {
            Logger.LogInformation($"orderTrades.Direction: {orderTrades.Direction}");
            Logger.LogInformation($"trade.Price: {trade.Price}");
            Logger.LogInformation($"trade.Quantity: {trade.Quantity}");
            if (orderTrades.Direction == OrderDirection.Buy)
            {
                LotsSets.AddOrUpdate(trade.Price, trade.Quantity, (key, value) => {
                    Logger.LogInformation($"Previous value: {value}");
                    Logger.LogInformation($"New value: {value + trade.Quantity}");
                    return value + trade.Quantity;
                });
            }
            else if (orderTrades.Direction == OrderDirection.Sell)
            {
                Logger.LogInformation($"orderTrades.OrderId: {orderTrades.OrderId}");
                if (ActiveSellOrderSourcePrice.TryGetValue(orderTrades.OrderId, out decimal sourcePrice))
                {
                    // Logger.LogInformation($"LotsSets.Count before TryUpdateOrRemove: {LotsSets.Count}");
                    Logger.LogInformation($"sourcePrice: {sourcePrice}");
                    var result = LotsSets.TryUpdateOrRemove(sourcePrice, (key, value) => {
                        Logger.LogInformation($"Previous value: {value}");
                        Logger.LogInformation($"New value: {value - trade.Quantity}");
                        return value - trade.Quantity;
                    }, (key, value) => {
                        Logger.LogInformation($"Remove condition: {value <= 0}");
                        return value <= 0;
                    });
                    Logger.LogInformation($"TryUpdateOrRemove.result: {result}");
                    // Logger.LogInformation($"LotsSets.Count after TryUpdateOrRemove: {LotsSets.Count}");
                }
            }
        }
    }

    protected async Task SendOrdersLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await SendOrders(cancellationToken);
            }
            catch(Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    Logger.LogError(ex, "SendOrders exception.");
                    await Task.Delay(2500);
                    Refresh(forceReset: true);
                }
            }
        }
    }
    
    protected async Task ReceiveTradesLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ReceiveTrades(cancellationToken);
            }
            catch(Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    Logger.LogError(ex, "ReceiveTrades exception.");
                    await Task.Delay(2500);
                    Refresh(forceReset: true);
                }
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
                Instruments = {new OrderBookInstrument {Figi = CurrentInstrument.Figi, Depth = Settings.MarketOrderBookDepth }},
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
            // Logger.LogInformation($"data.PayloadCase: {data.PayloadCase}");
            if (data.PayloadCase == MarketDataResponse.PayloadOneofCase.SubscribeOrderBookResponse)
            {
                Logger.LogInformation($"data.SubscribeOrderBookResponse.OrderBook.Instruments.Count: {data.SubscribeOrderBookResponse.OrderBookSubscriptions}");
            }
            else if (data.PayloadCase == MarketDataResponse.PayloadOneofCase.Orderbook)
            {
                // Logger.LogInformation($"Order book: {data.Orderbook}");

                var orderBook = data.Orderbook;
                // Logger.LogInformation("Orderbook data received from stream: {OrderBook}", orderBook);

                var topBidOrder = orderBook.Bids.First();
                var topBidPrice = topBidOrder.Price;
                var topBid = QuotationToDecimal(topBidPrice);
                var bestBidOrder = orderBook.Bids.FirstOrDefault(x => x.Quantity > Settings.MinimumMarketOrderSizeToBuy);
                if (bestBidOrder == null)
                {
                    Logger.LogInformation($"No best bid order, skipping.");
                    continue;
                }
                var bestBidPrice = bestBidOrder.Price;
                var bestBid = QuotationToDecimal(bestBidPrice);
                var bestAskOrder = orderBook.Asks.FirstOrDefault(x => x.Quantity > Settings.MinimumMarketOrderSizeToSell);
                if (bestAskOrder == null)
                {
                    Logger.LogInformation($"No best ask order, skipping.");
                    continue;
                }
                var bestAskPrice = bestAskOrder.Price;
                var bestAsk = QuotationToDecimal(bestAskPrice);

                // Logger.LogInformation($"ask: {bestAsk}, bid: {bestBid}.");
                
                // Logger.LogInformation($"Time: {DateTime.Now}");
                // Logger.LogInformation($"ActiveBuyOrders.Count: {ActiveBuyOrders.Count}");
                // Logger.LogInformation($"ActiveSellOrders.Count: {ActiveSellOrders.Count}");

                if (ActiveBuyOrders.Count == 0 && ActiveSellOrders.Count == 0)
                {
                    var areOrdersPlaced = false;
                    Logger.LogInformation($"ask: {bestAsk}, bid: {bestBid}.");
                    // Process potential sell order
                    foreach (var lotsSet in LotsSets)
                    {
                        var lotsSetPrice = lotsSet.Key;
                        Logger.LogInformation($"lotsSetPrice: {lotsSetPrice}");
                        var lotsSetAmount = lotsSet.Value;
                        Logger.LogInformation($"lotsSetAmount: {lotsSetAmount}");
                        var minimumSellPrice = GetMinimumSellPrice(lotsSetPrice);
                        var targetSellPrice = GetTargetSellPrice(minimumSellPrice, bestAsk);
                        Logger.LogInformation($"sell activated");
                        var response = await PlaceSellOrder(lotsSetAmount, targetSellPrice);
                        ActiveSellOrderSourcePrice[response.OrderId] = lotsSetPrice;
                        areOrdersPlaced = true;
                    }
                    if (!areOrdersPlaced)
                    {
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
                            Logger.LogInformation($"buy activated");
                            var response = await PlaceBuyOrder(lots, bestBid);
                            areOrdersPlaced = true;
                        }
                    }
                    if (areOrdersPlaced)
                    {
                        SyncActiveOrders();
                    }
                }
                else if (ActiveBuyOrders.Count == 1)
                {
                    var activeBuyOrder = ActiveBuyOrders.Single().Value;
                    var initialOrderPrice = MoneyValueToDecimal(activeBuyOrder.InitialSecurityPrice);
                    if (initialOrderPrice != bestBid && bestBidOrder.Quantity > Settings.MinimumMarketOrderSizeToChangeBuyPrice)
                    {
                        Logger.LogInformation($"ask: {bestAsk}, bid: {bestBid}.");
                        Logger.LogInformation($"initial buy order price: {initialOrderPrice}");
                        Logger.LogInformation($"buy order price change activated");
                        // Cancel order
                        await CancelOrder(activeBuyOrder.OrderId);
                        // Place new order
                        var lotSize = CurrentInstrument.Lot;
                        var previousOrderTotalPrice = activeBuyOrder.LotsRequested * lotSize  * initialOrderPrice;
                        var newLotPrice = bestBid * lotSize;
                        if (previousOrderTotalPrice > newLotPrice)
                        {
                            var lots = (long) (previousOrderTotalPrice / newLotPrice);
                            var response = await PlaceBuyOrder(lots, bestBid);
                        }
                        SyncActiveOrders();
                        Logger.LogInformation($"buy order price change is complete");
                    }
                }
                else if (ActiveSellOrders.Count == 1)
                {
                    var activeSellOrder = ActiveSellOrders.Single().Value;
                    if (ActiveSellOrderSourcePrice.TryGetValue(activeSellOrder.OrderId, out var sourcePrice))
                    {
                        if (topBidPrice == sourcePrice && topBidOrder.Quantity < (Settings.EarlySellOwnedLotsDelta + activeSellOrder.LotsRequested * Settings.EarlySellOwnedLotsMultiplier))
                        {
                            Logger.LogInformation($"bestAsk: {bestAsk}, topBid: {topBid}, bestBid: {bestBid}.");
                            Logger.LogInformation($"topBidOrder.Quantity: {topBidOrder.Quantity}");
                            Logger.LogInformation($"EarlySellOwnedLotsDelta: {Settings.EarlySellOwnedLotsDelta}");
                            Logger.LogInformation($"EarlySellOwnedLotsMultiplier: {Settings.EarlySellOwnedLotsMultiplier}");
                            Logger.LogInformation($"LotsRequested: {activeSellOrder.LotsRequested}");
                            Logger.LogInformation($"Threshold: {(Settings.EarlySellOwnedLotsDelta + activeSellOrder.LotsRequested * Settings.EarlySellOwnedLotsMultiplier)}");
                            Logger.LogInformation($"initial sell order price: {sourcePrice}");
                            // Cancel order
                            await CancelOrder(activeSellOrder.OrderId);
                            // Place new order at top bid price
                            Logger.LogInformation($"early sell is activated");
                            var response = await PlaceSellOrder(activeSellOrder.LotsRequested, topBid);
                            SyncActiveOrders();
                            SyncLots();
                            Logger.LogInformation($"early sell is complete");
                        }
                        else
                        {
                            var initialOrderPrice = MoneyValueToDecimal(activeSellOrder.InitialSecurityPrice);
                            var minimumSellPrice = GetMinimumSellPrice(sourcePrice);
                            if (bestAsk >= minimumSellPrice && bestAsk != initialOrderPrice && bestAskOrder.Quantity > Settings.MinimumMarketOrderSizeToChangeSellPrice)
                            {
                                Logger.LogInformation($"ask: {bestAsk}, bid: {bestBid}.");
                                Logger.LogInformation($"initial sell order price: {initialOrderPrice}");
                                Logger.LogInformation($"initial sell order source price: {sourcePrice}");
                                Logger.LogInformation($"minimumSellPrice: {minimumSellPrice}");
                                Logger.LogInformation($"sell order price change activated");
                                // Cancel order
                                await CancelOrder(activeSellOrder.OrderId);
                                // Place new order
                                var targetSellPrice = GetTargetSellPrice(minimumSellPrice, bestAsk);
                                var response = await PlaceSellOrder(activeSellOrder.LotsRequested, targetSellPrice);
                                ActiveSellOrderSourcePrice[response.OrderId] = sourcePrice;
                                SyncActiveOrders();
                                Logger.LogInformation($"sell order price change is complete");
                            }
                        }
                    }
                }
            }
        }
    }

    private decimal GetMinimumSellPrice(decimal sourcePrice)
    {
        var minimumSellPrice = sourcePrice + Settings.MinimumProfitSteps * PriceStep;
        // Logger.LogInformation($"minimumSellPrice: {minimumSellPrice}");
        return minimumSellPrice;
    }
    
    private decimal GetTargetSellPrice(decimal minimumSellPrice, decimal bestAsk)
    {
        var targetSellPrice = Math.Max(minimumSellPrice, bestAsk);
        Logger.LogInformation($"targetSellPrice: {targetSellPrice}");
        return targetSellPrice;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Refresh(forceReset: true);
        var tasks = new []
        {
            ReceiveTradesLoop(cancellationToken),
            SendOrdersLoop(cancellationToken)
        };
        await Task.WhenAll(tasks);
    }

    protected void Refresh(bool forceReset = false)
    {
        SyncActiveOrders(forceReset);
        LogActiveOrders();
        SyncLots(forceReset);
        LogLots();
        if (forceReset)
        {
            if (LotsSets.Count == 1 && ActiveSellOrders.Count == 1)
            {
                ActiveSellOrderSourcePrice[ActiveSellOrders.Single().Value.OrderId] = LotsSets.Single().Key;
            }
        }
    }

    public static decimal MoneyValueToDecimal(MoneyValue value) => value.Units + value.Nano / 1000000000m;
    
    public static decimal QuotationToDecimal(Quotation value) => value.Units + value.Nano / 1000000000m;

    public static Quotation DecimalToQuotation(decimal value)
    {
        var units = (long) Math.Truncate(value);
        var nano = (int) Math.Truncate((value - units) * 1000000000m);
        return new Quotation { Units = units, Nano = nano };
    }

    private List<Operation> GetOpenOperations()
    {
        List<Operation> openOperations = new ();

        // DateTime from = DateTime.SpecifyKind(DateTime.Parse("2022-06-10T13:42:31.613200Z"), DateTimeKind.Utc).AddHours(-3).AddTicks(1);
        DateTime from = DateTime.SpecifyKind(LastOperationsCheckpoint, DateTimeKind.Utc).AddHours(-3);
        var operations = InvestApi.Operations.GetOperations(new OperationsRequest
        {
            AccountId = CurrentAccount.Id,
            State = OperationState.Executed,
            Figi = CurrentInstrument.Figi,
            From = Timestamp.FromDateTime(from), // CurrentAccount.OpenedDate,
            To = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(4))
        }).Operations;
        
        // TODO: Compare Timestamp.FromDateTime(from) and CurrentAccount.OpenedDate, (use max)
        
        // log operations
        foreach (var operation in operations)
        {
            Logger.LogInformation($"{operation.OperationType} operation with {operation.Quantity - operation.QuantityRest} lots at {operation.Date}.");
        }

        if (operations.Any() && operations.OrderBy(x => x.Date).First().OperationType == OperationType.Sell)
        {
            throw new InvalidOperationException("Sell operation is first in list. It will not possible to correctly identify open operations.");
        }

        var totalSoldQuantity = operations.Where(o => o.OperationType == OperationType.Sell).Sum(o => o.GetActualQuantity());
        Logger.LogInformation($"Total sell operations quantity {totalSoldQuantity}");
        var totalBoughtQuantity = operations.Where(o => o.OperationType == OperationType.Buy).Sum(o => o.GetActualQuantity());
        Logger.LogInformation($"Total buy operations quantity {totalBoughtQuantity}");

        if (totalSoldQuantity > 0 && totalSoldQuantity == totalBoughtQuantity)
        {
            var baseDate = operations.OrderBy(x => x.Date).Last().Date.ToDateTime().AddMilliseconds(1);
            LastOperationsCheckpoint = baseDate.AddHours(3);
            Logger.LogInformation($"New last operations checkpoint: {baseDate.ToString("o", System.Globalization.CultureInfo.InvariantCulture)}");
        }
        
        // long totalSoldQuantity = 0;
        foreach (var operation in operations)
        {
            if (operation.OperationType == OperationType.Buy)
            {
                openOperations.Add(operation);
            }
            // else if (operation.OperationType == OperationType.Sell)
            // {
            //     totalSoldQuantity += operation.GetActualQuantity();
            // }
        }
        
        // Logger.LogInformation($"totalSoldQuantity: \t{totalSoldQuantity}");
        //
        // Logger.LogInformation($"Total buy operations quantity after filter \t{openOperations.Sum(o => o.GetActualQuantity())}");
        //
        openOperations.Sort((operation, operation1) => (operation.Date).CompareTo(operation1.Date));
        for (var i = 0; i < openOperations.Count; i++)
        {
            if (totalSoldQuantity == 0)
            {
                break;
            }
            var openOperation = openOperations[i];
            var actualQuantity = openOperation.GetActualQuantity();
            if (totalSoldQuantity < actualQuantity)
            {
                Logger.LogInformation($"final totalSoldQuantity: \t{totalSoldQuantity}");
                Logger.LogInformation($"final actualQuantity: \t{actualQuantity}");
                openOperation.Quantity = actualQuantity - totalSoldQuantity;
                Logger.LogInformation($"openOperation.Quantity: \t{openOperation.Quantity}");
                totalSoldQuantity = 0;
                continue;
            }
            totalSoldQuantity -= actualQuantity;
            openOperations.RemoveAt(i);
            --i;
        }
        
        // log operations
        foreach (var openOperation in openOperations)
        {
            Logger.LogInformation($"Open operation \t{openOperation}");
        }
        return openOperations;
    }

    private async Task<PostOrderResponse> PlaceSellOrder(long amount, decimal price)
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
        // var positions = await InvestApi.Operations.GetPositionsAsync(new PositionsRequest { AccountId = CurrentAccount.Id }).ResponseAsync;
        // var securityPosition = positions.Securities.SingleOrDefault(x => x.Figi == CurrentInstrument.Figi);
        // if (securityPosition == null)
        // {
        //     throw new InvalidOperationException($"Position for {CurrentInstrument.Figi} not found.");
        // }
        // Logger.LogInformation("Security position {SecurityPosition}", securityPosition);
        // if (securityPosition.Balance < amount)
        // {
        //     throw new InvalidOperationException($"Not enough amount to sell {amount} assets. Available amount: {securityPosition.Balance}");
        // }
        var response = await InvestApi.Orders.PostOrderAsync(sellOrderRequest).ResponseAsync;
        Logger.LogInformation($"Sell order placed: {response}");
        return response;
    }

    private async Task<PostOrderResponse> PlaceBuyOrder(long amount, decimal price)
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
        // var total = amount * price;
        // if (CashBalance < total)
        // {
        //     throw new InvalidOperationException($"Not enough money to buy {CurrentInstrument.Figi} asset.");
        // }
        var response = await InvestApi.Orders.PostOrderAsync(buyOrderRequest).ResponseAsync;
        Logger.LogInformation($"Buy order placed: {response}");
        return response;
    }

    private async Task<CancelOrderResponse> CancelOrder(string orderId)
    {
        var response = await InvestApi.Orders.CancelOrderAsync(new CancelOrderRequest
        {
            AccountId = CurrentAccount.Id,
            OrderId = orderId,
        });
        Logger.LogInformation($"Order cancelled: {response}");
        return response;
    }
}
