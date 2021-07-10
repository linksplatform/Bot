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
            if (arguments.Args[1] == "allFiles")
            {
                foreach (IFile file in arguments.FileStorage.GetAllFiles())
                {
                    if (file.Content.Length < 50)
                    {
                        Console.WriteLine($"{file.Path}: {file.Content} (Hash: {file.Content.GetHashCode()})");
                    }
                    else
                    {
                        Console.WriteLine($"{file.Path}: {file.Content.Substring(0, 50)} ... {file.Content.Substring(file.Content.Length - 50, 50)} (Hash: {file.Content.GetHashCode()})");
                    }
                }
            }
            else
            {
                Console.WriteLine(arguments.FileStorage.GetFileContent(ulong.Parse(arguments.Args[1])));
            }
        }
    }
}
