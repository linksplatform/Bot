using System;
using Storage;

namespace FileManager
{
    public class CreateHandle : ITrigger
    {
        public string Trigger => "create";

        public bool Run(string[] args, FileStorage fileManager)
        {
            var time = DateTime.Now;
            Console.WriteLine(fileManager.AddFile(args[1], FileLoader.LoadContent(args[2])));
            Console.WriteLine("Elapsed time: " + (DateTime.Now - time).TotalMilliseconds);
            return true;
        }
    }
}
