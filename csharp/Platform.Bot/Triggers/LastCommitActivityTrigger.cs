using Interfaces;
using Octokit;
using Platform.Communication.Protocol.Lino;
using Storage.Remote.GitHub;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;
    internal class LastCommitActivityTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _githubStorage;

        public LastCommitActivityTrigger(GitHubStorage storage) => _githubStorage = storage;

        public async Task<bool> Condition(TContext issue)
        {
            return "last 3 months commit activity" == issue.Title.ToLower();
        }

        public async Task Action(TContext issue)
        {
            var organizationName = issue.Repository.Owner.Login;

            var allMembers = await _githubStorage.GetAllOrganizationMembers(organizationName);
            var allRepositories = await _githubStorage.GetAllRepositories(organizationName);
            if (!allRepositories.Any())
            {
                return;
            }

            var commitsPerUserInLast3Months = await allRepositories
                .Where(repository => _githubStorage.Client.Repository.Branch.GetAll(repository.Id).Result.Any())
                .Select(repository => _githubStorage.GetCommits(repository.Id, new CommitRequest { Since = DateTime.Now.AddMonths(-3) }).Result)
                .SelectMany(x => x)
                .Where(commit => allMembers.Find(user => user.Id == commit.Author.Id) != null)
                .Aggregate(Task.FromResult(new Dictionary<User, List<GitHubCommit>>()), async (dictionaryTask, commit) =>
                {
                    var dictionary = await dictionaryTask;
                    var member = allMembers.Find(user => user.Id == commit.Author.Id)!;
                    if (dictionary.ContainsKey(member))
                    {
                        dictionary[member].Add(commit);
                    }
                    else
                    {
                        dictionary.Add(member, new List<GitHubCommit> { commit });
                    }
                    return dictionary;
                });
            StringBuilder messageSb = new();
            var ShortSummaryMessage = GetShortSummaryMessage(commitsPerUserInLast3Months.Select(pair => pair.Key).ToList());
            messageSb.Append(ShortSummaryMessage);
            messageSb.AppendLine("---");
            var detailedMessage = await GetDetailedMessage(commitsPerUserInLast3Months);
            messageSb.Append(detailedMessage);
            var message = messageSb.ToString();
            await _githubStorage.CreateIssueComment(issue.Repository.Id, issue.Number, message);
            Console.WriteLine($"Issue {issue.Title} is processed: {issue.HtmlUrl}");
            await _githubStorage.Client.Issue.Update(issue.Repository.Owner.Login, issue.Repository.Name, issue.Number, new IssueUpdate() { State = ItemState.Closed });
        }

        private string GetShortSummaryMessage(List<User> users)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("# Short Summary:");
            users.All(user =>
            {
                stringBuilder.AppendLine($"- [{user.Login}]({user.Url})");
                return true;
            });
            return stringBuilder.ToString();
        }

        private async Task<string> GetDetailedMessage(IDictionary<User, List<GitHubCommit>> commitsPerUser)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var userAndCommitedRepositories in commitsPerUser)
            {
                var user = userAndCommitedRepositories.Key;
                var commits = userAndCommitedRepositories.Value;
                stringBuilder.AppendLine($"## [{user.Login}]({user.Url})");
                commits
                    .ForEach(commit =>
                    {
                        Regex regex = new Regex(@"\n+");
                        var commitMessage = commit.Commit.Message;
                        var commitMesasgeWithoutNewLines = regex.Replace(commitMessage, " ");
                        stringBuilder.AppendLine($"- [{commitMesasgeWithoutNewLines}]({commit.Url})");
                    });
            }
            return stringBuilder.ToString();
        }
    }
}
