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
            var activeUsersString = string.Join("\n", GetActiveUsers(GetIgnoredRepositories(Parser.Parse(issue.Body))));
            var newIssue = Storage.Client.Issue;
            var owner = issue.Repository.Owner.Login ;
            newIssue.Comment.Create(owner, issue.Repository.Name, issue.Number, activeUsersString);
            newIssue.Update(owner, issue.Repository.Name, issue.Number, new IssueUpdate { State = ItemState.Closed });
        }

        public HashSet<Link> GetIgnoredRepositories(IList<Link> links)
        {
            HashSet<Link> ignoredRepos = new() { };
            foreach (var link in links)
            {
                if (link.Values.Count == 3 && string.Equals(link.Values.First().Id, "ignore", StringComparison.OrdinalIgnoreCase) && string.Equals(link.Values.Last().Id.Trim('.'), "repository", StringComparison.OrdinalIgnoreCase))
                {
                    ignoredRepos.Add(link.Values[1].Id);
                }
            }
            return ignoredRepos;
        }

        public HashSet<string> GetActiveUsers(HashSet<Link> ignoredRepositories)
        {
            HashSet<string> activeUsers = new();
            foreach (var repository in Storage.Client.Repository.GetAllForOrg("linksplatform").Result)
            {
                if (ignoredRepositories.Contains(repository.Name))
                {
                    continue;
                }
                foreach (var commit in Storage.GetCommits(repository.Owner.Login, repository.Name))
                {
                    activeUsers.Add(commit.Author.Login);
                }
                foreach (var pullRequest in Storage.GetPullRequests(repository.Owner.Login, repository.Name))
                {
                    foreach (var reviewer in pullRequest.RequestedReviewers)
                    {
                        activeUsers.Add(reviewer.Login);
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
