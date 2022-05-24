using Microsoft.Extensions.Configuration.UserSecrets;
using Tinkoff.InvestApi;
using TraderBot;

[assembly: UserSecretsId("2323bae0-f4bf-4c7b-90ce-1b87d3fd76a8")]

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
