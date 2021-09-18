using Interfaces;
using Octokit;
using Platform.Communication.Protocol.Lino;
using Storage.Remote.GitHub;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Platform.Bot
{
    /// <summary>
    /// <para>
    /// Represents the organization last month activity trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{Issue}"/>
    internal class OrganizationLastMonthActivityTrigger : ITrigger<Issue>
    {
        /// <summary>
        /// <para>
        /// The storage.
        /// </para>
        /// <para></para>
        /// </summary>
        private readonly GitHubStorage Storage;

        /// <summary>
        /// <para>
        /// The parser.
        /// </para>
        /// <para></para>
        /// </summary>
        private readonly Parser Parser = new();

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="OrganizationLastMonthActivityTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>A storage.</para>
        /// <para></para>
        /// </param>
        public OrganizationLastMonthActivityTrigger(GitHubStorage storage) => Storage = storage;

        /// <summary>
        /// <para>
        /// Determines whether this instance condition.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="issue">
        /// <para>The issue.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The bool</para>
        /// <para></para>
        /// </returns>
        public bool Condition(Issue issue) => issue.Title.Contains("organization last");

        /// <summary>
        /// <para>
        /// Actions the issue.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="issue">
        /// <para>The issue.</para>
        /// <para></para>
        /// </param>
        public void Action(Issue issue)
        {
            var issueService = Storage.Client.Issue;
            var owner = issue.Repository.Owner.Login;
            var activeUsersString = GetActiveUsers(GetIgnoredRepositories(Parser.Parse(issue.Body)), owner, new LastCommitActivityTrigger(this.Storage).GetSince(issue.Title));
            foreach (var mon in activeUsersString)
            {
                Console.WriteLine("\n\n\n" + mon.Last().Dates.First().ToString() + " - " + mon.Last().Dates.Last().ToString());
                foreach (var user in mon)
                {
                    if (user?.Url == null)
                    {
                        continue;
                    }
                    else
                    {
                        Console.WriteLine(user.Url);
                    }
                }
                //issueService.Comment.Create(owner, issue.Repository.Name, issue.Number, "organization last " + new LastCommitActivityTrigger(this.Storage).GetSince(issue.Title) + " month activity" + activeUsersString);
                //Storage.CloseIssue(issue);
            }
        }

        /// <summary>
        /// <para>
        /// Gets the ignored repositories using the specified links.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="links">
        /// <para>The links.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The ignored repos.</para>
        /// <para></para>
        /// </returns>
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
                foreach (var commit in Storage.GetCommits(repository.Owner.Login, repository.Name))
                {
                    if (!activeUsers.Any(x => x.Url == commit.Author.Login))
                    {
                        activeUsers.Add(new Activity() { Url = commit.Author.Login});
                        activeUsers.Last().Dates.Add(commit.Commit.Committer.Date.DateTime);
                    }
                }
                foreach (var pullRequest in Storage.GetPullRequests(repository.Owner.Login, repository.Name))
                {
                    if (pullRequest.Merged)
                    {
                        if (pullRequest.MergedAt.Value.DateTime > date)
                        {
                            activeUsers.Add(new Activity() { Url = pullRequest.MergedBy.Login });
                            activeUsers.Last().Dates.Add(pullRequest.MergedAt.Value.DateTime);
                        }
                    }
                    foreach (var reviewer in pullRequest.RequestedReviewers)
                    {
                        if (pullRequest.CreatedAt < date || pullRequest.UpdatedAt < date || pullRequest.ClosedAt < date || pullRequest.MergedAt < date)
                        {
                            if (!activeUsers.Any(x => x.Url == reviewer.Login))
                            {
                                activeUsers.Add(new Activity() { Url = reviewer.Login });
                                activeUsers.Last().Dates.Add(pullRequest.CreatedAt.DateTime);
                            }
                        }
                    }
                }
                foreach (var createdIssue in Storage.GetIssues(repository.Owner.Login, repository.Name,date))
                {
                    if(createdIssue.CreatedAt > date)
                    {
                        activeUsers.Add(new Activity() { Url = createdIssue.User.Login });
                        activeUsers.Last().Dates.Add(createdIssue.CreatedAt.DateTime);
                    }
                }
            }
            return activeUsers;
        }


        /// <summary>
        /// <para>
        /// Gets the active users using the specified ignored repositories.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="ignoredRepositories">
        /// <para>The ignored repositories.</para>
        /// <para></para>
        /// </param>
        /// <param name="owner">
        /// <para>The owner.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The active users.</para>
        /// <para></para>
        /// </returns>
        /// 
        public List<HashSet<Activity>> GetActiveUsers(HashSet<string> ignoredRepositories, string owner, List<Since> months)
        {
            List<HashSet<Activity>> activeUsers = new() { };
            var allActiveUsers = GetActivitiesInRepos(ignoredRepositories, owner, DateTime.Now.AddMonths(-1 * months.Count));
            foreach (var month in months)
            {
                activeUsers.Add(new HashSet<Activity>());
                var activeUsersInMonth = activeUsers.Last();
                foreach (var activeUser in allActiveUsers)
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
