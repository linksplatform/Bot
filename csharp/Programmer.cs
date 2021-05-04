using csharp;
using Interfaces;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GitHubBot
{
    internal class Programmer
    {
        private static readonly int interval = 1200;

        private GitHubClient client;

        private Credentials credentials;

        private readonly string owner;

        private readonly string token;

        private bool HwDisable = false;

        private readonly string name;

        // private readonly List<ITrigger<Issue>> triggers;
        private readonly List<File> Files;

        private DateTimeOffset lastIssue = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(14));

        public Programmer(string owner, string token, string name, List<File> files)
        {
            this.owner = owner;
            this.token = token;
            this.name = name;
            this.Files = files;
        }
        public Programmer(string owner, string token, string name,bool hwDisavle, List<File> files)
        {
            this.owner = owner;
            this.token = token;
            this.name = name;
            this.HwDisable = true;
            this.Files = files;
        }

        private IReadOnlyList<Issue> GetIssues()
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

        private void ProcessIssues(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var issues = GetIssues();
                foreach(var issue in issues)
                {
                    if (!(issue.Title.ToLower() == "hello world" && HwDisable == true))
                    {
                        foreach (var file in Files)
                        {
                            if (issue.Title == file.Trigger)
                            {
                                CreateOrUpdateFile(issue.Repository.Name, issue.Repository.DefaultBranch, file);
                                CloseIssue(issue);
                            }
                        }
                    }
                }
                Thread.Sleep(interval);
            }
        }

        public void Start(CancellationToken cancellationToken)
        {
            client = new GitHubClient(new ProductHeaderValue(name));
            credentials = new Credentials(token);
            client.Credentials = credentials;
            ProcessIssues(cancellationToken);
        }
    }
}