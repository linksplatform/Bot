using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Octokit;
using Storage.Analysis;

namespace Platform.Bot.Tests
{
    public class BugReproductionAnalyzerTests
    {
        private readonly BugReproductionAnalyzer _analyzer;

        public BugReproductionAnalyzerTests()
        {
            _analyzer = new BugReproductionAnalyzer();
        }

        [Fact]
        public async Task AnalyzeBugReproduction_WithMainFile_IdentifiesAsEntryPoint()
        {
            // Arrange
            var files = new List<RepositoryFile>
            {
                new RepositoryFile
                {
                    Path = "Program.cs",
                    Content = "using System;\n\nclass Program\n{\n    static void Main(string[] args)\n    {\n        Console.WriteLine(\"Hello World!\");\n    }\n}",
                    IsEntryPoint = true
                }
            };

            var issue = CreateMockIssue("Test bug", "This is a test bug description");

            // Act
            var result = await _analyzer.AnalyzeBugReproduction(files, issue);

            // Assert
            Assert.Contains("Program.cs", result.EntryPoints);
            Assert.Contains("Program.cs", result.CriticalFiles);
        }

        [Fact]
        public async Task AnalyzeBugReproduction_WithTestFile_IdentifiesAsTestFile()
        {
            // Arrange
            var files = new List<RepositoryFile>
            {
                new RepositoryFile
                {
                    Path = "Tests/BugTest.cs",
                    Content = "[Test]\npublic void TestBug()\n{\n    // Test code\n}",
                    IsTestFile = true
                }
            };

            var issue = CreateMockIssue("Test bug", "This is a test bug description");

            // Act
            var result = await _analyzer.AnalyzeBugReproduction(files, issue);

            // Assert
            Assert.Contains("Tests/BugTest.cs", result.TestFiles);
            Assert.Contains("Tests/BugTest.cs", result.CriticalFiles);
        }

        [Fact]
        public async Task AnalyzeBugReproduction_WithFileReferencedInBugReport_IdentifiesAsCritical()
        {
            // Arrange
            var files = new List<RepositoryFile>
            {
                new RepositoryFile
                {
                    Path = "BuggyClass.cs",
                    Content = "public class BuggyClass\n{\n    public void ProblematicMethod()\n    {\n        throw new Exception(\"Bug!\");\n    }\n}"
                }
            };

            var issue = CreateMockIssue("Bug in BuggyClass", "The BuggyClass.cs file contains a problematic method that throws an exception");

            // Act
            var result = await _analyzer.AnalyzeBugReproduction(files, issue);

            // Assert
            Assert.Contains("BuggyClass.cs", result.CriticalFiles);
            Assert.Contains("BuggyClass.cs", result.FileReasons.Keys);
        }

        [Fact]
        public async Task SimplifyCodebase_RemovesNonCriticalFiles()
        {
            // Arrange
            var originalFiles = new List<RepositoryFile>
            {
                new RepositoryFile { Path = "Program.cs", Content = "static void Main() {}", IsEntryPoint = true },
                new RepositoryFile { Path = "README.md", Content = "# Documentation" },
                new RepositoryFile { Path = "Examples/Sample.cs", Content = "// Example code" }
            };

            var analysisResult = new BugAnalysisResult();
            analysisResult.CriticalFiles.Add("Program.cs");
            analysisResult.RemovedFiles.Add("README.md");
            analysisResult.RemovedFiles.Add("Examples/Sample.cs");

            // Act
            var simplifiedFiles = await _analyzer.SimplifyCodebase(originalFiles, analysisResult);

            // Assert
            Assert.Single(simplifiedFiles);
            Assert.Contains(simplifiedFiles, f => f.Path == "Program.cs");
            Assert.DoesNotContain(simplifiedFiles, f => f.Path == "README.md");
            Assert.DoesNotContain(simplifiedFiles, f => f.Path == "Examples/Sample.cs");
        }

        [Fact]
        public async Task AnalyzeBugReproduction_WithMultipleFiles_CalculatesConfidenceScore()
        {
            // Arrange
            var files = new List<RepositoryFile>
            {
                new RepositoryFile
                {
                    Path = "Program.cs",
                    Content = "static void Main() {}",
                    IsEntryPoint = true
                },
                new RepositoryFile
                {
                    Path = "Tests/UnitTest.cs",
                    Content = "[Test] public void TestMethod() {}",
                    IsTestFile = true
                },
                new RepositoryFile
                {
                    Path = "README.md",
                    Content = "# Documentation"
                }
            };

            var issue = CreateMockIssue("Test issue", "Test description");

            // Act
            var result = await _analyzer.AnalyzeBugReproduction(files, issue);

            // Assert
            Assert.True(result.ConfidenceScore >= 0.0 && result.ConfidenceScore <= 1.0);
            Assert.True(result.ConfidenceScore > 0.8); // Should be high confidence with entry point and test file
        }

        private Issue CreateMockIssue(string title, string body)
        {
            // Create a mock issue for testing
            // Note: In a real implementation, you'd want to use a proper mocking framework
            // For this simple test, we'll create a minimal implementation
            return new Issue(
                url: "https://github.com/test/repo/issues/1",
                htmlUrl: "https://github.com/test/repo/issues/1",
                commentsUrl: "https://github.com/test/repo/issues/1/comments",
                eventsUrl: "https://github.com/test/repo/issues/1/events",
                number: 1,
                state: ItemState.Open,
                title: title,
                body: body,
                user: null,
                labels: new List<Label>().AsReadOnly(),
                assignee: null,
                assignees: new List<User>().AsReadOnly(),
                milestone: null,
                comments: 0,
                pullRequest: null,
                closedAt: null,
                createdAt: System.DateTimeOffset.Now,
                updatedAt: System.DateTimeOffset.Now,
                closedBy: null,
                id: 1,
                nodeId: "node1",
                locked: false,
                repository: CreateMockRepository(),
                reactions: null,
                activeLockReason: null,
                stateReason: null
            );
        }

        private Repository CreateMockRepository()
        {
            return new Repository(
                url: "https://github.com/test/repo",
                htmlUrl: "https://github.com/test/repo",
                cloneUrl: "https://github.com/test/repo.git",
                gitUrl: "git://github.com/test/repo.git",
                sshUrl: "git@github.com:test/repo.git",
                svnUrl: "https://github.com/test/repo",
                mirrorUrl: null,
                id: 1,
                nodeId: "node1",
                name: "repo",
                fullName: "test/repo",
                description: "Test repository",
                homepage: "",
                language: "C#",
                isPrivate: false,
                fork: false,
                forksCount: 0,
                stargazersCount: 0,
                watchersCount: 0,
                size: 100,
                defaultBranch: "main",
                openIssuesCount: 1,
                topics: new List<string>().AsReadOnly(),
                hasIssues: true,
                hasProjects: true,
                hasWiki: true,
                hasPages: false,
                hasDownloads: true,
                archived: false,
                disabled: false,
                pushedAt: System.DateTimeOffset.Now,
                createdAt: System.DateTimeOffset.Now,
                updatedAt: System.DateTimeOffset.Now,
                permissions: null,
                owner: CreateMockUser(),
                parent: null,
                source: null,
                templatesUrl: null,
                subscribersCount: 1,
                networkCount: 1,
                license: null,
                forksUrl: null,
                keysUrl: null,
                collaboratorsUrl: null,
                teamsUrl: null,
                hooksUrl: null,
                issueEventsUrl: null,
                eventsUrl: null,
                assigneesUrl: null,
                branchesUrl: null,
                tagsUrl: null,
                blobsUrl: null,
                gitTagsUrl: null,
                gitRefsUrl: null,
                treesUrl: null,
                statusesUrl: null,
                languagesUrl: null,
                stargazersUrl: null,
                contributorsUrl: null,
                subscribersUrl: null,
                commitsUrl: null,
                gitCommitsUrl: null,
                commentsUrl: null,
                issueCommentUrl: null,
                contentsUrl: null,
                compareUrl: null,
                mergesUrl: null,
                archiveUrl: null,
                downloadsUrl: null,
                issuesUrl: null,
                pullsUrl: null,
                milestonesUrl: null,
                notificationsUrl: null,
                labelsUrl: null,
                releasesUrl: null,
                deploymentsUrl: null,
                allowRebaseMerge: true,
                allowSquashMerge: true,
                allowMergeCommit: true,
                deleteBranchOnMerge: false,
                allowAutoMerge: false,
                allowUpdateBranch: false,
                useSquashPrTitleAsDefault: false,
                squashMergeCommitTitle: null,
                squashMergeCommitMessage: null,
                mergeCommitTitle: null,
                mergeCommitMessage: null,
                visibility: RepositoryVisibility.Public,
                webCommitSignoffRequired: false,
                securityAndAnalysis: null,
                customProperties: null
            );
        }

        private User CreateMockUser()
        {
            return new User(
                avatarUrl: "https://github.com/images/error/octocat_happy.gif",
                bio: "",
                blog: "",
                collaborators: 0,
                company: "",
                createdAt: System.DateTimeOffset.Now,
                diskUsage: 0,
                email: "test@example.com",
                followers: 0,
                following: 0,
                hireable: false,
                htmlUrl: "https://github.com/test",
                totalPrivateRepos: 0,
                id: 1,
                nodeId: "node1",
                location: "",
                login: "test",
                name: "Test User",
                ownedPrivateRepos: 0,
                plan: null,
                privateGists: 0,
                publicGists: 0,
                publicRepos: 1,
                updatedAt: System.DateTimeOffset.Now,
                url: "https://api.github.com/users/test",
                permissions: null,
                siteAdmin: false,
                ldapDistinguishedName: null,
                suspendedAt: null,
                type: AccountType.User,
                facebookId: null,
                gravatar: null,
                twitterUsername: null
            );
        }
    }
}