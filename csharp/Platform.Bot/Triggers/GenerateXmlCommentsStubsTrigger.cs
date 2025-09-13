using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Octokit;
using Storage.Remote.GitHub;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;

    /// <summary>
    /// <para>
    /// Represents the generate XML comments stubs trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class GenerateXmlCommentsStubsTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="GenerateXmlCommentsStubsTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>A GitHub storage.</para>
        /// <para></para>
        /// </param>
        public GenerateXmlCommentsStubsTrigger(GitHubStorage storage)
        {
            _storage = storage;
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
            
            return title.Contains("xml comment") || title.Contains("xml doc") || 
                   body.Contains("xml comment") || body.Contains("xml doc") ||
                   title.Contains("generate comments") || body.Contains("generate comments");
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
                var branchName = $"generate-xml-comments-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
                
                // Get all C# files in the repository
                var csharpFiles = await GetCSharpFilesFromRepository(repository);
                
                var modifiedFiles = new List<(string path, string content)>();
                
                foreach (var file in csharpFiles)
                {
                    var modifiedContent = await GenerateXmlCommentsForFile(file.content, file.path);
                    if (modifiedContent != file.content)
                    {
                        modifiedFiles.Add((file.path, modifiedContent));
                    }
                }

                if (modifiedFiles.Any())
                {
                    // Create a new branch using Octokit directly
                    await CreateBranch(repository, branchName, repository.DefaultBranch);
                    
                    // Update files with XML comments
                    foreach (var (path, content) in modifiedFiles)
                    {
                        await _storage.CreateOrUpdateFile(content, repository, branchName, path, 
                            "Generate XML comment stubs for public members");
                    }

                    // Create a pull request using Octokit directly
                    var pullRequest = await CreatePullRequest(repository, 
                        title: "Generate XML comment stubs for public members",
                        head: branchName,
                        baseRef: repository.DefaultBranch,
                        body: $"This PR automatically generates XML comment stubs for all public members.\n\nGenerated from issue #{context.Number}");

                    // Comment on the original issue
                    await _storage.CreateIssueComment(repository.Id, context.Number,
                        $"XML comment stubs have been generated! Please review the pull request: #{pullRequest.Number}");
                }
                else
                {
                    await _storage.CreateIssueComment(repository.Id, context.Number,
                        "No C# files found or all public members already have XML comments.");
                }

                _storage.CloseIssue(context);
            }
            catch (Exception ex)
            {
                await _storage.CreateIssueComment(context.Repository.Id, context.Number,
                    $"Error generating XML comments: {ex.Message}");
            }
        }

        private async Task<List<(string path, string content)>> GetCSharpFilesFromRepository(Repository repository)
        {
            var files = new List<(string path, string content)>();
            var contents = await _storage.Client.Repository.Content.GetAllContents(repository.Id, "");
            
            await CollectCSharpFiles(repository, contents, "", files);
            return files;
        }

        private async Task CollectCSharpFiles(Repository repository, IReadOnlyList<RepositoryContent> contents, 
            string currentPath, List<(string path, string content)> files)
        {
            foreach (var item in contents)
            {
                var fullPath = string.IsNullOrEmpty(currentPath) ? item.Name : $"{currentPath}/{item.Name}";
                
                if (item.Type == ContentType.File && item.Name.EndsWith(".cs"))
                {
                    var fileContents = await _storage.Client.Repository.Content.GetAllContents(repository.Id, fullPath);
                    var fileContent = fileContents.First().Content;
                    files.Add((fullPath, fileContent));
                }
                else if (item.Type == ContentType.Dir)
                {
                    var subContents = await _storage.Client.Repository.Content.GetAllContents(repository.Id, fullPath);
                    await CollectCSharpFiles(repository, subContents, fullPath, files);
                }
            }
        }

        private async Task CreateBranch(Repository repository, string branchName, string baseBranchName)
        {
            var baseBranch = await _storage.Client.Repository.Branch.Get(repository.Id, baseBranchName);
            var newReference = new NewReference($"refs/heads/{branchName}", baseBranch.Commit.Sha);
            await _storage.Client.Git.Reference.Create(repository.Id, newReference);
        }

        private async Task<PullRequest> CreatePullRequest(Repository repository, string title, string head, string baseRef, string body)
        {
            var newPullRequest = new NewPullRequest(title, head, baseRef)
            {
                Body = body
            };
            return await _storage.Client.PullRequest.Create(repository.Id, newPullRequest);
        }

        private async Task<string> GenerateXmlCommentsForFile(string fileContent, string filePath)
        {
            try
            {
                var tree = CSharpSyntaxTree.ParseText(fileContent);
                var root = await tree.GetRootAsync();
                var rewriter = new XmlCommentsRewriter();
                var newRoot = rewriter.Visit(root);
                
                return newRoot.ToFullString();
            }
            catch
            {
                // If parsing fails, return original content
                return fileContent;
            }
        }
    }

    internal class XmlCommentsRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (IsPublic(node.Modifiers) && !HasXmlComment(node))
            {
                var xmlComment = CreateXmlComment($"Represents the {node.Identifier.ValueText.ToLower()}.");
                node = node.WithLeadingTrivia(node.GetLeadingTrivia().Insert(0, xmlComment));
            }
            return base.VisitClassDeclaration(node);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (IsPublic(node.Modifiers) && !HasXmlComment(node))
            {
                var xmlComment = GenerateMethodXmlComment(node);
                node = node.WithLeadingTrivia(node.GetLeadingTrivia().Insert(0, xmlComment));
            }
            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (IsPublic(node.Modifiers) && !HasXmlComment(node))
            {
                var xmlComment = CreateXmlComment($"Gets or sets the {node.Identifier.ValueText.ToLower()}.");
                node = node.WithLeadingTrivia(node.GetLeadingTrivia().Insert(0, xmlComment));
            }
            return base.VisitPropertyDeclaration(node);
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (IsPublic(node.Modifiers) && !HasXmlComment(node))
            {
                var xmlComment = GenerateConstructorXmlComment(node);
                node = node.WithLeadingTrivia(node.GetLeadingTrivia().Insert(0, xmlComment));
            }
            return base.VisitConstructorDeclaration(node);
        }

        private static bool IsPublic(SyntaxTokenList modifiers)
        {
            return modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword));
        }

        private static bool HasXmlComment(SyntaxNode node)
        {
            var leadingTrivia = node.GetLeadingTrivia();
            return leadingTrivia.Any(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || 
                                         t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
        }

        private static SyntaxTrivia CreateXmlComment(string summary)
        {
            var commentText = $@"        /// <summary>
        /// <para>
        /// {summary}
        /// </para>
        /// <para></para>
        /// </summary>";
            
            return SyntaxFactory.Comment(commentText + Environment.NewLine);
        }

        private static SyntaxTrivia GenerateMethodXmlComment(MethodDeclarationSyntax method)
        {
            var sb = new StringBuilder();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// <para>");
            sb.AppendLine($"        /// {GetMethodDescription(method)}");
            sb.AppendLine("        /// </para>");
            sb.AppendLine("        /// <para></para>");
            sb.AppendLine("        /// </summary>");

            foreach (var parameter in method.ParameterList.Parameters)
            {
                sb.AppendLine($"        /// <param name=\"{parameter.Identifier}\">");
                sb.AppendLine("        /// <para>The parameter.</para>");
                sb.AppendLine("        /// <para></para>");
                sb.AppendLine("        /// </param>");
            }

            if (!method.ReturnType.ToString().Equals("void", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("        /// <returns>");
                sb.AppendLine($"        /// <para>The {method.ReturnType.ToString().ToLower()}</para>");
                sb.AppendLine("        /// <para></para>");
                sb.AppendLine("        /// </returns>");
            }

            return SyntaxFactory.Comment(sb.ToString());
        }

        private static SyntaxTrivia GenerateConstructorXmlComment(ConstructorDeclarationSyntax constructor)
        {
            var sb = new StringBuilder();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// <para>");
            sb.AppendLine($"        /// Initializes a new <see cref=\"{constructor.Identifier}\"/> instance.");
            sb.AppendLine("        /// </para>");
            sb.AppendLine("        /// <para></para>");
            sb.AppendLine("        /// </summary>");

            foreach (var parameter in constructor.ParameterList.Parameters)
            {
                sb.AppendLine($"        /// <param name=\"{parameter.Identifier}\">");
                sb.AppendLine("        /// <para>The parameter.</para>");
                sb.AppendLine("        /// <para></para>");
                sb.AppendLine("        /// </param>");
            }

            return SyntaxFactory.Comment(sb.ToString());
        }

        private static string GetMethodDescription(MethodDeclarationSyntax method)
        {
            var methodName = method.Identifier.ValueText;
            
            if (methodName.StartsWith("Get"))
                return $"Gets the {methodName.Substring(3).ToLower()}.";
            if (methodName.StartsWith("Set"))
                return $"Sets the {methodName.Substring(3).ToLower()}.";
            if (methodName.StartsWith("Create"))
                return $"Creates the {methodName.Substring(6).ToLower()}.";
            if (methodName.StartsWith("Update"))
                return $"Updates the {methodName.Substring(6).ToLower()}.";
            if (methodName.StartsWith("Delete"))
                return $"Deletes the {methodName.Substring(6).ToLower()}.";
            
            return $"Performs the {methodName.ToLower()} operation.";
        }
    }
}