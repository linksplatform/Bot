using System;
using Interfaces;

namespace FileManager
{
    public class DeleteHandler : ITrigger<Arguments>
    {
        public bool Condition(Arguments arguments)
        {
            return (arguments.Args[0].ToLower() == "delete");
        }

        public void Action(Arguments arguments)
        {
            if (arguments.FileStorage.LinkExist(arguments.Args[1]))
            {
                arguments.FileStorage.Delete(arguments.Args[1]);
            }
            else
            {
                Console.WriteLine("File does not exist");
            }
        }
    }
}
