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

        public OrganizationLastMonthActivityTrigger(GitHubStorage storage) => Storage = storage;

        public void Action(Issue obj)
        {
            var links = (new Parser()).Parse(obj.Body);
            HashSet<Link> ignoredRepos = new() { };
            foreach (var link in links)
            {
                if (link.Values.Count == 3 && string.Equals(link.Values.First().Id, "ignore", StringComparison.OrdinalIgnoreCase) && string.Equals(link.Values.Last().Id.Trim('.'), "repository", StringComparison.OrdinalIgnoreCase))
                {
                    ignoredRepos.Add(link.Values[1].Id);
                }
            }
            HashSet<string> activeUsers = new();
            foreach (var repos in Storage.Client.Repository.GetAllForOrg("linksplatform").Result)
            {
                if (ignoredRepos.Contains(repos.Name))
                {
                    continue;
                }
                foreach (var commit in Storage.GetCommits(repos.Owner.Login, repos.Name))
                {
                    activeUsers.Add(commit.Author.Login);
                }
                foreach (var pullRequest in Storage.GetPullRequests(repos.Owner.Login, repos.Name))
                {
                    foreach (var a in pullRequest.RequestedReviewers)
                    {
                        activeUsers.Add(a.Login);
                    }
                }
                foreach (var isuue in Storage.GetIssues(repos.Owner.Login, repos.Name))
                {
                    activeUsers.Add(isuue.User.Login);
                }
            }
            var activeUsersString = string.Join("\n", activeUsers);
            Storage.Client.Issue.Comment.Create(obj.Repository.Owner.Login, obj.Repository.Name, obj.Number, activeUsersString);
            Storage.Client.Issue.Update(obj.Repository.Owner.Login, obj.Repository.Name, obj.Number, new IssueUpdate { State = ItemState.Closed });
        }

        public bool Condition(Issue obj) => obj.Title.ToLower() == "organization last month activity";
    }
}
