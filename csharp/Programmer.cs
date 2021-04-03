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
        private GitHubClient Client;

        private Credentials Credentials;

        private static readonly int delay = 1000;

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
                return Client.Issue.GetAllForCurrent(request).Result.Single(issue =>
                issue.Title.Equals("hello world", StringComparison.OrdinalIgnoreCase));
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private void CreateOrUpdateFile(string repository, string branch, string output, string targetFile)
        {
            var content = Client.Repository.Content;
            try
            {
                var existingFile = content.GetAllContentsByRef(owner, repository, targetFile, branch);
                var updateChangeSet = content.UpdateFile(owner, repository, targetFile,
                new UpdateFileRequest("Update File", output, existingFile.Result.First().Sha, branch));
            }
            catch(Octokit.NotFoundException)
            {
                content.CreateFile(owner, repository, targetFile, new CreateFileRequest("Creation File", output, branch));
            }
        }

        private void CreateFiles(Issue issue)
        {
            string repository = issue.Repository.Name;
            string branch = "main";
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
            Client.Issue.Update(owner, issue.Repository.Name, issue.Number, issueUpdate);
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
                Thread.Sleep(delay);
            }
        }

        public void Start(CancellationToken cancellationToken)
        {
            Client = new GitHubClient(new ProductHeaderValue(name));
            Credentials = new Credentials(token);
            Client.Credentials = Credentials;
            Run(cancellationToken);
        }
    }
}
