using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi.V1;

namespace TraderBot;

using OperationsList = List<(OperationType Type, DateTime Date, long Quantity, decimal Price)>;

public class ProfitCalculationService
{
    private readonly ILogger<ProfitCalculationService> Logger;

    public ProfitCalculationService(ILogger<ProfitCalculationService> logger)
    {
        Logger = logger;
    }

    public ProfitAnalysis CalculateProfit(OperationsList operations, decimal currentPrice = 0)
    {
        if (operations == null || !operations.Any())
        {
            return new ProfitAnalysis
            {
                TotalAbsoluteProfit = 0,
                TotalRelativeProfit = 0,
                RealizedAbsoluteProfit = 0,
                RealizedRelativeProfit = 0,
                UnrealizedAbsoluteProfit = 0,
                UnrealizedRelativeProfit = 0,
                TotalInvested = 0,
                OpenPositionQuantity = 0,
                AverageBuyPrice = 0,
                CompletedTrades = new List<TradeProfit>()
            };
        }

        var analysis = new ProfitAnalysis();
        var completedTrades = new List<TradeProfit>();
        var openBuyOperations = new List<(OperationType Type, DateTime Date, long Quantity, decimal Price)>();
        
        decimal totalBought = 0;
        decimal totalSold = 0;
        long totalBoughtQuantity = 0;
        long totalSoldQuantity = 0;
        
        // Separate buy and sell operations
        var buyOperations = operations.Where(o => o.Type == OperationType.Buy).OrderBy(o => o.Date).ToList();
        var sellOperations = operations.Where(o => o.Type == OperationType.Sell).OrderBy(o => o.Date).ToList();

        // Calculate completed trades (FIFO matching)
        var remainingBuyOps = buyOperations.ToList();
        
        foreach (var sellOp in sellOperations)
        {
            var remainingSellQuantity = sellOp.Quantity;
            
            while (remainingSellQuantity > 0 && remainingBuyOps.Any())
            {
                var firstBuy = remainingBuyOps.First();
                var matchedQuantity = Math.Min(remainingSellQuantity, firstBuy.Quantity);
                
                // Calculate profit for this matched trade
                var buyValue = matchedQuantity * firstBuy.Price;
                var sellValue = matchedQuantity * sellOp.Price;
                var absoluteProfit = sellValue - buyValue;
                var relativeProfit = buyValue > 0 ? (absoluteProfit / buyValue) * 100 : 0;
                
                completedTrades.Add(new TradeProfit
                {
                    BuyDate = firstBuy.Date,
                    SellDate = sellOp.Date,
                    Quantity = matchedQuantity,
                    BuyPrice = firstBuy.Price,
                    SellPrice = sellOp.Price,
                    AbsoluteProfit = absoluteProfit,
                    RelativeProfit = relativeProfit
                });
                
                // Update totals
                totalSold += sellValue;
                totalSoldQuantity += matchedQuantity;
                
                // Update remaining quantities
                remainingSellQuantity -= matchedQuantity;
                
                if (firstBuy.Quantity <= matchedQuantity)
                {
                    totalBought += firstBuy.Quantity * firstBuy.Price;
                    totalBoughtQuantity += firstBuy.Quantity;
                    remainingBuyOps.RemoveAt(0);
                }
                else
                {
                    totalBought += matchedQuantity * firstBuy.Price;
                    totalBoughtQuantity += matchedQuantity;
                    remainingBuyOps[0] = (firstBuy.Type, firstBuy.Date, firstBuy.Quantity - matchedQuantity, firstBuy.Price);
                }
            }
        }
        
        // Remaining buy operations are open positions
        openBuyOperations = remainingBuyOps;
        
        // Calculate realized profit
        analysis.RealizedAbsoluteProfit = completedTrades.Sum(t => t.AbsoluteProfit);
        analysis.RealizedRelativeProfit = totalBought > 0 ? (analysis.RealizedAbsoluteProfit / totalBought) * 100 : 0;
        
        // Calculate open position metrics
        analysis.OpenPositionQuantity = openBuyOperations.Sum(o => o.Quantity);
        analysis.TotalInvested = openBuyOperations.Sum(o => o.Quantity * o.Price);
        analysis.AverageBuyPrice = analysis.OpenPositionQuantity > 0 ? analysis.TotalInvested / analysis.OpenPositionQuantity : 0;
        
        // Calculate unrealized profit (only if current price is provided)
        if (currentPrice > 0 && analysis.OpenPositionQuantity > 0)
        {
            var currentValue = analysis.OpenPositionQuantity * currentPrice;
            analysis.UnrealizedAbsoluteProfit = currentValue - analysis.TotalInvested;
            analysis.UnrealizedRelativeProfit = analysis.TotalInvested > 0 ? (analysis.UnrealizedAbsoluteProfit / analysis.TotalInvested) * 100 : 0;
        }
        
        // Calculate total profit
        analysis.TotalAbsoluteProfit = analysis.RealizedAbsoluteProfit + analysis.UnrealizedAbsoluteProfit;
        var totalInvestment = totalBought + analysis.TotalInvested;
        analysis.TotalRelativeProfit = totalInvestment > 0 ? (analysis.TotalAbsoluteProfit / totalInvestment) * 100 : 0;
        
        analysis.CompletedTrades = completedTrades;
        
        return analysis;
    }

    public void LogProfitAnalysis(ProfitAnalysis analysis)
    {
        Logger.LogInformation("=== PROFIT ANALYSIS ===");
        Logger.LogInformation($"Total Absolute Profit: {analysis.TotalAbsoluteProfit:F4}");
        Logger.LogInformation($"Total Relative Profit: {analysis.TotalRelativeProfit:F2}%");
        Logger.LogInformation("--- Realized Profit ---");
        Logger.LogInformation($"Realized Absolute Profit: {analysis.RealizedAbsoluteProfit:F4}");
        Logger.LogInformation($"Realized Relative Profit: {analysis.RealizedRelativeProfit:F2}%");
        Logger.LogInformation("--- Unrealized Profit ---");
        Logger.LogInformation($"Unrealized Absolute Profit: {analysis.UnrealizedAbsoluteProfit:F4}");
        Logger.LogInformation($"Unrealized Relative Profit: {analysis.UnrealizedRelativeProfit:F2}%");
        Logger.LogInformation("--- Position Info ---");
        Logger.LogInformation($"Open Position Quantity: {analysis.OpenPositionQuantity}");
        Logger.LogInformation($"Total Invested in Open Positions: {analysis.TotalInvested:F4}");
        Logger.LogInformation($"Average Buy Price: {analysis.AverageBuyPrice:F4}");
        Logger.LogInformation($"Completed Trades: {analysis.CompletedTrades.Count}");
        
        if (analysis.CompletedTrades.Any())
        {
            Logger.LogInformation("--- Recent Completed Trades ---");
            var recentTrades = analysis.CompletedTrades.TakeLast(5);
            foreach (var trade in recentTrades)
            {
                Logger.LogInformation($"Trade: {trade.Quantity} lots @ {trade.BuyPrice:F4} -> {trade.SellPrice:F4}, " +
                    $"Profit: {trade.AbsoluteProfit:F4} ({trade.RelativeProfit:F2}%), " +
                    $"Period: {(trade.SellDate - trade.BuyDate).TotalMinutes:F1}min");
            }
        }
        Logger.LogInformation("=======================");
    }
}

public class ProfitAnalysis
{
    public decimal TotalAbsoluteProfit { get; set; }
    public decimal TotalRelativeProfit { get; set; }
    public decimal RealizedAbsoluteProfit { get; set; }
    public decimal RealizedRelativeProfit { get; set; }
    public decimal UnrealizedAbsoluteProfit { get; set; }
    public decimal UnrealizedRelativeProfit { get; set; }
    public decimal TotalInvested { get; set; }
    public long OpenPositionQuantity { get; set; }
    public decimal AverageBuyPrice { get; set; }
    public List<TradeProfit> CompletedTrades { get; set; } = new List<TradeProfit>();
}

public class TradeProfit
{
    public DateTime BuyDate { get; set; }
    public DateTime SellDate { get; set; }
    public long Quantity { get; set; }
    public decimal BuyPrice { get; set; }
    public decimal SellPrice { get; set; }
    public decimal AbsoluteProfit { get; set; }
    public decimal RelativeProfit { get; set; }
}