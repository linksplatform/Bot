using Discord;
using Discord.WebSocket;
using Platform.Bot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.Bot.Services
{
    public class RoleTier
    {
        public string RoleName { get; set; } = null!;
        public double MinWorkScore { get; set; }
        public Color RoleColor { get; set; }
        public string Description { get; set; } = null!;
    }

    public class DiscordRoleSyncService
    {
        private readonly DiscordSocketClient _discordClient;
        private readonly List<RoleTier> _roleTiers;

        public DiscordRoleSyncService(string discordToken)
        {
            _discordClient = new DiscordSocketClient();
            _roleTiers = InitializeRoleTiers();
        }

        private List<RoleTier> InitializeRoleTiers()
        {
            return new List<RoleTier>
            {
                new RoleTier { RoleName = "Contributor Legend", MinWorkScore = 100, RoleColor = Color.Gold, Description = "Exceptional contributors with outstanding work" },
                new RoleTier { RoleName = "Senior Contributor", MinWorkScore = 50, RoleColor = Color.Purple, Description = "Highly active contributors" },
                new RoleTier { RoleName = "Active Contributor", MinWorkScore = 20, RoleColor = Color.Blue, Description = "Regular contributors" },
                new RoleTier { RoleName = "Contributor", MinWorkScore = 5, RoleColor = Color.Green, Description = "Getting started contributors" },
                new RoleTier { RoleName = "Member", MinWorkScore = 0, RoleColor = Color.LightGrey, Description = "Organization members" }
            };
        }

        public async Task ConnectAsync(string token)
        {
            await _discordClient.LoginAsync(TokenType.Bot, token);
            await _discordClient.StartAsync();
            
            _discordClient.Ready += OnReady;
            _discordClient.Log += LogAsync;
        }

        private async Task OnReady()
        {
            Console.WriteLine($"Discord bot {_discordClient.CurrentUser} is connected!");
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        public async Task SyncRolesWithContributions(ulong guildId, List<UserContribution> contributions, Dictionary<string, ulong> githubToDiscordMapping)
        {
            var guild = _discordClient.GetGuild(guildId);
            if (guild == null)
            {
                Console.WriteLine($"Guild with ID {guildId} not found.");
                return;
            }

            await EnsureRolesExist(guild);

            var roles = guild.Roles.Where(r => _roleTiers.Any(tier => tier.RoleName == r.Name)).ToList();

            foreach (var contribution in contributions)
            {
                var githubLogin = contribution.User.Login;
                if (!githubToDiscordMapping.ContainsKey(githubLogin))
                {
                    Console.WriteLine($"No Discord mapping found for GitHub user: {githubLogin}");
                    continue;
                }

                var discordUserId = githubToDiscordMapping[githubLogin];
                var discordUser = guild.GetUser(discordUserId);
                
                if (discordUser == null)
                {
                    Console.WriteLine($"Discord user with ID {discordUserId} not found in guild.");
                    continue;
                }

                await AssignAppropriateRole(discordUser, contribution.WorkScore, roles);
            }
        }

        private async Task EnsureRolesExist(SocketGuild guild)
        {
            foreach (var tier in _roleTiers)
            {
                var existingRole = guild.Roles.FirstOrDefault(r => r.Name == tier.RoleName);
                if (existingRole == null)
                {
                    await guild.CreateRoleAsync(tier.RoleName, 
                        color: tier.RoleColor, 
                        isMentionable: true,
                        options: new RequestOptions { AuditLogReason = $"Created role for contribution tier: {tier.Description}" });
                    
                    Console.WriteLine($"Created role: {tier.RoleName}");
                }
            }
        }

        private async Task AssignAppropriateRole(SocketGuildUser user, double workScore, List<SocketRole> roles)
        {
            var appropriateTier = _roleTiers
                .Where(tier => workScore >= tier.MinWorkScore)
                .OrderByDescending(tier => tier.MinWorkScore)
                .FirstOrDefault();

            if (appropriateTier == null)
            {
                appropriateTier = _roleTiers.Last(); 
            }

            var targetRole = roles.FirstOrDefault(r => r.Name == appropriateTier.RoleName);
            if (targetRole == null)
            {
                Console.WriteLine($"Target role {appropriateTier.RoleName} not found.");
                return;
            }

            var tierRoles = roles.Where(r => _roleTiers.Any(tier => tier.RoleName == r.Name)).ToList();
            var currentTierRoles = user.Roles.Where(r => tierRoles.Contains(r)).ToList();

            if (currentTierRoles.Any(r => r.Id == targetRole.Id))
            {
                Console.WriteLine($"User {user.Username} already has appropriate role: {targetRole.Name}");
                return;
            }

            try
            {
                foreach (var roleToRemove in currentTierRoles)
                {
                    await user.RemoveRoleAsync(roleToRemove);
                }

                await user.AddRoleAsync(targetRole);
                Console.WriteLine($"Updated {user.Username} to role: {targetRole.Name} (Work Score: {workScore:F1})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating roles for {user.Username}: {ex.Message}");
            }
        }

        public async Task GenerateContributionReport(ulong channelId, List<UserContribution> contributions)
        {
            var channel = _discordClient.GetChannel(channelId) as IMessageChannel;
            if (channel == null)
            {
                Console.WriteLine($"Channel with ID {channelId} not found.");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("🏆 Organization Contribution Leaderboard")
                .WithDescription("Based on activity in public repositories")
                .WithColor(Color.Gold)
                .WithTimestamp(DateTimeOffset.Now);

            var topContributors = contributions.Take(10).ToList();
            
            for (int i = 0; i < topContributors.Count; i++)
            {
                var contributor = topContributors[i];
                var trophy = i switch
                {
                    0 => "🥇",
                    1 => "🥈", 
                    2 => "🥉",
                    _ => "🏅"
                };

                var fieldValue = $"**Score:** {contributor.WorkScore:F1}\n" +
                               $"Commits: {contributor.CommitCount} | PRs: {contributor.PullRequestCount}\n" +
                               $"Issues: {contributor.IssueCount} | Reviews: {contributor.CodeReviewCount}";

                embed.AddField($"{trophy} #{contributor.Rank} {contributor.User.Login}", fieldValue, true);
            }

            await channel.SendMessageAsync(embed: embed.Build());
        }

        public async Task DisconnectAsync()
        {
            await _discordClient.LogoutAsync();
            await _discordClient.StopAsync();
        }
    }
}