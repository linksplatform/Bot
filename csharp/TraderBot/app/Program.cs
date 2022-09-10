using Microsoft.Extensions.Configuration.UserSecrets;
using Tinkoff.InvestApi;
using TraderBot;

var builder = Host.CreateDefaultBuilder(args);
var host = builder
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton(_ =>
        {
            var section = context.Configuration.GetSection(nameof(TradingSettings));
            return section.Get<TradingSettings>();
        });
        services.AddHostedService<TradingService>();
        services.AddInvestApiClient((_, settings) =>
        {
            var section = context.Configuration.GetSection(nameof(InvestApiSettings));
            var loadedSettings = section.Get<InvestApiSettings>();
            settings.AccessToken = loadedSettings.AccessToken;
            settings.AppName = loadedSettings.AppName;
            context.Configuration.Bind(settings);
        });
    })
    .Build();
await host.RunAsync();
