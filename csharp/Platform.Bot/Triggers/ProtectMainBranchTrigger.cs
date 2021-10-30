using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Interfaces;
using Octokit;
using Storage.Remote.GitHub;

namespace Platform.Bot
{
    public class ProtectMainBranchTrigger : ITrigger<Issue>
    {
        private readonly GitHubStorage Storage;
        public ProtectMainBranchTrigger(GitHubStorage storage) => Storage = storage;
        public bool Condition(Issue issue) => issue.Title.ToLower() == "protect default branch in all organization's repositories";

        public void Action(Issue issue)
        {
            var repositories = Storage.Client.Repository.GetAllForOrg(issue.Repository.Owner.Login).Result;
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
                Storage.Client.Issue.Comment.Create(issue.Repository.Id, issue.Number, failedRepositoriesComment.ToString());
            }
            else
            {
                Storage.Client.Issue.Comment.Create(issue.Repository.Id, issue.Number, "Success. All repositories default branch protection is updated.");
                Storage.CloseIssue(issue);
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
                    Storage.Client.Repository.Branch.UpdateBranchProtection(repository.Id,
                        repository.DefaultBranch, update);
                request.Wait();
                result.Add(repository.Name, request.IsCompletedSuccessfully);
            }
            return result;
        }
    }
}