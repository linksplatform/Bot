using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Storage.Local;
using Storage.Remote.GitHub;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using Storage.Analysis;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;

    /// <summary>
    /// <para>
    /// Represents the bug reproduction simplification trigger.
    /// </para>
    /// <para>This trigger analyzes a repository to identify code that affects bug reproduction and removes unnecessary code.</para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class SimplifyBugReproductionTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        private readonly FileStorage _fileStorage;
        private readonly BugReproductionAnalyzer _analyzer;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="SimplifyBugReproductionTrigger"/> instance.
        /// </para>
        /// </summary>
        /// <param name="storage">The GitHub storage.</param>
        /// <param name="fileStorage">The file storage.</param>
        public SimplifyBugReproductionTrigger(GitHubStorage storage, FileStorage fileStorage)
        {
            _storage = storage;
            _fileStorage = fileStorage;
            _analyzer = new BugReproductionAnalyzer();
        }

        /// <summary>
        /// <para>
        /// Determines whether this trigger should activate for the given issue.
        /// </para>
        /// </summary>
        /// <param name="context">The issue context.</param>
        /// <returns>True if the issue title contains "simplify bug reproduction".</returns>
        public async Task<bool> Condition(TContext context)
        {
            return context.Title.ToLower().Contains("simplify bug reproduction") ||
                   context.Title.ToLower().Contains("minimize reproduction") ||
                   context.Title.ToLower().Contains("reduce bug example");
        }

        /// <summary>
        /// <para>
        /// Performs the bug reproduction simplification action.
        /// </para>
        /// </summary>
        /// <param name="context">The issue context.</param>
        public async Task Action(TContext context)
        {
            try
            {
                // Get repository files
                var repoFiles = await _storage.GetRepositoryFiles(context.Repository);
                
                // Analyze files to identify dependencies and critical paths
                var analysisResult = await _analyzer.AnalyzeBugReproduction(repoFiles, context);
                
                // Create simplified version by removing non-critical code
                var simplifiedFiles = await _analyzer.SimplifyCodebase(repoFiles, analysisResult);
                
                // Create a new branch for the simplified version
                string simplifiedBranch = $"simplified-bug-reproduction-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
                await _storage.CreateBranch(context.Repository, simplifiedBranch);
                
                // Upload simplified files to the new branch
                foreach (var file in simplifiedFiles)
                {
                    await _storage.CreateOrUpdateFile(
                        file.Content, 
                        context.Repository, 
                        simplifiedBranch, 
                        file.Path, 
                        $"Simplified: {file.Path} - removed non-critical code"
                    );
                }
                
                // Create a summary report
                var report = GenerateSimplificationReport(analysisResult, repoFiles, simplifiedFiles);
                
                // Add comment to the issue with results
                await _storage.CreateIssueComment(context, report);
                
                // Close the issue
                await _storage.CloseIssueAsync(context);
            }
            catch (Exception ex)
            {
                await _storage.CreateIssueComment(context, 
                    $"Error during bug reproduction simplification: {ex.Message}\n\nPlease check the repository structure and try again.");
            }
        }

        private string GenerateSimplificationReport(BugAnalysisResult analysis, List<RepositoryFile> original, List<RepositoryFile> simplified)
        {
            var originalCount = original.Count;
            var simplifiedCount = simplified.Count;
            var removedCount = originalCount - simplifiedCount;
            var removalPercentage = originalCount > 0 ? (removedCount * 100.0 / originalCount) : 0;

            var report = $@"## Bug Reproduction Simplification Complete

### Summary
- **Original files**: {originalCount}
- **Simplified files**: {simplifiedCount}
- **Removed files**: {removedCount} ({removalPercentage:F1}%)

### Files Kept (Critical for bug reproduction)
{string.Join("\n", analysis.CriticalFiles.Select(f => $"- `{f}` - {analysis.FileReasons.GetValueOrDefault(f, "Critical dependency")}"))}

### Files Removed (Non-critical)
{string.Join("\n", analysis.RemovedFiles.Select(f => $"- `{f}` - {analysis.RemovalReasons.GetValueOrDefault(f, "Not required for bug reproduction")}"))}

### Analysis Details
- **Entry points detected**: {analysis.EntryPoints.Count}
- **Critical dependencies**: {analysis.CriticalDependencies.Count}
- **Test files preserved**: {analysis.TestFiles.Count}

### Next Steps
1. Check the `simplified-bug-reproduction-*` branch for the minimal reproduction case
2. Verify that the bug still reproduces with the simplified code
3. Use this simplified version for easier debugging and issue reporting

The simplified version should make it easier to:
- Understand the root cause of the bug
- Create focused unit tests
- Implement targeted fixes
- Share minimal reproduction examples with maintainers
";

            return report;
        }
    }
}