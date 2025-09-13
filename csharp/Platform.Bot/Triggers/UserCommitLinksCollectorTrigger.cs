using Interfaces;
using Octokit;
using Storage.Remote.GitHub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;
    
    /// <summary>
    /// <para>
    /// Represents a trigger that collects all links to all user's commits in chronological order.
    /// </para>
    /// <para></para>
    /// </summary>
    internal class UserCommitLinksCollectorTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _githubStorage;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="UserCommitLinksCollectorTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>The GitHub storage instance.</para>
        /// <para></para>
        /// </param>
        public UserCommitLinksCollectorTrigger(GitHubStorage storage) => _githubStorage = storage;

        /// <summary>
        /// <para>
        /// Determines whether this trigger should be activated for the given issue.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="issue">
        /// <para>The issue context.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>True if the issue title matches the expected pattern.</para>
        /// <para></para>
        /// </returns>
        public Task<bool> Condition(TContext issue)
        {
            var title = issue.Title.ToLower().Trim();
            return Task.FromResult(title.StartsWith("collect commits for user") || 
                   title.StartsWith("user commits") ||
                   title == "collect user commits" ||
                   title == "collect all user commits");
        }

        /// <summary>
        /// <para>
        /// Collects all commit links for the specified user in chronological order.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="issue">
        /// <para>The issue context.</para>
        /// <para></para>
        /// </param>
        public async Task Action(TContext issue)
        {
            try
            {
                var organizationName = issue.Repository.Owner.Login;
                var userName = ExtractUserNameFromIssue(issue);
                
                if (string.IsNullOrEmpty(userName))
                {
                    await _githubStorage.CreateIssueComment(issue.Repository.Id, issue.Number, 
                        "❌ Could not extract username from issue. Please specify the username in the issue title or body.\n\n" +
                        "Example: `Collect commits for user konard` or add `@username` in the issue body.");
                    return;
                }

                await _githubStorage.CreateIssueComment(issue.Repository.Id, issue.Number, 
                    $"🔍 Starting to collect commit links for user `{userName}` in chronological order...");

                var userCommitLinks = await CollectUserCommitLinksChronologically(organizationName, userName);
                
                if (!userCommitLinks.Any())
                {
                    await _githubStorage.CreateIssueComment(issue.Repository.Id, issue.Number, 
                        $"ℹ️ No commits found for user `{userName}` in organization `{organizationName}`.");
                }
                else
                {
                    var message = BuildCommitLinksMessage(userName, userCommitLinks);
                    await _githubStorage.CreateIssueComment(issue.Repository.Id, issue.Number, message);
                }

                Console.WriteLine($"Issue {issue.Title} is processed: {issue.HtmlUrl}");
                await _githubStorage.Client.Issue.Update(issue.Repository.Owner.Login, issue.Repository.Name, issue.Number, 
                    new IssueUpdate() { State = ItemState.Closed });
            }
            catch (Exception ex)
            {
                await _githubStorage.CreateIssueComment(issue.Repository.Id, issue.Number, 
                    $"❌ Error occurred while collecting commits: {ex.Message}");
                Console.WriteLine($"Error processing issue {issue.Title}: {ex.Message}");
            }
        }

        /// <summary>
        /// <para>
        /// Extracts the username from the issue title or body.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="issue">
        /// <para>The issue to extract username from.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The extracted username or null if not found.</para>
        /// <para></para>
        /// </returns>
        private string ExtractUserNameFromIssue(Issue issue)
        {
            var title = issue.Title.ToLower();
            var body = issue.Body?.ToLower() ?? string.Empty;
            
            // Try to extract from title patterns like "collect commits for user konard"
            var titleWords = title.Split(' ');
            for (int i = 0; i < titleWords.Length - 1; i++)
            {
                if (titleWords[i] == "user" && i + 1 < titleWords.Length)
                {
                    return titleWords[i + 1];
                }
            }
            
            // Try to extract @username from body
            if (body.Contains("@"))
            {
                var bodyWords = body.Split(' ', '\n', '\r');
                foreach (var word in bodyWords)
                {
                    if (word.StartsWith("@") && word.Length > 1)
                    {
                        return word.Substring(1);
                    }
                }
            }
            
            return null;
        }

        /// <summary>
        /// <para>
        /// Collects all commit links for a specific user across all organization repositories in chronological order.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="organizationName">
        /// <para>The organization name.</para>
        /// <para></para>
        /// </param>
        /// <param name="userName">
        /// <para>The username to collect commits for.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>List of commit information sorted chronologically (oldest first).</para>
        /// <para></para>
        /// </returns>
        private async Task<List<CommitInfo>> CollectUserCommitLinksChronologically(string organizationName, string userName)
        {
            var allCommitInfos = new List<CommitInfo>();
            var allRepositories = await _githubStorage.GetAllRepositories(organizationName);
            
            if (!allRepositories.Any())
            {
                return allCommitInfos;
            }

            foreach (var repository in allRepositories)
            {
                try
                {
                    // Check if repository has any branches
                    var branches = await _githubStorage.Client.Repository.Branch.GetAll(repository.Id);
                    if (!branches.Any())
                    {
                        continue;
                    }

                    // Get commits from all time for this repository
                    var commits = await _githubStorage.GetCommits(repository.Id, new CommitRequest 
                    { 
                        Author = userName
                    });

                    foreach (var commit in commits)
                    {
                        if (commit.Author?.Login?.ToLower() == userName.ToLower())
                        {
                            allCommitInfos.Add(new CommitInfo
                            {
                                Commit = commit,
                                Repository = repository,
                                CommittedDate = commit.Commit.Author.Date
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not get commits from repository {repository.Name}: {ex.Message}");
                    // Continue with other repositories
                }
            }

            // Sort chronologically (oldest first)
            return allCommitInfos.OrderBy(c => c.CommittedDate).ToList();
        }

        /// <summary>
        /// <para>
        /// Builds a formatted message with all commit links.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="userName">
        /// <para>The username.</para>
        /// <para></para>
        /// </param>
        /// <param name="commitInfos">
        /// <para>List of commit information.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>Formatted markdown message.</para>
        /// <para></para>
        /// </returns>
        private string BuildCommitLinksMessage(string userName, List<CommitInfo> commitInfos)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"# 📝 All Commit Links for User: `{userName}` (Chronological Order)");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"**Total commits found:** {commitInfos.Count}");
            stringBuilder.AppendLine();

            var currentRepository = string.Empty;
            foreach (var commitInfo in commitInfos)
            {
                // Add repository header when we switch to a new repository
                if (currentRepository != commitInfo.Repository.Name)
                {
                    currentRepository = commitInfo.Repository.Name;
                    stringBuilder.AppendLine($"## 📁 Repository: [{commitInfo.Repository.Name}]({commitInfo.Repository.HtmlUrl})");
                    stringBuilder.AppendLine();
                }

                // Format commit message (remove newlines and limit length)
                var commitMessage = commitInfo.Commit.Commit.Message.Replace('\n', ' ').Replace('\r', ' ');
                if (commitMessage.Length > 100)
                {
                    commitMessage = commitMessage.Substring(0, 97) + "...";
                }

                // Add commit link with date
                var commitDate = commitInfo.CommittedDate.ToString("yyyy-MM-dd HH:mm:ss");
                stringBuilder.AppendLine($"- **{commitDate}** - [{commitMessage}]({commitInfo.Commit.HtmlUrl})");
            }

            stringBuilder.AppendLine();
            stringBuilder.AppendLine("---");
            stringBuilder.AppendLine("✅ **Collection completed successfully!**");
            
            return stringBuilder.ToString();
        }
    }

    /// <summary>
    /// <para>
    /// Helper class to store commit information with repository context.
    /// </para>
    /// <para></para>
    /// </summary>
    internal class CommitInfo
    {
        public GitHubCommit Commit { get; set; } = null!;
        public Repository Repository { get; set; } = null!;
        public DateTimeOffset CommittedDate { get; set; }
    }
}