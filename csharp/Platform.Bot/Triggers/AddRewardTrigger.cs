using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Platform.Bot.Services;
using Storage.Local;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;

    /// <summary>
    /// <para>
    /// Represents the add reward trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    public class AddRewardTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        private readonly RewardsService _rewardsService;
        private readonly KarmaService _karmaService;
        private readonly Regex _addRewardPattern;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="AddRewardTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">The GitHub storage.</param>
        /// <param name="rewardsService">The rewards service.</param>
        /// <param name="karmaService">The karma service.</param>
        public AddRewardTrigger(GitHubStorage storage, RewardsService rewardsService, KarmaService karmaService)
        {
            _storage = storage;
            _rewardsService = rewardsService;
            _karmaService = karmaService;
            
            // Pattern to match: "add reward <issue_url> <description>"
            _addRewardPattern = new Regex(@"^add reward\s+(https://github\.com/[\w\-\.]+/[\w\-\.]+/issues/\d+)\s+(.+)$", RegexOptions.IgnoreCase);
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
            return _addRewardPattern.IsMatch(title);
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
            var match = _addRewardPattern.Match(context.Title.Trim());
            if (!match.Success)
            {
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, "❌ Invalid format. Use: `add reward <issue_url> <description>`");
                _storage.CloseIssue(context);
                return;
            }

            var issueUrl = match.Groups[1].Value;
            var description = match.Groups[2].Value;
            var username = context.User.Login;

            // Check if user has sufficient karma
            if (!_karmaService.HasSufficientKarma(username))
            {
                var minKarma = _karmaService.GetMinimumKarmaForRewards();
                var userKarma = _karmaService.GetOrInitializeUserKarma(username);
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                    $"❌ Insufficient karma. You have {userKarma.KarmaPoints} karma points, but need at least {minKarma} to add rewards.");
                _storage.CloseIssue(context);
                return;
            }

            // Validate issue URL
            if (!await _rewardsService.IsValidIssueUrl(issueUrl))
            {
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, "❌ Invalid or inaccessible issue URL. Please check the URL and try again.");
                _storage.CloseIssue(context);
                return;
            }

            // Add the reward
            if (_rewardsService.AddReward(issueUrl, description, username))
            {
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                    $"✅ Reward added successfully!\n\n**Issue:** {issueUrl}\n**Description:** {description}\n**Added by:** {username}");
            }
            else
            {
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, "❌ Failed to add reward. The issue might already have a reward.");
            }

            _storage.CloseIssue(context);
        }
    }
}