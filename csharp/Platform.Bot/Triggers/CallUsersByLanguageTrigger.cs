using System;
using System.Collections.Generic;
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
    /// Represents the call users by language trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class CallUsersByLanguageTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        private readonly Dictionary<string, string> _languageMap;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="CallUsersByLanguageTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>A git hub api.</para>
        /// <para></para>
        /// </param>
        public CallUsersByLanguageTrigger(GitHubStorage storage)
        {
            _storage = storage;
            _languageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"C++", "C++"},
                {"CPP", "C++"},
                {"C#", "C#"},
                {"CSHARP", "C#"},
                {"C", "C"},
                {"Java", "Java"},
                {"JavaScript", "JavaScript"},
                {"JS", "JavaScript"},
                {"Python", "Python"},
                {"PY", "Python"},
                {"Ruby", "Ruby"},
                {"RB", "Ruby"},
                {"Go", "Go"},
                {"Golang", "Go"},
                {"PHP", "PHP"},
                {"Swift", "Swift"},
                {"Kotlin", "Kotlin"},
                {"Rust", "Rust"},
                {"TypeScript", "TypeScript"},
                {"TS", "TypeScript"},
                {"Scala", "Scala"},
                {"R", "R"},
                {"Dart", "Dart"},
                {"Lua", "Lua"},
                {"Perl", "Perl"},
                {"Haskell", "Haskell"},
                {"Clojure", "Clojure"},
                {"F#", "F#"},
                {"FSHARP", "F#"},
                {"Objective-C", "Objective-C"},
                {"Shell", "Shell"},
                {"PowerShell", "PowerShell"},
                {"HTML", "HTML"},
                {"CSS", "CSS"},
                {"Vim", "Vim script"},
                {"Assembly", "Assembly"},
                {"MATLAB", "MATLAB"},
                {"Groovy", "Groovy"},
                {"Elixir", "Elixir"},
                {"Erlang", "Erlang"},
                {"Julia", "Julia"},
                {"Nim", "Nim"},
                {"Crystal", "Crystal"}
            };
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
            var languageText = context.Body.Trim().Substring(1).Trim();
            
            if (!_languageMap.TryGetValue(languageText, out var language))
            {
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                    $"Sorry, I don't recognize the programming language '{languageText}'. " +
                    $"Supported languages include: {string.Join(", ", _languageMap.Keys.Take(10))} and more.");
                return;
            }

            try
            {
                // Search for repositories with the specified language using raw query
                var query = $"language:{language}";
                var repositorySearchRequest = new SearchRepositoriesRequest(query)
                {
                    SortField = RepoSearchSort.Stars,
                    Order = SortDirection.Descending,
                    PerPage = 100
                };

                var repositoryResults = await _storage.Client.Search.SearchRepo(repositorySearchRequest);
                
                if (!repositoryResults.Items.Any())
                {
                    await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                        $"No repositories found with {languageText} as the primary language.");
                    return;
                }

                // Get unique users from these repositories
                var uniqueUsers = repositoryResults.Items
                    .Where(repo => repo.Owner != null)
                    .GroupBy(repo => repo.Owner.Login)
                    .Select(g => g.First().Owner)
                    .Take(10)
                    .ToList();

                if (!uniqueUsers.Any())
                {
                    await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                        $"No users found with {languageText} repositories.");
                    return;
                }

                var userMentions = uniqueUsers
                    .Select(user => $"@{user.Login}")
                    .ToList();

                var message = $"Calling all {languageText} developers! 👋\n\n{string.Join(" ", userMentions)}\n\n" +
                             $"Found {uniqueUsers.Count} top {languageText} developers based on repository stars.";

                await _storage.CreateIssueComment(context.Repository.Id, context.Number, message);
            }
            catch (Exception ex)
            {
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                    $"Sorry, I encountered an error while searching for {languageText} developers: {ex.Message}");
            }
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
            return !string.IsNullOrEmpty(context.Body) && 
                   context.Body.Trim().StartsWith("!") && 
                   context.Body.Trim().Length > 1 &&
                   context.Body.Trim().Split(' ').Length >= 1;
        }
    }
}