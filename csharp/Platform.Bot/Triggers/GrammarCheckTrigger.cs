using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;
    
    /// <summary>
    /// <para>
    /// Represents a grammar and spelling check trigger using LanguageTool API.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class GrammarCheckTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        private readonly HttpClient _httpClient;
        private const string LanguageToolApiUrl = "https://api.languagetool.org/v2/check";

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="GrammarCheckTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>A GitHub storage instance.</para>
        /// <para></para>
        /// </param>
        public GrammarCheckTrigger(GitHubStorage storage)
        {
            _storage = storage;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// <para>
        /// Determines whether this instance should be triggered based on the issue context.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The issue context.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>True if the issue title contains "grammar check" or "spell check", otherwise false.</para>
        /// <para></para>
        /// </returns>
        public async Task<bool> Condition(TContext context)
        {
            var title = context.Title.ToLowerInvariant();
            return title.Contains("grammar check") || 
                   title.Contains("spell check") || 
                   title.Contains("proofread") ||
                   title.Contains("grammar") ||
                   title.Contains("spelling");
        }

        /// <summary>
        /// <para>
        /// Performs grammar and spelling check on the issue description and posts corrections as a comment.
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
                // Get the text to check (issue description)
                var textToCheck = context.Body ?? "";
                
                if (string.IsNullOrWhiteSpace(textToCheck))
                {
                    await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                        "⚠️ No text found to check for grammar or spelling errors.");
                    return;
                }

                // Call LanguageTool API to check grammar and spelling
                var suggestions = await CheckGrammarAndSpelling(textToCheck);
                
                if (suggestions.Count == 0)
                {
                    await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                        "✅ No grammar or spelling issues found in the issue description!");
                    return;
                }

                // Format the response with suggestions
                var responseBuilder = new StringBuilder();
                responseBuilder.AppendLine("## Grammar and Spelling Check Results");
                responseBuilder.AppendLine();
                responseBuilder.AppendLine($"Found **{suggestions.Count}** potential issue(s):");
                responseBuilder.AppendLine();

                for (int i = 0; i < suggestions.Count; i++)
                {
                    var suggestion = suggestions[i];
                    responseBuilder.AppendLine($"**{i + 1}.** {suggestion.Message}");
                    
                    if (!string.IsNullOrEmpty(suggestion.Context))
                    {
                        responseBuilder.AppendLine($"   - Context: \"{suggestion.Context}\"");
                    }
                    
                    if (suggestion.Replacements?.Count > 0)
                    {
                        responseBuilder.AppendLine($"   - Suggested correction(s): {string.Join(", ", suggestion.Replacements)}");
                    }
                    
                    responseBuilder.AppendLine();
                }

                responseBuilder.AppendLine("---");
                responseBuilder.AppendLine("*Grammar checking powered by LanguageTool API*");

                // Post the comment
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, responseBuilder.ToString());
            }
            catch (Exception ex)
            {
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                    $"❌ Error occurred during grammar check: {ex.Message}");
            }
        }

        /// <summary>
        /// <para>
        /// Calls the LanguageTool API to check grammar and spelling.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="text">
        /// <para>The text to check.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A list of grammar and spelling suggestions.</para>
        /// <para></para>
        /// </returns>
        private async Task<List<GrammarSuggestion>> CheckGrammarAndSpelling(string text)
        {
            try
            {
                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("text", text),
                    new KeyValuePair<string, string>("language", "en-US")
                });

                var response = await _httpClient.PostAsync(LanguageToolApiUrl, formData);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<LanguageToolResponse>(jsonResponse);

                var suggestions = new List<GrammarSuggestion>();
                
                if (apiResponse?.matches != null)
                {
                    foreach (var match in apiResponse.matches)
                    {
                        var suggestion = new GrammarSuggestion
                        {
                            Message = match.message ?? "Grammar/spelling issue detected",
                            Context = GetContextFromMatch(text, match),
                            Replacements = match.replacements?.ConvertAll(r => r.value) ?? new List<string>()
                        };
                        
                        suggestions.Add(suggestion);
                    }
                }

                return suggestions;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to check grammar with LanguageTool API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// <para>
        /// Extracts context around a grammar/spelling error.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="text">
        /// <para>The full text.</para>
        /// <para></para>
        /// </param>
        /// <param name="match">
        /// <para>The match information from LanguageTool.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A context string showing the error in context.</para>
        /// <para></para>
        /// </returns>
        private static string GetContextFromMatch(string text, LanguageToolMatch match)
        {
            try
            {
                var start = Math.Max(0, match.offset - 20);
                var end = Math.Min(text.Length, match.offset + match.length + 20);
                var context = text.Substring(start, end - start);
                
                // Highlight the error in the context
                var errorText = text.Substring(match.offset, match.length);
                context = context.Replace(errorText, $"**{errorText}**");
                
                return context;
            }
            catch
            {
                return "";
            }
        }
    }

    /// <summary>
    /// <para>
    /// Represents a grammar or spelling suggestion.
    /// </para>
    /// <para></para>
    /// </summary>
    internal class GrammarSuggestion
    {
        public string Message { get; set; } = "";
        public string Context { get; set; } = "";
        public List<string> Replacements { get; set; } = new();
    }

    /// <summary>
    /// <para>
    /// Response model for LanguageTool API.
    /// </para>
    /// <para></para>
    /// </summary>
    internal class LanguageToolResponse
    {
        public List<LanguageToolMatch>? matches { get; set; }
    }

    /// <summary>
    /// <para>
    /// Match model for LanguageTool API response.
    /// </para>
    /// <para></para>
    /// </summary>
    internal class LanguageToolMatch
    {
        public string? message { get; set; }
        public int offset { get; set; }
        public int length { get; set; }
        public List<LanguageToolReplacement>? replacements { get; set; }
    }

    /// <summary>
    /// <para>
    /// Replacement model for LanguageTool API response.
    /// </para>
    /// <para></para>
    /// </summary>
    internal class LanguageToolReplacement
    {
        public string value { get; set; } = "";
    }
}