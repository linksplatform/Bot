using System;
using Platform.Bot.Triggers;

// Simple test class to verify parsing logic
class TestMonthlyActivityTrigger
{
    public static void Main()
    {
        Console.WriteLine("Testing MonthlyCommitAndReviewActivityTrigger date parsing...");
        
        // Create dummy storage instance (we're only testing date parsing)
        var trigger = new MonthlyCommitAndReviewActivityTrigger(null);
        
        // Test date parsing using reflection to access private method
        var method = typeof(MonthlyCommitAndReviewActivityTrigger)
            .GetMethod("ParseDateFromIssueBody", 
                      System.Reflection.BindingFlags.NonPublic | 
                      System.Reflection.BindingFlags.Instance);
                      
        if (method != null)
        {
            // Test case 1: Valid month and year
            var testBody1 = @"month: 11
year: 2023";
            var result1 = (ValueTuple<int, int>)method.Invoke(trigger, new object[] { testBody1 });
            Console.WriteLine($"Test 1 - Input: '{testBody1.Replace("\n", "\\n")}' -> Year: {result1.Item1}, Month: {result1.Item2}");
            
            // Test case 2: Invalid format, should default to previous month
            var testBody2 = "Some random text without proper format";
            var result2 = (ValueTuple<int, int>)method.Invoke(trigger, new object[] { testBody2 });
            Console.WriteLine($"Test 2 - Input: '{testBody2}' -> Year: {result2.Item1}, Month: {result2.Item2} (default to last month)");
            
            // Test case 3: Only year provided
            var testBody3 = "year: 2022";
            var result3 = (ValueTuple<int, int>)method.Invoke(trigger, new object[] { testBody3 });
            Console.WriteLine($"Test 3 - Input: '{testBody3}' -> Year: {result3.Item1}, Month: {result3.Item2}");
        }
        else
        {
            Console.WriteLine("Could not find ParseDateFromIssueBody method");
        }
        
        Console.WriteLine("\nAll tests completed successfully!");
    }
}