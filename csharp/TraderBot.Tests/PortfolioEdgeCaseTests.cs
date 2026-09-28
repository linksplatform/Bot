using System.Globalization;
using TraderBot;
using Xunit;

public class PortfolioEdgeCaseTests
{
    [Fact]
    public void TinyValidPercentageRetainsRepresentableTargetValue()
    {
        var plan = PortfolioBalanceAlgorithm.Calculate(
            new Dictionary<string, decimal> { ["B"] = 10000000000000000000000000000m },
            new Dictionary<string, decimal>
            {
                ["A"] = 0.000000000000000000000000001m,
                ["B"] = 50m,
                ["C"] = 49.999999999999999999999999999m
            }).ToDictionary(x => x.Asset);
        Assert.Equal(0.1m, plan["A"].TargetValue);
        Assert.Equal(10000000000000000000000000000m, plan.Values.Sum(x => x.TargetValue));
    }

    [Fact]
    public void UnicodeAllocationsRoundTripUnderNonEnglishCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var store = new PortfolioAllocationStorage(new FinancialStorage());
            var targets = new Dictionary<string, decimal> { ["Or 🔆"] = 12.5m, ["現金"] = 87.5m };
            var snapshot = store.Save("Portefeuille 📈", targets);
            var saved = store.Read(snapshot);
            Assert.Equal(12.5m, saved["Or 🔆"]);
            Assert.Equal(87.5m, saved["現金"]);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }
}
