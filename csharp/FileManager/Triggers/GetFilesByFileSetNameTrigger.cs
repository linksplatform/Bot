using Interfaces;
using System;
using System.Collections.Generic;

namespace FileManager
{
    public class GetFilesByFileSetNameTrigger : ITrigger<Context>
    {
        public bool Condition(Context arguments)
        {
            return arguments.Args[0].ToLower() == "getfilesbyfilessetname";
        }

        public void Action(Context arguments)
        {
            List<IFile> files = arguments.FileStorage.GetFilesFromSet(arguments.Args[1]);
            foreach (IFile file in files)
            {
                Console.WriteLine($"Path: {file.Path}\nContent: {file.Content}");
            }
        }
    }
}
