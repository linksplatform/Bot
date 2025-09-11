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
    /// Represents the HeadHunter response trigger that processes answers to recruitment questions.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class HeadHunterResponseTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        private const string HeadHunterQuestion = "Would you like to become a part of LinksPlatform team?";

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="HeadHunterResponseTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>A GitHub storage.</para>
        /// <para></para>
        /// </param>
        public HeadHunterResponseTrigger(GitHubStorage storage)
        {
            this._storage = storage;
        }

        /// <summary>
        /// <para>
        /// Determines whether this instance condition should process a HeadHunter response.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The issue context.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>True if this is a response to a HeadHunter question, false otherwise</para>
        /// <para></para>
        /// </returns>
        public async Task<bool> Condition(TContext context)
        {
            // Check if any comments contain our HeadHunter question
            var comments = await _storage.Client.Issue.Comment.GetAllForIssue(context.Repository.Id, context.Number);
            var hasHeadHunterQuestion = comments.Any(c => c.Body.Contains(HeadHunterQuestion));
            
            if (!hasHeadHunterQuestion)
                return false;

            // Check if there are responses from users (not bot)
            var lastComment = comments.LastOrDefault();
            if (lastComment == null || lastComment.User.Type == AccountType.Bot)
                return false;

            var body = lastComment.Body.ToLower();
            return body.Contains("yes") || body.Contains("no") || body.Contains("✅") || body.Contains("❌");
        }

        /// <summary>
        /// <para>
        /// Actions the HeadHunter response by processing yes/no answers.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The issue context.</para>
        /// <para></para>
        /// </param>
        public async Task Action(TContext context)
        {
            var comments = await _storage.Client.Issue.Comment.GetAllForIssue(context.Repository.Id, context.Number);
            var lastComment = comments.LastOrDefault();
            
            if (lastComment == null) return;

            var body = lastComment.Body.ToLower();
            var isPositiveResponse = body.Contains("yes") || body.Contains("✅");
            
            if (isPositiveResponse)
            {
                // Focus on positive responses - provide next steps
                var followUpComment = $"Great to hear you're interested, @{lastComment.User.Login}! 🎉\n\n" +
                                    $"Welcome to the LinksPlatform community! Here are your next steps:\n\n" +
                                    $"1. 📧 **Contact Information**: Please provide your contact details (email/Discord/Telegram)\n" +
                                    $"2. 💻 **Skills**: Tell us about your programming experience and preferred languages\n" +
                                    $"3. 🎯 **Interests**: Which areas of platform development interest you most?\n" +
                                    $"4. 🔗 **Portfolio**: Share your GitHub profile or any relevant projects\n\n" +
                                    $"A team member will reach out to you soon with more information about contributing " +
                                    $"to our projects and potentially joining the organization.\n\n" +
                                    $"*Thank you for your interest in LinksPlatform! 🚀*";
                
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, followUpComment);
            }
            else if (body.Contains("no") || body.Contains("❌"))
            {
                // Close the issue for negative responses as per requirement
                var closingComment = $"Thank you for your response, @{lastComment.User.Login}. " +
                                   $"We understand and respect your decision. " +
                                   $"Feel free to reach out in the future if you change your mind! 👋";
                
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, closingComment);
                _storage.CloseIssue(context);
            }
        }
    }
}