using System;
using Octokit;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace projects
{
    class Program
    {

        public static GitHubClient client { get; set; }
        public static Credentials Credentials { get; set; }
        static bool IsStart = true;

        public static Issue GetIssue()
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
                if (issue.Title.ToLower() == "hello world")
                {
                    return issue;
                }
            }
            throw new Exception("Not Found");
        }
        public static void CreateOrUpdateFile(string owner, string Repository, string branch, string output, string targetFile)
        {
            try
            {
                var existingFile = client.Repository.Content.GetAllContentsByRef(owner, Repository, targetFile, branch);
                var updateChangeSet = client.Repository.Content.UpdateFile(owner, Repository, targetFile,
                  new UpdateFileRequest("Update File", output, existingFile.Result.First().Sha, branch));
            }
            catch (Exception)
            {
                client.Repository.Content.CreateFile(owner, Repository, targetFile, new CreateFileRequest("Creation File", output, branch));
            }
        }
        public static void CreateFiles(Issue issue, string owner)
        {
            var repository = issue.Repository.Name;
            var branch = "main";
            string ProgramCs = "" +
@"using System;

namespace helloworld
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""Hello World!"");
        }
    }
}";

            Task.Run(() => CreateOrUpdateFile(owner, repository, branch, ProgramCs, "program.cs"));
            string ProgramCsproj = "" +
 @"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
     <OutputType>Exe</OutputType>
    <TargetFramework>net5.0</TargetFramework>
   
     </PropertyGroup>
   

   </Project> ";
            Task.Run(() => CreateOrUpdateFile(owner, repository, branch, ProgramCsproj, "HelloWorld.csproj"));
            string dotnetYml = "" +
@"name: .NET

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:

    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v2
    - name: Run
      run: dotnet run";
            Task.Run(() => CreateOrUpdateFile(owner, repository, branch, dotnetYml, ".github/workflows/CD.yml"));

        }
        public static void Start(string owner, string token,string Name)
        {
            
            client = new GitHubClient(new ProductHeaderValue(Name));
            Credentials = new Credentials(token);
            client.Credentials = Credentials;
            Console.WriteLine("Bot has been started. Press any key to stop ");
            while (IsStart == true)
            {
                try
                {
                    Issue issue = GetIssue();
                    CreateFiles(issue,owner);
                    var IssueUpdate = new IssueUpdate()
                    {
                        State = ItemState.Closed,
                        Body = issue.Body,
                    };

                    client.Issue.Update(owner, issue.Repository.Name, issue.Number, IssueUpdate);
                }
                catch (Exception)
                {

                }
            }
        }


        static void Main(string[] args)
        {
            Console.WriteLine("Enter your username");
            string username = Console.ReadLine();
            Console.WriteLine("Enter your token");
            string token = Console.ReadLine();
            Console.WriteLine("Enter name your app");
            string Name = Console.ReadLine();
            Task.Run(() => Start(username, token,Name));
            Console.ReadLine();
        }
    }
}