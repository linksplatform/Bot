using Storage.Local;
using Storage.Remote.GitHub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace Platform.Bot.Services
{
    /// <summary>
    /// <para>
    /// Service for managing rewards.
    /// </para>
    /// <para></para>
    /// </summary>
    public class RewardsService
    {
        private readonly FileStorage _fileStorage;
        private readonly GitHubStorage _gitHubStorage;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="RewardsService"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="fileStorage">The file storage.</param>
        /// <param name="gitHubStorage">The GitHub storage.</param>
        public RewardsService(FileStorage fileStorage, GitHubStorage gitHubStorage)
        {
            _fileStorage = fileStorage;
            _gitHubStorage = gitHubStorage;
        }

        /// <summary>
        /// <para>
        /// Adds a reward for an issue.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="issueUrl">The issue URL.</param>
        /// <param name="description">The reward description.</param>
        /// <param name="addedBy">The user who added the reward.</param>
        /// <returns>True if reward was added successfully.</returns>
        public bool AddReward(string issueUrl, string description, string addedBy)
        {
            try
            {
                // Check if reward already exists for this issue
                var existingRewards = _fileStorage.GetActiveRewards();
                if (existingRewards.Any(r => r.IssueUrl.Equals(issueUrl, StringComparison.OrdinalIgnoreCase)))
                {
                    return false; // Reward already exists
                }

                var reward = new Reward
                {
                    IssueUrl = issueUrl,
                    Description = description,
                    AddedBy = addedBy,
                    AddedDate = DateTime.UtcNow,
                    IsActive = true
                };

                _fileStorage.AddReward(reward);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// <para>
        /// Gets all active rewards.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <returns>List of active rewards.</returns>
        public List<Reward> GetActiveRewards()
        {
            return _fileStorage.GetActiveRewards();
        }

        /// <summary>
        /// <para>
        /// Removes a reward (marks as inactive).
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="issueUrl">The issue URL to remove reward for.</param>
        /// <returns>True if reward was removed successfully.</returns>
        public bool RemoveReward(string issueUrl)
        {
            try
            {
                var activeRewards = _fileStorage.GetActiveRewards();
                var reward = activeRewards.FirstOrDefault(r => r.IssueUrl.Equals(issueUrl, StringComparison.OrdinalIgnoreCase));
                
                if (reward != null)
                {
                    _fileStorage.UpdateRewardStatus(issueUrl, false);
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// <para>
        /// Checks if an issue URL is valid and accessible.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="issueUrl">The issue URL to validate.</param>
        /// <returns>True if the issue URL is valid.</returns>
        public async Task<bool> IsValidIssueUrl(string issueUrl)
        {
            try
            {
                // Basic URL format check for GitHub issues
                if (!issueUrl.Contains("github.com") || !issueUrl.Contains("/issues/"))
                {
                    return false;
                }

                // Extract owner, repo, and issue number from URL
                var parts = issueUrl.Replace("https://github.com/", "").Split('/');
                if (parts.Length < 4)
                {
                    return false;
                }

                var owner = parts[0];
                var repo = parts[1];
                
                if (!int.TryParse(parts[3], out var issueNumber))
                {
                    return false;
                }

                // Try to fetch the issue to validate it exists and is accessible
                var issue = await _gitHubStorage.Client.Issue.Get(owner, repo, issueNumber);
                return issue != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// <para>
        /// Checks if an issue is closed and automatically removes its reward.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="issueUrl">The issue URL to check.</param>
        /// <returns>True if the issue was closed and reward removed.</returns>
        public async Task<bool> CheckAndRemoveClosedIssueReward(string issueUrl)
        {
            try
            {
                var parts = issueUrl.Replace("https://github.com/", "").Split('/');
                if (parts.Length < 4)
                {
                    return false;
                }

                var owner = parts[0];
                var repo = parts[1];
                
                if (!int.TryParse(parts[3], out var issueNumber))
                {
                    return false;
                }

                var issue = await _gitHubStorage.Client.Issue.Get(owner, repo, issueNumber);
                
                if (issue.State == ItemState.Closed)
                {
                    return RemoveReward(issueUrl);
                }
                
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// <para>
        /// Formats the rewards list for display.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <returns>Formatted string of rewards.</returns>
        public string FormatRewardsForDisplay()
        {
            var rewards = GetActiveRewards();
            
            if (!rewards.Any())
            {
                return "No active rewards available.";
            }

            var result = "🎁 **Active Rewards:**\n\n";
            for (int i = 0; i < rewards.Count; i++)
            {
                var reward = rewards[i];
                result += $"{i + 1}. **{reward.Description}**\n";
                result += $"   📌 Issue: {reward.IssueUrl}\n";
                result += $"   👤 Added by: {reward.AddedBy}\n";
                result += $"   📅 Added: {reward.AddedDate:yyyy-MM-dd}\n\n";
            }

            return result;
        }
    }
}