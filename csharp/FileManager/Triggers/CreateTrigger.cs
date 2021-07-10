using System;
using System.IO;
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
            Console.WriteLine(arguments.FileStorage.AddFile(File.ReadAllText(arguments.Args[2])));
        }
    }
}
