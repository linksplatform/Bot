using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tinkoff.InvestApi.Sample;

[assembly: UserSecretsId("2323bae0-f4bf-4c7b-90ce-1b87d3fd76a8")]

var builder = Host.CreateDefaultBuilder(args);
var host = builder
    .ConfigureServices((context, services) =>
    {
        // services.AddInvestApiClient((provider, settings) => settings.AccessToken = "t.CVTYuEJAOviHK8Ex9_hGQgkjLaTqINTIGxqC5twPvBZm-dc81XKWuUxD7Nz1AYgwwhmXrX4Auh5JqScww3GEpQ");
        if (context.Configuration.GetValue<bool>("Sync"))
        {
            services.AddHostedService<SyncService>();
        }
        else
        {
            services.AddHostedService<AsyncService>();
        }
        services.AddInvestApiClient((_, settings) => context.Configuration.Bind(settings));
    })
    .Build();
await host.RunAsync();
