using GitHubBot;
using Interfaces;
using Octokit;
using System.Collections.Generic;

namespace csharp
{
    class Trigger : ITrigger<Issue>
    {
        private readonly Programmer programmer;

        private readonly List<File> files;

        public string KeyWord { get; set; }

        public Trigger(Programmer programmer,List<File> files)
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
            return obj.Title == KeyWord;
        }
    }
}

