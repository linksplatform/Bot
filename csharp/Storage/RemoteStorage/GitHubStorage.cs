using Octokit;
using Platform.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Octokit.Internal;
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
        public Octokit.GraphQL.ProductHeaderValue ProductInformation;
        public Octokit.GraphQL.Connection GraphQlClient;
        
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
            var credentials = new Credentials(token);
            Client = new GitHubClient(new ProductHeaderValue(name))
            {
                Credentials = credentials
            };
            MinimumInteractionInterval = new(0, 0, 0, 0, 1200);
            ProductInformation = new Octokit.GraphQL.ProductHeaderValue("LinksPlatform", "1.0.0");
            GraphQlClient = new Octokit.GraphQL.Connection(ProductInformation, token);
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

        /// <summary>
        /// <para>
        /// Creates the or update file using the specified repository.
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
        /// <param name="repository">
        /// <para>The repository.</para>
        /// <para></para>
        /// </param>
        /// <param name="branchName">
        /// <para>The branch.</para>
        /// <para></para>
        /// </param>
        /// <param name="commitMessage">
        /// <para>The commit message.</para>
        /// <para></para>
        /// </param>
        public async Task<RepositoryContentChangeSet> CreateOrUpdateFile(string fileContent, Repository repository, string branchName, string filePath, string commitMessage)
        {
            var repositoryContent = Client.Repository.Content;
            var branch = await Client.Repository.Branch.Get(repository.Id, branchName);
            var tree = await Client.Git.Tree.GetRecursive(repository.Id, branch.Commit.Sha);
            var isFileExists = false;
            var fileToUpdateSha = "";
            foreach (var treeItem in tree.Tree)
            {
                if (treeItem.Path == filePath)
                {
                    isFileExists = true;
                    fileToUpdateSha = treeItem.Sha;
                }
            }
            if (isFileExists)
            {
                var fileToUpdate = repositoryContent.GetAllContentsByRef(repository.Id, filePath, branchName);
                fileToUpdate.Wait();
                return await repositoryContent.UpdateFile(repository.Id, filePath, new UpdateFileRequest(commitMessage, fileContent, fileToUpdateSha));
            }
            else
            {
                return await repositoryContent.CreateFile(repository.Id, filePath, new CreateFileRequest(commitMessage, fileContent, branchName));
            }
        }

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
        
        public async Task<List<int>> GetAuthorIdsOfCommits(long repositoryId, CommitRequest commitRequest)
        {
            var commits = await Client.Repository.Commit.GetAll(repositoryId, commitRequest);
            return commits.Select(commit => commit.Author.Id).ToList();
        }

        public Task<IReadOnlyList<Repository>> GetAllRepositories(string ownerName) => Client.Repository.GetAllForOrg(ownerName);
        
        #region Content

        // public async Task<RepositoryContentChangeSet> CreateOrUpdateFile(string fileContent, string filePath, Repository repository, string branchName, string commitMessage)
        // {
        //     try
        //     {
        //         var fileToUpdateContents = Client.Repository.Content.GetAllContents(repository.Id, filePath).Result;
        //     }
        //     catch (NotFoundException e)
        //     {
        //         return await Client.Repository.Content.UpdateFile(repository.Id, filePath, new UpdateFileRequest(commitMessage, fileContent, fileToUpdateContents.First().Sha, branchName));
        //     }
        // }

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

        #region Workflow

        public void RemoveLanguageSpecificWorkflowIfFolderDoesNotExist(long repositoryId, string branchName, List<string> languages)
        {
            var branch = Client.Repository.Branch.Get(repositoryId, branchName).Result;
            var treeResponse = Client.Git.Tree.GetRecursive(repositoryId, branch.Commit.Sha).Result;
            List<string> languageWithoutFolderList = languages;
            Dictionary<string, string?> languageWorkflowShaDictionary = new ();
            foreach (var treeItem in treeResponse.Tree)
            {
                for (var i = 0; i < languageWithoutFolderList.Count; i++)
                {
                    var languageWithoutFolder = languageWithoutFolderList[i];
                    var hasFolder = treeItem.Path.StartsWith($"{languageWithoutFolder}/");
                    if (hasFolder)
                    {
                        languageWithoutFolderList.Remove(languageWithoutFolder);
                        --i;
                        continue;
                    }
                    var workflowPath = $".github/workflows/{languageWithoutFolder}.yml";
                    if (treeItem.Path == workflowPath)
                    {
                        languageWorkflowShaDictionary[languageWithoutFolder] = treeItem.Sha;
                        continue;
                    }
                }
            }
            foreach (var languageWithoutFolder in languageWithoutFolderList)
            {
                languageWorkflowShaDictionary.TryGetValue(languageWithoutFolder, out var workflowSha);
                if (workflowSha == null)
                {
                    continue;
                }
                var blob = Client.Git.Blob.Get(repositoryId, languageWorkflowShaDictionary[languageWithoutFolder]).Result;
                Client.Repository.Content.DeleteFile(repositoryId, $".github/workflows/{languageWithoutFolder}.yml", new DeleteFileRequest("Remove redundand workflow cause of its folder lack.", blob.Sha)).Wait();
            }
        }

        #endregion

        #region Organization

        #region Member

        public async Task<List<User>> GetAllOrganizationMembers(string organizationName)
        {
            var allMembers = new List<User>();
            var options = new ApiOptions
            {
                // Here you can specify the number of items per page (up to 100).
                // You may want to tweak this to balance between the number of requests and the memory consumption.
                PageSize = 100,
                PageCount = 1,
                StartPage = 1,
            };

            while (true)
            {
                var pageOfUsers = await Client.Organization.Member.GetAll(organizationName, OrganizationMembersRole.All, options);

                if (!pageOfUsers.Any())
                {
                    break;
                }

                allMembers.AddRange(pageOfUsers);
                options.StartPage++;
            }

            return allMembers;
        }


        #endregion

        #endregion
    }
}
