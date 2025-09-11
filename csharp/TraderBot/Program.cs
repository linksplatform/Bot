using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi;
using TraderBot;
using TraderBot.Interfaces;
using TraderBot.Providers;

var builder = Host.CreateDefaultBuilder(args);
var host = builder
    .ConfigureServices((context, services) =>
    {
        // Register settings
        services.AddSingleton(_ =>
        {
            var section = context.Configuration.GetSection(nameof(TradingSettings));
            return section.Get<TradingSettings>()!;
        });

        services.AddSingleton(_ =>
        {
            var section = context.Configuration.GetSection(nameof(ApiProviderSettings));
            return section.Get<ApiProviderSettings>() ?? new ApiProviderSettings();
        });

        services.AddSingleton(_ =>
        {
            var section = context.Configuration.GetSection(nameof(InvestApiSettings));
            return section.Get<InvestApiSettings>()!;
        });

        // Register API providers
        services.AddInvestApiClient((_, settings) =>
        {
            var section = context.Configuration.GetSection(nameof(InvestApiSettings));
            var loadedSettings = section.Get<InvestApiSettings>();
            settings.AccessToken = loadedSettings!.AccessToken;
            settings.AppName = loadedSettings.AppName;
            context.Configuration.Bind(settings);
        });

        // Register the trading API provider based on configuration
        services.AddSingleton<ITradingApiProvider>(provider =>
        {
            var apiProviderSettings = provider.GetRequiredService<ApiProviderSettings>();

            switch (apiProviderSettings.Provider.ToLowerInvariant())
            {
                case "tinkoff":
                    var investApi = provider.GetRequiredService<InvestApiClient>();
                    var tinkoffLogger = provider.GetRequiredService<ILogger<TinkoffApiProvider>>();
                    return new TinkoffApiProvider(investApi, tinkoffLogger);
                case "mock":
                    var mockLogger = provider.GetRequiredService<ILogger<MockApiProvider>>();
                    return new MockApiProvider(mockLogger);
                default:
                    throw new InvalidOperationException($"Unsupported API provider: {apiProviderSettings.Provider}");
            }
        });

        // Register the trading service
        services.AddHostedService<AbstractTradingService>();
    })
    .Build();

await host.RunAsync();