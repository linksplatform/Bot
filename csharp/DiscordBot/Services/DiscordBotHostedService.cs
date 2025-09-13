using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot.Services
{
    public class DiscordBotHostedService : BackgroundService
    {
        private readonly DiscordBotService _discordBotService;
        private readonly ILogger<DiscordBotHostedService> _logger;

        public DiscordBotHostedService(DiscordBotService discordBotService, ILogger<DiscordBotHostedService> logger)
        {
            _discordBotService = discordBotService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await _discordBotService.StartAsync();
                _logger.LogInformation("Discord bot started successfully");

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while running Discord bot");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _discordBotService.StopAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}