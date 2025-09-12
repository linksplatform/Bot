# Portfolio Balance Algorithm

This document describes the portfolio auto-balance algorithm implementation for the TraderBot that uses Deep (associative data storage).

## Overview

The portfolio balance algorithm automatically rebalances a trading portfolio according to predefined asset allocation percentages. This feature allows traders to maintain their desired risk/return profile by automatically adjusting positions when allocations drift beyond specified thresholds.

## Features

### Core Functionality
- **Automatic Portfolio Analysis**: Continuously monitors current asset allocations vs. target percentages
- **Deep Storage Integration**: Uses associative data storage (Platform.Data.Doublets) to store portfolio state and decisions
- **Configurable Thresholds**: Customizable rebalance triggers and check intervals
- **Multi-Asset Support**: Handles various asset types (ETFs, Shares, Cash, etc.)
- **Risk Management**: Only rebalances when deviations exceed specified thresholds

### Deep Storage Benefits
- **Associative Data Model**: Portfolio relationships stored as links between concepts
- **Rational Number Precision**: Exact decimal calculations without floating-point errors
- **Query Capabilities**: Efficient queries for portfolio analysis and historical tracking
- **Data Persistence**: Portfolio state and rebalance history maintained across restarts

## Configuration

### Basic Setup
Add portfolio balance configuration to your `appsettings.json`:

```json
{
  "TradingSettings": {
    "PortfolioBalance": {
      "Enabled": true,
      "RebalanceThresholdPercent": 5.0,
      "RebalanceCheckInterval": "00:10:00",
      "AssetAllocations": [
        {
          "AssetType": "Gold",
          "Ticker": "TGLD",
          "TargetPercent": 25.0,
          "Instrument": "Etf"
        },
        {
          "AssetType": "USD", 
          "Ticker": "FXMM",
          "TargetPercent": 25.0,
          "Instrument": "Etf"
        },
        {
          "AssetType": "TCS Group stocks",
          "Ticker": "TCSG", 
          "TargetPercent": 50.0,
          "Instrument": "Shares"
        }
      ]
    }
  }
}
```

### Configuration Parameters
- **Enabled**: Whether portfolio balancing is active
- **RebalanceThresholdPercent**: Minimum deviation (%) required to trigger rebalancing
- **RebalanceCheckInterval**: How often to analyze portfolio (format: HH:MM:SS)
- **AssetAllocations**: Array of target allocations for each asset

## Algorithm Details

### Portfolio Analysis Process
1. **Portfolio Snapshot**: Retrieve current positions and values
2. **Allocation Calculation**: Calculate current percentage allocations
3. **Deviation Analysis**: Compare current vs. target allocations
4. **Threshold Check**: Identify assets exceeding rebalance thresholds
5. **Action Generation**: Create buy/sell actions to restore target allocations
6. **Deep Storage**: Store analysis results in associative format

### Rebalance Logic
```
For each asset:
  current_percent = (current_value / total_portfolio_value) * 100
  deviation = |current_percent - target_percent|
  
  if deviation > threshold_percent:
    target_value = total_portfolio_value * (target_percent / 100)
    amount_to_rebalance = target_value - current_value
    
    action = amount_to_rebalance > 0 ? BUY : SELL
    store_in_deep_storage(asset, deviation, action, amount)
```

### Deep Storage Schema
The algorithm stores portfolio data using these associative relationships:
- **Asset → TargetAllocation**: Links assets to their target percentages
- **Asset → CurrentAllocation**: Links assets to their current percentages  
- **Asset → RebalanceAction**: Links assets to required rebalance amounts
- **Portfolio → Type**: Categorizes different portfolio data types

## Example Usage

### Running the Algorithm
The algorithm runs automatically as part of the TradingService when enabled. It:
1. Checks portfolio balance at configured intervals
2. Logs analysis results and required actions
3. Executes rebalancing for instruments managed by current trading instance
4. Stores all decisions and state in Deep storage

### Sample Output
```
[10:00:00] Starting portfolio balance analysis
[10:00:01] Asset TGLD: Current 15.2%, Target 25.0%, Deviation 9.8%
[10:00:01] Asset FXMM: Current 28.5%, Target 25.0%, Deviation 3.5%
[10:00:01] Asset TCSG: Current 56.3%, Target 50.0%, Deviation 6.3%
[10:00:02] Rebalance needed for TGLD: Buy 9800.00 RUB
[10:00:02] Rebalance needed for TCSG: Sell 6300.00 RUB
[10:00:02] Portfolio analysis complete. 2 rebalance actions identified
```

## Testing

### Unit Tests
Run the included tests to verify algorithm functionality:

```bash
cd TraderBot
dotnet run --test
```

### Test Coverage
- **Portfolio Balance Calculations**: Validates percentage calculations and thresholds
- **Rebalance Action Generation**: Tests buy/sell decision logic
- **Deep Storage Integration**: Verifies associative data storage and retrieval

## Integration Notes

### Multi-Instance Coordination
- Each TradingService instance handles its configured instrument
- Portfolio-wide rebalancing requires coordination between instances
- Deep storage provides shared state for cross-instance communication

### Risk Considerations
- Algorithm respects existing trading rules (time windows, minimum amounts)
- Cash balance validation before executing buy orders
- Position validation before executing sell orders
- Gradual rebalancing to minimize market impact

## Architecture

### Key Components
- **PortfolioBalanceAlgorithm**: Core analysis and decision engine
- **PortfolioBalanceSettings**: Configuration model
- **RebalanceAction**: Action representation with metadata
- **FinancialStorage**: Deep storage interface for portfolio data

### Deep Storage Benefits
- **Exact Arithmetic**: Rational numbers prevent rounding errors
- **Associative Queries**: Efficient relationship-based data access  
- **State Persistence**: Portfolio history maintained across restarts
- **Concurrent Access**: Thread-safe storage for multi-instance scenarios

## Future Enhancements

### Planned Features
- **Historical Analysis**: Track rebalancing performance over time
- **Advanced Strategies**: Support for momentum-based and volatility-adjusted allocations
- **Risk Metrics**: Value-at-Risk and correlation analysis
- **Automated Reporting**: Portfolio performance dashboards

### Deep Storage Extensions
- **Graph Queries**: Complex portfolio relationship analysis
- **Machine Learning**: Pattern recognition for optimal rebalancing timing
- **Distributed Storage**: Multi-node portfolio state synchronization

## Troubleshooting

### Common Issues
1. **Configuration Errors**: Ensure asset allocations sum to 100%
2. **API Access**: Verify Tinkoff InvestAPI credentials and permissions
3. **Storage Issues**: Check Deep storage initialization and memory limits
4. **Timing Conflicts**: Avoid overlapping rebalance intervals

### Debug Information
Enable detailed logging by setting log level to "Debug" in configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "TraderBot.PortfolioBalanceAlgorithm": "Debug"
    }
  }
}
```