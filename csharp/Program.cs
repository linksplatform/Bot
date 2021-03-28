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
        public static GitHubClient client = new GitHubClient(new ProductHeaderValue("MyAmaxingA2134p"));
        public static Credentials Credentials = new Credentials(" a8ad3c1e55cbc534b7672e7743460ade7cf5c011 "); // NOTE: not real token

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
        public static void CreateOrUpdateFile(string owner,string Repository,string branch,string output,string targetFile)
        {
            try
            {
                var existingFile = client.Repository.Content.GetAllContentsByRef(owner, Repository, targetFile, branch);
                var updateChangeSet = client.Repository.Content.UpdateFile(owner, Repository, targetFile,
                  new UpdateFileRequest("Update File", output, existingFile.Result.First().Sha, branch));
            }
            catch(Exception)
            {
               client.Repository.Content.CreateFile(owner, Repository, targetFile, new CreateFileRequest("Creation File", output, branch));
            }
        }
        public static void CreateFiles(Issue issue)
        {
            string owner = "FirstAfterGod2501";
            var repository = issue.Repository.Name;
            var branch = "main";
            string ProgramCs =""+
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

            Task.Run(() => CreateOrUpdateFile(owner, repository, branch,ProgramCs,"program.cs"));
            string ProgramCsproj = "" +
 @"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
     <OutputType>Exe</OutputType>
    <TargetFramework>net5.0</TargetFramework>
   
     </PropertyGroup>
   

   </Project> ";
            Task.Run(() => CreateOrUpdateFile(owner, repository, branch,ProgramCsproj,"HelloWorld.csproj"));
            string dotnetYml =""+
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
            Task.Run(() => CreateOrUpdateFile(owner, repository, branch,dotnetYml, ".github/workflows/CD.yml"));

        }


        static void Main(string[] args)
        {

            client.Credentials = Credentials;
            try
            {
                Issue issue = GetIssue();
                CreateFiles(issue);
                var IssueUpdate = new IssueUpdate()
                {
                    State = ItemState.Closed,
                    Body = issue.Body,
                };
                
                client.Issue.Update("FirstAfterGod2501", issue.Repository.Name, issue.Number, IssueUpdate);
                Console.ReadLine();
            }
            catch (Exception)
            {

            }
        }     
    }
}
