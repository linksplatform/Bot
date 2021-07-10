using csharp;
using Interfaces;
using Octokit;
using Platform.Exceptions;
using Platform.IO;
using Storage.Local;
using Storage.Remote.GitHub;
using System;
using System.Collections.Generic;

namespace Bot
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            using ConsoleCancellation cancellation = new ConsoleCancellation();
            string username = ConsoleHelpers.GetOrReadArgument(0, "Username", args);
            string token = ConsoleHelpers.GetOrReadArgument(1, "Token", args);
            string appName = ConsoleHelpers.GetOrReadArgument(2, "App Name", args);
            string databaseFileName = ConsoleHelpers.GetOrReadArgument(3, "Database file name", args);
            string fileSetName = ConsoleHelpers.GetOrReadArgument(4, "File set name ", args);//For defoult Hello World: HelloWorldSet
            FileStorage dbContext = new FileStorage(databaseFileName);
            Console.WriteLine($"Bot has been started. {Environment.NewLine}Press CTRL+C to close");
            try
            {
                GitHubStorage api = new GitHubStorage(username, token, appName);
                new ProgrammerRole(new List<ITrigger<Issue>> { new HelloWorldTrigger(api, dbContext, fileSetName), new OrganizationLastMonthActivityTrigger(api) }, api).Start(cancellation.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToStringWithAllInnerExceptions());
            }
        }
    }
}
