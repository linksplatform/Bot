using Interfaces;
using Octokit;
using Platform.Communication.Protocol.Lino;
using Storage.Remote.GitHub;
using System;
using System.Collections.Concurrent;
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
            var organizationName = issue.Repository.Owner.Login;
            var allMembers = _githubStorage.GetOrganizationMembers(organizationName);
            var usersAndRepositoriesTheyCommited = new ConcurrentDictionary<User, HashSet<Repository>>();
            allMembers.All(user =>
            {
                usersAndRepositoriesTheyCommited.TryAdd(user, new HashSet<Repository>());
                return true;
            });
            var allRepositories = _githubStorage.GetAllRepositories(organizationName).Result;
            var allTasks = new Queue<Task>();
            foreach (var repository in allRepositories)
            {
                var repositoryCommitsTask = _githubStorage.GetCommits(repository.Id, new CommitRequest{Since = DateTime.Now.AddMonths(-3)});
                repositoryCommitsTask.ContinueWith(task =>
                {
                    task.Result.All(commit =>
                    {
                        allMembers.All(user =>
                        {
                            if (commit.Author?.Id == user.Id)
                            {
                                usersAndRepositoriesTheyCommited[user].Add(repository);
                            }
                            return true;
                        });
                        return true;
                    });
                });
                allTasks.Enqueue(repositoryCommitsTask);
            }
            Task.WaitAll(allTasks.ToArray());
            var activeUsersAndRepositoriesTheyCommited = usersAndRepositoriesTheyCommited.Where(userAndRepositoriesCommited => userAndRepositoriesCommited.Value.Count > 0).ToDictionary(pair => pair.Key, pair => pair.Value);
            StringBuilder messageSb = new();
            AddTldrMessageToSb(activeUsersAndRepositoriesTheyCommited, messageSb);
            AddUsersAndRepositoriesTheyCommitedToSb(activeUsersAndRepositoriesTheyCommited, messageSb);
            var message = messageSb.ToString();
            var comment = _githubStorage.CreateIssueComment(issue.Repository.Id, issue.Number, message);
            comment.Wait();
            Console.WriteLine($"Issue {issue.Title} is processed: {issue.Url}");
            _githubStorage.CloseIssue(issue);
        }

        private void AddTldrMessageToSb(IDictionary<User, HashSet<Repository>> usersAndRepositoriesCommited, StringBuilder sb)
        {
            sb.AppendLine("## TLDR:");
            usersAndRepositoriesCommited.Keys.All(user =>
            {
                sb.AppendLine($"[{user.Login}]({user.Url})");
                return true;
            });
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        private void AddUsersAndRepositoriesTheyCommitedToSb(IDictionary<User, HashSet<Repository>> usersAndRepositoriesCommited, StringBuilder sb)
        {
            foreach (var userAndCommitedRepositories in usersAndRepositoriesCommited)
            {
                var user = userAndCommitedRepositories.Key;
                var repositoriesUserCommited = userAndCommitedRepositories.Value;
                sb.AppendLine($"**[{user.Login}]({user.Url})**");
                repositoriesUserCommited.All(repository => {
                    sb.AppendLine($"- [{repository.Name}]({repository.Url})");
                    return true;
                });
                sb.AppendLine("---");
            }
        }
    }
}
