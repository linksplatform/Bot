using System;
using System.Threading.Tasks;
using Octokit;
using Platform.Bot.Triggers;

namespace TestTrigger
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Test trigger condition logic without actual GitHub API calls
            var trigger = new CallUsersByLanguageTrigger(null);
            
            // Test cases
            var testIssues = new[]
            {
                new { Body = "! C++", Expected = true, Description = "Valid C++ call" },
                new { Body = "! Python", Expected = true, Description = "Valid Python call" },
                new { Body = "!JavaScript", Expected = true, Description = "Valid JavaScript call without space" },
                new { Body = "Hello world", Expected = false, Description = "Regular issue body" },
                new { Body = "!", Expected = false, Description = "Just exclamation mark" },
                new { Body = "", Expected = false, Description = "Empty body" },
            };
            
            Console.WriteLine("Testing CallUsersByLanguageTrigger conditions:");
            Console.WriteLine("=" + new string('=', 50));
            
            foreach (var test in testIssues)
            {
                var mockIssue = CreateMockIssue(test.Body);
                var result = await trigger.Condition(mockIssue);
                var status = result == test.Expected ? "✓ PASS" : "✗ FAIL";
                Console.WriteLine($"{status} {test.Description}: '{test.Body}' -> {result} (expected: {test.Expected})");
            }
        }
        
        static Issue CreateMockIssue(string body)
        {
            // Create a basic mock issue with the given body
            // Note: This is a simplified mock for testing purposes
            return new Issue(
                url: "https://api.github.com/repos/test/test/issues/1",
                htmlUrl: "https://github.com/test/test/issues/1",
                commentsUrl: "https://api.github.com/repos/test/test/issues/1/comments",
                eventsUrl: "https://api.github.com/repos/test/test/issues/1/events",
                number: 1,
                state: ItemState.Open,
                title: "Test Issue",
                body: body,
                user: null,
                labels: null,
                assignee: null,
                assignees: null,
                milestone: null,
                comments: 0,
                pullRequest: null,
                closedAt: null,
                createdAt: DateTimeOffset.Now,
                updatedAt: DateTimeOffset.Now,
                closedBy: null,
                nodeId: "test",
                locked: false,
                repository: null,
                reactions: null,
                activeLockReason: null,
                stateReason: null
            );
        }
    }
}