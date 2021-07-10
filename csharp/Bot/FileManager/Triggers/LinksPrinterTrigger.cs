using Interfaces;
using Storage;
using System;

namespace FileManager
{
    public class LinksPrinterTrigger : ITrigger<Context>
    {
        public bool Condition(Context arguments)
        {
            return (arguments.Args[0].ToLower() == "print");
        }

        public void Action(Context arguments)
        {
            Console.WriteLine(arguments.FileStorage.AllLinksToString());
        }
    }
}
