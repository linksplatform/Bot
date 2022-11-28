using System.Collections.Concurrent;
using System.Globalization;
using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;

namespace TraderBot;

using OperationsList = List<(OperationType Type, DateTime Date, long Quantity, decimal Price)>;

public class TradingService : BackgroundService
{
    protected const bool PreferLocalCashBalance = true;
    protected static readonly TimeSpan RecoveryInterval = TimeSpan.FromSeconds(20);
    protected static readonly TimeSpan FailedCancelOrderInterval = TimeSpan.FromSeconds(10);
    protected static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(10);
    protected static readonly TimeSpan SyncInterval = TimeSpan.FromSeconds(20);
    protected static readonly TimeSpan WaitOutputInterval = TimeSpan.FromSeconds(20);
    protected readonly InvestApiClient InvestApi;
    protected readonly ILogger<TradingService> Logger;
    protected readonly IHostApplicationLifetime Lifetime;
    protected readonly TradingSettings Settings;
    protected readonly Account CurrentAccount;
    protected readonly string Figi;
    protected readonly int LotSize;
    protected readonly decimal PriceStep;
    protected decimal CashBalanceFree;
    protected decimal CashBalanceLocked;
    protected DateTime LastOperationsCheckpoint;
    protected long LastRefreshTicks;
    protected long LastSyncTicks;
    protected long LastWaitOutputTicks;
    protected TimeSpan MinimumTimeToBuy;
    protected TimeSpan MaximumTimeToBuy;
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
        Logger.LogInformation($"Instrument: {settings.Instrument}");
        Logger.LogInformation($"Ticker: {settings.Ticker}");
        Logger.LogInformation($"CashCurrency: {settings.CashCurrency}");
        Logger.LogInformation($"AccountIndex: {settings.AccountIndex}");
        Logger.LogInformation($"MinimumProfitSteps: {settings.MinimumProfitSteps}");
        Logger.LogInformation($"MarketOrderBookDepth: {settings.MarketOrderBookDepth}");
        Logger.LogInformation($"MinimumMarketOrderSizeToChangeBuyPrice: {settings.MinimumMarketOrderSizeToChangeBuyPrice}");
        Logger.LogInformation($"MinimumMarketOrderSizeToChangeSellPrice: {settings.MinimumMarketOrderSizeToChangeSellPrice}");
        Logger.LogInformation($"MinimumMarketOrderSizeToBuy: {settings.MinimumMarketOrderSizeToBuy}");
        Logger.LogInformation($"MinimumMarketOrderSizeToSell: {settings.MinimumMarketOrderSizeToSell}");
        MinimumTimeToBuy = TimeSpan.Parse(settings.MinimumTimeToBuy ?? "00:00:00", CultureInfo.InvariantCulture);
        Logger.LogInformation($"MinimumTimeToBuy: {MinimumTimeToBuy}");
        MaximumTimeToBuy = TimeSpan.Parse(settings.MaximumTimeToBuy ?? "23:59:59", CultureInfo.InvariantCulture);
        Logger.LogInformation($"MaximumTimeToBuy: {MaximumTimeToBuy}");
        Logger.LogInformation($"EarlySellOwnedLotsDelta: {settings.EarlySellOwnedLotsDelta}");
        Logger.LogInformation($"EarlySellOwnedLotsMultiplier: {settings.EarlySellOwnedLotsMultiplier}");
        Logger.LogInformation($"LoadOperationsFrom: {settings.LoadOperationsFrom}");

        var currentTime = DateTime.UtcNow.TimeOfDay;
        Logger.LogInformation($"Current time: {currentTime}");

        var accounts = InvestApi.Users.GetAccounts().Accounts;
        Logger.LogInformation("Accounts:");
        for (int i = 0; i < accounts.Count; i++)
        {
            Logger.LogInformation($"[{i}]: {accounts[i]}");
        }
        if (settings.AccountIndex < 0 || settings.AccountIndex >= accounts.Count)
        {
            throw new ArgumentException($"Account index {settings.AccountIndex} is out of range. Please select a valid account index ({0}-{accounts.Count - 1}).");
        }
        CurrentAccount = accounts[settings.AccountIndex];
        Logger.LogInformation($"CurrentAccount (with {settings.AccountIndex} index): {CurrentAccount}");

        if (settings.Instrument == Instrument.Etf)
        {
            var currentInstrument = InvestApi.Instruments.Etfs().Instruments.First(etf => etf.Ticker == settings.Ticker);
            Logger.LogInformation($"CurrentInstrument: {currentInstrument}");
            Figi = currentInstrument.Figi;
            Logger.LogInformation($"Figi: {Figi}");
            PriceStep = QuotationToDecimal(currentInstrument.MinPriceIncrement);
            Logger.LogInformation($"PriceStep: {PriceStep}");
            LotSize = currentInstrument.Lot;
            Logger.LogInformation($"LotSize: {LotSize}");
        }
        else if (settings.Instrument == Instrument.Shares) 
        {
            var currentInstrument = InvestApi.Instruments.Shares().Instruments.First(etf => etf.Ticker == settings.Ticker);
            Logger.LogInformation($"CurrentInstrument: {currentInstrument}");
            Figi = currentInstrument.Figi;
            Logger.LogInformation($"Figi: {Figi}");
            PriceStep = QuotationToDecimal(currentInstrument.MinPriceIncrement);
            Logger.LogInformation($"PriceStep: {PriceStep}");
            LotSize = currentInstrument.Lot;
            Logger.LogInformation($"LotSize: {LotSize}");
        }
        else
        {
            throw new InvalidOperationException("Not supported instrument type.");
        }
        
        ActiveBuyOrders = new ConcurrentDictionary<string, OrderState>();
        ActiveSellOrders = new ConcurrentDictionary<string, OrderState>();
        LotsSets = new ConcurrentDictionary<decimal, long>();
        ActiveSellOrderSourcePrice = new ConcurrentDictionary<string, decimal>();
        LastOperationsCheckpoint = settings.LoadOperationsFrom;
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
                UpdateCashBalance(orderTrades);
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

    protected void UpdateCashBalance(OrderTrades orderTrades)
    {
        foreach (var trade in orderTrades.Trades)
        {
            var cashBalanceDelta = trade.Quantity * trade.Price;
            if(orderTrades.Direction == OrderDirection.Buy)
            {
                SetCashBalance(CashBalanceFree, CashBalanceLocked - cashBalanceDelta);
            }
            else if (orderTrades.Direction == OrderDirection.Sell)
            {
                SetCashBalance(CashBalanceFree + cashBalanceDelta, CashBalanceLocked);
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
            if (orderState.Figi == Figi)
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
        if (ActiveBuyOrders.Count == 0 && CashBalanceLocked > 0)
        {
            Logger.LogInformation("No active buy orders, locked cash balance will be reset.");
            SetCashBalance(CashBalanceFree + CashBalanceLocked, 0);
        }
        if (LotsSets.Count == 1 && ActiveSellOrders.Count == 1)
        {
            ActiveSellOrderSourcePrice[ActiveSellOrders.Single().Value.OrderId] = LotsSets.Single().Key;
        }
    }

    protected void SyncLots(bool forceReset = false)
    {
        if (forceReset)
        {
            LotsSets.Clear();
        }
        // Get positions
        var securitiesPositions = InvestApi.Operations.GetPositions(new PositionsRequest { AccountId = CurrentAccount.Id }).Securities;
        var currentInstrumentPosition = securitiesPositions.Where(p => p.Figi == Figi).FirstOrDefault();
        if (currentInstrumentPosition == null)
        {
            Logger.LogInformation($"Current instrument not found in positions.");
        }
        else
        {
            Logger.LogInformation($"Current instrument found in positions: {currentInstrumentPosition}");
        }
        // Get portfolio
        var portfolio = InvestApi.Operations.GetPortfolio(new PortfolioRequest { AccountId = CurrentAccount.Id }).Positions;
        var currentInstrumentPortfolio = portfolio.Where(p => p.Figi == Figi).FirstOrDefault();
        if (currentInstrumentPortfolio == null)
        {
            Logger.LogInformation($"Current instrument not found in portfolio.");
        }
        else
        {
            Logger.LogInformation($"Current instrument found in portfolio: {currentInstrumentPortfolio}");
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
                await Refresh(forceReset: true);
                await SendOrders(cancellationToken);
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    Logger.LogError(ex, "SendOrders exception.");
                    await Task.Delay(RecoveryInterval);
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
                await Refresh(forceReset: true);
                await ReceiveTrades(cancellationToken);
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    Logger.LogError(ex, "ReceiveTrades exception.");
                    await Task.Delay(RecoveryInterval);
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
                Instruments = {new OrderBookInstrument {Figi = Figi, Depth = Settings.MarketOrderBookDepth }},
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

                var topBidOrder = orderBook.Bids.FirstOrDefault();
                if (topBidOrder == null)
                {
                    Logger.LogInformation("No top bid order, skipping.");
                    continue;
                }
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

                // Logger.LogInformation($"bid: {bestBid}, ask: {bestAsk}.");
                
                // Logger.LogInformation($"Time: {DateTime.Now}");
                // Logger.LogInformation($"ActiveBuyOrders.Count: {ActiveBuyOrders.Count}");
                // Logger.LogInformation($"ActiveSellOrders.Count: {ActiveSellOrders.Count}");

                if (ActiveBuyOrders.Count == 0 && ActiveSellOrders.Count == 0)
                {
                    var areOrdersPlaced = false;
                    // Process potential sell order
                    if (LotsSets.Count > 0)
                    {
                        Logger.LogInformation($"sell activated");
                        Logger.LogInformation($"bid: {bestBid}, ask: {bestAsk}.");
                        var maxPrice = LotsSets.Keys.Max();
                        Logger.LogInformation($"maxPrice: {maxPrice}");
                        var totalAmount = LotsSets.Values.Sum();
                        Logger.LogInformation($"totalAmount: {totalAmount}");
                        var minimumSellPrice = GetMinimumSellPrice(maxPrice);
                        var targetSellPrice = GetTargetSellPrice(minimumSellPrice, bestAsk);
                        var marketLotsAtTargetPrice = orderBook.Asks.FirstOrDefault(o => o.Price == targetSellPrice)?.Quantity ?? 0;
                        Logger.LogInformation($"marketLotsAtTargetPrice: {marketLotsAtTargetPrice}");
                        var response = await PlaceSellOrder(totalAmount, targetSellPrice);
                        ActiveSellOrderSourcePrice[response.OrderId] = maxPrice;
                        Logger.LogInformation($"sell complete");
                        areOrdersPlaced = true;
                    }
                    if (!areOrdersPlaced)
                    {
                        if (IsTimeToBuy())
                        {
                            // Process potential buy order
                            var (cashBalance, _) = await GetCashBalance();
                            var lotPrice = bestBid * LotSize;
                            if (cashBalance > lotPrice)
                            {
                                Logger.LogInformation($"buy activated");
                                Logger.LogInformation($"bid: {bestBid}, ask: {bestAsk}.");
                                var lots = (long)(cashBalance / lotPrice);
                                var marketLotsAtTargetPrice = orderBook.Bids.FirstOrDefault(o => o.Price == bestBid)?.Quantity ?? 0;
                                Logger.LogInformation($"marketLotsAtTargetPrice: {marketLotsAtTargetPrice}");
                                var response = await PlaceBuyOrder(lots, bestBid);
                                Logger.LogInformation($"buy complete");
                                areOrdersPlaced = true;
                            }
                        }
                        else
                        {
                            var currentTime = DateTime.UtcNow.TimeOfDay;
                            var nowTicks = DateTime.UtcNow.Ticks;
                            var originalValue = Interlocked.Read(ref LastWaitOutputTicks);
                            if (nowTicks - originalValue > WaitOutputInterval.Ticks)
                            {
                                Interlocked.Exchange(ref LastWaitOutputTicks, nowTicks);
                                Logger.LogInformation($"Buy order will be placed from {Settings.MinimumTimeToBuy} to {Settings.MaximumTimeToBuy}. Now it is {currentTime:hh\\:mm\\:ss}.");
                            }
                            continue;
                        }
                    }
                    if (areOrdersPlaced)
                    {
                        SyncActiveOrders();
                    }
                    else
                    {
                        var nowTicks = DateTime.UtcNow.Ticks;
                        var originalValue = Interlocked.Read(ref LastSyncTicks);
                        if (nowTicks - originalValue > SyncInterval.Ticks)
                        {
                            Interlocked.Exchange(ref LastSyncTicks, nowTicks);
                            SyncLots();
                        }
                    }
                }
                else if (ActiveBuyOrders.Count == 1)
                {
                    var activeBuyOrder = ActiveBuyOrders.Single().Value;
                    if (IsTimeToBuy())
                    {
                        var initialOrderPrice = MoneyValueToDecimal(activeBuyOrder.InitialSecurityPrice);
                        if (LotsSets.TryGetValue(initialOrderPrice, out var boughtLots) || LotsSets.Count == 0)
                        {
                            if (initialOrderPrice != bestBid && bestBidOrder.Quantity > Settings.MinimumMarketOrderSizeToChangeBuyPrice)
                            {
                                if (boughtLots > 0)
                                {
                                    Logger.LogInformation($"buy trades are in progress");
                                    continue;
                                }
                                Logger.LogInformation($"bid: {bestBid}, ask: {bestAsk}.");
                                Logger.LogInformation($"initial buy order price: {initialOrderPrice}");
                                Logger.LogInformation($"buy order price change activated");
                                // Cancel order
                                if (!await TryCancelOrder(activeBuyOrder.OrderId))
                                {
                                    ActiveBuyOrders.Clear();
                                    Logger.LogInformation($"failed to cancel buy order.");
                                    continue;
                                }
                                SetCashBalance(CashBalanceFree + CashBalanceLocked, 0);
                                // Place new order
                                var (cashBalance, _) = await GetCashBalance();
                                var lotPrice = bestBid * LotSize;
                                if (cashBalance > lotPrice)
                                {
                                    var lots = (long)(cashBalance / lotPrice);
                                    var marketLotsAtTargetPrice = orderBook.Bids.FirstOrDefault(o => o.Price == bestBid)?.Quantity ?? 0;
                                    Logger.LogInformation($"marketLotsAtTargetPrice: {marketLotsAtTargetPrice}");
                                    var response = await PlaceBuyOrder(lots, bestBid);
                                }
                                SyncActiveOrders();
                                Logger.LogInformation($"buy order price change is complete");
                            }
                        }
                        else
                        {
                            Logger.LogInformation($"bought lots with other prices found, cancelling buy order");
                            // Cancel order
                            if (!await TryCancelOrder(activeBuyOrder.OrderId))
                            {
                                ActiveBuyOrders.Clear();
                                Logger.LogInformation($"failed to cancel buy order.");
                                continue;
                            }
                            SyncActiveOrders();
                            Logger.LogInformation($"buy order cancelled");
                        }
                    }
                    else
                    {
                        Logger.LogInformation($"It is not time to buy, cancelling buy order");
                        // Cancel order
                        if (!await TryCancelOrder(activeBuyOrder.OrderId))
                        {
                            ActiveBuyOrders.Clear();
                            Logger.LogInformation($"failed to cancel buy order.");
                            continue;
                        }
                        SyncActiveOrders();
                        Logger.LogInformation($"buy order cancelled");
                    }
                }
                else if (ActiveSellOrders.Count == 1)
                {
                    var activeSellOrder = ActiveSellOrders.Single().Value;
                    if (ActiveSellOrderSourcePrice.TryGetValue(activeSellOrder.OrderId, out var sourcePrice))
                    {
                        var initialLots = activeSellOrder.InitialOrderPrice / activeSellOrder.InitialSecurityPrice;
                        var minimumSellPrice = GetMinimumSellPrice(sourcePrice);
                        if (topBidPrice <= sourcePrice && topBidPrice >= minimumSellPrice && topBidOrder.Quantity < (Settings.EarlySellOwnedLotsDelta + activeSellOrder.LotsRequested * Settings.EarlySellOwnedLotsMultiplier))
                        {
                            if (activeSellOrder.LotsRequested < initialLots)
                            {
                                Logger.LogInformation($"sell trades are in progress");
                                continue;
                            }
                            Logger.LogInformation($"early sell is activated");
                            Logger.LogInformation($"topBid: {topBid}, bestBid: {bestBid}, bestAsk: {bestAsk}.");
                            Logger.LogInformation($"topBidOrder.Quantity: {topBidOrder.Quantity}");
                            Logger.LogInformation($"EarlySellOwnedLotsDelta: {Settings.EarlySellOwnedLotsDelta}");
                            Logger.LogInformation($"EarlySellOwnedLotsMultiplier: {Settings.EarlySellOwnedLotsMultiplier}");
                            Logger.LogInformation($"LotsRequested: {activeSellOrder.LotsRequested}");
                            Logger.LogInformation($"Threshold: {(Settings.EarlySellOwnedLotsDelta + activeSellOrder.LotsRequested * Settings.EarlySellOwnedLotsMultiplier)}");
                            Logger.LogInformation($"initial sell order price: {sourcePrice}");
                            // Cancel order
                            if (!await TryCancelOrder(activeSellOrder.OrderId))
                            {
                                ActiveSellOrders.Clear();
                                Logger.LogInformation($"failed to cancel sell order.");
                                continue;
                            }
                            // Place new order at top bid price
                            var response = await PlaceSellOrder(activeSellOrder.LotsRequested, topBid);
                            SyncActiveOrders();
                            Logger.LogInformation($"early sell is complete");
                        }
                        else
                        {
                            var initialOrderPrice = MoneyValueToDecimal(activeSellOrder.InitialSecurityPrice);
                            if (bestAsk >= minimumSellPrice && bestAsk != initialOrderPrice && bestAskOrder.Quantity > Settings.MinimumMarketOrderSizeToChangeSellPrice)
                            {
                                Logger.LogInformation($"sell order price change activated");
                                Logger.LogInformation($"bid: {bestBid}, ask: {bestAsk}.");
                                Logger.LogInformation($"initial sell order price: {initialOrderPrice}");
                                Logger.LogInformation($"initial sell order source price: {sourcePrice}");
                                Logger.LogInformation($"minimumSellPrice: {minimumSellPrice}");
                                // Cancel order
                                if (!await TryCancelOrder(activeSellOrder.OrderId))
                                {
                                    ActiveSellOrders.Clear();
                                    Logger.LogInformation($"failed to cancel sell order.");
                                    continue;
                                }
                                // Place new order
                                var targetSellPrice = GetTargetSellPrice(minimumSellPrice, bestAsk);
                                var marketLotsAtTargetPrice = orderBook.Asks.FirstOrDefault(o => o.Price == targetSellPrice)?.Quantity ?? 0;
                                Logger.LogInformation($"marketLotsAtTargetPrice: {marketLotsAtTargetPrice}");
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

    private bool IsTimeToBuy() 
    {
       var currentTime = DateTime.UtcNow.TimeOfDay;
       return currentTime > MinimumTimeToBuy && currentTime < MaximumTimeToBuy;
    } 

    private async Task<(decimal, decimal)> GetCashBalance(bool forceRemote = false)
    {
        var response = (await InvestApi.Operations.GetPositionsAsync(new PositionsRequest { AccountId = CurrentAccount.Id }));
        var balanceFree = (decimal)response.Money.First(m => m.Currency == Settings.CashCurrency);
        var balanceLocked = response.Blocked.Any() ? (decimal)response.Blocked.First(m => m.Currency == Settings.CashCurrency) : 0;
        Logger.LogInformation($"Local cash balance, {Settings.CashCurrency}: {CashBalanceFree} ({CashBalanceLocked} locked)");
        Logger.LogInformation($"Remote cash balance, {Settings.CashCurrency}: {balanceFree} ({balanceLocked} locked)");
        // If remote balance is greater than local balance, update local balance
        if (balanceFree > CashBalanceFree)
        {
            SetCashBalance(balanceFree, balanceLocked);
            return (CashBalanceFree, CashBalanceLocked);
        }
        return (!forceRemote && PreferLocalCashBalance) ? (CashBalanceFree, CashBalanceLocked) : (balanceFree, balanceLocked);
    }

    private void SetCashBalance(decimal free, decimal locked)
    {
        CashBalanceFree = free;
        CashBalanceLocked = locked;
        Logger.LogInformation($"New local cash balance, {Settings.CashCurrency}: {CashBalanceFree} ({CashBalanceLocked} locked)");
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
        var tasks = new []
        {
            ReceiveTradesLoop(cancellationToken),
            SendOrdersLoop(cancellationToken)
        };
        await Task.WhenAll(tasks);
    }

    protected async Task Refresh(bool forceReset = false)
    {
        var nowTicks = DateTime.UtcNow.Ticks;
        var originalValue = Interlocked.Exchange(ref LastRefreshTicks, nowTicks);
        if (nowTicks - originalValue < RefreshInterval.Ticks)
        {
            return;
        }
        SyncActiveOrders(forceReset);
        LogActiveOrders();
        SyncLots(forceReset);
        LogLots();
        if (forceReset)
        {
            var cashBalance = await GetCashBalance(forceRemote: true);
            SetCashBalance(cashBalance.Item1, cashBalance.Item2);
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

    private OperationsList GetOpenOperations()
    {
        DateTime accountOpenDate =  DateTime.SpecifyKind(CurrentAccount.OpenedDate.ToDateTime(), DateTimeKind.Utc).AddHours(-3);
        DateTime lastCheckpoint = DateTime.SpecifyKind(LastOperationsCheckpoint, DateTimeKind.Utc).AddHours(-3);
        DateTime from = new [] { accountOpenDate, lastCheckpoint }.Max();
        var operations = InvestApi.Operations.GetOperations(new OperationsRequest
        {
            AccountId = CurrentAccount.Id,
            State = OperationState.Executed,
            Figi = Figi,
            From = Timestamp.FromDateTime(from),
            To = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(4))
        }).Operations.Select<Operation, (OperationType Type, DateTime Date, long Quantity, decimal Price)>(o => (o.OperationType, o.Date.ToDateTime(), o.GetActualQuantity(), o.Price)).OrderBy(x => x.Date).ToList();
        
        // Log operations
        foreach (var operation in operations)
        {
            Logger.LogInformation($"{operation.Type} operation with {operation.Quantity} lots at {operation.Price} price on {operation.Date.ToString("o", System.Globalization.CultureInfo.InvariantCulture)}.");
        }

        if (operations.Any() && operations.First().Type == OperationType.Sell)
        {
            throw new InvalidOperationException("Sell operation is first in list. It will not possible to correctly identify open operations.");
        }

        var totalSoldQuantity = operations.Where(o => o.Type == OperationType.Sell).Sum(o => o.Quantity);
        Logger.LogInformation($"Total sell operations quantity {totalSoldQuantity}");

        var openOperations = operations.Where(o => o.Type == OperationType.Buy).ToList();

        var totalBoughtQuantity = openOperations.Sum(o => o.Quantity);
        Logger.LogInformation($"Total buy operations quantity {totalBoughtQuantity}");

        if (totalSoldQuantity > 0 && totalSoldQuantity == totalBoughtQuantity)
        {
            var baseDate = operations.Last().Date.AddMilliseconds(1);
            LastOperationsCheckpoint = baseDate.AddHours(3);
            Logger.LogInformation($"New last operations checkpoint: {baseDate.ToString("o", System.Globalization.CultureInfo.InvariantCulture)}");
        }

        for (var i = 0; totalSoldQuantity > 0 && i < openOperations.Count; i++)
        {
            var openOperation = openOperations[i];
            var actualQuantity = openOperation.Quantity;
            if (totalSoldQuantity < actualQuantity)
            {
                Logger.LogInformation($"final totalSoldQuantity: \t{totalSoldQuantity}");
                Logger.LogInformation($"final actualQuantity: \t{actualQuantity}");
                openOperations[i] = (openOperation.Type, openOperation.Date, actualQuantity - totalSoldQuantity, openOperation.Price);
                Logger.LogInformation($"openOperation.Quantity: \t{openOperations[i].Quantity}");
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

        if (openOperations.Any(o => o.Price == 0m))
        {
            throw new InvalidOperationException("Open operation with price 0 is found.");
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
            Figi = Figi,
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
            Figi = Figi,
            Quantity = amount,
            Price = DecimalToQuotation(price),
        };
        // if (CashBalance < total)
        // {
        //     throw new InvalidOperationException($"Not enough money to buy {CurrentInstrument.Figi} asset.");
        // }
        var response = await InvestApi.Orders.PostOrderAsync(buyOrderRequest).ResponseAsync;
        var total = amount * price;
        SetCashBalance(CashBalanceFree - total, CashBalanceLocked + total);
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

    private async Task<bool> TryCancelOrder(string orderId)
    {
        try
        {
            await CancelOrder(orderId);
            return true;
        }
        catch (RpcException ex)
        {
            await Task.Delay(FailedCancelOrderInterval);
            if (ex.StatusCode == StatusCode.NotFound)
            {
                return false;
            }
            Logger.LogError(ex, "Error while cancelling order");
            return false;
        }
        catch (Exception e)
        {
            await Task.Delay(FailedCancelOrderInterval);
            Logger.LogError(e, "Error while cancelling order");
            return false;
        }
    }
}
