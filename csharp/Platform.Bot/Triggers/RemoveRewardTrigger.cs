using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Platform.Bot.Services;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;

    /// <summary>
    /// <para>
    /// Represents the remove reward trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    public class RemoveRewardTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        private readonly RewardsService _rewardsService;
        private readonly KarmaService _karmaService;
        private readonly Regex _removeRewardPattern;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="RemoveRewardTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">The GitHub storage.</param>
        /// <param name="rewardsService">The rewards service.</param>
        /// <param name="karmaService">The karma service.</param>
        public RemoveRewardTrigger(GitHubStorage storage, RewardsService rewardsService, KarmaService karmaService)
        {
            _storage = storage;
            _rewardsService = rewardsService;
            _karmaService = karmaService;
            
            // Pattern to match: "remove reward <issue_url>"
            _removeRewardPattern = new Regex(@"^remove reward\s+(https://github\.com/[\w\-\.]+/[\w\-\.]+/issues/\d+)$", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// <para>
        /// Determines whether this instance condition.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The bool</returns>
        public async Task<bool> Condition(TContext context)
        {
            var title = context.Title.Trim();
            return _removeRewardPattern.IsMatch(title);
        }

        /// <summary>
        /// <para>
        /// Actions the context.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">The context.</param>
        public async Task Action(TContext context)
        {
            var match = _removeRewardPattern.Match(context.Title.Trim());
            if (!match.Success)
            {
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, "❌ Invalid format. Use: `remove reward <issue_url>`");
                _storage.CloseIssue(context);
                return;
            }

            var issueUrl = match.Groups[1].Value;
            var username = context.User.Login;

            // Check if user has sufficient karma to remove rewards
            if (!_karmaService.HasSufficientKarma(username))
            {
                var minKarma = _karmaService.GetMinimumKarmaForRewards();
                var userKarma = _karmaService.GetOrInitializeUserKarma(username);
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                    $"❌ Insufficient karma. You have {userKarma.KarmaPoints} karma points, but need at least {minKarma} to remove rewards.");
                _storage.CloseIssue(context);
                return;
            }

            // Remove the reward
            if (_rewardsService.RemoveReward(issueUrl))
            {
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                    $"✅ Reward removed successfully!\n\n**Issue:** {issueUrl}\n**Removed by:** {username}");
            }
            else
            {
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, "❌ Failed to remove reward. The reward might not exist or is already inactive.");
            }

            _storage.CloseIssue(context);
        }
    }
}