using System;
using System.Linq;
using System.Text;
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
    /// Represents the list user links trigger for displaying user links.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class ListUserLinksTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        private readonly FileStorage _fileStorage;
        private readonly Regex _listMyLinksPattern;
        private readonly Regex _listUserLinksPattern;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="ListUserLinksTrigger"/> instance.
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
        public ListUserLinksTrigger(GitHubStorage storage, FileStorage fileStorage)
        {
            _storage = storage;
            _fileStorage = fileStorage;
            _listMyLinksPattern = new Regex(@"^(list my links|my links)$", RegexOptions.IgnoreCase);
            _listUserLinksPattern = new Regex(@"^list links (\w+)$", RegexOptions.IgnoreCase);
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
        /// <para>True if the issue title matches any list links pattern.</para>
        /// <para></para>
        /// </returns>
        public async Task<bool> Condition(TContext context)
        {
            var title = context.Title.Trim();
            return _listMyLinksPattern.IsMatch(title) || _listUserLinksPattern.IsMatch(title);
        }

        /// <summary>
        /// <para>
        /// Executes the list user links action.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The issue context.</para>
        /// <para></para>
        /// </param>
        public async Task Action(TContext context)
        {
            var title = context.Title.Trim();
            var targetUsername = context.User.Login; // Default to the requesting user
            var isRequestingOwnLinks = true;

            // Check if requesting links for a specific user
            var userMatch = _listUserLinksPattern.Match(title);
            if (userMatch.Success)
            {
                targetUsername = userMatch.Groups[1].Value;
                isRequestingOwnLinks = string.Equals(targetUsername, context.User.Login, StringComparison.OrdinalIgnoreCase);
            }

            try
            {
                var userLinks = _fileStorage.GetUserLinks(targetUsername);
                
                if (!userLinks.Any())
                {
                    var message = isRequestingOwnLinks 
                        ? "You don't have any links registered." 
                        : $"User @{targetUsername} doesn't have any links registered.";
                    
                    await _storage.CreateIssueComment(context.Repository.Id, context.Number, message);
                }
                else
                {
                    var messageBuilder = new StringBuilder();
                    var displayUsername = isRequestingOwnLinks ? "Your" : $"@{targetUsername}'s";
                    messageBuilder.AppendLine($"## {displayUsername} Links\n");

                    foreach (var link in userLinks.OrderBy(l => l.Platform))
                    {
                        messageBuilder.AppendLine($"**{link.Platform}**: {link.Url}");
                    }

                    messageBuilder.AppendLine($"\n*Total: {userLinks.Count} link{(userLinks.Count == 1 ? "" : "s")}*");

                    if (isRequestingOwnLinks)
                    {
                        messageBuilder.AppendLine("\n---");
                        messageBuilder.AppendLine("**Commands:**");
                        messageBuilder.AppendLine("- `add link <platform> <url>` - Add a new link");
                        messageBuilder.AppendLine("- `remove link <platform>` - Remove a link");
                        messageBuilder.AppendLine("- `my links` - View your links");
                        
                        var supportedPlatforms = string.Join(", ", DomainWhitelist.GetSupportedPlatforms());
                        messageBuilder.AppendLine($"\n**Supported platforms:** {supportedPlatforms}");
                    }

                    await _storage.CreateIssueComment(context.Repository.Id, context.Number, messageBuilder.ToString());
                }
            }
            catch (Exception ex)
            {
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                    $"❌ Error retrieving links: {ex.Message}");
            }

            _storage.CloseIssue(context);
        }
    }
}