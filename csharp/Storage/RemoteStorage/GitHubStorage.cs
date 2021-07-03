using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using Interfaces;

namespace Storage.Remote.GitHub
{
    public class GitHubStorage : IRemoteCodeStorage<Issue>
    {
        public readonly GitHubClient Сlient;

        public readonly string Owner;

        public TimeSpan MinimumInteractionInterval { get; }

        private DateTimeOffset lastIssue = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(14));

        public GitHubStorage(string owner, string token, string name)
        {
            Owner = owner;
            Сlient = new GitHubClient(new ProductHeaderValue(name))
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
            var issues = Сlient.Issue.GetAllForCurrent(request).Result;
            lastIssue = issues[issues.Count - 1].CreatedAt;
            return issues;
        }

        public void CreateOrUpdateFile(string repository, string branch, IFile file)
        {
            var repositoryContent = Сlient.Repository.Content;
            try
            {
                var existingFile = repositoryContent.GetAllContentsByRef(Owner, repository, file.Path, branch);
                var updateChangeSet = repositoryContent.UpdateFile(Owner, repository, file.Path,
                new UpdateFileRequest("Update File", file.Content, existingFile.Result[0].Sha, branch));
            }
            catch (AggregateException)
            {
                repositoryContent.CreateFile(Owner, repository, file.Path, new CreateFileRequest("Creation File", file.Content, branch));
            }
        }

        public void CloseIssue(Issue issue)
        {
            IssueUpdate issueUpdate = new()
            {
                State = ItemState.Closed,
                Body = issue.Body,
            };
            Сlient.Issue.Update(Owner, issue.Repository.Name, issue.Number, issueUpdate);
        }
    }
}
