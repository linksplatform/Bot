using Interfaces;
using Octokit;
using Platform.Exceptions;
using Platform.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GitHubBot
{
    class Action : IAction
    {
        public string Trigger { get; set; }
        public List<IContent> Content { get; set; }
    }
    class Contents : IContent
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

        private readonly List<IAction> actions = new List<IAction> {  };

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
                Issue issue = new Issue();
                foreach (var a in actions)
                {
                    issue= client.Issue.GetAllForCurrent(request).Result.Single(issue =>
                    issue.Title.Equals(a.Trigger, StringComparison.OrdinalIgnoreCase));
                }
                return issue;
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
            catch (AggregateException ex)//если файл не найден,Octokit кидает именно его
            {
                Console.WriteLine(ex.Message);
                repositoryContent.CreateFile(owner, repository, path, new CreateFileRequest("Creation File", content, branch));
            }
        }

        private void CreateFiles(Issue issue)
        {
            string repository = issue.Repository.Name;
            string branch = issue.Repository.DefaultBranch;
            var Action = actions.FirstOrDefault(Action => Action.Trigger == issue.Title);
            foreach(var file in Action.Content)
            {
                CreateOrUpdateFile(repository, branch, file.Content, file.Path);
            }
            //CreateOrUpdateFile(repository, branch, CSharpHelloWorld.ProgramCs, "program.cs");
            //CreateOrUpdateFile(repository, branch, CSharpHelloWorld.ProgramCsproj, "HelloWorld.csproj");
            //CreateOrUpdateFile(repository, branch, CSharpHelloWorld.dotnetYml, ".github/workflows/CD.yml");
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
            var Content = new Contents { Content = CSharpHelloWorld.dotnetYml, Path = "da/net/dotda.yml" };
            var Content2 = new Contents { Content = CSharpHelloWorld.ProgramCs, Path = "da/net/program.cs" };
            var Action = new Action { Content = new List<IContent> { Content, Content2 }, Trigger = "okay thats issue" };
            actions.Add(Action);
            Run(cancellationToken);


        }
    }
}