using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.Bot.Services
{
    /// <summary>
    /// Service for Discord operations including creating invite links
    /// </summary>
    public class DiscordService
    {
        private readonly DiscordSocketClient _client;
        private readonly string? _token;
        private readonly ulong? _guildId;
        private readonly ulong? _channelId;
        private bool _isConnected = false;

        public DiscordService(string? token = null, ulong? guildId = null, ulong? channelId = null)
        {
            _token = token;
            _guildId = guildId;
            _channelId = channelId;
            _client = new DiscordSocketClient();
        }

        public async Task<bool> ConnectAsync()
        {
            if (string.IsNullOrEmpty(_token))
                return false;

            try
            {
                await _client.LoginAsync(TokenType.Bot, _token);
                await _client.StartAsync();
                _isConnected = true;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<string?> CreateInviteLink()
        {
            if (!_isConnected || !_guildId.HasValue)
                return null;

            try
            {
                var guild = _client.GetGuild(_guildId.Value);
                if (guild == null)
                    return null;

                var channel = guild.DefaultChannel ?? guild.TextChannels.FirstOrDefault();
                if (channel == null)
                    return null;

                var invite = await channel.CreateInviteAsync(maxAge: (int)TimeSpan.FromDays(1).TotalSeconds, maxUses: 1, isTemporary: false, isUnique: true);
                return invite.Url;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_isConnected)
            {
                await _client.StopAsync();
                await _client.LogoutAsync();
                _isConnected = false;
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}