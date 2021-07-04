using csharp;
using Interfaces;
using Octokit;
using Platform.Exceptions;
using Platform.IO;
using Storage;
using Storage.Local;
using Storage.Remote.GitHub;
using System;
using System.Collections.Generic;

namespace Bot
{
    class Program
    {
        private static void Main(string[] args)
        {
            using ConsoleCancellation cancellation = new ConsoleCancellation();
            var username = ConsoleHelpers.GetOrReadArgument(0, "Username", args);
            var token = ConsoleHelpers.GetOrReadArgument(1, "Token", args);
            var appName = ConsoleHelpers.GetOrReadArgument(2, "App Name", args);
            var databaseFileName = ConsoleHelpers.GetOrReadArgument(3, "Database file name", args);
            var fileSetName = ConsoleHelpers.GetOrReadArgument(4, "File set name ", args);//For defoult Hello World: HelloWorldSet
            var dbContext = new FileStorage(databaseFileName);
            Console.WriteLine($"Bot has been started. {Environment.NewLine}Press CTRL+C to close");
            try
            {
                var api = new GitHubStorage(username,token,appName);
                new ProgrammerRole(new List<ITrigger<Issue>> { new HelloWorldTrigger(api,dbContext,fileSetName)}, api).Start(cancellation.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToStringWithAllInnerExceptions());
            }
        }
    }
}
