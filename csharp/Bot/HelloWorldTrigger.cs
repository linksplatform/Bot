using Interfaces;
using Octokit;
using Storage.Local;
using Storage.Remote.GitHub;
using System.Collections.Generic;

namespace csharp
{
    class HelloWorldTrigger : ITrigger<Issue>
    {
        private readonly GitHubStorage gitHubAPI;

        private readonly List<IFile> files;

        public HelloWorldTrigger(GitHubStorage gitHubAPI,FileStorage fileStorage, string fileSetName)
        {
            this.gitHubAPI = gitHubAPI;
            files = fileStorage.GetFilesFromSet(fileSetName);
        }

        public void Action(Issue obj)
        {
            foreach (var file in files)
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
