#!/bin/bash

set -o pipefail
set +e

jq -n \
  --arg logLevelDefault "$LOG_LEVEL_DEFAULT" \
  --arg microsoftHostingLifetime "$MICROSOFT_HOSTING_LIFETIME" \
  --arg accessToken "$ACCESS_TOKEN" \
  --arg appName "$APP_NAME" \
  --arg instrument "$INSTRUMENT" \
  --arg ticker "$TICKER" \
  --arg cashCurrency "$CASH_CURRENCY" \
  --arg accountIndex "$ACCOUNT_INDEX" \
  --arg minimumProfitSteps "$MINIMUM_PROFIT_STEPS" \
  --arg marketOrderBookDepth "$MARKET_ORDER_BOOK_DEPTH" \
  --arg minimumMarketOrderSizeToChangeBuyPrice "$MINIMUM_MARKET_ORDER_SIZE_TO_CHANGE_BUY_PRICE" \
  --arg minimumMarketOrderSizeToChangeSellPrice "$MINIMUM_MARKET_ORDER_SIZE_TO_CHANGE_SELL_PRICE" \
  --arg minimumMarketOrderSizeToBuy "$MINIMUM_MARKET_ORDER_SIZE_TO_BUY" \
  --arg minimumMarketOrderSizeToSell "$MINIMUM_MARKET_ORDER_SIZE_TO_SELL" \
  --arg minimumTimeToBuy "$MINIMUM_TIME_TO_BUY" \
  --arg maximumTimeToBuy "$MAXIMUM_TIME_TO_BUY" \
  --arg earlySellOwnedLotsDelta "$EARLY_SELL_OWNED_LOTS_DELTA" \
  --arg earlySellOwnedLotsMultiplier "$EARLY_SELL_OWNED_LOTS_MULTIPLIER" \
  --arg loadOperationsFrom "$LOAD_OPERATIONS_FROM" \
  '{
    "Logging": {
      "LogLevel": {
        "Default": $logLevelDefault,
        "Microsoft.Hosting.Lifetime": $microsoftHostingLifetime
        }
      },
    "InvestApiSettings": {
      "AccessToken": $accessToken,
      "AppName": $appName
    },
    "TradingSettings": {
      "Instrument": $instrument,
      "Ticker": $ticker,
      "CashCurrency": $cashCurrency,
      "AccountIndex": $accountIndex,
      "MinimumProfitSteps": $minimumProfitSteps,
      "MarketOrderBookDepth": $marketOrderBookDepth,
      "MinimumMarketOrderSizeToChangeBuyPrice": $minimumMarketOrderSizeToChangeBuyPrice,
      "MinimumMarketOrderSizeToChangeSellPrice": $minimumMarketOrderSizeToChangeSellPrice,
      "MinimumMarketOrderSizeToBuy": $minimumMarketOrderSizeToBuy,
      "MinimumMarketOrderSizeToSell": $minimumMarketOrderSizeToSell,
      "MinimumTimeToBuy": $minimumTimeToBuy,
      "MaximumTimeToBuy": $maximumTimeToBuy,
      "EarlySellOwnedLotsDelta": $earlySellOwnedLotsDelta,
      "EarlySellOwnedLotsMultiplier": $earlySellOwnedLotsMultiplier,
      "LoadOperationsFrom": $loadOperationsFrom
    }
  }' > appsettings.json

dotnet TraderBot.dll