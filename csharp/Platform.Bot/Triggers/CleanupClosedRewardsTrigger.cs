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
    /// Represents a trigger that automatically removes rewards for closed issues.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    public class CleanupClosedRewardsTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        private readonly RewardsService _rewardsService;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="CleanupClosedRewardsTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">The GitHub storage.</param>
        /// <param name="rewardsService">The rewards service.</param>
        public CleanupClosedRewardsTrigger(GitHubStorage storage, RewardsService rewardsService)
        {
            _storage = storage;
            _rewardsService = rewardsService;
        }

        /// <summary>
        /// <para>
        /// Determines whether this instance condition is met.
        /// This trigger runs on all issues to check and cleanup closed reward issues.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>Always returns true to check all issues.</returns>
        public async Task<bool> Condition(TContext context)
        {
            // This trigger should run periodically to clean up closed issues
            // For now, we'll trigger on issues with title "cleanup rewards"
            var title = context.Title.Trim().ToLower();
            return title == "cleanup rewards" || title == "cleanup closed rewards";
        }

        /// <summary>
        /// <para>
        /// Actions the context by checking all active rewards and removing those for closed issues.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">The context.</param>
        public async Task Action(TContext context)
        {
            var activeRewards = _rewardsService.GetActiveRewards();
            int removedCount = 0;
            string removedRewards = "";

            foreach (var reward in activeRewards)
            {
                if (await _rewardsService.CheckAndRemoveClosedIssueReward(reward.IssueUrl))
                {
                    removedCount++;
                    removedRewards += $"- {reward.IssueUrl}: {reward.Description}\n";
                }
            }

            if (removedCount > 0)
            {
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                    $"🧹 **Cleanup Complete!**\n\n" +
                    $"Removed {removedCount} reward(s) for closed issues:\n\n" +
                    removedRewards);
            }
            else
            {
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                    "✅ **Cleanup Complete!**\n\nNo rewards needed to be removed. All active rewards are for open issues.");
            }

            _storage.CloseIssue(context);
        }
    }
}