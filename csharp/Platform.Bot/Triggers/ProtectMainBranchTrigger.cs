using System.Collections.Generic;
using System.Linq;
using System.Text;
using Interfaces;
using Octokit;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;
    public class ProtectMainBranchTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        public ProtectMainBranchTrigger(GitHubStorage storage) => _storage = storage;
        public bool Condition(TContext context) => context.Title.ToLower() == "protect default branch in all organization's repositories";

        public void Action(TContext context)
        {
            var repositories = _storage.Client.Repository.GetAllForOrg(context.Repository.Owner.Login).Result;
            var results = UpdateRepositoriesDefaultBranchProtection(repositories);
            StringBuilder failedRepositoriesComment = new(repositories.Count * repositories[0].Name.Length);
            foreach (var result in results.Where(result => !result.Value))
            {
                failedRepositoriesComment.AppendLine($"- [ ] {result.Key}");
            }
            if (failedRepositoriesComment.Length != 0)
            {
                failedRepositoriesComment.AppendLine(
                    "TODO: Fix default branch protection of these repositories. Failed repositories:");
                _storage.Client.Issue.Comment.Create(context.Repository.Id, context.Number, failedRepositoriesComment.ToString());
            }
            else
            {
                _storage.Client.Issue.Comment.Create(context.Repository.Id, context.Number, "Success. All repositories default branch protection is updated.");
                _storage.CloseIssue(context);
            }
        }

        public Dictionary<string, bool> UpdateRepositoriesDefaultBranchProtection(IReadOnlyList<Repository> repositories)
        {
            Dictionary<string, bool> result = new(repositories.Count);
            foreach (var repository in repositories)
            {
                if (repository.Private)
                {
                    continue;
                }
                var update = new BranchProtectionSettingsUpdate(new BranchProtectionPushRestrictionsUpdate());
                var request =
                    _storage.Client.Repository.Branch.UpdateBranchProtection(repository.Id,
                        repository.DefaultBranch, update);
                request.Wait();
                result.Add(repository.Name, request.IsCompletedSuccessfully);
            }
            return result;
        }
    }
}
