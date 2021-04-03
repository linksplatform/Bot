using System;
using Octokit;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using csharp;

namespace projects
{
    class Program
    { 
     
        static void Main(string[] args)
        {
            Console.WriteLine("Enter your username");
            string username = Console.ReadLine();
            Console.WriteLine("Enter your token");
            string token = Console.ReadLine();
            Console.WriteLine("Enter name your app");
            string Name = Console.ReadLine();
            var programmer = new Programmer();
            Task.Run(() => programmer.Start(username, token,Name));
            Console.WriteLine("Bot has been started. Press any key to stop");
            Console.ReadLine();
        }
    }
}