using System;

namespace FileManager
{
    class Help : IInputHandler
    {
        public string Trigger => "Help";

        public bool Run(string[] args, Manager fileManager)
        {
            Console.WriteLine("Use this program to manage links in your links repository. For close just press CTRL+C.\n\n " +
                "Avalible commands:\n" +
                "1. Delete [addres]\n" +
                "2. Create [addres] [path to file]\n" +
                "3. Help\n");
            return true;
        }
    }
}
