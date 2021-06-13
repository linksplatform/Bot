using Interfaces;
using System;

namespace FileManager
{
    public class ShowHandler : ITrigger<Arguments>
    {
        public bool Condition(Arguments arguments)
        {
            return (arguments.Args[0].ToLower() == "show");
        }
        public void Action(Arguments arguments)
        {
            Console.WriteLine(arguments.FileStorage.PutFile(arguments.Args[1]));
        }
    }
}
