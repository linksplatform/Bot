# Multiple Trading Configurations Support

This update adds support for running multiple trading configurations simultaneously on the same TraderBot instance. This allows you to:

- Trade the same instrument on different broker accounts
- Test different trading settings simultaneously
- Trade multiple instruments at the same time
- Use different broker accounts for different configurations

## Configuration Structure

### Single Configuration (Legacy Mode)
The existing single configuration format is still supported for backward compatibility:

```json
{
  "InvestApiSettings": {
    "AccessToken": "your-token",
    "AppName": "your-app-name"
  },
  "TradingSettings": {
    "Instrument": "Etf",
    "Ticker": "TMON@",
    // ... other settings
  }
}
```

### Multiple Configurations (New Mode)
For multiple configurations, use the new format:

```json
{
  "MultiTradingConfiguration": {
    "Configurations": [
      {
        "Name": "TMON_Account1",
        "InvestApiSettings": {
          "AccessToken": "account1-token",
          "AppName": "LinksPlatformScalper_TMON_Account1"
        },
        "TradingSettings": {
          "Instrument": "Etf",
          "Ticker": "TMON@",
          "AccountIndex": 0,
          // ... other settings
        }
      },
      {
        "Name": "TMON_Account2", 
        "InvestApiSettings": {
          "AccessToken": "account2-token",
          "AppName": "LinksPlatformScalper_TMON_Account2"
        },
        "TradingSettings": {
          "Instrument": "Etf",
          "Ticker": "TMON@",
          "AccountIndex": 1,
          // ... different settings
        }
      }
    ]
  }
}
```

## Usage Examples

### Example 1: Same Instrument, Different Accounts
Trade TMON@ on two different broker accounts with different settings:

- Account 1: Conservative settings with MinimumProfitSteps: -1
- Account 2: Aggressive settings with MinimumProfitSteps: -2

### Example 2: Multiple Instruments
Trade different instruments simultaneously:

- Configuration 1: TMON@ on Account 1
- Configuration 2: TRUR on Account 1  
- Configuration 3: TMON@ on Account 2

### Example 3: A/B Testing
Test different trading parameters on the same instrument:

- Configuration 1: Trading hours 00:00-23:59
- Configuration 2: Trading hours 09:00-14:45

## Logging

Each configuration runs independently and logs are prefixed with the configuration name for easy identification:

```
[TMON_Account1] Instrument: Etf
[TMON_Account1] Ticker: TMON@
[TMON_Account2] Instrument: Etf  
[TMON_Account2] Ticker: TMON@
```

## Configuration Files

Sample configuration files are provided:

- `appsettings.TMON.json` - Single TMON configuration (legacy)
- `appsettings.TRUR.json` - Single TRUR configuration (legacy)  
- `appsettings.MultipleConfigurations.json` - Multiple configurations example

## Technical Implementation

- Each configuration gets its own `TradingService` instance
- Each configuration uses a separate `InvestApiClient` instance
- All services run concurrently as hosted services
- Backward compatibility is maintained with existing single configuration setups

## Migration Guide

To migrate from single to multiple configurations:

1. Keep your existing configuration files as-is for backward compatibility
2. Or create a new configuration file using the `MultiTradingConfiguration` format
3. Move `InvestApiSettings` and `TradingSettings` under each configuration object
4. Add a unique `Name` field for each configuration
5. Update any external references to account for the new configuration names