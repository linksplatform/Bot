using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Platform.Bot.Services;
using Storage.Local;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;

    /// <summary>
    /// <para>
    /// Represents a trigger that solves bugs based on unit tests provided in issues.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class BugSolverTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        private readonly FileStorage _fileStorage;
        private readonly RepositorySearchService _searchService;
        private readonly TestExecutionService _testExecutionService;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="BugSolverTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>The GitHub storage instance.</para>
        /// <para></para>
        /// </param>
        /// <param name="fileStorage">
        /// <para>The file storage instance.</para>
        /// <para></para>
        /// </param>
        public BugSolverTrigger(GitHubStorage storage, FileStorage fileStorage)
        {
            _storage = storage;
            _fileStorage = fileStorage;
            _searchService = new RepositorySearchService(storage);
            _testExecutionService = new TestExecutionService(storage);
        }

        /// <summary>
        /// <para>
        /// Determines whether this instance condition is met for bug solving.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The issue context.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>True if the issue contains a bug report with unit tests.</para>
        /// <para></para>
        /// </returns>
        public async Task<bool> Condition(TContext context)
        {
            if (context?.Body == null) return false;
            
            var body = context.Body.ToLower();
            var title = context.Title?.ToLower() ?? "";
            
            // Check if this is a bug report with test cases
            var hasBugKeywords = title.Contains("bug") || title.Contains("fix") || title.Contains("error") || 
                               body.Contains("bug") || body.Contains("expected behavior") || body.Contains("actual behavior");
            
            var hasTestCode = body.Contains("```") && (
                body.Contains("test") || body.Contains("assert") || body.Contains("expect") ||
                body.Contains("unittest") || body.Contains("testcase") || body.Contains("should")
            );
            
            return hasBugKeywords && hasTestCode;
        }

        /// <summary>
        /// <para>
        /// Performs the bug solving action.
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
                // Parse the issue for test cases and expected behavior
                var testCases = ExtractTestCasesFromIssue(context.Body);
                var expectedBehavior = ExtractExpectedBehavior(context.Body);
                
                if (!testCases.Any())
                {
                    await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                        "🤖 I couldn't find any test cases in this issue. Please provide unit tests that demonstrate the bug.");
                    return;
                }

                // Search for relevant code sections
                var relevantFiles = await SearchForRelevantCode(context, testCases, expectedBehavior);
                
                if (!relevantFiles.Any())
                {
                    await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                        "🤖 I couldn't locate the code responsible for this behavior. Please provide more specific information about the affected components.");
                    return;
                }

                // Attempt to create a naive fix
                await AttemptBugFix(context, testCases, relevantFiles, expectedBehavior);
            }
            catch (Exception ex)
            {
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                    $"🤖 I encountered an error while trying to solve this bug: {ex.Message}");
            }
        }

        /// <summary>
        /// <para>
        /// Extracts test cases from the issue body.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="issueBody">
        /// <para>The issue body content.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A list of extracted test cases.</para>
        /// <para></para>
        /// </returns>
        private List<string> ExtractTestCasesFromIssue(string issueBody)
        {
            var testCases = new List<string>();
            
            // Extract code blocks
            var codeBlockRegex = new Regex(@"```[\w]*\n?(.*?)\n?```", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var matches = codeBlockRegex.Matches(issueBody);
            
            foreach (Match match in matches)
            {
                var code = match.Groups[1].Value.Trim();
                if (IsTestCase(code))
                {
                    testCases.Add(code);
                }
            }
            
            return testCases;
        }

        /// <summary>
        /// <para>
        /// Determines if the given code snippet is a test case.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="code">
        /// <para>The code snippet to check.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>True if the code appears to be a test case.</para>
        /// <para></para>
        /// </returns>
        private bool IsTestCase(string code)
        {
            var lowerCode = code.ToLower();
            return lowerCode.Contains("test") || lowerCode.Contains("assert") || 
                   lowerCode.Contains("expect") || lowerCode.Contains("should") ||
                   lowerCode.Contains("unittest") || lowerCode.Contains("@test");
        }

        /// <summary>
        /// <para>
        /// Extracts expected behavior description from the issue.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="issueBody">
        /// <para>The issue body content.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The expected behavior description.</para>
        /// <para></para>
        /// </returns>
        private string ExtractExpectedBehavior(string issueBody)
        {
            var patterns = new[]
            {
                @"expected behavior:?\s*(.*?)(?=actual behavior|steps to reproduce|$)",
                @"should:?\s*(.*?)(?=but|however|instead|$)",
                @"expected:?\s*(.*?)(?=actual|got|received|$)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(issueBody, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }

            return "No clear expected behavior specified";
        }

        /// <summary>
        /// <para>
        /// Searches for code files relevant to the bug report.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The issue context.</para>
        /// <para></para>
        /// </param>
        /// <param name="testCases">
        /// <para>The extracted test cases.</para>
        /// <para></para>
        /// </param>
        /// <param name="expectedBehavior">
        /// <para>The expected behavior description.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A list of relevant file paths.</para>
        /// <para></para>
        /// </returns>
        private async Task<List<string>> SearchForRelevantCode(TContext context, List<string> testCases, string expectedBehavior)
        {
            var relevantFiles = new List<string>();
            
            // Extract function/method names from test cases
            var functionNames = ExtractFunctionNames(testCases);
            
            if (functionNames.Any())
            {
                // Search for files containing these functions using the search service
                var functionSearchResults = await _searchService.SearchForFunctions(context.Repository, functionNames);
                
                foreach (var result in functionSearchResults.Values)
                {
                    relevantFiles.AddRange(result);
                }
            }
            
            // Also search by keywords from the issue title and expected behavior
            var keywords = ExtractKeywords(context.Title, expectedBehavior);
            if (keywords.Any())
            {
                var keywordResults = await _searchService.SearchByKeywords(context.Repository, keywords);
                relevantFiles.AddRange(keywordResults);
            }
            
            return relevantFiles.Distinct().ToList();
        }

        /// <summary>
        /// <para>
        /// Extracts function/method names from test cases.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="testCases">
        /// <para>The test cases to analyze.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A list of function names found in the test cases.</para>
        /// <para></para>
        /// </returns>
        private List<string> ExtractFunctionNames(List<string> testCases)
        {
            var functionNames = new List<string>();
            
            foreach (var testCase in testCases)
            {
                // Look for function calls in various formats
                var patterns = new[]
                {
                    @"(\w+)\s*\(",  // functionName(
                    @"\.(\w+)\s*\(",  // object.methodName(
                    @"(\w+)::",  // ClassName::
                    @"def\s+(\w+)",  // def function_name (Python)
                    @"function\s+(\w+)",  // function functionName (JavaScript)
                    @"public\s+\w+\s+(\w+)\s*\(",  // public returnType MethodName( (C#/Java)
                };
                
                foreach (var pattern in patterns)
                {
                    var matches = Regex.Matches(testCase, pattern);
                    foreach (Match match in matches)
                    {
                        var name = match.Groups[1].Value;
                        if (name.Length > 2 && !IsKeyword(name))
                        {
                            functionNames.Add(name);
                        }
                    }
                }
            }
            
            return functionNames.Distinct().ToList();
        }

        /// <summary>
        /// <para>
        /// Checks if a string is a programming language keyword.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="word">
        /// <para>The word to check.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>True if the word is a keyword.</para>
        /// <para></para>
        /// </returns>
        private bool IsKeyword(string word)
        {
            var keywords = new HashSet<string>
            {
                "if", "else", "for", "while", "do", "return", "function", "def", "class", "public", "private",
                "protected", "static", "void", "int", "string", "bool", "true", "false", "null", "var", "let",
                "const", "assert", "test", "expect", "should", "describe", "it"
            };
            
            return keywords.Contains(word.ToLower());
        }

        /// <summary>
        /// <para>
        /// Extracts keywords from issue title and expected behavior for searching.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="title">
        /// <para>The issue title.</para>
        /// <para></para>
        /// </param>
        /// <param name="expectedBehavior">
        /// <para>The expected behavior description.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A list of keywords for searching.</para>
        /// <para></para>
        /// </returns>
        private List<string> ExtractKeywords(string title, string expectedBehavior)
        {
            var keywords = new List<string>();
            
            // Extract meaningful words from title
            if (!string.IsNullOrEmpty(title))
            {
                var titleWords = Regex.Split(title, @"\W+")
                    .Where(word => word.Length > 3 && !IsCommonWord(word))
                    .ToList();
                keywords.AddRange(titleWords);
            }
            
            // Extract meaningful words from expected behavior
            if (!string.IsNullOrEmpty(expectedBehavior) && expectedBehavior != "No clear expected behavior specified")
            {
                var behaviorWords = Regex.Split(expectedBehavior, @"\W+")
                    .Where(word => word.Length > 3 && !IsCommonWord(word))
                    .ToList();
                keywords.AddRange(behaviorWords);
            }
            
            return keywords.Distinct().ToList();
        }

        /// <summary>
        /// <para>
        /// Checks if a word is a common word that should be filtered out.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="word">
        /// <para>The word to check.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>True if the word is common and should be filtered.</para>
        /// <para></para>
        /// </returns>
        private bool IsCommonWord(string word)
        {
            var commonWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "the", "and", "with", "that", "this", "from", "they", "should", "would", "could",
                "when", "where", "what", "have", "does", "will", "been", "were", "said", "each",
                "which", "their", "time", "work", "only", "used", "make", "more", "also", "after",
                "first", "well", "many", "must", "before", "here", "through", "back", "much",
                "other", "very", "such", "being", "over", "still", "some", "like", "into", "than"
            };
            
            return commonWords.Contains(word);
        }

        /// <summary>
        /// <para>
        /// Attempts to create a naive fix for the bug.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The issue context.</para>
        /// <para></para>
        /// </param>
        /// <param name="testCases">
        /// <para>The test cases that should pass.</para>
        /// <para></para>
        /// </param>
        /// <param name="relevantFiles">
        /// <para>The files that might contain the bug.</para>
        /// <para></para>
        /// </param>
        /// <param name="expectedBehavior">
        /// <para>The expected behavior description.</para>
        /// <para></para>
        /// </param>
        private async Task AttemptBugFix(TContext context, List<string> testCases, List<string> relevantFiles, string expectedBehavior)
        {
            var analysisComment = "🤖 **Bug Analysis Report**\n\n";
            analysisComment += "**Test Cases Found:**\n";
            
            foreach (var testCase in testCases)
            {
                analysisComment += $"```\n{testCase}\n```\n";
            }
            
            analysisComment += $"\n**Expected Behavior:** {expectedBehavior}\n\n";
            analysisComment += "**Potentially Relevant Files:**\n";
            
            foreach (var file in relevantFiles)
            {
                analysisComment += $"- {file}\n";
            }
            
            analysisComment += "\n**Recommended Actions:**\n";
            analysisComment += "1. Review the identified files for the logic that handles the tested functionality\n";
            analysisComment += "2. Run the provided test cases against the current implementation\n";
            analysisComment += "3. Identify where the actual behavior deviates from expected behavior\n";
            analysisComment += "4. Implement fixes in the identified locations\n";
            analysisComment += "5. Verify that all existing tests still pass\n\n";
            
            // Generate specific fix suggestions based on common patterns
            var fixSuggestions = GenerateFixSuggestions(testCases, expectedBehavior);
            if (fixSuggestions.Any())
            {
                analysisComment += "**Potential Fix Locations:**\n";
                foreach (var suggestion in fixSuggestions)
                {
                    analysisComment += $"- {suggestion}\n";
                }
                analysisComment += "\n";
            }
            
            // Add test execution instructions
            var testInstructions = await _testExecutionService.GenerateTestInstructions(context.Repository, testCases);
            analysisComment += testInstructions;
            analysisComment += "\n";
            
            analysisComment += "*Note: This is an automated analysis. Manual review is recommended.*";
            
            await _storage.CreateIssueComment(context.Repository.Id, context.Number, analysisComment);
        }

        /// <summary>
        /// <para>
        /// Generates specific fix suggestions based on test cases and expected behavior.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="testCases">
        /// <para>The test cases to analyze.</para>
        /// <para></para>
        /// </param>
        /// <param name="expectedBehavior">
        /// <para>The expected behavior description.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A list of fix suggestions.</para>
        /// <para></para>
        /// </returns>
        private List<string> GenerateFixSuggestions(List<string> testCases, string expectedBehavior)
        {
            var suggestions = new List<string>();
            
            foreach (var testCase in testCases)
            {
                var lowerCase = testCase.ToLower();
                
                if (lowerCase.Contains("null") && lowerCase.Contains("assert"))
                {
                    suggestions.Add("Check for null reference handling in the tested methods");
                }
                
                if (lowerCase.Contains("empty") || lowerCase.Contains("[]") || lowerCase.Contains("\"\""))
                {
                    suggestions.Add("Verify edge case handling for empty inputs");
                }
                
                if (lowerCase.Contains("exception") || lowerCase.Contains("error"))
                {
                    suggestions.Add("Review error handling and exception throwing logic");
                }
                
                if (lowerCase.Contains("equal") || lowerCase.Contains("=="))
                {
                    suggestions.Add("Check comparison logic and data type conversions");
                }
                
                if (lowerCase.Contains("length") || lowerCase.Contains("count") || lowerCase.Contains("size"))
                {
                    suggestions.Add("Verify collection size and boundary condition handling");
                }
            }
            
            return suggestions.Distinct().ToList();
        }
    }
}