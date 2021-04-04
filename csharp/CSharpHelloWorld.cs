using csharp;
using System.Collections.Generic;

namespace GitHubBot
{
    internal class CSharpHelloWorld { 

        public static readonly List<File> files = new List<File>
        { 
            new File
            {
                Path = "program.cs",
                Content = 
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
}"
            },
            new File
            {
                Path = "HelloWorld.csproj",
                Content =
@"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
     <OutputType>Exe</OutputType>
    <TargetFramework>net5.0</TargetFramework>
   
     </PropertyGroup>
   

</Project>"
            },
            new File
            { 
                Path = "CD.yml",
                Content = 
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
      run: dotnet run"
            }
        };
    }
}
