using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Logging;
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
        services.AddHostedService<TradingService>(provider => 
            new TradingService(
                provider.GetRequiredService<ILogger<TradingService>>(),
                provider.GetRequiredService<InvestApiClient>(),
                provider.GetRequiredService<IHostApplicationLifetime>(),
                provider.GetRequiredService<TradingSettings>(),
                provider
            ));
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

// Check if running in test mode
if (args.Length > 0 && args[0] == "--test")
{
    Console.WriteLine("Running Portfolio Balance Algorithm Tests...");
    await PortfolioBalanceAlgorithmTests.RunTests();
}
else
{
    await host.RunAsync();
}
