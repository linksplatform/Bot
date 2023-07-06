using Interfaces;
using Octokit;
using Storage.Remote.GitHub;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Platform.Collections.Lists;
using Platform.Threading;

namespace Platform.Bot.Trackers
{
    /// <summary>
    /// <para>
    /// Represents the programmer role.
    /// </para>
    /// <para></para>
    /// </summary>
    public class PullRequestTracker : ITracker<PullRequest>
    {
        /// <summary>
        /// <para>
        /// The git hub api.
        /// </para>
        /// <para></para>
        /// </summary>
        private GitHubStorage _storage;

        /// <summary>
        /// <para>
        /// The triggers.
        /// </para>
        /// <para></para>
        /// </summary>
        private IList<ITrigger<PullRequest>> _triggers;

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
        /// <param name="storage">
        /// <para>A git hub api.</para>
        /// <para></para>
        /// </param>
        public PullRequestTracker(GitHubStorage storage, params ITrigger<PullRequest>[] triggers)
        {
            _storage = storage;
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
            foreach (var trigger in _triggers)
            {
                foreach (var repository in _storage.Client.Repository.GetAllForOrg("linksplatform").AwaitResult())
                {
                    foreach (var pullRequest in _storage.Client.PullRequest.GetAllForRepository(repository.Id).AwaitResult())
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                        var detailedPullRequest = _storage.Client.PullRequest.Get(repository.Id, pullRequest.Number).AwaitResult();
                        if (await trigger.Condition(detailedPullRequest))
                        {
                            await trigger.Action(detailedPullRequest);
                        }
                    }
                }
            }
        }
    }
}
