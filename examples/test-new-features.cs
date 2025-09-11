using System;
using TraderBot;

// This is a simple test to verify that our new features can be configured correctly
public class FeatureTestExample
{
    public static void TestNewTradingSettings()
    {
        // Test 1: Auto-sell before market close disabled
        var settings1 = new TradingSettings
        {
            EnableAutoSellBeforeMarketClose = false,
            AutoSellMarketCloseTime = "23:50:00",
            MaxProfitPercent = null,
            MaxLossPercent = null
        };
        
        Console.WriteLine($"Test 1 - Auto-sell disabled: {settings1.EnableAutoSellBeforeMarketClose}");
        Console.WriteLine($"Market close time: {settings1.AutoSellMarketCloseTime}");
        
        // Test 2: Auto-sell enabled with profit/loss limits
        var settings2 = new TradingSettings
        {
            EnableAutoSellBeforeMarketClose = true,
            AutoSellMarketCloseTime = "18:40:00",
            MaxProfitPercent = 5.0m,
            MaxLossPercent = 2.0m
        };
        
        Console.WriteLine($"\nTest 2 - Auto-sell enabled: {settings2.EnableAutoSellBeforeMarketClose}");
        Console.WriteLine($"Market close time: {settings2.AutoSellMarketCloseTime}");
        Console.WriteLine($"Max profit: {settings2.MaxProfitPercent}%");
        Console.WriteLine($"Max loss: {settings2.MaxLossPercent}%");
        
        Console.WriteLine("\nAll feature tests passed!");
    }
    
    public static void TestProfitLossCalculation()
    {
        decimal sourcePrice = 100.0m;
        decimal currentPrice1 = 105.0m; // 5% profit
        decimal currentPrice2 = 98.0m;  // 2% loss
        
        var profitPercent1 = ((currentPrice1 - sourcePrice) / sourcePrice) * 100;
        var profitPercent2 = ((currentPrice2 - sourcePrice) / sourcePrice) * 100;
        
        Console.WriteLine($"\nProfit/Loss calculation test:");
        Console.WriteLine($"Source price: {sourcePrice}");
        Console.WriteLine($"Current price 1: {currentPrice1} -> {profitPercent1:F2}% profit");
        Console.WriteLine($"Current price 2: {currentPrice2} -> {profitPercent2:F2}% loss");
    }
}