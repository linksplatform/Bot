using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interfaces;
using Octokit;
using OpenAI;
using OpenAI.Chat;
using Storage.Local;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;
    
    /// <summary>
    /// <para>
    /// Represents the code optimizer trigger that uses ChatGPT/GPT-4 to optimize code.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class CodeOptimizerTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        private readonly FileStorage _fileStorage;
        private readonly OpenAIClient _openAiClient;
        private readonly string _model;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="CodeOptimizerTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>A GitHub storage.</para>
        /// <para></para>
        /// </param>
        /// <param name="fileStorage">
        /// <para>A file storage.</para>
        /// <para></para>
        /// </param>
        /// <param name="openAiApiKey">
        /// <para>OpenAI API key.</para>
        /// <para></para>
        /// </param>
        /// <param name="model">
        /// <para>The model to use (default: gpt-4).</para>
        /// <para></para>
        /// </param>
        public CodeOptimizerTrigger(GitHubStorage storage, FileStorage fileStorage, string openAiApiKey, string model = "gpt-4")
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
            _openAiClient = new OpenAIClient(openAiApiKey);
            _model = model;
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
            var title = context.Title.ToLower();
            var body = context.Body?.ToLower() ?? "";
            
            return title.Contains("optimize") || title.Contains("optimization") || 
                   body.Contains("optimize code") || body.Contains("code optimization") ||
                   title.Contains("improve performance") || body.Contains("improve performance");
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
                var repository = context.Repository;
                var optimizedFiles = new List<(string path, string content)>();

                // Get all code files from the repository
                var codeFiles = await GetCodeFilesFromRepository(repository);

                foreach (var file in codeFiles)
                {
                    var optimizedCode = await OptimizeCodeWithGPT(file.content, file.path);
                    if (!string.IsNullOrWhiteSpace(optimizedCode) && optimizedCode != file.content)
                    {
                        optimizedFiles.Add((file.path, optimizedCode));
                    }
                }

                // Create a new branch for optimizations
                var branchName = $"optimize-code-{context.Number}";
                var defaultBranch = repository.DefaultBranch;
                
                // Create branch and commit optimized files
                if (optimizedFiles.Any())
                {
                    // Create branch reference
                    var defaultBranchRef = await _storage.GetBranch(repository.Id, defaultBranch);
                    var newReference = new NewReference($"refs/heads/{branchName}", defaultBranchRef.Commit.Sha);
                    await _storage.CreateReference(repository.Id, newReference);
                    
                    foreach (var file in optimizedFiles)
                    {
                        await _storage.CreateOrUpdateFile(file.content, repository, branchName, file.path, 
                            $"Optimize code in {file.path} using GPT-4");
                    }

                    // Create pull request using Octokit client directly
                    var newPr = new NewPullRequest(
                        $"Code optimization for issue #{context.Number}",
                        branchName,
                        defaultBranch)
                    {
                        Body = $"This PR contains code optimizations generated by GPT-4 for issue #{context.Number}.\n\n" +
                               $"Files optimized:\n{string.Join("\n", optimizedFiles.Select(f => $"- {f.path}"))}"
                    };
                    
                    await _storage.Client.PullRequest.Create(repository.Id, newPr);

                    // Add comment to the issue
                    var comment = $"I've analyzed the code and created optimizations using GPT-4. " +
                                  $"Please check the pull request with the optimized code: {branchName}";
                    await _storage.CreateIssueComment(repository.Id, context.Number, comment);
                }
                else
                {
                    await _storage.CreateIssueComment(repository.Id, context.Number,
                        "I've analyzed the code but couldn't find any significant optimizations to suggest at this time.");
                }
            }
            catch (Exception ex)
            {
                await _storage.CreateIssueComment(context.Repository.Id, context.Number,
                    $"An error occurred while optimizing the code: {ex.Message}");
            }
        }

        private async Task<List<(string path, string content)>> GetCodeFilesFromRepository(Repository repository)
        {
            var files = new List<(string path, string content)>();
            var supportedExtensions = new[] { ".cs", ".js", ".ts", ".py", ".java", ".cpp", ".c", ".h" };

            try
            {
                // Get all contents from repository using Octokit client directly
                var contents = await _storage.Client.Repository.Content.GetAllContents(repository.Id);
                
                foreach (var content in contents.Where(c => c.Type == ContentType.File))
                {
                    var extension = Path.GetExtension(content.Name);
                    if (supportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                    {
                        // Get file content
                        var fileContents = await _storage.Client.Repository.Content.GetAllContentsByRef(repository.Id, content.Path, repository.DefaultBranch);
                        var fileContent = fileContents.FirstOrDefault()?.Content;
                        
                        if (!string.IsNullOrWhiteSpace(fileContent))
                        {
                            // Decode base64 content
                            var decodedContent = Encoding.UTF8.GetString(Convert.FromBase64String(fileContent));
                            files.Add((content.Path, decodedContent));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but continue
                Console.WriteLine($"Error getting repository contents: {ex.Message}");
            }

            return files;
        }

        private async Task<string> OptimizeCodeWithGPT(string code, string filePath)
        {
            try
            {
                var fileExtension = Path.GetExtension(filePath);
                var language = GetLanguageFromExtension(fileExtension);

                var systemPrompt = $"You are a code optimization expert. Analyze the provided {language} code and suggest optimizations for:" +
                                   "\n1. Performance improvements" +
                                   "\n2. Memory usage optimization" +
                                   "\n3. Code readability and maintainability" +
                                   "\n4. Best practices compliance" +
                                   "\n5. Algorithmic improvements" +
                                   "\n\nProvide only the optimized code without explanations. If no significant optimizations are possible, return the original code.";

                var userPrompt = $"Optimize this {language} code:\n\n```{language}\n{code}\n```";

                var chatClient = _openAiClient.GetChatClient(_model);
                var messages = new List<ChatMessage>
                {
                    ChatMessage.CreateSystemMessage(systemPrompt),
                    ChatMessage.CreateUserMessage(userPrompt)
                };

                var response = await chatClient.CompleteChatAsync(messages);
                var optimizedCode = response.Value.Content[0].Text;

                // Clean up the response - remove markdown code blocks if present
                optimizedCode = CleanCodeResponse(optimizedCode);

                return optimizedCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error optimizing code with GPT: {ex.Message}");
                return code; // Return original code if optimization fails
            }
        }

        private string GetLanguageFromExtension(string extension)
        {
            return extension.ToLower() switch
            {
                ".cs" => "C#",
                ".js" => "JavaScript",
                ".ts" => "TypeScript",
                ".py" => "Python",
                ".java" => "Java",
                ".cpp" => "C++",
                ".c" => "C",
                ".h" => "C/C++ Header",
                _ => "code"
            };
        }

        private string CleanCodeResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return response;

            // Remove markdown code blocks
            var lines = response.Split('\n');
            var codeLines = new List<string>();
            bool inCodeBlock = false;

            foreach (var line in lines)
            {
                if (line.StartsWith("```"))
                {
                    inCodeBlock = !inCodeBlock;
                    continue;
                }

                if (inCodeBlock || !response.Contains("```"))
                {
                    codeLines.Add(line);
                }
            }

            return string.Join('\n', codeLines).Trim();
        }
    }
}