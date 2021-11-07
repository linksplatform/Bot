using Interfaces;
using Octokit;
using Storage.Remote.GitHub;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Platform.Bot
{
    /// <summary>
    /// <para>
    /// Represents the programmer role.
    /// </para>
    /// <para></para>
    /// </summary>
    public class ProgrammerRole
    {
        /// <summary>
        /// <para>
        /// The git hub api.
        /// </para>
        /// <para></para>
        /// </summary>
        public readonly GitHubStorage GitHubAPI;

        /// <summary>
        /// <para>
        /// The minimum interaction interval.
        /// </para>
        /// <para></para>
        /// </summary>
        public readonly TimeSpan MinimumInteractionInterval;

        /// <summary>
        /// <para>
        /// The triggers.
        /// </para>
        /// <para></para>
        /// </summary>
        public readonly List<ITrigger<Issue>> Triggers;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="ProgrammerRole"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="triggers">
        /// <para>A triggers.</para>
        /// <para></para>
        /// </param>
        /// <param name="gitHubAPI">
        /// <para>A git hub api.</para>
        /// <para></para>
        /// </param>
        public ProgrammerRole(List<ITrigger<Issue>> triggers, GitHubStorage gitHubAPI)
        {
            GitHubAPI = gitHubAPI;
            Triggers = triggers;
            MinimumInteractionInterval = gitHubAPI.MinimumInteractionInterval;
        }

        /// <summary>
        /// <para>
        /// Processes the issues using the specified token.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="token">
        /// <para>The token.</para>
        /// <para></para>
        /// </param>
        private void ProcessIssues(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                foreach (var issue in GitHubAPI.GetIssues())
                {
                    foreach (var trigger in Triggers)
                    {
                        if (trigger.Condition(issue))
                        {
                            trigger.Action(issue);
                        }
                    }
                }
                Thread.Sleep(MinimumInteractionInterval);
            }
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
        public void Start(CancellationToken cancellationToken) => ProcessIssues(cancellationToken);
    }
}
