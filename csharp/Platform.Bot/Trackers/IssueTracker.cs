using Interfaces;
using Octokit;
using Storage.Remote.GitHub;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.Bot.Trackers
{
    /// <summary>
    /// <para>
    /// Represents the programmer role.
    /// </para>
    /// <para></para>
    /// </summary>
    public class IssueTracker : ITracker<Issue>
    {
        /// <summary>
        /// <para>
        /// The git hub api.
        /// </para>
        /// <para></para>
        /// </summary>
        private GitHubStorage _storage { get; }

        /// <summary>
        /// <para>
        /// The triggers.
        /// </para>
        /// <para></para>
        /// </summary>
        private IList<ITrigger<Issue>> _triggers { get; }

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="IssueTracker"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="triggers">
        /// <para>A triggers.</para>
        /// <para></para>
        /// </param>
        /// <param name="gitHubApi">
        /// <para>A git hub api.</para>
        /// <para></para>
        /// </param>
        public IssueTracker(GitHubStorage gitHubApi, params ITrigger<Issue>[] triggers)
        {
            _storage = gitHubApi;
            _triggers = triggers;
        }

        /// <summary>
        /// <para>
        /// Starts the cancellation token.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="cancellationToken">
        /// <para>The cancellation token.</para>
        /// <para></para>
        /// </param>
        public async Task Start(CancellationToken cancellationToken)
        {
            var allIssues = _storage.GetIssues();
            foreach (var issue in allIssues)
            {
                foreach (var trigger in _triggers)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    if (await trigger.Condition(issue))
                    {
                        await trigger.Action(issue);
                    }
                }
            }
        }
    }
}
