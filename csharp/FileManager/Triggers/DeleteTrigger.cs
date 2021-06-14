using System;
using Interfaces;

namespace FileManager
{
    public class DeleteTrigger : ITrigger<Context>
    {
        public bool Condition(Context arguments)
        {
            return (arguments.Args[0].ToLower() == "delete");
        }

        public void Action(Context arguments)
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
