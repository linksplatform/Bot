using Octokit;
using Platform.IO;
using System;
using System.Linq;
using System.Threading;

namespace GitHubBot
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
            try
            {
                return client.Issue.GetAllForCurrent(recently).Result.Single(issue =>
                issue.Title.Equals("hello world", StringComparison.OrdinalIgnoreCase) && issue.State.Value == ItemState.Open);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
        void CreateOrUpdateFile(string owner, string repository, string branch, string output, string targetFile)
        {
            var content = client.Repository.Content;
            try
            {
                var existingFile = content.GetAllContentsByRef(owner, repository, targetFile, branch);
                var updateChangeSet = content.UpdateFile(owner, repository, targetFile,
                new UpdateFileRequest("Update File", output, existingFile.Result.First().Sha, branch));
            }
            catch
            {
                content.CreateFile(owner, repository, targetFile, new CreateFileRequest("Creation File", output, branch));
            }
        }
        void CreateFiles(Issue issue, string owner)
        {
            var repository = issue.Repository.Name;
            var branch = "main";

            CreateOrUpdateFile(owner, repository, branch, CSharpHelloWorld.ProgramCs, "program.cs");

            CreateOrUpdateFile(owner, repository, branch, CSharpHelloWorld.ProgramCsproj, "HelloWorld.csproj");

            CreateOrUpdateFile(owner, repository, branch, CSharpHelloWorld.dotnetYml, ".github/workflows/CD.yml");

        }

        void Run(string owner,ConsoleCancellation cancellation)
        {
                while (cancellation.NotRequested)
                {
                    try
                    {
                        var issue = GetIssue();
                        if (issue != null)
                        {
                            CreateFiles(issue, owner);
                            var issueUpdate = new IssueUpdate()
                            {
                                State = ItemState.Closed,
                                Body = issue.Body,
                            };

                            client.Issue.Update(owner, issue.Repository.Name, issue.Number, issueUpdate);
                            Thread.Sleep(1000);
                        }
                    }
                    catch (AggregateException e)
                    {
                        if (e.Message.Contains("Bad credentials"))
                        {
                            Console.WriteLine("Invalid login or password.");
                            return;
                        }
                    }
                }
        }

        public void Start(string owner, string token, string name, ConsoleCancellation cancellation)
        {
            try
            {
                client = new GitHubClient(new ProductHeaderValue(name));
                Credentials = new Credentials(token);
                client.Credentials = Credentials;

                Run(owner, cancellation);
            }
            catch (FormatException e)
            {
                Console.WriteLine(e.Message);
            }
        }

    }
}
