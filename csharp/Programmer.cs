using Octokit;
using Platform.Exceptions;
using Platform.IO;
using System;
using System.Linq;
using System.Threading;

namespace GitHubBot
{
    internal class Programmer
    {
        private GitHubClient client;

        private Credentials credentials;

        private static readonly int interval = 1000;

        private readonly string owner;

        private readonly string token;

        private readonly string name;

        private DateTimeOffset lastIssue = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(14));

        public Programmer(string owner, string token, string name)
        {
            this.owner = owner;
            this.token = token;
            this.name = name;
        }

        private Issue GetIssue()
        {
            IssueRequest request = new IssueRequest
            {
                Filter = IssueFilter.All,
                State = ItemStateFilter.Open,
                Since = lastIssue
            };
            try
            {
                return client.Issue.GetAllForCurrent(request).Result.Single(issue =>
                issue.Title.Equals("hello world", StringComparison.OrdinalIgnoreCase));
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private void CreateOrUpdateFile(string repository, string branch, string content, string path)
        {
            var repositoryContent = client.Repository.Content;
            try
            {
                var existingFile = repositoryContent.GetAllContentsByRef(owner, repository, path, branch);
                var updateChangeSet = repositoryContent.UpdateFile(owner, repository, path,
                new UpdateFileRequest("Update File", content, existingFile.Result.First().Sha, branch));
            }
            catch (NotFoundException)
            {
                repositoryContent.CreateFile(owner, repository, path, new CreateFileRequest("Creation File", content, branch));
            }
        }

        private void CreateFiles(Issue issue)
        {
            string repository = issue.Repository.Name;
            string branch = issue.Repository.DefaultBranch;
            CreateOrUpdateFile(repository, branch, CSharpHelloWorld.ProgramCs, "program.cs");
            CreateOrUpdateFile(repository, branch, CSharpHelloWorld.ProgramCsproj, "HelloWorld.csproj");
            CreateOrUpdateFile(repository, branch, CSharpHelloWorld.dotnetYml, ".github/workflows/CD.yml");
        }

        private void ProcessIssue(Issue issue)
        {
            CreateFiles(issue);
            IssueUpdate issueUpdate = new IssueUpdate()
            {
                State = ItemState.Closed,
                Body = issue.Body,
            };
            client.Issue.Update(owner, issue.Repository.Name, issue.Number, issueUpdate);
        }

        private void Run(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Issue issue = GetIssue();
                if (issue != null)
                {
                    lastIssue = issue.CreatedAt;
                    ProcessIssue(issue);
                }
                Thread.Sleep(interval);
            }
        }

        public void Start(CancellationToken cancellationToken)
        {
            client = new GitHubClient(new ProductHeaderValue(name));
            credentials = new Credentials(token);
            client.Credentials = credentials;
            Run(cancellationToken);
        }
    }
}