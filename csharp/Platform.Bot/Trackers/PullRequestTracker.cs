using Interfaces;
using Octokit;
using Storage.Remote.GitHub;
using System.Collections.Generic;
using System.Threading;
using Platform.Threading;

namespace Platform.Bot.Trackers
{
    using TContext = PullRequest;
    /// <summary>
    /// <para>
    /// Represents the programmer role.
    /// </para>
    /// <para></para>
    /// </summary>
    public class PullRequestTracker : ITracker<TContext>
    {
        /// <summary>
        /// <para>
        /// The git hub api.
        /// </para>
        /// <para></para>
        /// </summary>
        public GitHubStorage Storage { get; }

        /// <summary>
        /// <para>
        /// The triggers.
        /// </para>
        /// <para></para>
        /// </summary>
        public List<ITrigger<TContext>> Triggers { get; }

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
        public PullRequestTracker(List<ITrigger<TContext>> triggers, GitHubStorage storage)
        {
            Storage = storage;
            Triggers = triggers;
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
        public void Start(CancellationToken cancellationToken)
        {
            foreach (var trigger in Triggers)
            {
                foreach (var repository in Storage.Client.Repository.GetAllForOrg("linksplatform").AwaitResult())
                {
                    foreach (var pullRequest in Storage.Client.PullRequest.GetAllForRepository(repository.Id).AwaitResult())
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                        if (trigger.Condition(pullRequest))
                        {
                            trigger.Action(pullRequest);
                        }
                    }
                }
            }
        }
    }
}
