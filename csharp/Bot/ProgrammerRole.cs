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
        public GitHubStorage gitHubAPI;

        public TimeSpan MinimumInteractionInterval;

        public List<ITrigger<Issue>> triggers;

        public ProgrammerRole(List<ITrigger<Issue>> triggers,IRemoteCodeStorage<Issue> gitHubAPI)
        {
            this.gitHubAPI = (GitHubStorage)gitHubAPI;
            this.triggers = triggers;
            MinimumInteractionInterval = gitHubAPI.MinimumInteractionInterval;
        }

        private void ProcessIssues(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                foreach (var trigger in triggers)
                {
                    foreach (var issue in gitHubAPI.GetIssues())
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
