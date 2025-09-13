using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Storage.Remote.GitHub;
using Platform.Bot.Services;

namespace Platform.Bot.Triggers
{
    /// <summary>
    /// Handles team invitation requests via GitHub issues
    /// Automatically invites approved users to both GitHub organization and Discord server
    /// </summary>
    public class TeamInvitationTrigger : ITrigger<Issue>
    {
        private readonly GitHubStorage _githubStorage;
        private readonly DiscordService _discordService;
        private readonly Regex _invitationPattern = new(@"@bot\s+invite\s+@?(\w+)", RegexOptions.IgnoreCase);

        public TeamInvitationTrigger(GitHubStorage githubStorage, DiscordService discordService)
        {
            _githubStorage = githubStorage;
            _discordService = discordService;
        }

        public async Task<bool> Condition(Issue issue)
        {
            if (issue.State.Value != ItemState.Open)
                return false;

            var match = _invitationPattern.Match(issue.Body ?? "");
            if (!match.Success)
                return false;

            var issueAuthorPermission = await _githubStorage.Client.Repository.Collaborator.ReviewPermission(issue.Repository.Id, issue.User.Login);
            return issueAuthorPermission.Permission == "admin" || issueAuthorPermission.Permission == "maintain";
        }

        public async Task Action(Issue issue)
        {
            var match = _invitationPattern.Match(issue.Body ?? "");
            if (!match.Success)
                return;

            var usernameToInvite = match.Groups[1].Value;

            try
            {
                await _githubStorage.InviteToOrganization(_githubStorage.Owner, usernameToInvite);
                await _githubStorage.CreateIssueComment(issue.Repository.Id, issue.Number, 
                    $"✅ Successfully sent GitHub organization invitation to @{usernameToInvite}");

                if (_discordService != null)
                {
                    var inviteLink = await _discordService.CreateInviteLink();
                    if (!string.IsNullOrEmpty(inviteLink))
                    {
                        await _githubStorage.CreateIssueComment(issue.Repository.Id, issue.Number, 
                            $"🎮 Discord invite link for @{usernameToInvite}: {inviteLink}");
                    }
                }

                _githubStorage.CloseIssue(issue);
            }
            catch (System.Exception ex)
            {
                await _githubStorage.CreateIssueComment(issue.Repository.Id, issue.Number, 
                    $"❌ Failed to invite @{usernameToInvite}: {ex.Message}");
            }
        }
    }
}