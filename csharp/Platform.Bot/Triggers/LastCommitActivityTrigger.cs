using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Interfaces;
using Octokit;
using Platform.Communication.Protocol.Lino;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;
    internal class LastCommitActivityTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage Storage;

        private readonly Parser Parser = new();

        public LastCommitActivityTrigger(GitHubStorage storage) => Storage = storage;

        public bool Condition(TContext context) => context.Title.ToLower() == "last 3 months commit activity";

        public void Action(TContext context)
        {
            var issueService = Storage.Client.Issue;
            var owner = context.Repository.Owner.Login;
            var users = GetActivities(GetIgnoredRepositories(Parser.Parse(context.Body)), owner);
            StringBuilder sb = new();
            foreach (var user in users)
            {
                sb.AppendLine($"- **{user.Url.Replace("api.", "").Replace("users/", "")}**");
                foreach (var repo in user.Repositories)
                {
                    sb.AppendLine($"  - {repo.Replace("api.", "").Replace("repos/", "")}");
                }
                // Break line
                sb.AppendLine("------------------");
            }
            var result = sb.ToString();
            var comment = issueService.Comment.Create(owner, context.Repository.Name, context.Number, result);
            comment.Wait();
            Console.WriteLine($"Last commit activity comment is added: {comment.Result.HtmlUrl}");
            Storage.CloseIssue(context);
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
