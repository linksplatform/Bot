using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using Interfaces;

namespace Storage.Remote.GitHub
{
    public class GitHubStorage : IRemoteCodeStorage<Issue>
    {
        public GitHubClient Сlient { get; set; }

        public string owner { get; set; }

        public TimeSpan MinimumInteractionInterval { get; set; }

        public DateTimeOffset lastIssue = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(14));

        public GitHubStorage(string owner, string token, string name)
        {
            this.owner = owner;
            this.Сlient = new GitHubClient(new ProductHeaderValue(name))
            {
                Credentials = new Credentials(token)
            };
            MinimumInteractionInterval = new(0, 0, 0, 0, 1200);
        }

        public IReadOnlyList<Issue> GetIssues()
        {
            IssueRequest request = new()
            {
                Filter = IssueFilter.All,
                State = ItemStateFilter.Open,
                Since = lastIssue
            };
            return Сlient.Issue.GetAllForCurrent(request).Result;
        }

        public void CreateOrUpdateFile(string repository, string branch, IFile file)
        {
            var repositoryContent = Сlient.Repository.Content;
            try
            {
                var existingFile = repositoryContent.GetAllContentsByRef(owner, repository, file.Path, branch);
                var updateChangeSet = repositoryContent.UpdateFile(owner, repository, file.Path,
                new UpdateFileRequest("Update File", file.Content, existingFile.Result[0].Sha, branch));
            }
            catch (AggregateException)
            {
                repositoryContent.CreateFile(owner, repository, file.Path, new CreateFileRequest("Creation File", file.Content, branch));
            }
        }

        public void CloseIssue(Issue issue)
        {
            IssueUpdate issueUpdate = new()
            {
                State = ItemState.Closed,
                Body = issue.Body,
            };
            Сlient.Issue.Update(owner, issue.Repository.Name, issue.Number, issueUpdate);
        }
    }
}
