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
    /// Represents the list rewards trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    public class ListRewardsTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        private readonly RewardsService _rewardsService;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="ListRewardsTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">The GitHub storage.</param>
        /// <param name="rewardsService">The rewards service.</param>
        public ListRewardsTrigger(GitHubStorage storage, RewardsService rewardsService)
        {
            _storage = storage;
            _rewardsService = rewardsService;
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
            var title = context.Title.Trim().ToLower();
            return title == "list rewards" || title == "show rewards" || title == "rewards";
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
            var rewardsDisplay = _rewardsService.FormatRewardsForDisplay();
            await _storage.CreateIssueComment(context.Repository.Id, context.Number, rewardsDisplay);
            _storage.CloseIssue(context);
        }
    }
}