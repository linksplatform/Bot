using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;
    
    /// <summary>
    /// <para>
    /// Represents a trigger that splits a repository into smaller repositories,
    /// with each repository containing a single entity (class, interface, enum).
    /// The namespace becomes the repository name.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    public class SplitRepositoryByEntitiesTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _githubStorage;
        
        // Regex patterns to identify different types of entities
        private static readonly Regex ClassPattern = new(@"^\s*(public\s+|private\s+|protected\s+|internal\s+)*\s*(abstract\s+|sealed\s+|static\s+)*class\s+(\w+)", RegexOptions.Multiline);
        private static readonly Regex InterfacePattern = new(@"^\s*(public\s+|private\s+|protected\s+|internal\s+)*\s*interface\s+(\w+)", RegexOptions.Multiline);
        private static readonly Regex EnumPattern = new(@"^\s*(public\s+|private\s+|protected\s+|internal\s+)*\s*enum\s+(\w+)", RegexOptions.Multiline);
        private static readonly Regex NamespacePattern = new(@"namespace\s+([\w\.]+)", RegexOptions.Multiline);

        /// <summary>
        /// <para>
        /// Represents an entity found in source code.
        /// </para>
        /// <para></para>
        /// </summary>
        public class CodeEntity
        {
            public string Name { get; set; } = string.Empty;
            public EntityType Type { get; set; }
            public string Namespace { get; set; } = string.Empty;
            public string FilePath { get; set; } = string.Empty;
            public string FileContent { get; set; } = string.Empty;
        }

        public enum EntityType
        {
            Class,
            Interface,
            Enum
        }

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="SplitRepositoryByEntitiesTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="githubStorage">
        /// <para>The GitHub storage instance.</para>
        /// <para></para>
        /// </param>
        public SplitRepositoryByEntitiesTrigger(GitHubStorage githubStorage)
        {
            _githubStorage = githubStorage;
        }

        /// <summary>
        /// <para>
        /// Determines whether this trigger should execute based on the issue title.
        /// Trigger phrase: "Split repository by entities"
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The issue context.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>True if the trigger should execute; otherwise, false.</para>
        /// <para></para>
        /// </returns>
        public Task<bool> Condition(TContext context)
        {
            return Task.FromResult(context.Title.ToLower().Contains("split repository by entities"));
        }

        /// <summary>
        /// <para>
        /// Executes the repository splitting action.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The issue context.</para>
        /// <para></para>
        /// </param>
        public async Task Action(TContext context)
        {
            var statusMessage = new StringBuilder();
            statusMessage.AppendLine("🚀 Starting repository splitting by entities...");
            
            try
            {
                // Get the repository to split
                var sourceRepository = context.Repository;
                statusMessage.AppendLine($"📁 Analyzing repository: {sourceRepository.FullName}");

                // Get all repository contents
                var allContents = await _githubStorage.GetAllRepositoryContentsRecursive(sourceRepository);
                var csFiles = allContents.Where(c => c.Type == ContentType.File && c.Name.EndsWith(".cs")).ToList();
                
                statusMessage.AppendLine($"📄 Found {csFiles.Count} C# files to analyze");

                // Extract entities from all C# files
                var allEntities = new List<CodeEntity>();
                foreach (var file in csFiles)
                {
                    var fileContent = _githubStorage.GetDecodedFileContent(file);
                    var entities = ExtractEntitiesFromFile(file.Path, fileContent);
                    allEntities.AddRange(entities);
                }

                statusMessage.AppendLine($"🔍 Extracted {allEntities.Count} entities (classes, interfaces, enums)");

                // Group entities by namespace
                var entitiesByNamespace = GroupEntitiesByNamespace(allEntities);
                statusMessage.AppendLine($"📂 Found {entitiesByNamespace.Count} unique namespaces");

                // Create repositories for each namespace and populate them
                var createdRepositories = new List<string>();
                foreach (var namespaceGroup in entitiesByNamespace)
                {
                    var namespaceName = namespaceGroup.Key;
                    var entities = namespaceGroup.Value;
                    
                    try
                    {
                        // Create repository for this namespace
                        var repositoryName = SanitizeRepositoryName(namespaceName);
                        var description = $"Repository containing {entities.Count} entities from namespace {namespaceName}";
                        
                        var newRepository = await _githubStorage.CreateRepository(repositoryName, description);
                        statusMessage.AppendLine($"✅ Created repository: {newRepository.FullName}");

                        // Add entities to the new repository
                        foreach (var entity in entities)
                        {
                            var fileName = Path.GetFileName(entity.FilePath);
                            var commitMessage = $"Add {entity.Type} {entity.Name} from {sourceRepository.Name}";
                            
                            await _githubStorage.CreateOrUpdateFile(
                                entity.FileContent,
                                newRepository,
                                newRepository.DefaultBranch,
                                fileName,
                                commitMessage
                            );
                            
                            statusMessage.AppendLine($"  📝 Added {entity.Type} {entity.Name} to {fileName}");
                        }
                        
                        createdRepositories.Add(newRepository.FullName);
                    }
                    catch (Exception ex)
                    {
                        statusMessage.AppendLine($"❌ Error creating repository for namespace {namespaceName}: {ex.Message}");
                    }
                }

                statusMessage.AppendLine($"🎉 Repository splitting completed!");
                statusMessage.AppendLine($"📊 Summary:");
                statusMessage.AppendLine($"  - Source repository: {sourceRepository.FullName}");
                statusMessage.AppendLine($"  - Entities processed: {allEntities.Count}");
                statusMessage.AppendLine($"  - Namespaces found: {entitiesByNamespace.Count}");
                statusMessage.AppendLine($"  - Repositories created: {createdRepositories.Count}");
                
                if (createdRepositories.Any())
                {
                    statusMessage.AppendLine("📋 Created repositories:");
                    foreach (var repo in createdRepositories)
                    {
                        statusMessage.AppendLine($"  - {repo}");
                    }
                }
            }
            catch (Exception ex)
            {
                statusMessage.AppendLine($"💥 Fatal error during repository splitting: {ex.Message}");
                Console.WriteLine($"Error in SplitRepositoryByEntitiesTrigger: {ex}");
            }

            // Post the status message as a comment on the issue
            await _githubStorage.CreateIssueComment(context.Repository.Id, context.Number, statusMessage.ToString());
            
            // Close the issue since the operation is complete
            _githubStorage.CloseIssue(context);
        }

        /// <summary>
        /// <para>
        /// Extracts all entities from a source file.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="filePath">
        /// <para>The file path.</para>
        /// <para></para>
        /// </param>
        /// <param name="fileContent">
        /// <para>The file content.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A list of extracted entities.</para>
        /// <para></para>
        /// </returns>
        private List<CodeEntity> ExtractEntitiesFromFile(string filePath, string fileContent)
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
        /// <para>
        /// Groups entities by their namespace.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="entities">
        /// <para>The entities to group.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A dictionary of namespace to entities.</para>
        /// <para></para>
        /// </returns>
        private Dictionary<string, List<CodeEntity>> GroupEntitiesByNamespace(List<CodeEntity> entities)
        {
            return entities
                .GroupBy(e => e.Namespace)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// <para>
        /// Sanitizes a namespace name to be a valid GitHub repository name.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="namespaceName">
        /// <para>The namespace name to sanitize.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A sanitized repository name.</para>
        /// <para></para>
        /// </returns>
        private static string SanitizeRepositoryName(string namespaceName)
        {
            // GitHub repository names must:
            // - Be lowercase
            // - Use hyphens instead of dots/spaces/underscores
            // - Be between 1-100 characters
            return namespaceName
                .ToLowerInvariant()
                .Replace(".", "-")
                .Replace(" ", "-")
                .Replace("_", "-")
                .Substring(0, Math.Min(namespaceName.Length, 100));
        }
    }
}