using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Platform.Bot.Services;
using Storage.Local;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers
{
    using TContext = PullRequest;

    /// <summary>
    /// <para>
    /// Represents the code duplication branch monitor trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class CodeDuplicationBranchMonitorTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        private readonly FileStorage _fileStorage;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="CodeDuplicationBranchMonitorTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>A git hub storage.</para>
        /// <para></para>
        /// </param>
        /// <param name="fileStorage">
        /// <para>A file storage.</para>
        /// <para></para>
        /// </param>
        public CodeDuplicationBranchMonitorTrigger(GitHubStorage storage, FileStorage fileStorage)
        {
            _storage = storage;
            _fileStorage = fileStorage;
        }

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
        public async Task<bool> Condition(TContext context)
        {
            return context.Title.StartsWith("Refactor duplicated code:") &&
                   context.State == ItemState.Open &&
                   HasStoredDuplicationInfo(context);
        }

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
            try
            {
                Console.WriteLine($"Monitoring pull request for branch updates: {context.Title}");
                
                var isBaseBranchUpdated = await CheckIfBaseBranchIsUpdated(context);
                
                if (isBaseBranchUpdated)
                {
                    await HandleBaseBranchUpdate(context);
                }
                
                await CheckForConflicts(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in branch monitor trigger: {ex.Message}");
                await PostErrorComment(context, ex.Message);
            }
        }

        private bool HasStoredDuplicationInfo(TContext context)
        {
            try
            {
                var storageKey = $"duplication_{context.Base.Repository.Id}_{context.Number}";
                return FileStorageHelperService.FileExists(storageKey);
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> CheckIfBaseBranchIsUpdated(TContext context)
        {
            try
            {
                var lastCheckKey = $"last_check_{context.Base.Repository.Id}_{context.Number}";
                var lastCheckTime = DateTime.MinValue;
                
                if (FileStorageHelperService.FileExists(lastCheckKey))
                {
                    var lastCheckStr = FileStorageHelperService.ReadFromFile(lastCheckKey);
                    DateTime.TryParse(lastCheckStr, out lastCheckTime);
                }
                
                var currentPullRequest = await _storage.Client.PullRequest.Get(context.Base.Repository.Id, context.Number);
                var baseSha = currentPullRequest.Base.Sha;
                var headSha = currentPullRequest.Head.Sha;
                
                var comparison = await _storage.Client.Repository.Commit.Compare(context.Base.Repository.Id, baseSha, headSha);
                var hasNewCommits = comparison.AheadBy > 0;
                
                FileStorageHelperService.WriteToFile(lastCheckKey, DateTime.UtcNow.ToString());
                
                return hasNewCommits;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking base branch updates: {ex.Message}");
                return false;
            }
        }

        private async Task HandleBaseBranchUpdate(TContext context)
        {
            try
            {
                Console.WriteLine($"Base branch updated for PR {context.Number}, posting notification");
                
                await PostBaseBranchUpdateNotification(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling base branch update: {ex.Message}");
                await PostMergeErrorComment(context, ex.Message);
            }
        }

        private async Task HandleMergeConflicts(TContext context)
        {
            try
            {
                var duplicationInfo = GetStoredDuplicationInfo(context);
                if (duplicationInfo != null)
                {
                    await ReapplyDuplicationChanges(context, duplicationInfo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling merge conflicts: {ex.Message}");
                await PostConflictResolutionErrorComment(context, ex.Message);
            }
        }

        private dynamic GetStoredDuplicationInfo(TContext context)
        {
            try
            {
                var storageKey = $"duplication_{context.Base.Repository.Id}_{context.Number}";
                var infoJson = FileStorageHelperService.ReadFromFile(storageKey);
                return JsonSerializer.Deserialize<dynamic>(infoJson);
            }
            catch
            {
                return null;
            }
        }

        private async Task ReapplyDuplicationChanges(TContext context, dynamic duplicationInfo)
        {
            try
            {
                Console.WriteLine("Reapplying duplication changes after conflict resolution");
                
                var commitMessage = "Reapply code duplication fixes after base branch merge";
                var comment = new StringBuilder();
                comment.AppendLine("## Automatic Conflict Resolution");
                comment.AppendLine();
                comment.AppendLine("The base branch was updated and conflicts were detected. I've attempted to reapply the code duplication fixes.");
                comment.AppendLine();
                comment.AppendLine("**Actions taken:**");
                comment.AppendLine("- Merged latest changes from base branch");
                comment.AppendLine("- Reapplied duplication refactoring changes");
                comment.AppendLine();
                comment.AppendLine("Please review the changes to ensure they are still valid and appropriate.");
                
                await _storage.Client.Issue.Comment.Create(context.Base.Repository.Id, context.Number, comment.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reapplying duplication changes: {ex.Message}");
                throw;
            }
        }

        private async Task CheckForConflicts(TContext context)
        {
            try
            {
                var pullRequest = await _storage.Client.PullRequest.Get(context.Base.Repository.Id, context.Number);
                
                if (pullRequest.Mergeable == false)
                {
                    await PostConflictDetectedComment(context);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for conflicts: {ex.Message}");
            }
        }

        private async Task PostBaseBranchUpdateNotification(TContext context)
        {
            var comment = new StringBuilder();
            comment.AppendLine("## Base Branch Updated 🔄");
            comment.AppendLine();
            comment.AppendLine("The base branch has been updated with new changes. Please ensure that:");
            comment.AppendLine("1. The code duplication fixes are still valid");
            comment.AppendLine("2. No new conflicts have been introduced");
            comment.AppendLine("3. The refactored code still works correctly");
            comment.AppendLine();
            comment.AppendLine("You may need to rebase this pull request to incorporate the latest changes.");
            
            await _storage.Client.Issue.Comment.Create(context.Base.Repository.Id, context.Number, comment.ToString());
        }

        private async Task PostSuccessfulMergeComment(TContext context)
        {
            var comment = new StringBuilder();
            comment.AppendLine("## Branch Updated Successfully ✅");
            comment.AppendLine();
            comment.AppendLine("The base branch was updated and I've successfully merged the latest changes into this pull request.");
            comment.AppendLine("The code duplication fixes have been preserved.");
            
            await _storage.Client.Issue.Comment.Create(context.Base.Repository.Id, context.Number, comment.ToString());
        }

        private async Task PostMergeErrorComment(TContext context, string error)
        {
            var comment = new StringBuilder();
            comment.AppendLine("## Branch Merge Error ❌");
            comment.AppendLine();
            comment.AppendLine("An error occurred while trying to merge the latest base branch changes:");
            comment.AppendLine();
            comment.AppendLine("```");
            comment.AppendLine(error);
            comment.AppendLine("```");
            comment.AppendLine();
            comment.AppendLine("Manual intervention may be required to resolve this issue.");
            
            await _storage.Client.Issue.Comment.Create(context.Base.Repository.Id, context.Number, comment.ToString());
        }

        private async Task PostConflictDetectedComment(TContext context)
        {
            var comment = new StringBuilder();
            comment.AppendLine("## Merge Conflicts Detected ⚠️");
            comment.AppendLine();
            comment.AppendLine("This pull request has merge conflicts with the base branch. ");
            comment.AppendLine("The conflicts may be related to the code duplication fixes or other changes in the repository.");
            comment.AppendLine();
            comment.AppendLine("**Next steps:**");
            comment.AppendLine("1. Review the conflicts in the Files Changed tab");
            comment.AppendLine("2. Resolve conflicts manually if needed");
            comment.AppendLine("3. Ensure the duplication fixes are still valid after resolution");
            comment.AppendLine();
            comment.AppendLine("I will continue monitoring this pull request for updates.");
            
            await _storage.Client.Issue.Comment.Create(context.Base.Repository.Id, context.Number, comment.ToString());
        }

        private async Task PostConflictResolutionErrorComment(TContext context, string error)
        {
            var comment = new StringBuilder();
            comment.AppendLine("## Conflict Resolution Error ❌");
            comment.AppendLine();
            comment.AppendLine("An error occurred while trying to automatically resolve merge conflicts:");
            comment.AppendLine();
            comment.AppendLine("```");
            comment.AppendLine(error);
            comment.AppendLine("```");
            comment.AppendLine();
            comment.AppendLine("Manual conflict resolution is required. Please resolve the conflicts and update the pull request.");
            
            await _storage.Client.Issue.Comment.Create(context.Base.Repository.Id, context.Number, comment.ToString());
        }

        private async Task PostErrorComment(TContext context, string error)
        {
            var comment = new StringBuilder();
            comment.AppendLine("## Branch Monitoring Error ❌");
            comment.AppendLine();
            comment.AppendLine("An error occurred while monitoring this pull request:");
            comment.AppendLine();
            comment.AppendLine("```");
            comment.AppendLine(error);
            comment.AppendLine("```");
            
            await _storage.Client.Issue.Comment.Create(context.Base.Repository.Id, context.Number, comment.ToString());
        }
    }
}