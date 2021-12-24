using System;
using Interfaces;
using Octokit;
using Platform.Threading;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers
{
    using TContext = PullRequest;
    public class MergeDependabotBumpsTrigger : ITrigger<TContext>
    {
        private const int DependabotId = 49699333;
        private readonly GitHubStorage _gitHubApi;
        public MergeDependabotBumpsTrigger(GitHubStorage gitHubApi)
        {
            _gitHubApi = gitHubApi;
        }

        public bool Condition(TContext context)
        {
            var isDependabotAuthor = DependabotId == context.User.Id;
            var isTestAndDeployCompleted = false;
            var repositoryId = context.Base.Repository.Id;
            var checks = _gitHubApi.Client.Check.Run.GetAllForReference(repositoryId, context.Head.Sha).AwaitResult();
            foreach (var checkRun in checks.CheckRuns)
            {
                if (checkRun.Name == "testAndDeploy" && checkRun.Status.Value == CheckStatus.Completed)
                {
                    isTestAndDeployCompleted = true;
                }
            }
            return isDependabotAuthor && isTestAndDeployCompleted;
        }

        public void Action(TContext context)
        {
            var repositoryId = context.Base.Repository.Id;
            var prMerge = _gitHubApi.Client.PullRequest.Merge(repositoryId, context.Number, new MergePullRequest()).AwaitResult();
            Console.WriteLine($"{context.HtmlUrl} is {(prMerge.Merged ? "successfully":"not successfully")} merged.");
        }
    }
}
