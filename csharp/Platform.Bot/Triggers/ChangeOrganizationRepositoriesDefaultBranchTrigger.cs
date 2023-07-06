using System;
using System.Text;
using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Storage.Local;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers;

public class ChangeOrganizationRepositoriesDefaultBranchTrigger : ITrigger<Issue>
{
    private readonly GitHubStorage _githubStorage;

    private readonly FileStorage _linksStorage;

    public ChangeOrganizationRepositoriesDefaultBranchTrigger(GitHubStorage githubStorage, FileStorage linksStorage)
    {
        _githubStorage = githubStorage;
        _linksStorage = linksStorage;
    }
    public async Task<bool> Condition(Issue issue)
    {
        return issue.Title.ToLower().Contains("Change default branch in organization repositories to".ToLower());
    }

    public async Task Action(Issue context)
    {
        var newDefaultBranch = context.Title.Substring("Change default branch in organization repositories to ".Length);
        var repositories = _githubStorage.GetAllRepositories(context.Repository.Owner.Login).Result;
        var sb = new StringBuilder();
        foreach (var repository in repositories)
        {
            if (repository.DefaultBranch == newDefaultBranch)
            {
                continue;
            }
            var oldDefaultBranchSha = _githubStorage.GetBranch(repository.Id, repository.DefaultBranch).Result.Commit.Sha;
            _githubStorage.CreateReference(repository.Id, new NewReference($"refs/heads/{newDefaultBranch}", oldDefaultBranchSha));
            var repositoryUpdateQuery = new RepositoryUpdate() { Name = repository.Name,DefaultBranch = newDefaultBranch };
            _githubStorage.Client.Repository.Edit(repository.Id, repositoryUpdateQuery).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    var message = $"The default branch of {repository.Name} has been changed to {newDefaultBranch}.";
                    Console.WriteLine(message);
                    sb.AppendLine(message);
                }
            });
        }
        var message = $"The default branch of organization repositories has been changed to {newDefaultBranch}.";
        Console.WriteLine(message);
        sb.AppendLine(message);
        _githubStorage.CreateIssueComment(context.Repository.Id, context.Number, sb.ToString()).Wait();
        _githubStorage.CloseIssue(context);
    }
}
