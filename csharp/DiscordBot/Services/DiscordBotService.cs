using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Storage.Local;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Services
{
    public class DiscordBotService
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly DiscordBotSettings _settings;
        private readonly ProgrammingLanguageRoleService _roleService;
        private readonly ILogger<DiscordBotService> _logger;
        private readonly FileStorage _storage;

        public DiscordBotService(
            IOptions<DiscordBotSettings> settings,
            ProgrammingLanguageRoleService roleService,
            ILogger<DiscordBotService> logger)
        {
            _settings = settings.Value;
            _roleService = roleService;
            _logger = logger;
            _storage = new FileStorage(_settings.DatabasePath);

            var config = new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 100,
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
            };

            _client = new DiscordSocketClient(config);
            _commands = new CommandService();

            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.MessageReceived += HandleCommandAsync;
            _client.UserJoined += OnUserJoinedAsync;
        }

        public async Task StartAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);
            
            await _client.LoginAsync(TokenType.Bot, _settings.Token);
            await _client.StartAsync();
        }

        public async Task StopAsync()
        {
            await _client.LogoutAsync();
            await _client.StopAsync();
        }

        private Task LogAsync(LogMessage log)
        {
            _logger.LogInformation("{Source}: {Message}", log.Source, log.Message);
            return Task.CompletedTask;
        }

        private async Task ReadyAsync()
        {
            _logger.LogInformation("Discord bot is ready! Logged in as {Username}#{Discriminator}", 
                _client.CurrentUser.Username, _client.CurrentUser.Discriminator);

            var guild = _client.GetGuild(_settings.GuildId);
            if (guild != null)
            {
                _logger.LogInformation("Connected to guild: {GuildName} ({GuildId})", guild.Name, guild.Id);
            }

            await _client.SetGameAsync("Synchronizing programming language roles", type: ActivityType.Watching);
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            if (messageParam is not SocketUserMessage message || message.Author.IsBot)
                return;

            int argPos = 0;
            if (!message.HasStringPrefix("!", ref argPos) && !message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                return;

            var context = new SocketCommandContext(_client, message);

            var result = await _commands.ExecuteAsync(context, argPos, null);
            
            if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
            {
                _logger.LogWarning("Command execution failed: {Error} - {ErrorReason}", result.Error, result.ErrorReason);
                await context.Channel.SendMessageAsync($"Error: {result.ErrorReason}");
            }
        }

        private async Task OnUserJoinedAsync(SocketGuildUser user)
        {
            _logger.LogInformation("User {Username} joined the guild", user.Username);
            
            var channel = user.Guild.SystemChannel ?? user.Guild.DefaultChannel;
            if (channel != null)
            {
                var embed = new EmbedBuilder()
                    .WithTitle("Welcome to LinksPlatform!")
                    .WithDescription($"Welcome {user.Mention}! 🎉\n\n" +
                                   "To get programming language roles based on your GitHub activity, use:\n" +
                                   "`!sync-roles <your-github-username>`\n\n" +
                                   "Use `!help` to see all available commands.")
                    .WithColor(Color.Green)
                    .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                    .WithTimestamp(DateTimeOffset.Now)
                    .Build();

                await channel.SendMessageAsync(embed: embed);
            }
        }

        public void StoreUserGitHubMapping(ulong discordUserId, string githubUsername)
        {
            _storage.AddLink(discordUserId.ToString(), githubUsername);
        }

        public string? GetUserGitHubUsername(ulong discordUserId)
        {
            var links = _storage.GetLinks();
            return links.FirstOrDefault(l => l.Split(':')[0] == discordUserId.ToString())?.Split(':')[1];
        }

        public async Task SyncUserRolesAsync(ulong userId, string githubUsername)
        {
            await _roleService.SynchronizeUserRolesAsync(_client, userId, githubUsername);
        }
    }
}