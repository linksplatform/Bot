using Interfaces;
using Octokit;
using Platform.Communication.Protocol.Lino;
using Storage.Remote.GitHub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bot
{
    internal class LastCommitActivityTrigger : ITrigger<Issue>
    {
        private readonly GitHubStorage Storage;

        private readonly Parser Parser = new();

        public LastCommitActivityTrigger(GitHubStorage storage) => Storage = storage;

        public bool Condition(Issue issue) => issue.Title.ToLower() == "last 3 months commit activity";

        public void Action(Issue issue)
        {
            var issueService = Storage.Client.Issue;
            var owner = issue.Repository.Owner.Login;
            var users = GetActivities(GetIgnoredRepositories(Parser.Parse(issue.Body)), owner);
            StringBuilder sb = new();
            sb.Append("```");
            foreach (var user in users)
            {
                sb.AppendLine($"{Environment.NewLine}" + user.Url.Replace("api.", "").Replace("users/", ""));
                foreach (var repo in user.Repositories)
                {
                    sb.AppendLine(repo.Replace("api.", "").Replace("repos/", ""));
                }
            }
            Console.WriteLine(sb.Append("```").ToString());
            issueService.Comment.Create(owner, issue.Repository.Name, issue.Number, sb.Append("```").ToString());
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

        public HashSet<Activity> GetActivities(HashSet<string> ignoredRepositories, string owner)
        {
            HashSet<Activity> activeUsers = new();
            foreach (var repository in Storage.Client.Repository.GetAllForOrg(owner).Result)
            {
                if (ignoredRepositories.Contains(repository.Name))
                {
                    continue;
                }
                foreach (var commit in Storage.GetCommits(repository.Owner.Login, repository.Name, DateTime.Today.AddMonths(-3)))
                {
                    if (!activeUsers.Any(x => x.Url == commit.Author.Url))
                    {
                        activeUsers.Add(new Activity() { Url = commit.Author.Url, Repositories = new List<string> { repository.Url } });
                    }
                    else
                    {
                        if (!activeUsers.Any(x => x.Repositories.Any(y => y == repository.Url) == true))
                        {
                            activeUsers.FirstOrDefault(x => x.Url == commit.Author.Url).Repositories.Add(repository.Url);
                        }
                    }
                }
            }
            return activeUsers;
        }
    }
}
