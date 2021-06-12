using Interfaces;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Services.GitHubAPI
{
    class GitHubAPI : ICodeStorage<Issue, GitHubClient>
    {
        public GitHubClient client { get; set; }

        public string owner { get; set; }

        private Credentials credentials;

        public DateTimeOffset lastIssue = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(14));

        public GitHubAPI(string owner, string token, string name)
        {
            this.owner = owner;
            this.client = new GitHubClient(new ProductHeaderValue(name));
            this.client.Credentials = new Credentials(token);
        }

        public IReadOnlyList<Issue> GetIssues()
        {
            IssueRequest request = new IssueRequest
            {
                Filter = IssueFilter.All,
                State = ItemStateFilter.Open,
                Since = lastIssue
            };
            return client.Issue.GetAllForCurrent(request).Result;
        }

        public void CreateOrUpdateFile(string repository, string branch, IFile file)
        {
            var repositoryContent = client.Repository.Content;
            try
            {
                var existingFile = repositoryContent.GetAllContentsByRef(owner, repository, file.Path, branch);
                var updateChangeSet = repositoryContent.UpdateFile(owner, repository, file.Path,
                new UpdateFileRequest("Update File", file.Content, existingFile.Result.First().Sha, branch));
            }
            catch (AggregateException)//если файл не найден,Octokit кидает именно его
            {
                repositoryContent.CreateFile(owner, repository, file.Path, new CreateFileRequest("Creation File", file.Content, branch));
            }
        }

        public void CloseIssue(Issue issue)
        {
            IssueUpdate issueUpdate = new IssueUpdate()
            {
                State = ItemState.Closed,
                Body = issue.Body,
            };
            client.Issue.Update(owner, issue.Repository.Name, issue.Number, issueUpdate);
        }
    }
}
