using Interfaces;
using System;

namespace FileManager
{
    public class ShowHandler : ITrigger<Arguments>
    {
        public bool Condition(Arguments arguments)
        {
            if (arguments.Args[0].ToLower() == "show")
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
            Console.WriteLine(arguments.FileStorage.PutFile(arguments.Args[1]));
        }
    }
}
