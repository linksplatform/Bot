using Microsoft.Extensions.Logging;

namespace TraderBot;

/// <summary>
/// Configuration provider that supports Links Notation format
/// This is a simplified implementation - a full implementation would integrate with
/// the Links Platform Communication.Protocol.Lino package
/// </summary>
public class LinksNotationConfigurationProvider
{
    private readonly ILogger<LinksNotationConfigurationProvider> _logger;
    private readonly Dictionary<string, object> _configuration = new();

    public LinksNotationConfigurationProvider(ILogger<LinksNotationConfigurationProvider> logger)
    {
        _logger = logger;
    }

    public async Task LoadFromLinksNotation(string configPath)
    {
        try
        {
            if (!File.Exists(configPath))
            {
                _logger.LogWarning($"Links notation config file not found: {configPath}");
                return;
            }

            var content = await File.ReadAllTextAsync(configPath);
            ParseLinksNotation(content);
            
            _logger.LogInformation($"Loaded configuration from Links notation file: {configPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to load Links notation configuration from {configPath}");
        }
    }

    private void ParseLinksNotation(string content)
    {
        // Simplified Links Notation parser
        // In a real implementation, this would use the actual Links Platform parsing libraries
        
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("//") || string.IsNullOrWhiteSpace(trimmedLine))
                continue;

            // Parse basic key-value pairs in Links notation style
            // Format: (key (value))
            if (TryParseKeyValue(trimmedLine, out var key, out var value))
            {
                _configuration[key] = value;
                _logger.LogDebug($"Parsed config: {key} = {value}");
            }
        }
    }

    private bool TryParseKeyValue(string line, out string key, out object value)
    {
        key = null;
        value = null;

        // Simple regex-like parsing for basic Links notation
        // Real implementation would be much more sophisticated
        var trimmed = line.Trim('(', ')', ' ');
        var parts = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length == 2)
        {
            key = parts[0];
            var valueStr = parts[1].Trim('(', ')', ' ', '"');
            
            // Try to parse different types
            if (bool.TryParse(valueStr, out var boolValue))
                value = boolValue;
            else if (int.TryParse(valueStr, out var intValue))
                value = intValue;
            else if (decimal.TryParse(valueStr, out var decimalValue))
                value = decimalValue;
            else
                value = valueStr;
                
            return true;
        }

        return false;
    }

    public T GetValue<T>(string key, T defaultValue = default(T))
    {
        if (_configuration.TryGetValue(key, out var value))
        {
            try
            {
                if (value is T directValue)
                    return directValue;
                    
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to convert config value for key {key} to type {typeof(T).Name}");
            }
        }

        return defaultValue;
    }

    public TradingSettings BuildTradingSettings()
    {
        return new TradingSettings
        {
            Ticker = GetValue<string>("etf_ticker", "TRUR"),
            CashCurrency = GetValue<string>("cash_currency", "rub"),
            AccountIndex = GetValue<int>("account_index", -1),
            MinimumProfitSteps = GetValue<long>("minimum_profit_steps", -2),
            MarketOrderBookDepth = GetValue<int>("market_order_book_depth", 10),
            MinimumMarketOrderSizeToBuy = GetValue<long>("minimum_market_order_size_to_buy", 300000),
            MinimumMarketOrderSizeToSell = GetValue<long>("minimum_market_order_size_to_sell", 0),
            MinimumTimeToBuy = GetValue<string>("minimum_time_to_buy", "09:00:00"),
            MaximumTimeToBuy = GetValue<string>("maximum_time_to_buy", "14:45:00"),
            EarlySellOwnedLotsDelta = GetValue<long>("early_sell_owned_lots_delta", 300000),
            EarlySellOwnedLotsMultiplier = GetValue<decimal>("early_sell_owned_lots_multiplier", 0),
            LoadOperationsFrom = GetValue<DateTime>("load_operations_from", DateTime.UtcNow.AddMonths(-1))
        };
    }

    public void SaveSampleConfiguration(string filePath)
    {
        var sampleConfig = @"// Trading Bot Configuration in Links Notation
// This is a simplified representation - full Links Notation would be more complex

(etf_ticker ""TRUR"")
(cash_currency ""rub"")
(account_index -1)
(minimum_profit_steps -2)
(market_order_book_depth 10)
(minimum_market_order_size_to_buy 300000)
(minimum_market_order_size_to_sell 0)
(minimum_time_to_buy ""09:00:00"")
(maximum_time_to_buy ""14:45:00"")
(early_sell_owned_lots_delta 300000)
(early_sell_owned_lots_multiplier 0)

// Strategy configuration
(strategy_name ""OptimalBid"")
(number_of_bids 5)
(bid_spacing 0.01)
(lots_per_bid 100)

// API configuration
(use_simulation true)
(tinkoff_access_token """")
(app_name ""LinksPlatformBot"")

// Performance tracking
(performance_goal_annual_outperformance 0.01)
(enable_performance_tracking true)
";

        File.WriteAllText(filePath, sampleConfig);
    }
}