using Platform.Exceptions;
using Platform.IO;
using System;

namespace GitHubBot
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            using (ConsoleCancellation cancellation = new ConsoleCancellation())
            {
                string username = ConsoleHelpers.GetOrReadArgument(0, "Username", args);
                string token = ConsoleHelpers.GetOrReadArgument(1, "Token", args);
                string appName = ConsoleHelpers.GetOrReadArgument(2, "App Name", args);
                Console.WriteLine("Bot has been started.\nPress CTRL+C to close");
                try 
                {
                    new Programmer(username, token, appName).Start(cancellation.Token);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToStringWithAllInnerExceptions());
                }
            }
        }
    }
}