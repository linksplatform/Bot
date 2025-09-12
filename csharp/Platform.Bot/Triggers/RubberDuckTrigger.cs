using System;
using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;
    /// <summary>
    /// <para>
    /// Represents the rubber duck debugging trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class RubberDuckTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="RubberDuckTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>A git hub api.</para>
        /// <para></para>
        /// </param>
        public RubberDuckTrigger(GitHubStorage storage)
        {
            this._storage = storage;
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
            var openerQuestion = $@"🦆 **Rubber Duck Debugging Assistant**

Do you have problem with code?
У вас проблема с кодом?

**Instructions:**
- Reply with **""Yes""** or **""Да""** to start debugging session
- I'll guide you through the problem-solving process step by step
- Explaining your problem to me (the rubber duck) often helps you find the solution yourself!

**Common debugging steps I can help with:**
1. Understanding the problem clearly
2. Breaking down the problem into smaller parts
3. Checking assumptions
4. Reviewing recent changes
5. Testing hypotheses
6. Finding similar examples

Ready to debug? 🐛➡️✨";

            await _storage.CreateIssueComment(context.Repository.Id, context.Number, openerQuestion);
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
            var title = context.Title.ToLower().Trim();
            var triggerWords = new[]
            {
                "help",
                "help me", 
                "sos",
                "помощь",
                "помогите",
                "помогите мне",
                "спасите"
            };

            return Array.Exists(triggerWords, trigger => title == trigger);
        }
    }
}