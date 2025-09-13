using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// Represents the code duplication detection trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class CodeDuplicationDetectionTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        private readonly FileStorage _fileStorage;
        private readonly CodeDuplicationAnalysisService _analysisService;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="CodeDuplicationDetectionTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>A git hub storage.</para>
        /// <para></para>
        /// </param>
        /// <param name="fileStorage">
        /// <para>A file storage.</para>
        /// <para></para>
        /// </param>
        public CodeDuplicationDetectionTrigger(GitHubStorage storage, FileStorage fileStorage)
        {
            _storage = storage;
            _fileStorage = fileStorage;
            _analysisService = new CodeDuplicationAnalysisService(storage);
        }

        /// <summary>
        /// <para>
        /// Determines whether this instance condition.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The context.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The bool</para>
        /// <para></para>
        /// </returns>
        public async Task<bool> Condition(TContext context)
        {
            return context.Title.ToLower().Contains("find") && 
                   context.Title.ToLower().Contains("repeated") &&
                   context.Title.ToLower().Contains("code");
        }

        /// <summary>
        /// <para>
        /// Actions the context.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The context.</para>
        /// <para></para>
        /// </param>
        public async Task Action(TContext context)
        {
            try
            {
                Console.WriteLine($"Starting code duplication analysis for repository: {context.Repository.Name}");
                
                var duplications = await _analysisService.AnalyzeRepositoryAsync(context.Repository);
                
                if (duplications.Any())
                {
                    await ProcessDuplications(context, duplications);
                }
                else
                {
                    await PostNoDuplicationsComment(context);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in code duplication detection: {ex.Message}");
                await PostErrorComment(context, ex.Message);
            }
        }

        private async Task ProcessDuplications(TContext context, List<CodeDuplicationAnalysisService.DuplicationGroup> duplications)
        {
            var topDuplications = duplications.Take(5).ToList();
            
            foreach (var duplication in topDuplications)
            {
                try
                {
                    await CreatePullRequestForDuplication(context, duplication);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating pull request for duplication: {ex.Message}");
                }
            }
            
            await PostAnalysisResultComment(context, duplications);
        }

        private async Task CreatePullRequestForDuplication(TContext context, CodeDuplicationAnalysisService.DuplicationGroup duplication)
        {
            var branchName = $"fix-duplication-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            var repository = context.Repository;
            
            try
            {
                var defaultBranch = await _storage.Client.Repository.Get(repository.Id);
                var defaultBranchSha = defaultBranch.DefaultBranch;
                var masterBranch = await _storage.Client.Git.Reference.Get(repository.Id, $"heads/{defaultBranchSha}");
                
                var newBranch = new NewReference($"refs/heads/{branchName}", masterBranch.Object.Sha);
                await _storage.Client.Git.Reference.Create(repository.Id, newBranch);
                
                var refactoredCode = GenerateRefactoredCode(duplication);
                var commitMessage = $"Refactor duplicated code into {duplication.SuggestedMethodName} method";
                
                foreach (var fragment in duplication.Fragments)
                {
                    var currentContent = await GetCurrentFileContent(repository, fragment.FilePath);
                    var updatedContent = ApplyRefactoring(currentContent, fragment, refactoredCode);
                    
                    await _storage.Client.Repository.Content.UpdateFile(
                        repository.Id,
                        fragment.FilePath,
                        new UpdateFileRequest(
                            commitMessage,
                            updatedContent,
                            await GetFileSha(repository, fragment.FilePath),
                            branchName
                        )
                    );
                }
                
                var pullRequest = await CreatePullRequest(repository, branchName, duplication);
                
                StoreDuplicationInfo(context, duplication, pullRequest.Number);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating pull request: {ex.Message}");
            }
        }

        private async Task<string> GetCurrentFileContent(Repository repository, string filePath)
        {
            try
            {
                var contents = await _storage.Client.Repository.Content.GetAllContents(repository.Id, filePath);
                return contents.First().Content;
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task<string> GetFileSha(Repository repository, string filePath)
        {
            try
            {
                var contents = await _storage.Client.Repository.Content.GetAllContents(repository.Id, filePath);
                return contents.First().Sha;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string GenerateRefactoredCode(CodeDuplicationAnalysisService.DuplicationGroup duplication)
        {
            var firstFragment = duplication.Fragments.First();
            var methodName = duplication.SuggestedMethodName;
            
            var refactoredMethod = $@"
        /// <summary>
        /// <para>
        /// {methodName} method extracted from duplicated code.
        /// </para>
        /// <para></para>
        /// </summary>
        private static void {methodName}()
        {{
{firstFragment.Content}
        }}";

            return refactoredMethod;
        }

        private string ApplyRefactoring(string fileContent, CodeDuplicationAnalysisService.CodeFragment fragment, string refactoredCode)
        {
            var lines = fileContent.Split('\n').ToList();
            
            var startIndex = fragment.StartLine - 1;
            var endIndex = fragment.EndLine - 1;
            
            if (startIndex < 0 || endIndex >= lines.Count || startIndex > endIndex)
            {
                return fileContent;
            }
            
            lines.RemoveRange(startIndex, endIndex - startIndex + 1);
            var commentPrefix = fragment.FilePath.EndsWith(".cs") ? "" : "// ";
            lines.Insert(startIndex, $"            {commentPrefix}{fragment.Content.Split('\n').First().Trim()} -> Call to {fragment.GetHashCode()}");
            
            return string.Join('\n', lines);
        }

        private async Task<PullRequest> CreatePullRequest(Repository repository, string branchName, CodeDuplicationAnalysisService.DuplicationGroup duplication)
        {
            var title = $"Refactor duplicated code: Extract {duplication.SuggestedMethodName} method";
            var body = GeneratePullRequestBody(duplication);
            
            var pullRequest = new NewPullRequest(title, branchName, repository.DefaultBranch)
            {
                Body = body
            };
            
            return await _storage.Client.PullRequest.Create(repository.Id, pullRequest);
        }

        private string GeneratePullRequestBody(CodeDuplicationAnalysisService.DuplicationGroup duplication)
        {
            var body = new StringBuilder();
            body.AppendLine("## Code Duplication Refactoring");
            body.AppendLine();
            body.AppendLine($"This pull request addresses code duplication found in {duplication.Count} locations:");
            body.AppendLine();
            
            foreach (var fragment in duplication.Fragments)
            {
                body.AppendLine($"- `{fragment.FilePath}` (lines {fragment.StartLine}-{fragment.EndLine})");
            }
            
            body.AppendLine();
            body.AppendLine("## Proposed Solution");
            body.AppendLine($"Extract the duplicated code into a method named `{duplication.SuggestedMethodName}`.");
            body.AppendLine();
            body.AppendLine("## Questions for Review");
            body.AppendLine($"1. **Method Name**: Is `{duplication.SuggestedMethodName}` an appropriate name? Please suggest a better name if needed.");
            body.AppendLine("2. **Method Placement**: Where should this method be placed in the codebase?");
            body.AppendLine("3. **Method Signature**: Should this method have parameters or return values?");
            body.AppendLine("4. **Access Modifier**: Should this be private, protected, public, or internal?");
            body.AppendLine();
            body.AppendLine("## Original Duplicated Code");
            body.AppendLine("```");
            body.AppendLine(duplication.Fragments.First().Content);
            body.AppendLine("```");
            body.AppendLine();
            body.AppendLine("*This pull request was created automatically by the Links Platform Bot.*");
            
            return body.ToString();
        }

        private void StoreDuplicationInfo(TContext context, CodeDuplicationAnalysisService.DuplicationGroup duplication, int pullRequestNumber)
        {
            try
            {
                var storageKey = $"duplication_{context.Repository.Id}_{pullRequestNumber}";
                var info = new
                {
                    RepositoryId = context.Repository.Id,
                    PullRequestNumber = pullRequestNumber,
                    SuggestedMethodName = duplication.SuggestedMethodName,
                    FragmentCount = duplication.Count,
                    CreatedAt = DateTime.UtcNow,
                    Fragments = duplication.Fragments.Select(f => new
                    {
                        f.FilePath,
                        f.StartLine,
                        f.EndLine,
                        f.Hash
                    }).ToList()
                };
                
                FileStorageHelperService.WriteToFile(storageKey, System.Text.Json.JsonSerializer.Serialize(info));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing duplication info: {ex.Message}");
            }
        }

        private async Task PostAnalysisResultComment(TContext context, List<CodeDuplicationAnalysisService.DuplicationGroup> duplications)
        {
            var comment = new StringBuilder();
            comment.AppendLine("## Code Duplication Analysis Results");
            comment.AppendLine();
            comment.AppendLine($"Found **{duplications.Count}** groups of duplicated code:");
            comment.AppendLine();
            
            var topDuplications = duplications.Take(10);
            foreach (var duplication in topDuplications)
            {
                comment.AppendLine($"- **{duplication.Count} occurrences** of similar code (suggested method: `{duplication.SuggestedMethodName}`)");
                foreach (var fragment in duplication.Fragments.Take(3))
                {
                    comment.AppendLine($"  - `{fragment.FilePath}` (lines {fragment.StartLine}-{fragment.EndLine})");
                }
                if (duplication.Fragments.Count > 3)
                {
                    comment.AppendLine($"  - ... and {duplication.Fragments.Count - 3} more occurrences");
                }
                comment.AppendLine();
            }
            
            if (duplications.Count > 10)
            {
                comment.AppendLine($"... and {duplications.Count - 10} more duplication groups.");
                comment.AppendLine();
            }
            
            comment.AppendLine("Pull requests have been created for the top duplication groups. Please review and provide feedback on method names and placement.");
            
            await _storage.Client.Issue.Comment.Create(context.Repository.Id, context.Number, comment.ToString());
        }

        private async Task PostNoDuplicationsComment(TContext context)
        {
            var comment = "## Code Duplication Analysis Results\n\nNo significant code duplications were found in this repository. Great job maintaining clean code! 🎉";
            await _storage.Client.Issue.Comment.Create(context.Repository.Id, context.Number, comment);
        }

        private async Task PostErrorComment(TContext context, string error)
        {
            var comment = $"## Code Duplication Analysis Error\n\nAn error occurred during the analysis:\n```\n{error}\n```\n\nPlease check the repository structure and try again.";
            await _storage.Client.Issue.Comment.Create(context.Repository.Id, context.Number, comment);
        }
    }
}