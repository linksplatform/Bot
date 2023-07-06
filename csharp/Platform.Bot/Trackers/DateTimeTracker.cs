using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Interfaces;
using Platform.Timestamps;
using Storage.Remote.GitHub;

namespace Platform.Bot.Trackers;

public class DateTimeTracker : ITracker<DateTime?>
{
    private GitHubStorage _storage { get; }

    private IList<ITrigger<DateTime?>> _triggers { get; }

    public DateTimeTracker(GitHubStorage storage, params ITrigger<DateTime?>[] triggers)
    {
        _storage = storage;
        _triggers = triggers;
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        foreach (var trigger in _triggers)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            if (await trigger.Condition(null))
            {
                await trigger.Action(null);
            }
        }
    }
}
