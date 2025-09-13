using System;
using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Platform.Threading;
using Storage.Remote.GitHub;
using System.Linq;
using System.Text.RegularExpressions;
using Storage.Local;
using System.Collections.Generic;

namespace Platform.Bot.Triggers
{
    public class DependencyOnlyReleaseTrigger : ITrigger<GitHubCommit>
    {
        private readonly GitHubStorage _githubStorage;
        private readonly HashSet<string> _processedCommits;

        public DependencyOnlyReleaseTrigger(GitHubStorage storage)
        {
            _githubStorage = storage;
            _processedCommits = new HashSet<string>();
        }

        public async Task<bool> Condition(GitHubCommit commit)
        {
            try
            {
                // Skip if we already processed this commit
                if (_processedCommits.Contains(commit.Sha))
                {
                    return false;
                }

                // Skip if this is not a dependabot commit
                if (!IsDependabotCommit(commit))
                {
                    return false;
                }

                // Get the repository
                var repositoryId = commit.Repository?.Id;
                if (repositoryId == null)
                {
                    return false;
                }

                // Check if this commit only changes dependency files
                var commitDetails = _githubStorage.Client.Repository.Commit.Get(repositoryId.Value, commit.Sha).AwaitResult();
                
                // Check if files changed are only dependency-related files
                var changedFiles = commitDetails.Files;
                if (changedFiles == null || !changedFiles.Any())
                {
                    return false;
                }

                // Check if all changed files are dependency files
                var isDependencyOnlyChange = changedFiles.All(file => IsDependencyFile(file.Filename));
                
                if (!isDependencyOnlyChange)
                {
                    return false;
                }

                // Check if a release with this commit already exists to avoid duplicates
                var releases = _githubStorage.Client.Repository.Release.GetAll(repositoryId.Value).AwaitResult();
                var existingRelease = releases.FirstOrDefault(r => r.TagName.Contains(commit.Sha.Substring(0, 7)));
                
                return existingRelease == null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DependencyOnlyReleaseTrigger.Condition: {ex.Message}");
                return false;
            }
        }

        public async Task Action(GitHubCommit commit)
        {
            try
            {
                var repositoryId = commit.Repository?.Id;
                if (repositoryId == null)
                {
                    return;
                }

                var repository = _githubStorage.Client.Repository.Get(repositoryId.Value).AwaitResult();
                
                // Generate a new version tag based on the current date and commit
                var currentDate = DateTime.UtcNow;
                var shortCommitSha = commit.Sha.Substring(0, 7);
                var tagName = $"deps-{currentDate:yyyy.MM.dd}-{shortCommitSha}";
                
                // Create release name and body
                var releaseName = $"Dependency Updates - {currentDate:yyyy-MM-dd}";
                var releaseBody = $"Automatic release for dependency updates.\n\nCommit: {commit.HtmlUrl}\nCommit Message: {commit.Commit.Message}\n\n🤖 Generated with [Claude Code](https://claude.ai/code)";

                // Create the release
                var newRelease = new NewRelease(tagName)
                {
                    Name = releaseName,
                    Body = releaseBody,
                    Draft = false,
                    Prerelease = false,
                    TargetCommitish = commit.Sha
                };

                var createdRelease = await _githubStorage.Client.Repository.Release.Create(repositoryId.Value, newRelease);
                
                Console.WriteLine($"Created automatic release for dependency updates: {createdRelease.HtmlUrl}");
                Console.WriteLine($"Repository: {repository.FullName}");
                Console.WriteLine($"Tag: {tagName}");
                
                // Mark this commit as processed to avoid duplicates
                _processedCommits.Add(commit.Sha);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DependencyOnlyReleaseTrigger.Action: {ex.Message}");
            }
        }

        private bool IsDependabotCommit(GitHubCommit commit)
        {
            // Check if the commit is from dependabot by checking the author or commit message
            if (commit.Author?.Id == GitHubStorage.DependabotId)
            {
                return true;
            }

            if (commit.Committer?.Id == GitHubStorage.DependabotId)
            {
                return true;
            }

            // Check commit message patterns
            var commitMessage = commit.Commit?.Message?.ToLower() ?? "";
            var dependabotPatterns = new[]
            {
                "bump ",
                "update ",
                "dependabot",
                "dependency"
            };

            return dependabotPatterns.Any(pattern => commitMessage.Contains(pattern));
        }

        private bool IsDependencyFile(string filename)
        {
            var dependencyFilePatterns = new[]
            {
                @"\.csproj$",           // C# project files
                @"packages\.config$",   // NuGet packages.config
                @"\.sln$",              // Solution files (sometimes updated by dependabot)
                @"package\.json$",      // Node.js package.json
                @"package-lock\.json$", // Node.js lock file
                @"yarn\.lock$",         // Yarn lock file
                @"Cargo\.toml$",        // Rust Cargo.toml
                @"Cargo\.lock$",        // Rust Cargo.lock
                @"requirements\.txt$",  // Python requirements
                @"Pipfile$",            // Python Pipfile
                @"Pipfile\.lock$",      // Python Pipfile.lock
                @"pyproject\.toml$",    // Python pyproject.toml
                @"go\.mod$",            // Go modules
                @"go\.sum$",            // Go sum file
                @"Gemfile$",            // Ruby Gemfile
                @"Gemfile\.lock$"       // Ruby Gemfile.lock
            };

            return dependencyFilePatterns.Any(pattern => Regex.IsMatch(filename, pattern, RegexOptions.IgnoreCase));
        }
    }
}