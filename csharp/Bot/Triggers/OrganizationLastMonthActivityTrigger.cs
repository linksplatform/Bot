using Interfaces;
using Octokit;
using Platform.Communication.Protocol.Lino;
using Storage.Remote.GitHub;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bot
{
    internal class OrganizationLastMonthActivityTrigger : ITrigger<Issue>
    {
        private readonly GitHubStorage Storage;

        private readonly Parser Parser = new();

        public OrganizationLastMonthActivityTrigger(GitHubStorage storage) => Storage = storage;

        public bool Condition(Issue issue) => issue.Title.ToLower() == "organization last month activity";

        public void Action(Issue issue)
        {
            var issueService = Storage.Client.Issue;
            var owner = issue.Repository.Owner.Login;
            var activeUsersString = string.Join("\n", GetActiveUsers(GetIgnoredRepositories(Parser.Parse(issue.Body)), owner));
            issueService.Comment.Create(owner, issue.Repository.Name, issue.Number, activeUsersString);
            Storage.CloseIssue(issue);
        }

        public HashSet<string> GetIgnoredRepositories(IList<Link> links)
        {
            HashSet<string> ignoredRepos = new() { };
            foreach (var link in links)
            {
                var values = link.Values;
                if (values != null && values.Count == 3 && string.Equals(values.First().Id, "ignore", StringComparison.OrdinalIgnoreCase) && string.Equals(values.Last().Id.Trim('.'), "repository", StringComparison.OrdinalIgnoreCase))
                {
                    ignoredRepos.Add(values[1].Id);
                }
            }
            return ignoredRepos;
        }

        public HashSet<string> GetActiveUsers(HashSet<string> ignoredRepositories, string owner)
        {
            var date = DateTime.Today.AddMonths(-1);
            HashSet<string> activeUsers = new();
            foreach (var repository in Storage.Client.Repository.GetAllForOrg(owner).Result)
            {
                if (ignoredRepositories.Contains(repository.Name))
                {
                    continue;
                }
                foreach (var commit in Storage.GetCommits(repository.Owner.Login, repository.Name, DateTime.Today.AddMonths(-1)))
                {

                    activeUsers.Add(commit.Author.Login);

                }
                foreach (var pullRequest in Storage.GetPullRequests(repository.Owner.Login, repository.Name))
                {
                    foreach (var reviewer in pullRequest.RequestedReviewers)
                    {
                        if (pullRequest.CreatedAt < date || pullRequest.UpdatedAt < date || pullRequest.ClosedAt < date || pullRequest.MergedAt < date)
                        {
                            activeUsers.Add(reviewer.Login);
                        }
                    }
                }
                foreach (var createdIssue in Storage.GetIssues(repository.Owner.Login, repository.Name))
                {
                    activeUsers.Add(createdIssue.User.Login);
                }
            }
            return activeUsers;
        }
    }
}
