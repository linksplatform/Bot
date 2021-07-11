using Interfaces;
using System;

namespace FileManager
{
    public class GetFilesByFileSetNameTrigger : ITrigger<Context>
    {
        public bool Condition(Context arguments) => arguments.Args[0].ToLower() == "getfilesbyfilessetname";

        public void Action(Context arguments)
        {
            var files = arguments.FileStorage.GetFilesFromSet(arguments.Args[1]);
            foreach (var file in files)
            {
                Console.WriteLine($"Path: {file.Path}\nContent: {file.Content}");
            }
        }
    }
}
