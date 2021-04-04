using GitHubBot;
using Interfaces;
using Octokit;
using System.Collections.Generic;

namespace csharp
{
    class HelloWorldTrigger : ITrigger<Issue>
    {
        private readonly Programmer programmer;

        private readonly List<File> files;

        public HelloWorldTrigger(Programmer programmer,List<File> files)
        {
            this.programmer = programmer;
            this.files = files;
        }

        public void Action(Issue obj)
        {
            foreach(var file in files)
            {
                programmer.CreateOrUpdateFile(obj.Repository.Name, obj.Repository.DefaultBranch,file);
            }
            programmer.CloseIssue(obj);
        }

        public bool Condition(Issue obj)
        {
            return obj.Title.ToLower() == "hello world";
        }
    }
}

