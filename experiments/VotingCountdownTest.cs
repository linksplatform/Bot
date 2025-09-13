using System;
using System.Collections.Generic;
using System.Linq;

namespace VotingCountdownExperiments
{
    /// <summary>
    /// <para>
    /// Test simulation to demonstrate the voting countdown logic.
    /// This helps verify that our VotingCountdownTrigger logic works correctly.
    /// </para>
    /// </summary>
    public class VotingCountdownTest
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== Voting Countdown Logic Test ===");
            Console.WriteLine("Testing scenarios for preventing rapid '-' responses\n");

            // Test Scenario 1: Valid behavior - sufficient time between votes
            TestScenario1();

            // Test Scenario 2: Invalid behavior - too quick response
            TestScenario2();

            // Test Scenario 3: Multiple users voting
            TestScenario3();

            Console.WriteLine("=== All tests completed ===");
        }

        private static void TestScenario1()
        {
            Console.WriteLine("Scenario 1: Valid behavior - 6 minutes between '-' votes");
            
            var countdownMinutes = 5;
            var comment1Time = DateTimeOffset.UtcNow.AddMinutes(-10);
            var comment2Time = DateTimeOffset.UtcNow.AddMinutes(-4); // 6 minutes later
            
            var timeDiff = comment2Time - comment1Time;
            var isValid = timeDiff >= TimeSpan.FromMinutes(countdownMinutes);
            
            Console.WriteLine($"  First '-' comment: {comment1Time:HH:mm:ss}");
            Console.WriteLine($"  Second '-' comment: {comment2Time:HH:mm:ss}");
            Console.WriteLine($"  Time difference: {timeDiff.TotalMinutes:F1} minutes");
            Console.WriteLine($"  Required minimum: {countdownMinutes} minutes");
            Console.WriteLine($"  Result: {(isValid ? "ALLOWED" : "BLOCKED")}");
            Console.WriteLine($"  Expected: ALLOWED\n");
        }

        private static void TestScenario2()
        {
            Console.WriteLine("Scenario 2: Invalid behavior - 2 minutes between '-' votes");
            
            var countdownMinutes = 5;
            var comment1Time = DateTimeOffset.UtcNow.AddMinutes(-7);
            var comment2Time = DateTimeOffset.UtcNow.AddMinutes(-5); // Only 2 minutes later
            
            var timeDiff = comment2Time - comment1Time;
            var isValid = timeDiff >= TimeSpan.FromMinutes(countdownMinutes);
            
            Console.WriteLine($"  First '-' comment: {comment1Time:HH:mm:ss}");
            Console.WriteLine($"  Second '-' comment: {comment2Time:HH:mm:ss}");
            Console.WriteLine($"  Time difference: {timeDiff.TotalMinutes:F1} minutes");
            Console.WriteLine($"  Required minimum: {countdownMinutes} minutes");
            Console.WriteLine($"  Result: {(isValid ? "ALLOWED" : "BLOCKED")}");
            Console.WriteLine($"  Expected: BLOCKED\n");
        }

        private static void TestScenario3()
        {
            Console.WriteLine("Scenario 3: Multiple users - different rules for different users");
            
            var countdownMinutes = 5;
            var baseTime = DateTimeOffset.UtcNow.AddMinutes(-10);
            
            // Simulate comments from different users
            var comments = new List<(string user, DateTimeOffset time, string content)>
            {
                ("user1", baseTime, "-"),
                ("user2", baseTime.AddMinutes(2), "-"), // 2 minutes later, different user
                ("user1", baseTime.AddMinutes(3), "-"), // 3 minutes after user1's first vote - should be blocked
                ("user3", baseTime.AddMinutes(4), "-"), // 4 minutes later, different user
                ("user1", baseTime.AddMinutes(7), "-")  // 7 minutes after user1's first vote - should be allowed
            };

            var userVoteTimes = new Dictionary<string, DateTimeOffset>();

            foreach (var comment in comments)
            {
                if (comment.content == "-")
                {
                    var shouldBlock = false;
                    if (userVoteTimes.ContainsKey(comment.user))
                    {
                        var timeSinceLastVote = comment.time - userVoteTimes[comment.user];
                        if (timeSinceLastVote < TimeSpan.FromMinutes(countdownMinutes))
                        {
                            shouldBlock = true;
                        }
                    }
                    
                    Console.WriteLine($"  {comment.user} votes '-' at {comment.time:HH:mm:ss} -> {(shouldBlock ? "BLOCKED" : "ALLOWED")}");
                    
                    if (!shouldBlock)
                    {
                        userVoteTimes[comment.user] = comment.time;
                    }
                }
            }
            Console.WriteLine();
        }
    }
}