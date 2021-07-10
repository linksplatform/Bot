using Interfaces;
using Octokit;
using Storage.Local;
using Storage.Remote.GitHub;

namespace csharp
{
    class HelloWorldTrigger : ITrigger<Issue>
    {
        private readonly GitHubStorage gitHubAPI;

        private readonly FileStorage fileStorage;

        private readonly string fileSetName;

        public HelloWorldTrigger(GitHubStorage gitHubAPI,FileStorage fileStorage, string fileSetName)
        {
            this.gitHubAPI = gitHubAPI;
            this.fileStorage = fileStorage;
            this.fileSetName = fileSetName;
        }

        public void Action(Issue obj)
        {
            foreach (var file in fileStorage.GetFilesFromSet(fileSetName))
            {
                gitHubAPI.CreateOrUpdateFile(obj.Repository.Name, obj.Repository.DefaultBranch, file);
            }
            gitHubAPI.CloseIssue(obj);
        }

        public bool Condition(Issue obj)
        {
            return obj.Title.ToLower() == "hello world";
        }
    }
}
