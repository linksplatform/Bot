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

            var discordTokenOption = new Option<string?>(
                name: "--discord-token",
                description: "Discord bot token (optional).");

            var discordGuildIdOption = new Option<ulong?>(
                name: "--discord-guild-id",
                description: "Discord guild/server ID (optional).");

            var discordChannelIdOption = new Option<ulong?>(
                name: "--discord-channel-id",
                description: "Discord channel ID for invites (optional).");

            var rootCommand = new RootCommand("Sample app for System.CommandLine")
            {
                githubUserNameOption,
                githubApiTokenOption,
                githubApplicationNameOption,
                databaseFilePathOption,
                fileSetNameOption,
                minimumInteractionIntervalOption,
                discordTokenOption,
                discordGuildIdOption,
                discordChannelIdOption
            };

            rootCommand.SetHandler(async (context) => 
            {
                var githubUserName = context.ParseResult.GetValueForOption(githubUserNameOption)!;
                var githubApiToken = context.ParseResult.GetValueForOption(githubApiTokenOption)!;
                var githubApplicationName = context.ParseResult.GetValueForOption(githubApplicationNameOption)!;
                var databaseFilePath = context.ParseResult.GetValueForOption(databaseFilePathOption);
                var fileSetName = context.ParseResult.GetValueForOption(fileSetNameOption);
                var minimumInteractionInterval = context.ParseResult.GetValueForOption(minimumInteractionIntervalOption);
                var discordToken = context.ParseResult.GetValueForOption(discordTokenOption);
                var discordGuildId = context.ParseResult.GetValueForOption(discordGuildIdOption);
                var discordChannelId = context.ParseResult.GetValueForOption(discordChannelIdOption);

                Debug.WriteLine($"Nickname: {githubUserName}");
                Debug.WriteLine($"GitHub API Token: {githubApiToken}");
                Debug.WriteLine($"Application Name: {githubApplicationName}");
                Debug.WriteLine($"Database File Path: {databaseFilePath?.FullName}");
                Debug.WriteLine($"File Set Name: {fileSetName}");
                Debug.WriteLine($"Minimum Interaction Interval: {minimumInteractionInterval} seconds");
                Debug.WriteLine($"Discord Token: {(string.IsNullOrEmpty(discordToken) ? "Not provided" : "Provided")}");
                Debug.WriteLine($"Discord Guild ID: {discordGuildId}");
                Debug.WriteLine($"Discord Channel ID: {discordChannelId}");
                
                var dbContext = new FileStorage(databaseFilePath?.FullName ?? new TemporaryFile().Filename);
                Console.WriteLine($"Bot has been started. {Environment.NewLine}Press CTRL+C to close");
                var githubStorage = new GitHubStorage(githubUserName, githubApiToken, githubApplicationName);
                
                var discordService = !string.IsNullOrEmpty(discordToken) && discordGuildId.HasValue && discordChannelId.HasValue 
                    ? new Platform.Bot.Services.DiscordService(discordToken, discordGuildId.Value, discordChannelId.Value) 
                    : null;
                
                if (discordService != null)
                {
                    await discordService.ConnectAsync();
                }
                
                var issueTracker = new IssueTracker(githubStorage, new HelloWorldTrigger(githubStorage, dbContext, fileSetName ?? "HelloWorldSet"), new OrganizationLastMonthActivityTrigger(githubStorage), new LastCommitActivityTrigger(githubStorage), new AdminAuthorIssueTriggerDecorator(new ProtectDefaultBranchTrigger(githubStorage), githubStorage), new AdminAuthorIssueTriggerDecorator(new ChangeOrganizationRepositoriesDefaultBranchTrigger(githubStorage, dbContext), githubStorage), new AdminAuthorIssueTriggerDecorator(new ChangeOrganizationPullRequestsBaseBranchTrigger(githubStorage, dbContext), githubStorage), new OwnerKeeperApprovalTriggerDecorator(new TeamInvitationTrigger(githubStorage, discordService!), githubStorage));
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
            });

            return await rootCommand.InvokeAsync(args);
        }
    }
}
