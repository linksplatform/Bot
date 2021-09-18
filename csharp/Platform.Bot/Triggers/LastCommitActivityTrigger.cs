using Interfaces;
using Octokit;
using Platform.Communication.Protocol.Lino;
using Storage.Remote.GitHub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Platform.Bot
{
    internal class LastCommitActivityTrigger : ITrigger<Issue>
    {
        private readonly GitHubStorage Storage;

        private readonly Parser Parser = new();

        public LastCommitActivityTrigger(GitHubStorage storage) => Storage = storage;

        public bool Condition(Issue issue) => issue.Title.Contains("months commit activity")&& issue.Title.Contains("last");

        public void Action(Issue issue)
        {
            var issueService = Storage.Client.Issue;
            var owner = issue.Repository.Owner.Login;
            var months = GetActivities(GetIgnoredRepositories(Parser.Parse(issue.Body)), owner,GetSince(issue.Title));
            StringBuilder sb = new();
            sb.Append("```\n last " + GetSince(issue.Title).Count + " months commit activity\n");
            foreach (var month in months)
            {
                sb.AppendLine("\n\n\n"+ month.Last().Dates.First().ToString() + " - " + month.Last().Dates.Last().ToString());
                foreach (var user in month)
                {
                    if (user?.Url == null)
                    {
                        continue;
                    }
                    sb.AppendLine($"{Environment.NewLine}" + user.Url.Replace("api.", "").Replace("users/", ""));
                    foreach (var repo in user.Repositories)
                    {
                        sb.AppendLine(repo.Replace("api.", "").Replace("repos/", ""));
                    }
                }
            }
            Console.WriteLine(sb.Append("```").ToString());
            //issueService.Comment.Create(owner, issue.Repository.Name, issue.Number, sb.Append("```").ToString());
            //Storage.CloseIssue(issue);
        }
        private bool ContainsNumber(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] >= '0' && text[i] <= '9')
                {
                    return true;
                }
            }
            return false;
        }

        public List<Since> GetSince(string text)
        {
            if (ContainsNumber(text))
            {
                var sinces = new List<Since>();
                var mon = int.Parse(Regex.Match(text, @"(\d)+",
               RegexOptions.IgnoreCase).Value);
                for (int i = 0; i < mon; ++i)
                {
                    sinces.Add(new Since()
                    {
                        StartDate = DateTime.Now.AddMonths(-1 * i).AddDays(1 - DateTime.Now.Day),
                        EndDate = DateTime.Now.AddMonths(-1 * i + 1).AddDays(0 - DateTime.Now.Day)
                    });
                }
                return sinces;
            }
            return new List<Since>() 
            {
            new Since()
            {
              StartDate = DateTime.Now.AddMonths(-1).AddDays(1 - DateTime.Now.Day),
              EndDate = DateTime.Now.AddMonths(0).AddDays(0 - DateTime.Now.Day)
            }
            };
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

        public HashSet<Activity> GetActivitiesInRepos(HashSet<string> ignoredRepositories, string owner, DateTime date)
        {
            HashSet<Activity> activeUsers = new();
            foreach (var repository in Storage.Client.Repository.GetAllForOrg(owner).Result)
            {
                if (ignoredRepositories.Contains(repository.Name))
                {
                    continue;
                }
                foreach (var commit in Storage.GetCommits(repository.Owner.Login, repository.Name, date))
                {
                    if (!activeUsers.Any(x => x.Url == commit.Author.Url))
                    {
                        activeUsers.Add(new Activity() { Url = commit.Author.Url, Repositories = new List<string> { repository.Url }});
                    }
                    else
                    {
                        if (!activeUsers.Any(x => x.Repositories.Any(y => y == repository.Url) == true))
                        {
                            var user = activeUsers.FirstOrDefault(x => x.Url == commit.Author.Url);
                            user.Repositories.Add(repository.Url);
                        }
                    }
                    activeUsers.FirstOrDefault(x => x.Url == commit.Author.Url).Dates.Add(commit.Commit.Committer.Date.DateTime);
                }
            }
            return activeUsers;
        }

        public List<HashSet<Activity>> GetActivities(HashSet<string> ignoredRepositories, string owner, List<Since> months)
        {
            List<HashSet<Activity>> activeUsers = new() { };
            var allActiveUsers = GetActivitiesInRepos(ignoredRepositories, owner, DateTime.Now.AddMonths(-1*months.Count));
            foreach(var month in months)
            {
                activeUsers.Add(new HashSet<Activity>());
                var activeUsersInMonth = activeUsers.Last();
                foreach(var activeUser in allActiveUsers)
                {
                    foreach (var date in activeUser.Dates)
                    {
                        if (month.StartDate < date && month.EndDate > date)
                        {
                            if (!activeUsersInMonth.Any(x => x.Url == activeUser.Url))
                            {
                                activeUsersInMonth.Add(new Activity()
                                {
                                    Url = activeUser.Url,
                                    Repositories = activeUser.Repositories,
                                    Dates = activeUser.Dates
                                });
                            }
                        }
                    }
                }
                activeUsersInMonth.Add(new Activity());
                activeUsersInMonth.Last().Dates.Add(month.StartDate);
                activeUsersInMonth.Last().Dates.Add(month.EndDate);
            }
            return activeUsers;
        }
    }
}
