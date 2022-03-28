using Interfaces;
using Octokit;

namespace Platform.Bot.Triggers.Decorators;

public class AdminAuthorIssueTriggerDecorator : ITrigger<Issue>
{
    public readonly ITrigger<Issue> Trigger;
    public AdminAuthorIssueTriggerDecorator(ITrigger<Issue> trigger)
    {
        Trigger = trigger;
    }

    public virtual bool Condition(Issue issue)
    {
        return issue.User.Permissions.Admin && Trigger.Condition(issue);
    }

    public virtual void Action(Issue issue)
    {
        Trigger.Action(issue);
    }
}