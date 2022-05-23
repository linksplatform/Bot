# TraderBot | Торговый бот

This bot is participating in the [Tinkoff Invest Robot Contest](https://github.com/Tinkoff/invest-robot-contest).
Этот бот участвует в конкурсе [Tinkoff Invest Robot Contest](https://github.com/Tinkoff/invest-robot-contest).

## Prerequisites | Предварительные требования
* Linux, macOS, Windows
* [Git](https://git-scm.com/downloads)
* [.NET SDK](https://dotnet.microsoft.com/download)

## Install | Установка
```
git clone https://github.com/linksplatform/Bot
cd Bot/csharp/TraderBot
dotnet restore
```

## Prepare | Подготовка

For the bot to work, you need to get a token. You can do this here: https://www.tinkoff.ru/invest/settings.
Для работы бота вам необходимо получить токен. Вы можете сделать это здесь: https://www.tinkoff.ru/invest/settings.

Token should be placed in `appsettings.json` file at `InvestApiSettings`/`AccessToken` key.
Токен должен быть установлен в файле `appsettings.json` в ключе `InvestApiSettings`/`AccessToken`.

## Start | Запуск
```sh
dotnet run
```
To stop the bot, press Ctrl+C. | Для остановки бота нажмите Ctrl+C.

## Description | Описание

At the moment, the bot is able to:
* Place sell order at best or +0.01 price
* Place buy order at best price

The bot does single order at a time. After placing the order, the bot stops. You can start the bot again to place another order.
You can configure the bot to trade ETF/cash pair at `TradingSettings`/`EtfTicker` and `TradingSettings`/`CashCurrency` keys in `appsettings.json` file.

В настоящее время бот имеет следующие возможности:
* Продажа по лучшей цене или +0.01
* Покупка по лучшей цене
Бот делает одну заявку за раз. После подачи заявки бот останавливается. Вы можете запустить бота снова, чтобы подать еще одну заявку.
Вы можете настроить торговлю ETF/наличными ботом в ключах `TradingSettings`/`EtfTicker` и `TradingSettings`/`CashCurrency` в файле `appsettings.json`.

# Strategy | Стратегия

This bot implements a simple scalping strategy.
It helps to execute continuous loops of buying and selling each time making 1 pt of profit per each iteration of buying and selling.
Works best on growing market.

Этот бот реализует простую стратегию скальпинга.
Он помогает выполнять постоянный цикл покупок и продаж каждый раз получая прибыль размером в 1 пункт за каждую итерацию покупки и продажи.
Работает наилучшим образом на растущем рынке.

# Roadmap | Дорожная карта
- [x] Make a bot that can place buy and sell orders at best price on a single run | Сделать бота, который может подавать заявки на покупку и продажу по лучшей цене за один запуск
- [ ] Make this bot work without restarts | Сделать этот бот работал без перезапусков
- [ ] Add short trading mode | Добавить режим короткой торговли
- [ ] Optimize the number of requests to the API | Оптимизировать количество запросов к API
- [ ] Store all the data in the database (doublets links storage) | Хранить все данные в базе данных (хранилище связей-дуплетов)