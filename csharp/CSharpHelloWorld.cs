
namespace csharp
{
    class CSharpHelloWorld
    {
        public static string ProgramCs { get => 
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
}"; }

        public static string ProgramCsproj { get =>
@"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
     <OutputType>Exe</OutputType>
    <TargetFramework>net5.0</TargetFramework>
   
     </PropertyGroup>
   

</Project> ";}

        public static string dotnetYml { get => 
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
      run: dotnet run"; }

    }
}
