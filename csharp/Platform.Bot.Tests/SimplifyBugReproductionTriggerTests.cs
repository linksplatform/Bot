using System.Threading.Tasks;
using Xunit;
using Moq;
using Octokit;
using Platform.Bot.Triggers;
using Storage.Local;
using Storage.Remote.GitHub;

namespace Platform.Bot.Tests
{
    public class SimplifyBugReproductionTriggerTests
    {
        private readonly Mock<GitHubStorage> _mockGitHubStorage;
        private readonly Mock<FileStorage> _mockFileStorage;
        private readonly SimplifyBugReproductionTrigger _trigger;

        public SimplifyBugReproductionTriggerTests()
        {
            _mockGitHubStorage = new Mock<GitHubStorage>("test", "token", "app");
            _mockFileStorage = new Mock<FileStorage>("test.db");
            _trigger = new SimplifyBugReproductionTrigger(_mockGitHubStorage.Object, _mockFileStorage.Object);
        }

        [Theory]
        [InlineData("Simplify bug reproduction", true)]
        [InlineData("simplify bug reproduction", true)]
        [InlineData("Minimize reproduction case", true)]
        [InlineData("Reduce bug example", true)]
        [InlineData("Hello world", false)]
        [InlineData("Fix the bug", false)]
        public async Task Condition_WithVariousTitles_ReturnsExpectedResult(string title, bool expected)
        {
            // Arrange
            var issue = CreateMockIssue(title, "Test description");

            // Act
            var result = await _trigger.Condition(issue);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Condition_WithMinimizeReproductionTitle_ReturnsTrue()
        {
            // Arrange
            var issue = CreateMockIssue("Minimize reproduction", "Test description");

            // Act
            var result = await _trigger.Condition(issue);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task Condition_WithReduceBugExampleTitle_ReturnsTrue()
        {
            // Arrange
            var issue = CreateMockIssue("Please reduce bug example", "Test description");

            // Act
            var result = await _trigger.Condition(issue);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task Condition_WithUnrelatedTitle_ReturnsFalse()
        {
            // Arrange
            var issue = CreateMockIssue("Fix authentication bug", "This is about authentication");

            // Act
            var result = await _trigger.Condition(issue);

            // Assert
            Assert.False(result);
        }

        private Issue CreateMockIssue(string title, string body)
        {
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
                labels: new System.Collections.Generic.List<Label>().AsReadOnly(),
                assignee: null,
                assignees: new System.Collections.Generic.List<User>().AsReadOnly(),
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
                topics: new System.Collections.Generic.List<string>().AsReadOnly(),
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