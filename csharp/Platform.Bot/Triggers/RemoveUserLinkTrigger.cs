using System;
using System.Linq;
using System.Text.RegularExpressions;
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
    /// Represents the remove user link trigger for handling user link removal commands.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class RemoveUserLinkTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        private readonly FileStorage _fileStorage;
        private readonly Regex _commandPattern;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="RemoveUserLinkTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>The GitHub storage.</para>
        /// <para></para>
        /// </param>
        /// <param name="fileStorage">
        /// <para>The file storage.</para>
        /// <para></para>
        /// </param>
        public RemoveUserLinkTrigger(GitHubStorage storage, FileStorage fileStorage)
        {
            _storage = storage;
            _fileStorage = fileStorage;
            _commandPattern = new Regex(@"^remove link (\w+)$", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// <para>
        /// Determines whether this instance condition matches the context.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The issue context.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>True if the issue title matches the remove link pattern.</para>
        /// <para></para>
        /// </returns>
        public async Task<bool> Condition(TContext context)
        {
            return _commandPattern.IsMatch(context.Title.Trim());
        }

        /// <summary>
        /// <para>
        /// Executes the remove user link action.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The issue context.</para>
        /// <para></para>
        /// </param>
        public async Task Action(TContext context)
        {
            var match = _commandPattern.Match(context.Title.Trim());
            if (!match.Success)
            {
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, "❌ Invalid command format. Use: `remove link <platform>`");
                _storage.CloseIssue(context);
                return;
            }

            var platform = match.Groups[1].Value;
            var username = context.User.Login;

            // Validate platform is supported
            if (!DomainWhitelist.GetSupportedPlatforms().Any(p => string.Equals(p, platform, StringComparison.OrdinalIgnoreCase)))
            {
                var supportedPlatforms = string.Join(", ", DomainWhitelist.GetSupportedPlatforms());
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                    $"❌ Platform '{platform}' is not supported. Supported platforms: {supportedPlatforms}");
                _storage.CloseIssue(context);
                return;
            }

            try
            {
                // Check if user has a link for this platform
                var existingLinks = _fileStorage.GetUserLinks(username);
                var existingLink = existingLinks.Find(link => string.Equals(link.Platform, platform, StringComparison.OrdinalIgnoreCase));
                
                if (existingLink == null)
                {
                    await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                        $"❌ You don't have a {platform} link to remove.");
                    _storage.CloseIssue(context);
                    return;
                }

                // Remove the user link
                var removed = _fileStorage.RemoveUserLink(username, platform);
                
                if (removed)
                {
                    await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                        $"✅ Successfully removed {platform} link for @{username} ({existingLink.Url})");
                }
                else
                {
                    await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                        $"❌ Failed to remove {platform} link. Please try again.");
                }
            }
            catch (Exception ex)
            {
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                    $"❌ Error removing link: {ex.Message}");
            }

            _storage.CloseIssue(context);
        }
    }
}