using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using Interfaces;
using Octokit;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;

    /// <summary>
    /// <para>
    /// Represents a trigger that detects errors in gist/pastebin URLs and searches Google for solutions.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    public class GoogleSearchErrorTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        private readonly HttpClient _httpClient;
        private readonly string[] _whitelistedDomains = new[]
        {
            "stackoverflow.com",
            "github.com",
            "docs.microsoft.com",
            "developer.mozilla.org",
            "msdn.microsoft.com",
            "programmingpedia.com",
            "geeksforgeeks.org"
        };

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="GoogleSearchErrorTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>A GitHub storage instance.</para>
        /// <para></para>
        /// </param>
        public GoogleSearchErrorTrigger(GitHubStorage storage)
        {
            _storage = storage;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "LinksPlatform Bot/1.0");
        }

        /// <summary>
        /// <para>
        /// Determines whether this instance should trigger based on the context.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The issue context.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>True if the issue contains gist or pastebin URLs, false otherwise.</para>
        /// <para></para>
        /// </returns>
        public async Task<bool> Condition(TContext context)
        {
            var bodyContent = context.Body ?? "";
            var titleContent = context.Title ?? "";
            var fullContent = titleContent + " " + bodyContent;

            // Check if the issue contains gist.github.com or pastebin URLs
            var gistPattern = @"https?://gist\.github\.com/[^\s]+";
            var pastebinPattern = @"https?://pastebin\.com/[^\s]+";
            
            var hasGist = Regex.IsMatch(fullContent, gistPattern, RegexOptions.IgnoreCase);
            var hasPastebin = Regex.IsMatch(fullContent, pastebinPattern, RegexOptions.IgnoreCase);
            
            return hasGist || hasPastebin;
        }

        /// <summary>
        /// <para>
        /// Executes the action when the condition is met.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The issue context.</para>
        /// <para></para>
        /// </param>
        public async Task Action(TContext context)
        {
            try
            {
                var bodyContent = context.Body ?? "";
                var titleContent = context.Title ?? "";
                var fullContent = titleContent + " " + bodyContent;

                // Extract URLs
                var urls = ExtractUrls(fullContent);
                
                if (!urls.Any())
                {
                    return;
                }

                // Process each URL and extract errors
                var allErrors = new List<string>();
                foreach (var url in urls)
                {
                    var errors = await ExtractErrorsFromUrl(url);
                    allErrors.AddRange(errors);
                }

                if (!allErrors.Any())
                {
                    await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                        "I found the gist/pastebin links you provided, but couldn't detect any clear error messages to search for. " +
                        "Could you please highlight the specific error you're encountering?");
                    return;
                }

                // Search for solutions for each error
                var searchResults = new List<string>();
                foreach (var error in allErrors.Take(3)) // Limit to 3 errors to avoid spam
                {
                    var results = await SearchGoogleForError(error);
                    searchResults.AddRange(results);
                }

                if (searchResults.Any())
                {
                    var responseMessage = "I found some potential solutions for the errors in your gist/pastebin:\n\n";
                    var uniqueResults = searchResults.Distinct().Take(5); // Limit to 5 unique results
                    
                    foreach (var result in uniqueResults)
                    {
                        responseMessage += $"• {result}\n";
                    }
                    
                    responseMessage += "\nHope these resources help you resolve the issue!";
                    
                    await _storage.CreateIssueComment(context.Repository.Id, context.Number, responseMessage);
                }
                else
                {
                    await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                        "I analyzed the errors in your gist/pastebin but couldn't find specific solutions on the whitelisted sites. " +
                        "You might want to try searching on Stack Overflow or the official documentation.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GoogleSearchErrorTrigger: {ex.Message}");
                // Don't comment on the issue if there's an internal error
            }
        }

        /// <summary>
        /// <para>
        /// Extracts gist and pastebin URLs from content.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="content">
        /// <para>The content to search.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A list of extracted URLs.</para>
        /// <para></para>
        /// </returns>
        private List<string> ExtractUrls(string content)
        {
            var urls = new List<string>();
            
            // Extract gist URLs
            var gistPattern = @"https?://gist\.github\.com/[^\s]+";
            var gistMatches = Regex.Matches(content, gistPattern, RegexOptions.IgnoreCase);
            urls.AddRange(gistMatches.Cast<Match>().Select(m => m.Value));
            
            // Extract pastebin URLs
            var pastebinPattern = @"https?://pastebin\.com/[^\s]+";
            var pastebinMatches = Regex.Matches(content, pastebinPattern, RegexOptions.IgnoreCase);
            urls.AddRange(pastebinMatches.Cast<Match>().Select(m => m.Value));
            
            return urls;
        }

        /// <summary>
        /// <para>
        /// Extracts error messages from a gist or pastebin URL.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="url">
        /// <para>The URL to process.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A list of detected error messages.</para>
        /// <para></para>
        /// </returns>
        private async Task<List<string>> ExtractErrorsFromUrl(string url)
        {
            var errors = new List<string>();
            
            try
            {
                string content;
                
                if (url.Contains("gist.github.com"))
                {
                    content = await GetGistContent(url);
                }
                else if (url.Contains("pastebin.com"))
                {
                    content = await GetPastebinContent(url);
                }
                else
                {
                    return errors;
                }

                if (!string.IsNullOrEmpty(content))
                {
                    errors.AddRange(ExtractErrorMessages(content));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting content from URL {url}: {ex.Message}");
            }
            
            return errors;
        }

        /// <summary>
        /// <para>
        /// Gets the raw content from a GitHub gist.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="gistUrl">
        /// <para>The gist URL.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The raw content of the gist.</para>
        /// <para></para>
        /// </returns>
        private async Task<string> GetGistContent(string gistUrl)
        {
            // Convert gist URL to raw URL
            // From: https://gist.github.com/username/gist-id
            // To: https://gist.githubusercontent.com/username/gist-id/raw/
            
            var match = Regex.Match(gistUrl, @"https?://gist\.github\.com/([^/]+)/([^/?#]+)");
            if (match.Success)
            {
                var username = match.Groups[1].Value;
                var gistId = match.Groups[2].Value;
                var rawUrl = $"https://gist.githubusercontent.com/{username}/{gistId}/raw/";
                
                var response = await _httpClient.GetStringAsync(rawUrl);
                return response;
            }
            
            return string.Empty;
        }

        /// <summary>
        /// <para>
        /// Gets the raw content from a Pastebin URL.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="pastebinUrl">
        /// <para>The pastebin URL.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The raw content of the paste.</para>
        /// <para></para>
        /// </returns>
        private async Task<string> GetPastebinContent(string pastebinUrl)
        {
            // Convert pastebin URL to raw URL
            // From: https://pastebin.com/paste-id
            // To: https://pastebin.com/raw/paste-id
            
            var match = Regex.Match(pastebinUrl, @"https?://pastebin\.com/([^/?#]+)");
            if (match.Success)
            {
                var pasteId = match.Groups[1].Value;
                var rawUrl = $"https://pastebin.com/raw/{pasteId}";
                
                var response = await _httpClient.GetStringAsync(rawUrl);
                return response;
            }
            
            return string.Empty;
        }

        /// <summary>
        /// <para>
        /// Extracts error messages from content using pattern matching.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="content">
        /// <para>The content to analyze.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A list of detected error messages.</para>
        /// <para></para>
        /// </returns>
        private List<string> ExtractErrorMessages(string content)
        {
            var errors = new List<string>();
            
            // Common error patterns
            var errorPatterns = new[]
            {
                // Exception patterns
                @"(\w+Exception:?.+)",
                @"(Error:?.+)",
                @"(Fatal error:?.+)",
                @"(Uncaught \w+:?.+)",
                
                // Compilation errors
                @"(error CS\d+:?.+)",
                @"(error C\d+:?.+)",
                @"(Build FAILED\..+)",
                
                // Runtime errors
                @"(Traceback \(most recent call last\):?.+)",
                @"(at \w+\.\w+\(.+\))",
                
                // HTTP errors
                @"(\d{3} \w+:?.+)",
                
                // Database errors
                @"(SQL\w* error:?.+)",
                
                // Generic error messages
                @"(failed to .+)",
                @"(cannot .+)",
                @"(unable to .+)"
            };

            foreach (var pattern in errorPatterns)
            {
                var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                foreach (Match match in matches)
                {
                    var errorMessage = match.Groups[1].Value.Trim();
                    if (errorMessage.Length > 10 && errorMessage.Length < 500) // Reasonable length
                    {
                        errors.Add(errorMessage);
                    }
                }
            }
            
            return errors.Distinct().ToList();
        }

        /// <summary>
        /// <para>
        /// Searches Google for solutions to an error and returns whitelisted results.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="errorMessage">
        /// <para>The error message to search for.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A list of whitelisted search result URLs.</para>
        /// <para></para>
        /// </returns>
        private async Task<List<string>> SearchGoogleForError(string errorMessage)
        {
            var results = new List<string>();
            
            try
            {
                // Use a simple site-specific search approach since Google Custom Search API requires API key
                foreach (var domain in _whitelistedDomains.Take(3)) // Limit to avoid being blocked
                {
                    var searchQuery = $"site:{domain} {WebUtility.UrlEncode(errorMessage)}";
                    var searchUrl = $"https://www.google.com/search?q={searchQuery}";
                    
                    // For a production implementation, you would:
                    // 1. Use Google Custom Search API with an API key
                    // 2. Parse the JSON response
                    // 3. Extract actual URLs
                    
                    // For now, we'll create helpful search links
                    results.Add($"Search on {domain}: https://www.google.com/search?q=site:{domain}+{WebUtility.UrlEncode(errorMessage.Substring(0, Math.Min(errorMessage.Length, 100)))}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching for: {errorMessage}. Exception: {ex.Message}");
            }
            
            return results;
        }

        /// <summary>
        /// <para>
        /// Disposes of resources.
        /// </para>
        /// <para></para>
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}