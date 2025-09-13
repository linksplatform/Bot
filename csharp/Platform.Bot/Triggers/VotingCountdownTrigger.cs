using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Storage.Local;
using Storage.Remote.GitHub;
using System.Numerics;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;

    /// <summary>
    /// <para>
    /// Represents the voting countdown trigger that prevents users from responding with "-" to another "-" comment within a specified time period.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class VotingCountdownTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        private readonly FileStorage _fileStorage;
        private readonly TimeSpan _countdownPeriod;
        private readonly string _votingTrackingKey = "voting_countdown_tracking";

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="VotingCountdownTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>A GitHub storage instance.</para>
        /// <para></para>
        /// </param>
        /// <param name="fileStorage">
        /// <para>A file storage instance.</para>
        /// <para></para>
        /// </param>
        /// <param name="countdownMinutes">
        /// <para>The countdown period in minutes (default: 5 minutes).</para>
        /// <para></para>
        /// </param>
        public VotingCountdownTrigger(GitHubStorage storage, FileStorage fileStorage, int countdownMinutes = 5)
        {
            _storage = storage;
            _fileStorage = fileStorage;
            _countdownPeriod = TimeSpan.FromMinutes(countdownMinutes);
        }

        /// <summary>
        /// <para>
        /// Determines whether this instance should process the issue for voting countdown enforcement.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The issue context.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>True if the issue has recent "-" comments that need countdown enforcement.</para>
        /// <para></para>
        /// </returns>
        public async Task<bool> Condition(TContext context)
        {
            try
            {
                var comments = await _storage.GetIssueComments(context.Repository.Id, context.Number);
                
                // Only process if there are comments
                if (!comments.Any()) return false;

                // Check if there are any "-" comments in the recent timeframe
                var recentComments = comments.Where(c => c.CreatedAt > DateTimeOffset.UtcNow.Subtract(_countdownPeriod)).ToList();
                var minusComments = recentComments.Where(c => c.Body.Trim() == "-").ToList();

                return minusComments.Any();
            }
            catch (Exception)
            {
                // If we can't retrieve comments, don't trigger
                return false;
            }
        }

        /// <summary>
        /// <para>
        /// Enforces the voting countdown rules by deleting or warning about invalid "-" responses.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The issue context.</para>
        /// <para></para>
        /// </param>
        public async Task Action(TContext context)
        {
            try
            {
                var comments = await _storage.GetIssueComments(context.Repository.Id, context.Number);
                var minusComments = comments.Where(c => c.Body.Trim() == "-")
                                           .OrderBy(c => c.CreatedAt)
                                           .ToList();

                if (minusComments.Count < 2) return; // Need at least 2 minus comments to check

                // Track user voting timestamps
                var userVotingData = GetUserVotingData(context.Repository.Id, context.Number);
                var now = DateTimeOffset.UtcNow;
                bool hasViolations = false;

                for (int i = 1; i < minusComments.Count; i++)
                {
                    var currentComment = minusComments[i];
                    var previousComment = minusComments[i - 1];
                    var timeDifference = currentComment.CreatedAt - previousComment.CreatedAt;

                    // Check if this is a response to the previous "-" comment within the countdown period
                    if (timeDifference < _countdownPeriod)
                    {
                        // Check if the user had already voted with "-" recently
                        var userKey = $"{currentComment.User.Login}_{context.Repository.Id}_{context.Number}";
                        var lastVoteTime = GetLastVoteTime(userVotingData, userKey);

                        if (lastVoteTime.HasValue && (currentComment.CreatedAt - lastVoteTime.Value) < _countdownPeriod)
                        {
                            // This is a violation - user voted with "-" too soon after another "-"
                            await _storage.CreateIssueComment(context.Repository.Id, context.Number,
                                $"@{currentComment.User.Login} Please wait {_countdownPeriod.TotalMinutes} minutes before responding with \"-\" to another \"-\" comment. " +
                                $"This helps maintain civil discussion. Your comment was posted too quickly after a previous \"-\" vote.");
                            
                            hasViolations = true;
                        }

                        // Update the user's voting timestamp
                        SetLastVoteTime(userVotingData, userKey, currentComment.CreatedAt);
                    }
                }

                if (hasViolations)
                {
                    // Save the updated voting tracking data
                    SaveUserVotingData(context.Repository.Id, context.Number, userVotingData);
                }
            }
            catch (Exception)
            {
                // Log error if needed, but don't crash the bot
            }
        }

        private Dictionary<string, DateTimeOffset> GetUserVotingData(long repositoryId, int issueNumber)
        {
            try
            {
                var key = $"{_votingTrackingKey}_{repositoryId}_{issueNumber}";
                var fileSet = _fileStorage.GetFileSet(key);
                
                if (fileSet != 0) // FileSet exists
                {
                    var files = _fileStorage.GetFilesFromSet(key);
                    var dataFile = files.FirstOrDefault();
                    if (dataFile != null)
                    {
                        // Parse the stored data (format: "user_repo_issue:timestamp,user_repo_issue:timestamp")
                        var result = new Dictionary<string, DateTimeOffset>();
                        var lines = dataFile.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        
                        foreach (var line in lines)
                        {
                            var parts = line.Split(':', 2);
                            if (parts.Length == 2 && DateTimeOffset.TryParse(parts[1], out var timestamp))
                            {
                                result[parts[0]] = timestamp;
                            }
                        }
                        return result;
                    }
                }
            }
            catch (Exception)
            {
                // If parsing fails, return empty dictionary
            }

            return new Dictionary<string, DateTimeOffset>();
        }

        private void SaveUserVotingData(long repositoryId, int issueNumber, Dictionary<string, DateTimeOffset> votingData)
        {
            try
            {
                var key = $"{_votingTrackingKey}_{repositoryId}_{issueNumber}";
                
                // Convert dictionary to string format
                var dataLines = votingData.Select(kvp => $"{kvp.Key}:{kvp.Value:O}").ToArray();
                var content = string.Join('\n', dataLines);
                
                // Create or update the file set
                var fileSet = _fileStorage.CreateFileSet(key);
                var file = _fileStorage.AddFile(content);
                _fileStorage.AddFileToSet(fileSet, file, $"{key}_data.txt");
            }
            catch (Exception)
            {
                // If saving fails, continue silently
            }
        }

        private DateTimeOffset? GetLastVoteTime(Dictionary<string, DateTimeOffset> votingData, string userKey)
        {
            return votingData.TryGetValue(userKey, out var timestamp) ? timestamp : null;
        }

        private void SetLastVoteTime(Dictionary<string, DateTimeOffset> votingData, string userKey, DateTimeOffset timestamp)
        {
            votingData[userKey] = timestamp;
        }
    }
}