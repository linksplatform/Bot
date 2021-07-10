using FileManager;
using Interfaces;
using Storage.Local;
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
            var files = arguments.FileStorage.GetFilesFromSet(arguments.Args[1]);
            foreach(var file in files)
            {
                Console.WriteLine($"Path: {file.Path}\nContent: {file.Content}");
            }
        }  
    }
}
