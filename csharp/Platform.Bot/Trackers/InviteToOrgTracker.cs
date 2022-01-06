using Storage.Local;
using Storage.Remote.GitHub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.Bot.Trackers
{
    public class InviteToOrgTracker
    {
        private string OrgName {get;set;}

        private int MinimumInteractionInterval = 1000;

        private FileStorage Storage { get; set; }

        GitHubStorage GitHubStorage { get; set; }

        public InviteToOrgTracker(string orgName,int minimumInteractionInterval,FileStorage storage, GitHubStorage gitHubStorage)
        {
            OrgName = orgName;
            MinimumInteractionInterval = minimumInteractionInterval;
            Storage = storage;
            GitHubStorage = gitHubStorage;
        }

        public void Start(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var link in Storage.GetLinksToInvite())
                {
                    Console.WriteLine(link);
                    GitHubStorage.InviteToOrg(OrgName, link.Replace("https://github.com/", ""));
                }
                Thread.Sleep(MinimumInteractionInterval);
            }
        }
    }
}
