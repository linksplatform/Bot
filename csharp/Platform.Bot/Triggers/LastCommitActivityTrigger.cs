using Interfaces;
using Octokit;
using Platform.Communication.Protocol.Lino;
using Storage.Remote.GitHub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;
    internal class LastCommitActivityTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _githubStorage;

        public LastCommitActivityTrigger(GitHubStorage storage) => _githubStorage = storage;

        public bool Condition(TContext issue) => "last 3 months commit activity" == issue.Title.ToLower();

        public void Action(TContext issue)
        {
            var issueService = _githubStorage.Client.Issue;
            var organizationName = issue.Repository.Owner.Login;
            var allMembers = _githubStorage.GetOrganizationMembers(organizationName);
            var usersAndRepisitoriesTheyCommited = new Dictionary<User, List<Repository>>();
            var allRepositorens = _githubStorage.GetAllRepositories(organizationName);
            var allTasks = new List<Task>();
            foreach (var member in allMembers)
            {
                usersAndRepisitoriesTheyCommited.Add(member, new List<Repository>());
                foreach (var repository in allRepositorens)
                {
                    var memberCommitsToRepositoryTask = _githubStorage.GetCommits(organizationName, repository.Name, DateTime.Now.AddMonths(-3), member.Login);
                    memberCommitsToRepositoryTask.ContinueWith(task =>
                    {
                        if (task.Result.Count != 0)
                        {
                            usersAndRepisitoriesTheyCommited[member].Add(repository);
                        }
                    });
                    allTasks.Add(memberCommitsToRepositoryTask);
                }
            }
            Task.WaitAll(allTasks.ToArray());
            StringBuilder messageSb = new();
            AddTldrMessageToSb(usersAndRepisitoriesTheyCommited, messageSb);
            AddUsersAndRepositoriesTheyCommitedToSb(usersAndRepisitoriesTheyCommited, messageSb);
            var message = messageSb.ToString();
            var comment = issueService.Comment.Create(organizationName, issue.Repository.Name, issue.Number, message);
            comment.Wait();
            Console.WriteLine($"Issue {issue.Title} is processed: {issue.Url}");
            _githubStorage.CloseIssue(issue);
        }

        private void AddTldrMessageToSb(Dictionary<User, List<Repository>> usersAndRepositoriesCommited, StringBuilder sb)
        {
            sb.AppendLine("TLDR:");
            usersAndRepositoriesCommited.Keys.All(user =>
            {
                sb.AppendLine($"({user.Name})[{user.Url}]");
                return true;
            });
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        private void AddUsersAndRepositoriesTheyCommitedToSb(Dictionary<User, List<Repository>> usersAndRepositoriesCommited, StringBuilder sb)
        {
            foreach (var userAndCommitedRepositories in usersAndRepositoriesCommited)
            {
                var user = userAndCommitedRepositories.Key;
                var repositoriesThatUserCommited = userAndCommitedRepositories.Value;
                sb.AppendLine($"**({user.Login})[{user.Url}]**");
                repositoriesThatUserCommited.All(repository => {
                    sb.AppendLine($"- ({repository.Name})[{repository.Url}]");
                    return true;
                });
                sb.AppendLine("---");
            }
        }
    }
}
