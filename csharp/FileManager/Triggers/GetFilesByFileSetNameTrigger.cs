using FileManager;
using Interfaces;
using Storage.Local;
using System;
using System.Collections.Generic;

namespace FileManager
{
    class GetFilesByFileSetNameTrigger : ITrigger<Context>
    {
        public void Action(Context arguments)
        {
            var a = arguments.FileStorage.GetFilesFromSet(arguments.Args[1]);
            foreach(var b in a)
            {
                Console.WriteLine($"Path: "+b.Path+"\n" +
                                  $"Content : " +b.Content);
            }
        }

        public bool Condition(Context arguments)
        {
            return arguments.Args[0].ToLower() == "getfilesbyfilessetname"; 
        }
    }
}
