using Interfaces;
using System;

namespace FileManager
{
    public class ShowTrigger : ITrigger<Context>
    {
        public bool Condition(Context arguments)
        {
            return (arguments.Args[0].ToLower() == "show");
        }
        public void Action(Context arguments)
        {
            Console.WriteLine(arguments.FileStorage.PutFile(arguments.Args[1]));
        }
    }
}
