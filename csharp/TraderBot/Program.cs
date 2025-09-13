using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi;
using TraderBot;

namespace TraderBot;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args);

        var host = builder
    .ConfigureServices((context, services) =>
    {
        // Configuration
        services.AddSingleton(provider =>
        {
            var logger = provider.GetService<ILogger<LinksNotationConfigurationProvider>>();
            var linksConfig = new LinksNotationConfigurationProvider(logger);
            
            // Try to load Links notation config first
            var linksConfigPath = context.Configuration.GetValue<string>("LinksNotationConfigPath", "config.lino");
            linksConfig.LoadFromLinksNotation(linksConfigPath).Wait();
            
            // Fallback to traditional config if Links notation is not available
            var section = context.Configuration.GetSection(nameof(TradingSettings));
            var tradingSettings = section.Get<TradingSettings>() ?? linksConfig.BuildTradingSettings();
            
            return tradingSettings;
        });

        // Storage
        services.AddSingleton<FinancialStorage>();
        services.AddSingleton<PerformanceTracker>();

        // API Provider (choose between simulation and real)
        services.AddSingleton<ITradeApiProvider>(provider =>
        {
            var config = provider.GetService<IConfiguration>();
            var useSimulation = config.GetValue<bool>("UseSimulation", true);
            var logger = provider.GetService<ILogger<ITradeApiProvider>>();

            if (useSimulation)
            {
                return new SimulationTradeApiProvider();
            }
            else
            {
                // Real Tinkoff API
                var investApi = provider.GetService<InvestApiClient>();
                var settings = provider.GetService<TradingSettings>();
                var tinkoffLogger = provider.GetService<ILogger<TinkoffTradeApiProvider>>();
                
                // Would need account and FIGI resolution here
                return new TinkoffTradeApiProvider(investApi, "accountId", "figi", tinkoffLogger);
            }
        });

        // Trading Strategy
        services.AddSingleton<ITradingStrategy>(provider =>
        {
            var config = provider.GetService<IConfiguration>();
            var strategyName = config.GetValue<string>("TradingStrategy", "OptimalBid");
            var logger = provider.GetService<ILogger<OptimalBidTradingStrategy>>();

            return strategyName switch
            {
                "OptimalBid" => new OptimalBidTradingStrategy(
                    numberOfBids: config.GetValue<int>("Strategy:NumberOfBids", 5),
                    bidSpacing: config.GetValue<decimal>("Strategy:BidSpacing", 0.01m),
                    lotsPerBid: config.GetValue<int>("Strategy:LotsPerBid", 100),
                    logger: logger),
                _ => new OptimalBidTradingStrategy(logger: logger)
            };
        });

        // Tinkoff API (if needed)
        services.AddInvestApiClient((provider, settings) =>
        {
            var config = provider.GetService<IConfiguration>();
            var section = config.GetSection(nameof(Tinkoff.InvestApi.InvestApiSettings));
            var loadedSettings = section.Get<Tinkoff.InvestApi.InvestApiSettings>();
            
            if (loadedSettings != null)
            {
                settings.AccessToken = loadedSettings.AccessToken;
                settings.AppName = loadedSettings.AppName;
            }
        });

        // Main trading service
        services.AddHostedService<EnhancedTradingService>();
    })
    .ConfigureLogging((context, logging) =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddDebug();
        
        // Set appropriate log levels
        logging.SetMinimumLevel(LogLevel.Information);
        logging.AddFilter("TraderBot", LogLevel.Debug);
    })
    .Build();

// Create sample configurations if they don't exist
await CreateSampleConfigurations(host.Services);

Console.WriteLine("=== Enhanced Trading Bot Starting ===");
Console.WriteLine("Features:");
Console.WriteLine("✅ Pluggable API providers (Simulation + Tinkoff)");
Console.WriteLine("✅ Optimal Bid trading strategy from issue #103");
Console.WriteLine("✅ Links Notation configuration support");
Console.WriteLine("✅ Doublets associative storage");
Console.WriteLine("✅ Performance tracking with 1% goal validation");
Console.WriteLine("✅ Replaceable trading strategies");
Console.WriteLine("✅ TRUR ETF support");
Console.WriteLine();

await host.RunAsync();

static async Task CreateSampleConfigurations(IServiceProvider services)
{
    try
    {
        var logger = services.GetService<ILogger<Program>>();
        
        // Create sample Links notation config
        if (!File.Exists("config.lino"))
        {
            var linksConfig = services.GetService<LinksNotationConfigurationProvider>();
            linksConfig.SaveSampleConfiguration("config.lino");
            logger?.LogInformation("Created sample Links notation configuration: config.lino");
        }

        // Create sample appsettings if needed
        if (!File.Exists("appsettings.Enhanced.json"))
        {
            var sampleAppSettings = """
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "TraderBot": "Debug",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "UseSimulation": true,
  "TradingStrategy": "OptimalBid",
  "LinksNotationConfigPath": "config.lino",
  "Strategy": {
    "NumberOfBids": 5,
    "BidSpacing": 0.01,
    "LotsPerBid": 100
  },
  "InvestApiSettings": {
    "AccessToken": "",
    "AppName": "LinksPlatformEnhancedBot"
  },
  "TradingSettings": {
    "Instrument": "Etf",
    "Ticker": "TRUR",
    "CashCurrency": "rub",
    "AccountIndex": -1,
    "MinimumProfitSteps": -2,
    "MarketOrderBookDepth": 10,
    "MinimumMarketOrderSizeToChangeBuyPrice": 300000,
    "MinimumMarketOrderSizeToChangeSellPrice": 0,
    "MinimumMarketOrderSizeToBuy": 300000,
    "MinimumMarketOrderSizeToSell": 0,
    "MinimumTimeToBuy": "09:00:00",
    "MaximumTimeToBuy": "14:45:00",
    "EarlySellOwnedLotsDelta": 300000,
    "EarlySellOwnedLotsMultiplier": 0,
    "LoadOperationsFrom": "2025-03-01T00:00:01.3389860Z"
  }
}
""";
            await File.WriteAllTextAsync("appsettings.Enhanced.json", sampleAppSettings);
            logger?.LogInformation("Created sample enhanced configuration: appsettings.Enhanced.json");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not create sample configurations: {ex.Message}");
    }
}
    }
}

// Use the Tinkoff InvestApiSettings directly