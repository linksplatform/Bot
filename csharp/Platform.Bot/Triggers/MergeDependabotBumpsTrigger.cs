using System;
using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Platform.Threading;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers
{
    public class MergeDependabotBumpsTrigger : ITrigger<PullRequest>
    {
        private readonly GitHubStorage _githubStorage;
        public MergeDependabotBumpsTrigger(GitHubStorage storage)
        {
            _githubStorage = storage;
        }

        public async Task<bool> Condition(PullRequest pullRequest)
        {
            var isDependabotAuthor = GitHubStorage.DependabotId == pullRequest.User.Id;
            if (!isDependabotAuthor)
            {
                return false;
            }
            var isDeployCheckCompleted = false;
            var hasDeployCheck = false;
            var repositoryId = pullRequest.Base.Repository.Id;
            var checks = _githubStorage.Client.Check.Run.GetAllForReference(repositoryId, pullRequest.Head.Sha).AwaitResult();
            foreach (var checkRun in checks.CheckRuns)
            {
                if (checkRun.Name is "testAndDeploy" or "deploy")
                {
                    hasDeployCheck = true;
                    if(CheckStatus.Completed == checkRun.Status.Value)
                    {
                        isDeployCheckCompleted = true;
                    }
                }
            }
            var isMergable = pullRequest.Mergeable ?? false;
            return (isDeployCheckCompleted || !hasDeployCheck) && isMergable;
        }

        public async Task Action(PullRequest pullRequest)
        {
            var repositoryId = pullRequest.Base.Repository.Id;
            var prMerge = _githubStorage.Client.PullRequest.Merge(repositoryId, pullRequest.Number, new MergePullRequest()).AwaitResult();
            Console.WriteLine($"{pullRequest.HtmlUrl} is {(prMerge.Merged ? "successfully":"not successfully")} merged.");
        }
    }
}
