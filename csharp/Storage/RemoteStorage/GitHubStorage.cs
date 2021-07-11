using Octokit;
using Platform.Exceptions;
using Storage.Local;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Storage.Remote.GitHub
{
    public class GitHubStorage
    {
        public readonly GitHubClient Client;

        public readonly string Owner;

        public TimeSpan MinimumInteractionInterval { get; }

        private DateTimeOffset lastIssue = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(14));

        public GitHubStorage(string owner, string token, string name)
        {
            Owner = owner;
            Client = new GitHubClient(new ProductHeaderValue(name))
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
            var issues = Client.Issue.GetAllForCurrent(request).Result;
            if (issues.Count != 0)
            {
                lastIssue = issues.Max(x => x.CreatedAt);
                return issues;
            }
            return new List<Issue>();
        }

        public IReadOnlyList<GitHubCommit> GetCommits(string owner, string reposiroty)
        {
            DateTime date;
            var now = DateTime.Now;
            if (DateTime.Now.Month != 1)
            {
                date = new DateTime(now.Year, now.Month - 1, now.Day);
            }
            else
            {
                date = new DateTime(now.Year, 12, now.Day - 1);
            }
            return Client.Repository.Commit.GetAll(owner, reposiroty, new CommitRequest() { Since = date }).Result;
        }

        public IReadOnlyList<PullRequest> GetPullRequests(string owner, string reposiroty) => Client.PullRequest.GetAllForRepository(owner, reposiroty).Result;

        public IReadOnlyList<Issue> GetIssues(string owner, string reposiroty)
        {
            DateTime date;
            var now = DateTime.Now;
            if (DateTime.Now.Month != 1)
            {
                date = new DateTime(now.Year, now.Month - 1, now.Day - 1);
            }
            else
            {
                date = new DateTime(now.Year, 12, now.Day - 1);
            }
            return Client.Issue.GetAllForRepository(owner, reposiroty, new RepositoryIssueRequest() { Since = date }).Result;
        }

        public void CreateOrUpdateFile(string repository, string branch, File file)
        {
            var repositoryContent = Client.Repository.Content;
            try
            {
                repositoryContent.UpdateFile(
                    Owner,
                    repository,
                    file.Path,
                    new UpdateFileRequest(
                        "Update file.",
                        file.Content,
                        repositoryContent.GetAllContentsByRef(
                            Owner,
                            repository,
                            file.Path,
                            branch
                        ).Result[0].Sha
                    )
                );
            }
            catch (Exception ex)
            {
                ex.Ignore();
                repositoryContent.CreateFile(Owner, repository, file.Path, new CreateFileRequest("Creation File", file.Content, branch));
            }
        }

        public void CloseIssue(Issue issue)
        {
            IssueUpdate issueUpdate = new()
            {
                State = ItemState.Closed
            };
            Client.Issue.Update(Owner, issue.Repository.Name, issue.Number, issueUpdate);
        }
    }
}
