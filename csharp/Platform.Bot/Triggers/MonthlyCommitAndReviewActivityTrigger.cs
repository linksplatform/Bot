using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using Interfaces;
using Octokit;
using Platform.Communication.Protocol.Lino;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;
    /// <summary>
    /// <para>
    /// Represents the monthly commit and review activity trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{Issue}"/>
    internal class MonthlyCommitAndReviewActivityTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        private readonly Parser _parser = new();

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="MonthlyCommitAndReviewActivityTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>A storage.</para>
        /// <para></para>
        /// </param>
        public MonthlyCommitAndReviewActivityTrigger(GitHubStorage storage) => _storage = storage;

        /// <summary>
        /// <para>
        /// Determines whether this instance condition.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The context.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The bool</para>
        /// <para></para>
        /// </returns>
        public async Task<bool> Condition(TContext context) => context.Title.ToLower().Contains("monthly commit and review activity") || context.Title.ToLower().Contains("collect users who made commits");

        /// <summary>
        /// <para>
        /// Actions the context.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The context.</para>
        /// <para></para>
        /// </param>
        public async Task Action(TContext context)
        {
            var issueService = _storage.Client.Issue;
            var owner = context.Repository.Owner.Login;

            try
            {
                var (year, month) = ParseDateFromIssueBody(context.Body);
                var ignoredRepositories = GetIgnoredRepositories(_parser.Parse(context.Body));
                var activeUsers = await GetActiveUsersInMonth(ignoredRepositories, owner, year, month);
                
                var resultMessage = FormatResult(activeUsers, year, month);
                await issueService.Comment.Create(owner, context.Repository.Name, context.Number, resultMessage);
                _storage.CloseIssue(context);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error processing monthly activity request: {ex.Message}\n\nPlease ensure the issue body contains the month and year in format:\n- `month: 11` (for November)\n- `year: 2023` (for 2023)\n\nExample:\n```\nmonth: 11\nyear: 2023\n```";
                await issueService.Comment.Create(owner, context.Repository.Name, context.Number, errorMessage);
            }
        }

        /// <summary>
        /// <para>
        /// Parses the date from issue body.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="issueBody">
        /// <para>The issue body.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A tuple containing year and month.</para>
        /// <para></para>
        /// </returns>
        private (int year, int month) ParseDateFromIssueBody(string issueBody)
        {
            var lines = issueBody?.Split('\n', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            
            int? year = null;
            int? month = null;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (trimmedLine.StartsWith("year:", StringComparison.OrdinalIgnoreCase))
                {
                    var yearStr = trimmedLine.Substring(5).Trim();
                    if (int.TryParse(yearStr, out var parsedYear))
                    {
                        year = parsedYear;
                    }
                }
                else if (trimmedLine.StartsWith("month:", StringComparison.OrdinalIgnoreCase))
                {
                    var monthStr = trimmedLine.Substring(6).Trim();
                    if (int.TryParse(monthStr, out var parsedMonth) && parsedMonth >= 1 && parsedMonth <= 12)
                    {
                        month = parsedMonth;
                    }
                }
            }

            if (!year.HasValue || !month.HasValue)
            {
                // Default to previous month if not specified
                var lastMonth = DateTime.Now.AddMonths(-1);
                year ??= lastMonth.Year;
                month ??= lastMonth.Month;
            }

            return (year.Value, month.Value);
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

        /// <summary>
        /// <para>
        /// Gets the active users in the specified month.
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
        /// <param name="year">
        /// <para>The year.</para>
        /// <para></para>
        /// </param>
        /// <param name="month">
        /// <para>The month.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A dictionary with user activities.</para>
        /// <para></para>
        /// </returns>
        public async Task<Dictionary<string, List<string>>> GetActiveUsersInMonth(HashSet<string> ignoredRepositories, string owner, int year, int month)
        {
            var usersActivity = new Dictionary<string, List<string>>();
            
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var repositories = await _storage.GetAllRepositories(owner);

            foreach (var repository in repositories)
            {
                if (ignoredRepositories.Contains(repository.Name))
                {
                    continue;
                }

                // Get commits for the specified month
                var commits = await _storage.GetCommits(repository.Id, new CommitRequest 
                { 
                    Since = startDate,
                    Until = endDate
                });

                foreach (var commit in commits)
                {
                    var authorLogin = commit.Author?.Login;
                    if (!string.IsNullOrEmpty(authorLogin))
                    {
                        if (!usersActivity.ContainsKey(authorLogin))
                        {
                            usersActivity[authorLogin] = new List<string>();
                        }
                        
                        var activity = $"Commit in {repository.Name}: {commit.Commit.Message.Split('\n').FirstOrDefault()}";
                        if (!usersActivity[authorLogin].Contains(activity))
                        {
                            usersActivity[authorLogin].Add(activity);
                        }
                    }
                }

                // Get pull requests created/updated in the specified month
                var pullRequests = await _storage.GetPullRequests(repository.Id);
                
                foreach (var pr in pullRequests)
                {
                    // Check if PR was created or updated in the target month
                    if ((pr.CreatedAt >= startDate && pr.CreatedAt <= endDate) ||
                        (pr.UpdatedAt >= startDate && pr.UpdatedAt <= endDate))
                    {
                        // Get reviews for this pull request
                        var reviews = await _storage.Client.PullRequest.Review.GetAll(repository.Id, pr.Number);
                        
                        foreach (var review in reviews)
                        {
                            if (review.SubmittedAt >= startDate && review.SubmittedAt <= endDate)
                            {
                                var reviewerLogin = review.User?.Login;
                                if (!string.IsNullOrEmpty(reviewerLogin))
                                {
                                    if (!usersActivity.ContainsKey(reviewerLogin))
                                    {
                                        usersActivity[reviewerLogin] = new List<string>();
                                    }
                                    
                                    var activity = $"Review in {repository.Name}: PR #{pr.Number} - {pr.Title}";
                                    if (!usersActivity[reviewerLogin].Contains(activity))
                                    {
                                        usersActivity[reviewerLogin].Add(activity);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return usersActivity;
        }

        /// <summary>
        /// <para>
        /// Formats the result for display.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="usersActivity">
        /// <para>The users activity.</para>
        /// <para></para>
        /// </param>
        /// <param name="year">
        /// <para>The year.</para>
        /// <para></para>
        /// </param>
        /// <param name="month">
        /// <para>The month.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The formatted result string.</para>
        /// <para></para>
        /// </returns>
        private string FormatResult(Dictionary<string, List<string>> usersActivity, int year, int month)
        {
            var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
            
            if (!usersActivity.Any())
            {
                return $"No commit or review activity found for {monthName} {year}.";
            }

            var result = $"# Users with commit and/or review activity in {monthName} {year}\n\n";
            
            var sortedUsers = usersActivity.OrderBy(kvp => kvp.Key).ToList();
            
            result += $"**Total active users: {sortedUsers.Count}**\n\n";
            
            foreach (var userActivity in sortedUsers)
            {
                result += $"## @{userActivity.Key}\n";
                result += $"Activities ({userActivity.Value.Count}):\n";
                
                foreach (var activity in userActivity.Value.Take(5)) // Limit to 5 activities per user to avoid too long messages
                {
                    result += $"- {activity}\n";
                }
                
                if (userActivity.Value.Count > 5)
                {
                    result += $"- ... and {userActivity.Value.Count - 5} more activities\n";
                }
                result += "\n";
            }

            return result;
        }
    }
}