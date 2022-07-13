# TraderBot ([русская версия](README.ru.md))

This bot was participating in the [Tinkoff Invest Robot Contest](https://github.com/Tinkoff/invest-robot-contest) (no prize).

[![Trading with bot](trading.png)](trading.png)

## Prerequisites
* Linux, macOS, Windows
* [Git](https://git-scm.com/downloads)
* [.NET SDK](https://dotnet.microsoft.com/download)

## Install
```
git clone https://github.com/linksplatform/Bot
cd Bot/csharp/TraderBot
dotnet restore
```

## Prepare

For the bot to work, you need to get a token. You can do this here: https://www.tinkoff.ru/invest/settings.

Token should be placed in `appsettings.json` file at `InvestApiSettings`/`AccessToken` key. Also set `TradingSettings`/`AccountIndex` to the index of the account to which the token has access. The list of accounts with their indices is written by the bot to the console at start-up.

## Start
```sh
dotnet run
```
To stop the bot, press Ctrl+C.

## Description

At the moment, the bot is able to:
* Place sell order at best price or buy order price;
* Place buy order at best price;
* Move buy and sell orders.

After placing the order, the bot will wait for the order to be filled. Then it will place the next order:
* After buy order is filled, the bot will place sell order at best price or buy order price;
* After sell order is filled, the bot will place buy order at best price.

You can configure the bot to trade ETF/cash pair at `TradingSettings`/`EtfTicker` and `TradingSettings`/`CashCurrency` keys in `appsettings.json` file.

In order to make the bot sell only with profit (`+1` pt or for example `+0.01`) set `TradingSettings`/`MinimumProfitSteps` to `1`. WARNING: This can limit the ability of the bot to follow the price (especially down).

## Strategy

This bot implements a simple scalping strategy.
It executes continuous loop of buying and selling sometimes making 1 pt of profit per iteration of buying and selling.
Works best on growing market and with least volatile asset.

Hypothesis:
```
It is possible to make more profit using this strategy
than with buy and hold strategy
due to effect of compound interest
on each buy and sell iteration with profit.
```

## Configuration

Configuration is located in `appsettings.json` file.

| Setting | Description |
| ------- | ----------- |
| `InvestApiSettings` | Settings for the Tinkoff API |
| `InvestApiSettings`/`AccessToken` | Token for the Tinkoff API |
| `InvestApiSettings`/`AppName` | Name of the application |
| `TradingSettings` | Settings for trading |
| `TradingSettings`/`EtfTicker` | Ticker of ETF to trade |
| `TradingSettings`/`CashCurrency` | Ticker of cash currency to trade |
| `TradingSettings`/`AccountIndex` | Index of the account to use (the list of accounts is shown just after the start-up of the bot). |
| `TradingSettings`/`MinimumProfitSteps` | Minimum number of profit steps to trade. The step is predefined for the ETF by broker/exchange (for example for `TRUR` it is equal to `0.01`). In other words this is the minimum profit that bot is allowed to take. Positive numbers will make the bot sell only with profit. Zero allows bot to sell at the buy price (it makes it easier for the bot to follow price changes on the market, and yet it is free). Negative numbers will allow bot to sell with a loss (it makes it even easier to follow price changes on the market, now ability to follow the price costs money). |
| `TradingSettings`/`MarketOrderBookDepth` | Depth of the market order book to use (allowed values are: `1`, `10`, `20`, `30`, `40`, `50`). This setting is required when you put the limit on weather the price is acceptable to trade at. There can be multiple prices in order book, but only some of them are acceptable. So if you choose `1` value for this setting you risk to force the bot to wait until the price at `1th` position on the market is acceptable, on the other hand if you use `10` value, the first matched price will be used. Bigger values should be used when acceptable prices are rare. |
| `TradingSettings`/`MinimumMarketOrderSizeToChangeBuyPrice` | Minimum size of the market order to change buy price. The price will be not acceptable unless there is that much lots on the market at this price. |
| `TradingSettings`/`MinimumMarketOrderSizeToChangeSellPrice` | Minimum size of the market order to change sell price. The price will be not acceptable unless there is that much lots on the market at this price. |
| `TradingSettings`/`MinimumMarketOrderSizeToBuy` | Minimum size of the market order to buy. The price will be not acceptable unless there is that much lots on the market at this price. |
| `TradingSettings`/`MinimumMarketOrderSizeToSell` | Minimum size of the market order to sell. The price will be not acceptable unless there is that much lots on the market at this price. |
| `TradingSettings`/`EarlySellOwnedLotsDelta` | A constant component of the minimum number of lots that the market order placed at the buy price should have in order to trigger the immediate sell order. Complete formula: (`TradingSettings`/`EarlySellOwnedLotsDelta` + `TradingSettings`/`EarlySellOwnedLotsMultiplier` * `Lots requested to sell`). |
| `TradingSettings`/`EarlySellOwnedLotsMultiplier` | A multiplier of lots requested to sell. A component of the minimum number of lots that the market order placed at the buy price should have in order to trigger the immediate sell order. Complete formula: (`TradingSettings`/`EarlySellOwnedLotsDelta` + `TradingSettings`/`EarlySellOwnedLotsMultiplier` * `Lots requested to sell`). |
| `TradingSettings`/`LoadOperationsFrom` | Minimum data and time to load operations from. |

## Current default trading time-frame

At the moment, the bot trades only starting 12:00:00 till 17:00:00 Moscow time. This corresponds to the UTC time-frame: `09:00:00` till `14:00:00` settings in the `appsettings.json` file.

[![Trading with bot](day-volatility.png)](day-volatility.png)

## Roadmap
- [x] Make a bot that can place buy and sell orders at best price on a single run
- [x] Make this bot work without restarts
- [x] Support moving buy order up
- [x] Optional support moving sell order down (up to equal to buy price)
- [x] Add short trading mode
- [ ] Optimize the number of requests to the API
- [ ] Store all the data in the associative database ([Deep](https://github.com/deep-foundation) or [Doublets links storage](https://github.com/linksplatform/Data.Doublets))
