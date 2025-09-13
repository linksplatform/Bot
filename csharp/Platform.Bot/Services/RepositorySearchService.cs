using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Octokit;
using Storage.Remote.GitHub;

namespace Platform.Bot.Services
{
    /// <summary>
    /// <para>
    /// Service for searching code within repositories.
    /// </para>
    /// <para></para>
    /// </summary>
    public class RepositorySearchService
    {
        private readonly GitHubStorage _gitHubStorage;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="RepositorySearchService"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="gitHubStorage">
        /// <para>The GitHub storage instance.</para>
        /// <para></para>
        /// </param>
        public RepositorySearchService(GitHubStorage gitHubStorage)
        {
            _gitHubStorage = gitHubStorage;
        }

        /// <summary>
        /// <para>
        /// Searches for files containing specific functions or methods.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="repository">
        /// <para>The repository to search in.</para>
        /// <para></para>
        /// </param>
        /// <param name="functionNames">
        /// <para>The function names to search for.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A dictionary mapping function names to file paths.</para>
        /// <para></para>
        /// </returns>
        public async Task<Dictionary<string, List<string>>> SearchForFunctions(Repository repository, List<string> functionNames)
        {
            var results = new Dictionary<string, List<string>>();
            
            foreach (var functionName in functionNames)
            {
                var files = await SearchForFunction(repository, functionName);
                results[functionName] = files;
            }
            
            return results;
        }

        /// <summary>
        /// <para>
        /// Searches for a specific function in the repository.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="repository">
        /// <para>The repository to search in.</para>
        /// <para></para>
        /// </param>
        /// <param name="functionName">
        /// <para>The function name to search for.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A list of file paths containing the function.</para>
        /// <para></para>
        /// </returns>
        private async Task<List<string>> SearchForFunction(Repository repository, string functionName)
        {
            var foundFiles = new List<string>();
            
            try
            {
                // Get the repository tree
                var tree = await GetRepositoryTree(repository);
                
                // Search through files for the function
                foreach (var item in tree.Tree)
                {
                    if (item.Type == TreeType.Blob && IsSourceFile(item.Path))
                    {
                        var containsFunction = await CheckFileContainsFunction(repository, item.Path, functionName);
                        if (containsFunction)
                        {
                            foundFiles.Add(item.Path);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but continue searching
                Console.WriteLine($"Error searching for function {functionName}: {ex.Message}");
            }
            
            return foundFiles;
        }

        /// <summary>
        /// <para>
        /// Gets the complete repository tree.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="repository">
        /// <para>The repository to get the tree for.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The repository tree.</para>
        /// <para></para>
        /// </returns>
        private async Task<TreeResponse> GetRepositoryTree(Repository repository)
        {
            var branch = await _gitHubStorage.GetBranch(repository.Id, repository.DefaultBranch);
            return await _gitHubStorage.Client.Git.Tree.GetRecursive(repository.Id, branch.Commit.Sha);
        }

        /// <summary>
        /// <para>
        /// Checks if a file is a source code file based on its extension.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="filePath">
        /// <para>The file path to check.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>True if the file is a source code file.</para>
        /// <para></para>
        /// </returns>
        private bool IsSourceFile(string filePath)
        {
            var sourceExtensions = new HashSet<string>
            {
                ".cs", ".py", ".js", ".ts", ".java", ".cpp", ".c", ".h", ".hpp",
                ".rb", ".php", ".go", ".rs", ".kt", ".swift", ".scala", ".clj",
                ".hs", ".ml", ".fs", ".vb", ".pas", ".nim", ".cr", ".d"
            };
            
            var extension = System.IO.Path.GetExtension(filePath).ToLower();
            return sourceExtensions.Contains(extension);
        }

        /// <summary>
        /// <para>
        /// Checks if a specific file contains a function definition.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="repository">
        /// <para>The repository containing the file.</para>
        /// <para></para>
        /// </param>
        /// <param name="filePath">
        /// <para>The path to the file to check.</para>
        /// <para></para>
        /// </param>
        /// <param name="functionName">
        /// <para>The function name to search for.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>True if the file contains the function.</para>
        /// <para></para>
        /// </returns>
        private async Task<bool> CheckFileContainsFunction(Repository repository, string filePath, string functionName)
        {
            try
            {
                var fileContents = await _gitHubStorage.Client.Repository.Content.GetAllContents(repository.Id, filePath);
                var content = fileContents.FirstOrDefault()?.Content;
                
                if (string.IsNullOrEmpty(content))
                    return false;
                
                return ContainsFunction(content, functionName, GetFileLanguage(filePath));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// <para>
        /// Determines the programming language based on file extension.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="filePath">
        /// <para>The file path.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The programming language identifier.</para>
        /// <para></para>
        /// </returns>
        private string GetFileLanguage(string filePath)
        {
            var extension = System.IO.Path.GetExtension(filePath).ToLower();
            return extension switch
            {
                ".cs" => "csharp",
                ".py" => "python", 
                ".js" => "javascript",
                ".ts" => "typescript",
                ".java" => "java",
                ".cpp" or ".c" or ".h" or ".hpp" => "cpp",
                ".rb" => "ruby",
                ".php" => "php",
                ".go" => "go",
                ".rs" => "rust",
                ".kt" => "kotlin",
                ".swift" => "swift",
                _ => "unknown"
            };
        }

        /// <summary>
        /// <para>
        /// Checks if content contains a function definition using language-specific patterns.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="content">
        /// <para>The file content to search in.</para>
        /// <para></para>
        /// </param>
        /// <param name="functionName">
        /// <para>The function name to search for.</para>
        /// <para></para>
        /// </param>
        /// <param name="language">
        /// <para>The programming language.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>True if the content contains the function.</para>
        /// <para></para>
        /// </returns>
        private bool ContainsFunction(string content, string functionName, string language)
        {
            var patterns = GetFunctionPatterns(functionName, language);
            
            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// <para>
        /// Gets function definition patterns for different programming languages.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="functionName">
        /// <para>The function name to create patterns for.</para>
        /// <para></para>
        /// </param>
        /// <param name="language">
        /// <para>The programming language.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A list of regex patterns for finding the function.</para>
        /// <para></para>
        /// </returns>
        private List<string> GetFunctionPatterns(string functionName, string language)
        {
            var patterns = new List<string>();
            var escapedName = Regex.Escape(functionName);
            
            switch (language)
            {
                case "csharp":
                    patterns.AddRange(new[]
                    {
                        $@"\b(public|private|protected|internal)?\s*(static)?\s*\w+\s+{escapedName}\s*\(",
                        $@"\b{escapedName}\s*\(",
                        $@"\.{escapedName}\s*\("
                    });
                    break;
                    
                case "python":
                    patterns.AddRange(new[]
                    {
                        $@"def\s+{escapedName}\s*\(",
                        $@"\.{escapedName}\s*\(",
                        $@"{escapedName}\s*="
                    });
                    break;
                    
                case "javascript":
                case "typescript":
                    patterns.AddRange(new[]
                    {
                        $@"function\s+{escapedName}\s*\(",
                        $@"{escapedName}\s*:\s*function",
                        $@"{escapedName}\s*=\s*\(",
                        $@"\.{escapedName}\s*\(",
                        $@"{escapedName}\s*=>\s*"
                    });
                    break;
                    
                case "java":
                    patterns.AddRange(new[]
                    {
                        $@"\b(public|private|protected)?\s*(static)?\s*\w+\s+{escapedName}\s*\(",
                        $@"\.{escapedName}\s*\("
                    });
                    break;
                    
                case "cpp":
                    patterns.AddRange(new[]
                    {
                        $@"\b\w+\s+{escapedName}\s*\(",
                        $@"{escapedName}\s*\(",
                        $@"::{escapedName}\s*\("
                    });
                    break;
                    
                default:
                    patterns.AddRange(new[]
                    {
                        $@"\b{escapedName}\s*\(",
                        $@"\.{escapedName}\s*\(",
                        $@"{escapedName}\s*="
                    });
                    break;
            }
            
            return patterns;
        }

        /// <summary>
        /// <para>
        /// Searches for files that might be related to the bug based on keywords.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="repository">
        /// <para>The repository to search in.</para>
        /// <para></para>
        /// </param>
        /// <param name="keywords">
        /// <para>Keywords to search for.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A list of potentially relevant file paths.</para>
        /// <para></para>
        /// </returns>
        public async Task<List<string>> SearchByKeywords(Repository repository, List<string> keywords)
        {
            var relevantFiles = new List<string>();
            
            try
            {
                var tree = await GetRepositoryTree(repository);
                
                foreach (var item in tree.Tree)
                {
                    if (item.Type == TreeType.Blob && IsSourceFile(item.Path))
                    {
                        var isRelevant = await CheckFileContainsKeywords(repository, item.Path, keywords);
                        if (isRelevant)
                        {
                            relevantFiles.Add(item.Path);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching by keywords: {ex.Message}");
            }
            
            return relevantFiles;
        }

        /// <summary>
        /// <para>
        /// Checks if a file contains any of the specified keywords.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="repository">
        /// <para>The repository containing the file.</para>
        /// <para></para>
        /// </param>
        /// <param name="filePath">
        /// <para>The path to the file to check.</para>
        /// <para></para>
        /// </param>
        /// <param name="keywords">
        /// <para>The keywords to search for.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>True if the file contains any of the keywords.</para>
        /// <para></para>
        /// </returns>
        private async Task<bool> CheckFileContainsKeywords(Repository repository, string filePath, List<string> keywords)
        {
            try
            {
                var fileContents = await _gitHubStorage.Client.Repository.Content.GetAllContents(repository.Id, filePath);
                var content = fileContents.FirstOrDefault()?.Content?.ToLower();
                
                if (string.IsNullOrEmpty(content))
                    return false;
                
                return keywords.Any(keyword => content.Contains(keyword.ToLower()));
            }
            catch
            {
                return false;
            }
        }
    }
}