using System;

namespace FileManager
{
    public class CreateHandle : IInputHandler
    {
        public string Trigger => "create";

        public bool Run(string[] args,Manager fileManager)
        {
            var time = DateTime.Now;
            fileManager.AddFile(args[1], FileLoader.LoadContent(args[2]));
            Console.WriteLine("Elapsed time: " + (DateTime.Now - time).TotalMilliseconds);
            return true;
        }
    }
}
