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
    /// Represents the rubber duck debugging dialog continuation trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class RubberDuckDialogTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="RubberDuckDialogTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>A git hub api.</para>
        /// <para></para>
        /// </param>
        public RubberDuckDialogTrigger(GitHubStorage storage)
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
            var debuggingGuide = $@"🦆 **Great! Let's debug together!**

I'm your rubber duck debugging assistant. Please follow these steps:

## Step 1: Describe Your Problem
Please tell me:
- **What are you trying to do?**
- **What is happening instead?**
- **What error messages (if any) are you seeing?**

## Step 2: Show Me The Code
- Share the relevant code snippet
- Include any error messages or unexpected output

## Step 3: Recent Changes
- **What was the last thing you changed before this problem appeared?**
- Have you tried undoing recent changes?

## Step 4: Let's Think Through This Together
I'll help you by asking questions like:
- What assumptions are you making about the code?
- Can you trace through the code line by line?
- Have you checked the most obvious things first?

## Step 5: Problem-Solving Techniques
We can try:
- Adding debug logging/print statements
- Testing with simpler inputs
- Checking documentation
- Looking for similar examples

**🎯 Often, just explaining your problem step-by-step to me will help you spot the issue!**

Go ahead and describe your problem in detail. I'm listening! 👂";

            await _storage.CreateIssueComment(context.Repository.Id, context.Number, debuggingGuide);
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
            return title == "yes" || title == "да";
        }
    }
}