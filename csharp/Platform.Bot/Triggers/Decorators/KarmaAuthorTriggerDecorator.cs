using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Platform.Bot.Services;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers.Decorators
{
    /// <summary>
    /// <para>
    /// Decorator that checks if the issue author has sufficient karma before executing the trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    public class KarmaAuthorTriggerDecorator : ITrigger<Issue>
    {
        public readonly ITrigger<Issue> Trigger;
        public readonly KarmaService KarmaService;
        public readonly GitHubStorage GitHubStorage;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="KarmaAuthorTriggerDecorator"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="trigger">The trigger to decorate.</param>
        /// <param name="karmaService">The karma service.</param>
        /// <param name="gitHubStorage">The GitHub storage.</param>
        public KarmaAuthorTriggerDecorator(ITrigger<Issue> trigger, KarmaService karmaService, GitHubStorage gitHubStorage)
        {
            Trigger = trigger;
            KarmaService = karmaService;
            GitHubStorage = gitHubStorage;
        }

        /// <summary>
        /// <para>
        /// Determines whether this instance condition is met and user has sufficient karma.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="issue">The issue context.</param>
        /// <returns>True if condition is met and user has sufficient karma.</returns>
        public virtual async Task<bool> Condition(Issue issue)
        {
            var username = issue.User.Login;
            var hasSufficientKarma = KarmaService.HasSufficientKarma(username);
            var baseCondition = await Trigger.Condition(issue);
            
            return baseCondition && hasSufficientKarma;
        }

        /// <summary>
        /// <para>
        /// Executes the decorated trigger action if karma check passes.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="issue">The issue context.</param>
        public virtual async Task Action(Issue issue)
        {
            var username = issue.User.Login;
            
            if (!KarmaService.HasSufficientKarma(username))
            {
                var minKarma = KarmaService.GetMinimumKarmaForRewards();
                var userKarma = KarmaService.GetOrInitializeUserKarma(username);
                await GitHubStorage.CreateIssueComment(issue.Repository.Id, issue.Number, 
                    $"❌ Insufficient karma. You have {userKarma.KarmaPoints} karma points, but need at least {minKarma} for this action.");
                GitHubStorage.CloseIssue(issue);
                return;
            }

            await Trigger.Action(issue);
        }
    }
}