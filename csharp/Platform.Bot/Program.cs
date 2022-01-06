using Interfaces;
using Octokit;
using Platform.Exceptions;
using Platform.IO;
using Storage.Local;
using Storage.Remote.GitHub;
using System;
using System.Collections.Generic;
using Platform.Bot.Trackers;
using Platform.Bot.Triggers;
using System.Threading.Tasks;

namespace Platform.Bot
{
    /// <summary>
    /// <para>
    /// Represents the program.
    /// </para>
    /// <para></para>
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            using var cancellation = new ConsoleCancellation();
            var argumentIndex = 0;
            var username = ConsoleHelpers.GetOrReadArgument(argumentIndex++, "Username", args);
            var token = ConsoleHelpers.GetOrReadArgument(argumentIndex++, "Token", args);
            var appName = ConsoleHelpers.GetOrReadArgument(argumentIndex++, "App Name", args);
            var databaseFileName = ConsoleHelpers.GetOrReadArgument(argumentIndex++, "Database file name", args);
            var fileSetName = ConsoleHelpers.GetOrReadArgument(argumentIndex++, "File set name ", args);
            var discordToken = ConsoleHelpers.GetOrReadArgument(argumentIndex++, "Token for discord bot", args);
            var OrgName = ConsoleHelpers.GetOrReadArgument(argumentIndex++, "Name of the organization",args);
            var dbContext = new FileStorage(databaseFileName);
            Console.WriteLine($"Bot has been started. {Environment.NewLine}Press CTRL+C to close");
            new LinksPlatformDiscordBot.BotStartup(discordToken, dbContext).StartupAsync();
            try
            {
                var api = new GitHubStorage(username, token, appName);
                Task.Run(() => new InviteToOrgTracker(OrgName, 1000, dbContext, api).Start(cancellation.Token));

                new IssueTracker(
                    new List<ITrigger<Issue>> {
                        new HelloWorldTrigger(api, dbContext, fileSetName),
                        new OrganizationLastMonthActivityTrigger(api),
                        new LastCommitActivityTrigger(api),
                        new ProtectMainBranchTrigger(api),
                    },
                    api
                ).Start(cancellation.Token);
                new PullRequestTracker(new List<ITrigger<PullRequest>> { new MergeDependabotBumpsTrigger(api) }, api).Start(cancellation.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToStringWithAllInnerExceptions());
            }
        }
    }
}
