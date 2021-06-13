using System;
using Interfaces;
using Storage;


namespace FileManager
{
    public class HelpHandler : ITrigger<Arguments>
    {
        public bool Condition(Arguments arguments)
        {
            if (arguments.Args[0].ToLower() == "help")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Action(Arguments arguments)
        {
            Console.WriteLine("Use this program to manage links in your links repository. For close just press CTRL+C.\n\n " +
                "Avalible commands:\n" +
                "1. Delete [addres]\n" +
                "2. Create [addres] [path to file]\n" +
                "3. Help\n" +
                "4. Print\n" +
                "5. Show [addres]");
        }
    }
}
