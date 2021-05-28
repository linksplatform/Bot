using Platform.Exceptions;
using Platform.IO;
using System;

namespace GitHubBot
{
    class Program
    {
        private static void Main(string[] args)
        {
            using ConsoleCancellation cancellation = new ConsoleCancellation();
            var username = ConsoleHelpers.GetOrReadArgument(0, "Username", args);
            var token = ConsoleHelpers.GetOrReadArgument(1, "Token", args);
            var appName = ConsoleHelpers.GetOrReadArgument(2, "App Name", args);
            var DatabaseFileName = ConsoleHelpers.GetOrReadArgument(3, "Database file name", args);
            var dbContext = new Database.DBContext(DatabaseFileName);
            Console.WriteLine("Bot has been started.\nPress CTRL+C to close");
            try
            {
                new Programmer(username, token, appName , dbContext).Start(cancellation.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToStringWithAllInnerExceptions());
            }
        }
    }
}