
namespace GitHubBot
{
    internal class CSharpHelloWorld
    {
        public static readonly string ProgramCs =
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

        public static readonly string ProgramCsproj =
 @"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
     <OutputType>Exe</OutputType>
    <TargetFramework>net5.0</TargetFramework>
   
     </PropertyGroup>
   

</Project> ";

        public static readonly string dotnetYml =
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
