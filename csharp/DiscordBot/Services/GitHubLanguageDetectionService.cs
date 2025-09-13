using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Services
{
    public class GitHubLanguageDetectionService
    {
        private readonly GitHubClient _gitHubClient;
        private readonly ILogger<GitHubLanguageDetectionService> _logger;

        public GitHubLanguageDetectionService(IOptions<DiscordBotSettings> settings, ILogger<GitHubLanguageDetectionService> logger)
        {
            _logger = logger;
            _gitHubClient = new GitHubClient(new ProductHeaderValue("LinksPlatform-DiscordBot"))
            {
                Credentials = new Credentials(settings.Value.GitHubToken)
            };
        }

        public async Task<Dictionary<string, int>> GetUserProgrammingLanguagesAsync(string githubUsername)
        {
            try
            {
                _logger.LogInformation("Fetching programming languages for GitHub user: {Username}", githubUsername);

                var repositories = await _gitHubClient.Repository.GetAllForUser(githubUsername);
                var languageStats = new Dictionary<string, int>();

                foreach (var repo in repositories.Where(r => !r.Fork))
                {
                    try
                    {
                        var languages = await _gitHubClient.Repository.GetAllLanguages(repo.Id);
                        
                        foreach (var language in languages)
                        {
                            if (languageStats.ContainsKey(language.Name))
                            {
                                languageStats[language.Name] += (int)language.NumberOfBytes;
                            }
                            else
                            {
                                languageStats[language.Name] = (int)language.NumberOfBytes;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get languages for repository {RepoName}", repo.Name);
                    }
                }

                _logger.LogInformation("Found {Count} programming languages for user {Username}", languageStats.Count, githubUsername);
                return languageStats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch programming languages for user {Username}", githubUsername);
                return new Dictionary<string, int>();
            }
        }

        public static List<string> GetTopLanguages(Dictionary<string, int> languageStats, int minBytes = 1000, int maxLanguages = 10)
        {
            return languageStats
                .Where(kvp => kvp.Value >= minBytes)
                .OrderByDescending(kvp => kvp.Value)
                .Take(maxLanguages)
                .Select(kvp => kvp.Key)
                .ToList();
        }
    }
}