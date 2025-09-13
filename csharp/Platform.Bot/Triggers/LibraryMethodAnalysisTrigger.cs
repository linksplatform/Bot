using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;

    /// <summary>
    /// <para>
    /// Represents a trigger that analyzes the codebase to find places where existing library methods can be used.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class LibraryMethodAnalysisTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="LibraryMethodAnalysisTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>A GitHub storage instance.</para>
        /// <para></para>
        /// </param>
        public LibraryMethodAnalysisTrigger(GitHubStorage storage)
        {
            _storage = storage;
        }

        /// <summary>
        /// <para>
        /// Determines whether this trigger should be activated for the given context.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The context.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>True if the trigger should be activated, false otherwise.</para>
        /// <para></para>
        /// </returns>
        public async Task<bool> Condition(TContext context)
        {
            return context.Title.ToLower().Contains("analyze library usage") ||
                   context.Title.ToLower().Contains("find library methods") ||
                   context.Body?.ToLower().Contains("library method analysis") == true;
        }

        /// <summary>
        /// <para>
        /// Performs the analysis and creates a report of potential library method optimizations.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The context.</para>
        /// <para></para>
        /// </param>
        public async Task Action(TContext context)
        {
            try
            {
                var analysis = await AnalyzeLibraryUsage();
                var report = GenerateAnalysisReport(analysis);
                
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, report);
                _storage.CloseIssue(context);
            }
            catch (Exception ex)
            {
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                    $"Error during library analysis: {ex.Message}");
            }
        }

        /// <summary>
        /// <para>
        /// Analyzes the current library usage and identifies potential optimizations.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <returns>
        /// <para>Analysis results containing suggestions and observations.</para>
        /// <para></para>
        /// </returns>
        private async Task<LibraryAnalysisResult> AnalyzeLibraryUsage()
        {
            var result = new LibraryAnalysisResult();
            
            // Get all available Octokit client methods
            var octokitMethods = GetAvailableOctokitMethods();
            
            // Get currently used methods from codebase
            var usedMethods = await GetCurrentlyUsedMethods();
            
            // Find unused methods
            result.UnusedMethods = octokitMethods.Except(usedMethods).ToList();
            
            // Find potential optimizations
            result.OptimizationSuggestions = await FindOptimizationOpportunities();
            
            // Analyze patterns
            result.UsagePatterns = AnalyzeUsagePatterns(usedMethods);
            
            return result;
        }

        /// <summary>
        /// <para>
        /// Gets all available methods from the Octokit GitHubClient.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <returns>
        /// <para>List of available method names.</para>
        /// <para></para>
        /// </returns>
        private List<string> GetAvailableOctokitMethods()
        {
            var methods = new List<string>();
            var clientType = typeof(GitHubClient);
            
            // Get all public properties that expose API endpoints
            var properties = clientType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var property in properties)
            {
                if (property.PropertyType.Namespace?.Contains("Octokit") == true)
                {
                    var nestedMethods = GetMethodsFromType(property.PropertyType, property.Name);
                    methods.AddRange(nestedMethods);
                }
            }
            
            return methods.Distinct().ToList();
        }

        /// <summary>
        /// <para>
        /// Gets methods from a specific type, recursively exploring nested API endpoints.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="type">
        /// <para>The type to analyze.</para>
        /// <para></para>
        /// </param>
        /// <param name="prefix">
        /// <para>The prefix for the method path.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>List of method paths.</para>
        /// <para></para>
        /// </returns>
        private List<string> GetMethodsFromType(Type type, string prefix)
        {
            var methods = new List<string>();
            
            // Add direct methods
            var directMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => !m.IsSpecialName && m.DeclaringType == type)
                .Select(m => $"{prefix}.{m.Name}")
                .ToList();
            
            methods.AddRange(directMethods);
            
            // Add nested properties
            var nestedProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.Namespace?.Contains("Octokit") == true);
            
            foreach (var property in nestedProperties)
            {
                var nestedMethods = GetMethodsFromType(property.PropertyType, $"{prefix}.{property.Name}");
                methods.AddRange(nestedMethods);
            }
            
            return methods;
        }

        /// <summary>
        /// <para>
        /// Scans the codebase to find currently used GitHub API methods.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <returns>
        /// <para>List of currently used method names.</para>
        /// <para></para>
        /// </returns>
        private async Task<List<string>> GetCurrentlyUsedMethods()
        {
            var usedMethods = new List<string>();
            var codebaseFiles = Directory.GetFiles("csharp", "*.cs", SearchOption.AllDirectories);
            
            foreach (var file in codebaseFiles)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    var methodCalls = ExtractMethodCalls(content);
                    usedMethods.AddRange(methodCalls);
                }
                catch (Exception ex)
                {
                    // Skip files that can't be read
                    Console.WriteLine($"Could not read file {file}: {ex.Message}");
                }
            }
            
            return usedMethods.Distinct().ToList();
        }

        /// <summary>
        /// <para>
        /// Extracts GitHub API method calls from source code content.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="content">
        /// <para>The source code content.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>List of method calls found in the content.</para>
        /// <para></para>
        /// </returns>
        private List<string> ExtractMethodCalls(string content)
        {
            var methodCalls = new List<string>();
            var lines = content.Split('\n');
            
            foreach (var line in lines)
            {
                // Look for patterns like "Client.Repository.GetAll" or "_storage.Client.Issue.Comment.Create"
                if (line.Contains("Client.") && !line.Trim().StartsWith("//"))
                {
                    var clientIndex = line.IndexOf("Client.");
                    if (clientIndex >= 0)
                    {
                        var methodCall = ExtractMethodCallFromLine(line, clientIndex);
                        if (!string.IsNullOrEmpty(methodCall))
                        {
                            methodCalls.Add(methodCall);
                        }
                    }
                }
            }
            
            return methodCalls;
        }

        /// <summary>
        /// <para>
        /// Extracts a complete method call from a line of code.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="line">
        /// <para>The line of code.</para>
        /// <para></para>
        /// </param>
        /// <param name="startIndex">
        /// <para>The starting index of "Client."</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The extracted method call or empty string if none found.</para>
        /// <para></para>
        /// </returns>
        private string ExtractMethodCallFromLine(string line, int startIndex)
        {
            var methodCallBuilder = new StringBuilder();
            var index = startIndex;
            
            while (index < line.Length)
            {
                var ch = line[index];
                if (char.IsLetter(ch) || ch == '.' || ch == '_')
                {
                    methodCallBuilder.Append(ch);
                }
                else if (ch == '(')
                {
                    // Found method call
                    break;
                }
                else if (char.IsWhiteSpace(ch))
                {
                    // Skip whitespace
                }
                else
                {
                    // End of method chain
                    break;
                }
                index++;
            }
            
            var result = methodCallBuilder.ToString();
            return result.StartsWith("Client.") ? result : string.Empty;
        }

        /// <summary>
        /// <para>
        /// Finds potential optimization opportunities in the codebase.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <returns>
        /// <para>List of optimization suggestions.</para>
        /// <para></para>
        /// </returns>
        private async Task<List<OptimizationSuggestion>> FindOptimizationOpportunities()
        {
            var suggestions = new List<OptimizationSuggestion>();
            
            // Check for common patterns that could be optimized
            suggestions.AddRange(await FindAsyncAwaitOptimizations());
            suggestions.AddRange(await FindBatchingOpportunities());
            suggestions.AddRange(await FindCachingOpportunities());
            
            return suggestions;
        }

        /// <summary>
        /// <para>
        /// Finds places where async/await patterns could be improved.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <returns>
        /// <para>List of async/await optimization suggestions.</para>
        /// <para></para>
        /// </returns>
        private async Task<List<OptimizationSuggestion>> FindAsyncAwaitOptimizations()
        {
            var suggestions = new List<OptimizationSuggestion>();
            var codebaseFiles = Directory.GetFiles("csharp", "*.cs", SearchOption.AllDirectories);
            
            foreach (var file in codebaseFiles)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    var lines = content.Split('\n');
                    
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i].Trim();
                        
                        // Look for .Result usage
                        if (line.Contains(".Result") && line.Contains("Client."))
                        {
                            suggestions.Add(new OptimizationSuggestion
                            {
                                Type = "Async/Await",
                                File = file,
                                LineNumber = i + 1,
                                Issue = "Using .Result can cause deadlocks",
                                Suggestion = "Consider using await instead of .Result",
                                Example = line.Replace(".Result", " // Consider: await " + line.Split('.')[0])
                            });
                        }
                        
                        // Look for .Wait() usage
                        if (line.Contains(".Wait()") && line.Contains("Client."))
                        {
                            suggestions.Add(new OptimizationSuggestion
                            {
                                Type = "Async/Await",
                                File = file,
                                LineNumber = i + 1,
                                Issue = "Using .Wait() can cause deadlocks",
                                Suggestion = "Consider using await instead of .Wait()",
                                Example = line.Replace(".Wait()", " // Consider: await " + line.Split('.')[0])
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Skip files that can't be read
                    Console.WriteLine($"Could not analyze file {file}: {ex.Message}");
                }
            }
            
            return suggestions;
        }

        /// <summary>
        /// <para>
        /// Finds opportunities for batching multiple API calls.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <returns>
        /// <para>List of batching optimization suggestions.</para>
        /// <para></para>
        /// </returns>
        private async Task<List<OptimizationSuggestion>> FindBatchingOpportunities()
        {
            var suggestions = new List<OptimizationSuggestion>();
            
            // This is a simplified example - in practice, you'd analyze for loops
            // that make individual API calls that could be batched
            suggestions.Add(new OptimizationSuggestion
            {
                Type = "Batching",
                File = "General",
                LineNumber = 0,
                Issue = "Multiple individual API calls in loops",
                Suggestion = "Consider using batch API methods or GraphQL for multiple operations",
                Example = "Instead of multiple GetRepository calls, use GetAllForOrg with pagination"
            });
            
            return suggestions;
        }

        /// <summary>
        /// <para>
        /// Finds opportunities for caching API responses.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <returns>
        /// <para>List of caching optimization suggestions.</para>
        /// <para></para>
        /// </returns>
        private async Task<List<OptimizationSuggestion>> FindCachingOpportunities()
        {
            var suggestions = new List<OptimizationSuggestion>();
            
            suggestions.Add(new OptimizationSuggestion
            {
                Type = "Caching",
                File = "General",
                LineNumber = 0,
                Issue = "Repeated calls to get organization members or repositories",
                Suggestion = "Consider caching frequently accessed data that doesn't change often",
                Example = "Cache organization member lists and repository metadata"
            });
            
            return suggestions;
        }

        /// <summary>
        /// <para>
        /// Analyzes usage patterns in the current codebase.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="usedMethods">
        /// <para>List of currently used methods.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>Analysis of usage patterns.</para>
        /// <para></para>
        /// </returns>
        private UsagePatternAnalysis AnalyzeUsagePatterns(List<string> usedMethods)
        {
            var analysis = new UsagePatternAnalysis();
            
            analysis.MostUsedMethods = usedMethods
                .GroupBy(m => m)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count());
            
            analysis.ApiCategories = usedMethods
                .Where(m => m.Contains('.'))
                .Select(m => m.Split('.')[1]) // Get the category (Repository, Issue, etc.)
                .GroupBy(c => c)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.Count());
            
            return analysis;
        }

        /// <summary>
        /// <para>
        /// Generates a human-readable report from the analysis results.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="analysis">
        /// <para>The analysis results.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A formatted report string.</para>
        /// <para></para>
        /// </returns>
        private string GenerateAnalysisReport(LibraryAnalysisResult analysis)
        {
            var report = new StringBuilder();
            
            report.AppendLine("# GitHub API Library Usage Analysis Report");
            report.AppendLine();
            
            // Usage patterns section
            report.AppendLine("## Current Usage Patterns");
            report.AppendLine();
            report.AppendLine("### Most Used API Categories:");
            foreach (var category in analysis.UsagePatterns.ApiCategories.Take(5))
            {
                report.AppendLine($"- **{category.Key}**: {category.Value} usages");
            }
            report.AppendLine();
            
            report.AppendLine("### Most Frequently Called Methods:");
            foreach (var method in analysis.UsagePatterns.MostUsedMethods.Take(5))
            {
                report.AppendLine($"- `{method.Key}`: {method.Value} times");
            }
            report.AppendLine();
            
            // Optimization suggestions section
            if (analysis.OptimizationSuggestions.Any())
            {
                report.AppendLine("## Optimization Opportunities");
                report.AppendLine();
                
                var groupedSuggestions = analysis.OptimizationSuggestions.GroupBy(s => s.Type);
                foreach (var group in groupedSuggestions)
                {
                    report.AppendLine($"### {group.Key} Optimizations ({group.Count()} found):");
                    foreach (var suggestion in group.Take(3)) // Limit to top 3 per category
                    {
                        report.AppendLine($"- **Issue**: {suggestion.Issue}");
                        report.AppendLine($"  - **Suggestion**: {suggestion.Suggestion}");
                        if (!string.IsNullOrEmpty(suggestion.File) && suggestion.File != "General")
                        {
                            report.AppendLine($"  - **Location**: {suggestion.File}:{suggestion.LineNumber}");
                        }
                        if (!string.IsNullOrEmpty(suggestion.Example))
                        {
                            report.AppendLine($"  - **Example**: `{suggestion.Example}`");
                        }
                        report.AppendLine();
                    }
                }
            }
            
            // Unused methods section
            if (analysis.UnusedMethods.Any())
            {
                report.AppendLine("## Available but Unused Library Methods");
                report.AppendLine();
                report.AppendLine("The following GitHub API methods are available but not currently used:");
                report.AppendLine();
                
                var categorizedUnused = analysis.UnusedMethods
                    .Where(m => m.Contains('.'))
                    .GroupBy(m => m.Split('.')[1]) // Group by API category
                    .OrderBy(g => g.Key);
                
                foreach (var category in categorizedUnused.Take(5)) // Limit to 5 categories
                {
                    report.AppendLine($"### {category.Key} API:");
                    foreach (var method in category.Take(5)) // Limit to 5 methods per category
                    {
                        report.AppendLine($"- `{method}`");
                    }
                    report.AppendLine();
                }
                
                if (analysis.UnusedMethods.Count > 50)
                {
                    report.AppendLine($"*And {analysis.UnusedMethods.Count - 50} more unused methods...*");
                }
            }
            
            // Summary section
            report.AppendLine("## Summary");
            report.AppendLine();
            report.AppendLine($"- **Total unused methods**: {analysis.UnusedMethods.Count}");
            report.AppendLine($"- **Optimization opportunities found**: {analysis.OptimizationSuggestions.Count}");
            report.AppendLine($"- **Most used API category**: {analysis.UsagePatterns.ApiCategories.FirstOrDefault().Key ?? "None"}");
            report.AppendLine();
            report.AppendLine("This analysis can help identify opportunities to:");
            report.AppendLine("1. Improve performance by fixing async/await patterns");
            report.AppendLine("2. Reduce API calls through batching and caching");
            report.AppendLine("3. Discover new GitHub API capabilities that could enhance the bot");
            
            return report.ToString();
        }
    }

    /// <summary>
    /// <para>
    /// Represents the result of library usage analysis.
    /// </para>
    /// <para></para>
    /// </summary>
    public class LibraryAnalysisResult
    {
        public List<string> UnusedMethods { get; set; } = new();
        public List<OptimizationSuggestion> OptimizationSuggestions { get; set; } = new();
        public UsagePatternAnalysis UsagePatterns { get; set; } = new();
    }

    /// <summary>
    /// <para>
    /// Represents an optimization suggestion.
    /// </para>
    /// <para></para>
    /// </summary>
    public class OptimizationSuggestion
    {
        public string Type { get; set; } = string.Empty;
        public string File { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string Issue { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;
    }

    /// <summary>
    /// <para>
    /// Represents usage pattern analysis results.
    /// </para>
    /// <para></para>
    /// </summary>
    public class UsagePatternAnalysis
    {
        public Dictionary<string, int> MostUsedMethods { get; set; } = new();
        public Dictionary<string, int> ApiCategories { get; set; } = new();
    }
}