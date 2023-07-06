using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers.Decorators;

public class AdminAuthorIssueTriggerDecorator : ITrigger<Issue>
{
    public readonly ITrigger<Issue> Trigger;
    public readonly GitHubStorage GithubStorage;
    public AdminAuthorIssueTriggerDecorator(ITrigger<Issue> trigger, GitHubStorage githubStorage)
    {
        Trigger = trigger;
        GithubStorage = githubStorage;
    }

    public virtual async Task<bool> Condition(Issue issue)
    {
        var authorPermission = await GithubStorage.Client.Repository.Collaborator.ReviewPermission(issue.Repository.Id, issue.User.Login);
        var isAdmin = authorPermission.Permission == "admin";
        return isAdmin && await Trigger.Condition(issue);
    }

    public virtual async Task Action(Issue issue)
    {
        await Trigger.Action(issue);
    }
}
