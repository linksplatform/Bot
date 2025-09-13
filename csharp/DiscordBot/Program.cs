using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DiscordBot.Services;
using System.Threading.Tasks;

namespace DiscordBot
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddCommandLine(args);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<DiscordBotSettings>(context.Configuration.GetSection("DiscordBot"));
                    services.AddSingleton<DiscordBotService>();
                    services.AddSingleton<ProgrammingLanguageRoleService>();
                    services.AddSingleton<GitHubLanguageDetectionService>();
                    services.AddHostedService<DiscordBotHostedService>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                });
    }
}