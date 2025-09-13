using System.Collections.Concurrent;

namespace TraderBot;

public class SimulationTradeApiProvider : ITradeApiProvider
{
    private readonly ConcurrentDictionary<string, decimal> _balances = new();
    private readonly ConcurrentDictionary<string, SimulatedOrder> _orders = new();
    private readonly Random _random = new();
    private decimal _currentPrice = 100.0m; // Starting price for TRUR simulation
    private readonly object _priceLock = new();
    
    public SimulationTradeApiProvider()
    {
        // Initialize with some virtual money
        _balances["rub"] = 1000000; // 1M rubles
        _balances["TRUR"] = 0; // No ETF shares initially
        
        // Start price simulation
        _ = Task.Run(SimulatePriceMovement);
    }

    public Task<decimal> GetCurrentPrice(string symbol)
    {
        lock (_priceLock)
        {
            return Task.FromResult(_currentPrice);
        }
    }

    public Task<string> PlaceBuyOrder(string symbol, int lots, decimal price)
    {
        var orderId = Guid.NewGuid().ToString();
        var order = new SimulatedOrder
        {
            Id = orderId,
            Symbol = symbol,
            Lots = lots,
            Price = price,
            Type = OrderType.Buy,
            State = OrderState.New,
            PlacedAt = DateTime.UtcNow
        };
        
        _orders[orderId] = order;
        
        // Simulate order filling
        _ = Task.Run(() => SimulateOrderFilling(order));
        
        return Task.FromResult(orderId);
    }

    public Task<string> PlaceSellOrder(string symbol, int lots, decimal price)
    {
        var orderId = Guid.NewGuid().ToString();
        var order = new SimulatedOrder
        {
            Id = orderId,
            Symbol = symbol,
            Lots = lots,
            Price = price,
            Type = OrderType.Sell,
            State = OrderState.New,
            PlacedAt = DateTime.UtcNow
        };
        
        _orders[orderId] = order;
        
        // Simulate order filling
        _ = Task.Run(() => SimulateOrderFilling(order));
        
        return Task.FromResult(orderId);
    }

    public Task<bool> CancelOrder(string orderId)
    {
        if (_orders.TryGetValue(orderId, out var order) && order.State == OrderState.New)
        {
            order.State = OrderState.Cancelled;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<OrderState> GetOrderStatus(string orderId)
    {
        if (_orders.TryGetValue(orderId, out var order))
        {
            return Task.FromResult(order.State);
        }
        return Task.FromResult(OrderState.Rejected);
    }

    public Task<(decimal Free, decimal Locked)> GetBalance(string currency)
    {
        var free = _balances.GetValueOrDefault(currency, 0);
        return Task.FromResult((free, 0m)); // Simplified: no locked amounts in simulation
    }

    public Task<List<OrderBook>> GetOrderBook(string symbol, int depth)
    {
        var orderBook = new List<OrderBook>();
        
        lock (_priceLock)
        {
            // Simulate order book around current price
            for (int i = 1; i <= depth; i++)
            {
                // Buy orders (bids) below current price
                orderBook.Add(new OrderBook
                {
                    Price = _currentPrice - (i * 0.01m),
                    Quantity = _random.Next(100, 1000),
                    Type = OrderType.Buy
                });
                
                // Sell orders (asks) above current price
                orderBook.Add(new OrderBook
                {
                    Price = _currentPrice + (i * 0.01m),
                    Quantity = _random.Next(100, 1000),
                    Type = OrderType.Sell
                });
            }
        }
        
        return Task.FromResult(orderBook);
    }

    private async Task SimulatePriceMovement()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(5)); // Update every 5 seconds
            
            lock (_priceLock)
            {
                // Simulate random walk with slight upward bias (to simulate ETF growth)
                var change = (_random.NextDouble() - 0.48) * 0.02; // Slight upward bias
                _currentPrice *= (decimal)(1 + change);
                _currentPrice = Math.Max(_currentPrice, 50m); // Floor price
                _currentPrice = Math.Min(_currentPrice, 200m); // Ceiling price
            }
        }
    }

    private async Task SimulateOrderFilling(SimulatedOrder order)
    {
        // Wait a bit to simulate order processing
        await Task.Delay(_random.Next(1000, 5000));
        
        if (order.State != OrderState.New) return; // Order was cancelled
        
        decimal currentPrice;
        lock (_priceLock)
        {
            currentPrice = _currentPrice;
        }
        
        // Simple fill logic: fill if price crosses order price
        bool shouldFill = (order.Type == OrderType.Buy && currentPrice <= order.Price) ||
                         (order.Type == OrderType.Sell && currentPrice >= order.Price);
        
        if (shouldFill)
        {
            order.State = OrderState.Filled;
            
            // Update balances
            if (order.Type == OrderType.Buy)
            {
                var totalCost = order.Lots * order.Price;
                if (_balances.GetValueOrDefault("rub", 0) >= totalCost)
                {
                    _balances["rub"] -= totalCost;
                    _balances[order.Symbol] = _balances.GetValueOrDefault(order.Symbol, 0) + order.Lots;
                }
                else
                {
                    order.State = OrderState.Rejected;
                }
            }
            else // Sell
            {
                if (_balances.GetValueOrDefault(order.Symbol, 0) >= order.Lots)
                {
                    _balances[order.Symbol] -= order.Lots;
                    _balances["rub"] += order.Lots * order.Price;
                }
                else
                {
                    order.State = OrderState.Rejected;
                }
            }
        }
    }

    private class SimulatedOrder
    {
        public string Id { get; set; }
        public string Symbol { get; set; }
        public int Lots { get; set; }
        public decimal Price { get; set; }
        public OrderType Type { get; set; }
        public OrderState State { get; set; }
        public DateTime PlacedAt { get; set; }
    }
}