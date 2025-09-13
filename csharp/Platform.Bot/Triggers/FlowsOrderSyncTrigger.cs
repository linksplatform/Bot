using Interfaces;
using Octokit;
using Platform.Bot.Services;
using Platform.Communication.Protocol.Lino;
using Storage.Local;
using Storage.Remote.GitHub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;

    public class FlowsOrderSyncTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _githubStorage;
        private readonly FileStorage _fileStorage;
        private readonly ContributionTrackingService _contributionService;
        private readonly DiscordRoleSyncService _discordService;
        private readonly Parser _parser = new();

        public FlowsOrderSyncTrigger(GitHubStorage githubStorage, FileStorage fileStorage, DiscordRoleSyncService discordService)
        {
            _githubStorage = githubStorage;
            _fileStorage = fileStorage;
            _contributionService = new ContributionTrackingService(githubStorage);
            _discordService = discordService;
        }

        public async Task<bool> Condition(TContext context)
        {
            var title = context.Title.ToLower();
            return title.Contains("flows order sync") || 
                   title.Contains("sync discord roles") ||
                   title.Contains("contribution sync");
        }

        public async Task Action(TContext context)
        {
            try
            {
                Console.WriteLine($"Starting flows order sync for issue: {context.Title}");

                var organizationName = context.Repository.Owner.Login;
                var parsedBody = _parser.Parse(context.Body);
                var config = ExtractConfigurationFromIssue(parsedBody);

                var sinceDate = DateTime.Now.AddMonths(-config.MonthsToAnalyze);
                Console.WriteLine($"Analyzing contributions since: {sinceDate:yyyy-MM-dd}");

                var contributions = await _contributionService.GetOrganizationContributions(organizationName, sinceDate);

                await GenerateContributionReport(context, contributions);

                if (config.SyncDiscordRoles && !string.IsNullOrEmpty(config.DiscordBotToken))
                {
                    await SyncDiscordRoles(contributions, config);
                }

                await SaveContributionData(organizationName, contributions);

                await _githubStorage.CreateIssueComment(context.Repository.Id, context.Number,
                    "✅ Flows order sync completed successfully!\n\n" +
                    $"📊 Analyzed {contributions.Count} contributors\n" +
                    $"📅 Time period: {config.MonthsToAnalyze} months\n" +
                    $"🔄 Discord sync: {(config.SyncDiscordRoles ? "Enabled" : "Disabled")}");

                _githubStorage.CloseIssue(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in FlowsOrderSyncTrigger: {ex.Message}");
                await _githubStorage.CreateIssueComment(context.Repository.Id, context.Number,
                    $"❌ Error during flows order sync: {ex.Message}");
            }
        }

        private FlowsSyncConfig ExtractConfigurationFromIssue(IList<Link> links)
        {
            var config = new FlowsSyncConfig();

            foreach (var link in links)
            {
                if (link.Values?.Count >= 3)
                {
                    var key = link.Values[0].Id.ToLower();
                    var value = link.Values[2].Id;

                    switch (key)
                    {
                        case "months":
                            if (int.TryParse(value, out int months))
                                config.MonthsToAnalyze = months;
                            break;
                        case "discord_token":
                            config.DiscordBotToken = value;
                            break;
                        case "discord_guild_id":
                            if (ulong.TryParse(value, out ulong guildId))
                                config.DiscordGuildId = guildId;
                            break;
                        case "discord_channel_id":
                            if (ulong.TryParse(value, out ulong channelId))
                                config.DiscordChannelId = channelId;
                            break;
                        case "sync_roles":
                            config.SyncDiscordRoles = value.ToLower() == "true" || value == "1";
                            break;
                    }
                }
            }

            return config;
        }

        private async Task GenerateContributionReport(TContext context, List<UserContribution> contributions)
        {
            var report = new StringBuilder();
            report.AppendLine("# 🏆 Organization Contribution Report");
            report.AppendLine($"*Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss} UTC*");
            report.AppendLine();

            report.AppendLine("## Top Contributors");
            report.AppendLine("| Rank | User | Work Score | Commits | PRs | Issues | Reviews |");
            report.AppendLine("|------|------|------------|---------|-----|--------|---------|");

            var topContributors = contributions.Take(20).ToList();
            foreach (var contributor in topContributors)
            {
                var trophy = contributor.Rank switch
                {
                    1 => "🥇",
                    2 => "🥈",
                    3 => "🥉",
                    _ => ""
                };

                report.AppendLine($"| {trophy} #{contributor.Rank} | [{contributor.User.Login}]({contributor.User.HtmlUrl}) | {contributor.WorkScore:F1} | {contributor.CommitCount} | {contributor.PullRequestCount} | {contributor.IssueCount} | {contributor.CodeReviewCount} |");
            }

            report.AppendLine();
            report.AppendLine("## Scoring System");
            report.AppendLine("- **Commits**: 1.0 point each");
            report.AppendLine("- **Pull Requests**: 3.0 points each");
            report.AppendLine("- **Issues Created**: 1.5 points each");
            report.AppendLine("- **Code Reviews**: 2.0 points each");

            await _githubStorage.CreateIssueComment(context.Repository.Id, context.Number, report.ToString());
        }

        private async Task SyncDiscordRoles(List<UserContribution> contributions, FlowsSyncConfig config)
        {
            if (config.DiscordGuildId == 0)
            {
                Console.WriteLine("Discord Guild ID not provided, skipping Discord sync.");
                return;
            }

            await _discordService.ConnectAsync(config.DiscordBotToken);

            var githubToDiscordMapping = LoadGitHubToDiscordMapping();

            await _discordService.SyncRolesWithContributions(config.DiscordGuildId, contributions, githubToDiscordMapping);

            if (config.DiscordChannelId != 0)
            {
                await _discordService.GenerateContributionReport(config.DiscordChannelId, contributions);
            }

            await _discordService.DisconnectAsync();
        }

        private Dictionary<string, ulong> LoadGitHubToDiscordMapping()
        {
            try
            {
                var mappingKey = "github_discord_mapping";
                var mappingKeyLink = _fileStorage.CreateString(mappingKey);
                var filesInSet = _fileStorage.GetFilesFromSet(mappingKey);
                
                if (filesInSet.Any())
                {
                    var jsonString = filesInSet.First().Content;
                    return JsonSerializer.Deserialize<Dictionary<string, ulong>>(jsonString) ?? new Dictionary<string, ulong>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading GitHub to Discord mapping: {ex.Message}");
            }

            return new Dictionary<string, ulong>();
        }

        private async Task SaveContributionData(string organizationName, List<UserContribution> contributions)
        {
            try
            {
                var data = new
                {
                    Organization = organizationName,
                    GeneratedAt = DateTime.UtcNow,
                    Contributions = contributions.Select(c => new
                    {
                        GitHubLogin = c.User.Login,
                        GitHubId = c.User.Id,
                        WorkScore = c.WorkScore,
                        Rank = c.Rank,
                        CommitCount = c.CommitCount,
                        PullRequestCount = c.PullRequestCount,
                        IssueCount = c.IssueCount,
                        CodeReviewCount = c.CodeReviewCount
                    }).ToList()
                };

                var jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                var setName = $"contribution_data_{organizationName}";
                var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                
                var fileSet = _fileStorage.CreateFileSet(setName);
                var fileLink = _fileStorage.AddFile(jsonData);
                _fileStorage.AddFileToSet(fileSet, fileLink, fileName);
                
                Console.WriteLine($"Saved contribution data to set: {setName}, file: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving contribution data: {ex.Message}");
            }
        }
    }

    public class FlowsSyncConfig
    {
        public int MonthsToAnalyze { get; set; } = 3;
        public string DiscordBotToken { get; set; } = string.Empty;
        public ulong DiscordGuildId { get; set; } = 0;
        public ulong DiscordChannelId { get; set; } = 0;
        public bool SyncDiscordRoles { get; set; } = true;
    }
}