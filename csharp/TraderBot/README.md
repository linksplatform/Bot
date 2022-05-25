# TraderBot ([русская версия](README.ru.md))

This bot is participating in the [Tinkoff Invest Robot Contest](https://github.com/Tinkoff/invest-robot-contest).

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

Token should be placed in `appsettings.json` file at `InvestApiSettings`/`AccessToken` key.

## Start
```sh
dotnet run
```
To stop the bot, press Ctrl+C.

## Description

At the moment, the bot is able to:
* Place sell order at best price or +0.01 from buy order price;
* Place buy order at best price.

After placing the order, the bot will wait for the order to be filled. Then it will place the next order:
* After buy order is filled, the bot will place sell order at best price or +0.01 from buy order price;
* After sell order is filled, the bot will place buy order at best price.
You can configure the bot to trade ETF/cash pair at `TradingSettings`/`EtfTicker` and `TradingSettings`/`CashCurrency` keys in `appsettings.json` file.

## Strategy

This bot implements a simple scalping strategy.
It executes continuous loop of buying and selling each time making 1 pt of profit per each iteration of buying and selling.
Works best on growing market and with least volotile asset.

## Roadmap
- [x] Make a bot that can place buy and sell orders at best price on a single run
- [x] Make this bot work without restarts
- [ ] Support moving buy order up
- [ ] Add short trading mode
- [ ] Optimize the number of requests to the API
- [ ] Store all the data in the database (doublets links storage)
