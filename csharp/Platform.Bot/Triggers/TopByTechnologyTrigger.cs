using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Platform.Communication.Protocol.Lino;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;
    /// <summary>
    /// <para>
    /// Represents the top by technology trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{Issue}"/>
    public class TopByTechnologyTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        private readonly Parser _parser = new();

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="TopByTechnologyTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>A storage.</para>
        /// <para></para>
        /// </param>
        public TopByTechnologyTrigger(GitHubStorage storage) => _storage = storage;

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
        public async Task<bool> Condition(TContext context) => context.Title.ToLower().StartsWith("top by technology");

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
            var issueService = _storage.Client.Issue;
            var owner = context.Repository.Owner.Login;
            var technology = ExtractTechnology(context.Title);
            
            if (string.IsNullOrEmpty(technology))
            {
                await issueService.Comment.Create(owner, context.Repository.Name, context.Number, 
                    "Please specify a technology. Example: 'Top by technology CUDA' or 'Top by technology Qt'");
                return;
            }
            
            var topUsers = await GetTopUsersByTechnology(owner, technology);
            var responseMessage = BuildTopByTechnologyResponse(technology, topUsers);
            
            await issueService.Comment.Create(owner, context.Repository.Name, context.Number, responseMessage);
            _storage.CloseIssue(context);
        }

        /// <summary>
        /// <para>
        /// Extracts technology name from the issue title.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="title">
        /// <para>The issue title.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The technology name or empty string.</para>
        /// <para></para>
        /// </returns>
        private string ExtractTechnology(string title)
        {
            const string prefix = "top by technology";
            var lowerTitle = title.ToLower();
            if (!lowerTitle.StartsWith(prefix))
                return string.Empty;
            
            var technology = title.Substring(prefix.Length).Trim();
            return technology;
        }

        /// <summary>
        /// <para>
        /// Gets the top users by technology based on repository languages and commit activity.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="owner">
        /// <para>The organization owner.</para>
        /// <para></para>
        /// </param>
        /// <param name="technology">
        /// <para>The technology to search for.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A dictionary of user login and their score.</para>
        /// <para></para>
        /// </returns>
        private async Task<Dictionary<string, int>> GetTopUsersByTechnology(string owner, string technology)
        {
            var userScores = new Dictionary<string, int>();
            var repositories = await _storage.GetAllRepositories(owner);
            var threeMonthsAgo = DateTime.Now.AddMonths(-3);

            foreach (var repository in repositories)
            {
                try
                {
                    // Check if repository contains the technology
                    var repositoryContainsTechnology = await RepositoryContainsTechnology(repository, technology);
                    if (!repositoryContainsTechnology)
                        continue;

                    // Get commits from the last 3 months
                    var commits = await _storage.GetCommits(repository.Id, new CommitRequest { Since = threeMonthsAgo });
                    
                    foreach (var commit in commits)
                    {
                        if (commit.Author?.Login != null)
                        {
                            var authorLogin = commit.Author.Login;
                            if (!userScores.ContainsKey(authorLogin))
                                userScores[authorLogin] = 0;
                            
                            // Score based on commit activity in technology-related repositories
                            userScores[authorLogin] += 1;
                        }
                    }

                    // Additional scoring for repository contributors
                    var languages = await _storage.Client.Repository.GetAllLanguages(repository.Id);
                    var technologyWeight = GetTechnologyWeight(languages, technology);
                    
                    if (technologyWeight > 0)
                    {
                        var contributors = await _storage.Client.Repository.GetAllContributors(repository.Id);
                        foreach (var contributor in contributors.Take(10)) // Top 10 contributors
                        {
                            if (!userScores.ContainsKey(contributor.Login))
                                userScores[contributor.Login] = 0;
                            
                            userScores[contributor.Login] += contributor.Contributions * technologyWeight / 100;
                        }
                    }
                }
                catch (Exception)
                {
                    // Skip repositories that cause errors (private repos, API limits, etc.)
                    continue;
                }
            }

            return userScores;
        }

        /// <summary>
        /// <para>
        /// Determines if a repository contains the specified technology.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="repository">
        /// <para>The repository.</para>
        /// <para></para>
        /// </param>
        /// <param name="technology">
        /// <para>The technology to check for.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>True if the repository contains the technology.</para>
        /// <para></para>
        /// </returns>
        private async Task<bool> RepositoryContainsTechnology(Repository repository, string technology)
        {
            try
            {
                var lowerTechnology = technology.ToLower();
                
                // Check repository name and description
                if (repository.Name.ToLower().Contains(lowerTechnology) ||
                    (repository.Description?.ToLower().Contains(lowerTechnology) ?? false))
                {
                    return true;
                }

                // Check topics
                if (repository.Topics?.Any(topic => topic.ToLower().Contains(lowerTechnology)) ?? false)
                {
                    return true;
                }

                // Check languages
                var languages = await _storage.Client.Repository.GetAllLanguages(repository.Id);
                if (languages.Any(lang => lang.Name.ToLower().Contains(lowerTechnology)))
                {
                    return true;
                }

                // Check for common technology file patterns
                var technologyPatterns = GetTechnologyFilePatterns(lowerTechnology);
                if (technologyPatterns.Any())
                {
                    try
                    {
                        var contents = await _storage.Client.Repository.Content.GetAllContents(repository.Id);
                        return contents.Any(content => technologyPatterns.Any(pattern => 
                            content.Name.ToLower().Contains(pattern)));
                    }
                    catch
                    {
                        // If we can't access contents, skip this check
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// <para>
        /// Gets file patterns associated with a technology.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="technology">
        /// <para>The technology name.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A list of file patterns.</para>
        /// <para></para>
        /// </returns>
        private List<string> GetTechnologyFilePatterns(string technology)
        {
            var patterns = new List<string>();
            
            switch (technology)
            {
                case "cuda":
                    patterns.AddRange(new[] { ".cu", ".cuh", "cuda", "nvcc" });
                    break;
                case "qt":
                    patterns.AddRange(new[] { ".pro", ".pri", ".ui", ".qrc", "qmake", "cmake" });
                    break;
                case "react":
                    patterns.AddRange(new[] { "react", ".jsx", ".tsx", "package.json" });
                    break;
                case "docker":
                    patterns.AddRange(new[] { "dockerfile", "docker-compose", ".dockerignore" });
                    break;
                case "kubernetes":
                case "k8s":
                    patterns.AddRange(new[] { ".yaml", ".yml", "helm", "kubectl" });
                    break;
                case "tensorflow":
                    patterns.AddRange(new[] { "tensorflow", ".pb", ".h5", "keras" });
                    break;
                case "pytorch":
                    patterns.AddRange(new[] { "pytorch", "torch", ".pth", ".pt" });
                    break;
                default:
                    patterns.Add(technology);
                    break;
            }
            
            return patterns;
        }

        /// <summary>
        /// <para>
        /// Gets the weight of a technology based on repository languages.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="languages">
        /// <para>The repository languages.</para>
        /// <para></para>
        /// </param>
        /// <param name="technology">
        /// <para>The technology name.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The technology weight.</para>
        /// <para></para>
        /// </returns>
        private int GetTechnologyWeight(IReadOnlyList<RepositoryLanguage> languages, string technology)
        {
            var lowerTechnology = technology.ToLower();
            var totalBytes = languages.Sum(l => l.NumberOfBytes);
            
            if (totalBytes == 0)
                return 0;
            
            foreach (var language in languages)
            {
                if (language.Name.ToLower().Contains(lowerTechnology))
                {
                    return (int)((language.NumberOfBytes * 100) / totalBytes);
                }
            }
            
            return 0;
        }

        /// <summary>
        /// <para>
        /// Builds the response message for top users by technology.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="technology">
        /// <para>The technology name.</para>
        /// <para></para>
        /// </param>
        /// <param name="userScores">
        /// <para>The user scores dictionary.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The formatted response message.</para>
        /// <para></para>
        /// </returns>
        private string BuildTopByTechnologyResponse(string technology, Dictionary<string, int> userScores)
        {
            if (!userScores.Any())
            {
                return $"No users found with activity in {technology} repositories in the last 3 months.";
            }

            var sortedUsers = userScores.OrderByDescending(kv => kv.Value).Take(10);
            var response = $"# Top Contributors for {technology}\n\n";
            response += "Based on commits and contributions in repositories containing this technology (last 3 months):\n\n";
            
            var rank = 1;
            foreach (var user in sortedUsers)
            {
                response += $"{rank}. @{user.Key} - {user.Value} points\n";
                rank++;
            }
            
            response += "\n*Points are calculated based on commits in technology-related repositories and overall contribution activity.*";
            
            return response;
        }
    }
}