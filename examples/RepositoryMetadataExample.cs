using System;
using System.IO;
using System.Threading.Tasks;
using Platform.Bot;
using Platform.Bot.Triggers;
using Storage.Local;
using Storage.Remote.GitHub;

namespace Examples;

/// <summary>
/// Example demonstrating how to use the repository metadata extraction functionality.
/// This example shows how to:
/// 1. Extract complete repository metadata from GitHub
/// 2. Generate statistics and reports
/// 3. Use the data for backup and analysis purposes
/// </summary>
public class RepositoryMetadataExample
{
    public static async Task Main(string[] args)
    {
        // GitHub credentials (replace with actual values or use environment variables)
        var githubUserName = Environment.GetEnvironmentVariable("GITHUB_USERNAME") ?? "your-username";
        var githubApiToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? "your-token";
        var githubApplicationName = "Platform.Bot.Example";
        var organizationName = "linksplatform";
        var outputDirectory = "repository-metadata-example";

        try
        {
            Console.WriteLine("Repository Metadata Extraction Example");
            Console.WriteLine("=====================================");
            
            await ExtractRepositoryMetadata(githubUserName, githubApiToken, githubApplicationName, organizationName, outputDirectory);
            await GenerateStatistics(outputDirectory);
            await GenerateTextReport(outputDirectory);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Example: Extract repository metadata from GitHub organization
    /// </summary>
    private static async Task ExtractRepositoryMetadata(
        string githubUserName, 
        string githubApiToken, 
        string githubApplicationName, 
        string organizationName,
        string outputDirectory)
    {
        Console.WriteLine($"\n1. Extracting repository metadata for organization: {organizationName}");
        
        // Initialize GitHub storage
        var githubStorage = new GitHubStorage(githubUserName, githubApiToken, githubApplicationName);
        var fileStorage = new FileStorage("temp.db");
        
        // Create metadata extractor (force immediate extraction with TimeSpan.Zero)
        var extractor = new RepositoryMetadataExtractorTrigger(
            githubStorage, 
            fileStorage, 
            organizationName, 
            outputDirectory,
            TimeSpan.Zero);

        // Extract metadata
        await extractor.Action(null);
        
        Console.WriteLine($"✓ Metadata extraction completed. Data saved to: {outputDirectory}");
    }

    /// <summary>
    /// Example: Generate statistics from extracted metadata
    /// </summary>
    private static async Task GenerateStatistics(string outputDirectory)
    {
        Console.WriteLine($"\n2. Generating statistics from extracted metadata");
        
        var analyzer = new RepositoryStatisticsAnalyzer(outputDirectory);
        var stats = await analyzer.GenerateStatistics();
        await analyzer.SaveStatisticsReport(stats);
        
        Console.WriteLine($"✓ Statistics generated:");
        Console.WriteLine($"   - Total repositories: {stats.TotalRepositories}");
        Console.WriteLine($"   - Public repositories: {stats.PublicRepositories}");
        Console.WriteLine($"   - Private repositories: {stats.PrivateRepositories}");
        Console.WriteLine($"   - Total stars: {stats.TotalStars:N0}");
        Console.WriteLine($"   - Total forks: {stats.TotalForks:N0}");
        Console.WriteLine($"   - Most used language: {stats.LanguageDistribution.FirstOrDefault().Key ?? "N/A"}");
    }

    /// <summary>
    /// Example: Generate human-readable text report
    /// </summary>
    private static async Task GenerateTextReport(string outputDirectory)
    {
        Console.WriteLine($"\n3. Generating text report");
        
        var analyzer = new RepositoryStatisticsAnalyzer(outputDirectory);
        var stats = await analyzer.GenerateStatistics();
        var textReport = analyzer.GenerateTextSummary(stats);
        
        var reportPath = Path.Combine(outputDirectory, "example-report.md");
        await File.WriteAllTextAsync(reportPath, textReport);
        
        Console.WriteLine($"✓ Text report saved to: {reportPath}");
        Console.WriteLine("\n=== REPORT PREVIEW ===");
        Console.WriteLine(textReport.Substring(0, Math.Min(500, textReport.Length)) + "...");
    }
}

/// <summary>
/// Example demonstrating manual usage of the metadata extraction API
/// </summary>
public class ManualMetadataExample
{
    public static async Task ExtractSingleRepository(
        string githubUserName, 
        string githubApiToken, 
        string githubApplicationName,
        string owner,
        string repositoryName)
    {
        var githubStorage = new GitHubStorage(githubUserName, githubApiToken, githubApplicationName);
        
        try
        {
            // Get repository information
            var repositories = await githubStorage.GetAllRepositories(owner);
            var repository = repositories.FirstOrDefault(r => r.Name == repositoryName);
            
            if (repository == null)
            {
                Console.WriteLine($"Repository {repositoryName} not found in organization {owner}");
                return;
            }

            Console.WriteLine($"Repository: {repository.FullName}");
            Console.WriteLine($"Description: {repository.Description ?? "No description"}");
            Console.WriteLine($"Language: {repository.Language ?? "Not specified"}");
            Console.WriteLine($"Stars: {repository.StargazersCount}");
            Console.WriteLine($"Forks: {repository.ForksCount}");
            Console.WriteLine($"Open issues: {repository.OpenIssuesCount}");
            Console.WriteLine($"Created: {repository.CreatedAt:yyyy-MM-dd}");
            Console.WriteLine($"Last update: {repository.UpdatedAt:yyyy-MM-dd}");
            
            // Get recent commits
            var commits = await githubStorage.GetCommits(repository.Id, new CommitRequest 
            { 
                Since = DateTime.UtcNow.AddDays(-7) 
            });
            
            Console.WriteLine($"Recent commits (last 7 days): {commits.Count}");
            
            // Get pull requests
            var pullRequests = await githubStorage.GetPullRequests(repository.Id);
            var openPRs = pullRequests.Count(pr => pr.State == ItemState.Open);
            
            Console.WriteLine($"Open pull requests: {openPRs}");
            Console.WriteLine($"Total pull requests: {pullRequests.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting repository data: {ex.Message}");
        }
    }
}