using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi.Sample;

public class AsyncService : BackgroundService
{
    private readonly InvestApiClient _investApi;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<AsyncService> _logger;
    private MoneyValue? _rubWithdrawLimit;

    public AsyncService(ILogger<AsyncService> logger, InvestApiClient investApi, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _investApi = investApi;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        var account = _investApi.Users.GetAccounts().Accounts[0];
        var withdrawLimitsResponse = _investApi.Operations.GetWithdrawLimits(new WithdrawLimitsRequest(){AccountId = account.Id});
        _rubWithdrawLimit = withdrawLimitsResponse.Money.First(moneyValue => moneyValue.Currency == "rub");
        var etfs = _investApi.Instruments.Etfs();
        var trurEtf = etfs.Instruments.First(etf => etf.Ticker == "TRUR");
        var marketDataStream = _investApi.MarketDataStream.MarketDataStream();
        await marketDataStream.RequestStream.WriteAsync(new MarketDataRequest()
        {
            SubscribeOrderBookRequest = new SubscribeOrderBookRequest()
            {
                Instruments = { new OrderBookInstrument() { Figi = trurEtf.Figi, Depth = 1} },
                SubscriptionAction = SubscriptionAction.Subscribe
            },
        }).ContinueWith((task) =>
        {
            if (!task.IsCompletedSuccessfully)
            {
                _logger.LogError(task.Exception, "Error while subscribing to market data");
                return;
            }
            _logger.LogInformation("Subscribed to market data");
        }, stoppingToken);
        var repsonseStream = marketDataStream.ResponseStream;
        await foreach (var data in repsonseStream.ReadAllAsync(stoppingToken))
        {
            if (data.PayloadCase == MarketDataResponse.PayloadOneofCase.Orderbook)
            {
                _logger.LogInformation($"Orderbook data received from stream.");
            }
        }

        _lifetime.StopApplication();
    }
}
