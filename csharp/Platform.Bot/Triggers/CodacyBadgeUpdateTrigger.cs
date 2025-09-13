using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Storage.Remote.GitHub;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;
    /// <summary>
    /// <para>
    /// Represents the Codacy badge update trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class CodacyBadgeUpdateTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="CodacyBadgeUpdateTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>A git hub storage.</para>
        /// <para></para>
        /// </param>
        public CodacyBadgeUpdateTrigger(GitHubStorage storage)
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
            var repository = context.Repository;
            var readmeContent = await GetReadmeContent(repository);
            
            if (readmeContent == null) return;

            var updatedContent = UpdateCodacyBadges(readmeContent, repository.Owner.Login, repository.Name);
            
            if (updatedContent != readmeContent)
            {
                await _storage.CreateOrUpdateFile(updatedContent, repository, repository.DefaultBranch, "README.md", "Codacy badge is updated.");
            }
            
            _storage.CloseIssue(context);
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
        public async Task<bool> Condition(TContext context) => 
            context.Title.ToLower().Contains("codacy") && 
            context.Title.ToLower().Contains("badge") &&
            context.Title.ToLower().Contains("update");

        private async Task<string?> GetReadmeContent(Repository repository)
        {
            try
            {
                var readmeFiles = await _storage.Client.Repository.Content.GetAllContents(repository.Id, "README.md");
                return readmeFiles.FirstOrDefault()?.Content;
            }
            catch
            {
                return null;
            }
        }

        private string UpdateCodacyBadges(string content, string owner, string repositoryName)
        {
            // Pattern to match Codacy badges - matches both old and new formats
            var codacyBadgePattern = @"\[\!\[Codacy Badge\]\(https://api\.codacy\.com/project/badge/Grade/[a-f0-9]+\)\]\(https://[^)]+\)";
            
            // Find all Codacy badges
            var matches = Regex.Matches(content, codacyBadgePattern);
            
            if (matches.Count == 0) return content;

            // Extract the project ID from the first badge found
            var firstMatch = matches[0].Value;
            var projectIdMatch = Regex.Match(firstMatch, @"Grade/([a-f0-9]+)");
            string projectId = projectIdMatch.Success ? projectIdMatch.Groups[1].Value : GenerateProjectId(owner, repositoryName);
            
            // Remove all existing Codacy badges
            var cleanedContent = Regex.Replace(content, codacyBadgePattern, "");
            
            // Remove extra empty lines that might have been left behind
            cleanedContent = Regex.Replace(cleanedContent, @"\n\s*\n\s*\n", "\n\n");
            
            // Create the new standardized Codacy badge with modern URL format
            var newBadge = $"[![Codacy Badge](https://api.codacy.com/project/badge/Grade/{projectId})](https://app.codacy.com/gh/{owner}/{repositoryName}?utm_source=github.com&utm_medium=referral&utm_content={owner}/{repositoryName}&utm_campaign=Badge_Grade_Settings)";
            
            // Insert the badge at the beginning of the file (before any other content)
            return newBadge + "\n" + cleanedContent.TrimStart('\n');
        }

        private string GenerateProjectId(string owner, string repositoryName)
        {
            // Generate a consistent 32-character hex string based on repository info
            var input = $"{owner.ToLower()}/{repositoryName.ToLower()}";
            var hash = System.Security.Cryptography.MD5.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}