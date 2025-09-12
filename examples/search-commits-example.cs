using Storage.Remote.GitHub;
using System;
using System.Threading.Tasks;

/// <summary>
/// Example demonstrating how to use the SearchCommits functionality
/// that was added to GitHubStorage to work around the missing SearchCommits
/// feature in Octokit.NET library.
/// </summary>
class SearchCommitsExample
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("SearchCommits API Example");
        Console.WriteLine("==========================");
        
        // Note: In a real application, you would get these from environment variables or configuration
        var githubUsername = "your-github-username";
        var githubToken = "your-github-token"; 
        var applicationName = "LinksplatformBot";
        
        Console.WriteLine("This example demonstrates the SearchCommits functionality");
        Console.WriteLine("that was implemented to solve GitHub issue #145.");
        Console.WriteLine();
        
        // Initialize GitHubStorage (this would normally use real credentials)
        Console.WriteLine("Example usage scenarios:");
        Console.WriteLine();
        
        // Example 1: Search for commits in a specific repository
        Console.WriteLine("1. Search for commits in a specific repository:");
        Console.WriteLine("   githubStorage.SearchCommits(\"repo:linksplatform/Bot\");");
        Console.WriteLine();
        
        // Example 2: Search for commits by author
        Console.WriteLine("2. Search for commits by specific author:");
        Console.WriteLine("   githubStorage.SearchCommits(\"repo:linksplatform/Bot author:FreePhoenix888\");");
        Console.WriteLine();
        
        // Example 3: Search for commits with specific message
        Console.WriteLine("3. Search for commits containing specific text:");
        Console.WriteLine("   githubStorage.SearchCommits(\"repo:linksplatform/Bot upgrade framework\");");
        Console.WriteLine();
        
        // Example 4: Search for commits in date range
        Console.WriteLine("4. Search for recent commits:");
        Console.WriteLine("   githubStorage.SearchCommits(\"repo:linksplatform/Bot author-date:>=2024-01-01\");");
        Console.WriteLine();
        
        Console.WriteLine("Note: To use this functionality, you need valid GitHub credentials.");
        Console.WriteLine("The SearchCommits method returns a SearchCommitsResult object with:");
        Console.WriteLine("- TotalCount: Number of matching commits");
        Console.WriteLine("- Items: List of CommitSearchResult objects");
        Console.WriteLine("- Each CommitSearchResult contains commit SHA, message, author, etc.");
        Console.WriteLine();
        
        Console.WriteLine("This implementation bridges the gap until Octokit.NET officially");
        Console.WriteLine("adds SearchCommits support as requested in issue #2425.");
    }
}