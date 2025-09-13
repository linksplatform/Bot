using System.Collections.Generic;

namespace DiscordBot
{
    public class DiscordBotSettings
    {
        public string Token { get; set; } = string.Empty;
        public string GitHubToken { get; set; } = string.Empty;
        public ulong GuildId { get; set; }
        public Dictionary<string, ulong> LanguageRoles { get; set; } = new();
        public string DatabasePath { get; set; } = "discord_bot.db";
        public int SyncIntervalHours { get; set; } = 24;
    }
}