using Platform.Data.Doublets;
using TraderBot;
using Xunit;

public class PortfolioBalanceTests
{
    private static Dictionary<string, decimal> Targets() => new()
        { ["Gold"] = 25m, ["USD"] = 25m, ["TCS Group stocks"] = 50m };
    private static Dictionary<string, decimal> Values() => new()
        { ["Gold"] = 10000m, ["USD"] = 30000m, ["TCS Group stocks"] = 60000m };

    [Fact]
    public void CalculatesTheRequestedPortfolioAndConservesValue()
    {
        var plan = PortfolioBalanceAlgorithm.Calculate(Values(), Targets()).ToDictionary(action => action.Asset);
        Assert.Equal(25000m, plan["Gold"].TargetValue);
        Assert.Equal(15000m, plan["Gold"].ValueChange);
        Assert.Equal("Buy", plan["Gold"].Action);
        Assert.Equal(-5000m, plan["USD"].ValueChange);
        Assert.Equal(-10000m, plan["TCS Group stocks"].ValueChange);
        Assert.Equal("Sell", plan["USD"].Action);
        Assert.Equal(0m, plan.Values.Sum(action => action.ValueChange));
    }

    [Fact]
    public void BalancedPortfolioHoldsAllAssets()
    {
        var plan = PortfolioBalanceAlgorithm.Calculate(new Dictionary<string, decimal>
            { ["Gold"] = 25m, ["USD"] = 25m, ["TCS Group stocks"] = 50m }, Targets());
        Assert.All(plan, action => Assert.Equal("Hold", action.Action));
    }

    [Fact]
    public void AbsentTargetAssetIsBoughtAndUnallocatedHoldingIsSold()
    {
        var plan = PortfolioBalanceAlgorithm.Calculate(new Dictionary<string, decimal> { ["Old"] = 10m },
            new Dictionary<string, decimal> { ["New"] = 100m }).ToDictionary(action => action.Asset);
        Assert.Equal(10m, plan["New"].ValueChange);
        Assert.Equal(-10m, plan["Old"].ValueChange);
        Assert.Equal(0m, plan["Old"].TargetPercent);
    }

    [Fact]
    public void EmptyPortfolioReturnsZeroValuedTargets()
    {
        var plan = PortfolioBalanceAlgorithm.Calculate(new Dictionary<string, decimal>(), Targets());
        Assert.Equal(3, plan.Count);
        Assert.All(plan, action => Assert.Equal(0m, action.ValueChange));
    }

    [Fact]
    public void FractionalPercentagesKeepAllValueAndDoNotProduceNegativeTargets()
    {
        var targets = new Dictionary<string, decimal>
            { ["A"] = 33.33333333333333333333333333m, ["B"] = 66.66666666666666666666666666m,
              ["C"] = 0.00000000000000000000000001m };
        var plan = PortfolioBalanceAlgorithm.Calculate(new Dictionary<string, decimal> { ["A"] = 1m }, targets);
        Assert.Equal(1m, plan.Sum(action => action.TargetValue));
        Assert.Equal(0m, plan.Sum(action => action.ValueChange));
        Assert.All(plan, action => Assert.True(action.TargetValue >= 0m));
    }

    [Fact]
    public void LargePortfolioDoesNotOverflowDuringPercentageMultiplication()
    {
        var plan = PortfolioBalanceAlgorithm.Calculate(new Dictionary<string, decimal> { ["A"] = decimal.MaxValue },
            new Dictionary<string, decimal> { ["A"] = 50m, ["B"] = 50m });
        Assert.Equal(decimal.MaxValue, plan.Sum(action => action.TargetValue));
    }

    [Fact]
    public void UsesOrdinalAssetIdentityRegardlessOfInputComparer()
    {
        var values = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["USD"] = 10m };
        var plan = PortfolioBalanceAlgorithm.Calculate(values,
            new Dictionary<string, decimal> { ["usd"] = 100m });
        Assert.Equal(2, plan.Count);
        Assert.Equal(10m, plan.Sum(action => action.CurrentValue));
        Assert.Equal(0m, plan.Sum(action => action.ValueChange));
    }

    [Theory]
    [InlineData(-1, 101)]
    [InlineData(101, -1)]
    [InlineData(20, 20)]
    [InlineData(100, 1)]
    public void RejectsInvalidPercentages(int first, int second)
        => Assert.Throws<ArgumentException>(() => PortfolioBalanceAlgorithm.Calculate(Values(),
            new Dictionary<string, decimal> { ["A"] = first, ["B"] = second }));

    [Fact]
    public void RejectsEmptyTargets()
        => Assert.Throws<ArgumentException>(() => PortfolioBalanceAlgorithm.Calculate(Values(), new Dictionary<string, decimal>()));

    [Fact]
    public void RejectsNegativeHoldings()
        => Assert.Throws<ArgumentException>(() => PortfolioBalanceAlgorithm.Calculate(
            new Dictionary<string, decimal> { ["Gold"] = -1m }, Targets()));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void RejectsBlankAssetNames(string asset)
    {
        Assert.Throws<ArgumentException>(() => PortfolioBalanceAlgorithm.Calculate(
            new Dictionary<string, decimal> { [asset] = 1m }, Targets()));
        Assert.Throws<ArgumentException>(() => PortfolioBalanceAlgorithm.Calculate(Values(),
            new Dictionary<string, decimal> { [asset] = 100m }));
    }

    [Fact]
    public void DeepStorageReadbackWorksFromAnotherWrapper()
    {
        var financial = new FinancialStorage();
        var first = new PortfolioAllocationStorage(financial);
        var snapshot = first.Save("Example", Targets());
        var second = new PortfolioAllocationStorage(financial);
        Assert.Equal(Targets().OrderBy(pair => pair.Key), second.Read(snapshot).OrderBy(pair => pair.Key));
        Assert.Equal(PortfolioBalanceAlgorithm.Calculate(Values(), Targets()), second.Calculate(snapshot, Values()));
        var memberships = new List<ulong>();
        financial.Storage.Each(new Link<ulong>(financial.Storage.Constants.Any, snapshot, financial.Storage.Constants.Any), link =>
        {
            memberships.Add(financial.Storage.GetTarget(link));
            return financial.Storage.Constants.Continue;
        });
        Assert.Equal(3, memberships.Count);
    }

    [Fact]
    public void AllocationSnapshotsStayIndependentAndReadOnly()
    {
        var store = new PortfolioAllocationStorage(new FinancialStorage());
        var oldSnapshot = store.Save("Same portfolio", Targets());
        var newSnapshot = store.Save("Same portfolio", new Dictionary<string, decimal> { ["USD"] = 100m });
        Assert.NotEqual(oldSnapshot, newSnapshot);
        Assert.Equal(3, store.Read(oldSnapshot).Count);
        Assert.Equal(100m, store.Read(newSnapshot)["USD"]);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, decimal>)store.Read(oldSnapshot)).Add("Fake", 1m));
    }

    [Fact]
    public void DeepStorageSupportsZeroAndFractionalPercentages()
    {
        var store = new PortfolioAllocationStorage(new FinancialStorage());
        var input = new Dictionary<string, decimal> { ["Gold"] = 12.5m, ["Cash"] = 87.5m, ["Other"] = 0m };
        Assert.Equal(input.OrderBy(pair => pair.Key), store.Read(store.Save("Fractional", input)).OrderBy(pair => pair.Key));
    }

    [Fact]
    public void RejectsInvalidOrUnrelatedSnapshot()
    {
        var financial = new FinancialStorage();
        var store = new PortfolioAllocationStorage(financial);
        Assert.Throws<ArgumentException>(() => store.Read(0));
        Assert.Throws<ArgumentException>(() => store.Read(financial.Type));
        Assert.Throws<ArgumentException>(() => store.Save(" ", Targets()));
    }

    [Fact]
    public void InvalidAllocationDoesNotWritePartialDeepSnapshot()
    {
        var financial = new FinancialStorage();
        var store = new PortfolioAllocationStorage(financial);
        var before = financial.Storage.Count(new Link<ulong>(financial.Storage.Constants.Any, financial.Storage.Constants.Any, financial.Storage.Constants.Any));
        Assert.Throws<ArgumentException>(() => store.Save("Invalid", new Dictionary<string, decimal> { ["Gold"] = 1m }));
        Assert.Equal(before, financial.Storage.Count(new Link<ulong>(financial.Storage.Constants.Any, financial.Storage.Constants.Any, financial.Storage.Constants.Any)));
    }

    [Fact]
    public void OfflineCommandReadsFixtureWithoutBrokerConfiguration()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """
                {"PortfolioName":"Demo","TargetPercentages":{"Gold":25,"USD":25,"TCS Group stocks":50},
                 "CurrentValues":{"Gold":10000,"USD":30000,"TCS Group stocks":60000}}
                """);
            var output = new StringWriter();
            var error = new StringWriter();
            Assert.Equal(0, PortfolioBalanceCommand.Run(new[] { "--balance-portfolio", path }, output, error));
            Assert.Contains("15000", output.ToString());
            Assert.Equal("", error.ToString());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void OfflineCommandRejectsMissingPathAndMalformedInput()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        Assert.Equal(1, PortfolioBalanceCommand.Run(new[] { "--balance-portfolio" }, output, error));
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "{}");
            Assert.Equal(1, PortfolioBalanceCommand.Run(new[] { "--balance-portfolio", path }, output, error));
        }
        finally { File.Delete(path); }
    }
}
