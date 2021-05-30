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
            Files.Add(new File() { Path = "Program.cs", Content = dBContext.GetOrLoadFile(@"E:\Projects\Test\HelloWorld\HelloWorld\Program.cs", "Program.cs") });
            Files.Add(new File() { Path = "HelloWorld.csproj", Content = dBContext.GetOrLoadFile(@"E:\Projects\Test\HelloWorld\HelloWorld\HelloWorld.csproj", "HelloWorld.csproj") });
            Files.Add(new File() { Path = "CD.yml", Content = dBContext.GetOrLoadFile(@"E:\Projects\Test\HelloWorld\HelloWorld\CD.yml", "CD.yml") });
            return Files;
        }
    }
}
