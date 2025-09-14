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
    /// Represents the add user link trigger for handling user link addition commands.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class AddUserLinkTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        private readonly FileStorage _fileStorage;
        private readonly Regex _commandPattern;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="AddUserLinkTrigger"/> instance.
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
        public AddUserLinkTrigger(GitHubStorage storage, FileStorage fileStorage)
        {
            _storage = storage;
            _fileStorage = fileStorage;
            _commandPattern = new Regex(@"^add link (\w+) (.+)$", RegexOptions.IgnoreCase);
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
        /// <para>True if the issue title matches the add link pattern.</para>
        /// <para></para>
        /// </returns>
        public async Task<bool> Condition(TContext context)
        {
            return _commandPattern.IsMatch(context.Title.Trim());
        }

        /// <summary>
        /// <para>
        /// Executes the add user link action.
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
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, "❌ Invalid command format. Use: `add link <platform> <url>`");
                _storage.CloseIssue(context);
                return;
            }

            var platform = match.Groups[1].Value;
            var url = match.Groups[2].Value;
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

            // Validate URL is allowed for the platform
            if (!DomainWhitelist.IsUrlAllowed(platform, url))
            {
                var allowedDomains = string.Join(", ", DomainWhitelist.GetAllowedDomains(platform));
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                    $"❌ URL '{url}' is not allowed for platform '{platform}'. Allowed domains: {allowedDomains}");
                _storage.CloseIssue(context);
                return;
            }

            try
            {
                // Check if user already has a link for this platform
                var existingLinks = _fileStorage.GetUserLinks(username);
                var existingLink = existingLinks.Find(link => string.Equals(link.Platform, platform, StringComparison.OrdinalIgnoreCase));
                
                if (existingLink != null)
                {
                    await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                        $"❌ You already have a {platform} link: {existingLink.Url}. Remove it first if you want to change it.");
                    _storage.CloseIssue(context);
                    return;
                }

                // Add the user link
                _fileStorage.AddUserLink(username, platform, url);
                
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                    $"✅ Successfully added {platform} link for @{username}: {url}");
            }
            catch (Exception ex)
            {
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                    $"❌ Error adding link: {ex.Message}");
            }

            _storage.CloseIssue(context);
        }
    }
}