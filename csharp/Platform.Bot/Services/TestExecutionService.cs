using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Octokit;
using Storage.Remote.GitHub;

namespace Platform.Bot.Services
{
    /// <summary>
    /// <para>
    /// Service for executing tests in repositories.
    /// </para>
    /// <para></para>
    /// </summary>
    public class TestExecutionService
    {
        private readonly GitHubStorage _gitHubStorage;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="TestExecutionService"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="gitHubStorage">
        /// <para>The GitHub storage instance.</para>
        /// <para></para>
        /// </param>
        public TestExecutionService(GitHubStorage gitHubStorage)
        {
            _gitHubStorage = gitHubStorage;
        }

        /// <summary>
        /// <para>
        /// Determines the test framework and execution command for a repository.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="repository">
        /// <para>The repository to analyze.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A test execution strategy or null if no tests found.</para>
        /// <para></para>
        /// </returns>
        public async Task<TestExecutionStrategy?> DetectTestFramework(Repository repository)
        {
            try
            {
                var branch = await _gitHubStorage.GetBranch(repository.Id, repository.DefaultBranch);
                var tree = await _gitHubStorage.Client.Git.Tree.GetRecursive(repository.Id, branch.Commit.Sha);

                foreach (var item in tree.Tree)
                {
                    if (item.Type == TreeType.Blob)
                    {
                        var strategy = AnalyzeFileForTestFramework(item.Path);
                        if (strategy != null)
                        {
                            return strategy;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error detecting test framework: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// <para>
        /// Analyzes a file path to determine the test framework.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="filePath">
        /// <para>The file path to analyze.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A test execution strategy or null.</para>
        /// <para></para>
        /// </returns>
        private TestExecutionStrategy? AnalyzeFileForTestFramework(string filePath)
        {
            var fileName = Path.GetFileName(filePath).ToLower();
            var extension = Path.GetExtension(filePath).ToLower();
            var directory = Path.GetDirectoryName(filePath)?.ToLower() ?? "";

            // C# Test Frameworks
            if (extension == ".csproj" && (fileName.Contains("test") || fileName.Contains(".tests.")))
            {
                return new TestExecutionStrategy
                {
                    Language = "csharp",
                    Framework = "dotnet",
                    TestCommand = "dotnet test",
                    BuildCommand = "dotnet build"
                };
            }

            // Python Test Frameworks
            if (fileName == "pytest.ini" || fileName == "setup.cfg" || fileName == "tox.ini")
            {
                return new TestExecutionStrategy
                {
                    Language = "python",
                    Framework = "pytest",
                    TestCommand = "pytest",
                    BuildCommand = "python -m pip install -e ."
                };
            }

            if (fileName == "requirements.txt" && directory.Contains("test"))
            {
                return new TestExecutionStrategy
                {
                    Language = "python",
                    Framework = "unittest",
                    TestCommand = "python -m unittest discover",
                    BuildCommand = "python -m pip install -r requirements.txt"
                };
            }

            // JavaScript/TypeScript Test Frameworks
            if (fileName == "package.json")
            {
                return new TestExecutionStrategy
                {
                    Language = "javascript",
                    Framework = "npm",
                    TestCommand = "npm test",
                    BuildCommand = "npm install"
                };
            }

            // Java Test Frameworks
            if (fileName == "pom.xml")
            {
                return new TestExecutionStrategy
                {
                    Language = "java",
                    Framework = "maven",
                    TestCommand = "mvn test",
                    BuildCommand = "mvn compile"
                };
            }

            if (fileName == "build.gradle" || fileName == "build.gradle.kts")
            {
                return new TestExecutionStrategy
                {
                    Language = "java",
                    Framework = "gradle",
                    TestCommand = "gradle test",
                    BuildCommand = "gradle build"
                };
            }

            // Rust Test Framework
            if (fileName == "cargo.toml")
            {
                return new TestExecutionStrategy
                {
                    Language = "rust",
                    Framework = "cargo",
                    TestCommand = "cargo test",
                    BuildCommand = "cargo build"
                };
            }

            // Go Test Framework
            if (extension == ".go" && fileName.Contains("_test"))
            {
                return new TestExecutionStrategy
                {
                    Language = "go",
                    Framework = "go",
                    TestCommand = "go test ./...",
                    BuildCommand = "go build ./..."
                };
            }

            return null;
        }

        /// <summary>
        /// <para>
        /// Generates test execution instructions for a bug fix.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="repository">
        /// <para>The repository containing the bug.</para>
        /// <para></para>
        /// </param>
        /// <param name="testCases">
        /// <para>The test cases that should pass.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>Test execution instructions.</para>
        /// <para></para>
        /// </returns>
        public async Task<string> GenerateTestInstructions(Repository repository, List<string> testCases)
        {
            var strategy = await DetectTestFramework(repository);
            var instructions = "## Test Execution Instructions\n\n";

            if (strategy == null)
            {
                instructions += "⚠️ **No automated test framework detected.** Manual testing required.\n\n";
                instructions += "### Manual Testing Steps:\n";
                instructions += "1. Set up the development environment according to the project README\n";
                instructions += "2. Add the provided test cases to the appropriate test files\n";
                instructions += "3. Run the tests manually to verify the bug exists\n";
                instructions += "4. Apply the fix and re-run tests to verify the solution\n\n";
            }
            else
            {
                instructions += $"🔧 **Detected Test Framework:** {strategy.Framework} ({strategy.Language})\n\n";
                instructions += "### Automated Testing Steps:\n\n";
                
                instructions += "#### 1. Clone and Setup\n";
                instructions += "```bash\n";
                instructions += $"git clone {repository.CloneUrl}\n";
                instructions += $"cd {repository.Name}\n";
                instructions += $"{strategy.BuildCommand}\n";
                instructions += "```\n\n";

                instructions += "#### 2. Add Test Cases\n";
                instructions += "Create or update test files with the provided test cases:\n\n";
                
                foreach (var testCase in testCases)
                {
                    instructions += "```" + strategy.Language + "\n";
                    instructions += testCase + "\n";
                    instructions += "```\n\n";
                }

                instructions += "#### 3. Run Tests (Should Fail)\n";
                instructions += "```bash\n";
                instructions += $"{strategy.TestCommand}\n";
                instructions += "```\n\n";

                instructions += "#### 4. Apply Fix\n";
                instructions += "Modify the identified source files to fix the bug.\n\n";

                instructions += "#### 5. Verify Fix\n";
                instructions += "```bash\n";
                instructions += $"{strategy.TestCommand}\n";
                instructions += "```\n";
                instructions += "All tests should now pass.\n\n";
            }

            instructions += "### Additional Verification\n";
            instructions += "- Run the full test suite to ensure no regressions\n";
            instructions += "- Test edge cases related to the bug\n";
            instructions += "- Update documentation if needed\n";

            return instructions;
        }
    }

    /// <summary>
    /// <para>
    /// Represents a test execution strategy for a specific project.
    /// </para>
    /// <para></para>
    /// </summary>
    public class TestExecutionStrategy
    {
        /// <summary>
        /// <para>
        /// Gets or sets the programming language.
        /// </para>
        /// <para></para>
        /// </summary>
        public string Language { get; set; } = "";

        /// <summary>
        /// <para>
        /// Gets or sets the test framework name.
        /// </para>
        /// <para></para>
        /// </summary>
        public string Framework { get; set; } = "";

        /// <summary>
        /// <para>
        /// Gets or sets the command to run tests.
        /// </para>
        /// <para></para>
        /// </summary>
        public string TestCommand { get; set; } = "";

        /// <summary>
        /// <para>
        /// Gets or sets the command to build the project.
        /// </para>
        /// <para></para>
        /// </summary>
        public string BuildCommand { get; set; } = "";
    }
}