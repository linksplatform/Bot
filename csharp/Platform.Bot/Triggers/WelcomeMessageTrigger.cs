using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Storage.Local;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;
    /// <summary>
    /// <para>
    /// Represents the welcome message trigger for new users.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class WelcomeMessageTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        private readonly FileStorage _fileStorage;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="WelcomeMessageTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>A GitHub storage.</para>
        /// <para></para>
        /// </param>
        /// <param name="fileStorage">
        /// <para>A file storage.</para>
        /// <para></para>
        /// </param>
        public WelcomeMessageTrigger(GitHubStorage storage, FileStorage fileStorage)
        {
            this._storage = storage;
            this._fileStorage = fileStorage;
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
            var welcomeMessage = @"👋 Welcome to the platform!

Thank you for creating your first issue. Here are some things you can do to get started:

🌐 **Set up your languages list**: Let us know which programming languages you're interested in by mentioning them in your issues or profile.

🔗 **Set up your GitHub link**: Make sure your GitHub profile is properly linked and accessible.

❓ **Need help?**: Use the `help` command or create an issue with ""help"" in the title to get assistance with available commands and features.

We're glad to have you here! Feel free to explore and don't hesitate to ask questions.";

            await _storage.CreateIssueComment(context.Repository.Id, context.Number, welcomeMessage);
            
            // Mark user as welcomed
            _fileStorage.MarkUserAsWelcomed(context.User.Login);
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
        public async Task<bool> Condition(TContext context) => _fileStorage.IsNewUser(context.User.Login);
    }
}