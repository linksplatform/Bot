using csharp;
using Interfaces;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GitHubBot
{
    class Contents : IFile
    {
        public string Path { get; set; }
        public string Content { get; set; }
    }

    internal class Programmer
    {
        private GitHubClient client;

        private Credentials credentials;

        private static readonly int interval = 1000;

        private readonly string owner;

        private readonly string token;

        private readonly string name;

        private readonly List<ITrigger<Issue>> triggers = new List<ITrigger<Issue>> {  };

        private DateTimeOffset lastIssue = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(14));

        public Programmer(string owner, string token, string name)
        {
            this.owner = owner;
            this.token = token;
            this.name = name;
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

        public void CreateOrUpdateFile(string repository, string branch, string content, string path)
        {
            var repositoryContent = client.Repository.Content;
            try
            {
                var existingFile = repositoryContent.GetAllContentsByRef(owner, repository, path, branch);
                var updateChangeSet = repositoryContent.UpdateFile(owner, repository, path,
                new UpdateFileRequest("Update File", content, existingFile.Result.First().Sha, branch));
            }
            catch (AggregateException ex)//если файл не найден,Octokit кидает именно его
            {
                Console.WriteLine(ex.Message);
                repositoryContent.CreateFile(owner, repository, path, new CreateFileRequest("Creation File", content, branch));
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
            var ProgramCs = new File { Path = "program.cs", Content = CSharpHelloWorld.ProgramCs };
            var dotnetYml = new File { Path = "CD.yml", Content = CSharpHelloWorld.dotnetYml };
            var HelloCsproj = new File { Path = "HelloWorld.csproj", Content = CSharpHelloWorld.ProgramCsproj };
            List<File> files = new List<File> { ProgramCs, dotnetYml, HelloCsproj };
            var Trigger = new HelloWorldTrigger(this,files);
            triggers.Add(Trigger);
            ProcessIssues(cancellationToken);
        }
    }
}