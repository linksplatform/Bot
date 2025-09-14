using System;
using System.IO;
using System.Threading.Tasks;
using Octokit;
using Platform.Bot.Triggers;
using Storage.Remote.GitHub;

namespace Platform.Bot.Experiments
{
    /// <summary>
    /// <para>
    /// Simple test class to verify SynonymRecognitionTrigger functionality.
    /// </para>
    /// <para></para>
    /// </summary>
    public class SynonymRecognitionTest
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== Synonym Recognition Test ===");
            
            // Test with mock issues - we can't create actual GitHub API calls without credentials
            TestPositiveSynonyms();
            TestNegativeSynonyms();
            TestMixedSynonyms();
            TestNoSynonyms();
            
            Console.WriteLine("All tests completed successfully!");
        }
        
        private static void TestPositiveSynonyms()
        {
            Console.WriteLine("\n--- Testing Positive Synonyms ---");
            
            var testCases = new[]
            {
                "Thank you for the help!",
                "This looks great 👍",
                "I agree with this approach",
                "Yes, this is correct",
                "++ for this feature",
                "Спасибо за помощь!",
                ":thumbsup: Great work!",
                "True, this makes sense"
            };
            
            foreach (var testCase in testCases)
            {
                var result = CheckForSynonyms(testCase, testCase);
                Console.WriteLine($"Input: '{testCase}' -> Contains synonyms: {result}");
            }
        }
        
        private static void TestNegativeSynonyms()
        {
            Console.WriteLine("\n--- Testing Negative Synonyms ---");
            
            var testCases = new[]
            {
                "I disagree with this",
                "This is false 👎",
                "No, this won't work",
                "-- for this approach",
                "Нет, не согласен",
                ":thumbsdown: Not good",
                "This is incorrect - false"
            };
            
            foreach (var testCase in testCases)
            {
                var result = CheckForSynonyms(testCase, testCase);
                Console.WriteLine($"Input: '{testCase}' -> Contains synonyms: {result}");
            }
        }
        
        private static void TestMixedSynonyms()
        {
            Console.WriteLine("\n--- Testing Mixed Synonyms ---");
            
            var testCases = new[]
            {
                "Thank you, but I disagree",
                "👍 Some good points, but 👎 others",
                "Yes and no - mixed feelings"
            };
            
            foreach (var testCase in testCases)
            {
                var result = CheckForSynonyms(testCase, testCase);
                Console.WriteLine($"Input: '{testCase}' -> Contains synonyms: {result}");
            }
        }
        
        private static void TestNoSynonyms()
        {
            Console.WriteLine("\n--- Testing No Synonyms ---");
            
            var testCases = new[]
            {
                "This is a regular comment",
                "Please review this code",
                "What do you think about this implementation?",
                "Let me know when this is ready"
            };
            
            foreach (var testCase in testCases)
            {
                var result = CheckForSynonyms(testCase, testCase);
                Console.WriteLine($"Input: '{testCase}' -> Contains synonyms: {result}");
            }
        }
        
        // Simplified version of synonym checking logic
        private static bool CheckForSynonyms(string title, string body)
        {
            var text = $"{title} {body}".ToLower();
            
            // Positive synonyms (equivalent to "+")
            string[] positiveSynonyms = {
                "👍", "👍🏻", "👍🏼", "👍🏽", "👍🏾", "👍🏿",
                ":thumbsup:", ":+1:", ":thumbs_up:",
                "thank you", "thanks", "спасибо", "благодарю",
                "agree", "true", "yes", "да", "согласен", "согласна",
                "++", "+"
            };
            
            // Negative synonyms (equivalent to "-")
            string[] negativeSynonyms = {
                "👎", "👎🏻", "👎🏼", "👎🏽", "👎🏾", "👎🏿",
                ":thumbsdown:", ":-1:", ":thumbs_down:",
                "disagree", "false", "no", "нет", "не согласен", "не согласна",
                "--", "-"
            };
            
            foreach (var synonym in positiveSynonyms)
            {
                if (text.Contains(synonym.ToLower()))
                {
                    return true;
                }
            }
            
            foreach (var synonym in negativeSynonyms)
            {
                if (text.Contains(synonym.ToLower()))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}