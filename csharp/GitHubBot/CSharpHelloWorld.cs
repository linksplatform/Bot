using csharp;
using Database;
using System;
using System.Collections.Generic;

namespace GitHubBot
{
    class CSharpHelloWorld
    {
        public static List<File> Files(DBContext dBContext)
        {
            var Files = new List<File>() { };
            Files.Add(new File() { Path = "Program.cs", Content = dBContext.GetFile("Program.cs") });
            Files.Add(new File() { Path = "HelloWorld.csproj", Content = dBContext.GetFile("HelloWorld.csproj") });
            Files.Add(new File() { Path = "CD.yml", Content = dBContext.GetFile("CD.yml") });
            return Files;
        }
    }
}
