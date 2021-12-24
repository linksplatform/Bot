using Interfaces;
using Octokit;
using Platform.Communication.Protocol.Lino;
using Storage.Remote.GitHub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;
    internal class LastCommitActivityTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _gitHubApi;

        private readonly Parser _parser = new();

        public LastCommitActivityTrigger(GitHubStorage gitHubApi) => _gitHubApi = gitHubApi;

        public bool Condition(TContext context) => context.Title.ToLower() == "last 3 months commit activity";

        public void Action(TContext context)
        {
            var issueService = _gitHubApi.Client.Issue;
            var owner = context.Repository.Owner.Login;
            var ignoredRepositories =
                context.Body != null ? GetIgnoredRepositories(_parser.Parse(context.Body)) : default;
            var users = GetActivities(owner, ignoredRepositories);
            StringBuilder sb = new();
            foreach (var user in users)
            {
                sb.AppendLine($"- **{user.Url.Replace("api.", "").Replace("users/", "")}**");
                // Break line
                sb.AppendLine("------------------");
            }
            var result = sb.ToString();
            var comment = issueService.Comment.Create(owner, context.Repository.Name, context.Number, result);
            comment.Wait();
            Console.WriteLine($"Last commit activity comment is added: {comment.Result.HtmlUrl}");
            _gitHubApi.CloseIssue(context);
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

        public HashSet<Activity> GetActivities(string owner, HashSet<string> ignoredRepositories = default)
        {
            HashSet<Activity> activeUsers = new();
            foreach (var repository in _gitHubApi.Client.Repository.GetAllForOrg(owner).Result)
            {
                if (ignoredRepositories?.Contains(repository.Name) ?? false)
                {
                    continue;
                }
                foreach (var commit in _gitHubApi.GetCommits(repository.Owner.Login, repository.Name, DateTime.Today.AddMonths(-3)))
                {
                    if (!_gitHubApi.Client.Organization.Member.CheckMember(owner, commit.Author.Login).Result)
                    {
                        continue;
                    }
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
