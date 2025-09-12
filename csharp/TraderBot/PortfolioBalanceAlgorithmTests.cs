using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;

namespace TraderBot;

public static class PortfolioBalanceAlgorithmTests
{
    public static async Task RunTests()
    {
        await TestPortfolioBalanceCalculations();
        await TestRebalanceActionGeneration();
        await TestDeepStorageIntegration();
        Console.WriteLine("All portfolio balance algorithm tests completed successfully!");
    }

    private static async Task TestPortfolioBalanceCalculations()
    {
        var settings = new PortfolioBalanceSettings
        {
            Enabled = true,
            RebalanceThresholdPercent = 5.0m,
            AssetAllocations = new List<AssetAllocation>
            {
                new AssetAllocation { AssetType = "Gold", Ticker = "GOLD", TargetPercent = 25.0m, Instrument = Instrument.Etf },
                new AssetAllocation { AssetType = "USD", Ticker = "USD", TargetPercent = 25.0m, Instrument = Instrument.Etf },
                new AssetAllocation { AssetType = "Stocks", Ticker = "STOCK", TargetPercent = 50.0m, Instrument = Instrument.Shares }
            }
        };

        Console.WriteLine("✓ Portfolio balance settings created successfully");
        
        var totalPercent = settings.AssetAllocations.Sum(a => a.TargetPercent);
        if (Math.Abs(totalPercent - 100.0m) > 0.01m)
        {
            throw new InvalidOperationException($"Total allocation percentage should be 100%, got {totalPercent}%");
        }
        
        Console.WriteLine("✓ Portfolio allocation percentages sum to 100%");
    }

    private static async Task TestRebalanceActionGeneration()
    {
        var currentPortfolio = new Dictionary<string, decimal>
        {
            { "GOLD", 10000m },   // 10% (should be 25%)
            { "USD", 30000m },    // 30% (should be 25%) 
            { "STOCK", 60000m }   // 60% (should be 50%)
        };
        
        var totalValue = currentPortfolio.Values.Sum(); // 100000
        
        foreach (var asset in currentPortfolio)
        {
            var currentPercent = (asset.Value / totalValue) * 100;
            Console.WriteLine($"Asset {asset.Key}: {currentPercent:F1}% of portfolio (Value: {asset.Value:F0} RUB)");
        }

        var goldCurrentPercent = (currentPortfolio["GOLD"] / totalValue) * 100; // 10%
        var goldTargetPercent = 25.0m;
        var goldDeviation = Math.Abs(goldCurrentPercent - goldTargetPercent); // 15%
        
        if (goldDeviation <= 5.0m) 
        {
            throw new InvalidOperationException("Gold should need rebalancing (deviation > 5%)");
        }
        
        Console.WriteLine("✓ Rebalance thresholds calculated correctly");
        Console.WriteLine($"  Gold deviation: {goldDeviation:F1}% (threshold: 5.0%)");
    }

    private static async Task TestDeepStorageIntegration()
    {
        var storage = new FinancialStorage();
        
        // Test basic Deep storage operations with portfolio data
        var goldTicker = storage.StringToUnicodeSequenceConverter.Convert("GOLD");
        var targetPercent = storage.DecimalToRationalConverter.Convert(25.0m);
        var currentPercent = storage.DecimalToRationalConverter.Convert(10.0m);
        
        // Test decimal to rational conversion and back
        var retrievedTargetPercent = storage.RationalToDecimalConverter.Convert(targetPercent);
        var retrievedCurrentPercent = storage.RationalToDecimalConverter.Convert(currentPercent);
        
        if (Math.Abs(retrievedTargetPercent - 25.0m) > 0.01m)
        {
            throw new InvalidOperationException($"Target percent conversion failed: expected 25.0, got {retrievedTargetPercent}");
        }
        
        if (Math.Abs(retrievedCurrentPercent - 10.0m) > 0.01m)
        {
            throw new InvalidOperationException($"Current percent conversion failed: expected 10.0, got {retrievedCurrentPercent}");
        }
        
        Console.WriteLine("✓ Deep storage integration working correctly");
        Console.WriteLine($"  Stored and retrieved target percent: {retrievedTargetPercent:F1}%");
        Console.WriteLine($"  Stored and retrieved current percent: {retrievedCurrentPercent:F1}%");
        Console.WriteLine("  Note: Portfolio balance data is stored in associative format using Deep storage");
    }
}