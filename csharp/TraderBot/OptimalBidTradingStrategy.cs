using Microsoft.Extensions.Logging;

namespace TraderBot;

/// <summary>
/// Implementation of the optimal trading strategy described in issue #103:
/// - Each day should be closed in half ETF, half cash for optimal performance
/// - All sell bids are not movable
/// - All buy bids are movable (should be moved up when empty slots appear)
/// - Strategy can have 2*N bids: +1..+N (sell) and -1..-N (buy)
/// - Each day should start with placing N bids for sale and N bids for buy
/// - Works best on non-volatile ETFs or currencies
/// </summary>
public class OptimalBidTradingStrategy : ITradingStrategy
{
    public string Name => "OptimalBid";
    
    private readonly int _numberOfBids;
    private readonly decimal _bidSpacing; // Spacing between bids in price units
    private readonly int _lotsPerBid;
    private readonly ILogger<OptimalBidTradingStrategy>? _logger;

    public OptimalBidTradingStrategy(int numberOfBids = 5, decimal bidSpacing = 0.01m, int lotsPerBid = 100, ILogger<OptimalBidTradingStrategy>? logger = null)
    {
        _numberOfBids = numberOfBids;
        _bidSpacing = bidSpacing;
        _lotsPerBid = lotsPerBid;
        _logger = logger;
    }

    public async Task<IEnumerable<TradingAction>> CalculateActions(TradingContext context)
    {
        var actions = new List<TradingAction>();
        
        try
        {
            // Get current active orders
            var activeBuyOrders = context.ActiveOrders
                .Where(o => o.Type == OrderType.Buy && o.State == OrderState.New)
                .OrderByDescending(o => o.Price)
                .ToList();
                
            var activeSellOrders = context.ActiveOrders
                .Where(o => o.Type == OrderType.Sell && o.State == OrderState.New)
                .OrderBy(o => o.Price)
                .ToList();

            // Calculate target positions for optimal half-ETF, half-cash balance
            var totalValue = context.CashBalance + (context.AssetBalance * context.CurrentPrice);
            var targetCashValue = totalValue / 2;
            var targetAssetValue = totalValue / 2;
            var targetAssetLots = (int)(targetAssetValue / context.CurrentPrice);

            _logger?.LogInformation($"Total value: {totalValue:F2}, Target cash: {targetCashValue:F2}, Target asset lots: {targetAssetLots}");

            // Place sell orders (+1..+N from current price)
            var plannedSellOrders = GenerateSellOrderPrices(context.CurrentPrice, _numberOfBids);
            actions.AddRange(await PlaceMissingSellOrders(context, activeSellOrders, plannedSellOrders));

            // Place and manage buy orders (-1..-N from current price) 
            var plannedBuyOrders = GenerateBuyOrderPrices(context.CurrentPrice, _numberOfBids);
            actions.AddRange(await ManageBuyOrders(context, activeBuyOrders, plannedBuyOrders));

            _logger?.LogInformation($"Generated {actions.Count} trading actions");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error calculating trading actions");
        }

        return actions;
    }

    private List<decimal> GenerateSellOrderPrices(decimal currentPrice, int count)
    {
        var prices = new List<decimal>();
        for (int i = 1; i <= count; i++)
        {
            prices.Add(currentPrice + (i * _bidSpacing));
        }
        return prices;
    }

    private List<decimal> GenerateBuyOrderPrices(decimal currentPrice, int count)
    {
        var prices = new List<decimal>();
        for (int i = 1; i <= count; i++)
        {
            prices.Add(currentPrice - (i * _bidSpacing));
        }
        return prices;
    }

    private async Task<List<TradingAction>> PlaceMissingSellOrders(TradingContext context, List<ActiveOrder> activeSellOrders, List<decimal> plannedPrices)
    {
        var actions = new List<TradingAction>();
        
        foreach (var targetPrice in plannedPrices)
        {
            // Check if we already have a sell order at this price level
            var existingOrder = activeSellOrders.FirstOrDefault(o => Math.Abs(o.Price - targetPrice) < 0.001m);
            if (existingOrder == null)
            {
                // Check if we have enough assets to sell
                if (context.AssetBalance >= _lotsPerBid)
                {
                    actions.Add(new PlaceSellOrderAction
                    {
                        Symbol = context.Settings.Ticker,
                        Lots = _lotsPerBid,
                        Price = targetPrice
                    });
                    
                    _logger?.LogInformation($"Planning sell order: {_lotsPerBid} lots at {targetPrice:F2}");
                }
            }
        }

        return actions;
    }

    private async Task<List<TradingAction>> ManageBuyOrders(TradingContext context, List<ActiveOrder> activeBuyOrders, List<decimal> plannedPrices)
    {
        var actions = new List<TradingAction>();

        // Cancel buy orders that are too far from current planned prices
        foreach (var activeOrder in activeBuyOrders)
        {
            var closestPlannedPrice = plannedPrices.OrderBy(p => Math.Abs(p - activeOrder.Price)).First();
            if (Math.Abs(activeOrder.Price - closestPlannedPrice) > _bidSpacing / 2)
            {
                actions.Add(new CancelOrderAction { OrderId = activeOrder.Id });
                _logger?.LogInformation($"Canceling misplaced buy order: {activeOrder.Id} at {activeOrder.Price:F2}");
            }
        }

        // Place missing buy orders
        foreach (var targetPrice in plannedPrices)
        {
            var existingOrder = activeBuyOrders.FirstOrDefault(o => Math.Abs(o.Price - targetPrice) < _bidSpacing / 2);
            if (existingOrder == null)
            {
                var requiredCash = _lotsPerBid * targetPrice;
                if (context.CashBalance >= requiredCash)
                {
                    actions.Add(new PlaceBuyOrderAction
                    {
                        Symbol = context.Settings.Ticker,
                        Lots = _lotsPerBid,
                        Price = targetPrice
                    });
                    
                    _logger?.LogInformation($"Planning buy order: {_lotsPerBid} lots at {targetPrice:F2}");
                }
            }
        }

        // Move buy orders up when "empty slots appear" (when price moves up)
        await MoveBuyOrdersUpIfNeeded(context, activeBuyOrders, plannedPrices, actions);

        return actions;
    }

    private async Task MoveBuyOrdersUpIfNeeded(TradingContext context, List<ActiveOrder> activeBuyOrders, List<decimal> plannedPrices, List<TradingAction> actions)
    {
        // This implements the "movable buy orders" requirement
        // Buy orders should be moved up when empty slots appear (i.e., when price moves up)
        
        foreach (var activeOrder in activeBuyOrders)
        {
            // Find if there's a higher planned price that's not occupied
            var higherPlannedPrices = plannedPrices.Where(p => p > activeOrder.Price).OrderBy(p => p);
            
            foreach (var higherPrice in higherPlannedPrices)
            {
                // Check if this higher price slot is empty
                var occupiedByOtherOrder = activeBuyOrders.Any(o => o.Id != activeOrder.Id && Math.Abs(o.Price - higherPrice) < _bidSpacing / 2);
                
                if (!occupiedByOtherOrder)
                {
                    // Move this order up to fill the empty slot
                    actions.Add(new MoveBuyOrderAction
                    {
                        OrderId = activeOrder.Id,
                        Symbol = context.Settings.Ticker,
                        Lots = activeOrder.Lots,
                        NewPrice = higherPrice
                    });
                    
                    _logger?.LogInformation($"Moving buy order {activeOrder.Id} from {activeOrder.Price:F2} to {higherPrice:F2}");
                    break; // Only move to the first available higher slot
                }
            }
        }
    }
}