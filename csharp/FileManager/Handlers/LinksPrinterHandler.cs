using Storage;
using System;

namespace FileManager
{
    public class LinksPrinterHandler : IInputHandler
    {
        public string Trigger => "print";

        public bool Run(string[] args, FileStorage fileManager)
        {
            Console.WriteLine(fileManager.AllLinksToString());
            return true;
        }
    }
}
