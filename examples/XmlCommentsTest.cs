using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Platform.Bot.Triggers;

namespace Example
{
    class XmlCommentsTest
    {
        static async Task Main(string[] args)
        {
            var testCode = @"using System;

namespace Example
{
    public class TestClass
    {
        public string Name { get; set; }
        
        public int Count { get; set; }
        
        public TestClass(string name)
        {
            Name = name;
        }
        
        public void DoSomething()
        {
            Console.WriteLine($""Doing something with {Name}"");
        }
        
        public string GetFormattedName()
        {
            return $""Name: {Name}"";
        }
        
        public void SetCount(int value)
        {
            Count = value;
        }
        
        public bool CreateNewInstance()
        {
            return true;
        }
    }
}";
            
            // Test the XML comment generation
            var tree = CSharpSyntaxTree.ParseText(testCode);
            var root = await tree.GetRootAsync();
            var rewriter = new XmlCommentsRewriter();
            var newRoot = rewriter.Visit(root);
            
            Console.WriteLine("Generated XML comments:");
            Console.WriteLine(newRoot.ToFullString());
        }
    }
}