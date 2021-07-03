using Interfaces;
using Octokit;
using Storage.Remote.GitHub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Bot
{
    public class ProgrammerRole
    {
        public readonly GitHubStorage GitHubAPI;

        public readonly TimeSpan MinimumInteractionInterval;

        public readonly List<ITrigger<Issue>> Triggers;

        public ProgrammerRole(List<ITrigger<Issue>> triggers,IRemoteCodeStorage<Issue> gitHubAPI)
        {
            GitHubAPI = (GitHubStorage)gitHubAPI;
            Triggers = triggers;
            MinimumInteractionInterval = gitHubAPI.MinimumInteractionInterval;
        }

        private void ProcessIssues(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                foreach (var trigger in Triggers)
                {
                    foreach (var issue in GitHubAPI.GetIssues())
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

        public void Start(CancellationToken cancellationToken)
        {
            ProcessIssues(cancellationToken);
        }
    }
}
