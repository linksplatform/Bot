using Interfaces;
using System;
using System.IO;

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
