using csharp;
using Database;
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

        private readonly string name;

        private readonly List<ITrigger<Issue>> triggers;

        private readonly DBContext dbContext;

        private DateTimeOffset lastIssue = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(14));

        public Programmer(string owner, string token, string name,DBContext dbContext)
        {
            this.owner = owner;
            this.token = token;
            this.name = name;
            this.dbContext = dbContext;
            triggers = new List<ITrigger<Issue>> { new HelloWorldTrigger(this, CSharpHelloWorld.Files(dbContext)) };
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
                foreach (var trigger in triggers)
                {
                    foreach (var issue in issues)
                    {
                        if (trigger.Condition(issue))
                        {
                            trigger.Action(issue);
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