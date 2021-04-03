using csharp;
using Octokit;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace csharp
{
    class Programmer
    {
        static GitHubClient client { get; set; }
        static Credentials Credentials { get; set; }

        Issue GetIssue()
        {
            var recently = new IssueRequest
            {
                Filter = IssueFilter.All,
                State = ItemStateFilter.All,
                Since = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(14))
            };
            var issues = client.Issue.GetAllForCurrent(recently);
            for (int i = 0; i < issues.Result.Count; i++)
            {
                var issue = new Issue();
                issue = issues.Result[i];
                if ((issue.Title.ToLower() == "hello world") && (issue.State.Value == ItemState.Open))
                {
                    return issue;
                }
            }
            return null;
        }
        void CreateOrUpdateFile(string owner, string Repository, string branch, string output, string targetFile)
        {
            try
            {
                var existingFile = client.Repository.Content.GetAllContentsByRef(owner, Repository, targetFile, branch);
                var updateChangeSet = client.Repository.Content.UpdateFile(owner, Repository, targetFile,
                  new UpdateFileRequest("Update File", output, existingFile.Result.First().Sha, branch));
            }
            catch
            {
                client.Repository.Content.CreateFile(owner, Repository, targetFile, new CreateFileRequest("Creation File", output, branch));
            }
        }
        void CreateFiles(Issue issue, string owner)
        {
            var repository = issue.Repository.Name;
            var branch = "main";

            Task.Run(() => CreateOrUpdateFile(owner, repository, branch, CSharpHelloWorld.ProgramCs, "program.cs"));

            Task.Run(() => CreateOrUpdateFile(owner, repository, branch, CSharpHelloWorld.ProgramCsproj, "HelloWorld.csproj"));

            Task.Run(() => CreateOrUpdateFile(owner, repository, branch, CSharpHelloWorld.dotnetYml, ".github/workflows/CD.yml"));

        }

        void Run(string owner)
        {
            while (true)
            {
                try
                {
                    Issue issue = GetIssue();
                    if (issue != null)
                    {
                        CreateFiles(issue, owner);
                        var IssueUpdate = new IssueUpdate()
                        {
                            State = ItemState.Closed,
                            Body = issue.Body,
                        };

                        client.Issue.Update(owner, issue.Repository.Name, issue.Number, IssueUpdate);
                        Thread.Sleep(1000);
                    }
                }
                catch (AggregateException e)
                {
                    if (e.Message.Contains("Bad credentials"))
                    {
                        Console.WriteLine("Invalid login or password\nPress any key to close");

                        return;
                    }
                }
            }
        }

        public void Start(string owner, string token, string Name)
        {

            client = new GitHubClient(new ProductHeaderValue(Name));
            Credentials = new Credentials(token);
            client.Credentials = Credentials;

            Run(owner);
        }

    }
}
