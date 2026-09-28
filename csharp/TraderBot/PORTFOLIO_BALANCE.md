# Portfolio allocation and balance planning

Define a target percentage for each asset and supply its current market value in
one common valuation currency. For example, issue #138's 25% Gold, 25% USD and
50% TCS Group stocks allocation can be evaluated with the included fixture:

```sh
dotnet run --project csharp/TraderBot -- --balance-portfolio csharp/TraderBot/portfolio-balance.example.json
```

This explicit command runs before the trading host is constructed. It requires
no broker account, access token or market-data connection and places no orders.
It saves target allocations into the existing Deep associative store, reads them
back, and prints the calculated plan as JSON. The example returns:

| Asset | Current value | Target value | Value change |
|---|---:|---:|---:|
| Gold | 10000 | 25000 | +15000 |
| USD | 30000 | 25000 | -5000 |
| TCS Group stocks | 60000 | 50000 | -10000 |

`ValueChange` is an amount in the supplied valuation currency, **not a quantity
of shares**. Positive values are labelled `Buy`, negative values `Sell`, and zero
values `Hold`. Names are opaque, case-sensitive asset identifiers. No price or
exchange-rate assumptions are made: value all assets, including cash, consistently
before supplying the input. Output contains each current or target asset once,
sorted by its ordinal identifier.

## C# API

```csharp
var financial = new FinancialStorage();
var allocations = new PortfolioAllocationStorage(financial);
var snapshot = allocations.Save("Example", new Dictionary<string, decimal>
{
    ["Gold"] = 25m,
    ["USD"] = 25m,
    ["TCS Group stocks"] = 50m
});
var plan = allocations.Calculate(snapshot, new Dictionary<string, decimal>
{
    ["Gold"] = 10000m,
    ["USD"] = 30000m,
    ["TCS Group stocks"] = 60000m
});
```

Targets must sum to exactly 100%, and each must be between 0 and 100. Negative
current values and blank identifiers are rejected. Missing target assets have
zero current value; holdings absent from the target set receive a zero target
and a sell value. An empty/zero-value portfolio produces zero-value actions.
Calculations use `decimal`; the final positive target receives any rounding
remainder so total value is conserved. Amounts are not rounded to a broker's
currency precision or lot size. Inputs whose total exceeds `decimal.MaxValue`
are rejected by overflow rather than producing an invalid plan.

Each `Save` creates an immutable snapshot. Changing a portfolio's targets means
saving a new snapshot; earlier snapshots remain readable. A new
`PortfolioAllocationStorage` wrapper around the same `FinancialStorage` can
read the saved targets and calculate a plan without relying on a dictionary
cached by the first wrapper.

## Deep relationships

The graph uses the following doublet relationships:

```text
portfolio      = (PortfolioBalancePortfolio type, portfolio-name sequence)
snapshot       = (PortfolioBalanceAllocations type, unique point)
membership     = (portfolio, snapshot)
percentage     = (PortfolioBalancePercentage type, invariant-decimal sequence)
asset target   = (asset-name sequence, percentage)
allocation     = (snapshot, asset target)
```

Percentages are encoded losslessly as typed invariant decimal text. This includes
zero and fractional values and is independent of the machine's current culture.
Both the asset identifier and its percentage are reachable from the snapshot.

`FinancialStorage` currently uses heap memory. These graph snapshots persist
within that store's lifetime; they are **not written to disk** and link IDs must
not be reused in a different store or process. Use one thread per store, matching
the existing storage's access model.

## Verification

```sh
dotnet test csharp/TraderBot.Tests/TraderBot.Tests.csproj
```

The tests use only synthetic portfolio data and in-memory Deep storage. They
exercise the calculation, validation, exact example, rounding conservation,
snapshot associations/readback, immutable updates, and offline command. The test
project is included in both solution files.

Broker execution, live market valuation, fees, currency conversion, lot sizing
and order reconciliation are outside this planning algorithm. The existing
scalping host is not connected to these plans and its behavior is unchanged.
