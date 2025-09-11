# Trader Bot API Abstraction

This implementation demonstrates how to make the Trader Bot independent of the Tinkoff API by introducing an abstraction layer.

## Key Changes

### 1. API Abstraction Layer
- Created `ITradingApiProvider` interface to abstract trading operations
- Defined interfaces for all trading data structures (`IAccount`, `IInstrument`, `IOrder`, etc.)
- Used custom enums to avoid namespace conflicts with provider-specific APIs

### 2. Provider Implementations
- `TinkoffApiProvider`: Wrapper around the Tinkoff InvestApi
- `MockApiProvider`: Example implementation for testing/demonstration

### 3. Configuration-Based Provider Selection
```json
{
  "ApiProviderSettings": {
    "Provider": "Tinkoff"
  }
}
```

## Usage Examples

### Using Tinkoff Provider
```json
{
  "ApiProviderSettings": {
    "Provider": "Tinkoff"
  },
  "InvestApiSettings": {
    "AccessToken": "your_token_here",
    "AppName": "YourApp"
  }
}
```

### Using Mock Provider (for testing)
```json
{
  "ApiProviderSettings": {
    "Provider": "Mock"
  }
}
```

## Adding New Providers

To add support for a new trading API (e.g., Interactive Brokers, Alpaca):

1. Create a new provider class implementing `ITradingApiProvider`
2. Implement all required wrapper classes for data structures
3. Add the provider to the dependency injection configuration
4. Add any provider-specific settings classes

Example:
```csharp
public class AlpacaApiProvider : ITradingApiProvider
{
    // Implementation here
}

// In Program.cs:
case "alpaca":
    var alpacaSettings = provider.GetRequiredService<AlpacaSettings>();
    var alpacaLogger = provider.GetRequiredService<ILogger<AlpacaApiProvider>>();
    return new AlpacaApiProvider(alpacaSettings, alpacaLogger);
```

## Benefits

1. **Reduced Dependency Risk**: No longer tied to a single API provider
2. **Easy Testing**: Mock provider allows testing without real API calls
3. **Provider Flexibility**: Can switch providers via configuration
4. **Future-Proof**: Easy to add new trading APIs as needed
5. **Isolation**: API instability only affects the specific provider implementation

## Files Created

### Interfaces
- `ITradingApiProvider.cs` - Main provider interface
- `IAccount.cs`, `IInstrument.cs`, `IOrder.cs`, etc. - Data structure interfaces

### Tinkoff Implementation
- `TinkoffApiProvider.cs` - Main Tinkoff provider
- `Tinkoff/TinkoffAccount.cs`, `Tinkoff/TinkoffOrder.cs`, etc. - Tinkoff-specific wrappers

### Mock Implementation
- `MockApiProvider.cs` - Mock provider with all necessary implementations

### Configuration
- `ApiProviderSettings.cs` - Provider selection configuration
- `appsettings.example.json` - Configuration example
- `appsettings.mock.json` - Mock provider configuration

This abstraction makes the bot truly independent of any single API provider, addressing the core issue of Tinkoff API instability.