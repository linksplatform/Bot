using System;
using Platform.IO;

namespace GitHubBot
{
    class Program
    { 
        static void Main(string[] args)
        {
            using (var cancellation = new ConsoleCancellation())
            {
                var username = ConsoleHelpers.GetOrReadArgument(0, "Username", args);
                var token = ConsoleHelpers.GetOrReadArgument(1, "Token", args);
                var appName =ConsoleHelpers.GetOrReadArgument(2, "App Name", args);
                Console.WriteLine("Bot has been started.\nPress CTRL+C to close");
                new Programmer().Start(username, token, appName, cancellation);
            }
        }
    }
}