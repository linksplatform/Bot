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
        // First check if we have multiple configurations
        var multiConfig = context.Configuration.GetSection(nameof(MultiTradingConfiguration)).Get<MultiTradingConfiguration>();
        
        if (multiConfig?.Configurations?.Length > 0)
        {
            // Multiple configurations mode
            foreach (var config in multiConfig.Configurations)
            {
                // Register TradingService for each configuration with its own InvestApiClient
                services.AddSingleton<IHostedService>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<TradingService>>();
                    var lifetime = provider.GetRequiredService<IHostApplicationLifetime>();
                    
                    // Create a specific InvestApiClient for this configuration
                    var investApiClient = InvestApiClientFactory.Create(config.InvestApiSettings.AccessToken ?? "");
                    
                    return new TradingService(logger, investApiClient, lifetime, config.TradingSettings, config.Name);
                });
            }
        }
        else
        {
            // Legacy single configuration mode for backward compatibility
            services.AddSingleton(_ =>
            {
                var section = context.Configuration.GetSection(nameof(TradingSettings));
                return section.Get<TradingSettings>() ?? new TradingSettings();
            });
            services.AddHostedService<TradingService>();
            services.AddInvestApiClient((_, settings) =>
            {
                var section = context.Configuration.GetSection(nameof(InvestApiSettings));
                var loadedSettings = section.Get<InvestApiSettings>();
                settings.AccessToken = loadedSettings?.AccessToken ?? "";
                settings.AppName = loadedSettings?.AppName ?? "";
                context.Configuration.Bind(settings);
            });
        }
    })
    .Build();

await host.RunAsync();
