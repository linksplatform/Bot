using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;
    /// <summary>
    /// <para>
    /// Represents the HeadHunter bot trigger that asks programmers to join LinksPlatform team.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class HeadHunterTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        private const string HeadHunterQuestion = "Would you like to become a part of LinksPlatform team?";

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="HeadHunterTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>A GitHub storage.</para>
        /// <para></para>
        /// </param>
        public HeadHunterTrigger(GitHubStorage storage)
        {
            this._storage = storage;
        }

        /// <summary>
        /// <para>
        /// Determines whether this instance condition should trigger the HeadHunter bot.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The issue context.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>True if this is a HeadHunter request, false otherwise</para>
        /// <para></para>
        /// </returns>
        public async Task<bool> Condition(TContext context)
        {
            var title = context.Title.ToLower();
            return title.Contains("headhunter") || title.Contains("recruit") || title.Contains("join team");
        }

        /// <summary>
        /// <para>
        /// Actions the HeadHunter bot by posting the recruitment question.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The issue context.</para>
        /// <para></para>
        /// </param>
        public async Task Action(TContext context)
        {
            var comment = $"Hello @{context.User.Login}! 👋\n\n" +
                         $"{HeadHunterQuestion}\n\n" +
                         $"If you're interested in contributing to open source projects focused on data structures, " +
                         $"algorithms, and platform development, we'd love to have you on board!\n\n" +
                         $"Please respond with:\n" +
                         $"- ✅ **Yes** - if you're interested in joining\n" +
                         $"- ❌ **No** - if you're not interested (we'll mark this as resolved)\n\n" +
                         $"*Note: We focus our attention only on positive responses to save everyone's time.*";

            await _storage.CreateIssueComment(context.Repository.Id, context.Number, comment);
        }
    }
}