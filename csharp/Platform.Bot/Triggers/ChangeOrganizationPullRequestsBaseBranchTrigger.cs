using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Storage.Local;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers;

public class ChangeOrganizationPullRequestsBaseBranchTrigger : ITrigger<Issue>
{
    private readonly GitHubStorage _githubStorage;

    private readonly FileStorage _linksStorage;

    public ChangeOrganizationPullRequestsBaseBranchTrigger(GitHubStorage githubStorage, FileStorage linksStorage)
    {
        _githubStorage = githubStorage;
        _linksStorage = linksStorage;
    }
    public async Task<bool> Condition(Issue issue) => issue.Title.Contains("Change organization pull requests base branch");

    public async Task Action(Issue issue)
    {
        var regexMatch = Regex.Match(issue.Title, @"Change organization pull requests base branch from (?<oldBaseBranch>\S+) to (?<newBaseBranch>\S+)");
        if (regexMatch.Groups.Count != 3)
        {
            _githubStorage.CreateIssueComment(issue.Repository.Id, issue.Number, "Wrong format of the title. Must be: Change organization pull requests base branch from <oldBaseBranch> to <newBaseBranch>");
        }
        var oldBaseBranch = regexMatch.Groups["oldBaseBranch"].Value;
        var newBaseBranch = regexMatch.Groups["newBaseBranch"].Value;
        var organizationRepositories = _githubStorage.GetAllRepositories(issue.Repository.Owner.Login).Result;
        var sb = new StringBuilder();
        foreach (var repository in organizationRepositories)
        {
            var pullRequests = _githubStorage.GetPullRequests(repository.Id).Result;
            foreach (var pullRequest in pullRequests)
            {
                if (pullRequest.Base.Ref != oldBaseBranch)
                {
                    continue;
                }
                var updatePullRequestResponse = _githubStorage.Client.PullRequest.Update(repository.Id, pullRequest.Number, new PullRequestUpdate { Base = newBaseBranch });
                updatePullRequestResponse.Wait();
                var isCompletedSuccessfully = updatePullRequestResponse.IsCompletedSuccessfully;
                if (isCompletedSuccessfully)
                {
                    sb.AppendLine($"✔️ The base branch of pull request {pullRequest.HtmlUrl} is updated from {oldBaseBranch} to {newBaseBranch}.");
                }
                else
                {
                    sb.AppendLine($"❌ The base branch of pull request {pullRequest.HtmlUrl} is failed to be updated from {oldBaseBranch} to {newBaseBranch}. Reason {updatePullRequestResponse.Exception.Message}");
                }
            }
        }
        var issueComment = _githubStorage.CreateIssueComment(issue.Repository.Id, issue.Number, sb.ToString()).Result;
        _githubStorage.CloseIssue(issue);
        Console.WriteLine($"[{issue.Title}] {issueComment.HtmlUrl}");
    }
}
