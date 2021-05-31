
using System;

namespace FileManager
{
    class CreateHandle : IInputHandler
    {
        public string Trigger => "create";

        public bool Run(string[] args,Manager fileManager)
        {
            if (fileManager.LinkExist(args[1]) == false)
            {
                var time = DateTime.Now;
                fileManager.AddFile(args[1], FileLoader.LoadContent(args[2]));
                Console.WriteLine("Elapsed time: " + (DateTime.Now - time).TotalMilliseconds);
                return true;
            }
            else
            {
                Console.WriteLine("File already exist");
                return false;
            }
        }
    }
}
