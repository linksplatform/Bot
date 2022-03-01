using System;
using Interfaces;
using Octokit;
using Platform.Threading;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers
{
    public class MergeDependabotBumpsTrigger : ITrigger<PullRequest>
    {
        private const int DependabotId = 49699333;
        private readonly GitHubStorage _githubStorage;
        public MergeDependabotBumpsTrigger(GitHubStorage storage)
        {
            _githubStorage = storage;
        }

        public bool Condition(PullRequest pullRequest)
        {
            var isDependabotAuthor = DependabotId == pullRequest.User.Id;
            if (!isDependabotAuthor)
            {
                return false;
            }
            var isTestAndDeployCompleted = false;
            var repositoryId = pullRequest.Base.Repository.Id;
            var checks = _githubStorage.Client.Check.Run.GetAllForReference(repositoryId, pullRequest.Head.Sha).AwaitResult();
            foreach (var checkRun in checks.CheckRuns)
            {
                if (checkRun.Name is "testAndDeploy" or "deploy" && checkRun.Status.Value == CheckStatus.Completed)
                {
                    isTestAndDeployCompleted = true;
                }
            }
            var isMergable = pullRequest.Mergeable ?? false;
            return isTestAndDeployCompleted && isMergable;
        }

        public void Action(PullRequest pullRequest)
        {
            var repositoryId = pullRequest.Base.Repository.Id;
            var prMerge = _githubStorage.Client.PullRequest.Merge(repositoryId, pullRequest.Number, new MergePullRequest()).AwaitResult();
            Console.WriteLine($"{pullRequest.HtmlUrl} is {(prMerge.Merged ? "successfully":"not successfully")} merged.");
        }
    }
}
