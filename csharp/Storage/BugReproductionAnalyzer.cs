using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Octokit;

namespace Storage.Analysis
{
    /// <summary>
    /// <para>
    /// Analyzes code repositories to identify files critical for bug reproduction.
    /// </para>
    /// <para>Uses static analysis techniques similar to code coverage to determine dependencies and critical paths.</para>
    /// </summary>
    public class BugReproductionAnalyzer
    {
        private readonly Dictionary<string, List<string>> _languagePatterns;

        public BugReproductionAnalyzer()
        {
            _languagePatterns = InitializeLanguagePatterns();
        }

        /// <summary>
        /// Analyzes the repository to identify which files are critical for bug reproduction.
        /// </summary>
        /// <param name="files">All files in the repository.</param>
        /// <param name="issue">The GitHub issue containing bug information.</param>
        /// <returns>Analysis result with critical and removable files.</returns>
        public async Task<BugAnalysisResult> AnalyzeBugReproduction(List<RepositoryFile> files, Issue issue)
        {
            var result = new BugAnalysisResult();

            // Step 1: Identify entry points
            IdentifyEntryPoints(files, result);

            // Step 2: Analyze dependencies between files
            AnalyzeDependencies(files, result);

            // Step 3: Identify files mentioned in the bug report
            AnalyzeBugReportReferences(files, issue, result);

            // Step 4: Perform dependency graph traversal to find critical files
            TraverseCriticalPaths(files, result);

            // Step 5: Identify removable files
            IdentifyRemovableFiles(files, result);

            // Step 6: Calculate confidence score
            CalculateConfidenceScore(result);

            return result;
        }

        /// <summary>
        /// Creates a simplified version of the codebase with only critical files.
        /// </summary>
        /// <param name="originalFiles">Original repository files.</param>
        /// <param name="analysis">Analysis result.</param>
        /// <returns>Simplified list of files.</returns>
        public async Task<List<RepositoryFile>> SimplifyCodebase(List<RepositoryFile> originalFiles, BugAnalysisResult analysis)
        {
            var simplifiedFiles = new List<RepositoryFile>();

            foreach (var file in originalFiles)
            {
                if (analysis.CriticalFiles.Contains(file.Path))
                {
                    // For critical files, we might still want to simplify their content
                    var simplifiedFile = await SimplifyFileContent(file, analysis);
                    simplifiedFiles.Add(simplifiedFile);
                }
            }

            return simplifiedFiles;
        }

        private void IdentifyEntryPoints(List<RepositoryFile> files, BugAnalysisResult result)
        {
            foreach (var file in files)
            {
                // Mark as entry point based on file patterns
                if (IsMainFile(file) || IsTestFile(file) || IsConfigFile(file))
                {
                    result.EntryPoints.Add(file.Path);
                    result.CriticalFiles.Add(file.Path);
                    
                    if (IsTestFile(file))
                    {
                        result.TestFiles.Add(file.Path);
                        result.FileReasons[file.Path] = "Test file - likely demonstrates the bug";
                    }
                    else if (IsMainFile(file))
                    {
                        result.FileReasons[file.Path] = "Entry point - main executable file";
                    }
                    else if (IsConfigFile(file))
                    {
                        result.ConfigFiles.Add(file.Path);
                        result.FileReasons[file.Path] = "Configuration file - affects execution environment";
                    }
                }
            }
        }

        private void AnalyzeDependencies(List<RepositoryFile> files, BugAnalysisResult result)
        {
            foreach (var file in files)
            {
                var dependencies = ExtractDependencies(file);
                if (dependencies.Any())
                {
                    result.CriticalDependencies[file.Path] = new HashSet<string>(dependencies);
                }
            }
        }

        private void AnalyzeBugReportReferences(List<RepositoryFile> files, Issue issue, BugAnalysisResult result)
        {
            var bugText = $"{issue.Title} {issue.Body}".ToLower();
            
            foreach (var file in files)
            {
                // Check if file is mentioned in the bug report
                if (bugText.Contains(file.FileName.ToLower()) || 
                    bugText.Contains(file.Path.ToLower()) ||
                    IsReferencedInBugReport(file, bugText))
                {
                    result.CriticalFiles.Add(file.Path);
                    result.FileReasons[file.Path] = "Referenced in bug report";
                }

                // Check if file contains error patterns mentioned in the bug report
                if (ContainsErrorPatterns(file, bugText))
                {
                    result.CriticalFiles.Add(file.Path);
                    result.FileReasons[file.Path] = "Contains error patterns from bug report";
                }
            }
        }

        private void TraverseCriticalPaths(List<RepositoryFile> files, BugAnalysisResult result)
        {
            var visited = new HashSet<string>();
            var queue = new Queue<string>(result.EntryPoints);

            while (queue.Count > 0)
            {
                var currentFile = queue.Dequeue();
                if (visited.Contains(currentFile)) continue;

                visited.Add(currentFile);
                result.CriticalFiles.Add(currentFile);

                // Add dependencies to the queue
                if (result.CriticalDependencies.ContainsKey(currentFile))
                {
                    foreach (var dependency in result.CriticalDependencies[currentFile])
                    {
                        if (!visited.Contains(dependency))
                        {
                            queue.Enqueue(dependency);
                            result.FileReasons[dependency] = $"Dependency of {currentFile}";
                        }
                    }
                }
            }
        }

        private void IdentifyRemovableFiles(List<RepositoryFile> files, BugAnalysisResult result)
        {
            foreach (var file in files)
            {
                if (!result.CriticalFiles.Contains(file.Path))
                {
                    result.RemovedFiles.Add(file.Path);
                    
                    // Determine reason for removal
                    if (IsDocumentationFile(file))
                    {
                        result.RemovalReasons[file.Path] = "Documentation file - not needed for bug reproduction";
                    }
                    else if (IsExampleFile(file))
                    {
                        result.RemovalReasons[file.Path] = "Example file - not part of core functionality";
                    }
                    else if (IsBuildFile(file))
                    {
                        result.RemovalReasons[file.Path] = "Build/tooling file - not needed for bug reproduction";
                    }
                    else
                    {
                        result.RemovalReasons[file.Path] = "Not in critical dependency path";
                    }
                }
            }
        }

        private void CalculateConfidenceScore(BugAnalysisResult result)
        {
            double score = 0.8; // Base confidence

            // Increase confidence if we found entry points
            if (result.EntryPoints.Any()) score += 0.1;

            // Increase confidence if we found test files
            if (result.TestFiles.Any()) score += 0.05;

            // Decrease confidence if we removed too few files (might be too conservative)
            var totalFiles = result.CriticalFiles.Count + result.RemovedFiles.Count;
            var removalRate = totalFiles > 0 ? (double)result.RemovedFiles.Count / totalFiles : 0;
            
            if (removalRate < 0.1) score -= 0.1; // Removed less than 10%
            if (removalRate > 0.8) score -= 0.2; // Removed more than 80% (might be too aggressive)

            result.ConfidenceScore = Math.Max(0.0, Math.Min(1.0, score));

            // Add warnings based on confidence
            if (result.ConfidenceScore < 0.6)
            {
                result.Warnings.Add("Low confidence in analysis. Manual review recommended.");
            }
            if (removalRate > 0.8)
            {
                result.Warnings.Add("High removal rate detected. Verify that critical functionality is preserved.");
            }
        }

        private async Task<RepositoryFile> SimplifyFileContent(RepositoryFile file, BugAnalysisResult analysis)
        {
            // For now, return the file as-is
            // In a more advanced implementation, we could:
            // - Remove unused functions/classes
            // - Remove comments and documentation
            // - Simplify complex expressions
            // - Remove debug/logging code
            
            return new RepositoryFile
            {
                Path = file.Path,
                Content = file.Content,
                IsEntryPoint = file.IsEntryPoint,
                IsTestFile = file.IsTestFile,
                IsConfigFile = file.IsConfigFile
            };
        }

        private List<string> ExtractDependencies(RepositoryFile file)
        {
            var dependencies = new List<string>();

            if (!_languagePatterns.ContainsKey(file.Language))
                return dependencies;

            var patterns = _languagePatterns[file.Language];

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(file.Content, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        var dependency = match.Groups[1].Value.Trim();
                        if (!string.IsNullOrEmpty(dependency) && !dependency.StartsWith("System") && !dependency.StartsWith("std"))
                        {
                            dependencies.Add(dependency);
                        }
                    }
                }
            }

            return dependencies.Distinct().ToList();
        }

        private bool IsMainFile(RepositoryFile file)
        {
            var fileName = file.FileName.ToLower();
            var content = file.Content.ToLower();

            return fileName.Contains("main") || 
                   fileName.Contains("program") ||
                   fileName.Contains("app") ||
                   content.Contains("static void main") ||
                   content.Contains("if __name__ == \"__main__\"") ||
                   content.Contains("function main(");
        }

        private bool IsTestFile(RepositoryFile file)
        {
            var path = file.Path.ToLower();
            var fileName = file.FileName.ToLower();

            return path.Contains("test") ||
                   path.Contains("spec") ||
                   fileName.Contains("test") ||
                   fileName.Contains("spec") ||
                   file.Content.Contains("[Test]") ||
                   file.Content.Contains("@Test") ||
                   file.Content.Contains("def test_") ||
                   file.Content.Contains("it(") ||
                   file.Content.Contains("describe(");
        }

        private bool IsConfigFile(RepositoryFile file)
        {
            var fileName = file.FileName.ToLower();
            var extension = file.Extension.ToLower();

            return extension == ".json" || extension == ".xml" || extension == ".yaml" || extension == ".yml" || extension == ".toml" ||
                   fileName.Contains("config") || fileName.Contains("settings") || fileName.Contains("appsettings") ||
                   fileName == "package.json" || fileName == "web.config" || fileName == "app.config";
        }

        private bool IsDocumentationFile(RepositoryFile file)
        {
            var extension = file.Extension.ToLower();
            var path = file.Path.ToLower();

            return extension == ".md" || extension == ".txt" || extension == ".rst" ||
                   path.Contains("doc") || path.Contains("readme") || path.Contains("license");
        }

        private bool IsExampleFile(RepositoryFile file)
        {
            var path = file.Path.ToLower();
            return path.Contains("example") || path.Contains("sample") || path.Contains("demo");
        }

        private bool IsBuildFile(RepositoryFile file)
        {
            var fileName = file.FileName.ToLower();
            return fileName.Contains("makefile") || fileName.Contains("build") || fileName.Contains("webpack") ||
                   fileName.Contains("grunt") || fileName.Contains("gulp") || fileName.Contains("cmake");
        }

        private bool IsReferencedInBugReport(RepositoryFile file, string bugText)
        {
            // Look for class names, function names, etc. in the bug report
            var identifierPattern = @"\b[A-Z][a-zA-Z0-9]*\b";
            var matches = Regex.Matches(file.Content, identifierPattern);

            foreach (Match match in matches)
            {
                if (bugText.Contains(match.Value.ToLower()))
                    return true;
            }

            return false;
        }

        private bool ContainsErrorPatterns(RepositoryFile file, string bugText)
        {
            // Look for error patterns like exception types, error messages
            var errorKeywords = new[] { "exception", "error", "fail", "bug", "issue", "problem", "crash" };
            
            var fileContent = file.Content.ToLower();
            return errorKeywords.Any(keyword => bugText.Contains(keyword) && fileContent.Contains(keyword));
        }

        private Dictionary<string, List<string>> InitializeLanguagePatterns()
        {
            return new Dictionary<string, List<string>>
            {
                ["csharp"] = new List<string>
                {
                    @"using\s+([^;]+);",
                    @"namespace\s+([^\s{]+)",
                    @"class\s+([^\s:{]+)",
                    @"interface\s+([^\s:{]+)"
                },
                ["javascript"] = new List<string>
                {
                    @"import\s+.*?\s+from\s+[""']([^""']+)[""']",
                    @"require\s*\(\s*[""']([^""']+)[""']\s*\)",
                    @"export\s+.*?\s+([^\s{]+)"
                },
                ["typescript"] = new List<string>
                {
                    @"import\s+.*?\s+from\s+[""']([^""']+)[""']",
                    @"import\s+[""']([^""']+)[""']",
                    @"export\s+.*?\s+([^\s{]+)"
                },
                ["python"] = new List<string>
                {
                    @"import\s+([^\s]+)",
                    @"from\s+([^\s]+)\s+import",
                    @"class\s+([^\s:(]+)",
                    @"def\s+([^\s(]+)"
                },
                ["java"] = new List<string>
                {
                    @"import\s+([^;]+);",
                    @"package\s+([^;]+);",
                    @"class\s+([^\s{]+)",
                    @"interface\s+([^\s{]+)"
                }
            };
        }
    }
}