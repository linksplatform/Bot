using Interfaces;
using Storage;
using System;

namespace FileManager
{
    public class LinksPrinterHandler : ITrigger<Arguments>
    {
        public bool Condition(Arguments arguments)
        {
            return (arguments.Args[0].ToLower() == "print");
        }

        public void Action(Arguments arguments)
        {
            Console.WriteLine(arguments.FileStorage.AllLinksToString());
        }
    }
}
