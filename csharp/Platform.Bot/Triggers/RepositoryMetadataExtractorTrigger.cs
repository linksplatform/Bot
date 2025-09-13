using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Storage.Local;
using Storage.Remote.GitHub;
using File = System.IO.File;

namespace Platform.Bot.Triggers;

/// <summary>
/// Trigger for extracting complete repository data and metadata to reduce API requests
/// and enable backup/statistics functionality.
/// </summary>
public class RepositoryMetadataExtractorTrigger : ITrigger<DateTime?>
{
    private readonly GitHubStorage _githubStorage;
    private readonly FileStorage _storage;
    private readonly string _organizationName;
    private readonly string _backupDirectory;
    private readonly TimeSpan _extractionInterval;

    public RepositoryMetadataExtractorTrigger(
        GitHubStorage githubStorage, 
        FileStorage storage, 
        string organizationName,
        string backupDirectory = "repository-metadata",
        TimeSpan? extractionInterval = null)
    {
        _githubStorage = githubStorage;
        _storage = storage;
        _organizationName = organizationName;
        _backupDirectory = backupDirectory;
        _extractionInterval = extractionInterval ?? TimeSpan.FromHours(6); // Default: extract every 6 hours
        
        // Ensure backup directory exists
        Directory.CreateDirectory(_backupDirectory);
    }

    public async Task<bool> Condition(DateTime? dateTime)
    {
        var lastExtractionFile = Path.Combine(_backupDirectory, "last-extraction.json");
        
        if (!File.Exists(lastExtractionFile))
        {
            return true; // First time extraction
        }
        
        try
        {
            var lastExtractionJson = await File.ReadAllTextAsync(lastExtractionFile);
            var lastExtraction = JsonSerializer.Deserialize<DateTime>(lastExtractionJson);
            var timeSinceLastExtraction = DateTime.UtcNow - lastExtraction;
            
            return timeSinceLastExtraction >= _extractionInterval;
        }
        catch
        {
            return true; // If we can't read the file, trigger extraction
        }
    }

    public async Task Action(DateTime? dateTime)
    {
        try
        {
            Console.WriteLine($"Starting repository metadata extraction for organization: {_organizationName}");
            
            var repositories = await _githubStorage.GetAllRepositories(_organizationName);
            var extractedData = new OrganizationMetadata
            {
                OrganizationName = _organizationName,
                ExtractionTimestamp = DateTime.UtcNow,
                Repositories = new List<RepositoryMetadata>()
            };

            foreach (var repo in repositories)
            {
                Console.WriteLine($"Extracting metadata for repository: {repo.Name}");
                
                var repoMetadata = await ExtractRepositoryMetadata(repo);
                extractedData.Repositories.Add(repoMetadata);
            }

            // Save the extracted data
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var backupFilePath = Path.Combine(_backupDirectory, $"metadata-{timestamp}.json");
            
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var json = JsonSerializer.Serialize(extractedData, jsonOptions);
            await File.WriteAllTextAsync(backupFilePath, json);
            
            // Update last extraction timestamp
            var lastExtractionFile = Path.Combine(_backupDirectory, "last-extraction.json");
            var lastExtractionJson = JsonSerializer.Serialize(DateTime.UtcNow, jsonOptions);
            await File.WriteAllTextAsync(lastExtractionFile, lastExtractionJson);
            
            // Also save latest as current snapshot
            var currentSnapshotPath = Path.Combine(_backupDirectory, "current-snapshot.json");
            await File.WriteAllTextAsync(currentSnapshotPath, json);
            
            Console.WriteLine($"Repository metadata extraction completed. Data saved to: {backupFilePath}");
            Console.WriteLine($"Total repositories processed: {extractedData.Repositories.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during repository metadata extraction: {ex.Message}");
        }
    }

    private async Task<RepositoryMetadata> ExtractRepositoryMetadata(Repository repository)
    {
        var metadata = new RepositoryMetadata
        {
            Id = repository.Id,
            Name = repository.Name,
            FullName = repository.FullName,
            Description = repository.Description,
            IsPrivate = repository.Private,
            Fork = repository.Fork,
            StargazersCount = repository.StargazersCount,
            WatchersCount = repository.SubscribersCount,
            ForksCount = repository.ForksCount,
            OpenIssuesCount = repository.OpenIssuesCount,
            Size = (int)repository.Size,
            DefaultBranch = repository.DefaultBranch,
            Language = repository.Language,
            CreatedAt = repository.CreatedAt,
            UpdatedAt = repository.UpdatedAt,
            PushedAt = repository.PushedAt,
            GitUrl = repository.GitUrl,
            SshUrl = repository.SshUrl,
            CloneUrl = repository.CloneUrl,
            Homepage = repository.Homepage,
            Topics = repository.Topics?.ToList() ?? new List<string>(),
            HasIssues = repository.HasIssues,
            HasProjects = false, // HasProjects property not available in this Octokit version
            HasWiki = repository.HasWiki,
            HasPages = repository.HasPages,
            HasDownloads = repository.HasDownloads,
            Archived = repository.Archived,
            Disabled = false, // Disabled property not available in this Octokit version
            Visibility = repository.Visibility?.ToString(),
            Permissions = new RepositoryPermissions
            {
                Admin = repository.Permissions?.Admin ?? false,
                Push = repository.Permissions?.Push ?? false,
                Pull = repository.Permissions?.Pull ?? false
            }
        };

        try
        {
            // Get recent commits
            var commitRequest = new CommitRequest
            {
                Since = DateTime.UtcNow.AddDays(-30) // Last 30 days
            };
            
            var commits = await _githubStorage.GetCommits(repository.Id, commitRequest);
            metadata.RecentCommitsCount = commits.Count;
            metadata.RecentCommits = commits.Take(10).Select(c => new CommitInfo
            {
                Sha = c.Sha,
                Message = c.Commit.Message,
                AuthorName = c.Commit.Author.Name,
                AuthorEmail = c.Commit.Author.Email,
                Date = c.Commit.Author.Date,
                Url = c.HtmlUrl
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not extract commits for {repository.Name}: {ex.Message}");
            metadata.RecentCommitsCount = 0;
            metadata.RecentCommits = new List<CommitInfo>();
        }

        try
        {
            // Get pull requests
            var pullRequests = await _githubStorage.GetPullRequests(repository.Id);
            metadata.OpenPullRequestsCount = pullRequests.Count(pr => pr.State == ItemState.Open);
            metadata.TotalPullRequestsCount = pullRequests.Count;
            
            metadata.RecentPullRequests = pullRequests.Take(5).Select(pr => new PullRequestInfo
            {
                Number = pr.Number,
                Title = pr.Title,
                State = pr.State.ToString(),
                CreatedAt = pr.CreatedAt,
                UpdatedAt = pr.UpdatedAt,
                AuthorLogin = pr.User.Login,
                Url = pr.HtmlUrl
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not extract pull requests for {repository.Name}: {ex.Message}");
            metadata.OpenPullRequestsCount = 0;
            metadata.TotalPullRequestsCount = 0;
            metadata.RecentPullRequests = new List<PullRequestInfo>();
        }

        try
        {
            // Get issues
            var issues = _githubStorage.GetIssues(_organizationName, repository.Name);
            metadata.RecentIssues = issues.Take(5).Select(issue => new IssueInfo
            {
                Number = issue.Number,
                Title = issue.Title,
                State = issue.State.ToString(),
                CreatedAt = issue.CreatedAt,
                UpdatedAt = issue.UpdatedAt,
                AuthorLogin = issue.User.Login,
                Labels = issue.Labels?.Select(l => l.Name).ToList() ?? new List<string>(),
                Url = issue.HtmlUrl
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not extract issues for {repository.Name}: {ex.Message}");
            metadata.RecentIssues = new List<IssueInfo>();
        }

        return metadata;
    }
}

#region Data Models

public class OrganizationMetadata
{
    public string OrganizationName { get; set; } = string.Empty;
    public DateTime ExtractionTimestamp { get; set; }
    public List<RepositoryMetadata> Repositories { get; set; } = new();
}

public class RepositoryMetadata
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPrivate { get; set; }
    public bool Fork { get; set; }
    public int StargazersCount { get; set; }
    public int WatchersCount { get; set; }
    public int ForksCount { get; set; }
    public int OpenIssuesCount { get; set; }
    public int Size { get; set; }
    public string DefaultBranch { get; set; } = string.Empty;
    public string? Language { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? PushedAt { get; set; }
    public string? GitUrl { get; set; }
    public string? SshUrl { get; set; }
    public string? CloneUrl { get; set; }
    public string? Homepage { get; set; }
    public List<string> Topics { get; set; } = new();
    public bool HasIssues { get; set; }
    public bool HasProjects { get; set; }
    public bool HasWiki { get; set; }
    public bool HasPages { get; set; }
    public bool HasDownloads { get; set; }
    public bool Archived { get; set; }
    public bool Disabled { get; set; }
    public string? Visibility { get; set; }
    public RepositoryPermissions Permissions { get; set; } = new();
    
    // Extended metadata
    public int RecentCommitsCount { get; set; }
    public List<CommitInfo> RecentCommits { get; set; } = new();
    public int OpenPullRequestsCount { get; set; }
    public int TotalPullRequestsCount { get; set; }
    public List<PullRequestInfo> RecentPullRequests { get; set; } = new();
    public List<IssueInfo> RecentIssues { get; set; } = new();
}

public class RepositoryPermissions
{
    public bool Admin { get; set; }
    public bool Push { get; set; }
    public bool Pull { get; set; }
}

public class CommitInfo
{
    public string Sha { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public string? Url { get; set; }
}

public class PullRequestInfo
{
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string AuthorLogin { get; set; } = string.Empty;
    public string? Url { get; set; }
}

public class IssueInfo
{
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string AuthorLogin { get; set; } = string.Empty;
    public List<string> Labels { get; set; } = new();
    public string? Url { get; set; }
}

#endregion