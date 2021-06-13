using Interfaces;
using Storage;
using System;

namespace FileManager
{
    public class LinksPrinterHandler : ITrigger<Arguments>
    {
        public bool Condition(Arguments arguments)
        {
            if (arguments.Args[0].ToLower() == "print")
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
            Console.WriteLine(arguments.FileStorage.AllLinksToString());
        }
    }
}
