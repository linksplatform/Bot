using Octokit;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Experiments
{
    /// <summary>
    /// Research class to understand what constitutes an "entity" in a codebase
    /// and how to identify them for repository splitting.
    /// 
    /// Based on the issue, an "entity" is:
    /// - Class
    /// - Interface  
    /// - Enum
    /// - Usually contained in a single file
    /// - The namespace becomes the repository name
    /// </summary>
    public class RepositorySplittingResearch
    {
        // Regex patterns to identify different types of entities
        private static readonly Regex ClassPattern = new(@"^\s*(public\s+|private\s+|protected\s+|internal\s+)*\s*(abstract\s+|sealed\s+|static\s+)*class\s+(\w+)", RegexOptions.Multiline);
        private static readonly Regex InterfacePattern = new(@"^\s*(public\s+|private\s+|protected\s+|internal\s+)*\s*interface\s+(\w+)", RegexOptions.Multiline);
        private static readonly Regex EnumPattern = new(@"^\s*(public\s+|private\s+|protected\s+|internal\s+)*\s*enum\s+(\w+)", RegexOptions.Multiline);
        private static readonly Regex NamespacePattern = new(@"namespace\s+([\w\.]+)", RegexOptions.Multiline);

        /// <summary>
        /// Represents an entity found in source code
        /// </summary>
        public class CodeEntity
        {
            public string Name { get; set; }
            public EntityType Type { get; set; }
            public string Namespace { get; set; }
            public string FilePath { get; set; }
            public string FileContent { get; set; }
        }

        public enum EntityType
        {
            Class,
            Interface,
            Enum
        }

        /// <summary>
        /// Analyzes a source file and extracts all entities
        /// </summary>
        public static List<CodeEntity> ExtractEntitiesFromFile(string filePath, string fileContent)
        {
            var entities = new List<CodeEntity>();
            
            // Extract namespace
            var namespaceMatch = NamespacePattern.Match(fileContent);
            string namespaceName = namespaceMatch.Success ? namespaceMatch.Groups[1].Value : "DefaultNamespace";

            // Find classes
            foreach (Match match in ClassPattern.Matches(fileContent))
            {
                entities.Add(new CodeEntity
                {
                    Name = match.Groups[3].Value,
                    Type = EntityType.Class,
                    Namespace = namespaceName,
                    FilePath = filePath,
                    FileContent = fileContent
                });
            }

            // Find interfaces  
            foreach (Match match in InterfacePattern.Matches(fileContent))
            {
                entities.Add(new CodeEntity
                {
                    Name = match.Groups[2].Value,
                    Type = EntityType.Interface,
                    Namespace = namespaceName,
                    FilePath = filePath,
                    FileContent = fileContent
                });
            }

            // Find enums
            foreach (Match match in EnumPattern.Matches(fileContent))
            {
                entities.Add(new CodeEntity
                {
                    Name = match.Groups[2].Value,
                    Type = EntityType.Enum,
                    Namespace = namespaceName,
                    FilePath = filePath,
                    FileContent = fileContent
                });
            }

            return entities;
        }

        /// <summary>
        /// Groups entities by namespace for repository creation
        /// </summary>
        public static Dictionary<string, List<CodeEntity>> GroupEntitiesByNamespace(List<CodeEntity> entities)
        {
            return entities
                .GroupBy(e => e.Namespace)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// Scans a repository and identifies all entities that could be split
        /// </summary>
        public static async Task<Dictionary<string, List<CodeEntity>>> AnalyzeRepositoryForSplitting(
            GitHubClient client, string owner, string repoName)
        {
            var allEntities = new List<CodeEntity>();
            
            try
            {
                // Get repository content recursively
                var contents = await client.Repository.Content.GetAllContents(owner, repoName);
                await ProcessContents(client, owner, repoName, contents, "", allEntities);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error analyzing repository: {ex.Message}");
            }

            return GroupEntitiesByNamespace(allEntities);
        }

        private static async Task ProcessContents(GitHubClient client, string owner, string repoName, 
            IReadOnlyList<RepositoryContent> contents, string currentPath, List<CodeEntity> allEntities)
        {
            foreach (var content in contents)
            {
                if (content.Type == ContentType.File && content.Name.EndsWith(".cs"))
                {
                    try
                    {
                        var fileContent = content.Content;
                        if (content.Encoding == "base64")
                        {
                            var bytes = Convert.FromBase64String(content.Content);
                            fileContent = System.Text.Encoding.UTF8.GetString(bytes);
                        }

                        var entities = ExtractEntitiesFromFile(content.Path, fileContent);
                        allEntities.AddRange(entities);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing file {content.Path}: {ex.Message}");
                    }
                }
                else if (content.Type == ContentType.Dir)
                {
                    try
                    {
                        var subContents = await client.Repository.Content.GetAllContents(owner, repoName, content.Path);
                        await ProcessContents(client, owner, repoName, subContents, content.Path, allEntities);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing directory {content.Path}: {ex.Message}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Repository creation and management for split repositories
    /// </summary>
    public class RepositoryManager
    {
        private readonly GitHubClient _client;
        private readonly string _owner;

        public RepositoryManager(GitHubClient client, string owner)
        {
            _client = client;
            _owner = owner;
        }

        /// <summary>
        /// Creates a new repository for an entity namespace
        /// </summary>
        public async Task<Repository> CreateRepositoryForNamespace(string namespaceName)
        {
            var repositoryName = SanitizeRepositoryName(namespaceName);
            
            var newRepository = new NewRepository(repositoryName)
            {
                Description = $"Repository for {namespaceName} entities",
                Private = false, // Can be configurable
                AutoInit = true
            };

            try
            {
                return await _client.Repository.Create(_owner, newRepository);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating repository {repositoryName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sanitizes namespace name to be a valid GitHub repository name
        /// </summary>
        private static string SanitizeRepositoryName(string namespaceName)
        {
            // GitHub repository names must:
            // - Be lowercase
            // - Use hyphens instead of dots/spaces
            // - Be between 1-100 characters
            return namespaceName
                .ToLowerInvariant()
                .Replace(".", "-")
                .Replace(" ", "-")
                .Replace("_", "-");
        }

        /// <summary>
        /// Creates files in the target repository for the entities
        /// </summary>
        public async Task PopulateRepositoryWithEntities(Repository targetRepo, List<RepositorySplittingResearch.CodeEntity> entities)
        {
            foreach (var entity in entities)
            {
                try
                {
                    var fileName = Path.GetFileName(entity.FilePath);
                    var commitMessage = $"Add {entity.Type} {entity.Name}";
                    
                    await _client.Repository.Content.CreateFile(
                        targetRepo.Id,
                        fileName,
                        new CreateFileRequest(commitMessage, entity.FileContent)
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating file for entity {entity.Name}: {ex.Message}");
                }
            }
        }
    }
}