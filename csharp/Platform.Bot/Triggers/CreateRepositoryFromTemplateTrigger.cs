using System;
using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Storage.Remote.GitHub;
using System.Text.RegularExpressions;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;
    
    /// <summary>
    /// <para>
    /// Represents the create repository from template trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class CreateRepositoryFromTemplateTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="CreateRepositoryFromTemplateTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>A github storage.</para>
        /// <para></para>
        /// </param>
        public CreateRepositoryFromTemplateTrigger(GitHubStorage storage)
        {
            this._storage = storage;
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
            var templateType = GetTemplateType(context.Title.ToLower());
            var repositoryName = GetRepositoryName(context.Body);
            
            if (string.IsNullOrEmpty(repositoryName))
            {
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                    "Please specify the repository name in the issue body. Example: `repository: my-new-repo`");
                return;
            }

            try
            {
                var templateInfo = GetTemplateInfo(templateType);
                var newRepository = await _storage.CreateRepositoryFromTemplate(
                    templateInfo.Owner, 
                    templateInfo.Name, 
                    context.Repository.Owner.Login, 
                    repositoryName, 
                    $"Generated {templateType} from template",
                    false
                );

                await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                    $"✅ Successfully created repository [{repositoryName}]({newRepository.HtmlUrl}) from {templateType} template!");
                
                _storage.CloseIssue(context);
            }
            catch (Exception ex)
            {
                await _storage.CreateIssueComment(context.Repository.Id, context.Number, 
                    $"❌ Error creating repository: {ex.Message}");
            }
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
            return title.Contains("hello world template") || 
                   title.Contains("console app template") || 
                   title.Contains("console application template") ||
                   title.Contains("library template");
        }

        private string GetTemplateType(string title)
        {
            if (title.Contains("hello world"))
                return "hello-world";
            if (title.Contains("console app") || title.Contains("console application"))
                return "console-app";
            if (title.Contains("library"))
                return "library";
            return "hello-world"; // default
        }

        private (string Owner, string Name) GetTemplateInfo(string templateType)
        {
            return templateType switch
            {
                "hello-world" => ("linksplatform", "Template.HelloWorld"),
                "console-app" => ("linksplatform", "Template.ConsoleApp"),
                "library" => ("linksplatform", "Template.Library"),
                _ => ("linksplatform", "Template.HelloWorld")
            };
        }

        private string? GetRepositoryName(string? body)
        {
            if (string.IsNullOrEmpty(body))
                return null;

            var match = Regex.Match(body, @"repository:\s*([a-zA-Z0-9._-]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }
    }
}