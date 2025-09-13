using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TraderBot;

public class EnhancedTradingService : BackgroundService
{
    private readonly ITradeApiProvider _apiProvider;
    private readonly ITradingStrategy _strategy;
    private readonly TradingSettings _settings;
    private readonly ILogger<EnhancedTradingService> _logger;
    private readonly PerformanceTracker _performanceTracker;
    
    private readonly TimeSpan _tradingInterval = TimeSpan.FromSeconds(30);
    
    public EnhancedTradingService(
        ITradeApiProvider apiProvider,
        ITradingStrategy strategy,
        TradingSettings settings,
        ILogger<EnhancedTradingService> logger,
        PerformanceTracker performanceTracker)
    {
        _apiProvider = apiProvider;
        _strategy = strategy;
        _settings = settings;
        _logger = logger;
        _performanceTracker = performanceTracker;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"Enhanced trading service started with strategy: {_strategy.Name}");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecuteTradingCycle();
                await Task.Delay(_tradingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in trading cycle");
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken); // Wait longer on error
            }
        }
        
        _logger.LogInformation("Enhanced trading service stopped");
    }

    private async Task ExecuteTradingCycle()
    {
        try
        {
            // Check if we're in trading hours
            if (!IsInTradingHours())
            {
                await Task.Delay(TimeSpan.FromMinutes(5)); // Check again in 5 minutes
                return;
            }

            _logger.LogDebug("Starting trading cycle");

            // Gather market data
            var context = await BuildTradingContext();
            
            // Record current portfolio value for performance tracking
            var portfolioValue = context.CashBalance + (context.AssetBalance * context.CurrentPrice);
            await _performanceTracker.RecordPortfolioValue(portfolioValue, context.CurrentPrice);

            // Get strategy recommendations
            var actions = await _strategy.CalculateActions(context);

            // Execute actions
            foreach (var action in actions)
            {
                try
                {
                    await action.Execute(_apiProvider);
                    _logger.LogInformation($"Executed action: {action.GetType().Name}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to execute action: {action.GetType().Name}");
                }
            }

            _logger.LogDebug($"Completed trading cycle - Portfolio value: {portfolioValue:F2}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in trading cycle execution");
        }
    }

    private async Task<TradingContext> BuildTradingContext()
    {
        var currentPrice = await _apiProvider.GetCurrentPrice(_settings.Ticker);
        var orderBook = await _apiProvider.GetOrderBook(_settings.Ticker, _settings.MarketOrderBookDepth);
        var (cashFree, cashLocked) = await _apiProvider.GetBalance(_settings.CashCurrency);
        var (assetFree, assetLocked) = await _apiProvider.GetBalance(_settings.Ticker);

        // Get active orders (this would need to be enhanced to track our orders)
        var activeOrders = new List<ActiveOrder>(); // Simplified for now

        return new TradingContext
        {
            CurrentPrice = currentPrice,
            OrderBook = orderBook,
            CashBalance = cashFree,
            AssetBalance = assetFree,
            ActiveOrders = activeOrders,
            CurrentTime = DateTime.UtcNow,
            Settings = _settings
        };
    }

    private bool IsInTradingHours()
    {
        var now = DateTime.UtcNow.TimeOfDay;
        
        // Parse trading hours from settings
        if (TimeSpan.TryParse(_settings.MinimumTimeToBuy, out var minTime) &&
            TimeSpan.TryParse(_settings.MaximumTimeToBuy, out var maxTime))
        {
            return now >= minTime && now <= maxTime;
        }

        // Default trading hours (Moscow market: 12:00-17:00 MSK = 09:00-14:00 UTC)
        return now >= TimeSpan.FromHours(9) && now <= TimeSpan.FromHours(14);
    }
}