using csharp;
using System.Collections.Generic;

namespace GitHubBot
{
    internal class CSharpHelloWorld { 

        public static readonly List<File> files = new List<File>
        { 
            new File { Path = "program.cs", Content = ProgramCs },
            new File { Path = "HelloWorld.csproj", Content = ProgramCsproj },
            new File { Path = "CD.yml", Content = dotnetYml }
        };

        static readonly string ProgramCs =
  @"
using System;

namespace helloworld
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""Hello World!"");
        }
    }
}";

        static readonly string ProgramCsproj =
 @"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
     <OutputType>Exe</OutputType>
    <TargetFramework>net5.0</TargetFramework>
   
     </PropertyGroup>
   

</Project> ";

        static readonly string dotnetYml =
 @"name: .NET

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:

    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v2
    - name: Run 
      run: dotnet run";

    }
}
