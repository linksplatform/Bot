using Interfaces;
using Octokit;
using Platform.Exceptions;
using Platform.IO;
using Storage.Local;
using Storage.Remote.GitHub;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Platform.Bot.Trackers;
using Platform.Bot.Triggers;
using Platform.Bot.Triggers.Decorators;
using Platform.Bot.Services;

namespace Platform.Bot
{
    // public class Options
    // {
    //     [Option("github-user-name", Required = true, HelpText = "User name")]
    //     public string GithubUserName { get; set; }
    //     
    //     [Option("github-api-token", Required = true, HelpText = "Github API Token")]
    //     public string GithubApiToken { get; set; }
    //     
    //     [Option("github-application-name", Required = true, HelpText = "Github Application Name")]
    //     public string GithubApplicationName { get; set; }
    //     
    //     [Option("database-file-path", Required = false, HelpText = "Database file path")]
    //     public string? DatabaseFilePath { get; set; }
    //     
    //     [Option("file-set-name", Required = false, HelpText = "File set name")]
    //     public string? FileSetName { get; set; }
    //     
    //     [Option("minimum-interaction-interval-in-seconds", Required = false, HelpText = "Minimum interaction interval in seconds")]
    //     public int? MinimumInteractionIntervalInSeconds { get; set; }
    // }
    /// <summary>
    /// <para>
    /// Represents the program.
    /// </para>
    /// <para></para>
    /// </summary>
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var githubUserNameOption = new Option<string>(
                name: "--github-user-name",
                description: "User name.");

            var githubApiTokenOption = new Option<string>(
                name: "--github-api-token",
                description: "GitHub API token.");

            var githubApplicationNameOption = new Option<string>(
                name: "--github-application-name",
                description: "Github application name.");

            var databaseFilePathOption = new Option<FileInfo?>(
                name: "--database-file-path",
                description: "The database file path.");

            var fileSetNameOption = new Option<string?>(
                name: "--file-set-name",
                description: "The file set name.");

            var minimumInteractionIntervalOption = new Option<int>(
                name: "--minimum-interaction-interval",
                description: "Minimum interaction interval in seconds.",
                getDefaultValue: () => 60);

            var discordBotTokenOption = new Option<string?>(
                name: "--discord-bot-token",
                description: "Discord bot token for role synchronization.");

            var enableFlowsSyncOption = new Option<bool>(
                name: "--enable-flows-sync",
                description: "Enable flows order sync functionality.",
                getDefaultValue: () => true);

            var rootCommand = new RootCommand("Sample app for System.CommandLine")
            {
                githubUserNameOption,
                githubApiTokenOption,
                githubApplicationNameOption,
                databaseFilePathOption,
                fileSetNameOption,
                minimumInteractionIntervalOption,
                discordBotTokenOption,
                enableFlowsSyncOption
            };

            rootCommand.SetHandler(async (githubUserName, githubApiToken, githubApplicationName, databaseFilePath, fileSetName, minimumInteractionInterval, discordBotToken, enableFlowsSync) => 
            {
                Debug.WriteLine($"Nickname: {githubUserName}");
                Debug.WriteLine($"GitHub API Token: {githubApiToken}");
                Debug.WriteLine($"Application Name: {githubApplicationName}");
                Debug.WriteLine($"Database File Path: {databaseFilePath?.FullName}");
                Debug.WriteLine($"File Set Name: {fileSetName}");
                Debug.WriteLine($"Minimum Interaction Interval: {minimumInteractionInterval} seconds");
                Debug.WriteLine($"Discord Bot Token: {(string.IsNullOrEmpty(discordBotToken) ? "Not provided" : "Provided")}");
                Debug.WriteLine($"Flows Sync Enabled: {enableFlowsSync}");
                
                var dbContext = new FileStorage(databaseFilePath?.FullName ?? new TemporaryFile().Filename);
                Console.WriteLine($"Bot has been started. {Environment.NewLine}Press CTRL+C to close");
                var githubStorage = new GitHubStorage(githubUserName, githubApiToken, githubApplicationName);
                
                var triggers = new List<ITrigger<Issue>>
                {
                    new HelloWorldTrigger(githubStorage, dbContext, fileSetName),
                    new OrganizationLastMonthActivityTrigger(githubStorage),
                    new LastCommitActivityTrigger(githubStorage),
                    new AdminAuthorIssueTriggerDecorator(new ProtectDefaultBranchTrigger(githubStorage), githubStorage),
                    new AdminAuthorIssueTriggerDecorator(new ChangeOrganizationRepositoriesDefaultBranchTrigger(githubStorage, dbContext), githubStorage),
                    new AdminAuthorIssueTriggerDecorator(new ChangeOrganizationPullRequestsBaseBranchTrigger(githubStorage, dbContext), githubStorage)
                };

                if (enableFlowsSync)
                {
                    var discordService = new DiscordRoleSyncService(discordBotToken ?? string.Empty);
                    triggers.Add(new AdminAuthorIssueTriggerDecorator(new FlowsOrderSyncTrigger(githubStorage, dbContext, discordService), githubStorage));
                }

                var issueTracker = new IssueTracker(githubStorage, triggers.ToArray());
                var pullRequenstTracker = new PullRequestTracker(githubStorage, new MergeDependabotBumpsTrigger(githubStorage));
                var timestampTracker = new DateTimeTracker(githubStorage, new CreateAndSaveOrganizationRepositoriesMigrationTrigger(githubStorage, dbContext, Path.Combine(Directory.GetCurrentDirectory(), "/github-migrations")));
                var cancellation = new CancellationTokenSource();
                while (true)
                {
                    try
                    {
                        await issueTracker.Start(cancellation.Token);
                        await pullRequenstTracker.Start(cancellation.Token);
                        // timestampTracker.Start(cancellation.Token);
                        Thread.Sleep(minimumInteractionInterval);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToStringWithAllInnerExceptions());
                    }
                }
            }, 
            githubUserNameOption, githubApiTokenOption, githubApplicationNameOption, databaseFilePathOption, fileSetNameOption, minimumInteractionIntervalOption, discordBotTokenOption, enableFlowsSyncOption);

            return await rootCommand.InvokeAsync(args);
        }
    }
}
