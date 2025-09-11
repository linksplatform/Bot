# Custom Buy Price Test Cases

## Test Scenario 1: Feature Disabled
- `EnableCustomBuyPrice`: false
- `bestBid`: 5.300
- `bestAsk`: 5.320
- **Expected Result**: 5.300 (should return bestBid)

## Test Scenario 2: Feature Enabled - 50% Spread
- `EnableCustomBuyPrice`: true
- `CustomBuyPriceSpreadPercentage`: 50.0
- `MaxCustomBuyPriceSteps`: 10
- `PriceStep`: 0.001
- `bestBid`: 5.300
- `bestAsk`: 5.320
- **Spread**: 0.020
- **50% of spread**: 0.010
- **Expected Result**: 5.310 (bestBid + 50% of spread)

## Test Scenario 3: Feature Enabled - Limited by MaxSteps
- `EnableCustomBuyPrice`: true
- `CustomBuyPriceSpreadPercentage`: 50.0
- `MaxCustomBuyPriceSteps`: 5
- `PriceStep`: 0.001
- `bestBid`: 5.300
- `bestAsk`: 5.350
- **Spread**: 0.050
- **50% of spread**: 0.025
- **Max allowed increase**: 5 * 0.001 = 0.005
- **Expected Result**: 5.305 (bestBid + maxSteps, capped)

## Test Scenario 4: Feature Enabled - Limited by bestAsk
- `EnableCustomBuyPrice`: true
- `CustomBuyPriceSpreadPercentage`: 100.0
- `MaxCustomBuyPriceSteps`: 100
- `PriceStep`: 0.001
- `bestBid`: 5.300
- `bestAsk`: 5.310
- **Spread**: 0.010
- **100% of spread**: 0.010
- **Expected Result**: 5.310 (limited by bestAsk)

This feature allows traders to:
1. Avoid long queues at the best bid price
2. Get faster execution by paying a premium (crossing the spread partially)
3. Control the maximum premium they're willing to pay
4. Maintain the existing behavior when disabled