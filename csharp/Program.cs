using csharp;
using Platform.Exceptions;
using Platform.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace GitHubBot
{
    internal class Program
    {
        private static bool IsHelloWorldDisabled(string[] args)
        {
            foreach(var a in args)
            {
                if (a.Contains("-hwDisable"))
                {
                    return true;
                }
            }
            return false;
        }
        
        private static void Main(string[] args)
        {
            using ConsoleCancellation cancellation = new ConsoleCancellation();
            var username = ConsoleHelpers.GetOrReadArgument(0, "Username", args);
            var token = ConsoleHelpers.GetOrReadArgument(1, "Token", args);
            var appName = ConsoleHelpers.GetOrReadArgument(2, "App Name", args);
            List<string> Cfgfiles = new List<string>() { };
            foreach (var file in args)
            {
                if (file.Contains(".json"))
                {
                    Cfgfiles.Add(file);
                }
            }
            var files = FileHandler.HandleAsync(Cfgfiles).Result;
            try
            {
                if (IsHelloWorldDisabled(args))
                {
                    new Programmer(username, token, appName,true, files).Start(cancellation.Token);
                }
                else
                {
                    new Programmer(username, token, appName,files).Start(cancellation.Token);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToStringWithAllInnerExceptions());
            }
        }
    }
}
