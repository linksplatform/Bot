using System;

namespace FileManager
{
    public class HelpHandler : IInputHandler
    {
        public string Trigger => "help";

        public bool Run(string[] args, Manager fileManager)
        {
            Console.WriteLine("Use this program to manage links in your links repository. For close just press CTRL+C.\n\n " +
                "Avalible commands:\n" +
                "1. Delete [addres]\n" +
                "2. Create [addres] [path to file]\n" +
                "3. Help\n" +
                "4. Ls\n" +
                "5. Show [addres]");
            return true;
        }
    }
}
