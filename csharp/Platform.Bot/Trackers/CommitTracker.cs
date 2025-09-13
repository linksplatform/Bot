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
    /// Represents the commit tracker.
    /// </para>
    /// <para></para>
    /// </summary>
    public class CommitTracker : ITracker<GitHubCommit>
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
        private IList<ITrigger<GitHubCommit>> _triggers;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="CommitTracker"/> instance.
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
        public CommitTracker(GitHubStorage storage, params ITrigger<GitHubCommit>[] triggers)
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
                    // Get commits from the main branch only
                    var commits = _storage.GetCommits(repository.Id, new CommitRequest { Sha = repository.DefaultBranch }).AwaitResult();
                    
                    foreach (var commit in commits)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                        
                        if (await trigger.Condition(commit))
                        {
                            await trigger.Action(commit);
                        }
                    }
                }
            }
        }
    }
}