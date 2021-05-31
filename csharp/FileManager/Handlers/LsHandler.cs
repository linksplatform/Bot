using FileManager;
using System;

namespace FileManager
{
    class LsHandler : IInputHandler
    {
        public string Trigger => "ls";

        public bool Run(string[] args, Manager fileManager)
        {
            Console.WriteLine(fileManager.GetAllLinks());
            return true;
        }
    }
}
