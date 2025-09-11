using System;
using System.Collections.Generic;
using System.Linq;
using TraderBot;

namespace TraderBot.Examples
{
    public class KellyCriterionTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Kelly Criterion Tests ===");
            
            TestBasicKellyCalculation();
            TestNegativeExpectedValue();
            TestMaximumFractionLimit();
            TestEdgeCases();
            TestHistoricalMetrics();
            
            Console.WriteLine("=== All tests completed ===");
        }
        
        private static void TestBasicKellyCalculation()
        {
            Console.WriteLine("\n--- Test: Basic Kelly Calculation ---");
            
            // Test case: 60% win rate, 2:1 profit/loss ratio
            var winProbability = 0.6;
            var profitLossRatio = 2.0;
            var result = KellyCriterion.CalculateOptimalBetSize(winProbability, profitLossRatio, 1.0);
            
            // Expected: (2 * 0.6 - 0.4) / 2 = 0.4
            var expected = 0.4;
            Console.WriteLine($"Win Rate: {winProbability}, P/L Ratio: {profitLossRatio}");
            Console.WriteLine($"Expected: {expected:F3}, Actual: {result:F3}");
            
            if (Math.Abs(result - expected) < 0.001)
                Console.WriteLine("✓ PASS");
            else
                Console.WriteLine("✗ FAIL");
        }
        
        private static void TestNegativeExpectedValue()
        {
            Console.WriteLine("\n--- Test: Negative Expected Value ---");
            
            // Test case: 40% win rate, 1:1 profit/loss ratio (negative expected value)
            var winProbability = 0.4;
            var profitLossRatio = 1.0;
            var result = KellyCriterion.CalculateOptimalBetSize(winProbability, profitLossRatio);
            
            Console.WriteLine($"Win Rate: {winProbability}, P/L Ratio: {profitLossRatio}");
            Console.WriteLine($"Result: {result:F3}");
            
            if (result == 0.0)
                Console.WriteLine("✓ PASS - Correctly returns 0 for negative expected value");
            else
                Console.WriteLine("✗ FAIL - Should return 0 for negative expected value");
        }
        
        private static void TestMaximumFractionLimit()
        {
            Console.WriteLine("\n--- Test: Maximum Fraction Limit ---");
            
            // Test case: Very high Kelly fraction that should be capped
            var winProbability = 0.9;
            var profitLossRatio = 10.0;
            var maxFraction = 0.25;
            var result = KellyCriterion.CalculateOptimalBetSize(winProbability, profitLossRatio, maxFraction);
            
            Console.WriteLine($"Win Rate: {winProbability}, P/L Ratio: {profitLossRatio}, Max: {maxFraction}");
            Console.WriteLine($"Result: {result:F3}");
            
            if (result <= maxFraction)
                Console.WriteLine("✓ PASS - Correctly capped at maximum fraction");
            else
                Console.WriteLine("✗ FAIL - Should be capped at maximum fraction");
        }
        
        private static void TestEdgeCases()
        {
            Console.WriteLine("\n--- Test: Edge Cases ---");
            
            try
            {
                // Test invalid win probability
                KellyCriterion.CalculateOptimalBetSize(-0.1, 1.0);
                Console.WriteLine("✗ FAIL - Should throw exception for negative win probability");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("✓ PASS - Correctly throws exception for negative win probability");
            }
            
            try
            {
                // Test invalid profit/loss ratio
                KellyCriterion.CalculateOptimalBetSize(0.5, -1.0);
                Console.WriteLine("✗ FAIL - Should throw exception for negative profit/loss ratio");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("✓ PASS - Correctly throws exception for negative profit/loss ratio");
            }
        }
        
        private static void TestHistoricalMetrics()
        {
            Console.WriteLine("\n--- Test: Historical Metrics Calculation ---");
            
            var operations = new List<(DateTime, decimal, decimal)>
            {
                (DateTime.Now.AddDays(-10), 100m, 110m), // Win: +10
                (DateTime.Now.AddDays(-9), 100m, 95m),   // Loss: -5
                (DateTime.Now.AddDays(-8), 100m, 108m),  // Win: +8
                (DateTime.Now.AddDays(-7), 100m, 92m),   // Loss: -8
                (DateTime.Now.AddDays(-6), 100m, 105m),  // Win: +5
                (DateTime.Now.AddDays(-5), 100m, 98m),   // Loss: -2
                (DateTime.Now.AddDays(-4), 100m, 112m),  // Win: +12
                (DateTime.Now.AddDays(-3), 100m, 97m),   // Loss: -3
                (DateTime.Now.AddDays(-2), 100m, 106m),  // Win: +6
                (DateTime.Now.AddDays(-1), 100m, 104m),  // Win: +4
            };
            
            var (winProb, profitLossRatio) = KellyCriterion.CalculateHistoricalMetrics(operations);
            
            // Expected: 6 wins out of 10 = 60% win rate
            // Average win: (10+8+5+12+6+4)/6 = 7.5
            // Average loss: (5+8+2+3)/4 = 4.5
            // Profit/Loss ratio: 7.5/4.5 = 1.67
            
            Console.WriteLine($"Win Probability: {winProb:F3} (expected ~0.600)");
            Console.WriteLine($"Profit/Loss Ratio: {profitLossRatio:F3} (expected ~1.667)");
            
            if (Math.Abs(winProb - 0.6) < 0.001 && Math.Abs(profitLossRatio - 1.667) < 0.01)
                Console.WriteLine("✓ PASS - Historical metrics calculated correctly");
            else
                Console.WriteLine("✗ FAIL - Historical metrics calculation error");
        }
    }
}