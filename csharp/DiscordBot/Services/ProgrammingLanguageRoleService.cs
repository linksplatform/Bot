using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Services
{
    public class ProgrammingLanguageRoleService
    {
        private readonly DiscordBotSettings _settings;
        private readonly GitHubLanguageDetectionService _languageDetectionService;
        private readonly ILogger<ProgrammingLanguageRoleService> _logger;

        public ProgrammingLanguageRoleService(
            IOptions<DiscordBotSettings> settings,
            GitHubLanguageDetectionService languageDetectionService,
            ILogger<ProgrammingLanguageRoleService> logger)
        {
            _settings = settings.Value;
            _languageDetectionService = languageDetectionService;
            _logger = logger;
        }

        public async Task SynchronizeUserRolesAsync(DiscordSocketClient client, ulong userId, string githubUsername)
        {
            try
            {
                var guild = client.GetGuild(_settings.GuildId);
                if (guild == null)
                {
                    _logger.LogWarning("Guild with ID {GuildId} not found", _settings.GuildId);
                    return;
                }

                var user = guild.GetUser(userId);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found in guild", userId);
                    return;
                }

                _logger.LogInformation("Synchronizing roles for user {Username} (Discord: {UserId}, GitHub: {GitHubUsername})", 
                    user.Username, userId, githubUsername);

                var languageStats = await _languageDetectionService.GetUserProgrammingLanguagesAsync(githubUsername);
                var topLanguages = GitHubLanguageDetectionService.GetTopLanguages(languageStats);

                var rolesToAdd = new List<IRole>();
                var rolesToRemove = new List<IRole>();

                foreach (var language in _settings.LanguageRoles.Keys)
                {
                    var roleId = _settings.LanguageRoles[language];
                    var role = guild.GetRole(roleId);
                    
                    if (role == null)
                    {
                        _logger.LogWarning("Role with ID {RoleId} for language {Language} not found", roleId, language);
                        continue;
                    }

                    var shouldHaveRole = ShouldUserHaveLanguageRole(language, topLanguages);
                    var currentlyHasRole = user.Roles.Any(r => r.Id == roleId);

                    if (shouldHaveRole && !currentlyHasRole)
                    {
                        rolesToAdd.Add(role);
                    }
                    else if (!shouldHaveRole && currentlyHasRole)
                    {
                        rolesToRemove.Add(role);
                    }
                }

                if (rolesToAdd.Any())
                {
                    await user.AddRolesAsync(rolesToAdd);
                    _logger.LogInformation("Added {Count} roles to user {Username}: {Roles}", 
                        rolesToAdd.Count, user.Username, string.Join(", ", rolesToAdd.Select(r => r.Name)));
                }

                if (rolesToRemove.Any())
                {
                    await user.RemoveRolesAsync(rolesToRemove);
                    _logger.LogInformation("Removed {Count} roles from user {Username}: {Roles}", 
                        rolesToRemove.Count, user.Username, string.Join(", ", rolesToRemove.Select(r => r.Name)));
                }

                if (!rolesToAdd.Any() && !rolesToRemove.Any())
                {
                    _logger.LogInformation("No role changes needed for user {Username}", user.Username);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to synchronize roles for user {UserId} with GitHub username {GitHubUsername}", 
                    userId, githubUsername);
            }
        }

        private static bool ShouldUserHaveLanguageRole(string language, List<string> userTopLanguages)
        {
            return userTopLanguages.Any(userLang => 
                string.Equals(userLang, language, StringComparison.OrdinalIgnoreCase) ||
                IsLanguageVariant(userLang, language));
        }

        private static bool IsLanguageVariant(string userLanguage, string roleLanguage)
        {
            var languageVariants = new Dictionary<string, string[]>
            {
                ["C#"] = new[] { "csharp", "c-sharp" },
                ["C++"] = new[] { "cpp", "c-plus-plus" },
                ["JavaScript"] = new[] { "js", "javascript" },
                ["TypeScript"] = new[] { "ts", "typescript" },
                ["Python"] = new[] { "python", "py" },
                ["Java"] = new[] { "java" },
                ["Go"] = new[] { "golang", "go" },
                ["Rust"] = new[] { "rust", "rs" },
                ["Ruby"] = new[] { "ruby", "rb" },
                ["PHP"] = new[] { "php" },
                ["Swift"] = new[] { "swift" },
                ["Kotlin"] = new[] { "kotlin", "kt" },
                ["Dart"] = new[] { "dart" },
                ["Scala"] = new[] { "scala" },
                ["F#"] = new[] { "fsharp", "f-sharp" },
                ["Clojure"] = new[] { "clojure", "clj" },
                ["Haskell"] = new[] { "haskell", "hs" }
            };

            foreach (var variants in languageVariants.Values)
            {
                if (variants.Contains(userLanguage.ToLowerInvariant()) && 
                    variants.Contains(roleLanguage.ToLowerInvariant()))
                {
                    return true;
                }
            }

            return false;
        }
    }
}