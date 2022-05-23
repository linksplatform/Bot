using Microsoft.Extensions.Configuration.UserSecrets;
using TraderBot;
using TLinkAddresss = System.UInt64;

[assembly: UserSecretsId("2323bae0-f4bf-4c7b-90ce-1b87d3fd76a8")]


var builder = Host.CreateDefaultBuilder(args);
var host = builder
    .ConfigureServices((context, services) =>
    {
        // if (context.Configuration.GetValue<bool>("Sync"))
        // {
        //     services.AddHostedService<SyncSample>();
        // }
        // else
        // {
            services.AddHostedService<AsyncService>();
        // }
        services.AddInvestApiClient((_, settings) =>
        {
            settings.AccessToken = "";
            settings.AppName = "LinksPlatformScalper";
            context.Configuration.Bind(settings);
        });
    })
    .Build();
await host.RunAsync();
