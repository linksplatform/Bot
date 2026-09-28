namespace TraderBot;

/// <summary>Plans value transfers in a single valuation currency; never places orders.</summary>
public static class PortfolioBalanceAlgorithm
{
    public static IReadOnlyList<PortfolioBalanceAction> Calculate(
        IReadOnlyDictionary<string, decimal> currentValues,
        IReadOnlyDictionary<string, decimal> targetPercentages)
    {
        ArgumentNullException.ThrowIfNull(currentValues);
        ValidateAllocations(targetPercentages);
        ValidateAssets(currentValues, nameof(currentValues));
        if (currentValues.Values.Any(value => value < 0))
            throw new ArgumentException("Asset values cannot be negative.", nameof(currentValues));

        // Treat identifiers consistently even when the caller uses a custom dictionary comparer.
        currentValues = new Dictionary<string, decimal>(currentValues, StringComparer.Ordinal);
        targetPercentages = new Dictionary<string, decimal>(targetPercentages, StringComparer.Ordinal);
        var total = currentValues.Values.Sum();
        var assets = currentValues.Keys.Union(targetPercentages.Keys, StringComparer.Ordinal)
            .OrderBy(asset => asset, StringComparer.Ordinal).ToArray();
        var targetValues = new Dictionary<string, decimal>(StringComparer.Ordinal);
        var positiveTargets = assets.Where(asset => targetPercentages.GetValueOrDefault(asset) > 0).ToArray();
        var remaining = total;
        for (var i = 0; i < positiveTargets.Length; i++)
        {
            var asset = positiveTargets[i];
            // Allocate from the remaining share to conserve value without negative rounding dust.
            var target = i == positiveTargets.Length - 1
                ? remaining : Math.Min(remaining, PercentageOf(total, targetPercentages[asset]));
            targetValues[asset] = target;
            remaining -= target;
        }

        return Array.AsReadOnly(assets.Select(asset => new PortfolioBalanceAction(
            asset,
            currentValues.GetValueOrDefault(asset),
            targetPercentages.GetValueOrDefault(asset),
            targetValues.GetValueOrDefault(asset))).ToArray());
    }

    private static decimal PercentageOf(decimal total, decimal percentage)
    {
        // Preserve tiny percentages by multiplying first whenever the product fits.
        // Scaling the total first is safe when only that product would overflow.
        if (percentage <= 1m || total <= decimal.MaxValue / percentage)
            return total * percentage / 100m;
        return (total / 100m) * percentage;
    }

    internal static void ValidateAllocations(IReadOnlyDictionary<string, decimal> allocations)
    {
        ArgumentNullException.ThrowIfNull(allocations);
        ValidateAssets(allocations, nameof(allocations));
        if (allocations.Count == 0 || allocations.Values.Any(value => value < 0 || value > 100) ||
            allocations.Values.Sum() != 100m)
            throw new ArgumentException("Target percentages must be between 0 and 100 and sum to 100.", nameof(allocations));
    }

    private static void ValidateAssets(IReadOnlyDictionary<string, decimal> assets, string parameter)
    {
        if (assets.Keys.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Asset identifiers cannot be empty or whitespace.", parameter);
    }
}

public sealed record PortfolioBalanceAction(
    string Asset, decimal CurrentValue, decimal TargetPercent, decimal TargetValue)
{
    /// <summary>Positive means buy/add value; negative means sell/remove value.</summary>
    public decimal ValueChange => TargetValue - CurrentValue;
    public string Action => ValueChange > 0 ? "Buy" : ValueChange < 0 ? "Sell" : "Hold";
}
