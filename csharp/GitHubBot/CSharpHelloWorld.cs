using csharp;
using Storage;
using System;
using System.Collections.Generic;

namespace GitHubBot
{
    class CSharpHelloWorld
    {
        public static List<File> Files(FileStorage dBContext)
        {
            var Files = new List<File>() { };
            Files.Add(new File() { Path = "Program.cs", Content = dBContext.PutFile("Program.cs") });
            Files.Add(new File() { Path = "HelloWorld.csproj", Content = dBContext.PutFile("HelloWorld.csproj") });
            Files.Add(new File() { Path = ".github/workflows/CD.yml", Content = dBContext.PutFile("CD.yml") });
            return Files;
        }
    }
}
