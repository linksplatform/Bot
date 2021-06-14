using System;
using Interfaces;
using Storage;

namespace FileManager
{
    public class CreateTrigger : ITrigger<Context>
    {
        public bool Condition(Context arguments)
        {
            return arguments.Args[0].ToLower() == "create";
        }

        public void Action(Context arguments)
        {
            var time = DateTime.Now;
            Console.WriteLine(arguments.FileStorage.AddFile(arguments.Args[1], FileLoader.LoadContent(arguments.Args[2])));
            Console.WriteLine("Elapsed time: " + (DateTime.Now - time).TotalMilliseconds);
        }
    }
}
