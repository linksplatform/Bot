using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Platform.Bot.Triggers;
using Storage.Local;
using Storage.Remote.GitHub;
using File = System.IO.File;

namespace Platform.Bot.Commands;

/// <summary>
/// Command line interface for repository metadata extraction and statistics.
/// </summary>
public static class RepositoryMetadataCommand
{
    public static Command CreateCommand()
    {
        var extractCommand = new Command("extract", "Extract repository metadata for the organization")
        {
            CreateGitHubUserNameOption(),
            CreateGitHubApiTokenOption(),
            CreateGitHubApplicationNameOption(),
            CreateOrganizationNameOption(),
            CreateOutputDirectoryOption()
        };

        extractCommand.SetHandler(HandleExtractCommand,
            extractCommand.Options[0] as Option<string>,
            extractCommand.Options[1] as Option<string>,
            extractCommand.Options[2] as Option<string>,
            extractCommand.Options[3] as Option<string>,
            extractCommand.Options[4] as Option<string>);

        var statsCommand = new Command("stats", "Generate statistics from extracted metadata")
        {
            CreateOutputDirectoryOption()
        };

        statsCommand.SetHandler(HandleStatsCommand,
            statsCommand.Options[0] as Option<string>);

        var reportCommand = new Command("report", "Generate a text report from statistics")
        {
            CreateOutputDirectoryOption()
        };

        reportCommand.SetHandler(HandleReportCommand,
            reportCommand.Options[0] as Option<string>);

        var metadataCommand = new Command("metadata", "Repository metadata extraction and analysis commands")
        {
            extractCommand,
            statsCommand,
            reportCommand
        };

        return metadataCommand;
    }

    private static Option<string> CreateGitHubUserNameOption()
    {
        return new Option<string>(
            name: "--github-user-name",
            description: "GitHub user name") { IsRequired = true };
    }

    private static Option<string> CreateGitHubApiTokenOption()
    {
        return new Option<string>(
            name: "--github-api-token",
            description: "GitHub API token") { IsRequired = true };
    }

    private static Option<string> CreateGitHubApplicationNameOption()
    {
        return new Option<string>(
            name: "--github-application-name",
            description: "GitHub application name") { IsRequired = true };
    }

    private static Option<string> CreateOrganizationNameOption()
    {
        return new Option<string>(
            name: "--organization",
            description: "GitHub organization name",
            getDefaultValue: () => "linksplatform");
    }

    private static Option<string> CreateOutputDirectoryOption()
    {
        return new Option<string>(
            name: "--output-dir",
            description: "Output directory for metadata and reports",
            getDefaultValue: () => "repository-metadata");
    }

    private static async Task HandleExtractCommand(
        string githubUserName,
        string githubApiToken,
        string githubApplicationName,
        string organizationName,
        string outputDirectory)
    {
        try
        {
            Console.WriteLine($"Starting repository metadata extraction for organization: {organizationName}");
            Console.WriteLine($"Output directory: {outputDirectory}");

            var githubStorage = new GitHubStorage(githubUserName, githubApiToken, githubApplicationName);
            var fileStorage = new FileStorage("temp-db.db"); // Temporary storage, not used in this context
            
            var extractor = new RepositoryMetadataExtractorTrigger(
                githubStorage, 
                fileStorage, 
                organizationName, 
                outputDirectory,
                TimeSpan.Zero); // Force immediate extraction

            // Force extraction by passing null (condition will return true)
            if (await extractor.Condition(null))
            {
                await extractor.Action(null);
                Console.WriteLine("Repository metadata extraction completed successfully!");
            }
            else
            {
                Console.WriteLine("Extraction not needed at this time.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during extraction: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private static async Task HandleStatsCommand(string outputDirectory)
    {
        try
        {
            Console.WriteLine($"Generating statistics from metadata in: {outputDirectory}");

            var analyzer = new RepositoryStatisticsAnalyzer(outputDirectory);
            var stats = await analyzer.GenerateStatistics();
            await analyzer.SaveStatisticsReport(stats);

            Console.WriteLine("Statistics generation completed successfully!");
            Console.WriteLine($"Total repositories analyzed: {stats.TotalRepositories}");
            Console.WriteLine($"Public repositories: {stats.PublicRepositories}");
            Console.WriteLine($"Private repositories: {stats.PrivateRepositories}");
            Console.WriteLine($"Total stars: {stats.TotalStars:N0}");
            Console.WriteLine($"Total forks: {stats.TotalForks:N0}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during statistics generation: {ex.Message}");
        }
    }

    private static async Task HandleReportCommand(string outputDirectory)
    {
        try
        {
            Console.WriteLine($"Generating text report from statistics in: {outputDirectory}");

            var analyzer = new RepositoryStatisticsAnalyzer(outputDirectory);
            var stats = await analyzer.GenerateStatistics();
            var textReport = analyzer.GenerateTextSummary(stats);

            var reportPath = Path.Combine(outputDirectory, $"report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.md");
            await File.WriteAllTextAsync(reportPath, textReport);

            Console.WriteLine("Text report generation completed successfully!");
            Console.WriteLine($"Report saved to: {reportPath}");
            Console.WriteLine();
            Console.WriteLine("=== REPORT PREVIEW ===");
            Console.WriteLine(textReport);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during report generation: {ex.Message}");
        }
    }
}