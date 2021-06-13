using Storage;
using System;

namespace FileManager
{
    public class ShowHandler : IInputHandler
    {
        public string Trigger => "show";

        public bool Run(string[] args, FileStorage fileManager)
        {
            Console.WriteLine(fileManager.PutFile(args[1]));
            return true;
        }
    }
}
