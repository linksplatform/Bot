using System;
using System.Linq;
using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;
    
    /// <summary>
    /// <para>
    /// Represents a trigger that deletes messages with "+ " as reply to other messages
    /// after 6 hours have passed since the last "+ " message.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class DeletePlusMessagesTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        private readonly TimeSpan _deletionThreshold = TimeSpan.FromHours(6);

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="DeletePlusMessagesTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>The GitHub storage instance.</para>
        /// <para></para>
        /// </param>
        public DeletePlusMessagesTrigger(GitHubStorage storage)
        {
            _storage = storage;
        }

        /// <summary>
        /// <para>
        /// Determines whether this trigger should process the given issue.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The issue context.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>True if the issue should be processed, false otherwise.</para>
        /// <para></para>
        /// </returns>
        public async Task<bool> Condition(TContext context)
        {
            try
            {
                var comments = await _storage.GetIssueComments(context.Repository.Id, context.Number);
                var plusComments = comments.Where(c => c.Body.Trim() == "+ ").ToList();
                
                if (!plusComments.Any())
                {
                    return false;
                }

                var lastPlusComment = plusComments.OrderByDescending(c => c.CreatedAt).First();
                var timeSinceLastPlus = DateTimeOffset.UtcNow - lastPlusComment.CreatedAt;
                
                return timeSinceLastPlus >= _deletionThreshold;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking condition for issue {context.Number}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// <para>
        /// Deletes all "+ " messages from the issue if 6 hours have passed since the last one.
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
                var plusComments = comments.Where(c => c.Body.Trim() == "+ ").ToList();

                foreach (var comment in plusComments)
                {
                    await _storage.DeleteIssueComment(context.Repository.Id, comment.Id);
                    Console.WriteLine($"Deleted '+ ' comment {comment.Id} from issue {context.Number} in repository {context.Repository.FullName}");
                }

                if (plusComments.Any())
                {
                    Console.WriteLine($"Deleted {plusComments.Count} '+ ' comment(s) from issue {context.Number} in repository {context.Repository.FullName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting '+ ' comments from issue {context.Number}: {ex.Message}");
            }
        }
    }
}