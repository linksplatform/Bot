using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers.Decorators
{
    /// <summary>
    /// Decorator that ensures only organization owners or repository admins can approve team invitations
    /// </summary>
    public class OwnerKeeperApprovalTriggerDecorator : ITrigger<Issue>
    {
        private readonly ITrigger<Issue> _trigger;
        private readonly GitHubStorage _githubStorage;

        public OwnerKeeperApprovalTriggerDecorator(ITrigger<Issue> trigger, GitHubStorage githubStorage)
        {
            _trigger = trigger;
            _githubStorage = githubStorage;
        }

        public async Task<bool> Condition(Issue issue)
        {
            if (!await _trigger.Condition(issue))
                return false;

            try
            {
                var issueAuthorLogin = issue.User.Login;
                
                var organizationMembership = await _githubStorage.Client.Organization.Member.GetOrganizationMembership(_githubStorage.Owner, issueAuthorLogin);
                if (organizationMembership.Role.Value == MembershipRole.Admin)
                {
                    return true;
                }
                
                var repositoryPermission = await _githubStorage.Client.Repository.Collaborator.ReviewPermission(issue.Repository.Id, issueAuthorLogin);
                return repositoryPermission.Permission == "admin" || repositoryPermission.Permission == "maintain";
            }
            catch
            {
                return false;
            }
        }

        public async Task Action(Issue issue)
        {
            await _trigger.Action(issue);
        }
    }
}