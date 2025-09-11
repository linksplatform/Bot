namespace TraderBot;

public static class KellyCriterion
{
    /// <summary>
    /// Calculates the optimal bet size fraction using the Kelly Criterion formula.
    /// Formula: f = (bp - q) / b
    /// Where:
    /// - f = fraction of capital to bet
    /// - b = profit/loss ratio (odds)
    /// - p = probability of winning
    /// - q = probability of losing (1-p)
    /// </summary>
    /// <param name="winProbability">Probability of winning (0.0 to 1.0)</param>
    /// <param name="profitLossRatio">The ratio of profit to loss (e.g., 2.0 means profit is 2x the loss)</param>
    /// <param name="maxFraction">Maximum fraction to limit risk (default 0.25)</param>
    /// <returns>The optimal fraction of capital to bet (0.0 to maxFraction)</returns>
    public static double CalculateOptimalBetSize(double winProbability, double profitLossRatio, double maxFraction = 0.25)
    {
        if (winProbability < 0 || winProbability > 1)
            throw new ArgumentException("Win probability must be between 0 and 1", nameof(winProbability));
        
        if (profitLossRatio <= 0)
            throw new ArgumentException("Profit/loss ratio must be positive", nameof(profitLossRatio));
        
        if (maxFraction <= 0 || maxFraction > 1)
            throw new ArgumentException("Max fraction must be between 0 and 1", nameof(maxFraction));

        double lossProbability = 1.0 - winProbability;
        
        // Kelly Criterion formula: f = (bp - q) / b
        double kellyFraction = (profitLossRatio * winProbability - lossProbability) / profitLossRatio;
        
        // Return 0 if Kelly suggests negative betting (negative expected value)
        if (kellyFraction <= 0)
            return 0.0;
        
        // Cap at maximum fraction to limit risk
        return Math.Min(kellyFraction, maxFraction);
    }

    /// <summary>
    /// Calculates the win probability and profit/loss ratio from historical operations
    /// </summary>
    /// <param name="operations">List of completed operations</param>
    /// <returns>Tuple containing (winProbability, profitLossRatio)</returns>
    public static (double WinProbability, double ProfitLossRatio) CalculateHistoricalMetrics(
        IEnumerable<(DateTime Date, decimal BuyPrice, decimal SellPrice)> operations)
    {
        var operationsList = operations.ToList();
        if (operationsList.Count < 10) // Need minimum historical data
            return (0.5, 1.0); // Default conservative values

        var wins = 0;
        var totalProfit = 0.0m;
        var totalLoss = 0.0m;

        foreach (var op in operationsList)
        {
            var profit = op.SellPrice - op.BuyPrice;
            if (profit > 0)
            {
                wins++;
                totalProfit += profit;
            }
            else if (profit < 0)
            {
                totalLoss += Math.Abs(profit);
            }
        }

        var winProbability = (double)wins / operationsList.Count;
        var avgProfit = wins > 0 ? (double)(totalProfit / wins) : 0.0;
        var avgLoss = (operationsList.Count - wins) > 0 ? (double)(totalLoss / (operationsList.Count - wins)) : 1.0;
        var profitLossRatio = avgLoss > 0 ? avgProfit / avgLoss : 1.0;

        return (winProbability, Math.Max(profitLossRatio, 0.1)); // Minimum ratio to avoid division issues
    }
}