using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Interfaces;
using Octokit;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;
    
    /// <summary>
    /// <para>
    /// Represents the detect unused packages trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class DetectUnusedPackagesTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="DetectUnusedPackagesTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>A git hub storage.</para>
        /// <para></para>
        /// </param>
        public DetectUnusedPackagesTrigger(GitHubStorage storage)
        {
            _storage = storage;
        }

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
        public async Task<bool> Condition(TContext context) 
        {
            var title = context.Title.ToLower();
            return title.Contains("detect unused packages") || 
                   title.Contains("unused dependencies") ||
                   title.Contains("unused package") ||
                   title.Contains("detect unused") ||
                   title.Contains("find unused packages");
        }

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
            var unusedPackagesReport = await DetectUnusedPackages(context.Repository);
            
            var comment = $"## Unused Packages Detection Report\n\n{unusedPackagesReport}";
            
            await _storage.CreateComment(context, comment);
            _storage.CloseIssue(context);
        }

        private async Task<string> DetectUnusedPackages(Repository repository)
        {
            var report = "### Scanning for unused packages across different project types:\n\n";
            var foundIssues = false;

            try
            {
                // Get repository contents
                var contents = await _storage.GetRepositoryContents(repository);
                
                // Detect C# projects
                var csharpResults = await DetectUnusedCSharpPackages(repository, contents);
                if (!string.IsNullOrEmpty(csharpResults))
                {
                    report += "#### C# Projects\n" + csharpResults + "\n";
                    foundIssues = true;
                }

                // Detect Node.js projects
                var nodeResults = await DetectUnusedNodePackages(repository, contents);
                if (!string.IsNullOrEmpty(nodeResults))
                {
                    report += "#### Node.js Projects\n" + nodeResults + "\n";
                    foundIssues = true;
                }

                // Detect Python projects
                var pythonResults = await DetectUnusedPythonPackages(repository, contents);
                if (!string.IsNullOrEmpty(pythonResults))
                {
                    report += "#### Python Projects\n" + pythonResults + "\n";
                    foundIssues = true;
                }

                // Detect Rust projects
                var rustResults = await DetectUnusedRustPackages(repository, contents);
                if (!string.IsNullOrEmpty(rustResults))
                {
                    report += "#### Rust Projects\n" + rustResults + "\n";
                    foundIssues = true;
                }

                if (!foundIssues)
                {
                    report += "✅ No unused packages detected in any supported project types.\n";
                }
            }
            catch (Exception ex)
            {
                report += $"❌ Error during analysis: {ex.Message}\n";
            }

            return report;
        }

        private async Task<string> DetectUnusedCSharpPackages(Repository repository, IReadOnlyList<RepositoryContent> contents)
        {
            var report = "";
            var csprojFiles = contents.Where(c => c.Name.EndsWith(".csproj")).ToList();

            foreach (var csprojFile in csprojFiles)
            {
                try
                {
                    var content = await _storage.GetFileContent(repository, csprojFile.Path);
                    var xml = XDocument.Parse(content);
                    
                    var packageReferences = xml.Descendants("PackageReference")
                        .Select(pr => pr.Attribute("Include")?.Value)
                        .Where(name => !string.IsNullOrEmpty(name))
                        .ToList();

                    if (packageReferences.Any())
                    {
                        var projectDir = Path.GetDirectoryName(csprojFile.Path);
                        var sourceFiles = contents.Where(c => 
                            c.Path.StartsWith(projectDir) && 
                            (c.Name.EndsWith(".cs") || c.Name.EndsWith(".fs") || c.Name.EndsWith(".vb")))
                            .ToList();

                        var unusedPackages = new List<string>();

                        foreach (var package in packageReferences)
                        {
                            bool isUsed = await IsPackageUsedInCSharpProject(repository, sourceFiles, package);
                            if (!isUsed)
                            {
                                unusedPackages.Add(package);
                            }
                        }

                        if (unusedPackages.Any())
                        {
                            report += $"**{csprojFile.Path}**: Potentially unused packages:\n";
                            foreach (var package in unusedPackages)
                            {
                                report += $"- `{package}`\n";
                            }
                            report += "\n";
                        }
                    }
                }
                catch (Exception ex)
                {
                    report += $"❌ Error analyzing {csprojFile.Path}: {ex.Message}\n";
                }
            }

            return report;
        }

        private async Task<string> DetectUnusedNodePackages(Repository repository, IReadOnlyList<RepositoryContent> contents)
        {
            var report = "";
            var packageJsonFiles = contents.Where(c => c.Name == "package.json").ToList();

            foreach (var packageJsonFile in packageJsonFiles)
            {
                try
                {
                    var content = await _storage.GetFileContent(repository, packageJsonFile.Path);
                    var packageJson = JsonDocument.Parse(content);
                    
                    var dependencies = new List<string>();
                    if (packageJson.RootElement.TryGetProperty("dependencies", out var deps))
                    {
                        dependencies.AddRange(deps.EnumerateObject().Select(prop => prop.Name));
                    }
                    if (packageJson.RootElement.TryGetProperty("devDependencies", out var devDeps))
                    {
                        dependencies.AddRange(devDeps.EnumerateObject().Select(prop => prop.Name));
                    }

                    if (dependencies.Any())
                    {
                        var projectDir = Path.GetDirectoryName(packageJsonFile.Path);
                        var sourceFiles = contents.Where(c => 
                            c.Path.StartsWith(projectDir) && 
                            (c.Name.EndsWith(".js") || c.Name.EndsWith(".ts") || c.Name.EndsWith(".jsx") || c.Name.EndsWith(".tsx")))
                            .ToList();

                        var unusedPackages = new List<string>();

                        foreach (var package in dependencies)
                        {
                            bool isUsed = await IsPackageUsedInNodeProject(repository, sourceFiles, package);
                            if (!isUsed)
                            {
                                unusedPackages.Add(package);
                            }
                        }

                        if (unusedPackages.Any())
                        {
                            report += $"**{packageJsonFile.Path}**: Potentially unused packages:\n";
                            foreach (var package in unusedPackages)
                            {
                                report += $"- `{package}`\n";
                            }
                            report += "\n";
                        }
                    }
                }
                catch (Exception ex)
                {
                    report += $"❌ Error analyzing {packageJsonFile.Path}: {ex.Message}\n";
                }
            }

            return report;
        }

        private async Task<string> DetectUnusedPythonPackages(Repository repository, IReadOnlyList<RepositoryContent> contents)
        {
            var report = "";
            var requirementsFiles = contents.Where(c => c.Name == "requirements.txt" || c.Name == "pyproject.toml").ToList();

            foreach (var requirementsFile in requirementsFiles)
            {
                try
                {
                    var content = await _storage.GetFileContent(repository, requirementsFile.Path);
                    var packages = new List<string>();

                    if (requirementsFile.Name == "requirements.txt")
                    {
                        packages = content.Split('\n')
                            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                            .Select(line => line.Split(new[] { '=', '>', '<', '!', '~' })[0].Trim())
                            .ToList();
                    }

                    if (packages.Any())
                    {
                        var projectDir = Path.GetDirectoryName(requirementsFile.Path);
                        var sourceFiles = contents.Where(c => 
                            c.Path.StartsWith(projectDir) && c.Name.EndsWith(".py"))
                            .ToList();

                        var unusedPackages = new List<string>();

                        foreach (var package in packages)
                        {
                            bool isUsed = await IsPackageUsedInPythonProject(repository, sourceFiles, package);
                            if (!isUsed)
                            {
                                unusedPackages.Add(package);
                            }
                        }

                        if (unusedPackages.Any())
                        {
                            report += $"**{requirementsFile.Path}**: Potentially unused packages:\n";
                            foreach (var package in unusedPackages)
                            {
                                report += $"- `{package}`\n";
                            }
                            report += "\n";
                        }
                    }
                }
                catch (Exception ex)
                {
                    report += $"❌ Error analyzing {requirementsFile.Path}: {ex.Message}\n";
                }
            }

            return report;
        }

        private async Task<string> DetectUnusedRustPackages(Repository repository, IReadOnlyList<RepositoryContent> contents)
        {
            var report = "";
            var cargoFiles = contents.Where(c => c.Name == "Cargo.toml").ToList();

            foreach (var cargoFile in cargoFiles)
            {
                try
                {
                    var content = await _storage.GetFileContent(repository, cargoFile.Path);
                    var packages = new List<string>();

                    // Simple TOML parsing for dependencies section
                    var lines = content.Split('\n');
                    bool inDependencies = false;

                    foreach (var line in lines)
                    {
                        if (line.Trim() == "[dependencies]")
                        {
                            inDependencies = true;
                            continue;
                        }
                        if (line.Trim().StartsWith("[") && line.Trim() != "[dependencies]")
                        {
                            inDependencies = false;
                            continue;
                        }
                        if (inDependencies && line.Contains("="))
                        {
                            var packageName = line.Split('=')[0].Trim();
                            if (!string.IsNullOrEmpty(packageName))
                            {
                                packages.Add(packageName);
                            }
                        }
                    }

                    if (packages.Any())
                    {
                        var projectDir = Path.GetDirectoryName(cargoFile.Path);
                        var sourceFiles = contents.Where(c => 
                            c.Path.StartsWith(projectDir) && c.Name.EndsWith(".rs"))
                            .ToList();

                        var unusedPackages = new List<string>();

                        foreach (var package in packages)
                        {
                            bool isUsed = await IsPackageUsedInRustProject(repository, sourceFiles, package);
                            if (!isUsed)
                            {
                                unusedPackages.Add(package);
                            }
                        }

                        if (unusedPackages.Any())
                        {
                            report += $"**{cargoFile.Path}**: Potentially unused packages:\n";
                            foreach (var package in unusedPackages)
                            {
                                report += $"- `{package}`\n";
                            }
                            report += "\n";
                        }
                    }
                }
                catch (Exception ex)
                {
                    report += $"❌ Error analyzing {cargoFile.Path}: {ex.Message}\n";
                }
            }

            return report;
        }

        private async Task<bool> IsPackageUsedInCSharpProject(Repository repository, IEnumerable<RepositoryContent> sourceFiles, string packageName)
        {
            try
            {
                foreach (var sourceFile in sourceFiles)
                {
                    var content = await _storage.GetFileContent(repository, sourceFile.Path);
                    
                    // Check for using statements or direct references
                    if (content.Contains($"using {packageName}") ||
                        content.Contains(packageName))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // If we can't read a file, assume the package might be used
                return true;
            }

            return false;
        }

        private async Task<bool> IsPackageUsedInNodeProject(Repository repository, IEnumerable<RepositoryContent> sourceFiles, string packageName)
        {
            try
            {
                foreach (var sourceFile in sourceFiles)
                {
                    var content = await _storage.GetFileContent(repository, sourceFile.Path);
                    
                    // Check for require or import statements
                    if (content.Contains($"require('{packageName}')") ||
                        content.Contains($"require(\"{packageName}\")") ||
                        content.Contains($"from '{packageName}'") ||
                        content.Contains($"from \"{packageName}\"") ||
                        content.Contains($"import {packageName}") ||
                        content.Contains($"import * from '{packageName}'") ||
                        content.Contains($"import * from \"{packageName}\""))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // If we can't read a file, assume the package might be used
                return true;
            }

            return false;
        }

        private async Task<bool> IsPackageUsedInPythonProject(Repository repository, IEnumerable<RepositoryContent> sourceFiles, string packageName)
        {
            try
            {
                foreach (var sourceFile in sourceFiles)
                {
                    var content = await _storage.GetFileContent(repository, sourceFile.Path);
                    
                    // Check for import statements
                    if (content.Contains($"import {packageName}") ||
                        content.Contains($"from {packageName}"))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // If we can't read a file, assume the package might be used
                return true;
            }

            return false;
        }

        private async Task<bool> IsPackageUsedInRustProject(Repository repository, IEnumerable<RepositoryContent> sourceFiles, string packageName)
        {
            try
            {
                foreach (var sourceFile in sourceFiles)
                {
                    var content = await _storage.GetFileContent(repository, sourceFile.Path);
                    
                    // Check for use statements or extern crate
                    if (content.Contains($"use {packageName}") ||
                        content.Contains($"extern crate {packageName}"))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // If we can't read a file, assume the package might be used
                return true;
            }

            return false;
        }
    }
}