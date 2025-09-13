using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Storage.Remote.GitHub;

namespace Platform.Bot.Trackers;

public class CommitTracker : ITracker<GitHubCommit>
{
    private readonly GitHubStorage _storage;
    private readonly string _organizationName;
    private readonly IList<ITrigger<GitHubCommit>> _triggers;

    public CommitTracker(GitHubStorage storage, string organizationName, params ITrigger<GitHubCommit>[] triggers)
    {
        _storage = storage;
        _organizationName = organizationName;
        _triggers = triggers;
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        var repositories = await _storage.GetAllRepositories(_organizationName);
        
        foreach (var repository in repositories)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            
            try
            {
                // Get recent commits from the default branch
                var commitRequest = new CommitRequest 
                { 
                    Sha = repository.DefaultBranch,
                    Since = DateTime.Now.AddHours(-1) // Check commits from last hour
                };
                
                var commits = await _storage.GetCommits(repository.Id, commitRequest);
                
                foreach (var commit in commits)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    
                    foreach (var trigger in _triggers)
                    {
                        if (await trigger.Condition(commit))
                        {
                            await trigger.Action(commit);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing repository {repository.Name}: {ex.Message}");
            }
        }
    }
}