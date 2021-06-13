using System;
using Interfaces;
using Storage;

namespace FileManager
{
    public class CreateHandle : ITrigger<Arguments>
    {
        public bool Condition(Arguments arguments)
        {
            if (arguments.Args[0].ToLower() == "create")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Action(Arguments arguments)
        {
            var time = DateTime.Now;
            Console.WriteLine(arguments.FileStorage.AddFile(arguments.Args[1], FileLoader.LoadContent(arguments.Args[2])));
            Console.WriteLine("Elapsed time: " + (DateTime.Now - time).TotalMilliseconds);
        }
    }
}
