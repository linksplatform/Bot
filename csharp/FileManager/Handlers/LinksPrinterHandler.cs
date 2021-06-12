using System;

namespace FileManager
{
    public class LinksPrinterHandler : IInputHandler
    {
        public string Trigger => "print";

        public bool Run(string[] args, Manager fileManager)
        {
            Console.WriteLine(fileManager.AllLinksToString());
            return true;
        }
    }
}
