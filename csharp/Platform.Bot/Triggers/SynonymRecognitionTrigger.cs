using System.Threading.Tasks;
using Interfaces;
using Octokit;
using Storage.Remote.GitHub;
using System.Text.RegularExpressions;
using System.Linq;

namespace Platform.Bot.Triggers
{
    using TContext = Issue;
    /// <summary>
    /// <para>
    /// Represents the synonym recognition trigger that recognizes positive and negative synonyms.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{TContext}"/>
    internal class SynonymRecognitionTrigger : ITrigger<TContext>
    {
        private readonly GitHubStorage _storage;
        
        // Positive synonyms (equivalent to "+")
        private readonly string[] _positiveSynonyms = {
            // Thumbs-up emojis and Unicode
            "👍", "👍🏻", "👍🏼", "👍🏽", "👍🏾", "👍🏿",
            ":thumbsup:", ":+1:", ":thumbs_up:",
            
            // Expressions of gratitude
            "thank you", "thanks", "спасибо", "благодарю",
            
            // Expressions of agreement
            "agree", "true", "yes", "да", "согласен", "согласна",
            
            // Plus operators
            "++", "+"
        };
        
        // Negative synonyms (equivalent to "-")
        private readonly string[] _negativeSynonyms = {
            // Thumbs-down emojis and Unicode
            "👎", "👎🏻", "👎🏼", "👎🏽", "👎🏾", "👎🏿",
            ":thumbsdown:", ":-1:", ":thumbs_down:",
            
            // Expressions of disagreement
            "disagree", "false", "no", "нет", "не согласен", "не согласна",
            
            // Minus operators
            "--", "-"
        };

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="SynonymRecognitionTrigger"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>A git hub storage.</para>
        /// <para></para>
        /// </param>
        public SynonymRecognitionTrigger(GitHubStorage storage)
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
            var sentiment = GetSentiment(context);
            var comment = $"Recognized sentiment: {sentiment}";
            
            // Create a comment on the issue with the recognized sentiment
            await _storage.Client.Issue.Comment.Create(context.Repository.Id, context.Number, comment);
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
            var text = $"{context.Title} {context.Body}".ToLower();
            return ContainsAnySynonym(text);
        }

        /// <summary>
        /// <para>
        /// Gets the sentiment from the issue content.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The context.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The sentiment string</para>
        /// <para></para>
        /// </returns>
        private string GetSentiment(TContext context)
        {
            var text = $"{context.Title} {context.Body}".ToLower();
            
            var positiveMatches = _positiveSynonyms.Count(synonym => text.Contains(synonym.ToLower()));
            var negativeMatches = _negativeSynonyms.Count(synonym => text.Contains(synonym.ToLower()));
            
            if (positiveMatches > negativeMatches)
            {
                return "Positive (+)";
            }
            else if (negativeMatches > positiveMatches)
            {
                return "Negative (-)";
            }
            else if (positiveMatches == negativeMatches && positiveMatches > 0)
            {
                return "Neutral (mixed positive and negative)";
            }
            else
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// <para>
        /// Checks if text contains any synonym.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="text">
        /// <para>The text to check.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>True if contains any synonym</para>
        /// <para></para>
        /// </returns>
        private bool ContainsAnySynonym(string text)
        {
            return _positiveSynonyms.Any(synonym => text.Contains(synonym.ToLower())) ||
                   _negativeSynonyms.Any(synonym => text.Contains(synonym.ToLower()));
        }
    }
}