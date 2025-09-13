using System;
using System.IO;
using Storage.Local;
using Storage.AST;

namespace Storage.Tests
{
    /// <summary>
    /// Example demonstrating how to use the AST transformation functionality.
    /// </summary>
    public class AstUsageExample
    {
        public static void RunExample()
        {
            var dbPath = Path.GetTempFileName();
            try
            {
                using var storage = new FileStorage(dbPath);
                
                // Example C# code to transform into AST
                var csharpCode = @"using System;
namespace MyNamespace
{
    public class Calculator
    {
        private int result;
        
        public int Add(int a, int b)
        {
            result = a + b;
            return result;
        }
        
        public void Reset()
        {
            result = 0;
        }
    }
}";

                Console.WriteLine("Original C# code:");
                Console.WriteLine(csharpCode);
                Console.WriteLine("\n" + new string('=', 50) + "\n");

                // Transform code into AST and store it in links
                var rootAstNodeLink = storage.TransformCodeToAst(csharpCode);
                Console.WriteLine($"Root AST node stored at link address: {rootAstNodeLink}");

                // Get all AST nodes from the links store
                var allAstNodes = storage.GetAllAstNodes();
                Console.WriteLine($"Total AST nodes stored: {allAstNodes.Count}");

                // Display information for each AST node
                Console.WriteLine("\nAST Nodes with position mapping:");
                Console.WriteLine(new string('-', 60));
                
                foreach (var nodeLink in allAstNodes)
                {
                    var nodeInfo = storage.GetAstNodeInfo(nodeLink);
                    if (!nodeInfo.ContainsKey("Error"))
                    {
                        Console.WriteLine($"Link: {nodeLink}");
                        Console.WriteLine($"Type: {nodeInfo["NodeType"]}");
                        Console.WriteLine($"Text: \"{nodeInfo["Text"]}\"");
                        Console.WriteLine($"Has Position Info: {nodeInfo["HasPositionInfo"]}");
                        Console.WriteLine();
                    }
                }

                // Demonstrate direct AST transformer usage
                var transformer = new CSharpAstTransformer();
                var astRoot = transformer.TransformCode(csharpCode);
                
                Console.WriteLine("\nDirect AST Transformation Result:");
                Console.WriteLine(new string('-', 40));
                PrintAstNode(astRoot, 0);

                // Get all nodes with positions
                var allNodes = transformer.GetAllNodesWithPositions(astRoot);
                Console.WriteLine($"\nTotal nodes with position mapping: {allNodes.Count}");
                Console.WriteLine("\nNode position details:");
                Console.WriteLine(new string('-', 80));
                
                foreach (var node in allNodes.Take(10)) // Show first 10 for brevity
                {
                    Console.WriteLine($"{node.NodeType,-20} | Pos: {node.StartPosition,3}-{node.EndPosition,3} | Line: {node.StartLine,2}-{node.EndLine,2} | \"{node.Text.Replace("\n", "\\n").Trim()}\"");
                }
                
                if (allNodes.Count > 10)
                {
                    Console.WriteLine($"... and {allNodes.Count - 10} more nodes");
                }
            }
            finally
            {
                if (System.IO.File.Exists(dbPath))
                {
                    System.IO.File.Delete(dbPath);
                }
            }
        }

        private static void PrintAstNode(AstNode node, int depth)
        {
            var indent = new string(' ', depth * 2);
            var truncatedText = node.Text.Length > 30 ? node.Text.Substring(0, 30) + "..." : node.Text;
            truncatedText = truncatedText.Replace("\n", "\\n").Replace("\r", "\\r");
            
            Console.WriteLine($"{indent}{node.NodeType} [{node.StartLine}:{node.StartColumn}-{node.EndLine}:{node.EndColumn}] \"{truncatedText}\"");
            
            foreach (var child in node.Children)
            {
                PrintAstNode(child, depth + 1);
            }
        }
    }
}

// Extension method to provide Take functionality
public static class EnumerableExtensions
{
    public static System.Collections.Generic.IEnumerable<T> Take<T>(this System.Collections.Generic.IEnumerable<T> source, int count)
    {
        var enumerator = source.GetEnumerator();
        int current = 0;
        while (current < count && enumerator.MoveNext())
        {
            yield return enumerator.Current;
            current++;
        }
    }
}