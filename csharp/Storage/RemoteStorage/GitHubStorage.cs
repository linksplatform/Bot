using Octokit;
using Platform.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Platform.Threading;
using File = Storage.Local.File;

namespace Storage.Remote.GitHub
{
    /// <summary>
    /// <para>
    /// Represents the git hub storage.
    /// </para>
    /// <para></para>
    /// </summary>
    public class GitHubStorage
    {
        /// <summary>
        /// <para>
        /// The client.
        /// </para>
        /// <para></para>
        /// </summary>
        public readonly GitHubClient Client;

        /// <summary>
        /// <para>
        /// The owner.
        /// </para>
        /// <para></para>
        /// </summary>
        public readonly string Owner;

        public const int DependabotId = 49699333;


        /// <summary>
        /// <para>
        /// Gets the minimum interaction interval value.
        /// </para>
        /// <para></para>
        /// </summary>
        public TimeSpan MinimumInteractionInterval { get; }
        private DateTimeOffset lastIssue = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(14));

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="GitHubStorage"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="owner">
        /// <para>A owner.</para>
        /// <para></para>
        /// </param>
        /// <param name="token">
        /// <para>A token.</para>
        /// <para></para>
        /// </param>
        /// <param name="name">
        /// <para>A name.</para>
        /// <para></para>
        /// </param>
        public GitHubStorage(string owner, string token, string name)
        {
            Owner = owner;
            Client = new GitHubClient(new ProductHeaderValue(name))
            {
                Credentials = new Credentials(token)
            };
            MinimumInteractionInterval = new(0, 0, 0, 0, 1200);
        }

        /// <summary>
        /// <para>
        /// Gets the issues.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <returns>
        /// <para>A read only list of issue</para>
        /// <para></para>
        /// </returns>
        public IReadOnlyList<Issue> GetIssues()
        {
            IssueRequest request = new()
            {
                Filter = IssueFilter.All,
                State = ItemStateFilter.Open,
                Since = lastIssue
            };
            var issues = Client.Issue.GetAllForCurrent(request).Result;
            if (issues.Count != 0)
            {
                lastIssue = issues.Max(x => x.CreatedAt);
                return issues;
            }
            return new List<Issue>();
        }

        public Task<IReadOnlyList<GitHubCommit>> GetCommits(long repositoryId, CommitRequest commitRequest) => Client.Repository.Commit.GetAll(repositoryId, commitRequest);

        public IReadOnlyList<User> GetOrganizationMembers(string organization)
        {
            return Client.Organization.Member.GetAll(organization).Result;
        }

        /// <summary>
        /// <para>
        /// Gets the pull requests using the specified owner.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="owner">
        /// <para>The owner.</para>
        /// <para></para>
        /// </param>
        /// <param name="reposiroty">
        /// <para>The repository.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A read only list of pull request</para>
        /// <para></para>
        /// </returns>
        public IReadOnlyList<PullRequest> GetPullRequests(string owner, string reposiroty) => Client.PullRequest.GetAllForRepository(owner, reposiroty).AwaitResult();

        public Task<IReadOnlyList<PullRequest>> GetPullRequests(long repositoryId) => Client.PullRequest.GetAllForRepository(repositoryId);

        public Task<IReadOnlyList<PullRequest>> GetPullRequests(long repositoryId, ApiOptions apiOptions) => Client.PullRequest.GetAllForRepository(repositoryId, apiOptions);

        public PullRequest GetPullRequest(int repositoryId, int number) => Client.PullRequest.Get(repositoryId, number).AwaitResult();

        /// <summary>
        /// <para>
        /// Gets the issues using the specified owner.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="owner">
        /// <para>The owner.</para>
        /// <para></para>
        /// </param>
        /// <param name="reposiroty">
        /// <para>The repository.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A read only list of issue</para>
        /// <para></para>
        /// </returns>
        public IReadOnlyList<Issue> GetIssues(string owner, string reposiroty)
        {
            var date = DateTime.Today.AddMonths(-1);
            return Client.Issue.GetAllForRepository(owner, reposiroty, new RepositoryIssueRequest() { Since = date }).Result;
        }

        // /// <summary>
        // /// <para>
        // /// Creates the or update file using the specified repository.
        // /// </para>
        // /// <para></para>
        // /// </summary>
        // /// <param name="repository">
        // /// <para>The repository.</para>
        // /// <para></para>
        // /// </param>
        // /// <param name="branch">
        // /// <para>The branch.</para>
        // /// <para></para>
        // /// </param>
        // /// <param name="file">
        // /// <para>The file.</para>
        // /// <para></para>
        // /// </param>
        // public void CreateOrUpdateFile(string repository, string branch, File file)
        // {
        //     var repositoryContent = Client.Repository.Content;
        //     try
        //     {
        //         repositoryContent.UpdateFile(
        //             Owner,
        //             repository,
        //             file.Path,
        //             new UpdateFileRequest(
        //                 "Update file.",
        //                 file.Content,
        //                 repositoryContent.GetAllContentsByRef(
        //                     Owner,
        //                     repository,
        //                     file.Path,
        //                     branch
        //                 ).Result[0].Sha
        //             )
        //         );
        //     }
        //     catch (Exception ex)
        //     {
        //         ex.Ignore();
        //         repositoryContent.CreateFile(Owner, repository, file.Path, new CreateFileRequest("Creation File", file.Content, branch));
        //     }
        // }

        /// <summary>
        /// <para>
        /// Closes the issue using the specified issue.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="issue">
        /// <para>The issue.</para>
        /// <para></para>
        /// </param>
        public void CloseIssue(Issue issue)
        {
            IssueUpdate issueUpdate = new()
            {
                State = ItemState.Closed
            };
            Client.Issue.Update(issue.Repository.Owner.Login, issue.Repository.Name, issue.Number, issueUpdate);
        }

        #region Repository

        public Task<IReadOnlyList<Repository>> GetAllRepositories(string ownerName) => Client.Repository.GetAllForOrg(ownerName);
        
        #region Content

        public async Task<RepositoryContentChangeSet> CreateOrUpdateFileInRepository(string fileContent, string filePath, Repository repository, string branchName, string commitMessage)
        {
            var fileToUpdateContents = Client.Repository.Content.GetAllContents(repository.Id, filePath).Result;
            if (fileToUpdateContents == null)
            {
                return await Client.Repository.Content.CreateFile(repository.Id, filePath, new CreateFileRequest(commitMessage, fileContent, branchName));
            }
            else
            {
                return await Client.Repository.Content.UpdateFile(repository.Id, filePath, new UpdateFileRequest(commitMessage, fileContent, fileToUpdateContents.First().Sha, branchName));
            }
        }

        #endregion

        #endregion

        #region Migrations

        public IReadOnlyList<Migration> GetAllMigrations(string organizationName) => Client.Migration.Migrations.GetAll(organizationName).AwaitResult();

        public void CreateMigration(string organizationName, params string[] repositoryNames)
        {
            CreateMigration(organizationName, new ReadOnlyCollection<string>(repositoryNames));
        }

        public Task<Migration?> CreateMigration(string organizationName, IReadOnlyList<string> repositoryNames)
        {
            var startMigrationRequest = new StartMigrationRequest(repositoryNames); ;
            return Client.Migration.Migrations.Start(organizationName, startMigrationRequest);
        }

        public Task SaveMigrationArchive(string organizationName, int migrationId, string filePath) => new Task(() =>
        {
            var migrationArchive = Client.Migration.Migrations.GetArchive(organizationName, migrationId).AwaitResult();
            System.IO.File.WriteAllBytes(filePath, migrationArchive);
        });

        #endregion

        #region Reference

        public Task<Reference> CreateReference(long repositoryId, NewReference reference)
        {
            return Client.Git.Reference.Create(repositoryId, reference);
        }

        #endregion

        #region Issue

        public Task<IssueComment> CreateIssueComment(long repositoryId, int issueNumber, string message)
        {
            return Client.Issue.Comment.Create(repositoryId, issueNumber, message);
        }

        #endregion

        #region Branch

        public Task<Branch> GetBranch(long repositoryId, string branchName)
        {
            return Client.Repository.Branch.Get(repositoryId, branchName);
        }

        #endregion

    }
}
