using System.Net.NetworkInformation;
using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;

namespace TraderBot;

public class AsyncService : BackgroundService
{
    private readonly InvestApiClient _investApi;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<AsyncService> _logger;
    private MoneyValue? _rubWithdrawLimit;
    public readonly int Quantity = 1;

    public AsyncService(ILogger<AsyncService> logger, InvestApiClient investApi, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _investApi = investApi;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        Asset? asset = null;
        var account = _investApi.Users.GetAccounts().Accounts[0];
        var withdrawLimitsResponse = _investApi.Operations.GetWithdrawLimits(new WithdrawLimitsRequest() { AccountId = account.Id });
        _rubWithdrawLimit = withdrawLimitsResponse.Money.First(moneyValue => moneyValue.Currency == "rub");
        var etfs = _investApi.Instruments.Etfs();
        var instrument = etfs.Instruments.First(etf => etf.Ticker == "TRUR");
        var marketDataStream = _investApi.MarketDataStream.MarketDataStream();
        await marketDataStream.RequestStream.WriteAsync(new MarketDataRequest()
            {
                SubscribeInfoRequest = new SubscribeInfoRequest()
                {
                    Instruments = { new InfoInstrument() { Figi = instrument.Figi } },
                    SubscriptionAction = SubscriptionAction.Subscribe
                },
                SubscribeOrderBookRequest = new SubscribeOrderBookRequest()
                {
                    Instruments = { new OrderBookInstrument() { Figi = instrument.Figi, Depth = 1 } },
                    SubscriptionAction = SubscriptionAction.Subscribe
                },
            })
            .ContinueWith((task) =>
            {
                if (!task.IsCompletedSuccessfully)
                {
                    throw new Exception("Error while subscribing to market data");
                }
                _logger.LogInformation("Subscribed to market data");
            }, stoppingToken);
        var repsonseStream = marketDataStream.ResponseStream;
        await foreach (var data in repsonseStream.ReadAllAsync(stoppingToken))
        {
            if (data.PayloadCase == MarketDataResponse.PayloadOneofCase.Orderbook)
            {
                var orderBook = data.Orderbook;
                _logger.LogInformation("Orderbook data received from stream: {OrderBook}", orderBook);
                TradeAssets(asset, account.Id, orderBook, instrument.Figi);
            }
            _lifetime.StopApplication();
        }
        await _investApi.;
    }

    private async void TradeAssets(Asset? asset, string accountId, OrderBook marketOrderBook, string figi)
    {
        var cheapestBidOrder = marketOrderBook.Bids[0];
        var cheapestAskOrder = marketOrderBook.Asks[0];
        if (asset == null)
        {
            PostOrderRequest buyOrderRequest = new()
            {
                Figi = figi,
                Quantity = Quantity,
                Price = cheapestAskOrder.Price,
                Direction = OrderDirection.Buy,
                AccountId = accountId,
                OrderType = OrderType.Limit,
                OrderId = ""
            };
            PostBuyOrder(buyOrderRequest);
        }
        else
        {
            PostOrderRequest sellOrderRequest = new()
            {
                Figi = figi,
                Quantity = Quantity,
                Price = cheapestBidOrder.Price,
                Direction = OrderDirection.Sell,
                AccountId = accountId,
                OrderType = OrderType.Limit,
                OrderId = ""
            };
            PostSellOrderIfProfitable(asset.Value, sellOrderRequest);
        }

    }

    private async void PostBuyOrder(PostOrderRequest sellOrderRequest)
    {
        var buyOrderResponse = await _investApi.Orders.PostOrderAsync(sellOrderRequest).ResponseAsync;
        _logger.LogInformation($"Buy order placed: {buyOrderResponse}");
    }

    private async void PostSellOrderIfProfitable(Asset asset, PostOrderRequest sellOrderRequest)
    {
        if (sellOrderRequest.Price < asset.Price)
        {
            return;
        }
        var sellOrderResponse = await _investApi.Orders.PostOrderAsync(sellOrderRequest).ResponseAsync;
        _logger.LogInformation($"Sell order placed: {sellOrderResponse}");
    }
}
