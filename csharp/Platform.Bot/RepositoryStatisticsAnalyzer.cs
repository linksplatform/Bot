using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Platform.Bot.Triggers;

namespace Platform.Bot;

/// <summary>
/// Provides statistical analysis and reporting capabilities for extracted repository metadata.
/// </summary>
public class RepositoryStatisticsAnalyzer
{
    private readonly string _backupDirectory;

    public RepositoryStatisticsAnalyzer(string backupDirectory = "repository-metadata")
    {
        _backupDirectory = backupDirectory;
    }

    /// <summary>
    /// Loads the latest repository metadata snapshot.
    /// </summary>
    public async Task<OrganizationMetadata?> LoadLatestSnapshot()
    {
        var currentSnapshotPath = Path.Combine(_backupDirectory, "current-snapshot.json");
        
        if (!File.Exists(currentSnapshotPath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(currentSnapshotPath);
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            return JsonSerializer.Deserialize<OrganizationMetadata>(json, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading snapshot: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Generates comprehensive statistics report for the organization.
    /// </summary>
    public async Task<OrganizationStatistics> GenerateStatistics()
    {
        var metadata = await LoadLatestSnapshot();
        if (metadata == null)
        {
            throw new InvalidOperationException("No metadata available. Run repository extraction first.");
        }

        var stats = new OrganizationStatistics
        {
            OrganizationName = metadata.OrganizationName,
            GeneratedAt = DateTime.UtcNow,
            DataTimestamp = metadata.ExtractionTimestamp,
            TotalRepositories = metadata.Repositories.Count
        };

        var repos = metadata.Repositories;

        // Basic repository statistics
        stats.PublicRepositories = repos.Count(r => !r.IsPrivate);
        stats.PrivateRepositories = repos.Count(r => r.IsPrivate);
        stats.ForkedRepositories = repos.Count(r => r.Fork);
        stats.SourceRepositories = repos.Count(r => !r.Fork);
        stats.ArchivedRepositories = repos.Count(r => r.Archived);
        stats.ActiveRepositories = repos.Count(r => !r.Archived);

        // Language distribution
        stats.LanguageDistribution = repos
            .Where(r => !string.IsNullOrEmpty(r.Language))
            .GroupBy(r => r.Language!)
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count());

        // Activity metrics
        stats.TotalStars = repos.Sum(r => r.StargazersCount);
        stats.TotalForks = repos.Sum(r => r.ForksCount);
        stats.TotalWatchers = repos.Sum(r => r.WatchersCount);
        stats.TotalOpenIssues = repos.Sum(r => r.OpenIssuesCount);
        stats.TotalOpenPullRequests = repos.Sum(r => r.OpenPullRequestsCount);

        // Repository size statistics
        if (repos.Any())
        {
            stats.AverageRepositorySize = repos.Average(r => r.Size);
            stats.LargestRepositorySize = repos.Max(r => r.Size);
            stats.LargestRepositoryName = repos.OrderByDescending(r => r.Size).First().Name;
        }

        // Most active repositories (by recent commits)
        stats.MostActiveRepositories = repos
            .Where(r => !r.Archived)
            .OrderByDescending(r => r.RecentCommitsCount)
            .Take(10)
            .Select(r => new ActiveRepositoryInfo
            {
                Name = r.Name,
                RecentCommitsCount = r.RecentCommitsCount,
                OpenPullRequestsCount = r.OpenPullRequestsCount,
                StargazersCount = r.StargazersCount,
                Language = r.Language
            })
            .ToList();

        // Most popular repositories (by stars)
        stats.MostPopularRepositories = repos
            .OrderByDescending(r => r.StargazersCount)
            .Take(10)
            .Select(r => new PopularRepositoryInfo
            {
                Name = r.Name,
                StargazersCount = r.StargazersCount,
                ForksCount = r.ForksCount,
                WatchersCount = r.WatchersCount,
                Language = r.Language,
                Description = r.Description
            })
            .ToList();

        // Recent activity summary
        var recentlyUpdated = repos
            .Where(r => r.UpdatedAt > DateTime.UtcNow.AddDays(-30))
            .Count();
        
        stats.RecentlyUpdatedRepositories = recentlyUpdated;
        stats.ActivityPercentage = repos.Any() ? (double)recentlyUpdated / repos.Count * 100 : 0;

        // Topics analysis
        var allTopics = repos
            .SelectMany(r => r.Topics)
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .Take(20)
            .ToDictionary(g => g.Key, g => g.Count());
        
        stats.PopularTopics = allTopics;

        return stats;
    }

    /// <summary>
    /// Saves statistics report to file.
    /// </summary>
    public async Task SaveStatisticsReport(OrganizationStatistics stats)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var reportPath = Path.Combine(_backupDirectory, $"statistics-report-{timestamp}.json");
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var json = JsonSerializer.Serialize(stats, options);
        await File.WriteAllTextAsync(reportPath, json);
        
        // Also save as latest report
        var latestReportPath = Path.Combine(_backupDirectory, "latest-statistics.json");
        await File.WriteAllTextAsync(latestReportPath, json);
        
        Console.WriteLine($"Statistics report saved to: {reportPath}");
    }

    /// <summary>
    /// Generates a human-readable summary of the statistics.
    /// </summary>
    public string GenerateTextSummary(OrganizationStatistics stats)
    {
        var summary = $@"
# Repository Statistics Report for {stats.OrganizationName}

**Generated:** {stats.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC
**Data from:** {stats.DataTimestamp:yyyy-MM-dd HH:mm:ss} UTC

## Overview
- **Total Repositories:** {stats.TotalRepositories}
- **Public Repositories:** {stats.PublicRepositories}
- **Private Repositories:** {stats.PrivateRepositories}
- **Forked Repositories:** {stats.ForkedRepositories}
- **Source Repositories:** {stats.SourceRepositories}
- **Archived Repositories:** {stats.ArchivedRepositories}
- **Active Repositories:** {stats.ActiveRepositories}

## Activity Metrics
- **Total Stars:** {stats.TotalStars:N0}
- **Total Forks:** {stats.TotalForks:N0}
- **Total Watchers:** {stats.TotalWatchers:N0}
- **Total Open Issues:** {stats.TotalOpenIssues:N0}
- **Total Open Pull Requests:** {stats.TotalOpenPullRequests:N0}
- **Recently Updated (30 days):** {stats.RecentlyUpdatedRepositories} ({stats.ActivityPercentage:F1}%)

## Repository Size
- **Average Size:** {stats.AverageRepositorySize:N0} KB
- **Largest Repository:** {stats.LargestRepositoryName} ({stats.LargestRepositorySize:N0} KB)

## Top Programming Languages
{string.Join("\n", stats.LanguageDistribution.Take(10).Select(kvp => $"- **{kvp.Key}:** {kvp.Value} repositories"))}

## Most Active Repositories (by recent commits)
{string.Join("\n", stats.MostActiveRepositories.Take(5).Select(r => $"- **{r.Name}** ({r.Language ?? "N/A"}): {r.RecentCommitsCount} recent commits, {r.StargazersCount} stars"))}

## Most Popular Repositories (by stars)
{string.Join("\n", stats.MostPopularRepositories.Take(5).Select(r => $"- **{r.Name}** ({r.Language ?? "N/A"}): {r.StargazersCount} stars, {r.ForksCount} forks"))}

## Popular Topics
{string.Join("\n", stats.PopularTopics.Take(10).Select(kvp => $"- **{kvp.Key}:** {kvp.Value} repositories"))}
";

        return summary;
    }
}

#region Statistics Data Models

public class OrganizationStatistics
{
    public string OrganizationName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime DataTimestamp { get; set; }
    
    // Basic counts
    public int TotalRepositories { get; set; }
    public int PublicRepositories { get; set; }
    public int PrivateRepositories { get; set; }
    public int ForkedRepositories { get; set; }
    public int SourceRepositories { get; set; }
    public int ArchivedRepositories { get; set; }
    public int ActiveRepositories { get; set; }
    
    // Activity metrics
    public int TotalStars { get; set; }
    public int TotalForks { get; set; }
    public int TotalWatchers { get; set; }
    public int TotalOpenIssues { get; set; }
    public int TotalOpenPullRequests { get; set; }
    public int RecentlyUpdatedRepositories { get; set; }
    public double ActivityPercentage { get; set; }
    
    // Size metrics
    public double AverageRepositorySize { get; set; }
    public int LargestRepositorySize { get; set; }
    public string LargestRepositoryName { get; set; } = string.Empty;
    
    // Analysis
    public Dictionary<string, int> LanguageDistribution { get; set; } = new();
    public Dictionary<string, int> PopularTopics { get; set; } = new();
    public List<ActiveRepositoryInfo> MostActiveRepositories { get; set; } = new();
    public List<PopularRepositoryInfo> MostPopularRepositories { get; set; } = new();
}

public class ActiveRepositoryInfo
{
    public string Name { get; set; } = string.Empty;
    public int RecentCommitsCount { get; set; }
    public int OpenPullRequestsCount { get; set; }
    public int StargazersCount { get; set; }
    public string? Language { get; set; }
}

public class PopularRepositoryInfo
{
    public string Name { get; set; } = string.Empty;
    public int StargazersCount { get; set; }
    public int ForksCount { get; set; }
    public int WatchersCount { get; set; }
    public string? Language { get; set; }
    public string? Description { get; set; }
}

#endregion