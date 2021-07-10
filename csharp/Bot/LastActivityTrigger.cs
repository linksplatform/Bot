using Interfaces;
using Octokit;
using Platform.Communication.Protocol.Lino;
using Storage.Remote.GitHub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot
{
    class LastActivityTrigger : ITrigger<Issue>
    {
        private readonly GitHubStorage Storage;

        public LastActivityTrigger(GitHubStorage storage)
        {
            this.Storage = storage;
        }
        
        public bool Condition(Issue obj)
        {
            return obj.Title == "organization last month activity";
        }
        
        public void Action(Issue obj)
        {
            List<string> Names = new();
            var links = (new Platform.Communication.Protocol.Lino.Parser()).Parse(obj.Body);
            StringBuilder sb = new();
            List<Link> ignoredRepos = new() { };
            foreach (var link in links)
            {
                if (link.Values.Count == 3 && string.Equals(link.Values.First().Id, "ignore", StringComparison.OrdinalIgnoreCase) && string.Equals(link.Values.Last().Id.Trim('.'), "repository", StringComparison.OrdinalIgnoreCase))
                {
                    ignoredRepos.Add(link.Values[1].Id);
                }
            }
            foreach (var repos in Storage.Client.Repository.GetAllForOrg("linksplatform").Result)
            {
                if (!ignoredRepos.Contains(repos.Name))
                {
                    foreach (var commit in Storage.GetCommits(repos.Owner.Login, repos.Name))
                    {
                        if (!Names.Contains(commit.Author.Login))
                        {
                            Names.Add(commit.Author.Login);
                        }
                    }
                    foreach (var pullRequest in Storage.GetPullRequests(repos.Owner.Login, repos.Name))
                    {
                        foreach (var a in pullRequest.RequestedReviewers)
                            if (!Names.Contains(a.Login))
                            {
                                Names.Add(a.Login);
                            }
                    }
                    foreach (var isuue in Storage.GetIssues(repos.Owner.Login, repos.Name))
                    {
                        if (!Names.Contains(isuue.User.Login))
                        {
                            Names.Add(isuue.User.Login);
                        }
                    }
                }
            }
            foreach (var a in Names)
            {
                sb.AppendLine(a);
            }
            Console.WriteLine(sb.ToString());
            Storage.Client.Issue.Comment.Create(obj.Repository.Owner.Login, obj.Repository.Name, obj.Number, sb.ToString());
            Storage.Client.Issue.Update(obj.Repository.Owner.Login, obj.Repository.Name,obj.Number, new IssueUpdate { State = ItemState.Closed});
        }
    }
}
