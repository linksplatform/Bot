using Interfaces;
using Octokit;
using Platform.Exceptions;
using Platform.IO;
using Storage.Local;
using Storage.Remote.GitHub;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Platform.Bot.Trackers;
using Platform.Bot.Triggers;

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
            var fileSetName = ConsoleHelpers.GetOrReadArgument(argumentIndex++, "File set name", args);
            var minimumInteractionIntervalStringInputInSeconds = ConsoleHelpers.GetOrReadArgument(argumentIndex, "Minimum interaction interval in seconds", args);
            var minimumInteractionInterval = TimeSpan.FromSeconds(Int32.Parse(minimumInteractionIntervalStringInputInSeconds));
            var dbContext = new FileStorage(databaseFileName);
            Console.WriteLine($"Bot has been started. {Environment.NewLine}Press CTRL+C to close");
            try
            {
                while (true)
                {
                    var api = new GitHubStorage(username, token, appName);
                    var issueTracker = new IssueTracker(api,
                            new HelloWorldTrigger(api, dbContext, fileSetName),
                            new OrganizationLastMonthActivityTrigger(api),
                            new LastCommitActivityTrigger(api),
                            new ProtectMainBranchTrigger(api));
                    var pullRequenstTracker = new PullRequestTracker(api, new MergeDependabotBumpsTrigger(api));
                    var timestampTracker = new DateTimeTracker(api, new CreateAndSaveOrganizationRepositoriesMigrationTrigger(api, dbContext, Path.Combine(Directory.GetCurrentDirectory(), "/github-migrations")));
                    issueTracker.Start(cancellation.Token);
                    pullRequenstTracker.Start(cancellation.Token);
                    timestampTracker.Start(cancellation.Token);
                    Thread.Sleep(minimumInteractionInterval);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToStringWithAllInnerExceptions());
            }
        }
    }
}
