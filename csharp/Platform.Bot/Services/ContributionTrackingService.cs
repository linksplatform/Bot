using Octokit;
using Storage.Remote.GitHub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.Bot.Services
{
    public class UserContribution
    {
        public User User { get; set; } = null!;
        public int CommitCount { get; set; }
        public int PullRequestCount { get; set; }
        public int IssueCount { get; set; }
        public int CodeReviewCount { get; set; }
        public double WorkScore { get; set; }
        public int Rank { get; set; }
    }

    public class ContributionTrackingService
    {
        private readonly GitHubStorage _githubStorage;

        public ContributionTrackingService(GitHubStorage githubStorage)
        {
            _githubStorage = githubStorage;
        }

        public async Task<List<UserContribution>> GetOrganizationContributions(string organizationName, DateTime since)
        {
            var allMembers = await _githubStorage.GetAllOrganizationMembers(organizationName);
            var allRepositories = await _githubStorage.GetAllRepositories(organizationName);
            
            var contributions = new Dictionary<int, UserContribution>();

            foreach (var member in allMembers)
            {
                contributions[member.Id] = new UserContribution
                {
                    User = member,
                    CommitCount = 0,
                    PullRequestCount = 0,
                    IssueCount = 0,
                    CodeReviewCount = 0
                };
            }

            foreach (var repository in allRepositories.Where(r => !r.Private))
            {
                await TrackCommitContributions(repository, contributions, since);
                await TrackPullRequestContributions(repository, contributions, since);
                await TrackIssueContributions(repository, contributions, since);
                await TrackCodeReviewContributions(repository, contributions, since);
            }

            var contributionList = contributions.Values.ToList();
            CalculateWorkScores(contributionList);
            AssignRanks(contributionList);

            return contributionList.OrderByDescending(c => c.WorkScore).ToList();
        }

        private async Task TrackCommitContributions(Repository repository, Dictionary<int, UserContribution> contributions, DateTime since)
        {
            try
            {
                var commits = await _githubStorage.GetCommits(repository.Id, new CommitRequest { Since = since });
                foreach (var commit in commits)
                {
                    if (commit.Author != null && contributions.ContainsKey(commit.Author.Id))
                    {
                        contributions[commit.Author.Id].CommitCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error tracking commits for repository {repository.Name}: {ex.Message}");
            }
        }

        private async Task TrackPullRequestContributions(Repository repository, Dictionary<int, UserContribution> contributions, DateTime since)
        {
            try
            {
                var pullRequests = _githubStorage.GetPullRequests(repository.Owner.Login, repository.Name);
                foreach (var pr in pullRequests.Where(pr => pr.CreatedAt >= since))
                {
                    if (pr.User != null && contributions.ContainsKey(pr.User.Id))
                    {
                        contributions[pr.User.Id].PullRequestCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error tracking pull requests for repository {repository.Name}: {ex.Message}");
            }
        }

        private async Task TrackIssueContributions(Repository repository, Dictionary<int, UserContribution> contributions, DateTime since)
        {
            try
            {
                var issues = _githubStorage.GetIssues(repository.Owner.Login, repository.Name);
                foreach (var issue in issues.Where(i => i.CreatedAt >= since))
                {
                    if (issue.User != null && contributions.ContainsKey(issue.User.Id))
                    {
                        contributions[issue.User.Id].IssueCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error tracking issues for repository {repository.Name}: {ex.Message}");
            }
        }

        private async Task TrackCodeReviewContributions(Repository repository, Dictionary<int, UserContribution> contributions, DateTime since)
        {
            try
            {
                var pullRequests = _githubStorage.GetPullRequests(repository.Owner.Login, repository.Name);
                foreach (var pr in pullRequests.Where(pr => pr.CreatedAt >= since))
                {
                    foreach (var reviewer in pr.RequestedReviewers)
                    {
                        if (contributions.ContainsKey(reviewer.Id))
                        {
                            contributions[reviewer.Id].CodeReviewCount++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error tracking code reviews for repository {repository.Name}: {ex.Message}");
            }
        }

        private void CalculateWorkScores(List<UserContribution> contributions)
        {
            const double commitWeight = 1.0;
            const double pullRequestWeight = 3.0;
            const double issueWeight = 1.5;
            const double codeReviewWeight = 2.0;

            foreach (var contribution in contributions)
            {
                contribution.WorkScore = 
                    (contribution.CommitCount * commitWeight) +
                    (contribution.PullRequestCount * pullRequestWeight) +
                    (contribution.IssueCount * issueWeight) +
                    (contribution.CodeReviewCount * codeReviewWeight);
            }
        }

        private void AssignRanks(List<UserContribution> contributions)
        {
            var sortedContributions = contributions.OrderByDescending(c => c.WorkScore).ToList();
            for (int i = 0; i < sortedContributions.Count; i++)
            {
                sortedContributions[i].Rank = i + 1;
            }
        }
    }
}