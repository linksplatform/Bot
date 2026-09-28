using System.Globalization;
using Platform.Data;
using Platform.Data.Doublets;

namespace TraderBot;

/// <summary>Stores immutable target-allocation snapshots in the existing Deep link store.</summary>
public sealed class PortfolioAllocationStorage
{
    private readonly FinancialStorage _financial;
    private readonly ulong _portfolioType;
    private readonly ulong _allocationSetType;
    private readonly ulong _percentageType;

    public PortfolioAllocationStorage(FinancialStorage financial)
    {
        ArgumentNullException.ThrowIfNull(financial);
        _financial = financial;
        _portfolioType = financial.GetOrCreateType(financial.Type, "PortfolioBalancePortfolio");
        _allocationSetType = financial.GetOrCreateType(financial.Type, "PortfolioBalanceAllocations");
        _percentageType = financial.GetOrCreateType(financial.Type, "PortfolioBalancePercentage");
    }

    public ulong Save(string portfolioName, IReadOnlyDictionary<string, decimal> targetPercentages)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portfolioName);
        PortfolioBalanceAlgorithm.ValidateAllocations(targetPercentages);
        var links = _financial.Storage;
        var portfolio = links.GetOrCreate(_portfolioType,
            _financial.StringToUnicodeSequenceConverter.Convert(portfolioName));
        var snapshot = links.GetOrCreate(_allocationSetType, links.CreatePoint());
        links.GetOrCreate(portfolio, snapshot);
        foreach (var allocation in targetPercentages.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var asset = _financial.StringToUnicodeSequenceConverter.Convert(allocation.Key);
            // Typed invariant decimal text is lossless, including fractional percentages.
            var percent = links.GetOrCreate(_percentageType,
                _financial.StringToUnicodeSequenceConverter.Convert(allocation.Value.ToString("G29", CultureInfo.InvariantCulture)));
            var assetAllocation = links.GetOrCreate(asset, percent);
            links.GetOrCreate(snapshot, assetAllocation);
        }
        return snapshot;
    }

    public IReadOnlyDictionary<string, decimal> Read(ulong snapshot)
    {
        var links = _financial.Storage;
        if (!links.Exists(snapshot) || links.GetSource(snapshot) != _allocationSetType)
            throw new ArgumentException("Not a portfolio allocation snapshot.", nameof(snapshot));
        var allocations = new Dictionary<string, decimal>(StringComparer.Ordinal);
        links.Each(new Link<ulong>(links.Constants.Any, snapshot, links.Constants.Any), link =>
        {
            var allocation = links.GetTarget(link);
            var asset = _financial.UnicodeSequenceToStringConverter.Convert(links.GetSource(allocation));
            var percentage = links.GetTarget(allocation);
            if (links.GetSource(percentage) != _percentageType)
                throw new InvalidOperationException("Invalid percentage link in allocation snapshot.");
            var percent = decimal.Parse(
                _financial.UnicodeSequenceToStringConverter.Convert(links.GetTarget(percentage)),
                NumberStyles.Float, CultureInfo.InvariantCulture);
            allocations.Add(asset, percent);
            return links.Constants.Continue;
        });
        PortfolioBalanceAlgorithm.ValidateAllocations(allocations);
        return new System.Collections.ObjectModel.ReadOnlyDictionary<string, decimal>(allocations);
    }

    public IReadOnlyList<PortfolioBalanceAction> Calculate(
        ulong snapshot, IReadOnlyDictionary<string, decimal> currentValues)
        => PortfolioBalanceAlgorithm.Calculate(currentValues, Read(snapshot));
}
