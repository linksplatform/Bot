using Discord;
using Discord.Commands;
using DiscordBot.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class LanguageRoleModule : ModuleBase<SocketCommandContext>
    {
        private readonly DiscordBotService _botService;
        private readonly GitHubLanguageDetectionService _languageService;
        private readonly ILogger<LanguageRoleModule> _logger;

        public LanguageRoleModule(
            DiscordBotService botService, 
            GitHubLanguageDetectionService languageService,
            ILogger<LanguageRoleModule> logger)
        {
            _botService = botService;
            _languageService = languageService;
            _logger = logger;
        }

        [Command("sync-roles")]
        [Summary("Synchronizes your Discord roles with programming languages from your GitHub profile")]
        public async Task SyncRolesAsync([Summary("Your GitHub username")] string githubUsername)
        {
            try
            {
                await Context.Channel.TriggerTypingAsync();

                _logger.LogInformation("User {DiscordUser} requested role sync with GitHub user {GitHubUser}", 
                    Context.User.Username, githubUsername);

                var embed = new EmbedBuilder()
                    .WithTitle("🔄 Synchronizing Roles...")
                    .WithDescription($"Analyzing GitHub profile: **{githubUsername}**\nThis may take a moment...")
                    .WithColor(Color.Blue)
                    .WithTimestamp(DateTimeOffset.Now)
                    .Build();

                var message = await ReplyAsync(embed: embed);

                _botService.StoreUserGitHubMapping(Context.User.Id, githubUsername);

                var languageStats = await _languageService.GetUserProgrammingLanguagesAsync(githubUsername);
                
                if (!languageStats.Any())
                {
                    var errorEmbed = new EmbedBuilder()
                        .WithTitle("❌ No Programming Languages Found")
                        .WithDescription($"No programming languages found for GitHub user **{githubUsername}**.\n\n" +
                                       "This could mean:\n" +
                                       "• The user doesn't exist\n" +
                                       "• The user has no public repositories\n" +
                                       "• The repositories don't contain detectable programming languages")
                        .WithColor(Color.Red)
                        .WithTimestamp(DateTimeOffset.Now)
                        .Build();

                    await message.ModifyAsync(m => m.Embed = errorEmbed);
                    return;
                }

                await _botService.SyncUserRolesAsync(Context.User.Id, githubUsername);

                var topLanguages = GitHubLanguageDetectionService.GetTopLanguages(languageStats);
                var languageList = topLanguages.Take(10).Select(lang => 
                {
                    var bytes = languageStats[lang];
                    var size = bytes < 1024 ? $"{bytes} B" : 
                              bytes < 1024 * 1024 ? $"{bytes / 1024:N0} KB" : 
                              $"{bytes / (1024 * 1024):N1} MB";
                    return $"• **{lang}** ({size})";
                });

                var successEmbed = new EmbedBuilder()
                    .WithTitle("✅ Roles Synchronized Successfully!")
                    .WithDescription($"**GitHub Profile:** {githubUsername}\n\n" +
                                   $"**Top Programming Languages:**\n{string.Join("\n", languageList)}\n\n" +
                                   "Your Discord roles have been updated to reflect your programming language expertise!")
                    .WithColor(Color.Green)
                    .WithTimestamp(DateTimeOffset.Now)
                    .WithFooter("Roles are synchronized based on your public repositories")
                    .Build();

                await message.ModifyAsync(m => m.Embed = successEmbed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during role synchronization for user {User} with GitHub {GitHub}", 
                    Context.User.Username, githubUsername);

                var errorEmbed = new EmbedBuilder()
                    .WithTitle("❌ Synchronization Failed")
                    .WithDescription("An error occurred while synchronizing your roles. Please try again later or contact an administrator.")
                    .WithColor(Color.Red)
                    .WithTimestamp(DateTimeOffset.Now)
                    .Build();

                await ReplyAsync(embed: errorEmbed);
            }
        }

        [Command("check-languages")]
        [Summary("Shows programming languages detected from a GitHub profile without changing roles")]
        public async Task CheckLanguagesAsync([Summary("GitHub username to check")] string githubUsername)
        {
            try
            {
                await Context.Channel.TriggerTypingAsync();

                _logger.LogInformation("User {DiscordUser} requested language check for GitHub user {GitHubUser}", 
                    Context.User.Username, githubUsername);

                var embed = new EmbedBuilder()
                    .WithTitle("🔍 Analyzing GitHub Profile...")
                    .WithDescription($"Scanning repositories for: **{githubUsername}**")
                    .WithColor(Color.Blue)
                    .WithTimestamp(DateTimeOffset.Now)
                    .Build();

                var message = await ReplyAsync(embed: embed);

                var languageStats = await _languageService.GetUserProgrammingLanguagesAsync(githubUsername);
                
                if (!languageStats.Any())
                {
                    var errorEmbed = new EmbedBuilder()
                        .WithTitle("❌ No Programming Languages Found")
                        .WithDescription($"No programming languages detected for GitHub user **{githubUsername}**.")
                        .WithColor(Color.Red)
                        .WithTimestamp(DateTimeOffset.Now)
                        .Build();

                    await message.ModifyAsync(m => m.Embed = errorEmbed);
                    return;
                }

                var topLanguages = GitHubLanguageDetectionService.GetTopLanguages(languageStats, maxLanguages: 15);
                var totalBytes = languageStats.Values.Sum();
                
                var languageList = topLanguages.Select((lang, index) => 
                {
                    var bytes = languageStats[lang];
                    var percentage = (double)bytes / totalBytes * 100;
                    var size = bytes < 1024 ? $"{bytes} B" : 
                              bytes < 1024 * 1024 ? $"{bytes / 1024:N0} KB" : 
                              $"{bytes / (1024 * 1024):N1} MB";
                    return $"{index + 1}. **{lang}** - {percentage:F1}% ({size})";
                });

                var resultEmbed = new EmbedBuilder()
                    .WithTitle($"📊 Programming Languages for {githubUsername}")
                    .WithDescription($"**Total Repositories Analyzed:** {languageStats.Count}\n\n" +
                                   $"**Languages Found:**\n{string.Join("\n", languageList)}")
                    .WithColor(Color.Blue)
                    .WithTimestamp(DateTimeOffset.Now)
                    .WithFooter("Analysis based on public repositories only")
                    .Build();

                await message.ModifyAsync(m => m.Embed = resultEmbed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during language check for GitHub user {GitHub}", githubUsername);

                var errorEmbed = new EmbedBuilder()
                    .WithTitle("❌ Analysis Failed")
                    .WithDescription("An error occurred while analyzing the GitHub profile. Please check the username and try again.")
                    .WithColor(Color.Red)
                    .WithTimestamp(DateTimeOffset.Now)
                    .Build();

                await ReplyAsync(embed: errorEmbed);
            }
        }

        [Command("my-sync")]
        [Summary("Synchronizes your roles using your previously linked GitHub account")]
        public async Task MySyncAsync()
        {
            var githubUsername = _botService.GetUserGitHubUsername(Context.User.Id);
            
            if (string.IsNullOrEmpty(githubUsername))
            {
                var embed = new EmbedBuilder()
                    .WithTitle("❌ No GitHub Account Linked")
                    .WithDescription("You haven't linked a GitHub account yet. Use:\n`!sync-roles <your-github-username>`")
                    .WithColor(Color.Red)
                    .WithTimestamp(DateTimeOffset.Now)
                    .Build();

                await ReplyAsync(embed: embed);
                return;
            }

            await SyncRolesAsync(githubUsername);
        }

        [Command("help")]
        [Summary("Shows available commands for role synchronization")]
        public async Task HelpAsync()
        {
            var embed = new EmbedBuilder()
                .WithTitle("🤖 Programming Language Role Bot - Commands")
                .WithDescription("This bot automatically assigns Discord roles based on your GitHub programming language usage.")
                .WithColor(Color.Blue)
                .AddField("!sync-roles <github-username>", 
                         "Links your GitHub account and synchronizes your Discord roles", false)
                .AddField("!my-sync", 
                         "Re-synchronizes your roles using your previously linked GitHub account", false)
                .AddField("!check-languages <github-username>", 
                         "Shows programming languages for a GitHub user without changing roles", false)
                .AddField("!help", 
                         "Shows this help message", false)
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter("LinksPlatform Discord Bot")
                .Build();

            await ReplyAsync(embed: embed);
        }
    }
}