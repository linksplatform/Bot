# Kelly Criterion Implementation for TraderBot

## Overview

This implementation adds Kelly Criterion position sizing to the TraderBot, enabling optimal capital allocation to maximize long-term profit while managing risk. The Kelly Criterion was developed by John Kelly at Bell Labs in 1956 and is widely used by professional traders and investors.

## Mathematical Formula

The Kelly Criterion calculates the optimal fraction of capital to risk using:

```
f = (bp - q) / b
```

Where:
- `f` = fraction of capital to bet/invest
- `b` = profit/loss ratio (odds received)
- `p` = probability of winning
- `q` = probability of losing (1-p)

## Key Features

### 1. Optimal Position Sizing
- Calculates optimal lot size based on historical performance or configured parameters
- Prevents over-betting and under-betting
- Maximizes long-term geometric growth rate

### 2. Dynamic Learning
- Automatically calculates win probability and profit/loss ratio from completed trades
- Adapts position sizing based on actual performance
- Falls back to configured values when insufficient historical data

### 3. Risk Management
- Configurable maximum fraction limit (default 25%) to prevent excessive risk
- Returns 0% allocation for negative expected value strategies
- Built-in safeguards against calculation errors

### 4. Comprehensive Configuration
- Toggle Kelly Criterion on/off per trading instrument
- Configure initial win probability and profit/loss ratio estimates
- Set maximum risk fraction limits

## Configuration Parameters

Add these parameters to your `appsettings.json` under `TradingSettings`:

```json
{
  "TradingSettings": {
    // ... existing settings ...
    "UseKellyCriterion": true,
    "WinProbability": 0.55,
    "ProfitLossRatio": 1.2,
    "KellyFractionLimit": 0.25
  }
}
```

### Parameter Details

- **UseKellyCriterion**: Enable/disable Kelly position sizing (default: false)
- **WinProbability**: Initial estimate of win rate (0.0 to 1.0)
- **ProfitLossRatio**: Initial estimate of average profit to average loss ratio
- **KellyFractionLimit**: Maximum fraction of capital to risk (0.0 to 1.0, default: 0.25)

## Real-World Examples

### Conservative Trading (Example 1)
- Win Rate: 52%
- Profit/Loss Ratio: 1.1:1
- Max Risk: 10%
- **Result: 8.4% of capital per trade**

### Aggressive Trading (Example 2)
- Win Rate: 65%
- Profit/Loss Ratio: 1.5:1
- Max Risk: 50%
- **Result: 41.7% of capital per trade**

### High Win Rate, Low Profit (Example 3)
- Win Rate: 80%
- Profit/Loss Ratio: 0.8:1
- Max Risk: 25%
- **Result: 25.0% of capital per trade (capped)**

### Breakeven Strategy (Example 4)
- Win Rate: 50%
- Profit/Loss Ratio: 1.0:1
- Max Risk: 25%
- **Result: 0.0% of capital per trade (negative expected value)**

## Implementation Details

### Core Components

1. **KellyCriterion.cs**: Static calculation methods
2. **TradingSettings.cs**: Configuration parameters
3. **TradingService.cs**: Integration with existing trading logic

### Key Methods

- `CalculateOptimalBetSize()`: Main Kelly calculation
- `CalculateHistoricalMetrics()`: Dynamic learning from trade history
- `CalculateOptimalLotSize()`: Integration with existing lot sizing
- `TrackCompletedOperation()`: Trade history tracking

### Behavior Changes

When Kelly Criterion is enabled:
1. **Position Sizing**: Uses Kelly formula instead of "all available cash"
2. **Risk Management**: Automatically reduces position size for poor-performing strategies
3. **Learning**: Adapts to actual performance over time
4. **Logging**: Provides detailed Kelly calculation information

When Kelly Criterion is disabled:
- Falls back to original position sizing logic
- No behavior changes to existing functionality

## Testing

Comprehensive test suite includes:
- Mathematical accuracy tests
- Edge case handling
- Historical metrics calculation
- Real-world scenario simulations

Run tests with:
```bash
cd examples
dotnet run
```

## Risk Considerations

### Important Warnings

1. **Accurate Probabilities Required**: Kelly Criterion requires accurate estimates of win probability and profit/loss ratios. Overestimating leads to excessive risk.

2. **Volatility**: Kelly sizing can be volatile. Consider using fractional Kelly (e.g., 50% of calculated size) for smoother equity curves.

3. **Historical Data**: Algorithm needs minimum 10 completed trades for dynamic learning. Uses configured values otherwise.

4. **Market Conditions**: Kelly assumes consistent market conditions. Performance may vary during regime changes.

### Best Practices

- Start with conservative estimates (lower win rates, profit/loss ratios)
- Use fractional Kelly (25% or less) to reduce volatility
- Monitor performance and adjust parameters based on actual results
- Maintain diverse trading strategies to spread risk

## Advanced Usage

### Fractional Kelly

Many professional traders use fractional Kelly to reduce volatility:
- Full Kelly: Use calculated fraction
- Half Kelly: Use 50% of calculated fraction
- Quarter Kelly: Use 25% of calculated fraction

Set `KellyFractionLimit` to implement fractional Kelly.

### Dynamic Adjustment

The system automatically switches to historical metrics after 10+ completed trades:
- Improves accuracy over time
- Adapts to changing market conditions
- Provides more reliable position sizing

## References

- Kelly, J. L. (1956). "A New Interpretation of Information Rate"
- Thorp, E. O. (2006). "The Kelly Capital Growth Investment Criterion"
- MacLean, L. C., Thorp, E. O., & Ziemba, W. T. (2011). "The Kelly Capital Growth Investment Criterion: Theory and Practice"

## Support

For questions or issues related to Kelly Criterion implementation:
1. Review configuration parameters
2. Check log output for Kelly calculation details
3. Run test suite to verify functionality
4. Ensure minimum trade history for dynamic learning