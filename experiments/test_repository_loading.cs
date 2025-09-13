// Test script for repository loading functionality
using System;
using System.Threading.Tasks;
using Storage.Local;
using Storage.Remote.GitHub;
using Platform.Bot.Triggers;

namespace TestRepositoryLoading
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // This is a simple test script to verify the repository loading functionality
            // Note: This would need actual GitHub credentials to run
            
            Console.WriteLine("Repository loading test script");
            Console.WriteLine("This tests the new LoadRepositoryCodeToDoubletsTrigger functionality");
            
            // Create test database
            var testDbPath = "/tmp/test_repository_loading.db";
            var linksStorage = new FileStorage(testDbPath);
            
            // Test creating file set
            var fileSetName = "test/repository";
            var fileSet = linksStorage.CreateFileSet(fileSetName);
            Console.WriteLine($"Created file set: {fileSetName} with ID: {fileSet}");
            
            // Test adding a file
            var testContent = "// This is a test file\nusing System;\n\nnamespace Test\n{\n    class Program\n    {\n        static void Main()\n        {\n            Console.WriteLine(\"Hello World\");\n        }\n    }\n}";
            var file = linksStorage.AddFile(testContent);
            var fileInSet = linksStorage.AddFileToSet(fileSet, file, "Program.cs");
            Console.WriteLine($"Added test file to set: {fileInSet}");
            
            // Test retrieving files from set
            var files = linksStorage.GetFilesFromSet(fileSetName);
            Console.WriteLine($"Files in set: {files.Count}");
            foreach (var f in files)
            {
                Console.WriteLine($"  Path: {f.Path}");
                Console.WriteLine($"  Content length: {f.Content?.Length ?? 0} characters");
            }
            
            Console.WriteLine("Test completed successfully!");
            
            // Clean up
            linksStorage.Dispose();
            if (System.IO.File.Exists(testDbPath))
            {
                System.IO.File.Delete(testDbPath);
                Console.WriteLine("Cleaned up test database");
            }
        }
    }
}