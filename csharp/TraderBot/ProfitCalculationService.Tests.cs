using Microsoft.Extensions.Logging;
using Moq;
using Tinkoff.InvestApi.V1;
using Xunit;

namespace TraderBot.Tests;

using OperationsList = List<(OperationType Type, DateTime Date, long Quantity, decimal Price)>;

public class ProfitCalculationServiceTests
{
    private readonly ProfitCalculationService _service;
    private readonly Mock<ILogger<ProfitCalculationService>> _mockLogger;

    public ProfitCalculationServiceTests()
    {
        _mockLogger = new Mock<ILogger<ProfitCalculationService>>();
        _service = new ProfitCalculationService(_mockLogger.Object);
    }

    [Fact]
    public void CalculateProfit_EmptyOperations_ReturnsZeroProfit()
    {
        // Arrange
        var operations = new OperationsList();

        // Act
        var result = _service.CalculateProfit(operations);

        // Assert
        Assert.Equal(0, result.TotalAbsoluteProfit);
        Assert.Equal(0, result.TotalRelativeProfit);
        Assert.Equal(0, result.RealizedAbsoluteProfit);
        Assert.Equal(0, result.RealizedRelativeProfit);
        Assert.Equal(0, result.UnrealizedAbsoluteProfit);
        Assert.Equal(0, result.UnrealizedRelativeProfit);
        Assert.Equal(0, result.OpenPositionQuantity);
        Assert.Empty(result.CompletedTrades);
    }

    [Fact]
    public void CalculateProfit_OnlyBuyOperations_ShowsUnrealizedProfit()
    {
        // Arrange
        var operations = new OperationsList
        {
            (OperationType.Buy, DateTime.Now.AddHours(-2), 10, 100.0m),
            (OperationType.Buy, DateTime.Now.AddHours(-1), 5, 110.0m)
        };
        var currentPrice = 120.0m;

        // Act
        var result = _service.CalculateProfit(operations, currentPrice);

        // Assert
        Assert.Equal(15, result.OpenPositionQuantity);
        Assert.Equal(1550.0m, result.TotalInvested); // (10*100) + (5*110)
        Assert.Equal(103.33m, Math.Round(result.AverageBuyPrice, 2)); // 1550/15
        Assert.Equal(1800.0m - 1550.0m, result.UnrealizedAbsoluteProfit); // (15*120) - 1550
        Assert.Equal(250.0m, result.UnrealizedAbsoluteProfit);
        Assert.Equal(Math.Round((250.0m / 1550.0m) * 100, 2), Math.Round(result.UnrealizedRelativeProfit, 2)); // ~16.13%
        Assert.Equal(0, result.RealizedAbsoluteProfit);
        Assert.Empty(result.CompletedTrades);
    }

    [Fact]
    public void CalculateProfit_BuyAndSellOperations_ShowsRealizedProfit()
    {
        // Arrange
        var operations = new OperationsList
        {
            (OperationType.Buy, DateTime.Now.AddHours(-3), 10, 100.0m),
            (OperationType.Buy, DateTime.Now.AddHours(-2), 5, 110.0m),
            (OperationType.Sell, DateTime.Now.AddHours(-1), 8, 120.0m)
        };
        var currentPrice = 115.0m;

        // Act
        var result = _service.CalculateProfit(operations, currentPrice);

        // Assert
        // Realized profit: First 8 lots sold at 120, bought at 100 (FIFO)
        // 8 * (120 - 100) = 160
        Assert.Equal(160.0m, result.RealizedAbsoluteProfit);
        
        // Remaining position: 2 lots at 100 + 5 lots at 110 = 7 lots
        Assert.Equal(7, result.OpenPositionQuantity);
        Assert.Equal(750.0m, result.TotalInvested); // (2*100) + (5*110)
        
        // Unrealized profit: 7 lots at current price 115 vs invested 750
        var currentValue = 7 * 115.0m; // 805
        var unrealizedProfit = currentValue - 750.0m; // 55
        Assert.Equal(55.0m, result.UnrealizedAbsoluteProfit);
        
        // Total profit = realized + unrealized
        Assert.Equal(215.0m, result.TotalAbsoluteProfit); // 160 + 55
        
        // Should have one completed trade
        Assert.Single(result.CompletedTrades);
        Assert.Equal(8, result.CompletedTrades[0].Quantity);
        Assert.Equal(100.0m, result.CompletedTrades[0].BuyPrice);
        Assert.Equal(120.0m, result.CompletedTrades[0].SellPrice);
        Assert.Equal(160.0m, result.CompletedTrades[0].AbsoluteProfit);
    }

    [Fact]
    public void CalculateProfit_MultipleBuysAndSells_FIFO_Matching()
    {
        // Arrange
        var operations = new OperationsList
        {
            (OperationType.Buy, DateTime.Now.AddHours(-6), 10, 100.0m),   // Buy 10 @ 100
            (OperationType.Buy, DateTime.Now.AddHours(-5), 5, 110.0m),    // Buy 5 @ 110
            (OperationType.Sell, DateTime.Now.AddHours(-4), 3, 120.0m),   // Sell 3 @ 120 (from first buy)
            (OperationType.Sell, DateTime.Now.AddHours(-3), 7, 125.0m),   // Sell 7 @ 125 (remaining 7 from first buy)
            (OperationType.Buy, DateTime.Now.AddHours(-2), 8, 105.0m),    // Buy 8 @ 105
            (OperationType.Sell, DateTime.Now.AddHours(-1), 2, 130.0m)    // Sell 2 @ 130 (from second buy)
        };

        // Act
        var result = _service.CalculateProfit(operations);

        // Assert
        // Completed trades:
        // Trade 1: 3 lots @ 100 -> 120 = 3 * (120-100) = 60
        // Trade 2: 7 lots @ 100 -> 125 = 7 * (125-100) = 175  
        // Trade 3: 2 lots @ 110 -> 130 = 2 * (130-110) = 40
        // Total realized: 60 + 175 + 40 = 275
        Assert.Equal(275.0m, result.RealizedAbsoluteProfit);
        
        // Remaining positions:
        // 3 lots from second buy @ 110
        // 8 lots from third buy @ 105
        // Total: 11 lots, value: (3*110) + (8*105) = 330 + 840 = 1170
        Assert.Equal(11, result.OpenPositionQuantity);
        Assert.Equal(1170.0m, result.TotalInvested);
        
        // Should have 3 completed trades
        Assert.Equal(3, result.CompletedTrades.Count);
    }

    [Fact]
    public void CalculateProfit_ProfitableTrade_CorrectRelativePercentage()
    {
        // Arrange
        var operations = new OperationsList
        {
            (OperationType.Buy, DateTime.Now.AddHours(-2), 10, 100.0m),   // Invest 1000
            (OperationType.Sell, DateTime.Now.AddHours(-1), 10, 110.0m)   // Sell for 1100
        };

        // Act
        var result = _service.CalculateProfit(operations);

        // Assert
        Assert.Equal(100.0m, result.RealizedAbsoluteProfit); // 1100 - 1000
        Assert.Equal(10.0m, result.RealizedRelativeProfit);  // (100/1000) * 100 = 10%
        Assert.Equal(0, result.OpenPositionQuantity); // All positions closed
    }

    [Fact]
    public void CalculateProfit_LossTrade_NegativeProfit()
    {
        // Arrange
        var operations = new OperationsList
        {
            (OperationType.Buy, DateTime.Now.AddHours(-2), 10, 100.0m),   // Invest 1000
            (OperationType.Sell, DateTime.Now.AddHours(-1), 10, 90.0m)    // Sell for 900
        };

        // Act
        var result = _service.CalculateProfit(operations);

        // Assert
        Assert.Equal(-100.0m, result.RealizedAbsoluteProfit); // 900 - 1000
        Assert.Equal(-10.0m, result.RealizedRelativeProfit);  // (-100/1000) * 100 = -10%
    }
}