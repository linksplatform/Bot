using System;
using System.Diagnostics;
using Interfaces;
using Octokit;
using Storage.Remote.GitHub;

namespace Platform.Bot
{
    public class ProtectMainBranchTrigger : ITrigger<Issue>
    {
        private readonly GitHubStorage Storage;
        public ProtectMainBranchTrigger(GitHubStorage storage) => Storage = storage;
        public bool Condition(Issue issue) => issue.Title.ToLower() == "protect default branch";

        public async void Action(Issue issue)
        {
            
            // Protect master branch
            var update = new BranchProtectionSettingsUpdate(new BranchProtectionPushRestrictionsUpdate());
            await Storage.Client.Repository.Branch.UpdateBranchProtection(issue.Repository.Id, issue.Repository.DefaultBranch, update);
            Storage.CloseIssue(issue);
        }
    }
}