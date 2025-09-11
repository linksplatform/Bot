using System;
using TraderBot.Examples;

namespace TraderBot.Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Kelly Criterion Test Suite");
            Console.WriteLine("==========================");
            
            try
            {
                KellyCriterionTests.RunAllTests();
                
                Console.WriteLine("\n=== Real-world Examples ===");
                DemonstrateRealWorldScenarios();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test execution failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            Console.WriteLine("\n=== Tests completed ===");
        }
        
        private static void DemonstrateRealWorldScenarios()
        {
            Console.WriteLine("\n--- Scenario 1: Conservative Trading ---");
            var result1 = KellyCriterion.CalculateOptimalBetSize(0.52, 1.1, 0.1);
            Console.WriteLine($"52% win rate, 1.1:1 ratio, max 10%: {result1:P1} of capital");
            
            Console.WriteLine("\n--- Scenario 2: Aggressive Trading ---");
            var result2 = KellyCriterion.CalculateOptimalBetSize(0.65, 1.5, 0.5);
            Console.WriteLine($"65% win rate, 1.5:1 ratio, max 50%: {result2:P1} of capital");
            
            Console.WriteLine("\n--- Scenario 3: High Win Rate, Low Profit ---");
            var result3 = KellyCriterion.CalculateOptimalBetSize(0.8, 0.8, 0.25);
            Console.WriteLine($"80% win rate, 0.8:1 ratio, max 25%: {result3:P1} of capital");
            
            Console.WriteLine("\n--- Scenario 4: Breakeven Strategy ---");
            var result4 = KellyCriterion.CalculateOptimalBetSize(0.5, 1.0, 0.25);
            Console.WriteLine($"50% win rate, 1.0:1 ratio, max 25%: {result4:P1} of capital");
        }
    }
}